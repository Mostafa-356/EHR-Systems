using EHRPlatform.Common.Extensions;
using EHRPlatform.Services.Notification.Consumers;
using EHRPlatform.Services.Notification.Hubs;
using MassTransit;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, config) =>
    config.ReadFrom.Configuration(ctx.Configuration));

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(opts =>
{
    opts.EnableDetailedErrors = builder.Environment.IsDevelopment();
    opts.MaximumReceiveMessageSize = 32 * 1024; // 32KB
});

// ── MassTransit: RabbitMQ (background jobs) + Kafka rider (domain events) ────
builder.Services.AddMassTransitHybrid(
    builder.Configuration,
    configureRabbitMqConsumers: x =>
    {
        x.AddConsumer<SendWelcomeNotificationConsumer>();
    },
    configureKafkaRider: rider =>
    {
        // Bridge: Kafka domain events → SignalR push
        rider.AddConsumer<LabResultConsumer>();
        // Add more Kafka consumers here as clinical events are added:
        //   rider.AddConsumer<VitalAlertConsumer>();
        //   rider.AddConsumer<AppointmentReminderConsumer>();
    });

// ── OpenTelemetry ─────────────────────────────────────────────────────────────
builder.Services.AddEHRTelemetry(builder.Configuration, "notification-service");

// ── Health Checks ─────────────────────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── CORS (must allow SignalR WebSocket connections) ───────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:4200", "http://localhost:5000"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("SignalRCors", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()); // Required for SignalR WebSocket
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("SignalRCors");
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<EHRNotificationHub>("/hubs/notifications");
app.MapHealthChecks("/health");

await app.RunAsync();
