using EHRPlatform.Common.Data;
using EHRPlatform.Common.Extensions;
using EHRPlatform.Common.Health;
using EHRPlatform.Common.Search;
using EHRPlatform.Common.Security;
using EHRPlatform.Services.Identity.Application.Identity.Extensions;
using EHRPlatform.Services.Identity.Data;
using EHRPlatform.Services.Identity.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;

// Bootstrap logger to capture startup errors
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ── Serilog ───────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, cfg) =>
        cfg.MinimumLevel.Information()
           .WriteTo.Console()
           .Enrich.FromLogContext());

    // ── Controllers & Swagger ─────────────────────────────────────────────────
    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "EHR Identity Service", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "Enter your JWT token"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id   = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // ── Database (PostgreSQL) ─────────────────────────────────────────────────
    var connectionString = builder.Configuration.BuildPostgresConnectionString();
    builder.Services.AddPostgresDataAccess<IdentityContext>(connectionString);

    // ── CQRS: handlers, validators, mappers ──────────────────────────────────
    builder.Services.AddIdentityServices();

    // ── Security ─────────────────────────────────────────────────────────────
    var encryptionKey = builder.Configuration["Security:EncryptionKey"]
        ?? Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
        ?? throw new InvalidOperationException("ENCRYPTION_KEY secret is required.");

    builder.Services.AddSingleton<IEncryptionService>(new EncryptionService(encryptionKey));
    builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();

    var jwtSecret = builder.Configuration["Jwt:Secret"]
        ?? Environment.GetEnvironmentVariable("JWT_SECRET")
        ?? throw new InvalidOperationException("JWT_SECRET secret is required.");

    var jwtIssuer   = builder.Configuration["Jwt:Issuer"]   ?? "ehr-platform";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ehr-api";
    var jwtExpMin   = int.TryParse(builder.Configuration["Jwt:ExpirationMinutes"], out var m) ? m : 60;

    builder.Services.AddSingleton<IJwtTokenService>(
        new JwtTokenService(jwtSecret, jwtIssuer, jwtAudience, jwtExpMin));
    builder.Services.AddJwtAuthentication(jwtSecret, jwtIssuer, jwtAudience);

    // ── Redis Caching (optional — degrades gracefully if unavailable) ─────────
    var redisConnStr = builder.Configuration["Redis:ConnectionString"]
        ?? Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING");

    if (!string.IsNullOrEmpty(redisConnStr))
    {
        try
        {
            builder.Services.AddRedisCaching(redisConnStr);
            Log.Information("Redis caching enabled");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Redis not available — caching disabled");
        }
    }
    else
    {
        Log.Warning("Redis:ConnectionString not configured — caching disabled");
    }

    // ── Elasticsearch (optional — degrades gracefully if unavailable) ─────────
    var esUrl = builder.Configuration["Elasticsearch:Url"]
        ?? Environment.GetEnvironmentVariable("ELASTICSEARCH_URL");

    if (!string.IsNullOrEmpty(esUrl))
    {
        try
        {
            builder.Services.AddElasticsearchSearch(esUrl);
            Log.Information("Elasticsearch enabled at {Url}", esUrl);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Elasticsearch not available — search disabled");
        }
    }
    else
    {
        Log.Warning("Elasticsearch:Url not configured — search disabled");
    }

    // ── CORS ─────────────────────────────────────────────────────────────────
    builder.Services.AddCors(options =>
        options.AddPolicy("AllowAll", p =>
            p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

    // ── Health checks ─────────────────────────────────────────────────────────
    var healthBuilder = builder.Services.AddHealthChecks()
        .AddDbContextCheck<IdentityContext>("postgres-identity", tags: new[] { "db", "postgres" });

    // Redis health check — only when Redis is wired up
    if (!string.IsNullOrEmpty(redisConnStr))
        healthBuilder.AddCacheHealthCheck("redis-identity");

    // Elasticsearch health check — only when ES is wired up
    if (!string.IsNullOrEmpty(esUrl))
        healthBuilder.AddElasticsearchHealthCheck("elasticsearch-identity");

    // ── Kestrel: listen on port 5000 (Replit preview port) ───────────────────
    builder.WebHost.UseUrls("http://0.0.0.0:5000");

    // ── Build ─────────────────────────────────────────────────────────────────
    var app = builder.Build();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "EHR Identity Service v1");
        c.RoutePrefix = string.Empty; // serve Swagger UI at root "/"
    });

    app.UseSerilogRequestLogging();
    app.UseCors("AllowAll");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");

    // ── Auto-create / migrate schema on first run ─────────────────────────────
    // EnsureCreatedAsync: fast for development; switch to MigrateAsync once
    // you generate the first EF migration with:
    //   dotnet ef migrations add Initial --project src/EHRPlatform.Services.Identity
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<IdentityContext>();
        await db.Database.EnsureCreatedAsync();
        Log.Information("Identity database schema verified/created");
    }

    Log.Information("EHR Identity Service starting on http://0.0.0.0:5000");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Identity Service terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

