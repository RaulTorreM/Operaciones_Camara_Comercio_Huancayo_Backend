using Inventario.API.Security;
using Inventario.API.Services;
using Inventario.Application;
using Inventario.Application.Interfaces;
using Inventario.Core.Enums;
using Inventario.Infrastructure;
using Inventario.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Inventario.API;

/// <summary>
/// Punto único de integración del módulo de inventario en su host.
/// El código del inventario vive aislado en los proyectos Inventario.*;
/// aquí solo se expone el "enchufe" que usa el host.
/// </summary>
public static class InventarioModuleExtensions
{
    /// <summary>Registra servicios del inventario (aplicación + infraestructura).</summary>
    public static IServiceCollection AddInventarioModule(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUserService>();
        services.AddApplication();
        services.AddInfrastructure(cfg);
        return services;
    }

    /// <summary>Esquema JWT propio del inventario (clave e issuer independientes de la web).</summary>
    public static AuthenticationBuilder AddInventarioJwt(this AuthenticationBuilder builder, IConfiguration cfg)
    {
        var jwt = JwtOptions.FromConfiguration(cfg);

        return builder.AddJwtBearer(InventarioAuth.Scheme, options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = !string.IsNullOrWhiteSpace(jwt.Issuer),
                ValidIssuer = jwt.Issuer,
                ValidateAudience = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = jwt.SigningKey,
                ClockSkew = TimeSpan.FromMinutes(2)
            };
        });
    }

    /// <summary>Política de autorización exclusiva del inventario.</summary>
    public static AuthorizationOptions AddInventarioAuthorization(this AuthorizationOptions options)
    {
        options.AddPolicy(InventarioAuth.AdminPolicy, policy => policy
            .AddAuthenticationSchemes(InventarioAuth.Scheme)
            .RequireAuthenticatedUser()
            .RequireRole(Roles.Admin));

        return options;
    }
}
