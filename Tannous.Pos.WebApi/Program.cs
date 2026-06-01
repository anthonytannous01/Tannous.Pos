using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Tannous.Pos.Application.Behaviors;
using Tannous.Pos.Application.Sync;
using Tannous.Pos.Domain.Interfaces;
using Tannous.Pos.Infrastructure;
using Tannous.Pos.Infrastructure.Data;
using Tannous.Pos.Infrastructure.Repositories;
using Tannous.Pos.Infrastructure.Services;
using Tannous.Pos.Infrastructure.Services.Printing;
using Tannous.Pos.Infrastructure.Persistence.Seed;
using Tannous.Pos.WebApi.HealthChecks;
using Tannous.Pos.WebApi.Filters;
using Tannous.Pos.WebApi.Controllers;
using Tannous.Pos.WebApi.RateLimiting;
using Tannous.Pos.WebApi.Logging;
using Tannous.Pos.WebApi.Middleware;
using Tannous.Pos.WebApi.Extensions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Serilog;
using Serilog.Events;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
builder.Host.ConfigureSerilog();

// Add services to the container.
builder.Services.AddControllers(options =>
{
    options.Filters.Add<RequireDeviceIdFilter>();
});

// Add HttpContextAccessor for audit service
builder.Services.AddHttpContextAccessor();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// Configure API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Tannous POS API", Version = "v1.0" });
        
        // Fix schema ID conflicts (e.g., UserDto in both Auth and Users namespaces)
        c.CustomSchemaIds(type => type.FullName?.Replace("+", "."));
        
        // Configure JWT authentication for Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        // Add Device-Id and Idempotency-Key headers
        c.AddSecurityDefinition("Device-Id", new OpenApiSecurityScheme
        {
            Description = "Device identifier header. Required for all POS operations.",
            Name = "Device-Id",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey
        });

        c.AddSecurityDefinition("Idempotency-Key", new OpenApiSecurityScheme
        {
            Description = "Idempotency key for preventing duplicate operations. Required for POST/PUT requests.",
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey
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
                new string[] {}
            }
        });

        // Tag controllers for better organization
        c.TagActionsBy(api =>
        {
            if (api.GroupName != null)
            {
                return new[] { api.GroupName };
            }

            var controllerActionDescriptor = api.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
            if (controllerActionDescriptor != null)
            {
                return new[] { controllerActionDescriptor.ControllerName };
            }

            return new[] { api.RelativePath };
        });

        c.DocInclusionPredicate((name, api) => true);
    });

// Configure Entity Framework
builder.Services.AddSingleton<ByteaRowVersionSaveInterceptor>();
builder.Services.AddDbContext<PosDbContext>((serviceProvider, options) =>
{
    // Read connection string from environment variable first, then config
    var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING") 
                          ?? builder.Configuration.GetConnectionString("Default")
                          ?? throw new InvalidOperationException("Database connection string not configured");
    
    options.UseNpgsql(connectionString, npg =>
    {
        npg.MigrationsAssembly(typeof(PosDbContext).Assembly.FullName);
        npg.CommandTimeout(builder.Configuration.GetValue("Db:CommandTimeoutSeconds", 30));
        npg.EnableRetryOnFailure(
            maxRetryCount: builder.Configuration.GetValue("Db:Retry:MaxRetries", 5),
            maxRetryDelay: TimeSpan.FromSeconds(builder.Configuration.GetValue("Db:Retry:MaxDelaySeconds", 10)),
            errorCodesToAdd: null);
    });
    options.AddInterceptors(serviceProvider.GetRequiredService<ByteaRowVersionSaveInterceptor>());

    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors(builder.Configuration.GetValue("Db:EnableDetailedErrors", true));
        options.EnableSensitiveDataLogging(builder.Configuration.GetValue("Db:EnableSensitiveDataLogging", true));
    }
});

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Read JWT settings from environment variables first, then config
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") 
                    ?? builder.Configuration["Jwt:Key"] 
                    ?? throw new InvalidOperationException("JWT signing key not configured. Set JWT_KEY environment variable.");
        
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                       ?? builder.Configuration["Jwt:Issuer"] 
                       ?? "TannousPOS";
        
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                         ?? builder.Configuration["Jwt:Audience"] 
                         ?? "TannousPOS";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning(
                    "JWT authentication failed: {ErrorType} on {Path}",
                    context.Exception.GetType().Name,
                    context.Request.Path.Value);
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Add Tannous POS authorization policies
    options.AddPosAuthorizationPolicies();
    
    // Legacy policies for backward compatibility (will be removed after controller migration)
    options.AddPolicy("Owner", policy => policy.RequireRole("Owner"));
    options.AddPolicy("Cashier", policy => policy.RequireRole("Cashier", "Manager", "Owner"));
    options.AddPolicy("CashierOrOwner", policy => policy.RequireRole("Cashier", "Manager", "Owner"));
    options.AddPolicy("OwnerOnly", policy => policy.RequireRole("Owner"));
    options.AddPolicy("Admin", policy => policy.RequireRole("Owner"));
    options.AddPolicy("AdminOrManager", policy => policy.RequireRole("Owner", "Manager"));
});

// Configure MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Tannous.Pos.Application.Orders.Commands.CreateOrder.CreateOrderCommand).Assembly));

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(Tannous.Pos.Application.Orders.Commands.CreateOrder.CreateOrderCommand).Assembly);

// Configure FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Tannous.Pos.Application.Orders.Commands.CreateOrder.CreateOrderCommand).Assembly);

// Configure Pipeline Behaviors
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// Configure Repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IMenuItemRepository, MenuItemRepository>();
builder.Services.AddScoped<IAddOnRepository, AddOnRepository>();
builder.Services.AddScoped<IIngredientRepository, IngredientRepository>();
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<ISupplierRepository, SupplierRepository>();
builder.Services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
builder.Services.AddScoped<IGoodsReceiptRepository, GoodsReceiptRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IShiftRepository, ShiftRepository>();
builder.Services.AddScoped<ICashDrawerEventRepository, CashDrawerEventRepository>();
builder.Services.AddScoped<IBusinessSettingsRepository, BusinessSettingsRepository>();
builder.Services.AddScoped<Tannous.Pos.Application.Interfaces.IAdminDatabaseStatsRepository, AdminDatabaseStatsRepository>();
builder.Services.AddScoped<IAdminOrderOperationsRepository, AdminOrderOperationsRepository>();
builder.Services.AddScoped<IAdminPurgeRepository, AdminPurgeRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Configure Services
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IReceiptNumberService, ReceiptNumberService>();
builder.Services.AddScoped<IAuthService, JwtAuthService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<ISyncPullService, SyncPullService>();
builder.Services.AddScoped<IIdempotencyStore, IdempotencyStore>();
builder.Services.AddScoped<ISyncPushOperationExecutionScope, SyncPushOperationExecutionScope>();
builder.Services.AddScoped<IDurableSyncReplayCoordinator, DurableSyncReplayCoordinator>();
builder.Services.AddScoped<ISyncConflictRecorder, SyncConflictRecorder>();
builder.Services.AddScoped<Tannous.Pos.Application.Audit.IOperationalAuditRecorder, OperationalAuditRecorder>();
builder.Services.AddScoped<Tannous.Pos.Application.Audit.IOperationalAuditTimelineService, OperationalAuditTimelineService>();
builder.Services.AddScoped<Tannous.Pos.Application.Audit.IOperationalAuditQueryService, OperationalAuditQueryService>();
builder.Services.AddScoped<Tannous.Pos.Application.Sync.ISyncConflictReconciliationService, SyncConflictReconciliationService>();
builder.Services.AddScoped<Tannous.Pos.Application.Audit.IOperationalForensicSnapshotService, OperationalForensicSnapshotService>();
builder.Services.AddScoped<Tannous.Pos.Application.Audit.IOperationalRetentionSummaryService, OperationalRetentionSummaryService>();
builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = Tannous.Pos.Application.Audit.OperationalDiagnosticsCacheConstants.MaxCacheEntryCount;
});
builder.Services.AddSingleton<Tannous.Pos.Application.Audit.IOperationalAuditPersistenceTelemetry, OperationalAuditPersistenceTelemetry>();
builder.Services.AddSingleton<OperationalResiliencePressureState>();
builder.Services.AddSingleton<Tannous.Pos.Application.Audit.IOperationalResiliencePressureState>(sp =>
    sp.GetRequiredService<OperationalResiliencePressureState>());
builder.Services.AddSingleton<Tannous.Pos.Application.Audit.IOperationalResiliencePressureGovernanceReset>(sp =>
    sp.GetRequiredService<OperationalResiliencePressureState>());
builder.Services.AddSingleton<Tannous.Pos.Application.Audit.IOperationalPressureLifecycleTracker, OperationalPressureLifecycleTracker>();
builder.Services.AddOperationalDiagnosticsPressureResetCoordinator();
builder.Services.AddOperationalGovernanceSnapshotReuse();
builder.Services.AddSingleton<Tannous.Pos.Application.Audit.IOperationalDiagnosticsCacheTelemetry, OperationalDiagnosticsCacheTelemetry>();
builder.Services.AddSingleton<Tannous.Pos.Application.Audit.IOperationalDiagnosticsCache, OperationalDiagnosticsCacheService>();
builder.Services.AddSingleton<Tannous.Pos.Application.Audit.IOperationalDiagnosticsCacheInvalidator, OperationalDiagnosticsCacheInvalidator>();
builder.Services.AddScoped<Tannous.Pos.Application.Audit.IOperationalDiagnosticsCacheDiagnosticsService, OperationalDiagnosticsCacheDiagnosticsService>();
builder.Services.AddScoped<Tannous.Pos.Application.Audit.IOperationalResilienceDiagnosticsService, OperationalResilienceDiagnosticsService>();
builder.Services.AddScoped<Tannous.Pos.Application.Audit.IOperationalIncidentCorrelationService, OperationalIncidentCorrelationService>();
builder.Services.AddScoped<Tannous.Pos.Application.Audit.IOperationalAlertSignalService, OperationalAlertSignalService>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalComposition.IOperationalReadCompositionHub, Tannous.Pos.Infrastructure.Services.OperationalComposition.OperationalReadCompositionHub>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalDashboard.IOperationalDashboardService, OperationalDashboardService>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalWorkbench.IOperationalReconciliationWorkbenchService, OperationalReconciliationWorkbenchService>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalInventoryWorkbench.IOperationalInventoryWorkbenchService, OperationalInventoryWorkbenchService>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalReplayWorkbench.IOperationalReplayWorkbenchService, OperationalReplayWorkbenchService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalTrends.IOperationalTrendWindowStore, Tannous.Pos.Infrastructure.Services.OperationalTrends.OperationalTrendWindowStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalTrends.IOperationalTrendService, OperationalTrendService>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalNavigation.IOperationalNavigationService, OperationalNavigationService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalTimeline.IOperationalTimelineWindowStore, Tannous.Pos.Infrastructure.Services.OperationalTimeline.OperationalTimelineWindowStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalTimeline.IOperationalTimelineService, OperationalTimelineService>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalTriage.IOperationalTriageService, OperationalTriageService>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalRecovery.IOperationalRecoveryService, OperationalRecoveryService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalIncidents.IOperationalIncidentCaseStore, Tannous.Pos.Infrastructure.Services.OperationalIncidents.OperationalIncidentCaseStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalIncidents.IOperationalIncidentService, OperationalIncidentService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalCausality.IOperationalCausalitySnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalCausality.OperationalCausalitySnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalCausality.IOperationalCausalityService, OperationalCausalityService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalSituationRoom.IOperationalSituationSnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalSituationRoom.OperationalSituationSnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalSituationRoom.IOperationalSituationRoomService, OperationalSituationRoomService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalSimulation.IOperationalSimulationSnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalSimulation.OperationalSimulationSnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalSimulation.IOperationalSimulationService, OperationalSimulationService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalPlaybooks.IOperationalPlaybookSnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalPlaybooks.OperationalPlaybookSnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalPlaybooks.IOperationalPlaybookService, OperationalPlaybookService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalPatterns.IOperationalPatternSnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalPatterns.OperationalPatternSnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalPatterns.IOperationalPatternService, OperationalPatternService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalIntegrity.IOperationalIntegritySnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalIntegrity.OperationalIntegritySnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalIntegrity.IOperationalIntegrityService, OperationalIntegrityService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalExperienceGraph.IOperationalExperienceSnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalExperienceGraph.OperationalExperienceSnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalExperienceGraph.IOperationalExperienceGraphService, OperationalExperienceGraphService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalDigest.IOperationalDigestSnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalDigest.OperationalDigestSnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalDigest.IOperationalDigestService, OperationalDigestService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalEvolution.IOperationalEvolutionSnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalEvolution.OperationalEvolutionSnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalEvolution.IOperationalEvolutionService, OperationalEvolutionService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalTopology.IOperationalTopologySnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalTopology.OperationalTopologySnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalTopology.IOperationalTopologyService, OperationalTopologyService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalConvergence.IOperationalConvergenceSnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalConvergence.OperationalConvergenceSnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalConvergence.IOperationalConvergenceService, OperationalConvergenceService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalResilience.IOperationalResilienceCognitionSnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalResilience.OperationalResilienceCognitionSnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalResilience.IOperationalResilienceCognitionService, OperationalResilienceCognitionService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalAttention.IOperationalAttentionSnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalAttention.OperationalAttentionSnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalAttention.IOperationalAttentionService, OperationalAttentionService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalStrategy.IOperationalStrategySnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalStrategy.OperationalStrategySnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalStrategy.IOperationalStrategyService, OperationalStrategyService>();
builder.Services.AddSingleton<Tannous.Pos.Application.OperationalEquilibrium.IOperationalEquilibriumSnapshotStore, Tannous.Pos.Infrastructure.Services.OperationalEquilibrium.OperationalEquilibriumSnapshotStore>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalEquilibrium.IOperationalEquilibriumService, OperationalEquilibriumService>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalBriefing.IOperationalBriefingService, OperationalBriefingService>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalHandoff.IOperationalHandoffService, OperationalHandoffService>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalEntityStatus.IOperationalEntityStatusService, OperationalEntityStatusService>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalInvestigation.IOperationalInvestigationService, OperationalInvestigationService>();
builder.Services.AddScoped<Tannous.Pos.Application.OperationalReconciliation.IOperationalReconciliationSystemService, OperationalReconciliationSystemService>();
builder.Services.AddScoped<IDeviceValidator, DeviceValidator>();
builder.Services.AddScoped<Tannous.Pos.Application.Interfaces.IPrintingService, PrintingService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IETagService, ETagService>();
builder.Services.AddScoped<DevSeeder>();
builder.Services.AddScoped<ProdLikeSeeder>();

// Configure Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database");

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    // Auth endpoints: 5 requests per minute per IP
    options.AddFixedWindowLimiter("AuthBurst", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(1);
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });

    // Device-based rate limiting for mutations
    options.AddPolicy<string, DeviceIdRateLimiterPolicy>("MutationsPerDevice");
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("TannousPOS", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? 
                            new[] { "http://localhost:3000", "http://localhost:8080" };
        
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
    
    // Add policy for mobile app testing
    options.AddPolicy("MobileApp", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Tannous POS API V1");
        c.RoutePrefix = string.Empty; // Serve Swagger UI at root
    });
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

// Keep permissive CORS only for development/testing; restrict non-dev to configured origins.
app.UseCors(app.Environment.IsDevelopment() ? "MobileApp" : "TannousPOS");

// Add correlation ID middleware
app.UseMiddleware<CorrelationIdMiddleware>();

// Request logging: suppress routine noise from health probes; elevate failures
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, _, ex) =>
    {
        if (httpContext.Request.Path.StartsWithSegments("/health"))
            return LogEventLevel.Debug;

        return ex != null ? LogEventLevel.Error : LogEventLevel.Information;
    };
});

// Add log enrichment middleware
app.UseMiddleware<LogEnrichmentMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Configure rate limiting (integration host sets RateLimiting:DisableForIntegration=true)
if (!builder.Configuration.GetValue<bool>("RateLimiting:DisableForIntegration"))
    app.UseRateLimiter();

// Map health checks (root paths + versioned aliases for mobile BASE_URL .../api/v1.0/)
var liveHealthOptions = new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
};
var readyHealthOptions = new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = HealthCheckResponseWriter.WriteAsync
};

app.MapHealthChecks("/health/live", liveHealthOptions);
app.MapHealthChecks("/health/ready", readyHealthOptions);
app.MapHealthChecks("/api/v1.0/health/live", liveHealthOptions);
app.MapHealthChecks("/api/v1.0/health/ready", readyHealthOptions);

app.MapControllers();

// Database seeding - Development only, requires explicit environment variables
try
{
    using (var scope = app.Services.CreateScope())
    {
        var context = scope.ServiceProvider.GetRequiredService<PosDbContext>();
        
        // Seed admin user ONLY in Development environment
        if (app.Environment.IsDevelopment())
        {
            var devSeeder = scope.ServiceProvider.GetRequiredService<DevSeeder>();
            await devSeeder.SeedAdminUserAsync();
            
            // Seed production-like data if enabled
            var shouldSeedProdData = builder.Configuration.GetValue<bool>("Seed:RunOnceOnStartup", false);
            if (shouldSeedProdData)
            {
                var prodSeeder = scope.ServiceProvider.GetRequiredService<ProdLikeSeeder>();
                await prodSeeder.SeedAsync();
            }
        }
    }
}
catch (Exception ex)
{
    // Log but don't crash - seeding is optional
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Database seeding failed. Application will continue, but admin user may need to be created manually.");
}

app.Run();

public partial class Program { }
