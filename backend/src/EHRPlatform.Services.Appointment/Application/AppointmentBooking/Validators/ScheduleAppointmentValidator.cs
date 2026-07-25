using FluentValidation;
using EHRPlatform.Services.Appointment.Features.Appointments.Commands;

namespace EHRPlatform.Services.Appointment.Application.AppointmentBooking.Validators;

/// <summary>Validator for ScheduleAppointmentCommand.</summary>
public class ScheduleAppointmentValidator : AbstractValidator<ScheduleAppointmentCommand>
{
    public ScheduleAppointmentValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.ProviderId).NotEmpty();
        RuleFor(x => x.ScheduledStart).GreaterThan(DateTime.UtcNow);
        RuleFor(x => x.DurationMinutes).GreaterThan(0).LessThanOrEqualTo(480);
        RuleFor(x => x.AppointmentType)
            .Must(t => new[] { "Office", "Telehealth", "Phone" }.Contains(t))
            .WithMessage("AppointmentType must be Office, Telehealth, or Phone");
    }
}
