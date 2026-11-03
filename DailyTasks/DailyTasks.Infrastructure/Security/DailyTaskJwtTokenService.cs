using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using DailyTasks.Application.Interfaces;
using DailyTasks.Core.Entities;
using Microsoft.IdentityModel.Tokens;

namespace DailyTasks.Infrastructure.Security;

public class DailyTaskJwtTokenService(DailyTaskJwtOptions options) : IDailyTaskJwtTokenService
{
    public string CreateToken(DailyTaskUser user)
    {
        var token = new JwtSecurityToken(
            issuer: string.IsNullOrWhiteSpace(options.Issuer) ? null : options.Issuer,
            claims:
            [
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("area", user.Area),
                new Claim("username", user.Username)
            ],
            expires: DateTime.UtcNow.AddDays(options.ExpireDays),
            signingCredentials: new SigningCredentials(options.SigningKey, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
