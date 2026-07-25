using EHRPlatform.Common.Extensions;
using EHRPlatform.Common.Health;
using EHRPlatform.Common.Messaging;
using EHRPlatform.Services.Patient.Data;
using EHRPlatform.Services.Patient.Messaging.Consumers;
using EHRPlatform.Services.Patient.Sagas;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ──────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, config) =>
    config.ReadFrom.Configuration(ctx.Configuration));

// ── Controllers & Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Database (PostgreSQL via Replit env vars or explicit connection string) ────
var connectionString = builder.Configuration.BuildPostgresConnectionString();
builder.Services.AddPostgresDataAccess<PatientContext>(connectionString);

// ── Outbox repository (writes domain events atomically with patient data) ──────
builder.Services.AddScoped<IOutboxRepository>(sp =>
    new OutboxRepository(sp.GetRequiredService<PatientContext>()));

// ── CQRS (MediatR + FluentValidation + pipeline behaviors) ───────────────────
builder.Services.AddCQRSFromCurrentAssembly();

// ── Redis Caching (optional — degrades gracefully if unavailable) ─────────────
var redisConnStr = builder.Configuration["Redis:ConnectionString"]
    ?? builder.Configuration["EHRCommon:RedisConnectionString"]
    ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

if (!string.IsNullOrEmpty(redisConnStr))
{
    try
    {
        builder.Services.AddRedisCaching(redisConnStr);
        Log.Information("Redis caching enabled for Patient Service");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Redis not available for Patient Service — caching disabled");
    }
}
else
{
    Log.Warning("Redis connection string not configured — caching disabled");
}

// ── Elasticsearch (optional — degrades gracefully if unavailable) ─────────────
var esUrl = builder.Configuration["Elasticsearch:Url"]
    ?? Environment.GetEnvironmentVariable("ELASTICSEARCH_URL");

if (!string.IsNullOrEmpty(esUrl))
{
    try
    {
        builder.Services.AddElasticsearchSearch(esUrl);
        Log.Information("Elasticsearch enabled for Patient Service at {Url}", esUrl);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Elasticsearch not available — search disabled for Patient Service");
    }
}
else
{
    Log.Warning("Elasticsearch:Url not configured — patient search disabled");
}

// ── MongoDB (optional — used for clinical documents, audit logs, device data) ──
var mongoConnStr  = builder.Configuration["MongoDB:ConnectionString"]
    ?? Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING");
var mongoDbName   = builder.Configuration["MongoDB:DatabaseName"] ?? "ehr_patient";

if (!string.IsNullOrEmpty(mongoConnStr))
{
    try
    {
        builder.Services.AddMongoDataAccess(mongoConnStr, mongoDbName);
        Log.Information("MongoDB enabled for Patient Service, database: {Db}", mongoDbName);
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "MongoDB not available — document store disabled for Patient Service");
    }
}
else
{
    Log.Warning("MongoDB:ConnectionString not configured — document store disabled");
}

// ── Kafka raw publisher + resilience decorator (outbox uses this) ─────────────
var kafkaServers = builder.Configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
builder.Services.AddKafkaMessaging(kafkaServers, builder.Environment.EnvironmentName);
builder.Services.AddResilientEventPublisher();

// ── MassTransit: Kafka (domain events) + RabbitMQ (background jobs + saga) ───
builder.Services.AddMassTransitHybrid(
    builder.Configuration,
    configureRabbitMqConsumers: x =>
    {
        // RabbitMQ consumers
        x.AddConsumer<WelcomeNotificationConsumer>();
        x.AddConsumer<PatientIndexConsumer>();

        // Saga
        x.AddSagaStateMachine<PatientRegistrationSaga, PatientRegistrationSagaState>()
            .EntityFrameworkRepository(r =>
            {
                r.ExistingDbContext<PatientContext>();
                r.UsePostgres();
            });
    },
    configureKafkaRider: rider =>
    {
        // Kafka consumer for PatientCreatedEvent (self-subscription for side effects)
        rider.AddConsumer<PatientCreatedKafkaConsumer>();

        rider.AddProducer<EHRPlatform.Services.Patient.Domain.Events.PatientCreatedEvent>(
            "patient-created-event");
    });

// ── OpenTelemetry ─────────────────────────────────────────────────────────────
builder.Services.AddEHRTelemetry(builder.Configuration, "patient-service");

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET")
    ?? throw new InvalidOperationException("JWT_SECRET not configured");
builder.Services.AddJwtAuthentication(jwtSecret);

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// ── Health Checks ─────────────────────────────────────────────────────────────
var healthBuilder = builder.Services.AddHealthChecks()
    .AddDbContextCheck<PatientContext>("postgres-patient", tags: new[] { "db", "postgres" });

if (!string.IsNullOrEmpty(redisConnStr))
    healthBuilder.AddCacheHealthCheck("redis-patient");

if (!string.IsNullOrEmpty(esUrl))
    healthBuilder.AddElasticsearchHealthCheck("elasticsearch-patient");

if (!string.IsNullOrEmpty(mongoConnStr))
    healthBuilder.AddMongoHealthCheck("mongodb-patient");

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Migrations (runs pending EF Core migrations at startup) ──────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PatientContext>();
    await db.Database.EnsureCreatedAsync();
    Log.Information("Patient database schema verified/created");
}

// ── Pipeline ──────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

await app.RunAsync();

