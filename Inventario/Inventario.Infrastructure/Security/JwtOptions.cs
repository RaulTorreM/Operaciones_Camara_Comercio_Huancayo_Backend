using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Inventario.Infrastructure.Security;

public class JwtOptions
{
    public string Key { get; init; } = "";
    public int ExpireDays { get; init; } = 7;
    public string? Issuer { get; init; }

    public SymmetricSecurityKey SigningKey => new(Encoding.UTF8.GetBytes(Key));

    public static JwtOptions FromConfiguration(IConfiguration cfg)
    {
        // Clave PROPIA del inventario: no comparte la clave de la web.
        var key = cfg["Inventario:Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key)) key = cfg["JWT_KEY"];

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException(
                "Falta la clave JWT del inventario. Define Inventario:Jwt:Key o la variable JWT_KEY.");

        if (key.Length < 32)
            throw new InvalidOperationException(
                "La clave JWT del inventario debe tener al menos 32 caracteres.");

        var days = 7;
        if (int.TryParse(cfg["Inventario:Jwt:ExpireDays"], out var configuredDays))
            days = configuredDays;

        if (days is < 1 or > 30)
            throw new InvalidOperationException(
                "Inventario:Jwt:ExpireDays debe estar entre 1 y 30 días.");

        // Si no hay issuer propio, se usa el de la web. Si tampoco existe, no se valida.
        var issuer = cfg["Inventario:Jwt:Issuer"];
        if (string.IsNullOrWhiteSpace(issuer)) issuer = cfg["JWT:ISSUER"];

        return new JwtOptions
        {
            Key = key,
            ExpireDays = days,
            Issuer = string.IsNullOrWhiteSpace(issuer) ? null : issuer
        };
    }
}
