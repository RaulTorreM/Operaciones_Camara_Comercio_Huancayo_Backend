using DailyTasks.Application.Common;

namespace DailyTasks.Tests;

public class DailyTaskCreationScheduleTests
{
    private static readonly TimeOnly Start = new(8, 30);
    private static readonly TimeOnly End = new(19, 0);

    [Fact]
    public void Weekday_WithinSchedule_IsAllowed()
    {
        var friday = new DateOnly(2026, 11, 13);
        Assert.True(DailyTaskCreationSchedule.IsWithin(
            friday, new TimeOnly(12, 0), Start, End, allowWeekend: false));
    }

    [Fact]
    public void Weekday_BeforeOpening_IsBlocked()
    {
        var friday = new DateOnly(2026, 11, 13);
        Assert.False(DailyTaskCreationSchedule.IsWithin(
            friday, new TimeOnly(8, 0), Start, End, allowWeekend: false));
    }

    [Fact]
    public void Weekday_AfterClosing_IsBlocked()
    {
        var friday = new DateOnly(2026, 11, 13);
        Assert.False(DailyTaskCreationSchedule.IsWithin(
            friday, new TimeOnly(19, 30), Start, End, allowWeekend: false));
    }

    [Fact]
    public void Weekend_Disabled_IsBlockedEvenWithinHours()
    {
        var saturday = new DateOnly(2026, 11, 14);
        Assert.False(DailyTaskCreationSchedule.IsWithin(
            saturday, new TimeOnly(12, 0), Start, End, allowWeekend: false));
    }

    [Fact]
    public void Weekend_Enabled_IsAllowedWithinHours()
    {
        var sunday = new DateOnly(2026, 11, 15);
        Assert.True(DailyTaskCreationSchedule.IsWithin(
            sunday, new TimeOnly(12, 0), Start, End, allowWeekend: true));
    }

    [Fact]
    public void Weekend_Enabled_IsBlockedOutsideHours()
    {
        var sunday = new DateOnly(2026, 11, 15);
        Assert.False(DailyTaskCreationSchedule.IsWithin(
            sunday, new TimeOnly(20, 0), Start, End, allowWeekend: true));
    }
}
