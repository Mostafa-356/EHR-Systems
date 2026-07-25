using Microsoft.EntityFrameworkCore;
using EHRPlatform.Common.Data;
// Alias resolves the naming conflict between the Appointment class and the
// EHRPlatform.Services.Appointment namespace segment.
using Appointment = EHRPlatform.Services.Appointment.Features.Appointments.Domain.Appointment;
using AppointmentReminder = EHRPlatform.Services.Appointment.Features.Appointments.Domain.AppointmentReminder;
using ProviderAvailability = EHRPlatform.Services.Appointment.Features.Appointments.Domain.ProviderAvailability;

namespace EHRPlatform.Services.Appointment.Data;

/// <summary>DbContext for Appointment Service.</summary>
public class AppointmentContext : BaseDbContext
{
    public AppointmentContext(DbContextOptions<AppointmentContext> options) : base(options) { }

    public DbSet<Appointment> Appointments { get; set; } = null!;
    public DbSet<AppointmentReminder> AppointmentReminders { get; set; } = null!;
    public DbSet<ProviderAvailability> ProviderAvailability { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new Configuration.AppointmentConfiguration());
        modelBuilder.ApplyConfiguration(new Configuration.AppointmentReminderConfiguration());
        modelBuilder.ApplyConfiguration(new Configuration.ProviderAvailabilityConfiguration());
        modelBuilder.SeedAppointments();
    }
}
