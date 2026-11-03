using DailyTasks.Core.Entities;

namespace DailyTasks.Core.Interfaces;

public interface IDailyTaskHistoryRepository
{
    Task<List<DailyTaskHistory>> ListByTaskAsync(int taskId);

    Task<List<DailyTaskHistory>> ListAsync(
        Models.DailyTaskAuditFilter filter,
        int skip,
        int take);

    Task<int> CountAsync(Models.DailyTaskAuditFilter filter);

    Task AddAsync(DailyTaskHistory history);
}
