using DailyTasks.Core.Entities;
using DailyTasks.Core.Interfaces;
using DailyTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyTasks.Infrastructure.Repositories;

public class DailyTaskNotificationRepository(DailyTasksDbContext db) : IDailyTaskNotificationRepository
{
    public async Task<List<DailyTaskNotification>> ListForUserAsync(int userId, int take = 20) =>
        await db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderBy(n => n.ReadAt != null)
            .ThenByDescending(n => n.CreatedAt)
            .Take(take)
            .ToListAsync();

    public Task<DailyTaskNotification?> GetForUserAsync(int id, int userId) =>
        db.Notifications.FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

    public async Task AddAsync(DailyTaskNotification notification)
    {
        db.Notifications.Add(notification);
        await db.SaveChangesAsync();
    }

    public async Task MarkReadAsync(DailyTaskNotification notification)
    {
        await db.SaveChangesAsync();
    }

    public async Task MarkAllReadAsync(int userId)
    {
        var now = DateTime.UtcNow;
        await db.Notifications
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.ReadAt, now));
    }
}
