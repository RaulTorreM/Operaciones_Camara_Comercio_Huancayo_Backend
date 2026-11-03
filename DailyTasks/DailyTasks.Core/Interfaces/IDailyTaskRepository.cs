namespace DailyTasks.Core.Interfaces;

using DailyTasks.Core.Entities;

public interface IDailyTaskRepository
{
    Task<DailyTask?> GetByIdAsync(int id, bool includeDeleted = false);

    Task<List<DailyTask>> ListAsync(
        Models.DailyTaskFilter filter,
        bool includeDeleted,
        string? sortBy,
        bool descending,
        int skip,
        int take);

    Task<int> CountAsync(Models.DailyTaskFilter filter, bool includeDeleted);

    Task<List<DailyTask>> ListAllAsync(Models.DailyTaskFilter filter, bool includeDeleted = false);

    Task<List<DailyTask>> ListChangedSinceAsync(DateTime since, int limit = 500);

    Task<List<int>> ListDeletedIdsSinceAsync(DateTime since, int limit = 500);

    Task AddAsync(DailyTask task);
}
