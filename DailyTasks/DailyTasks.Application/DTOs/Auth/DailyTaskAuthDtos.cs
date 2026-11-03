using DailyTasks.Application.DTOs.Users;

namespace DailyTasks.Application.DTOs.Auth;

public record DailyTaskLoginRequest(string? Username, string? Password);

public record DailyTaskLoginResponse(string Token, DailyTaskUserDto User);

public record ChangePasswordRequest(string? CurrentPassword, string? NewPassword);
