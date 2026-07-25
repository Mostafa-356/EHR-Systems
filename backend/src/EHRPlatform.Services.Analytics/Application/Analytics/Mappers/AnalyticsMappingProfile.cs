using Mapster;
using EHRPlatform.Services.Analytics.Domain.Entities;
using EHRPlatform.Services.Analytics.Features.Analytics.Dtos.Responses;

namespace EHRPlatform.Services.Analytics.Application.Analytics.Mappers;

/// <summary>
/// Mapster configuration for Analytics entities → DTOs.
/// </summary>
public static class AnalyticsMappingConfig
{
    public static void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Dashboard, DashboardResponseDto>()
            .Map(dest => dest.Widgets, src => src.DashboardWidgets.Select(w => new DashboardWidgetDto
            {
                Id = w.Id,
                WidgetType = w.WidgetType,
                Title = w.Title,
                MetricName = w.MetricName
            }).ToList());

        config.NewConfig<Report, ReportResponseDto>();
        config.NewConfig<ReportExecution, ReportExecutionResponseDto>();
        config.NewConfig<AnalyticsMetric, MetricItemDto>()
            .Map(dest => dest.Name, src => src.MetricName);
    }
}
