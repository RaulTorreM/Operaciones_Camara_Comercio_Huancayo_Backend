using DailyTasks.Core.Entities;
using DailyTasks.Core.Interfaces;
using DailyTasks.Core.Models;
using DailyTasks.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DailyTasks.Infrastructure.Repositories;

public class DailyTaskHistoryRepository(DailyTasksDbContext db) : IDailyTaskHistoryRepository
{
    public async Task<List<DailyTaskHistory>> ListByTaskAsync(int taskId) =>
        await db.TaskHistory
            .AsNoTracking()
            .Include(h => h.ChangedBy)
            .Where(h => h.TaskId == taskId)
            .OrderByDescending(h => h.ChangedAt)
            .ToListAsync();

    public async Task<List<DailyTaskHistory>> ListAsync(
        DailyTaskAuditFilter filter,
        int skip,
        int take) =>
        await ApplyFilter(Query(), filter)
            .OrderByDescending(h => h.ChangedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

    public Task<int> CountAsync(DailyTaskAuditFilter filter) =>
        ApplyFilter(Query(), filter).CountAsync();

    public async Task AddAsync(DailyTaskHistory history) => await db.TaskHistory.AddAsync(history);

    private IQueryable<DailyTaskHistory> Query() =>
        db.TaskHistory
            .AsNoTracking()
            .Include(h => h.ChangedBy)
            .Include(h => h.Task)
                .ThenInclude(t => t!.AssignedTo);

    private static IQueryable<DailyTaskHistory> ApplyFilter(
        IQueryable<DailyTaskHistory> query,
        DailyTaskAuditFilter filter)
    {
        if (filter.From.HasValue)
        {
            var fromUtc = PeruDateStartUtc(filter.From.Value);
            query = query.Where(h => h.ChangedAt >= fromUtc);
        }

        if (filter.To.HasValue)
        {
            var toExclusiveUtc = PeruDateStartUtc(filter.To.Value.AddDays(1));
            query = query.Where(h => h.ChangedAt < toExclusiveUtc);
        }

        if (filter.UserId.HasValue)
            query = query.Where(h => h.ChangedById == filter.UserId.Value);

        if (filter.TaskId.HasValue)
            query = query.Where(h => h.TaskId == filter.TaskId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Action))
        {
            var action = filter.Action.Trim().ToUpperInvariant();
            query = query.Where(h => h.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(filter.Field))
        {
            var field = filter.Field.Trim();
            query = query.Where(h => h.Field == field);
        }

        if (filter.SensitiveOnly)
        {
            query = query.Where(h =>
                h.Field == "Title" || h.Field == "DueDate" || h.Field == "AssignedTo");
        }

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var term = filter.Query.Trim();
            query = query.Where(h =>
                EF.Functions.ILike(h.Task!.Title, $"%{term}%")
                || (h.OldValue != null && EF.Functions.ILike(h.OldValue, $"%{term}%"))
                || (h.NewValue != null && EF.Functions.ILike(h.NewValue, $"%{term}%")));
        }

        return query;
    }

    /// <summary>Medianoche de una fecha Perú (UTC-5) expresada en UTC.</summary>
    private static DateTime PeruDateStartUtc(DateOnly date) =>
        DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue).AddHours(5), DateTimeKind.Utc);
}
