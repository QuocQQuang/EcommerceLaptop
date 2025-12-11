using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using System.Text;
using Serilog;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Infrastructure.Services;
using EcommerceLaptop.Infrastructure.Services.Security;
using EcommerceLaptop.Infrastructure.Middleware;
using EcommerceLaptop.API.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Ensure legacy code page encodings (required by iText for Identity-H, etc.)
System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

// Add Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", Serilog.Events.LogEventLevel.Error)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .Filter.ByExcluding(logEvent => logEvent.Properties.ContainsKey("SourceContext") &&
                        logEvent.Properties["SourceContext"].ToString().Contains("Microsoft.EntityFrameworkCore.Database.Command"))
    .WriteTo.Console()
    .WriteTo.File("bin/logs/app-.log", rollingInterval: RollingInterval.Day, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("EcommerceLaptop.Infrastructure")));

// JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
    // Diagnostic events and cookie fallback for admin session stored in cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            try
            {
                // If Authorization header is missing, try to read from admin-session cookie (frontend uses this)
                if (string.IsNullOrEmpty(context.Request.Headers["Authorization"]))
                {
                    var cookieToken = context.Request.Cookies["admin-session"];
                    if (!string.IsNullOrEmpty(cookieToken))
                    {
                        context.Token = cookieToken;
                    }
                }
            }
            catch (Exception ex)
            {
                // Log to console and allow pipeline to continue for diagnostics
                Console.WriteLine($"[JwtBearer] OnMessageReceived error: {ex}");
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[JwtBearer] Authentication failed: {context.Exception?.Message}");
            return Task.CompletedTask;
        }
    };
});

// Authorization
builder.Services.AddAuthorization(options =>
{
    // Admin only policy
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("is_admin", "true"));

    // AdminPolicy for AdminPermissionsController compatibility
    options.AddPolicy("AdminPolicy", policy =>
        policy.RequireAuthenticatedUser()
              .RequireClaim("is_admin", "true"));
});

// Register dynamic authorization policy provider for admin permissions
builder.Services.AddSingleton<IAuthorizationPolicyProvider, EcommerceLaptop.API.Authorization.AdminPermissionPolicyProvider>();

// Register authorization handlers  
builder.Services.AddScoped<IAuthorizationHandler, EcommerceLaptop.API.Authorization.AdminPermissionHandler>();

// Redis cache configuration
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Session state configuration for guest cart management
builder.Services.AddDistributedMemoryCache(); // For development, use Redis in production
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromDays(30); // Session timeout for guest carts
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".EcommerceLaptop.Session";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add Memory Cache for in-memory caching (used in controllers)
builder.Services.AddMemoryCache();

// Add HttpContextAccessor for security logging
builder.Services.AddHttpContextAccessor();

// Elasticsearch configuration
builder.Services.Configure<EcommerceLaptop.Core.Configuration.ElasticsearchSettings>(
    builder.Configuration.GetSection("Elasticsearch"));

builder.Services.AddSingleton<Nest.IElasticClient>(provider =>
{
    var settings = builder.Configuration.GetSection("Elasticsearch")
        .Get<EcommerceLaptop.Core.Configuration.ElasticsearchSettings>()
        ?? new EcommerceLaptop.Core.Configuration.ElasticsearchSettings();

    var connectionSettings = new Nest.ConnectionSettings(new Uri(settings.Uri))
        .DefaultIndex(settings.IndexName)
        .RequestTimeout(settings.RequestTimeout)
        .MaxRetryTimeout(settings.MaxRetryTimeout);

    if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
    {
        connectionSettings.BasicAuthentication(settings.Username, settings.Password);
    }

    if (settings.EnableDebugMode)
    {
        connectionSettings.EnableDebugMode();
    }

    return new Nest.ElasticClient(connectionSettings);
});

// Authentication Services
// Phase 4: Legacy token services removed - using unified authentication only  
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IUserService, EcommerceLaptop.Infrastructure.Services.UserService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IPasswordResetService, EcommerceLaptop.Infrastructure.Services.PasswordResetService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IAdminDashboardService, EcommerceLaptop.Infrastructure.Services.AdminDashboardService>();
builder.Services.AddScoped<ISystemSettingsService, SystemSettingsService>();

// Phase 2: Unified Authentication Services  
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IAuthService, EcommerceLaptop.Infrastructure.Services.AuthService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.ITokenService, EcommerceLaptop.Infrastructure.Services.TokenService>();

// Clean Slate: Admin Setup Service for unified system
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IAdminSetupService, EcommerceLaptop.Infrastructure.Services.AdminSetupService>();

// Security Services - IP Blocking, Rate Limiting, Audit Logging
builder.Services.AddScoped<IIPBlockingService, IPBlockingService>();
builder.Services.AddScoped<IRateLimitingService, RateLimitingService>();
builder.Services.AddScoped<ISecurityEventService, SecurityEventService>();
builder.Services.AddScoped<IAuditLoggingService, AuditLoggingService>();

// Legacy Authentication Wrappers removed - now using unified IAuthService directly

// Phase 3: Feature Flag Service
builder.Services.AddSingleton<EcommerceLaptop.API.Services.IFeatureFlagService, EcommerceLaptop.API.Services.FeatureFlagService>();

// Business Services
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IProductService, EcommerceLaptop.Infrastructure.Services.ProductService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Interfaces.Services.ICategoryService, EcommerceLaptop.Infrastructure.Services.CategoryService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Interfaces.Services.IBrandService, EcommerceLaptop.Infrastructure.Services.BrandService>();

// Customer Management Services
builder.Services.AddScoped<ICustomerManagementService, CustomerManagementService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IWishlistService, EcommerceLaptop.Infrastructure.Services.WishlistService>();
// NOTE: IFileUploadService removed - all uploads now use IImageHostingService (ImgBB)

// Email Service Configuration
builder.Services.Configure<EcommerceLaptop.Core.Configuration.EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IEmailService, EcommerceLaptop.Infrastructure.Services.GmailEmailService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IEmailQueueService, EcommerceLaptop.Infrastructure.Services.EmailQueueService>();

// Email Security Services
builder.Services.AddDataProtection()
    .SetApplicationName("EcommerceLaptop");
// TODO: Add interfaces for EmailSecurityService and GmailOAuth2Service to Core.Services
builder.Services.AddScoped<EcommerceLaptop.Infrastructure.Services.IEmailSecurityService, EcommerceLaptop.Infrastructure.Services.EmailSecurityService>();
builder.Services.AddScoped<EcommerceLaptop.Infrastructure.Services.IGmailOAuth2Service, EcommerceLaptop.Infrastructure.Services.GmailOAuth2Service>();

// Background Services
builder.Services.AddHostedService<EcommerceLaptop.Infrastructure.Services.EmailBackgroundService>();

// ImgBB Service for Image Upload
builder.Services.Configure<EcommerceLaptop.Core.Configuration.ImgBBSettings>(
    builder.Configuration.GetSection("ImgBB"));
builder.Services.AddHttpClient<EcommerceLaptop.Infrastructure.Services.ImgBBService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IImageHostingService, EcommerceLaptop.Infrastructure.Services.ImgBBService>();

// T012: Inventory Management Services
builder.Services.AddScoped(typeof(EcommerceLaptop.Core.Interfaces.IAsyncRepository<>), typeof(EcommerceLaptop.Infrastructure.Repositories.EfRepository<>));
builder.Services.AddScoped<EcommerceLaptop.Core.Interfaces.IProductRepository, EcommerceLaptop.Infrastructure.Repositories.ProductRepository>();
builder.Services.AddScoped<EcommerceLaptop.Core.Interfaces.IInventoryRepository, EcommerceLaptop.Infrastructure.Repositories.InventoryRepository>();

builder.Services.AddScoped<EcommerceLaptop.Core.Services.IInventoryService, EcommerceLaptop.Core.Services.InventoryService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IInventoryReservationService, EcommerceLaptop.Infrastructure.Services.InventoryReservationService>();

// T007: Shopping Cart Services
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IShoppingCartService, EcommerceLaptop.Infrastructure.Services.ShoppingCartService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IPricingService, EcommerceLaptop.Infrastructure.Services.PricingService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.ICartValidationService, EcommerceLaptop.Infrastructure.Services.CartValidationService>();

// Order Management Services
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IOrderService, EcommerceLaptop.Infrastructure.Services.OrderService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IOrderWorkflowService, EcommerceLaptop.Infrastructure.Services.OrderWorkflowService>();

// T006: Advanced Search Services
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IProductSearchService, EcommerceLaptop.Infrastructure.Services.ProductSearchService>();

// Blog Services
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IBlogService, EcommerceLaptop.Infrastructure.Services.BlogService>();

// Category Services
// TODO: Fix CategoryService namespace issues
// builder.Services.AddScoped<EcommerceLaptop.Core.Interfaces.Services.ICategoryService, EcommerceLaptop.Infrastructure.Services.CategoryService>();

// Admin Management Services
builder.Services.AddScoped<EcommerceLaptop.Core.Interfaces.Services.IAdminRoleService, EcommerceLaptop.Infrastructure.Services.AdminRoleService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Interfaces.Services.IAdminUserService, EcommerceLaptop.Infrastructure.Services.AdminUserService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Interfaces.Services.IDevService, EcommerceLaptop.Infrastructure.Services.DevService>();
// builder.Services.AddScoped<EcommerceLaptop.Core.Interfaces.Services.IBrandService, EcommerceLaptop.Infrastructure.Services.BrandService>();
// builder.Services.AddScoped<EcommerceLaptop.Core.Interfaces.Services.ISystemSettingsService, EcommerceLaptop.Infrastructure.Services.SystemSettingsService>();
// TODO: Fix UserVipTierService namespace issues
// builder.Services.AddScoped<EcommerceLaptop.Core.Services.IUserVipTierService, EcommerceLaptop.Infrastructure.Services.SimpleUserVipTierService>();
// TODO: Fix RefundService namespace issues  
// builder.Services.AddScoped<EcommerceLaptop.Core.Services.IRefundService, EcommerceLaptop.Infrastructure.Services.RefundService>();

// T010: Payment Gateway Services
builder.Services.Configure<EcommerceLaptop.Core.Configuration.PaymentGatewaySettings>(
    builder.Configuration.GetSection("PaymentGateways"));

// Add HttpClient for payment gateway services
builder.Services.AddHttpClient();

// Payment Service Registrations
builder.Services.AddScoped<EcommerceLaptop.Core.Services.Payment.IPaymentOrchestrator, EcommerceLaptop.Infrastructure.Services.Payment.PaymentOrchestrator>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.Payment.IPaymentServiceFactory, EcommerceLaptop.Infrastructure.Services.Payment.PaymentServiceFactory>();

// Gateway-specific services
builder.Services.AddScoped<EcommerceLaptop.Core.Services.Payment.IVnPayService, EcommerceLaptop.Infrastructure.Services.Payment.VnPayService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.Payment.IMoMoService, EcommerceLaptop.Infrastructure.Services.Payment.MoMoService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.Payment.IPayPalService, EcommerceLaptop.Infrastructure.Services.Payment.PayPalService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.Payment.IZaloPayService, EcommerceLaptop.Infrastructure.Services.Payment.ZaloPayService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.Payment.IStripeService, EcommerceLaptop.Infrastructure.Services.Payment.StripeService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.Payment.ISePayService, EcommerceLaptop.Infrastructure.Services.Payment.SePayService>();

// SePay Gateway Adapter (wraps ISePayService to implement IPaymentGatewayService)
builder.Services.AddScoped<EcommerceLaptop.Infrastructure.Services.Payment.SePayGatewayAdapter>();

// PayPal webhook verification service
builder.Services.AddScoped<EcommerceLaptop.Infrastructure.Services.Payment.PayPalWebhookVerificationService>();

// Stripe webhook verification service
builder.Services.AddScoped<EcommerceLaptop.Infrastructure.Services.Payment.StripeWebhookVerificationService>();

// Payment webhook business logic service - Temporarily commented due to interface issues
builder.Services.AddScoped<EcommerceLaptop.Core.Services.Payment.IPaymentWebhookBusinessLogicService, EcommerceLaptop.Infrastructure.Services.Payment.PaymentWebhookBusinessLogicService>();

// Payment configuration validation
builder.Services.AddScoped<FluentValidation.IValidator<EcommerceLaptop.Core.Configuration.PaymentGatewaySettings>, EcommerceLaptop.Core.Validators.PaymentGatewaySettingsValidator>();

// Export Services
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IPdfExportService, EcommerceLaptop.Infrastructure.Services.PdfExportService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IExcelExportService, EcommerceLaptop.Infrastructure.Services.ExcelExportService>();
builder.Services.AddScoped<EcommerceLaptop.Core.Services.IReportingService, EcommerceLaptop.Infrastructure.Services.ReportingService>();

// Background Services
builder.Services.AddHostedService<EcommerceLaptop.Infrastructure.Services.ProductIndexingService>();

// AutoMapper Registration
builder.Services.AddAutoMapper(typeof(Program));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
        // Configure camelCase naming policy for frontend compatibility
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        // Add string enum converter to accept enum values as strings with camelCase naming
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter(System.Text.Json.JsonNamingPolicy.CamelCase));
    });

// Fallback Rate Limiting Registration (.NET 8 built-in - only for specific endpoints)
// Note: Our custom AdvancedRateLimitingMiddleware handles most rate limiting
// This serves as a fallback for critical endpoints only
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.OnRejected = async (context, cancellationToken) =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        var clientIp = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var endpoint = context.HttpContext.Request.Path;
        var userAgent = context.HttpContext.Request.Headers["User-Agent"].ToString();

        logger.LogWarning("Fallback rate limit exceeded - Client IP: {ClientIp}, Endpoint: {Endpoint}, User-Agent: {UserAgent}, Time: {Time}",
            clientIp, endpoint, userAgent, DateTime.UtcNow);

        context.HttpContext.Response.Headers.Append("Retry-After", "60");
        context.HttpContext.Response.Headers.Append("X-RateLimit-Reason", "Fallback rate limit exceeded");
        context.HttpContext.Response.Headers.Append("X-RateLimit-Source", "Built-in-Fallback");
        await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", cancellationToken);
    };

    // Only apply to specific critical endpoints that need extra protection
    // Most endpoints are handled by our custom AdvancedRateLimitingMiddleware

    // Password change rate limiting policy - 5 attempts per 15 minutes per user
    options.AddPolicy("PasswordChangePolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User?.Identity?.Name ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(15),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    // Admin operations policy - more permissive for authenticated admin users
    options.AddPolicy("AdminAuthPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100, // More permissive for admin operations
                Window = TimeSpan.FromMinutes(1), // Shorter window for faster reset
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 10 // Allow some queuing for burst requests
            }));
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "Ca Hng Laptop API", Version = "v1" });

    // Add JWT authentication to Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "https://localhost:3000", "http://localhost:3001", "https://localhost:3001", "http://localhost:3002", "https://localhost:3002")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    options.AddPolicy("Production", policy =>
    {
        policy.WithOrigins(
                "https://ecommerce-laptop-frontend.vercel.app",
                "https://ecommerce-laptop-frontend-git-*.vercel.app",
                "https://a33lprojecct.id.vn"  // Frontend custom domain nu c
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true);  // Cho wildcard Vercel preview URLs
    });

    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000", "https://localhost:3000",
                "http://localhost:3001", "https://localhost:3001",
                "http://localhost:3002", "https://localhost:3002",
                "http://127.0.0.1:3000", "https://127.0.0.1:3000",
                "https://ecommerce-laptop-frontend.vercel.app",
                "https://ecommerce-laptop-frontend-git-*.vercel.app",
                "https://a33lprojecct.id.vn"
              )
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetIsOriginAllowed(origin => origin.StartsWith("https://ecommerce-laptop-frontend") ||
                                           origin.StartsWith("http://localhost:") ||
                                           origin.StartsWith("https://localhost:") ||
                                           origin.StartsWith("http://127.0.0.1:") ||
                                           origin.StartsWith("https://127.0.0.1:"));
    });
});

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

// Ensure routing is set up before applying CORS
app.UseRouting();

app.UseCors(corsPolicy);
// Trigger restart for permission fix
// Serve static files from wwwroot (CSS, JS, favicon, etc.)
// All image uploads now handled by ImgBB cloud service
app.UseStaticFiles();

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

// 2. Advanced Rate Limiting (custom rules from database)
app.UseAdvancedRateLimit();

// 3. .NET Built-in Rate Limiting (fallback for basic protection)
// Note: This provides basic rate limiting as fallback, but our custom middleware takes precedence
app.UseRateLimiter();

app.UseSession(); // Enable session middleware for guest cart management
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

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

app.Run();

// Make Program class accessible for testing
public partial class Program { }
