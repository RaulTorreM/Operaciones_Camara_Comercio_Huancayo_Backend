using DailyTasks.Core.Entities;

namespace DailyTasks.Core.Interfaces;

public interface IDailyTaskUserRepository
{
    Task<DailyTaskUser?> GetByUsernameAsync(string username);
    Task<DailyTaskUser?> GetByIdAsync(int id);
    Task<List<DailyTaskUser>> ListActiveAsync(string? area = null);
    Task<bool> AnyAsync();
    Task AddAsync(DailyTaskUser user);
}
