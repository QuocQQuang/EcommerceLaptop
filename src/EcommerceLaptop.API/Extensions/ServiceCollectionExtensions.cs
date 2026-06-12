using System.Text;
using System.Threading.RateLimiting;
using EcommerceLaptop.API.Authorization;
using EcommerceLaptop.API.Services;
using EcommerceLaptop.Core.Configuration;
using EcommerceLaptop.Core.DomainEvents;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.Validators;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Jobs; // Correct namespace
using EcommerceLaptop.Infrastructure.Middleware;
using EcommerceLaptop.Infrastructure.Repositories;
using EcommerceLaptop.Infrastructure.Services;
using EcommerceLaptop.Infrastructure.Services.AI;
using EcommerceLaptop.Infrastructure.Services.Logging;
using EcommerceLaptop.Infrastructure.Services.Payment;
using EcommerceLaptop.Infrastructure.Services.Security;
using FluentValidation;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Typesense;
using Typesense.Setup;
using Microsoft.Extensions.Http.Resilience;

using StackExchange.Redis;

namespace EcommerceLaptop.API.Extensions;

public static class ServiceCollectionExtensions
{
    private static readonly string[] DefaultAllowedOrigins =
    [
        "http://localhost:3000",
        "https://localhost:3000",
        "http://localhost:3001",
        "https://localhost:3001",
        "http://localhost:3002",
        "https://localhost:3002",
        "http://127.0.0.1:3000",
        "https://127.0.0.1:3000",
        "https://ecommerce-laptop-quocquang.vercel.app"
    ];

    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext with warning suppression for pending model changes
        // (Model snapshot may differ slightly but migrations are correct)
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"),
                b => b.MigrationsAssembly("EcommerceLaptop.Infrastructure"))
            .ConfigureWarnings(w => w.Log(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

        // Redis cache configuration
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = configuration.GetConnectionString("Redis");
        });

        // Register IConnectionMultiplexer for direct Redis access (High Performance)
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? "localhost"));

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

        // Typesense Configuration
        // Typesense Configuration
        services.AddTypesenseClient(config =>
        {
            var typesenseConfig = configuration.GetSection("Typesense");
            config.ApiKey = typesenseConfig["ApiKey"] ?? "xyz";
            config.Nodes = new List<Node>
            {
                new Node(
                    typesenseConfig["Host"] ?? "localhost",
                    typesenseConfig["Port"] ?? "8108",
                    typesenseConfig["Protocol"] ?? "http"
                )
            };
        });

        // AI Configuration Provider
        services.AddScoped<ILlmConfigProvider, LlmConfigProvider>();
        services.AddScoped<ILlmManagementService, LlmManagementService>();

        // AI Ingestion Pipeline
        services.AddScoped<IProductChunkingService, ProductChunkingService>();
        services.AddScoped<IEmbeddingService, SemanticKernelEmbeddingService>();
        services.AddScoped<IVectorDbService, QdrantVectorDbService>();

        // Llm Client with Resilience
        services.AddHttpClient("llm-client")
            .AddStandardResilienceHandler(options =>
            {
                // Total timeout for the entire request execution including retries
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);

                // Timeout per individual attempt (prevent cutting off slow LLM responses)
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);

                // Retry policy configuration
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.Delay = TimeSpan.FromSeconds(1);

                // Circuit breaker configuration
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            });

        // Metrics
        services.AddSingleton<IRagMetricsService, RagMetricsService>();

        // RAG Service
        services.AddScoped<ISemanticCacheService, SemanticCacheService>();
        services.AddScoped<IChatPersistenceService, ChatPersistenceService>();
        services.AddScoped<IChatService, RagChatService>();

        // Tool Calling Infrastructure
        services.AddSingleton<IToolRegistry, ToolRegistry>();
        services.AddScoped<Infrastructure.Services.AI.Tools.GetProductInventoryTool>();
        services.AddScoped<Infrastructure.Services.AI.Plugins.RagChatToolsPlugin>();
        services.AddHostedService<ToolRegistrationService>();

        // Background Jobs (Hangfire)
        services.AddHangfire(config => config
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage(configuration.GetConnectionString("DefaultConnection")));

        services.AddHangfireServer();

        services.AddScoped<IProductIndexingService, ProductIndexingJob>();
        services.AddScoped<ProductIndexingJob>(); // Optional: if concrete type is needed elsewhere

        services.AddScoped<IProductIndexingManagementService, ProductIndexingManagementService>();

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
                            // 1. Check for SignalR access_token in Query String
                            var accessToken = context.Request.Query["access_token"];
                            var path = context.HttpContext.Request.Path;

                            if (!string.IsNullOrEmpty(accessToken) &&
                                path.StartsWithSegments("/chatHub"))
                            {
                                context.Token = accessToken;
                            }
                            // 2. Fallback to admin-session cookie
                            else
                            {
                                var cookieToken = context.Request.Cookies["admin-session"];
                                if (!string.IsNullOrEmpty(cookieToken))
                                {
                                    context.Token = cookieToken;
                                }
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
        // CSRF Protection - Antiforgery tokens
        services.AddAntiforgery(options =>
        {
            options.HeaderName = "X-CSRF-TOKEN";
            options.Cookie.Name = "XSRF-TOKEN";
            options.Cookie.HttpOnly = false; // Allow JS to read for header
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        // Security Services - IP Blocking, Rate Limiting, Audit Logging
        services.AddScoped<IIPBlockingService, IPBlockingService>();
        services.AddScoped<IRateLimitingService, RateLimitingService>();
        services.AddScoped<ISecurityEventService, SecurityEventService>();
        services.AddScoped<IAuditLoggingService, AuditLoggingService>();

        // Loki Client for security event querying from Grafana Loki
        // With resilience policies: retry + circuit breaker (via Microsoft.Extensions.Http.Resilience)
        services.AddHttpClient<ILokiClient, LokiClient>((sp, client) =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var baseUrl = configuration["Loki:BaseUrl"] ?? "http://localhost:3101";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(10); // Reduced from 30s
        })
        .AddStandardResilienceHandler();

        // Fallback Rate Limiting Registration
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;
            options.OnRejected = async (context, cancellationToken) =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var clientIp = context.HttpContext.GetClientIpAddress();
                var endpoint = context.HttpContext.Request.Path;
                var userAgent = context.HttpContext.Request.Headers["User-Agent"].ToString();

                logger.LogWarning("Rate limit exceeded - Client IP: {ClientIp}, Endpoint: {Endpoint}, User-Agent: {UserAgent}, Time: {Time}",
                    clientIp, endpoint, userAgent, DateTime.UtcNow);

                // Automatic IP Blocking
                try
                {
                    var ipBlockingService = context.HttpContext.RequestServices.GetService<IIPBlockingService>();
                    if (ipBlockingService != null && clientIp != "Unknown" && clientIp != "::1" && clientIp != "127.0.0.1")
                    {
                        // Block for 15 minutes
                        // Pass null for adminUserId (defaults to "System") and DateTime for expiration
                        await ipBlockingService.BlockIPAsync(clientIp, "Rate limit exceeded", null, DateTime.UtcNow.AddMinutes(15));
                        logger.LogInformation("Automatically blocked IP {ClientIp} for 15 minutes due to rate limit violation", clientIp);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to automatically block IP {ClientIp}", clientIp);
                }

                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                context.HttpContext.Response.Headers.Append("Retry-After", "900"); // 15 minutes
                context.HttpContext.Response.Headers.Append("X-RateLimit-Limit", "10");
                context.HttpContext.Response.Headers.Append("X-RateLimit-Remaining", "0");
                context.HttpContext.Response.Headers.Append("X-RateLimit-Reset", DateTimeOffset.UtcNow.AddMinutes(15).ToUnixTimeSeconds().ToString());
                context.HttpContext.Response.Headers.Append("X-RateLimit-Reason", "Rate limit exceeded");

                await context.HttpContext.Response.WriteAsync("Too many requests. Your IP has been temporarily blocked.", cancellationToken);
            };

            // Global Limiter: Applies to all endpoints unless overridden
            // 1000 requests per minute per IP for general API usage
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.GetClientIpAddress("unknown"),
                    factory: partition => new FixedWindowRateLimiterOptions
                    {
                        AutoReplenishment = true,
                        PermitLimit = 1000,
                        QueueLimit = 0,
                        Window = TimeSpan.FromMinutes(1)
                    }));

            // Strict policy for Authentication endpoints (Login, Register)
            // 10 requests per minute per IP
            options.AddPolicy("AuthPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.GetClientIpAddress("unknown"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Password Reset / Critical Actions
            // 3 requests per 15 minutes per IP
            options.AddPolicy("PasswordChangePolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.GetClientIpAddress("unknown"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 3,
                        Window = TimeSpan.FromMinutes(15),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));

            // Admin API Policy
            // 200 requests per minute per IP (Higher limit for admins)
            options.AddPolicy("AdminPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.GetClientIpAddress("unknown"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 200,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 10
                    }));

            // Admin Auth Policy (Login)
            // 20 requests per minute per IP
            options.AddPolicy("AdminAuthPolicy", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.GetClientIpAddress("unknown"),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 20,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });

        var allowedOrigins = GetAllowedOrigins(configuration);

        // CORS. Keep exact origins because credentials are enabled.
        services.AddCors(options =>
        {
            options.AddPolicy("Development", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });

            options.AddPolicy("Production", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });

            options.AddPolicy("AllowAll", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    private static string[] GetAllowedOrigins(IConfiguration configuration)
    {
        var configuredOrigins = configuration.GetSection("AllowedOrigins")
            .GetChildren()
            .Select(section => section.Value)
            .Where(origin => !string.IsNullOrWhiteSpace(origin));

        var csvOrigins = (configuration["AllowedOrigins"] ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var origins = configuredOrigins
            .Concat(csvOrigins)
            .Select(NormalizeOrigin)
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return origins.Length > 0 ? origins : DefaultAllowedOrigins;
    }

    private static string NormalizeOrigin(string? origin)
    {
        return (origin ?? string.Empty).Trim().TrimEnd('/');
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
        services.AddScoped<IPayPalService, PayPalService>();
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

        // Event Handlers
        services.AddScoped<IEventHandler<ProductCreatedEvent>, ProductSearchSyncHandler>();
        services.AddScoped<IEventHandler<ProductUpdatedEvent>, ProductSearchSyncHandler>();
        services.AddScoped<IEventHandler<ProductDeletedEvent>, ProductSearchSyncHandler>();

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
        // services.AddHostedService<ProductIndexingService>(); // Replaced by event-driven ProductIndexingJob

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

        // Security
        services.AddScoped<IGuardrailService, GuardrailService>();

        // AI Services
        services.AddScoped<IIntentClassifier, SemanticKernelIntentClassifier>();

        return services;
    }
}


