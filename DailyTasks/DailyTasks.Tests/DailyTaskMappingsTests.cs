using DailyTasks.Application.Common;
using DailyTasks.Application.Mappings;
using DailyTasks.Core.Enums;

namespace DailyTasks.Tests;

public class DailyTaskMappingsTests
{
    [Fact]
    public void ToDto_PendingPastDueDate_IsOverdue()
    {
        var today = DailyTaskTime.Today;
        var task = TestData.Task(7, status: DailyTaskStatus.Pending, dueDate: today.AddDays(-2));

        var dto = task.ToDto();

        Assert.Equal("DT-7", dto.Code);
        Assert.True(dto.IsOverdue);
        Assert.Equal(2, dto.DaysOverdue);
        Assert.False(dto.IsDeleted);
    }

    [Fact]
    public void ToDto_DonePastDueDate_IsNotOverdue()
    {
        var today = DailyTaskTime.Today;
        var task = TestData.Task(
            8,
            status: DailyTaskStatus.Done,
            dueDate: today.AddDays(-2),
            completedAt: DateTime.UtcNow,
            completedOn: today.AddDays(-1));

        var dto = task.ToDto();

        Assert.False(dto.IsOverdue);
        Assert.Equal(0, dto.DaysOverdue);
    }

    [Fact]
    public void ToCard_ComputesAreaNameAndAssignee()
    {
        var task = TestData.Task(9, assignedToId: 3);

        var card = task.ToCard();

        Assert.Equal("DT-9", card.Code);
        Assert.Equal("Usuario 3", card.AssignedToName);
        Assert.Equal(DailyTaskAreas.NameOf(DailyTaskAreas.Marketing), card.AreaName);
    }
}
