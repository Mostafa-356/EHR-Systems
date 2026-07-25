using EHRPlatform.Common.CQRS;
using EHRPlatform.Services.Analytics.Features.Analytics.Dtos.Responses;

namespace EHRPlatform.Services.Analytics.Features.Analytics.Commands;

/// <summary>Record event metric command. Called by event listeners consuming domain events.</summary>
public record RecordEventMetricCommand : ICommand
{
    public string EventType { get; init; } = string.Empty;
    public Guid AggregateId { get; init; }
    public Dictionary<string, string> Properties { get; init; } = new();
}

/// <summary>Aggregate metrics command. Runs aggregation job for daily/weekly/monthly metrics.</summary>
public record AggregateMetricsCommand : ICommand
{
    public string Frequency { get; init; } = string.Empty; // Daily, Weekly, Monthly
    public DateTime? ForPeriod { get; init; }
}

/// <summary>Create dashboard command.</summary>
public record CreateDashboardCommand : ICommand<DashboardResponseDto>
{
    public Guid UserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public bool IsDefault { get; init; }
}

/// <summary>Add widget to dashboard command.</summary>
public record AddDashboardWidgetCommand : ICommand
{
    public Guid DashboardId { get; init; }
    public string WidgetType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string MetricName { get; init; } = string.Empty;
}

/// <summary>Create report template command.</summary>
public record CreateReportCommand : ICommand<ReportResponseDto>
{
    public Guid UserId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string ReportType { get; init; } = string.Empty;
    public List<string> Metrics { get; init; } = new();
    public string Schedule { get; init; } = "OnDemand";
}

/// <summary>Generate report command.</summary>
public record GenerateReportCommand : ICommand<ReportExecutionResponseDto>
{
    public Guid ReportId { get; init; }
}
