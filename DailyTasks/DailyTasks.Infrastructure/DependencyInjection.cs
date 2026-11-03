using DailyTasks.Application.Interfaces;
using DailyTasks.Core.Interfaces;
using DailyTasks.Infrastructure.Data;
using DailyTasks.Infrastructure.Repositories;
using DailyTasks.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DailyTasks.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDailyTasksInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<DailyTasksDbContext>(o =>
            o.UseNpgsql(DailyTaskConnectionStringResolver.Resolve(cfg), npgsql =>
                // Historial de migraciones propio, dentro del esquema dailytasks.
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "dailytasks")));

        services.AddScoped<IDailyTaskUnitOfWork, DailyTaskUnitOfWork>();
        services.AddScoped<IDailyTaskUserRepository, DailyTaskUserRepository>();
        services.AddScoped<IDailyTaskRepository, DailyTaskRepository>();
        services.AddScoped<IDailyTaskHistoryRepository, DailyTaskHistoryRepository>();

        services.AddSingleton(DailyTaskJwtOptions.FromConfiguration(cfg));
        services.AddSingleton<IDailyTaskJwtTokenService, DailyTaskJwtTokenService>();
        services.AddSingleton<IDailyTaskPasswordHasher, DailyTaskBCryptPasswordHasher>();
        return services;
    }
}
