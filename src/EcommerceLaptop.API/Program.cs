using Serilog;
using EcommerceLaptop.Infrastructure.Middleware; // Correct namespace
using EcommerceLaptop.API.Extensions;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Services.Security;
using Microsoft.Extensions.Caching.Distributed;
using System.Threading.RateLimiting;
using Hangfire;
using EcommerceLaptop.API.Authorization;
using EcommerceLaptop.API.Hubs; // Added
using OpenTelemetry.Metrics;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);

// Ensure legacy code page encodings (required by iText for Identity-H, etc.)
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

// Add Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog();

// -- OpenTelemetry Configuration --
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddAspNetCoreInstrumentation()
               .AddMeter("EcommerceLaptop.AI") // Our Custom Meter
               .AddPrometheusExporter();
    });
// ---------------------------------

// Add services to the container using Extension Methods
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddIdentityServices(builder.Configuration);
builder.Services.AddExternalServices(builder.Configuration);
builder.Services.AddSecurityServices(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddSwaggerDocumentation();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    });

builder.Services.AddSignalR();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure CORS policy - allow all origins
var corsPolicy = "AllowAll";
Log.Information("CORS CONFIGURATION: Using {Policy}", corsPolicy);

// Enable WebSocket support for SignalR
app.UseWebSockets();

// Ensure routing is set up before applying CORS
app.UseRouting();

app.UseCors(corsPolicy);
// Trigger restart for permission fix
// Serve static files from wwwroot (CSS, JS, favicon, etc.)
// All image uploads now handled by ImgBB cloud service
app.UseStaticFiles();

// 4. Global Exception Handler (Replaces custom middleware)
app.UseExceptionHandler();

// Security Headers Middleware
app.Use(async (context, next) =>
{
    // Add security headers for all responses
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["X-Frame-Options"] = "DENY";
    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=()";

    // Only add HSTS in production
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }

    await next.Invoke();
});

app.UseSerilogRequestLogging();

//  DEBUG: Add request tracing middleware to track all incoming requests
app.Use(async (context, next) =>
{
    var timestamp = DateTime.UtcNow.ToString("HH:mm:ss.fff");
    var method = context.Request.Method;
    var path = context.Request.Path;

    Console.WriteLine($" [REQUEST-TRACE] {timestamp} {method} {path} - INCOMING");

    // Also log DELETE requests specifically
    if (method == "DELETE")
    {
        Console.WriteLine($" [DELETE-TRACE] {timestamp} DELETE {path} - Headers: {string.Join(", ", context.Request.Headers.Select(h => $"{h.Key}={h.Value.FirstOrDefault()}"))}");
    }

    // Log POST request bodies for debugging
    if (method == "POST" && path.ToString().Contains("rate-limit-rules"))
    {
        try
        {
            context.Request.EnableBuffering(); // Allow reading the body multiple times
            using var reader = new StreamReader(context.Request.Body, System.Text.Encoding.UTF8, leaveOpen: true);
            var body = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0; // Reset position for model binding

            Console.WriteLine($" [POST-BODY] {timestamp} POST {path} - ContentType: {context.Request.ContentType}");
            Console.WriteLine($" [POST-BODY] {timestamp} POST {path} - Body: {body}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($" [POST-BODY-ERROR] {timestamp} POST {path} - Error reading body: {ex.Message}");
        }
    }

    await next.Invoke();

    Console.WriteLine($" [REQUEST-TRACE] {timestamp} {method} {path} - COMPLETED with {context.Response.StatusCode}");
});

// Security Middleware Pipeline - Order is critical
// 1. IP Blocking (first line of defense)
app.UseIPBlocking();



// 3. .NET Built-in Rate Limiting (fallback for basic protection)
// Note: This provides basic rate limiting as fallback, but our custom middleware takes precedence
app.UseRateLimiter();

app.UseSession(); // Enable session middleware for guest cart management
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery(); // CSRF Protection
app.UseMiddleware<StructuredLoggingMiddleware>();

app.MapHub<ChatHub>("/chatHub"); // Map ChatHub
app.MapControllers();

// CSRF Token endpoint for SPA frontend
app.MapGet("/api/auth/csrf-token", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new { token = tokens.RequestToken });
}).AllowAnonymous();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Global 404 logger: capture NotFound responses as high-severity security events
app.Use(async (context, next) =>
{
    await next();
    if (context.Response.StatusCode == StatusCodes.Status404NotFound)
    {
        try
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
            var ua = context.Request.Headers["User-Agent"].ToString();
            var endpoint = context.Request.Path.ToString();
            var method = context.Request.Method;
            var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;

            using var scope = app.Services.CreateScope();
            var securityService = scope.ServiceProvider.GetRequiredService<ISecurityEventService>();

            var metadata = new Dictionary<string, object>
            {
                ["endpoint"] = endpoint,
                ["method"] = method,
                ["query"] = query ?? string.Empty,
                ["headers"] = context.Request.Headers.ToDictionary(h => h.Key, h => (object?)h.Value.ToString() ?? string.Empty),
                ["statusCode"] = 404,
                ["severity"] = "high"
            };

            await securityService.LogEventAsync(
                eventType: "http_404_not_found",
                description: $"404 NotFound at {endpoint}",
                userId: null,
                adminUserId: null,
                ipAddress: ip,
                userAgent: ua,
                correlationId: null,
                metadata: metadata
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[404-SECURITY-LOG] Failed to log 404 event: {ex.Message}");
        }
    }
});

// Initialize database and seed data via initializer
EcommerceLaptop.API.Seed.DataInitializer.Initialize(app.Services);

// Clean Slate: Ensure default admin exists after database initialization
using (var scope = app.Services.CreateScope())
{
    var adminSetupService = scope.ServiceProvider.GetRequiredService<EcommerceLaptop.Core.Services.IAdminSetupService>();
    await adminSetupService.EnsureDefaultAdminExistsAsync();
}

Log.Information("Ca Hng Laptop API started");

app.MapPrometheusScrapingEndpoint(); // /metrics

app.Run();

// Make Program class accessible for testing
public partial class Program { }
