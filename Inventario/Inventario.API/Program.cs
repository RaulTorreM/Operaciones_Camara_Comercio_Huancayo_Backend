using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using DailyTasks.API;
using DailyTasks.API.Controllers;
using DailyTasks.Infrastructure.Data;
using DotNetEnv;
using Inventario.API;
using Inventario.API.Middleware;
using Inventario.Infrastructure.Data;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

// En local, el .env puede estar en la raíz del repo o en una carpeta padre.
// En producción (Railway/Docker) se usan variables de entorno.
var envFile = FindEnvFile(Directory.GetCurrentDirectory());
if (envFile is not null) Env.Load(envFile);

var builder = WebApplication.CreateBuilder(args);

// Railway define PORT.
var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port)) builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

builder.Services
    .AddControllers(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddApplicationPart(typeof(DailyTasksController).Assembly)
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = _ =>
            new BadRequestObjectResult(new { error = "Datos inválidos. Revisa los campos enviados." });
    });

// Servicios del inventario: Application + Infrastructure + usuario actual.
builder.Services.AddInventarioModule(builder.Configuration);

// Módulo DailyTasks: API, aplicación, infraestructura, SignalR y usuario actual.
builder.Services.AddDailyTasksApi(builder.Configuration);

// Cada módulo usa su PROPIO esquema JWT, separado de la web y entre sí.
builder.Services
    .AddAuthentication()
    .AddInventarioJwt(builder.Configuration)
    .AddDailyTasksJwt(builder.Configuration);

builder.Services.AddAuthorization(options =>
{
    options.AddInventarioAuthorization();
    options.AddDailyTasksAuthorization();
});

// CORS: frontend de Inventario. En Development también se aceptan los puertos
// habituales de Vite para no depender de una configuración extra.
var allowedOrigins = new List<string?>
{
    builder.Configuration["Inventario:FrontendURL"] ?? "http://localhost:5173",
    builder.Configuration["DailyTasks:FrontendURL"] ?? "http://localhost:5175"
};

if (builder.Environment.IsDevelopment())
{
    allowedOrigins.AddRange(
    [
        "http://localhost:5173",
        "http://localhost:5174",
        "http://localhost:5175",
        "http://localhost:5176",
        "http://127.0.0.1:5173",
        "http://127.0.0.1:5174",
        "http://127.0.0.1:5175",
        "http://127.0.0.1:5176"
    ]);
}

var normalizedOrigins = allowedOrigins
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin!.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

builder.Services.AddCors(options =>
{
    options.AddPolicy("InventarioFrontend", policy =>
        policy.WithOrigins(normalizedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Content-Disposition"));
});

// Railway/containers reciben la IP real en X-Forwarded-For.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Límite de intentos de login por IP y endpoint.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"{context.Connection.RemoteIpAddress?.ToString() ?? "sin-ip"}:{context.Request.Path}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    // Consulta pública por etiqueta: límite por IP para evitar pruebas masivas.
    options.AddPolicy("inventario-public", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"public:{context.Connection.RemoteIpAddress?.ToString() ?? "sin-ip"}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Demasiados intentos. Espera unos minutos e intenta de nuevo." }, token);
    };
});

var app = builder.Build();

// Las herramientas de EF (dotnet ef) construyen la app sin arrancarla: no tocar la base.
// En producción se recomienda AutoMigrate=false y aplicar migrations-inventory.sql a mano.
if (!EF.IsDesignTime)
{
    await DatabaseInitializer.InitializeAsync(
        app.Services,
        app.Configuration.GetValue<bool>("Inventario:AutoMigrate"));

    await DailyTaskDatabaseInitializer.InitializeAsync(
        app.Services,
        app.Configuration.GetValue<bool>("DailyTasks:AutoMigrate"));
}

app.UseForwardedHeaders();
app.UseWebSockets();
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.Use(async (context, next) =>
{
    context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
    await next();
});

app.UseCors("InventarioFrontend");
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { ok = true }));

app.MapGet("/health/ready", async (InventarioDbContext db, IConfiguration cfg) =>
{
    var databaseOk = await db.Database.CanConnectAsync();
    var cloudName = cfg["Cloudinary:CloudName"]?.Trim();
    var apiKey = cfg["Cloudinary:ApiKey"]?.Trim();
    var apiSecret = cfg["Cloudinary:ApiSecret"]?.Trim();
    var storageConfigured = !string.IsNullOrWhiteSpace(cloudName) && cloudName != "tu-cloud-name"
        && !string.IsNullOrWhiteSpace(apiKey) && !string.IsNullOrWhiteSpace(apiSecret);

    if (!databaseOk)
        return Results.Json(
            new { ok = false, database = false, storage = storageConfigured ? "configured" : "not-configured" },
            statusCode: StatusCodes.Status503ServiceUnavailable);

    return Results.Ok(new
    {
        ok = true,
        database = true,
        storage = storageConfigured ? "configured" : "not-configured"
    });
});

app.MapControllers();
app.MapDailyTasksApi();

app.Run();

static string? FindEnvFile(string start)
{
    var dir = new DirectoryInfo(start);
    for (var i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
    {
        var candidate = Path.Combine(dir.FullName, ".env");
        if (File.Exists(candidate)) return candidate;
    }

    return null;
}
