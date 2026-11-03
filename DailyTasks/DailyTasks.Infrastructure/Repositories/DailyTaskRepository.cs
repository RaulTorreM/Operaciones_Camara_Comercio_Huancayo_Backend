using DailyTasks.Core.Entities;
using DailyTasks.Core.Interfaces;
using DailyTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyTasks.Infrastructure.Repositories;

public class DailyTaskRepository(DailyTasksDbContext db) : IDailyTaskRepository
{
    public Task<DailyTask?> GetByIdAsync(int id, bool includeDeleted = false) =>
        Query(includeDeleted).FirstOrDefaultAsync(t => t.Id == id);

    public async Task<List<DailyTask>> ListAsync(
        Core.Models.DailyTaskFilter filter,
        bool includeDeleted,
        string? sortBy,
        bool descending,
        int skip,
        int take)
    {
        var query = ApplySort(ApplyFilter(Query(includeDeleted).AsNoTracking(), filter), sortBy, descending);
        return await query.Skip(skip).Take(take).ToListAsync();
    }

    public Task<int> CountAsync(Core.Models.DailyTaskFilter filter, bool includeDeleted) =>
        ApplyFilter(BaseQuery(includeDeleted), filter).CountAsync();

    public async Task<List<DailyTask>> ListAllAsync(Core.Models.DailyTaskFilter filter, bool includeDeleted = false)
    {
        var query = ApplyFilter(Query(includeDeleted).AsNoTracking(), filter);
        return await query
            .OrderBy(t => t.DueDate)
            .ThenByDescending(t => t.Priority == Core.Enums.DailyTaskPriority.High ? 2
                : t.Priority == Core.Enums.DailyTaskPriority.Medium ? 1 : 0)
            .ToListAsync();
    }

    public async Task<List<DailyTask>> ListChangedSinceAsync(DateTime since, int limit = 500) =>
        await Query(includeDeleted: true)
            .AsNoTracking()
            .Where(t => t.UpdatedAt > since)
            .OrderBy(t => t.UpdatedAt)
            .Take(limit)
            .ToListAsync();

    public async Task<List<int>> ListDeletedIdsSinceAsync(DateTime since, int limit = 500) =>
        await db.Tasks
            .AsNoTracking()
            .Where(t => t.DeletedAt != null && t.DeletedAt > since)
            .OrderBy(t => t.DeletedAt)
            .Take(limit)
            .Select(t => t.Id)
            .ToListAsync();

    public async Task AddAsync(DailyTask task) => await db.Tasks.AddAsync(task);

    private IQueryable<DailyTask> BaseQuery(bool includeDeleted) =>
        db.Tasks.Where(t => includeDeleted || t.DeletedAt == null);

    private IQueryable<DailyTask> Query(bool includeDeleted) =>
        BaseQuery(includeDeleted)
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.DeletedBy);

    private static IQueryable<DailyTask> ApplyFilter(IQueryable<DailyTask> q, Core.Models.DailyTaskFilter f)
    {
        if (f.Status.HasValue) q = q.Where(t => t.Status == f.Status.Value);
        if (f.Priority.HasValue) q = q.Where(t => t.Priority == f.Priority.Value);
        if (f.AssignedToId.HasValue) q = q.Where(t => t.AssignedToId == f.AssignedToId.Value);
        if (!string.IsNullOrWhiteSpace(f.Area)) q = q.Where(t => t.Area == f.Area);
        if (f.CreatedFrom.HasValue) q = q.Where(t => t.CreatedOn >= f.CreatedFrom.Value);
        if (f.CreatedTo.HasValue) q = q.Where(t => t.CreatedOn <= f.CreatedTo.Value);
        if (f.DueFrom.HasValue) q = q.Where(t => t.DueDate >= f.DueFrom.Value);
        if (f.DueTo.HasValue) q = q.Where(t => t.DueDate <= f.DueTo.Value);
        if (f.DueDate.HasValue) q = q.Where(t => t.DueDate == f.DueDate.Value);
        if (f.CompletedOn.HasValue) q = q.Where(t => t.CompletedOn == f.CompletedOn.Value);

        if (f.Overdue == true)
            q = q.Where(t => t.Status != Core.Enums.DailyTaskStatus.Done && t.DueDate < f.Today);
        else if (f.Overdue == false)
            q = q.Where(t => t.Status == Core.Enums.DailyTaskStatus.Done || t.DueDate >= f.Today);

        if (!string.IsNullOrWhiteSpace(f.Query))
        {
            var term = f.Query.Trim();
            q = q.Where(t => EF.Functions.ILike(t.Title, $"%{term}%")
                || (t.Description != null && EF.Functions.ILike(t.Description, $"%{term}%")));
        }

        if (f.UpdatedSince.HasValue) q = q.Where(t => t.UpdatedAt > f.UpdatedSince.Value);

        return q;
    }

    private static IQueryable<DailyTask> ApplySort(IQueryable<DailyTask> q, string? sortBy, bool descending)
    {
        var key = (sortBy ?? "").Trim().ToLowerInvariant();

        return key switch
        {
            "duedate" => descending
                ? q.OrderByDescending(t => t.DueDate).ThenByDescending(t => t.Priority == Core.Enums.DailyTaskPriority.High ? 2
                    : t.Priority == Core.Enums.DailyTaskPriority.Medium ? 1 : 0)
                : q.OrderBy(t => t.DueDate).ThenByDescending(t => t.Priority == Core.Enums.DailyTaskPriority.High ? 2
                    : t.Priority == Core.Enums.DailyTaskPriority.Medium ? 1 : 0),
            "priority" => descending
                ? q.OrderByDescending(t => t.Priority == Core.Enums.DailyTaskPriority.High ? 2
                    : t.Priority == Core.Enums.DailyTaskPriority.Medium ? 1 : 0)
                : q.OrderBy(t => t.Priority == Core.Enums.DailyTaskPriority.High ? 2
                    : t.Priority == Core.Enums.DailyTaskPriority.Medium ? 1 : 0),
            "status" => descending
                ? q.OrderByDescending(t => t.Status == Core.Enums.DailyTaskStatus.Done ? 2
                    : t.Status == Core.Enums.DailyTaskStatus.InProgress ? 1 : 0)
                : q.OrderBy(t => t.Status == Core.Enums.DailyTaskStatus.Done ? 2
                    : t.Status == Core.Enums.DailyTaskStatus.InProgress ? 1 : 0),
            "title" => descending ? q.OrderByDescending(t => t.Title) : q.OrderBy(t => t.Title),
            _ => descending ? q.OrderByDescending(t => t.CreatedAt) : q.OrderBy(t => t.CreatedAt)
        };
    }
}
