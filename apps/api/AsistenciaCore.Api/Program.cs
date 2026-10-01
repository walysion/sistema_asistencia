using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using AsistenciaCore.Api.Data;
using AsistenciaCore.Api.Hubs;
using AsistenciaCore.Api.Middlewares;
using AsistenciaCore.Api.Services;

// Configurar Serilog como Logger principal antes de iniciar el Host
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/asistenciacore-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Iniciando la API de Asistencia SaaS...");

    var builder = WebApplication.CreateBuilder(args);

    // Conectar Serilog con la infraestructura de Microsoft.Extensions.Logging
    builder.Host.UseSerilog();

    // 1. Configurar CORS (Soporte completo para WebSockets con SignalR)
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    // 2. Configurar Rate Limiting (Protección contra DDoS: 100 req/min por IP)
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    AutoReplenishment = true,
                    PermitLimit = 100,
                    QueueLimit = 0,
                    Window = TimeSpan.FromMinutes(1)
                }));
    });

    // 3. Agregar Controladores y SignalR
    builder.Services.AddControllers();
    builder.Services.AddSignalR();

    // 4. Configurar PostgreSQL
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // 5. Configurar Health Checks
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("Database");

    // 6. Configurar Caché Distribuido con Redis
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
        options.InstanceName = "AsistenciaCore_";
    });

    // 7. Configurar Autenticación con JWT (Soporte para Token vía Query String en WebSockets)
    var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "ClaveSuperSecretaSaaSAsistencia2026_UltraSecureKey!";
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "AsistenciaCore",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "AsistenciaClients",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/asistencia"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

    // 8. Configurar Swagger
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "AsistenciaCore API",
            Version = "v1",
            Description = "API REST de Asistencia SaaS con SignalR WebSockets, JWT y Redis"
        });

        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Ingresa el token JWT en formato: Bearer {tu_token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // 9. Registrar Servicio en Segundo Plano
    builder.Services.AddHostedService<ConsolidadorAsistenciaWorker>();

    var app = builder.Build();

    // 10. Registrar Middleware de registro de peticiones HTTP con Serilog
    app.UseSerilogRequestLogging();

    // 11. Ejecutar Migraciones y Semillero
    await DbInitializer.InitializeAsync(app.Services);

    // 12. Registrar Middleware Global de Excepciones
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // 13. Activar CORS y Rate Limiting
    app.UseCors("AllowAll");
    app.UseRateLimiter();

    // 14. Mapear Endpoints HTTP, HealthCheck y SignalR Hub
    app.MapHealthChecks("/health");
    app.MapControllers();
    app.MapHub<AsistenciaHub>("/hubs/asistencia");

    // 15. Pipeline de Swagger
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger(c =>
        {
            c.SerializeAsV2 = true;
        });

        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "AsistenciaCore API v1");
            c.RoutePrefix = "swagger";
        });
    }

    app.UseAuthentication();
    app.UseAuthorization();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación falló críticamente al iniciar.");
}
finally
{
    Log.CloseAndFlush();
}