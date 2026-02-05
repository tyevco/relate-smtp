using Microsoft.AspNetCore.Authentication.JwtBearer;
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
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Host=localhost;Port=5432;Database=relate_smtp;Username=postgres;Password=postgres";

builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<UserProvisioningService>();
builder.Services.AddScoped<SmtpCredentialService>();
builder.Services.AddScoped<EmailFilterService>();
builder.Services.AddScoped<IEmailNotificationService, SignalREmailNotificationService>();
builder.Services.AddScoped<PushNotificationService>();

// Configure push notification options
builder.Services.Configure<PushOptions>(builder.Configuration.GetSection("Push"));

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
    // Development mode - accept any token or allow anonymous
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                ValidateIssuerSigningKey = false,
                RequireSignedTokens = false
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
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddSignalR();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

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

app.UseHttpsRedirection();

// Serve static files (frontend)
app.UseStaticFiles();
app.UseDefaultFiles();

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<EmailHub>("/hubs/email");

// Fallback to index.html for SPA routing
app.MapFallbackToFile("index.html");

app.Run();
