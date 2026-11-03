using DailyTasks.Application.Common;
using DailyTasks.Application.Interfaces;
using DailyTasks.Core.Entities;
using DailyTasks.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace DailyTasks.Infrastructure.Data.Seed;

public static class DailyTaskSeeder
{
    public static async Task RunAsync(
        DailyTasksDbContext db,
        IDailyTaskPasswordHasher hasher,
        IConfiguration cfg,
        IHostEnvironment env)
    {
        var configuredPassword = cfg["DAILYTASKS_SEED_PASSWORD"] ?? cfg["DailyTasks:SeedPassword"];
        var configuredUsers = cfg.GetSection("DailyTasks:SeedUsers").Get<DailyTaskSeedUser[]>() ?? [];
        var usersToSeed = configuredUsers.Length > 0 ? configuredUsers : DefaultUsers().ToArray();

        // La contraseña global solo es obligatoria si algún usuario no tiene una propia.
        var needsFallbackPassword = usersToSeed.Any(u =>
            !string.IsNullOrWhiteSpace(u.Username) && string.IsNullOrWhiteSpace(u.Password));

        if (needsFallbackPassword)
        {
            if (string.IsNullOrWhiteSpace(configuredPassword))
            {
                if (!env.IsDevelopment())
                    throw new InvalidOperationException(
                        "Define DAILYTASKS_SEED_PASSWORD o una contraseña por usuario en DailyTasks:SeedUsers.");

                // Solo en desarrollo: permite arrancar sin configurar nada.
                configuredPassword = "demo1234";
            }

            if (configuredPassword.Length < 8)
                throw new InvalidOperationException("DAILYTASKS_SEED_PASSWORD debe tener al menos 8 caracteres.");
        }

        // Permite aplicar las contraseñas del seed a usuarios ya existentes.
        // Úsalo una sola vez y luego vuelve a false para no pisar cambios de contraseña.
        var updatePasswords = cfg["DAILYTASKS_SEED_UPDATE_PASSWORDS"] is { Length: > 0 } updateValue
            && bool.TryParse(updateValue, out var updateEnabled) && updateEnabled;

        foreach (var seed in usersToSeed)
        {
            if (string.IsNullOrWhiteSpace(seed.Username)) continue;

            var username = seed.Username.Trim().ToLowerInvariant();

            var area = string.IsNullOrWhiteSpace(seed.Area)
                ? DailyTaskAreas.GerenciaGeneral
                : seed.Area.Trim().ToUpperInvariant();
            if (!DailyTaskAreas.IsValid(area))
                throw new InvalidOperationException($"Área inválida para el usuario {username}: {area}");

            var role = string.IsNullOrWhiteSpace(seed.Role)
                ? DailyTaskRoles.User
                : seed.Role.Trim().ToUpperInvariant();

            if (role is not (DailyTaskRoles.User or DailyTaskRoles.Manager))
                throw new InvalidOperationException($"Rol inválido para el usuario {username}: {role}");

            var password = string.IsNullOrWhiteSpace(seed.Password) ? configuredPassword! : seed.Password;
            if (password.Length < 8)
                throw new InvalidOperationException($"La contraseña del usuario {username} debe tener al menos 8 caracteres.");

            var existing = await db.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (existing is not null)
            {
                if (updatePasswords)
                {
                    existing.PasswordHash = hasher.Hash(password);
                    existing.FullName = string.IsNullOrWhiteSpace(seed.FullName) ? seed.Username.Trim() : seed.FullName.Trim();
                    existing.Area = area;
                    existing.Role = role;
                    existing.IsActive = true;
                }

                continue;
            }

            db.Users.Add(new DailyTaskUser
            {
                Username = username,
                FullName = string.IsNullOrWhiteSpace(seed.FullName) ? seed.Username.Trim() : seed.FullName.Trim(),
                Area = area,
                Role = role,
                PasswordHash = hasher.Hash(password),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();

        // Datos de demostración: en desarrollo por defecto; en producción solo con DAILYTASKS_SEED_DEMO=true.
        var seedDemo = cfg["DAILYTASKS_SEED_DEMO"] is { Length: > 0 } value
            ? bool.TryParse(value, out var enabled) && enabled
            : env.IsDevelopment();

        if (!seedDemo || await db.Tasks.AnyAsync()) return;

        var users = await db.Users.OrderBy(u => u.Id).ToListAsync();
        if (users.Count == 0) return;

        var today = DailyTaskTime.Today;
        var now = DateTime.UtcNow;

        var demo = new (string Title, string? Description, int Assignee, DailyTaskStatus Status, DailyTaskPriority Priority, int DueOffset)[]
        {
            ("Actualizar padrón de asociados", "Revisar altas y bajas del mes.", 0, DailyTaskStatus.Pending, DailyTaskPriority.High, 0),
            ("Conciliar caja chica", "Cuadrar comprobantes con el reporte del día.", 1, DailyTaskStatus.InProgress, DailyTaskPriority.Medium, 1),
            ("Enviar recordatorios de mora", "Llamar a los asociados con cuotas vencidas.", 2, DailyTaskStatus.Pending, DailyTaskPriority.High, -1),
            ("Publicar campaña de redes", "Programar piezas para la semana.", 3, DailyTaskStatus.InProgress, DailyTaskPriority.Low, 3),
            ("Archivar actas del consejo", "Digitalizar y registrar las actas pendientes.", 4, DailyTaskStatus.Done, DailyTaskPriority.Medium, -2),
            ("Revisar stock de útiles", "Coordinar reposición con logística.", 5, DailyTaskStatus.Pending, DailyTaskPriority.Medium, 2),
            ("Preparar expediente de arbitraje", "Ordenar documentos del caso programado.", 6, DailyTaskStatus.InProgress, DailyTaskPriority.High, 4),
            ("Cerrar conciliación contable", "Validar asientos y saldos del mes.", 7, DailyTaskStatus.Done, DailyTaskPriority.High, -3),
            ("Seguimiento a socios fidelizados", "Contactar a los asociados del programa.", 8, DailyTaskStatus.Pending, DailyTaskPriority.Low, 5),
            ("Actualizar base de fidelización", "Depurar duplicados y validar correos.", 9, DailyTaskStatus.Pending, DailyTaskPriority.Medium, 0),
            ("Revisar indicadores de gerencia", "Consolidar avances de todas las áreas.", 10, DailyTaskStatus.InProgress, DailyTaskPriority.High, 1)
        };

        foreach (var item in demo)
        {
            var assignee = users[Math.Min(item.Assignee, users.Count - 1)];
            var createdOffset = item.Status == DailyTaskStatus.Done ? 4 : 1;
            var createdAt = now.AddDays(-createdOffset);
            var task = new DailyTask
            {
                Title = item.Title,
                Description = item.Description,
                Status = item.Status,
                Priority = item.Priority,
                Area = assignee.Area,
                CreatedById = assignee.Id,
                CreatedBy = assignee,
                AssignedToId = assignee.Id,
                AssignedTo = assignee,
                CreatedAt = createdAt,
                CreatedOn = today.AddDays(-createdOffset),
                DueDate = today.AddDays(item.DueOffset),
                UpdatedAt = now,
                CompletedAt = item.Status == DailyTaskStatus.Done ? now.AddDays(-1) : null,
                CompletedOn = item.Status == DailyTaskStatus.Done ? today.AddDays(-1) : null
            };

            task.History.Add(new DailyTaskHistory
            {
                ChangedById = assignee.Id,
                ChangedBy = assignee,
                ChangedAt = createdAt,
                ChangeGroupId = Guid.NewGuid(),
                Action = "CREATED",
                NewValue = item.Title
            });

            db.Tasks.Add(task);
        }

        await db.SaveChangesAsync();
    }

    private static List<DailyTaskSeedUser> DefaultUsers() =>
    [
        new() { Username = "milagros", FullName = "Milagros", Area = DailyTaskAreas.GestionAsociado, Role = DailyTaskRoles.User },
        new() { Username = "brizeth", FullName = "Brizeth", Area = DailyTaskAreas.Tesoreria, Role = DailyTaskRoles.User },
        new() { Username = "arely", FullName = "Arely", Area = DailyTaskAreas.ProtestosMoras, Role = DailyTaskRoles.User },
        new() { Username = "maria", FullName = "Maria", Area = DailyTaskAreas.Marketing, Role = DailyTaskRoles.User },
        new() { Username = "maribel", FullName = "Maribel", Area = DailyTaskAreas.Secretaria, Role = DailyTaskRoles.User },
        new() { Username = "johonny", FullName = "Johonny", Area = DailyTaskAreas.Logistica, Role = DailyTaskRoles.User },
        new() { Username = "patricia", FullName = "Patricia", Area = DailyTaskAreas.CorteArbitraje, Role = DailyTaskRoles.User },
        new() { Username = "ginger", FullName = "Ginger", Area = DailyTaskAreas.Contabilidad, Role = DailyTaskRoles.User },
        new() { Username = "carmen", FullName = "Carmen", Area = DailyTaskAreas.Fidelizacion, Role = DailyTaskRoles.User },
        new() { Username = "german", FullName = "German", Area = DailyTaskAreas.Cobranzas, Role = DailyTaskRoles.User },
        new() { Username = "klaus", FullName = "Klaus", Area = DailyTaskAreas.GerenciaGeneral, Role = DailyTaskRoles.Manager }
    ];
}

/// <summary>Usuario inicial configurable en DailyTasks:SeedUsers.</summary>
public sealed class DailyTaskSeedUser
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? FullName { get; set; }
    public string? Area { get; set; }
    public string? Role { get; set; }
}
