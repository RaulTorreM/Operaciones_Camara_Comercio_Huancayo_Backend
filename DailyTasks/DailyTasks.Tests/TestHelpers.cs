using DailyTasks.Application.Common;
using DailyTasks.Application.Interfaces;
using DailyTasks.Core.Entities;
using DailyTasks.Core.Enums;

namespace DailyTasks.Tests;

internal sealed class FakeCurrentUser : IDailyTaskCurrentUser
{
    public int Id { get; init; } = 1;
    public string Name { get; init; } = "Tester";
    public string Role { get; init; } = DailyTaskRoles.User;
    public string Area { get; init; } = DailyTaskAreas.Marketing;

    public bool IsManager => string.Equals(Role, DailyTaskRoles.Manager, StringComparison.OrdinalIgnoreCase);
}

internal static class TestData
{
    public static DailyTaskUser User(
        int id,
        string? role = null,
        string? area = null,
        bool active = true) => new()
    {
        Id = id,
        Username = $"user{id}",
        FullName = $"Usuario {id}",
        Area = area ?? DailyTaskAreas.Marketing,
        Role = role ?? DailyTaskRoles.User,
        PasswordHash = "hash",
        IsActive = active
    };

    public static DailyTask Task(
        int id = 1,
        int assignedToId = 1,
        int createdById = 1,
        DailyTaskStatus status = DailyTaskStatus.Pending,
        DailyTaskPriority priority = DailyTaskPriority.Medium,
        string title = "Pendiente",
        string? description = "Descripción",
        DateOnly? dueDate = null,
        DateOnly? createdOn = null,
        DateTime? completedAt = null,
        DateOnly? completedOn = null,
        DateTime? deletedAt = null)
    {
        var today = DailyTaskTime.Today;
        var assigned = User(assignedToId);
        var creator = createdById == assignedToId ? assigned : User(createdById);

        return new DailyTask
        {
            Id = id,
            Title = title,
            Description = description,
            Status = status,
            Priority = priority,
            Area = assigned.Area,
            CreatedById = createdById,
            CreatedBy = creator,
            AssignedToId = assignedToId,
            AssignedTo = assigned,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            CreatedOn = createdOn ?? today.AddDays(-1),
            DueDate = dueDate ?? today,
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            CompletedAt = completedAt,
            CompletedOn = completedOn,
            DeletedAt = deletedAt
        };
    }
}
