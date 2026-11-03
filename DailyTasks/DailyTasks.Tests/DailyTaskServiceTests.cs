using DailyTasks.Application.Common;
using DailyTasks.Application.DTOs.Tasks;
using DailyTasks.Application.Interfaces;
using DailyTasks.Application.Services;
using DailyTasks.Core.Entities;
using DailyTasks.Core.Enums;
using DailyTasks.Core.Exceptions;
using DailyTasks.Core.Interfaces;
using DailyTasks.Core.Models;
using Moq;

namespace DailyTasks.Tests;

public class DailyTaskServiceTests
{
    private readonly Mock<IDailyTaskRepository> _tasks = new();
    private readonly Mock<IDailyTaskUserRepository> _users = new();
    private readonly Mock<IDailyTaskHistoryRepository> _history = new();
    private readonly Mock<IDailyTaskUnitOfWork> _uow = new();
    private readonly Mock<IDailyTaskNotifier> _notifier = new();

    public DailyTaskServiceTests()
    {
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _notifier.Setup(n => n.NotifyAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _tasks.Setup(r => r.AddAsync(It.IsAny<DailyTask>())).Returns(Task.CompletedTask);
    }

    private DailyTaskService Build(FakeCurrentUser current) =>
        new(_tasks.Object, _users.Object, _history.Object, _uow.Object, current, _notifier.Object);

    [Fact]
    public async Task CreateAsync_RegularUser_ForcesSelfAndDefaults()
    {
        var current = new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User, Area = DailyTaskAreas.Marketing };
        var user = TestData.User(1, area: DailyTaskAreas.Marketing);
        _users.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);

        DailyTask? captured = null;
        _tasks.Setup(r => r.AddAsync(It.IsAny<DailyTask>()))
            .Callback<DailyTask>(t => { t.Id = 10; captured = t; })
            .Returns(Task.CompletedTask);

        var service = Build(current);

        var dto = await service.CreateAsync(new CreateDailyTaskRequest("  Tarea  ", null, null, null, null));

        Assert.NotNull(captured);
        Assert.Equal("Tarea", captured!.Title);
        Assert.Equal(1, captured.AssignedToId);
        Assert.Equal(DailyTaskStatus.Pending, captured.Status);
        Assert.Equal(DailyTaskPriority.Medium, captured.Priority);
        Assert.Equal(DailyTaskTime.Today, captured.DueDate);
        Assert.Single(captured.History);
        Assert.Equal("CREATED", captured.History[0].Action);
        Assert.Equal("DT-10", dto.Code);
        _notifier.Verify(n => n.NotifyAsync("taskCreated", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_RegularUser_CannotAssignOther()
    {
        var current = new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User };
        var service = Build(current);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.CreateAsync(new CreateDailyTaskRequest("Tarea", null, null, null, 2)));
    }

    [Fact]
    public async Task CreateAsync_Manager_CanAssignOther()
    {
        var current = new FakeCurrentUser { Id = 11, Role = DailyTaskRoles.Manager, Area = DailyTaskAreas.GerenciaGeneral };
        var manager = TestData.User(11, role: DailyTaskRoles.Manager, area: DailyTaskAreas.GerenciaGeneral);
        var other = TestData.User(2, area: DailyTaskAreas.Tesoreria);
        _users.Setup(r => r.GetByIdAsync(11)).ReturnsAsync(manager);
        _users.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(other);

        DailyTask? captured = null;
        _tasks.Setup(r => r.AddAsync(It.IsAny<DailyTask>()))
            .Callback<DailyTask>(t => captured = t)
            .Returns(Task.CompletedTask);

        var service = Build(current);

        await service.CreateAsync(new CreateDailyTaskRequest(
            "Tarea", "Detalle", DailyTaskPriority.High, DailyTaskTime.Today, 2));

        Assert.NotNull(captured);
        Assert.Equal(2, captured!.AssignedToId);
        Assert.Equal(DailyTaskAreas.Tesoreria, captured.Area);
        Assert.Equal(DailyTaskPriority.High, captured.Priority);
    }

    [Fact]
    public async Task UpdateAsync_RegularUser_OwnTask_AddsHistory()
    {
        var current = new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User };
        var task = TestData.Task(5, assignedToId: 1, title: "Viejo");
        _tasks.Setup(r => r.GetByIdAsync(5, false)).ReturnsAsync(task);

        var service = Build(current);

        await service.UpdateAsync(5, new UpdateDailyTaskRequest(
            "Nuevo", "Otra descripción", DailyTaskPriority.High, DailyTaskTime.Today.AddDays(1)));

        Assert.Equal("Nuevo", task.Title);
        Assert.Equal("Otra descripción", task.Description);
        Assert.Equal(DailyTaskPriority.High, task.Priority);
        Assert.Equal(4, task.History.Count);
        Assert.All(task.History, h => Assert.Equal("UPDATED", h.Action));
        Assert.Single(task.History.Select(h => h.ChangeGroupId).Distinct());
        _notifier.Verify(n => n.NotifyAsync("taskUpdated", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_RegularUser_OtherTask_Forbidden()
    {
        var current = new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User };
        var task = TestData.Task(5, assignedToId: 2);
        _tasks.Setup(r => r.GetByIdAsync(5, false)).ReturnsAsync(task);

        var service = Build(current);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.UpdateAsync(5, new UpdateDailyTaskRequest("X", null, null, null)));
    }

    [Fact]
    public async Task ChangeStatusAsync_ToDone_SetsCompleted()
    {
        var current = new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User };
        var task = TestData.Task(5, assignedToId: 1, status: DailyTaskStatus.Pending);
        _tasks.Setup(r => r.GetByIdAsync(5, false)).ReturnsAsync(task);

        var service = Build(current);

        await service.ChangeStatusAsync(5, new ChangeDailyTaskStatusRequest(DailyTaskStatus.Done));

        Assert.Equal(DailyTaskStatus.Done, task.Status);
        Assert.NotNull(task.CompletedAt);
        Assert.Equal(DailyTaskTime.Today, task.CompletedOn);
        Assert.Single(task.History);
        Assert.Equal("STATUS_CHANGED", task.History[0].Action);
    }

    [Fact]
    public async Task ChangeStatusAsync_FromDone_ClearsCompleted()
    {
        var current = new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User };
        var task = TestData.Task(
            5,
            assignedToId: 1,
            status: DailyTaskStatus.Done,
            completedAt: DateTime.UtcNow,
            completedOn: DailyTaskTime.Today);
        _tasks.Setup(r => r.GetByIdAsync(5, false)).ReturnsAsync(task);

        var service = Build(current);

        await service.ChangeStatusAsync(5, new ChangeDailyTaskStatusRequest(DailyTaskStatus.InProgress));

        Assert.Equal(DailyTaskStatus.InProgress, task.Status);
        Assert.Null(task.CompletedAt);
        Assert.Null(task.CompletedOn);
    }

    [Fact]
    public async Task AssignAsync_RegularUser_Forbidden()
    {
        var current = new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User };
        var service = Build(current);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.AssignAsync(5, new AssignDailyTaskRequest(2)));
    }

    [Fact]
    public async Task AssignAsync_Manager_UpdatesAreaAndHistory()
    {
        var current = new FakeCurrentUser { Id = 11, Role = DailyTaskRoles.Manager, Area = DailyTaskAreas.GerenciaGeneral };
        var task = TestData.Task(5, assignedToId: 1);
        var other = TestData.User(2, area: DailyTaskAreas.Tesoreria);
        _tasks.Setup(r => r.GetByIdAsync(5, false)).ReturnsAsync(task);
        _users.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(other);

        var service = Build(current);

        await service.AssignAsync(5, new AssignDailyTaskRequest(2));

        Assert.Equal(2, task.AssignedToId);
        Assert.Equal(DailyTaskAreas.Tesoreria, task.Area);
        Assert.Single(task.History);
        Assert.Equal("ASSIGNED", task.History[0].Action);
    }

    [Fact]
    public async Task DeleteAsync_Manager_SoftDeletesAndHistory()
    {
        var current = new FakeCurrentUser { Id = 11, Role = DailyTaskRoles.Manager, Area = DailyTaskAreas.GerenciaGeneral };
        var task = TestData.Task(5, assignedToId: 1);
        _tasks.Setup(r => r.GetByIdAsync(5, false)).ReturnsAsync(task);

        var service = Build(current);

        await service.DeleteAsync(5);

        Assert.NotNull(task.DeletedAt);
        Assert.Equal(11, task.DeletedById);
        Assert.Single(task.History);
        Assert.Equal("DELETED", task.History[0].Action);
        _notifier.Verify(n => n.NotifyAsync("taskDeleted", It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RestoreAsync_Manager_ClearsDeleted()
    {
        var current = new FakeCurrentUser { Id = 11, Role = DailyTaskRoles.Manager, Area = DailyTaskAreas.GerenciaGeneral };
        var task = TestData.Task(5, assignedToId: 1, deletedAt: DateTime.UtcNow);
        _tasks.Setup(r => r.GetByIdAsync(5, true)).ReturnsAsync(task);

        var service = Build(current);

        await service.RestoreAsync(5);

        Assert.Null(task.DeletedAt);
        Assert.Null(task.DeletedById);
        Assert.Single(task.History);
        Assert.Equal("RESTORED", task.History[0].Action);
    }

    [Fact]
    public async Task ListAsync_RegularUser_IncludeDeleted_Forbidden()
    {
        var current = new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User };
        var service = Build(current);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.ListAsync(new DailyTaskQuery { IncludeDeleted = true }));
    }

    [Fact]
    public async Task GetAsync_DeletedTask_RegularUser_NotFound()
    {
        var current = new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User };
        _tasks.Setup(r => r.GetByIdAsync(5, false)).ReturnsAsync((DailyTask?)null);
        var service = Build(current);

        await Assert.ThrowsAsync<NotFoundException>(() => service.GetAsync(5));
    }

    [Fact]
    public async Task HistoryAsync_RegularUser_OtherTask_Forbidden()
    {
        var current = new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User };
        _tasks.Setup(r => r.GetByIdAsync(7, true))
            .ReturnsAsync(TestData.Task(7, assignedToId: 2));
        var service = Build(current);

        await Assert.ThrowsAsync<ForbiddenException>(() => service.HistoryAsync(7));
    }

    [Fact]
    public async Task HistoryAsync_RegularUser_OwnTask_ReturnsEntries()
    {
        var current = new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User };
        _tasks.Setup(r => r.GetByIdAsync(5, true))
            .ReturnsAsync(TestData.Task(5, assignedToId: 1));
        _history.Setup(r => r.ListByTaskAsync(5))
            .ReturnsAsync(new List<DailyTaskHistory>
            {
                new()
                {
                    Id = 1,
                    TaskId = 5,
                    ChangedById = 1,
                    ChangedAt = DateTime.UtcNow,
                    ChangeGroupId = Guid.NewGuid(),
                    Action = "CREATED"
                }
            });
        var service = Build(current);

        var result = await service.HistoryAsync(5);

        Assert.Single(result);
        Assert.Equal("CREATED", result[0].Action);
    }

    [Fact]
    public async Task HistoryAsync_Manager_AnyTask_ReturnsEntries()
    {
        var current = new FakeCurrentUser
        {
            Id = 11,
            Role = DailyTaskRoles.Manager,
            Area = DailyTaskAreas.GerenciaGeneral
        };
        _tasks.Setup(r => r.GetByIdAsync(6, true))
            .ReturnsAsync(TestData.Task(6, assignedToId: 2));
        _history.Setup(r => r.ListByTaskAsync(6))
            .ReturnsAsync(new List<DailyTaskHistory>());
        var service = Build(current);

        var result = await service.HistoryAsync(6);

        Assert.Empty(result);
    }

    [Fact]
    public async Task BoardAsync_Done_OnlyIncludesTasksCompletedOnBoardDate()
    {
        var current = new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User };
        var today = DailyTaskTime.Today;
        var yesterday = today.AddDays(-1);
        var pending = TestData.Task(1, status: DailyTaskStatus.Pending, dueDate: today);
        var doneYesterday = TestData.Task(2, status: DailyTaskStatus.Done, dueDate: today, completedOn: yesterday);
        var doneToday = TestData.Task(3, status: DailyTaskStatus.Done, dueDate: today.AddDays(3), completedOn: today);

        DailyTaskFilter? activeFilter = null;
        DailyTaskFilter? doneFilter = null;
        var calls = 0;

        _tasks.Setup(r => r.ListAllAsync(It.IsAny<DailyTaskFilter>(), It.IsAny<bool>()))
            .Returns((DailyTaskFilter filter, bool _) =>
            {
                if (calls++ == 0)
                {
                    activeFilter = filter;
                    return Task.FromResult(new List<DailyTask> { pending, doneYesterday, doneToday });
                }

                doneFilter = filter;
                return Task.FromResult(new List<DailyTask> { doneToday });
            });

        var service = Build(current);

        var board = await service.BoardAsync(new DailyTaskQuery { DueTo = today, DueFrom = today.AddDays(-10) });

        Assert.Single(board.Pending);
        Assert.Empty(board.InProgress);
        Assert.Single(board.Done);
        Assert.Equal(3, board.Done[0].Id);
        Assert.NotNull(activeFilter);
        Assert.Equal(today, activeFilter!.DueTo);
        Assert.NotNull(doneFilter);
        Assert.Equal(today, doneFilter!.CompletedOn);
        Assert.Equal(DailyTaskStatus.Done, doneFilter.Status);
        Assert.Null(doneFilter.DueTo);
        Assert.Null(doneFilter.DueDate);
    }

    [Fact]
    public async Task BoardAsync_DueDate_UsesSelectedDayForCompleted()
    {
        var current = new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User };
        var selected = DailyTaskTime.Today.AddDays(-2);
        var done = TestData.Task(4, status: DailyTaskStatus.Done, dueDate: selected, completedOn: selected);

        DailyTaskFilter? doneFilter = null;
        var calls = 0;

        _tasks.Setup(r => r.ListAllAsync(It.IsAny<DailyTaskFilter>(), It.IsAny<bool>()))
            .Returns((DailyTaskFilter filter, bool _) =>
            {
                if (calls++ == 0) return Task.FromResult(new List<DailyTask>());

                doneFilter = filter;
                return Task.FromResult(new List<DailyTask> { done });
            });

        var service = Build(current);

        var board = await service.BoardAsync(new DailyTaskQuery { DueDate = selected });

        Assert.Empty(board.Pending);
        Assert.Empty(board.InProgress);
        Assert.Single(board.Done);
        Assert.NotNull(doneFilter);
        Assert.Equal(selected, doneFilter!.CompletedOn);
        Assert.Null(doneFilter.DueDate);
    }
}
