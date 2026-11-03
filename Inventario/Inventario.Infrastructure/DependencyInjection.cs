using Inventario.Application.Interfaces;
using Inventario.Core.Interfaces;
using Inventario.Infrastructure.Data;
using Inventario.Infrastructure.Repositories;
using Inventario.Infrastructure.Security;
using Inventario.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventario.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddDbContext<InventarioDbContext>(o =>
            o.UseNpgsql(ConnectionStringResolver.Resolve(cfg), npgsql =>
                // Historial de migraciones propio, dentro del esquema inventory.
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "inventory")));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<ISupplyRepository, SupplyRepository>();
        services.AddScoped<ILoanRepository, LoanRepository>();

        services.AddSingleton(JwtOptions.FromConfiguration(cfg));
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddSingleton<IFileStorage, CloudinaryFileStorage>();
        return services;
    }
}
