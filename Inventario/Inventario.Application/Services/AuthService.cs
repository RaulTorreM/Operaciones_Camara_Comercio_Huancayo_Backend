using Inventario.Application.DTOs.Auth;
using Inventario.Application.Interfaces;
using Inventario.Core.Exceptions;
using Inventario.Core.Interfaces;

namespace Inventario.Application.Services;

public class AuthService(IUserRepository users, IPasswordHasher hasher, IJwtTokenService tokens) : IAuthService
{
    private const int MaxUsernameLength = 60;
    private const int MaxPasswordLength = 128;

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var username = (request.Username ?? "").Trim().ToLowerInvariant();
        var password = request.Password ?? "";

        // Mismo mensaje para usuario inválido, contraseña vacía o textos demasiado largos:
        // no revela si la cuenta existe y evita hashing de entradas gigantes.
        if (username.Length is 0 or > MaxUsernameLength || password.Length is 0 or > MaxPasswordLength)
            throw new UnauthorizedException("Usuario o contraseña incorrectos.");

        var user = await users.GetByUsernameAsync(username);
        if (user is null || !hasher.Verify(password, user.PasswordHash))
            throw new UnauthorizedException("Usuario o contraseña incorrectos.");

        return new LoginResponse(tokens.CreateToken(user), user.FullName, user.Role);
    }
}
