using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Inventario.Application.Interfaces;
using Inventario.Core.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Inventario.Infrastructure.Security;

public class JwtTokenService(JwtOptions options) : IJwtTokenService
{
    public string CreateToken(User user)
    {
        var token = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(options.Issuer) ? null : options.Issuer,
            claims: new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role)
            },
            expires: DateTime.UtcNow.AddDays(options.ExpireDays),
            signingCredentials: new SigningCredentials(options.SigningKey, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
