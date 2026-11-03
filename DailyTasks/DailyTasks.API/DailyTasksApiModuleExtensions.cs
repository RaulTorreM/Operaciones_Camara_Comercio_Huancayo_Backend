using System.Text.Json;
using DailyTasks.API.Hubs;
using DailyTasks.API.Security;
using DailyTasks.API.Services;
using DailyTasks.Application;
using DailyTasks.Application.Interfaces;
using DailyTasks.Core.Enums;
using DailyTasks.Infrastructure;
using DailyTasks.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DailyTasks.API;

/// <summary>
/// Punto único de integración del módulo DailyTasks en su host.
/// El código de DailyTasks vive aislado en los proyectos DailyTasks.*;
/// aquí solo se expone el "enchufe" que usa el host.
/// </summary>
public static class DailyTasksApiModuleExtensions
{
    /// <summary>Registra servicios del módulo (API + aplicación + infraestructura + SignalR).</summary>
    public static IServiceCollection AddDailyTasksApi(this IServiceCollection services, IConfiguration cfg)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<IDailyTaskCurrentUser, DailyTaskCurrentUserService>();
        services.AddDailyTasksApplication();
        services.AddDailyTasksInfrastructure(cfg);
        services.AddSignalR().AddJsonProtocol(options =>
            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
        services.AddScoped<IDailyTaskNotifier, SignalRDailyTaskNotifier>();
        return services;
    }

    /// <summary>Esquema JWT propio de DailyTasks, independiente del inventario.</summary>
    public static AuthenticationBuilder AddDailyTasksJwt(this AuthenticationBuilder builder, IConfiguration cfg)
    {
        var jwt = DailyTaskJwtOptions.FromConfiguration(cfg);

        return builder.AddJwtBearer(DailyTasksAuth.Scheme, options =>
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

            // SignalR no puede enviar el header Authorization desde el navegador:
            // solo para el hub se acepta el token en el query string.
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(accessToken)
                        && context.HttpContext.Request.Path.StartsWithSegments("/hubs/dailytasks"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });
    }

    /// <summary>Políticas de autorización de DailyTasks.</summary>
    public static AuthorizationOptions AddDailyTasksAuthorization(this AuthorizationOptions options)
    {
        options.AddPolicy(DailyTasksAuth.UserPolicy, policy => policy
            .AddAuthenticationSchemes(DailyTasksAuth.Scheme)
            .RequireAuthenticatedUser());

        options.AddPolicy(DailyTasksAuth.ManagerPolicy, policy => policy
            .AddAuthenticationSchemes(DailyTasksAuth.Scheme)
            .RequireAuthenticatedUser()
            .RequireRole(DailyTaskRoles.Manager));

        return options;
    }

    /// <summary>Mapea los endpoints propios del módulo.</summary>
    public static IEndpointRouteBuilder MapDailyTasksApi(this IEndpointRouteBuilder app)
    {
        app.MapHub<DailyTasksHub>("/hubs/dailytasks");
        return app;
    }
}
