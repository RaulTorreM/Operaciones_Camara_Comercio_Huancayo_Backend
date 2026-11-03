namespace DailyTasks.Application.DTOs.Users;

public record DailyTaskUserDto(
    int Id,
    string Username,
    string FullName,
    string Area,
    string AreaName,
    string Role);

public record DailyTaskAreaDto(string Code, string Name);
