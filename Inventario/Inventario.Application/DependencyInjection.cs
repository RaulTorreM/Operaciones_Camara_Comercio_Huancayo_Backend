using Inventario.Application.Interfaces;
using Inventario.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Inventario.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<ISupplyService, SupplyService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<ISummaryService, SummaryService>();
        return services;
    }
}
