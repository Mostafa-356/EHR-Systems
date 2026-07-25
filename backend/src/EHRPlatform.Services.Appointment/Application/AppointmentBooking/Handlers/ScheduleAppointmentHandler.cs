using EHRPlatform.Common.CQRS;
using EHRPlatform.Common.Data;
using EHRPlatform.Common.Messaging;
using EHRPlatform.Services.Appointment.Application.AppointmentManagement.Responses;
using EHRPlatform.Services.Appointment.Features.Appointments.Commands;
using Appointment = EHRPlatform.Services.Appointment.Features.Appointments.Domain.Appointment;
using ProviderAvailability = EHRPlatform.Services.Appointment.Features.Appointments.Domain.ProviderAvailability;

namespace EHRPlatform.Services.Appointment.Application.AppointmentBooking.Handlers;

/// <summary>
/// Schedule appointment handler. Validates provider availability, publishes event.
/// Delegates all mapping to AppointmentMapper (SRP).
/// </summary>
public class ScheduleAppointmentHandler : ICommandHandler<ScheduleAppointmentCommand, AppointmentResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outbox;
    private readonly AppointmentManagement.Mappers.AppointmentMapper _mapper;
    private readonly ILogger<ScheduleAppointmentHandler> _logger;

    public ScheduleAppointmentHandler(
        IUnitOfWork unitOfWork,
        IOutboxRepository outbox,
        AppointmentManagement.Mappers.AppointmentMapper mapper,
        ILogger<ScheduleAppointmentHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _outbox = outbox;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<AppointmentResponseDto> Handle(
        ScheduleAppointmentCommand command,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Scheduling appointment: Patient {PatientId}, Provider {ProviderId}, Start {Start}",
            command.PatientId, command.ProviderId, command.ScheduledStart);

        var availRepo = _unitOfWork.Repository<ProviderAvailability>();
        var availSlot = await availRepo.FirstOrDefaultAsync(
            q => q.Where(a =>
                a.ProviderId == command.ProviderId &&
                a.SlotStart <= command.ScheduledStart &&
                a.SlotEnd >= command.ScheduledStart.AddMinutes(command.DurationMinutes) &&
                a.IsActive),
            cancellationToken);

        if (availSlot == null || !availSlot.HasAvailability())
            throw new InvalidOperationException("Provider slot not available at requested time");

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            PatientId = command.PatientId,
            ProviderId = command.ProviderId,
            ScheduledStart = command.ScheduledStart,
            ScheduledEnd = command.ScheduledStart.AddMinutes(command.DurationMinutes),
            AppointmentType = command.AppointmentType,
            Status = "Scheduled",
            ReasonForVisit = command.ReasonForVisit,
            Notes = command.Notes,
            DurationMinutes = command.DurationMinutes
        };

        appointment.AddReminder(command.ScheduledStart.AddHours(-24), "Email");
        appointment.AddReminder(command.ScheduledStart.AddHours(-2), "SMS");
        availSlot.BookSlot();

        var appointmentRepo = _unitOfWork.Repository<Appointment>();
        await appointmentRepo.AddAsync(appointment, cancellationToken);
        await availRepo.UpdateAsync(availSlot, cancellationToken);

        var scheduledEvent = new Domain.Events.AppointmentScheduledEvent(
            appointment.Id, appointment.PatientId, appointment.ProviderId,
            appointment.ScheduledStart, appointment.AppointmentType);

        await _outbox.AddAsync(new OutboxEvent
        {
            Id = Guid.NewGuid(),
            AggregateId = appointment.Id,
            EventType = nameof(Domain.Events.AppointmentScheduledEvent),
            EventData = System.Text.Json.JsonSerializer.Serialize(scheduledEvent),
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Appointment scheduled {AppointmentId}", appointment.Id);
        return _mapper.MapToResponseDto(appointment);
    }
}
