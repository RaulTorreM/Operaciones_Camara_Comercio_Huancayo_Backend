using DailyTasks.Application.Interfaces;
using DailyTasks.Infrastructure.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DailyTasks.Infrastructure.Data;

public static class DailyTaskDatabaseInitializer
{
    /// <summary>
    /// Aplica las migraciones si <paramref name="autoMigrate"/> es true y ejecuta
    /// el seed idempotente de usuarios y datos demo.
    /// En producción se recomienda AutoMigrate=false y aplicar migrations-dailytasks.sql a mano.
    /// </summary>
    public static async Task InitializeAsync(IServiceProvider services, bool autoMigrate)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<DailyTasksDbContext>();

        if (autoMigrate)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    if (db.Database.GetMigrations().Any()) await db.Database.MigrateAsync();
                    break;
                }
                catch when (attempt < 6) { await Task.Delay(3000); }
            }
        }

        await DailyTaskSeeder.RunAsync(
            db,
            sp.GetRequiredService<IDailyTaskPasswordHasher>(),
            sp.GetRequiredService<IConfiguration>(),
            sp.GetRequiredService<IHostEnvironment>());
    }
}
