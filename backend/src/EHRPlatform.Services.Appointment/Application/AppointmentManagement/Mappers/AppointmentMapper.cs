using Mapster;
using EHRPlatform.Common.Mapping;
using EHRPlatform.Services.Appointment.Application.AppointmentManagement.Responses;
using Microsoft.Extensions.Logging;
using Appointment = EHRPlatform.Services.Appointment.Features.Appointments.Domain.Appointment;
using ProviderAvailability = EHRPlatform.Services.Appointment.Features.Appointments.Domain.ProviderAvailability;

namespace EHRPlatform.Services.Appointment.Application.AppointmentManagement.Mappers;

/// <summary>
/// Appointment mapper. Single Responsibility: convert Appointment domain model to DTOs.
/// </summary>
public class AppointmentMapper : MappingServiceBase<Appointment, AppointmentResponseDto>
{
    public AppointmentMapper(ILogger<AppointmentMapper> logger) : base(logger) { }

    public AppointmentResponseDto MapToResponseDto(Appointment appointment) => MapToDto(appointment);

    public List<AppointmentResponseDto> MapToResponseDtoList(ICollection<Appointment> appointments)
    {
        Logger.LogDebug("Mapping {Count} appointments to response DTO list", appointments.Count);
        return appointments.Adapt<List<AppointmentResponseDto>>();
    }

    public AppointmentListDto MapToListDto(IList<Appointment> appointments, int total, int pageNumber, int pageSize)
        => new()
        {
            Items = appointments.Adapt<List<AppointmentResponseDto>>(),
            Total = total, PageNumber = pageNumber, PageSize = pageSize
        };

    public ProviderAppointmentCalendarDto MapToProviderCalendarDto(Guid providerId, DateTime date, IList<Appointment> appointments)
        => new()
        {
            ProviderId = providerId, Date = date,
            Slots = appointments.Select(a => new AppointmentSlotDto
            {
                AppointmentId = a.Id, PatientId = a.PatientId,
                Start = a.ScheduledStart, End = a.ScheduledEnd,
                AppointmentType = a.AppointmentType, Status = a.Status
            }).ToList()
        };

    public ProviderAvailabilityListDto MapToAvailabilityListDto(Guid providerId, IList<ProviderAvailability> slots)
        => new()
        {
            ProviderId = providerId,
            Slots = slots.Select(s => new ProviderAvailabilitySlotDto
            {
                Id = s.Id, SlotStart = s.SlotStart, SlotEnd = s.SlotEnd,
                IsRecurring = s.IsRecurring, RecurrencePattern = s.RecurrencePattern,
                MaxAppointmentsPerSlot = s.MaxAppointmentsPerSlot, CurrentBookings = s.CurrentBookings,
                HasAvailability = s.HasAvailability()
            }).ToList()
        };
}
