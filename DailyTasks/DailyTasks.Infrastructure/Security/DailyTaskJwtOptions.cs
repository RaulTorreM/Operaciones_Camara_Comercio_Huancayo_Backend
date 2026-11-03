using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace DailyTasks.Infrastructure.Security;

public class DailyTaskJwtOptions
{
    public string Key { get; init; } = "";
    public int ExpireDays { get; init; } = 7;
    public string? Issuer { get; init; }

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));

    public static DailyTaskJwtOptions FromConfiguration(IConfiguration cfg)
    {
        // Clave PROPIA de DailyTasks: no comparte la clave del inventario ni la de la web.
        var key = cfg["DailyTasks:Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key)) key = cfg["DAILYTASKS_JWT_KEY"];

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "Falta la clave JWT de DailyTasks. Define DailyTasks:Jwt:Key o la variable DAILYTASKS_JWT_KEY.");

        if (key.Length < 32)
            throw new InvalidOperationException(
                "La clave JWT de DailyTasks debe tener al menos 32 caracteres.");

        var days = 7;
        if (int.TryParse(cfg["DailyTasks:Jwt:ExpireDays"], out var configuredDays))
            days = configuredDays;

        if (days is < 1 or > 30)
            throw new InvalidOperationException(
                "DailyTasks:Jwt:ExpireDays debe estar entre 1 y 30 días.");

        var issuer = cfg["DailyTasks:Jwt:Issuer"];
        if (string.IsNullOrWhiteSpace(issuer)) issuer = "CamaraComercio.DailyTasks";

        return new DailyTaskJwtOptions
        {
            Key = key,
            ExpireDays = days,
            Issuer = issuer
        };
    }
}
