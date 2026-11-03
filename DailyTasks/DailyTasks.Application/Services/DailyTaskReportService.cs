using DailyTasks.Application.Common;
using DailyTasks.Application.DTOs.Reports;
using DailyTasks.Application.DTOs.Tasks;
using DailyTasks.Application.Interfaces;
using DailyTasks.Application.Mappings;
using DailyTasks.Core.Entities;
using DailyTasks.Core.Enums;
using DailyTasks.Core.Exceptions;
using DailyTasks.Core.Interfaces;
using DailyTasks.Core.Models;

namespace DailyTasks.Application.Services;

public class DailyTaskReportService(
    IDailyTaskRepository tasks,
    IDailyTaskUserRepository users,
    IDailyTaskHistoryRepository history,
    IDailyTaskCurrentUser current) : IDailyTaskReportService
{
    public async Task<DailyTaskSummaryDto> GetSummaryAsync()
    {
        EnsureAuthenticated();

        var today = DailyTaskTime.Today;
        var all = await tasks.ListAllAsync(new Core.Models.DailyTaskFilter { Today = today });

        var total = all.Count;
        var pending = all.Count(t => t.Status == DailyTaskStatus.Pending);
        var inProgress = all.Count(t => t.Status == DailyTaskStatus.InProgress);
        var done = all.Count(t => t.Status == DailyTaskStatus.Done);
        var overdue = all.Count(t => t.Status != DailyTaskStatus.Done && t.DueDate < today);
        var dueToday = all.Count(t => t.Status != DailyTaskStatus.Done && t.DueDate == today);

        return new DailyTaskSummaryDto(
            total,
            pending,
            inProgress,
            done,
            overdue,
            dueToday,
            Rate(done, total),
            Enum.GetValues<DailyTaskStatus>()
                .Select(s => new CountByStatusDto(s, all.Count(t => t.Status == s)))
                .ToList(),
            Enum.GetValues<DailyTaskPriority>()
                .Select(p => new CountByPriorityDto(p, all.Count(t => t.Priority == p)))
                .ToList(),
            DailyTaskAreas.All
                .Select(a => new CountByAreaDto(a.Code, a.Name, all.Count(t => t.Area == a.Code)))
                .ToList());
    }

    public Task<DailyTaskReportDto> GetReportAsync(DailyTaskReportQuery query)
    {
        EnsureAuthenticated();

        if (!current.IsManager)
            throw new ForbiddenException("Solo Gerencia General puede ver informes globales.");

        return BuildReportAsync(query, query.UserId);
    }

    public Task<DailyTaskReportDto> GetMyReportAsync(DailyTaskReportQuery query)
    {
        EnsureAuthenticated();
        return BuildReportAsync(query, current.Id);
    }

    private async Task<DailyTaskReportDto> BuildReportAsync(DailyTaskReportQuery query, int? forcedUserId)
    {
        var today = DailyTaskTime.Today;
        var from = query.From ?? today.AddDays(-29);
        var to = query.To ?? today;

        if (to < from)
            throw new BusinessRuleException("La fecha final no puede ser anterior a la inicial.");
        if (to.DayNumber - from.DayNumber > 366)
            throw new BusinessRuleException("El rango del informe no puede superar un año.");

        var granularity = (query.Granularity ?? "day").Trim().ToLowerInvariant();
        if (granularity is not ("day" or "week" or "month"))
            throw new BusinessRuleException("La granularidad debe ser day, week o month.");

        var normalizedArea = string.IsNullOrWhiteSpace(query.Area) ? null : query.Area.Trim().ToUpperInvariant();
        if (normalizedArea is not null && !DailyTaskAreas.IsValid(normalizedArea))
            throw new BusinessRuleException("El área indicada no es válida.");

        var filter = new Core.Models.DailyTaskFilter
        {
            AssignedToId = forcedUserId ?? query.UserId,
            Area = normalizedArea,
            Status = query.Status,
            Priority = query.Priority,
            Today = today
        };

        var all = await tasks.ListAllAsync(filter);
        var createdInPeriod = all.Where(t => t.CreatedOn >= from && t.CreatedOn <= to).ToList();
        var completedInPeriod = all
            .Where(t => t.CompletedOn.HasValue && t.CompletedOn.Value >= from && t.CompletedOn.Value <= to)
            .ToList();

        var total = createdInPeriod.Count;
        var pending = createdInPeriod.Count(t => t.Status == DailyTaskStatus.Pending);
        var inProgress = createdInPeriod.Count(t => t.Status == DailyTaskStatus.InProgress);
        var done = createdInPeriod.Count(t => t.Status == DailyTaskStatus.Done);
        var overdue = createdInPeriod.Count(t => t.Status != DailyTaskStatus.Done && t.DueDate < today);
        var dueToday = createdInPeriod.Count(t => t.Status != DailyTaskStatus.Done && t.DueDate == today);

        var metrics = new DailyTaskReportMetricsDto(
            total,
            pending,
            inProgress,
            done,
            overdue,
            dueToday,
            Rate(done, total),
            total,
            completedInPeriod.Count);

        var series = BuildSeries(from, to, granularity, createdInPeriod, completedInPeriod);

        var activeUsers = await users.ListActiveAsync(normalizedArea);

        // En el informe propio (y cuando Gerencia filtra por usuario) solo se
        // muestra la fila del usuario consultado, no el resto del equipo.
        if (forcedUserId.HasValue)
            activeUsers = activeUsers.Where(u => u.Id == forcedUserId.Value).ToList();
        else if (query.UserId.HasValue)
            activeUsers = activeUsers.Where(u => u.Id == query.UserId.Value).ToList();

        var byUser = activeUsers.Select(u =>
        {
            var mine = createdInPeriod.Where(t => t.AssignedToId == u.Id).ToList();
            var myDone = mine.Count(t => t.Status == DailyTaskStatus.Done);
            return new DailyTaskReportByUserDto(
                u.Id,
                u.FullName,
                u.Area,
                DailyTaskAreas.NameOf(u.Area),
                mine.Count,
                mine.Count(t => t.Status == DailyTaskStatus.Pending),
                mine.Count(t => t.Status == DailyTaskStatus.InProgress),
                myDone,
                mine.Count(t => t.Status != DailyTaskStatus.Done && t.DueDate < today),
                Rate(myDone, mine.Count),
                mine.Count,
                completedInPeriod.Count(t => t.AssignedToId == u.Id));
        }).ToList();

        var byArea = DailyTaskAreas.All.Select(a =>
        {
            var areaTasks = createdInPeriod.Where(t => t.Area == a.Code).ToList();
            var areaDone = areaTasks.Count(t => t.Status == DailyTaskStatus.Done);
            return new DailyTaskReportByAreaDto(
                a.Code,
                a.Name,
                areaTasks.Count,
                areaDone,
                areaTasks.Count(t => t.Status != DailyTaskStatus.Done && t.DueDate < today),
                Rate(areaDone, areaTasks.Count));
        }).ToList();

        return new DailyTaskReportDto(
            from,
            to,
            granularity,
            metrics,
            series,
            byUser,
            byArea,
            Enum.GetValues<DailyTaskStatus>()
                .Select(s => new CountByStatusDto(s, createdInPeriod.Count(t => t.Status == s)))
                .ToList(),
            Enum.GetValues<DailyTaskPriority>()
                .Select(p => new CountByPriorityDto(p, createdInPeriod.Count(t => t.Priority == p)))
                .ToList());
    }

    private static List<DailyTaskReportSeriesPointDto> BuildSeries(
        DateOnly from,
        DateOnly to,
        string granularity,
        List<DailyTask> createdInPeriod,
        List<DailyTask> completedInPeriod)
    {
        var cursor = BucketStart(from, granularity);
        var last = BucketStart(to, granularity);
        var result = new List<DailyTaskReportSeriesPointDto>();

        while (cursor <= last)
        {
            var end = granularity switch
            {
                "week" => cursor.AddDays(6),
                "month" => cursor.AddMonths(1).AddDays(-1),
                _ => cursor
            };

            result.Add(new DailyTaskReportSeriesPointDto(
                cursor,
                createdInPeriod.Count(t => t.CreatedOn >= cursor && t.CreatedOn <= end),
                completedInPeriod.Count(t => t.CompletedOn.HasValue
                    && t.CompletedOn.Value >= cursor
                    && t.CompletedOn.Value <= end)));

            cursor = granularity switch
            {
                "week" => cursor.AddDays(7),
                "month" => cursor.AddMonths(1),
                _ => cursor.AddDays(1)
            };
        }

        return result;
    }

    private static DateOnly BucketStart(DateOnly date, string granularity) => granularity switch
    {
        // Día de la semana en .NET: domingo = 0. Se normaliza a lunes = 0.
        "week" => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)),
        "month" => new DateOnly(date.Year, date.Month, 1),
        _ => date
    };

    public async Task<PagedResult<DailyTaskAuditEntryDto>> GetAuditAsync(DailyTaskAuditQuery query)
    {
        EnsureAuthenticated();

        if (!current.IsManager)
            throw new ForbiddenException("Solo Gerencia General puede ver la auditoría.");

        if (query.From.HasValue && query.To.HasValue && query.To < query.From)
            throw new BusinessRuleException("La fecha final no puede ser anterior a la inicial.");

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize <= 0 ? 50 : query.PageSize, 1, 100);

        var filter = new DailyTaskAuditFilter
        {
            From = query.From,
            To = query.To,
            UserId = query.UserId,
            TaskId = query.TaskId,
            Action = query.Action,
            Field = query.Field,
            Query = string.IsNullOrWhiteSpace(query.Q) ? null : query.Q.Trim(),
            SensitiveOnly = query.SensitiveOnly
        };

        var total = await history.CountAsync(filter);
        var entries = await history.ListAsync(filter, (page - 1) * pageSize, pageSize);

        return new PagedResult<DailyTaskAuditEntryDto>(
            entries.Select(h => h.ToAuditDto()).ToList(),
            total,
            page,
            pageSize,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize));
    }

    private static decimal Rate(int done, int total) =>
        total == 0 ? 0 : Math.Round(done * 100m / total, 2);

    private void EnsureAuthenticated()
    {
        if (current.Id <= 0) throw new UnauthorizedException("Sesión no válida.");
    }
}
