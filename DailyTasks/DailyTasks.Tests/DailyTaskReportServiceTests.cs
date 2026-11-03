using DailyTasks.Application.Common;
using DailyTasks.Application.DTOs.Reports;
using DailyTasks.Application.Interfaces;
using DailyTasks.Application.Services;
using DailyTasks.Core.Entities;
using DailyTasks.Core.Enums;
using DailyTasks.Core.Exceptions;
using DailyTasks.Core.Interfaces;
using DailyTasks.Core.Models;
using Moq;

namespace DailyTasks.Tests;

public class DailyTaskReportServiceTests
{
    private readonly Mock<IDailyTaskRepository> _tasks = new();
    private readonly Mock<IDailyTaskUserRepository> _users = new();
    private readonly Mock<IDailyTaskHistoryRepository> _history = new();

    private DailyTaskReportService Build(FakeCurrentUser current) =>
        new(_tasks.Object, _users.Object, _history.Object, current);

    [Fact]
    public async Task GetSummaryAsync_CalculatesCountsAndRate()
    {
        var today = DailyTaskTime.Today;
        var data = new List<DailyTask>
        {
            TestData.Task(1, status: DailyTaskStatus.Pending, dueDate: today.AddDays(-1)),
            TestData.Task(2, status: DailyTaskStatus.InProgress, dueDate: today),
            TestData.Task(3, status: DailyTaskStatus.Done, dueDate: today,
                completedAt: DateTime.UtcNow, completedOn: today),
            TestData.Task(4, status: DailyTaskStatus.Pending, dueDate: today.AddDays(1))
        };
        _tasks.Setup(r => r.ListAllAsync(It.IsAny<DailyTaskFilter>(), false)).ReturnsAsync(data);

        var summary = await Build(new FakeCurrentUser { Id = 1 }).GetSummaryAsync();

        Assert.Equal(4, summary.Total);
        Assert.Equal(2, summary.Pending);
        Assert.Equal(1, summary.InProgress);
        Assert.Equal(1, summary.Done);
        Assert.Equal(1, summary.Overdue);
        Assert.Equal(1, summary.DueToday);
        Assert.Equal(25m, summary.CompletionRate);
        Assert.Equal(3, summary.ByStatus.Count);
        Assert.Equal(3, summary.ByPriority.Count);
        Assert.Equal(DailyTaskAreas.All.Count, summary.ByArea.Count);
    }

    [Fact]
    public async Task GetReportAsync_RegularUser_Forbidden()
    {
        var service = Build(new FakeCurrentUser { Id = 1, Role = DailyTaskRoles.User });

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetReportAsync(new DailyTaskReportQuery()));
    }

    [Fact]
    public async Task GetReportAsync_Manager_CalculatesPeriodAndSeries()
    {
        var today = DailyTaskTime.Today;
        var from = today.AddDays(-2);
        var to = today;

        var user1 = TestData.User(1);
        var user2 = TestData.User(2);
        var data = new List<DailyTask>
        {
            TestData.Task(1, assignedToId: 1, createdOn: today.AddDays(-1), status: DailyTaskStatus.Done,
                completedAt: DateTime.UtcNow, completedOn: today),
            TestData.Task(2, assignedToId: 2, createdOn: today, status: DailyTaskStatus.Pending),
            TestData.Task(3, assignedToId: 1, createdOn: today.AddDays(-5), status: DailyTaskStatus.Done,
                completedAt: DateTime.UtcNow, completedOn: today.AddDays(-1))
        };
        _tasks.Setup(r => r.ListAllAsync(It.IsAny<DailyTaskFilter>(), false)).ReturnsAsync(data);
        _users.Setup(r => r.ListActiveAsync(null)).ReturnsAsync(new List<DailyTaskUser> { user1, user2 });

        var report = await Build(new FakeCurrentUser
        {
            Id = 11,
            Role = DailyTaskRoles.Manager,
            Area = DailyTaskAreas.GerenciaGeneral
        }).GetReportAsync(new DailyTaskReportQuery { From = from, To = to, Granularity = "day" });

        Assert.Equal(2, report.Metrics.Total);
        Assert.Equal(1, report.Metrics.Done);
        Assert.Equal(2, report.Metrics.CompletedInPeriod);
        Assert.Equal(3, report.Series.Count);
        Assert.Equal(2, report.ByUser.Count);
        Assert.Equal(DailyTaskAreas.All.Count, report.ByArea.Count);
    }

    [Fact]
    public async Task GetMyReportAsync_ForcesCurrentUser()
    {
        var today = DailyTaskTime.Today;
        var current = new FakeCurrentUser { Id = 2, Role = DailyTaskRoles.User };
        var data = new List<DailyTask>
        {
            TestData.Task(1, assignedToId: 2, createdOn: today),
            TestData.Task(2, assignedToId: 3, createdOn: today)
        };
        _tasks.Setup(r => r.ListAllAsync(It.Is<DailyTaskFilter>(f => f.AssignedToId == 2), false))
            .ReturnsAsync(data.Where(t => t.AssignedToId == 2).ToList());
        _users.Setup(r => r.ListActiveAsync(null))
            .ReturnsAsync(new List<DailyTaskUser> { TestData.User(2) });

        var report = await Build(current).GetMyReportAsync(new DailyTaskReportQuery
        {
            From = today,
            To = today,
            UserId = 3 // debe ignorarse: el informe propio siempre usa el usuario autenticado
        });

        Assert.Equal(1, report.Metrics.Total);
        Assert.Single(report.ByUser);
        Assert.Equal(2, report.ByUser[0].UserId);
    }

    [Fact]
    public async Task GetReportAsync_InvalidRange_Throws()
    {
        var service = Build(new FakeCurrentUser
        {
            Id = 11,
            Role = DailyTaskRoles.Manager,
            Area = DailyTaskAreas.GerenciaGeneral
        });

        await Assert.ThrowsAsync<BusinessRuleException>(() =>
            service.GetReportAsync(new DailyTaskReportQuery
            {
                From = DailyTaskTime.Today,
                To = DailyTaskTime.Today.AddDays(-1)
            }));
    }

    [Fact]
    public async Task GetAuditAsync_RegularUser_Forbidden()
    {
        var service = Build(new FakeCurrentUser { Id = 2, Role = DailyTaskRoles.User });

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.GetAuditAsync(new DailyTaskAuditQuery()));
    }

    [Fact]
    public async Task GetAuditAsync_Manager_ReturnsPagedEntries()
    {
        var task = TestData.Task(5, title: "Tarea auditada");
        var history = new DailyTaskHistory
        {
            Id = 10,
            TaskId = task.Id,
            Task = task,
            ChangedById = 1,
            ChangedBy = TestData.User(1),
            ChangedAt = DateTime.UtcNow,
            Action = "UPDATED",
            Field = "DueDate",
            OldValue = "2026-11-01",
            NewValue = "2026-11-02"
        };

        _history.Setup(r => r.CountAsync(It.IsAny<DailyTaskAuditFilter>())).ReturnsAsync(1);
        _history.Setup(r => r.ListAsync(It.IsAny<DailyTaskAuditFilter>(), 0, 50))
            .ReturnsAsync(new List<DailyTaskHistory> { history });

        var result = await Build(new FakeCurrentUser
        {
            Id = 11,
            Role = DailyTaskRoles.Manager,
            Area = DailyTaskAreas.GerenciaGeneral
        }).GetAuditAsync(new DailyTaskAuditQuery { Page = 1, PageSize = 50 });

        Assert.Equal(1, result.Total);
        Assert.Single(result.Items);
        Assert.Equal("DT-5", result.Items[0].TaskCode);
        Assert.Equal("DueDate", result.Items[0].Field);
    }
}
