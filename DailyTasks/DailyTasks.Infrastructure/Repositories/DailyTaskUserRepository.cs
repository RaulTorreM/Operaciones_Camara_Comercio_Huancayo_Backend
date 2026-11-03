using DailyTasks.Core.Entities;
using DailyTasks.Core.Interfaces;
using DailyTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyTasks.Infrastructure.Repositories;

public class DailyTaskUserRepository(DailyTasksDbContext db) : IDailyTaskUserRepository
{
    public Task<DailyTaskUser?> GetByUsernameAsync(string username) =>
        db.Users.FirstOrDefaultAsync(u => u.Username == username);

    public Task<DailyTaskUser?> GetByIdAsync(int id) =>
        db.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<List<DailyTaskUser>> ListActiveAsync(string? area = null) =>
        await db.Users
            .AsNoTracking()
            .Where(u => u.IsActive && (area == null || u.Area == area))
            .OrderBy(u => u.FullName)
            .ToListAsync();

    public Task<bool> AnyAsync() => db.Users.AnyAsync();

    public async Task AddAsync(DailyTaskUser user) => await db.Users.AddAsync(user);
}
