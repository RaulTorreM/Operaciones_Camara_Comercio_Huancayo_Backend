using DailyTasks.Application.Common;
using DailyTasks.Application.DTOs.Tasks;
using DailyTasks.Application.DTOs.Users;
using DailyTasks.Application.Interfaces;
using DailyTasks.Application.Mappings;
using DailyTasks.Core.Entities;
using DailyTasks.Core.Enums;
using DailyTasks.Core.Exceptions;
using DailyTasks.Core.Interfaces;

namespace DailyTasks.Application.Services;

public class DailyTaskService(
    IDailyTaskRepository tasks,
    IDailyTaskUserRepository users,
    IDailyTaskHistoryRepository history,
    IDailyTaskUnitOfWork uow,
    IDailyTaskCurrentUser current,
    IDailyTaskNotifier notifier) : IDailyTaskService
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 2000;

    public async Task<PagedResult<DailyTaskDto>> ListAsync(DailyTaskQuery query)
    {
        EnsureAuthenticated();

        if (query.IncludeDeleted && !current.IsManager)
            throw new ForbiddenException("Solo Gerencia General puede ver pendientes eliminados.");

        var filter = query.ToFilter();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize <= 0 ? 20 : query.PageSize, 1, 100);
        var descending = string.Equals(query.SortDir, "desc", StringComparison.OrdinalIgnoreCase);

        var total = await tasks.CountAsync(filter, query.IncludeDeleted);
        var items = await tasks.ListAsync(
            filter, query.IncludeDeleted, query.SortBy, descending, (page - 1) * pageSize, pageSize);

        return new PagedResult<DailyTaskDto>(
            items.Select(t => t.ToDto()).ToList(),
            total,
            page,
            pageSize,
            total == 0 ? 0 : (int)Math.Ceiling(total / (double)pageSize));
    }

    public async Task<DailyTaskDto> GetAsync(int id)
    {
        EnsureAuthenticated();

        var task = await tasks.GetByIdAsync(id, includeDeleted: current.IsManager)
            ?? throw new NotFoundException("No se encontró el pendiente.");

        return task.ToDto();
    }

    public async Task<DailyTaskDto> CreateAsync(CreateDailyTaskRequest request)
    {
        EnsureAuthenticated();

        var title = DailyTaskValidation.Required(
            request.Title, MaxTitleLength, "El título es obligatorio y no debe superar 200 caracteres.");
        var description = DailyTaskValidation.Optional(
            request.Description, MaxDescriptionLength, "La descripción no debe superar 2000 caracteres.");
        DailyTaskValidation.CheckEnum(request.Priority, "La prioridad indicada no es válida.");

        var assignedToId = ResolveAssignee(request.AssignedToId);

        var creator = await users.GetByIdAsync(current.Id)
            ?? throw new UnauthorizedException("La cuenta ya no está activa.");
        if (!creator.IsActive)
            throw new UnauthorizedException("La cuenta ya no está activa.");

        var assignee = assignedToId == creator.Id ? creator : await users.GetByIdAsync(assignedToId);
        if (assignee is null || !assignee.IsActive)
            throw new BusinessRuleException("El responsable indicado no existe o está inactivo.");

        var now = DailyTaskTime.UtcNow;
        var today = DailyTaskTime.Today;

        var task = new DailyTask
        {
            Title = title,
            Description = description,
            Status = DailyTaskStatus.Pending,
            Priority = request.Priority ?? DailyTaskPriority.Medium,
            Area = assignee.Area,
            CreatedById = creator.Id,
            CreatedBy = creator,
            AssignedToId = assignee.Id,
            AssignedTo = assignee,
            CreatedAt = now,
            CreatedOn = today,
            DueDate = request.DueDate ?? today,
            UpdatedAt = now
        };

        task.History.Add(NewHistory(
            "CREATED", null, null, title, now, Guid.NewGuid()));

        await tasks.AddAsync(task);
        await uow.SaveChangesAsync();

        var dto = task.ToDto();
        await notifier.NotifyAsync("taskCreated", dto);
        return dto;
    }

    public async Task<DailyTaskDto> UpdateAsync(int id, UpdateDailyTaskRequest request)
    {
        EnsureAuthenticated();

        var task = await GetTaskForEditAsync(id);
        var title = DailyTaskValidation.Required(
            request.Title, MaxTitleLength, "El título es obligatorio y no debe superar 200 caracteres.");
        var description = DailyTaskValidation.Optional(
            request.Description, MaxDescriptionLength, "La descripción no debe superar 2000 caracteres.");
        DailyTaskValidation.CheckEnum(request.Priority, "La prioridad indicada no es válida.");

        var priority = request.Priority ?? task.Priority;
        var dueDate = request.DueDate ?? task.DueDate;
        var now = DailyTaskTime.UtcNow;
        var group = Guid.NewGuid();
        var changes = new List<DailyTaskHistory>();

        if (!string.Equals(task.Title, title, StringComparison.Ordinal))
            changes.Add(NewHistory("UPDATED", "Title", task.Title, title, now, group));

        if (!string.Equals(task.Description, description, StringComparison.Ordinal))
            changes.Add(NewHistory("UPDATED", "Description", task.Description, description, now, group));

        if (task.Priority != priority)
            changes.Add(NewHistory("UPDATED", "Priority", task.Priority.ToString(), priority.ToString(), now, group));

        if (task.DueDate != dueDate)
            changes.Add(NewHistory("UPDATED", "DueDate", task.DueDate.ToString("yyyy-MM-dd"), dueDate.ToString("yyyy-MM-dd"), now, group));

        if (changes.Count == 0) return task.ToDto();

        task.Title = title;
        task.Description = description;
        task.Priority = priority;
        task.DueDate = dueDate;
        task.UpdatedAt = now;
        foreach (var change in changes) task.History.Add(change);

        await uow.SaveChangesAsync();

        var dto = task.ToDto();
        await notifier.NotifyAsync("taskUpdated", dto);
        return dto;
    }

    public async Task<DailyTaskDto> ChangeStatusAsync(int id, ChangeDailyTaskStatusRequest request)
    {
        EnsureAuthenticated();

        if (!request.Status.HasValue)
            throw new BusinessRuleException("Indica el estado al que quieres mover el pendiente.");
        DailyTaskValidation.CheckEnum(request.Status, "El estado indicado no es válido.");

        var task = await GetTaskForEditAsync(id);
        if (task.Status == request.Status.Value) return task.ToDto();

        var oldStatus = task.Status;
        var now = DailyTaskTime.UtcNow;

        task.Status = request.Status.Value;
        task.UpdatedAt = now;

        if (task.Status == DailyTaskStatus.Done)
        {
            task.CompletedAt = now;
            task.CompletedOn = DailyTaskTime.Today;
        }
        else
        {
            task.CompletedAt = null;
            task.CompletedOn = null;
        }

        task.History.Add(NewHistory(
            "STATUS_CHANGED", "Status", oldStatus.ToString(), task.Status.ToString(), now, Guid.NewGuid()));

        await uow.SaveChangesAsync();

        var dto = task.ToDto();
        await notifier.NotifyAsync("taskStatusChanged", dto);
        return dto;
    }

    public async Task<DailyTaskDto> AssignAsync(int id, AssignDailyTaskRequest request)
    {
        EnsureAuthenticated();

        if (!current.IsManager)
            throw new ForbiddenException("Solo Gerencia General puede reasignar pendientes.");

        if (!request.AssignedToId.HasValue || request.AssignedToId.Value <= 0)
            throw new BusinessRuleException("Indica el responsable del pendiente.");

        var task = await tasks.GetByIdAsync(id)
            ?? throw new NotFoundException("No se encontró el pendiente.");

        var assignee = await users.GetByIdAsync(request.AssignedToId.Value);
        if (assignee is null || !assignee.IsActive)
            throw new BusinessRuleException("El responsable indicado no existe o está inactivo.");

        if (task.AssignedToId == assignee.Id) return task.ToDto();

        var oldName = task.AssignedTo?.FullName ?? task.AssignedToId.ToString();
        var now = DailyTaskTime.UtcNow;

        task.AssignedToId = assignee.Id;
        task.AssignedTo = assignee;
        task.Area = assignee.Area;
        task.UpdatedAt = now;
        task.History.Add(NewHistory(
            "ASSIGNED", "AssignedTo", oldName, assignee.FullName, now, Guid.NewGuid()));

        await uow.SaveChangesAsync();

        var dto = task.ToDto();
        await notifier.NotifyAsync("taskAssigned", dto);
        return dto;
    }

    public async Task DeleteAsync(int id)
    {
        EnsureAuthenticated();

        if (!current.IsManager)
            throw new ForbiddenException("Solo Gerencia General puede eliminar pendientes.");

        var task = await tasks.GetByIdAsync(id)
            ?? throw new NotFoundException("No se encontró el pendiente.");

        if (task.DeletedAt is not null) return;

        var now = DailyTaskTime.UtcNow;
        task.DeletedAt = now;
        task.DeletedById = current.Id;
        task.UpdatedAt = now;
        task.History.Add(NewHistory("DELETED", null, null, "Pendiente eliminado", now, Guid.NewGuid()));

        await uow.SaveChangesAsync();
        await notifier.NotifyAsync("taskDeleted", new { taskId = task.Id });
    }

    public async Task<DailyTaskDto> RestoreAsync(int id)
    {
        EnsureAuthenticated();

        if (!current.IsManager)
            throw new ForbiddenException("Solo Gerencia General puede restaurar pendientes.");

        var task = await tasks.GetByIdAsync(id, includeDeleted: true)
            ?? throw new NotFoundException("No se encontró el pendiente.");

        if (task.DeletedAt is null) return task.ToDto();

        var now = DailyTaskTime.UtcNow;
        task.DeletedAt = null;
        task.DeletedById = null;
        task.DeletedBy = null;
        task.UpdatedAt = now;
        task.History.Add(NewHistory("RESTORED", null, "Pendiente eliminado", null, now, Guid.NewGuid()));

        await uow.SaveChangesAsync();

        var dto = task.ToDto();
        await notifier.NotifyAsync("taskRestored", dto);
        return dto;
    }

    public async Task<List<DailyTaskHistoryDto>> HistoryAsync(int id)
    {
        EnsureAuthenticated();

        var task = await tasks.GetByIdAsync(id, includeDeleted: true)
            ?? throw new NotFoundException("No se encontró el pendiente.");

        if (task.DeletedAt is not null && !current.IsManager)
            throw new NotFoundException("No se encontró el pendiente.");

        if (!current.IsManager && task.AssignedToId != current.Id)
            throw new ForbiddenException("Solo puedes ver el historial de tus propios pendientes.");

        var entries = await history.ListByTaskAsync(id);
        return entries.Select(h => h.ToDto()).ToList();
    }

    public async Task<DailyTaskBoardDto> BoardAsync(DailyTaskQuery query)
    {
        EnsureAuthenticated();

        if (query.IncludeDeleted && !current.IsManager)
            throw new ForbiddenException("Solo Gerencia General puede ver pendientes eliminados.");

        var items = await tasks.ListAllAsync(query.ToFilter());
        var activeCards = items
            .Where(t => t.Status != DailyTaskStatus.Done)
            .Select(t => t.ToCard())
            .ToList();

        // La columna Completado representa el cierre del día del tablero,
        // no el historial de cierres de días anteriores.
        var boardDate = query.DueDate ?? DailyTaskTime.Today;
        var doneCards = new List<DailyTaskCardDto>();

        if (query.Overdue != true && (query.Status is null || query.Status == DailyTaskStatus.Done))
        {
            var doneFilter = query.ToFilter();
            doneFilter.Status = DailyTaskStatus.Done;
            doneFilter.CompletedOn = boardDate;
            doneFilter.DueFrom = null;
            doneFilter.DueTo = null;
            doneFilter.DueDate = null;

            var doneItems = await tasks.ListAllAsync(doneFilter);
            doneCards = doneItems.Select(t => t.ToCard()).ToList();
        }

        return new DailyTaskBoardDto(
            activeCards.Where(c => c.Status == DailyTaskStatus.Pending).ToList(),
            activeCards.Where(c => c.Status == DailyTaskStatus.InProgress).ToList(),
            doneCards);
    }

    public async Task<DailyTaskCalendarDto> CalendarAsync(DateOnly? from, DateOnly? to, DailyTaskQuery query)
    {
        EnsureAuthenticated();

        if (query.IncludeDeleted && !current.IsManager)
            throw new ForbiddenException("Solo Gerencia General puede ver pendientes eliminados.");

        var today = DailyTaskTime.Today;
        var start = from ?? (to.HasValue
            ? new DateOnly(to.Value.Year, to.Value.Month, 1)
            : new DateOnly(today.Year, today.Month, 1));
        var end = to ?? start.AddMonths(1).AddDays(-1);

        if (end < start)
            throw new BusinessRuleException("La fecha final no puede ser anterior a la inicial.");
        if (end.DayNumber - start.DayNumber > 366)
            throw new BusinessRuleException("El rango del calendario no puede superar un año.");

        var filter = query.ToFilter();
        filter.DueFrom = start;
        filter.DueTo = end;

        var items = await tasks.ListAllAsync(filter);
        var days = items
            .GroupBy(t => t.DueDate)
            .OrderBy(g => g.Key)
            .Select(g => new DailyTaskCalendarDayDto(
                g.Key,
                g.Select(t => t.ToCard()).ToList()))
            .ToList();

        return new DailyTaskCalendarDto(start, end, days);
    }

    public async Task<DailyTaskChangesDto> ChangesAsync(DateTime? sinceUtc)
    {
        EnsureAuthenticated();

        var since = sinceUtc ?? DailyTaskTime.UtcNow.AddDays(-1);
        var changed = await tasks.ListChangedSinceAsync(since);
        var deletedIds = await tasks.ListDeletedIdsSinceAsync(since);

        // Los usuarios regulares reciben los ids para retirar tarjetas eliminadas,
        // pero no el detalle de pendientes que solo Gerencia puede supervisar.
        var visible = current.IsManager
            ? changed
            : changed.Where(t => t.DeletedAt == null).ToList();

        return new DailyTaskChangesDto(
            visible.Select(t => t.ToDto()).ToList(),
            deletedIds);
    }

    public async Task<List<DailyTaskUserDto>> ListUsersAsync(string? area)
    {
        EnsureAuthenticated();

        var normalizedArea = string.IsNullOrWhiteSpace(area) ? null : area.Trim().ToUpperInvariant();
        if (normalizedArea is not null && !DailyTaskAreas.IsValid(normalizedArea))
            throw new BusinessRuleException("El área indicada no es válida.");

        var list = await users.ListActiveAsync(normalizedArea);
        return list.Select(u => u.ToDto()).ToList();
    }

    public Task<List<DailyTaskAreaDto>> ListAreasAsync()
    {
        EnsureAuthenticated();

        return Task.FromResult(DailyTaskAreas.All
            .Select(a => new DailyTaskAreaDto(a.Code, a.Name))
            .ToList());
    }

    private int ResolveAssignee(int? requested)
    {
        if (current.IsManager)
        {
            if (!requested.HasValue) return current.Id;
            if (requested.Value <= 0)
                throw new BusinessRuleException("El responsable indicado no es válido.");

            return requested.Value;
        }

        if (requested.HasValue && requested.Value != current.Id)
            throw new ForbiddenException("No puedes asignar pendientes a otros integrantes.");

        return current.Id;
    }

    private async Task<DailyTask> GetTaskForEditAsync(int id)
    {
        var task = await tasks.GetByIdAsync(id)
            ?? throw new NotFoundException("No se encontró el pendiente.");

        if (!current.IsManager && task.AssignedToId != current.Id)
            throw new ForbiddenException("Solo puedes modificar tus propios pendientes.");

        return task;
    }

    private DailyTaskHistory NewHistory(
        string action,
        string? field,
        string? oldValue,
        string? newValue,
        DateTime now,
        Guid group) => new()
        {
            ChangedById = current.Id,
            ChangedAt = now,
            ChangeGroupId = group,
            Action = action,
            Field = field,
            OldValue = oldValue,
            NewValue = newValue
        };

    private void EnsureAuthenticated()
    {
        if (current.Id <= 0) throw new UnauthorizedException("Sesión no válida.");
    }
}
