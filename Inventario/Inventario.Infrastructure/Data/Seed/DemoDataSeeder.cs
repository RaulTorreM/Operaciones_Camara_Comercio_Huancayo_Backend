using Inventario.Application.Common;
using Inventario.Application.Interfaces;
using Inventario.Core.Entities;
using Inventario.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Inventario.Infrastructure.Data.Seed;

public static class DemoDataSeeder
{
    public static async Task RunAsync(InventarioDbContext db, IPasswordHasher hasher, IConfiguration cfg, IHostEnvironment env)
    {
        if (!await db.Users.AnyAsync())
        {
            var adminPassword = cfg["ADMIN_PASSWORD"];
            if (string.IsNullOrWhiteSpace(adminPassword))
            {
                if (!env.IsDevelopment())
                    throw new InvalidOperationException(
                        "Define la variable ADMIN_PASSWORD para crear el usuario administrador del inventario.");

                // Solo en desarrollo: permite arrancar sin configurar nada.
                adminPassword = "demo1234";
            }

            if (adminPassword.Length < 8)
                throw new InvalidOperationException("ADMIN_PASSWORD debe tener al menos 8 caracteres.");

            db.Users.Add(new User
            {
                Username = (cfg["ADMIN_USER"] ?? "admin").Trim().ToLowerInvariant(),
                FullName = "Administrador",
                Role = Roles.Admin,
                PasswordHash = hasher.Hash(adminPassword)
            });
            await db.SaveChangesAsync();
        }

        // Usuarios adicionales del inventario (configurables, sin tocar código).
        var extraUsers = cfg.GetSection("Inventario:SeedUsers").Get<SeedUser[]>() ?? [];
        foreach (var su in extraUsers)
        {
            if (string.IsNullOrWhiteSpace(su.Username) || string.IsNullOrWhiteSpace(su.Password)) continue;

            var username = su.Username.Trim().ToLowerInvariant();
            if (await db.Users.AnyAsync(u => u.Username == username)) continue;

            db.Users.Add(new User
            {
                Username = username,
                FullName = string.IsNullOrWhiteSpace(su.FullName) ? su.Username.Trim() : su.FullName!,
                Role = string.IsNullOrWhiteSpace(su.Role) ? Roles.Admin : su.Role!,
                PasswordHash = hasher.Hash(su.Password)
            });
        }
        if (extraUsers.Length > 0) await db.SaveChangesAsync();

        // Datos de demostración: en desarrollo por defecto; en producción solo con SEED_DEMO=true.
        var seedDemo = cfg["SEED_DEMO"] is { Length: > 0 } seedValue
            ? bool.TryParse(seedValue, out var enabled) && enabled
            : env.IsDevelopment();

        if (!seedDemo || await db.Rooms.AnyAsync()) return;

        // ---- Datos de demostración (ficticios) ----
        var rooms = new[] { "Almacén", "Auditorio", "Sala A", "Sala B", "Sala C", "Sala de Gerencia" }
            .Select(n => new Room { Name = n }).ToList();
        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync();
        var room = rooms.ToDictionary(r => r.Name);

        const AssetType I = AssetType.Individual, L = AssetType.Lot;
        const AssetCondition G = AssetCondition.Good, F = AssetCondition.Fair, P = AssetCondition.Poor;
        const VerificationStatus V = VerificationStatus.Verified, N = VerificationStatus.Pending;

        var data = new (string? code, string name, string cat, AssetType type, int qty, string room, AssetCondition cond, VerificationStatus ver)[]
        {
            ("CCH-0001", "Proyector Epson X49",            "Equipos",    I, 1,   "Almacén",          G, V),
            ("CCH-0002", "Proyector BenQ MS550",           "Equipos",    I, 1,   "Almacén",          G, V),
            ("CCH-0003", "Laptop Lenovo IdeaPad 3",        "Equipos",    I, 1,   "Sala A",           G, V),
            ("CCH-0004", "Laptop HP 14 pulgadas",          "Equipos",    I, 1,   "Sala A",           F, V),
            (null,       "Silla apilable",                 "Mobiliario", L, 40,  "Sala A",           G, V),
            (null,       "Mesa plegable",                  "Mobiliario", L, 12,  "Sala A",           F, N),
            ("CCH-0005", "Televisor LG 55\"",              "Equipos",    I, 1,   "Sala B",           G, N),
            ("CCH-0006", "Parlante JBL PartyBox",          "Audio",      I, 1,   "Auditorio",        G, N),
            ("CCH-0007", "Micrófono inalámbrico Shure",    "Audio",      I, 1,   "Auditorio",        G, N),
            ("CCH-0008", "Micrófono inalámbrico Shure",    "Audio",      I, 1,   "Auditorio",        F, N),
            ("CCH-0009", "Consola de sonido Behringer",    "Audio",      I, 1,   "Auditorio",        G, N),
            (null,       "Silla de auditorio",             "Mobiliario", L, 120, "Auditorio",        G, N),
            ("CCH-0010", "Pantalla de proyección 2x2 m",   "Equipos",    I, 1,   "Auditorio",        G, N),
            ("CCH-0011", "Impresora Epson L3250",          "Oficina",    I, 1,   "Sala B",           G, N),
            ("CCH-0012", "Computadora de escritorio Dell", "Equipos",    I, 1,   "Sala B",           F, N),
            ("CCH-0013", "Computadora de escritorio Dell", "Equipos",    I, 1,   "Sala B",           G, N),
            (null,       "Escritorio de madera",           "Mobiliario", L, 8,   "Sala B",           G, N),
            ("CCH-0014", "Aire acondicionado portátil",    "Equipos",    I, 1,   "Sala C",           P, N),
            ("CCH-0015", "Pizarra acrílica 1.2 x 2 m",     "Oficina",    I, 1,   "Sala C",           G, N),
            (null,       "Mesa de reuniones",              "Mobiliario", L, 3,   "Sala C",           G, N),
            ("CCH-0016", "Laptop Dell Latitude",           "Equipos",    I, 1,   "Sala de Gerencia", G, N),
            ("CCH-0017", "Televisor Samsung 43\"",         "Equipos",    I, 1,   "Sala de Gerencia", G, N),
            ("CCH-0018", "Extensión eléctrica 10 m",       "Eléctrico",  I, 1,   "Almacén",          G, N),
            ("CCH-0019", "Extensión eléctrica 10 m",       "Eléctrico",  I, 1,   "Almacén",          G, N),
            (null,       "Mantel blanco para evento",      "Eventos",    L, 25,  "Almacén",          G, N),
        };

        var assets = data.Select(d => new Asset
        {
            Code = d.code, Name = d.name, Category = d.cat, Type = d.type, Quantity = d.qty,
            RoomId = room[d.room].Id, Condition = d.cond, Verification = d.ver,
            LastVerifiedAt = d.ver == V ? DateTime.UtcNow.AddDays(-2) : null
        }).ToList();
        db.Assets.AddRange(assets);

        var supplies = new List<Supply>
        {
            new() { Name = "Hojas bond A4",           Unit = "hojas", Mode = SupplyMode.Quantity, Stock = 1500, MinStock = 500 },
            new() { Name = "Lapiceros azules",        Unit = "unid.", Mode = SupplyMode.Quantity, Stock = 120,  MinStock = 30 },
            new() { Name = "Lapiceros negros",        Unit = "unid.", Mode = SupplyMode.Quantity, Stock = 22,   MinStock = 30 },
            new() { Name = "Tóner HP 105A",           Unit = "unid.", Mode = SupplyMode.Quantity, Stock = 3,    MinStock = 1 },
            new() { Name = "Botellas de agua 500 ml", Unit = "unid.", Mode = SupplyMode.Quantity, Stock = 96,   MinStock = 24 },
            new() { Name = "Folders manila",          Unit = "unid.", Mode = SupplyMode.Quantity, Stock = 60,   MinStock = 20 },
            new() { Name = "Plumones de pizarra",     Unit = "",      Mode = SupplyMode.Visual,   Level = 2 },
            new() { Name = "Clips y grapas",          Unit = "",      Mode = SupplyMode.Visual,   Level = 3 },
            new() { Name = "Cinta adhesiva",          Unit = "",      Mode = SupplyMode.Visual,   Level = 1 },
        };
        supplies[0].Movements.Add(new SupplyMovement { At = DateTime.UtcNow.AddDays(-9), Type = MovementType.In,  Quantity = 2000, ResultingStock = 2000, User = "Administrador" });
        supplies[0].Movements.Add(new SupplyMovement { At = DateTime.UtcNow.AddDays(-3), Type = MovementType.Out, Quantity = 500,  ResultingStock = 1500, Destination = "Evento de asociados", User = "Administrador" });
        supplies[1].Movements.Add(new SupplyMovement { At = DateTime.UtcNow.AddDays(-4), Type = MovementType.Out, Quantity = 30,   ResultingStock = 120,  Destination = "Sala A", User = "Administrador" });
        db.Supplies.AddRange(supplies);

        // Dos préstamos de ejemplo: uno vencido y uno vigente
        var a2 = assets.First(a => a.Code == "CCH-0002");
        var a6 = assets.First(a => a.Code == "CCH-0006");
        a2.OnLoan = true; a6.OnLoan = true;
        var today = PeruTime.Today;
        db.Loans.Add(new Loan
        {
            Borrower = "Constructora Andina SAC", OutAt = DateTime.UtcNow.AddDays(-6), DueDate = today.AddDays(-2),
            User = "Administrador", Notes = "Capacitación de personal", Items = { new LoanItem { Asset = a2 } }
        });
        db.Loans.Add(new Loan
        {
            Borrower = "Asociación de Comerciantes del Valle", OutAt = DateTime.UtcNow.AddDays(-1), DueDate = today.AddDays(3),
            User = "Administrador", Notes = "Feria comercial", Items = { new LoanItem { Asset = a6 } }
        });
        await db.SaveChangesAsync();
    }
}

/// <summary>Usuario inicial del inventario definido en configuración (Inventario:SeedUsers).</summary>
public sealed class SeedUser
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string? FullName { get; set; }
    public string? Role { get; set; }
}
