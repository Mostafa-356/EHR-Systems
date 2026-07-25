using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EHRPlatform.Services.Analytics.Features.Analytics.Commands;
using EHRPlatform.Services.Analytics.Features.Analytics.Queries;
using EHRPlatform.Services.Analytics.Features.Analytics.Dtos.Responses;

namespace EHRPlatform.Services.Analytics.Controllers;

/// <summary>
/// Analytics and reporting endpoints.
/// KPI dashboards, metrics, custom reports, business intelligence.
/// </summary>
[ApiController]
[Route("api/v1/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AnalyticsController(IMediator mediator) => _mediator = mediator;

    /// <summary>Get KPI summary (cached). Executive dashboard with key performance indicators.</summary>
    [HttpGet("kpi-summary")]
    [ProducesResponseType(typeof(AnalyticsMetricListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetKPISummary(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct)
        => Ok(await _mediator.Send(new GetKPISummaryQuery { PeriodStart = from, PeriodEnd = to }, ct));

    /// <summary>Get metrics by category (cached).</summary>
    [HttpGet("metrics")]
    [ProducesResponseType(typeof(AnalyticsMetricResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMetrics(
        [FromQuery] string category, [FromQuery] DateTime from, [FromQuery] DateTime to, CancellationToken ct)
        => Ok(await _mediator.Send(new GetMetricsQuery { Category = category, PeriodStart = from, PeriodEnd = to }, ct));

    /// <summary>Get user dashboards (cached).</summary>
    [HttpGet("dashboards")]
    [ProducesResponseType(typeof(List<DashboardResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserDashboards([FromQuery] Guid userId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetUserDashboardsQuery { UserId = userId }, ct));

    /// <summary>Get dashboard by id (cached). Includes widgets.</summary>
    [HttpGet("dashboards/{dashboardId:guid}")]
    [ProducesResponseType(typeof(DashboardResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDashboard(Guid dashboardId, [FromQuery] Guid userId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetUserDashboardQuery { DashboardId = dashboardId, UserId = userId }, ct));

    /// <summary>Create dashboard.</summary>
    [HttpPost("dashboards")]
    [ProducesResponseType(typeof(DashboardResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateDashboard([FromBody] CreateDashboardCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetDashboard), new { dashboardId = result.Id, userId = result.UserId }, result);
    }

    /// <summary>Add widget to dashboard.</summary>
    [HttpPost("dashboards/{dashboardId:guid}/widgets")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AddWidget(Guid dashboardId, [FromBody] AddDashboardWidgetCommand command, CancellationToken ct)
    {
        command = command with { DashboardId = dashboardId };
        await _mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>Get user reports (cached).</summary>
    [HttpGet("reports")]
    [ProducesResponseType(typeof(List<ReportResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserReports([FromQuery] Guid userId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetUserReportsQuery { UserId = userId }, ct));

    /// <summary>Get report by id (cached).</summary>
    [HttpGet("reports/{reportId:guid}")]
    [ProducesResponseType(typeof(ReportResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReport(Guid reportId, CancellationToken ct)
        => Ok(await _mediator.Send(new GetReportQuery { ReportId = reportId }, ct));

    /// <summary>Create report template.</summary>
    [HttpPost("reports")]
    [ProducesResponseType(typeof(ReportResponseDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateReport([FromBody] CreateReportCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetReport), new { reportId = result.Id }, result);
    }

    /// <summary>Generate report on-demand.</summary>
    [HttpPost("reports/{reportId:guid}/generate")]
    [ProducesResponseType(typeof(ReportExecutionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateReport(Guid reportId, CancellationToken ct)
        => Ok(await _mediator.Send(new GenerateReportCommand { ReportId = reportId }, ct));

    /// <summary>Health check endpoint.</summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health() => Ok(new { status = "healthy", service = "analytics-service" });
}
