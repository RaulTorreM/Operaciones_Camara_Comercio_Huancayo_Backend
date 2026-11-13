using DailyTasks.Core.Entities;

namespace DailyTasks.Core.Interfaces;

public interface IDailyTaskSettingsRepository
{
    Task<DailyTaskSettings?> GetAsync();
    Task SaveAsync(DailyTaskSettings settings);
}
