using EHRPlatform.Common.CQRS;
using EHRPlatform.Common.Data;
using EHRPlatform.Services.Billing.Features.Reports.Queries;
using Microsoft.Extensions.Logging;

namespace EHRPlatform.Services.Billing.Features.Reports.Handlers;

/// <summary>
/// Get billing report handler — generates aggregate billing metrics.
/// GetPatientInvoicesQueryHandler and GetPatientOutstandingBalanceQueryHandler
/// live in GetPatientInvoicesQueryHandler.cs (single source of truth).
/// </summary>
public class GetBillingReportQueryHandler : IQueryHandler<GetBillingReportQuery, BillingReportDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ReportMapper _mapper;
    private readonly ILogger<GetBillingReportQueryHandler> _logger;

    public GetBillingReportQueryHandler(
        IUnitOfWork unitOfWork,
        ReportMapper mapper,
        ILogger<GetBillingReportQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger;
    }

    public async Task<BillingReportDto> Handle(
        GetBillingReportQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Generating billing report for {StartDate} to {EndDate}",
            request.StartDate, request.EndDate);

        var repo = _unitOfWork.Repository<Invoice>();
        var invoices = await repo.ToListAsync(
            q => q.Where(i => i.ServiceDate >= request.StartDate && i.ServiceDate <= request.EndDate),
            cancellationToken);

        var report = _mapper.MapToBillingReportDto(request.StartDate, request.EndDate, invoices);

        _logger.LogInformation("Billing report generated with {InvoiceCount} invoices", invoices.Count);
        return report;
    }
}
