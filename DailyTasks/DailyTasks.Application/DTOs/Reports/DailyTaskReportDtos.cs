using DailyTasks.Core.Enums;

namespace DailyTasks.Application.DTOs.Reports;

public sealed class DailyTaskReportQuery
{
    public DateOnly? From { get; set; }
    public DateOnly? To { get; set; }

    /// <summary>day, week o month.</summary>
    public string? Granularity { get; set; }

    public int? UserId { get; set; }
    public string? Area { get; set; }
    public DailyTaskStatus? Status { get; set; }
    public DailyTaskPriority? Priority { get; set; }
}

public record CountByStatusDto(DailyTaskStatus Status, int Count);

public record CountByPriorityDto(DailyTaskPriority Priority, int Count);

public record CountByAreaDto(string Area, string AreaName, int Count);

public record DailyTaskSummaryDto(
    int Total,
    int Pending,
    int InProgress,
    int Done,
    int Overdue,
    int DueToday,
    decimal CompletionRate,
    List<CountByStatusDto> ByStatus,
    List<CountByPriorityDto> ByPriority,
    List<CountByAreaDto> ByArea);

public record DailyTaskReportMetricsDto(
    int Total,
    int Pending,
    int InProgress,
    int Done,
    int Overdue,
    int DueToday,
    decimal CompletionRate,
    int CreatedInPeriod,
    int CompletedInPeriod);

public record DailyTaskReportSeriesPointDto(
    DateOnly PeriodStart,
    int Created,
    int Completed);

public record DailyTaskReportByUserDto(
    int UserId,
    string FullName,
    string Area,
    string AreaName,
    int Total,
    int Pending,
    int InProgress,
    int Done,
    int Overdue,
    decimal CompletionRate,
    int CreatedInPeriod,
    int CompletedInPeriod);

public record DailyTaskReportByAreaDto(
    string Area,
    string AreaName,
    int Total,
    int Done,
    int Overdue,
    decimal CompletionRate);

public record DailyTaskReportDto(
    DateOnly From,
    DateOnly To,
    string Granularity,
    DailyTaskReportMetricsDto Metrics,
    List<DailyTaskReportSeriesPointDto> Series,
    List<DailyTaskReportByUserDto> ByUser,
    List<DailyTaskReportByAreaDto> ByArea,
    List<CountByStatusDto> ByStatus,
    List<CountByPriorityDto> ByPriority);
