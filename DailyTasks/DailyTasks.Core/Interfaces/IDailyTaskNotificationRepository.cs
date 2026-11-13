using DailyTasks.Core.Entities;

namespace DailyTasks.Core.Interfaces;

public interface IDailyTaskNotificationRepository
{
    Task<List<DailyTaskNotification>> ListForUserAsync(int userId, int take = 20);
    Task<DailyTaskNotification?> GetForUserAsync(int id, int userId);
    Task AddAsync(DailyTaskNotification notification);
    Task MarkReadAsync(DailyTaskNotification notification);
    Task MarkAllReadAsync(int userId);
}
