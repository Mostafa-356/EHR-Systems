using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EHRPlatform.Services.Appointment.Features.Appointments.Commands;
using EHRPlatform.Services.Appointment.Features.Appointments.Queries;

namespace EHRPlatform.Services.Appointment.Controllers;

/// <summary>
/// Appointment scheduling endpoints.
/// Book, confirm, cancel, check-in, complete appointments.
/// Provider availability management.
/// </summary>
[ApiController]
[Route("api/v1/appointments")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AppointmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Schedule new appointment.
    /// Validates provider availability, sets auto-reminders.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ScheduleAppointment(
        [FromBody] ScheduleAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetAppointment), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get appointment by ID (cached).
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(AppointmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAppointment(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetAppointmentQuery { AppointmentId = id },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get patient appointments (cached, paginated).
    /// Optional date range filtering.
    /// </summary>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(AppointmentListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPatientAppointments(
        Guid patientId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetPatientAppointmentsQuery
            {
                PatientId = patientId,
                FromDate = from,
                ToDate = to,
                PageNumber = page,
                PageSize = pageSize
            },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get provider calendar for specific date (cached).
    /// Shows all appointments and availability.
    /// </summary>
    [HttpGet("provider/{providerId}/calendar")]
    [ProducesResponseType(typeof(ProviderAppointmentCalendarDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderCalendar(
        Guid providerId,
        [FromQuery] DateTime date,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetProviderAppointmentsQuery { ProviderId = providerId, Date = date },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get provider availability slots (cached).
    /// Date range for booking.
    /// </summary>
    [HttpGet("provider/{providerId}/availability")]
    [ProducesResponseType(typeof(ProviderAvailabilityListDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProviderAvailability(
        Guid providerId,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new GetProviderAvailabilityQuery { ProviderId = providerId, FromDate = from, ToDate = to },
            cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Confirm appointment.
    /// Patient confirms scheduled appointment.
    /// </summary>
    [HttpPost("{id}/confirm")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmAppointment(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new ConfirmAppointmentCommand { AppointmentId = id },
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Cancel appointment.
    /// Returns availability slot to provider's open pool.
    /// </summary>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelAppointment(
        Guid id,
        [FromBody] string reason = "",
        CancellationToken cancellationToken = default)
    {
        await _mediator.Send(
            new CancelAppointmentCommand { AppointmentId = id, Reason = reason },
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Check-in for appointment.
    /// Patient arrives at clinic/virtual waiting room.
    /// </summary>
    [HttpPost("{id}/check-in")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckIn(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new CheckInAppointmentCommand { AppointmentId = id },
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Complete appointment.
    /// Provider marks appointment as completed.
    /// </summary>
    [HttpPost("{id}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteAppointment(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new CompleteAppointmentCommand { AppointmentId = id },
            cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Set provider availability slot.
    /// Create recurring or one-time availability.
    /// </summary>
    [HttpPost("provider/availability")]
    [ProducesResponseType(typeof(ProviderAvailabilityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetAvailability(
        [FromBody] SetProviderAvailabilityCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetProviderAvailability), result);
    }

    /// <summary>
    /// Health check.
    /// </summary>
    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "appointment-service" });
    }
}
