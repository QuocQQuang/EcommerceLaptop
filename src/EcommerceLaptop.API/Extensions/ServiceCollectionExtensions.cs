using System.Text;
using System.Threading.RateLimiting;
using EcommerceLaptop.API.Authorization;
using EcommerceLaptop.API.Services;
using EcommerceLaptop.Core.Configuration;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.Validators;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Repositories;
using EcommerceLaptop.Infrastructure.Services;
using EcommerceLaptop.Infrastructure.Services.Payment;
using EcommerceLaptop.Infrastructure.Services.Security;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Nest;

namespace EcommerceLaptop.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("EcommerceLaptop.Infrastructure")));

        // Redis cache configuration
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        // Session state configuration for guest cart management
        services.AddDistributedMemoryCache(); // For development, use Redis in production
        services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromDays(30); // Session timeout for guest carts
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.Name = ".EcommerceLaptop.Session";
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        // Memory Cache for in-memory caching (used in controllers)
        services.AddMemoryCache();

        // HttpContextAccessor for security logging
        services.AddHttpContextAccessor();

        // Elasticsearch configuration
        services.Configure<ElasticsearchSettings>(configuration.GetSection("Elasticsearch"));
        
        services.AddSingleton<IElasticClient>(provider =>
        {
            var settings = configuration.GetSection("Elasticsearch").Get<ElasticsearchSettings>() 
                ?? new ElasticsearchSettings();

            var connectionSettings = new ConnectionSettings(new Uri(settings.Uri))
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

            return new ElasticClient(connectionSettings);
        });

        return services;
    }

    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // JWT Authentication
        var jwtSettings = configuration.GetSection("Jwt");
        var key = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]!);

        services.AddAuthentication(options =>
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
        services.AddAuthorization(options =>
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
        services.AddSingleton<IAuthorizationPolicyProvider, AdminPermissionPolicyProvider>();
        // Register authorization handlers  
        services.AddScoped<IAuthorizationHandler, AdminPermissionHandler>();

        // Unified Authentication Services  
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<IAdminDashboardService, AdminDashboardService>();
        services.AddScoped<ISystemSettingsService, SystemSettingsService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAdminSetupService, AdminSetupService>();
        
        // Public Admin Management Services (Users, Roles, Dev)
        services.AddScoped<IAdminRoleService, AdminRoleService>();
        services.AddScoped<IAdminUserService, AdminUserService>();
        services.AddScoped<IDevService, DevService>();

        return services;
    }

    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Security Services - IP Blocking, Rate Limiting, Audit Logging
        services.AddScoped<IIPBlockingService, IPBlockingService>();
        services.AddScoped<IRateLimitingService, RateLimitingService>();
        services.AddScoped<ISecurityEventService, SecurityEventService>();
        services.AddScoped<IAuditLoggingService, AuditLoggingService>();

        // Fallback Rate Limiting Registration
        services.AddRateLimiter(options =>
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

            options.AddPolicy("AdminAuthPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100, 
                        Window = TimeSpan.FromMinutes(1), 
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10 
                    }));
        });

        // CORS
        services.AddCors(options =>
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
                        "https://a33lprojecct.id.vn" 
                    )
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials()
                    .SetIsOriginAllowed(_ => true); 
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

        return services;
    }

    public static IServiceCollection AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Email Service Configuration
        services.Configure<EmailSettings>(configuration.GetSection("EmailSettings"));
        services.AddScoped<IEmailService, GmailEmailService>();
        services.AddScoped<IEmailQueueService, EmailQueueService>();

        // Email Security Services
        services.AddDataProtection()
            .SetApplicationName("EcommerceLaptop");
        services.AddScoped<IEmailSecurityService, EmailSecurityService>();
        services.AddScoped<IGmailOAuth2Service, GmailOAuth2Service>();

        // ImgBB Service
        services.Configure<ImgBBSettings>(configuration.GetSection("ImgBB"));
        services.AddHttpClient<ImgBBService>();
        services.AddScoped<IImageHostingService, ImgBBService>();

        // Payment Gateway Services
        services.Configure<PaymentGatewaySettings>(configuration.GetSection("PaymentGateways"));
        services.AddHttpClient(); // For payments

        // Payment Services
        services.AddScoped<IPaymentOrchestrator, PaymentOrchestrator>();
        services.AddScoped<IPaymentServiceFactory, PaymentServiceFactory>();
        services.AddScoped<IVnPayService, VnPayService>();
        services.AddScoped<IMoMoService, MoMoService>();
        services.AddScoped<IPayPalService, PayPalService>();
        services.AddScoped<IZaloPayService, ZaloPayService>();
        services.AddScoped<IStripeService, StripeService>();
        services.AddScoped<ISePayService, SePayService>();
        services.AddScoped<SePayGatewayAdapter>();
        services.AddScoped<PayPalWebhookVerificationService>();
        services.AddScoped<StripeWebhookVerificationService>();
        services.AddScoped<IPaymentWebhookBusinessLogicService, PaymentWebhookBusinessLogicService>();
        
        // Payment Validators
        services.AddScoped<IValidator<PaymentGatewaySettings>, PaymentGatewaySettingsValidator>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Business Services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IReviewService, ReviewService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<ICustomerManagementService, CustomerManagementService>();
        services.AddScoped<IWishlistService, WishlistService>();
        
        // Inventory Management
        services.AddScoped(typeof(IAsyncRepository<>), typeof(EfRepository<>));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        
        services.AddScoped<IStockManagementService, StockManagementService>();
        services.AddScoped<IAssetTrackingService, AssetTrackingService>();
        services.AddScoped<IInventoryReportingService, InventoryReportingService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IInventoryReservationService, InventoryReservationService>();
        
        // Domain Events
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Feature Flags
        services.AddSingleton<IFeatureFlagService, FeatureFlagService>();

        // Shopping Cart
        services.AddScoped<IShoppingCartService, ShoppingCartService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<ICartValidationService, CartValidationService>();

        // Order Management
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IOrderWorkflowService, OrderWorkflowService>();

        // Search
        services.AddScoped<IProductSearchService, ProductSearchService>();

        // Blog
        services.AddScoped<IBlogService, BlogService>();

        // Export Services
        services.AddScoped<IPdfExportService, PdfExportService>();
        services.AddScoped<IExcelExportService, ExcelExportService>();
        services.AddScoped<IReportingService, ReportingService>();

        // Background Services
        services.AddHostedService<EmailBackgroundService>();
        services.AddHostedService<ProductIndexingService>();

        // AutoMapper
        services.AddAutoMapper(typeof(Program));

        return services;
    }
    
    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new() { Title = "Ca Hng Laptop API", Version = "v1" });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme",
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
        
        return services;
    }
}
