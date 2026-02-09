using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Relate.Smtp.Api.Authentication;
using Relate.Smtp.Api.Hubs;
using Relate.Smtp.Api.Services;
using Relate.Smtp.Infrastructure;
using Relate.Smtp.Infrastructure.Data;
using Relate.Smtp.Infrastructure.Services;
using Relate.Smtp.Infrastructure.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    if (builder.Environment.IsProduction())
    {
        throw new InvalidOperationException(
            "ConnectionStrings:DefaultConnection is required. Set via environment variable ConnectionStrings__DefaultConnection.");
    }
    // Development fallback - log warning
    connectionString = "Host=localhost;Port=5432;Database=relate_mail;Username=postgres;Password=postgres";
    Console.WriteLine("WARNING: Using default development database connection. Set ConnectionStrings__DefaultConnection for production.");
}

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<UserProvisioningService>();
builder.Services.AddScoped<SmtpCredentialService>();
builder.Services.AddScoped<EmailFilterService>();
builder.Services.AddScoped<IEmailNotificationService, SignalREmailNotificationService>();
builder.Services.AddScoped<PushNotificationService>();

// Configure push notification options
builder.Services.Configure<PushOptions>(builder.Configuration.GetSection("Push"));

// Configure outbound mail delivery
builder.Services.Configure<OutboundMailOptions>(builder.Configuration.GetSection(OutboundMailOptions.SectionName));
builder.Services.AddScoped<IDeliveryNotificationService, SignalRDeliveryNotificationService>();

// Configure OIDC/JWT authentication
var oidcAuthority = builder.Configuration["Oidc:Authority"];
var oidcAudience = builder.Configuration["Oidc:Audience"];

if (!string.IsNullOrEmpty(oidcAuthority))
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = oidcAuthority;
            options.Audience = oidcAudience;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = !string.IsNullOrEmpty(oidcAudience),
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };
        })
        .AddApiKeyAuthentication();
}
else
{
    // Development mode - use symmetric key for local testing
    var devKey = builder.Configuration["Jwt:DevelopmentKey"];
    if (string.IsNullOrEmpty(devKey))
    {
        // Generate a stable development key based on machine name
        devKey = $"dev-key-{Environment.MachineName}-relate-mail-development-only-key-32chars!";
        Console.WriteLine("WARNING: Using auto-generated development JWT key. Set Jwt:DevelopmentKey for consistent tokens across restarts.");
    }

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "relate-mail-dev",
                ValidateAudience = true,
                ValidAudience = "relate-mail",
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(devKey)),
                RequireSignedTokens = true
            };
        })
        .AddApiKeyAuthentication();
}

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder(
        JwtBearerDefaults.AuthenticationScheme,
        ApiKeyAuthenticationExtensions.ApiKeyScheme)
        .RequireAuthenticatedUser()
        .Build();
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];

        policy.WithOrigins(allowedOrigins)
            .WithHeaders("Authorization", "Content-Type", "Accept", "X-Requested-With", "X-SignalR-User-Agent")
            .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
            .AllowCredentials();
    });
});

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add rate limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Global policy for general API endpoints
    options.AddFixedWindowLimiter("api", config =>
    {
        config.PermitLimit = 100;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 10;
    });

    // Strict policy for auth-related endpoints
    options.AddFixedWindowLimiter("auth", config =>
    {
        config.PermitLimit = 10;
        config.Window = TimeSpan.FromMinutes(1);
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 2;
    });

    // Per-user policy for write operations
    options.AddSlidingWindowLimiter("write", config =>
    {
        config.PermitLimit = 30;
        config.Window = TimeSpan.FromMinutes(1);
        config.SegmentsPerWindow = 6;
        config.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        config.QueueLimit = 5;
    });
});

// Add OpenTelemetry
builder.Services.AddRelateTelemetry(
    builder.Configuration,
    "relate-mail-api",
    configureTracing: tracing =>
    {
        tracing.AddAspNetCoreInstrumentation(options =>
        {
            options.RecordException = true;
        });
        tracing.AddHttpClientInstrumentation();
    },
    configureMetrics: metrics =>
    {
        metrics.AddAspNetCoreInstrumentation();
        metrics.AddHttpClientInstrumentation();
    });

var app = builder.Build();

// Auto-migrate in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Exception handling middleware - maps domain exceptions to HTTP status codes
app.UseExceptionHandler(exceptionHandlerApp =>
{
    exceptionHandlerApp.Run(async context =>
    {
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var exception = exceptionFeature?.Error;

        if (exception is UnauthorizedAccessException)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new { error = exception.Message });
            return;
        }

        // For other exceptions, use default behavior
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred" });
    });
});

app.UseHttpsRedirection();

// Security headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

    // CSP: Allow self for scripts/styles, data: for inline images, and configured external sources
    if (!context.Request.Path.StartsWithSegments("/api"))
    {
        context.Response.Headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "img-src 'self' data: blob:; " +
            "font-src 'self'; " +
            "connect-src 'self' wss: ws:; " +
            "frame-ancestors 'none';";
    }

    await next();
});

// Serve static files (frontend)
app.UseStaticFiles();
app.UseDefaultFiles();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHub<EmailHub>("/hubs/email");

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();
