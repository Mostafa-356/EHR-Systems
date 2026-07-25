global using Microsoft.Extensions.Logging;
global using EHRPlatform.Services.Appointment.Data;
global using EHRPlatform.Services.Appointment.Data.Configuration;
global using EHRPlatform.Services.Appointment.Data.Seeds;
global using EHRPlatform.Services.Appointment.Features.Appointments.Domain;
global using EHRPlatform.Services.Appointment.Domain.Events;
global using EHRPlatform.Services.Appointment.Domain.Enums;
global using EHRPlatform.Services.Appointment.Application.AppointmentManagement;
global using EHRPlatform.Services.Appointment.Application.AppointmentManagement.Mappers;
global using EHRPlatform.Services.Appointment.Application.AppointmentManagement.Responses;
global using EHRPlatform.Services.Appointment.Application.AppointmentManagement.Requests;
global using EHRPlatform.Services.Appointment.Features.Appointments.Commands;
global using EHRPlatform.Services.Appointment.Features.Appointments.Queries;
global using EHRPlatform.Services.Appointment.Features.Appointments.Validation;
// Type alias to resolve 'Appointment' vs namespace 'EHRPlatform.Services.Appointment' ambiguity
global using Appointment = EHRPlatform.Services.Appointment.Features.Appointments.Domain.Appointment;
