namespace EHRPlatform.Services.Analytics.Features.Analytics.Dtos.Responses;

// ── Metric DTOs ───────────────────────────────────────────────────────────────

public class MetricItemDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string? Unit { get; set; }
}

public class TrendItemDto
{
    public DateTime Date { get; set; }
    public decimal Value { get; set; }
    public string? Label { get; set; }
}

public class AnalyticsMetricResponseDto
{
    public string Category { get; set; } = string.Empty;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<MetricItemDto> Metrics { get; set; } = new();
}

public class AnalyticsMetricListDto
{
    public decimal PatientVolume { get; set; }
    public decimal AppointmentUtilization { get; set; }
    public decimal RevenueTotal { get; set; }
    public List<TrendItemDto> Trends { get; set; } = new();
}

// ── Dashboard DTOs ────────────────────────────────────────────────────────────

public class DashboardWidgetDto
{
    public Guid Id { get; set; }
    public string WidgetType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
}

public class DashboardResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public List<DashboardWidgetDto> Widgets { get; set; } = new();
}

// ── Report DTOs ───────────────────────────────────────────────────────────────

public class ReportResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ReportType { get; set; }
    public List<string> Metrics { get; set; } = new();
    public string Schedule { get; set; } = "OnDemand";
    public DateTime? LastGeneratedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReportExecutionResponseDto
{
    public Guid Id { get; set; }
    public Guid ReportId { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public int RecordCount { get; set; }
}
