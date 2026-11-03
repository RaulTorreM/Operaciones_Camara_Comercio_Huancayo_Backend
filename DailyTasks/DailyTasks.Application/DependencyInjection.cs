using DailyTasks.Application.Interfaces;
using DailyTasks.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DailyTasks.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddDailyTasksApplication(this IServiceCollection services)
    {
        services.AddScoped<IDailyTaskAuthService, DailyTaskAuthService>();
        services.AddScoped<IDailyTaskService, DailyTaskService>();
        services.AddScoped<IDailyTaskReportService, DailyTaskReportService>();
        return services;
    }
}
