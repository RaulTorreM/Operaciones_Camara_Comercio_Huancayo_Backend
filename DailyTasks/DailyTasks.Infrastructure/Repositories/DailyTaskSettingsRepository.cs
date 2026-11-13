using DailyTasks.Core.Entities;
using DailyTasks.Core.Interfaces;
using DailyTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyTasks.Infrastructure.Repositories;

public class DailyTaskSettingsRepository(DailyTasksDbContext db) : IDailyTaskSettingsRepository
{
    public Task<DailyTaskSettings?> GetAsync() =>
        db.Settings.FirstOrDefaultAsync(s => s.Id == 1);

    public async Task SaveAsync(DailyTaskSettings settings)
    {
        if (settings.Id == 0) settings.Id = 1;

        var existing = await db.Settings.FirstOrDefaultAsync(s => s.Id == settings.Id);
        if (existing is null)
        {
            db.Settings.Add(settings);
        }
        else
        {
            existing.CreationStartTime = settings.CreationStartTime;
            existing.CreationEndTime = settings.CreationEndTime;
            existing.AllowWeekendCreation = settings.AllowWeekendCreation;
            existing.UpdatedAt = settings.UpdatedAt;
        }

        await db.SaveChangesAsync();
    }
}
