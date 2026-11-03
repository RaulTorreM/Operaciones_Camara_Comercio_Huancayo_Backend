using DailyTasks.Application.Common;
using DailyTasks.Application.DTOs.Auth;
using DailyTasks.Application.DTOs.Users;
using DailyTasks.Application.Interfaces;
using DailyTasks.Application.Mappings;
using DailyTasks.Core.Exceptions;
using DailyTasks.Core.Interfaces;

namespace DailyTasks.Application.Services;

public class DailyTaskAuthService(
    IDailyTaskUserRepository users,
    IDailyTaskPasswordHasher hasher,
    IDailyTaskJwtTokenService tokens,
    IDailyTaskCurrentUser current,
    IDailyTaskUnitOfWork uow) : IDailyTaskAuthService
{
    private const int MaxUsernameLength = 60;
    private const int MaxPasswordLength = 128;

    public async Task<DailyTaskLoginResponse> LoginAsync(DailyTaskLoginRequest request)
    {
        var username = (request.Username ?? "").Trim().ToLowerInvariant();
        var password = request.Password ?? "";

        // Mismo mensaje para todos los casos: no revela si la cuenta existe.
        if (username.Length is 0 or > MaxUsernameLength || password.Length is 0 or > MaxPasswordLength)
            throw new UnauthorizedException("Usuario o contraseña incorrectos.");

        var user = await users.GetByUsernameAsync(username);
        if (user is null || !user.IsActive || !hasher.Verify(password, user.PasswordHash))
            throw new UnauthorizedException("Usuario o contraseña incorrectos.");

        return new DailyTaskLoginResponse(tokens.CreateToken(user), user.ToDto());
    }

    public async Task<DailyTaskUserDto> GetMeAsync()
    {
        EnsureAuthenticated();

        var user = await users.GetByIdAsync(current.Id);
        if (user is null || !user.IsActive)
            throw new UnauthorizedException("La cuenta ya no está activa.");

        return user.ToDto();
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request)
    {
        EnsureAuthenticated();

        var user = await users.GetByIdAsync(current.Id)
            ?? throw new UnauthorizedException("La cuenta ya no está activa.");

        var currentPassword = request.CurrentPassword ?? "";
        var newPassword = request.NewPassword ?? "";

        if (!hasher.Verify(currentPassword, user.PasswordHash))
            throw new BusinessRuleException("La contraseña actual no es correcta.");

        if (newPassword.Length is < 8 or > MaxPasswordLength)
            throw new BusinessRuleException("La nueva contraseña debe tener entre 8 y 128 caracteres.");

        user.PasswordHash = hasher.Hash(newPassword);
        await uow.SaveChangesAsync();
    }

    private void EnsureAuthenticated()
    {
        if (current.Id <= 0) throw new UnauthorizedException("Sesión no válida.");
    }
}
