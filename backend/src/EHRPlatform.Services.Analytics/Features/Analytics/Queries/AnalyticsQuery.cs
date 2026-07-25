using EHRPlatform.Common.CQRS;
using EHRPlatform.Services.Analytics.Features.Analytics.Dtos.Responses;

namespace EHRPlatform.Services.Analytics.Features.Analytics.Queries;

/// <summary>Get metrics for period — cached query.</summary>
public record GetMetricsQuery : ICachedQuery<AnalyticsMetricResponseDto>
{
    public string Category { get; init; } = string.Empty;
    public DateTime PeriodStart { get; init; }
    public DateTime PeriodEnd { get; init; }

    public string CacheKey => $"metrics_{Category}_{PeriodStart:yyyyMMdd}_{PeriodEnd:yyyyMMdd}";
    public int CacheDurationSeconds => 3600;
}

/// <summary>Get KPI summary — cached query.</summary>
public record GetKPISummaryQuery : ICachedQuery<AnalyticsMetricListDto>
{
    public DateTime? PeriodStart { get; init; }
    public DateTime? PeriodEnd { get; init; }

    public string CacheKey => $"kpi_summary_{PeriodStart?.Date}_{PeriodEnd?.Date}";
    public int CacheDurationSeconds => 3600;
}

/// <summary>Get user dashboard — cached query.</summary>
public record GetUserDashboardQuery : ICachedQuery<DashboardResponseDto>
{
    public Guid UserId { get; init; }
    public Guid DashboardId { get; init; }

    public string CacheKey => $"dashboard_{UserId}_{DashboardId}";
    public int CacheDurationSeconds => 600;
}

/// <summary>Get user dashboards — cached query.</summary>
public record GetUserDashboardsQuery : ICachedQuery<List<DashboardResponseDto>>
{
    public Guid UserId { get; init; }

    public string CacheKey => $"dashboards_user_{UserId}";
    public int CacheDurationSeconds => 600;
}

/// <summary>Get report — cached query.</summary>
public record GetReportQuery : ICachedQuery<ReportResponseDto>
{
    public Guid ReportId { get; init; }

    public string CacheKey => $"report_{ReportId}";
    public int CacheDurationSeconds => 1800;
}

/// <summary>Get user reports — cached query.</summary>
public record GetUserReportsQuery : ICachedQuery<List<ReportResponseDto>>
{
    public Guid UserId { get; init; }

    public string CacheKey => $"reports_user_{UserId}";
    public int CacheDurationSeconds => 1800;
}
