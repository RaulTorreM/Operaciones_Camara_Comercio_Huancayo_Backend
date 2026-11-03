using Inventario.Application.Interfaces;
using Inventario.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Inventario.Infrastructure.Data;

public static class DatabaseInitializer
{
    /// <summary>
    /// Aplica las migraciones si <paramref name="autoMigrate"/> es true y luego
    /// ejecuta el seed idempotente del administrador/usuarios iniciales.
    /// En producción se recomienda AutoMigrate=false y aplicar migrations-inventory.sql a mano.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, bool autoMigrate)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<InventarioDbContext>();

        if (autoMigrate)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    // Nunca EnsureCreated: el esquema se crea con migraciones controladas.
                    if (db.Database.GetMigrations().Any()) await db.Database.MigrateAsync();
                    break;
                }
                catch when (attempt < 6) { await Task.Delay(3000); }
            }
        }

        // El seed es idempotente: solo crea usuarios si inventory.Users está vacío.
        // Si las tablas no existen, falla al arrancar para no dejar un backend a medias.
        await DemoDataSeeder.RunAsync(
            db,
            sp.GetRequiredService<IPasswordHasher>(),
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<IHostEnvironment>());
    }
}
