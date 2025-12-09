using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcommerceLaptop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithCorrectPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlogCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetaTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlogCategories_BlogCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "BlogCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BlogTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogTags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    BannerImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetAudience = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CartSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    BrowserFingerprint = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastAccessedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsMigrated = table.Column<bool>(type: "bit", nullable: false),
                    MigratedToUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    MigratedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiscountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ShippingCost = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Coupons",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MinimumOrderAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaximumDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    UsageLimit = table.Column<int>(type: "int", nullable: false),
                    UsedCount = table.Column<int>(type: "int", nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Coupons", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IPBlockRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "blacklist"),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ThreatLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "medium"),
                    CountryCode = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    ViolationCount = table.Column<int>(type: "int", nullable: false),
                    LastViolation = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RuleSource = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AutoGenerated = table.Column<bool>(type: "bit", nullable: false),
                    TriggerCount = table.Column<int>(type: "int", nullable: false),
                    LastTriggeredBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastTriggeredAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IPBlockRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoginAttempts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    AttemptedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "user"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    AdminUserId = table.Column<int>(type: "int", nullable: true),
                    TwoFactorMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TwoFactorRequired = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorSuccess = table.Column<bool>(type: "bit", nullable: false),
                    GeoLocation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SuspiciousActivity = table.Column<bool>(type: "bit", nullable: false),
                    RiskFactors = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoginAttempts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PaymentMethodConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Gateway = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    MinAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    MaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SupportedCurrencies = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FeePercentage = table.Column<decimal>(type: "decimal(5,4)", precision: 5, scale: 4, nullable: false),
                    FixedFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    Configuration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentMethodConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Module = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductBrands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Website = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ContactEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductBrands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductCategories_ProductCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RateLimitRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "ALL"),
                    RequestsPerMinute = table.Column<int>(type: "int", nullable: false),
                    RequestsPerHour = table.Column<int>(type: "int", nullable: false),
                    RequestsPerDay = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastUpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPWhitelist = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserRoleExceptions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ApiKeyExceptions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CooldownSeconds = table.Column<int>(type: "int", nullable: false),
                    CustomErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BlockOnExceed = table.Column<bool>(type: "bit", nullable: false),
                    BlockDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    IsSystemRule = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitRules", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAdminRole = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShippingRates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FromProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ToProvince = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BaseRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    RatePerKg = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EstimatedDeliveryDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SettingKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "string"),
                    IsEncrypted = table.Column<bool>(type: "bit", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserVipTiers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MinSpendAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Icon = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVipTiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Brand = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SKU = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    BrandId = table.Column<int>(type: "int", nullable: true),
                    CategoryId1 = table.Column<int>(type: "int", nullable: true),
                    ProductBrandId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Products_ProductBrands_BrandId",
                        column: x => x.BrandId,
                        principalTable: "ProductBrands",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Products_ProductBrands_ProductBrandId",
                        column: x => x.ProductBrandId,
                        principalTable: "ProductBrands",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Products_ProductCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ProductCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Products_ProductCategories_CategoryId1",
                        column: x => x.CategoryId1,
                        principalTable: "ProductCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RateLimitViolations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RuleId = table.Column<int>(type: "int", nullable: false),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    Endpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    RequestCount = table.Column<int>(type: "int", nullable: false),
                    LimitExceeded = table.Column<int>(type: "int", nullable: false),
                    TimeWindow = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RateLimitRuleId = table.Column<int>(type: "int", nullable: false),
                    WindowStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WindowEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    WasBlocked = table.Column<bool>(type: "bit", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "blocked")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitViolations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RateLimitViolations_RateLimitRules_RateLimitRuleId",
                        column: x => x.RateLimitRuleId,
                        principalTable: "RateLimitRules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                columns: table => new
                {
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ProfilePictureUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    LastPasswordChangeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    UserType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginIP = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VipTierId = table.Column<int>(type: "int", nullable: true),
                    TotalSpent = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    VipTierUpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsAdminRole = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_UserVipTiers_VipTierId",
                        column: x => x.VipTierId,
                        principalTable: "UserVipTiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Accessories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    AccessoryType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Compatibility = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Specifications = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Connectivity = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accessories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accessories_Products_Id",
                        column: x => x.Id,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bundles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    BundleType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ValidTo = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bundles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bundles_Products_Id",
                        column: x => x.Id,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CampaignProducts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CampaignId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    FixedDiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    SpecialPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CampaignProducts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CampaignProducts_Campaigns_CampaignId",
                        column: x => x.CampaignId,
                        principalTable: "Campaigns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CampaignProducts_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    QuantityInStock = table.Column<int>(type: "int", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "int", nullable: false),
                    ReorderLevel = table.Column<int>(type: "int", nullable: false),
                    MaxStockLevel = table.Column<int>(type: "int", nullable: false),
                    WarehouseLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastStockUpdate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inventories_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Laptops",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Series = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CpuBrand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CpuModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CpuGeneration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CpuCores = table.Column<int>(type: "int", nullable: false),
                    CpuBaseClockGHz = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    CpuBoostClockGHz = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    CpuCache = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RamType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RamCapacityGB = table.Column<int>(type: "int", nullable: false),
                    RamSlots = table.Column<int>(type: "int", nullable: false),
                    RamSpeed = table.Column<int>(type: "int", nullable: false),
                    RamUpgradeable = table.Column<bool>(type: "bit", nullable: false),
                    StorageType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StorageCapacityGB = table.Column<int>(type: "int", nullable: false),
                    StorageInterface = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NvMeSupport = table.Column<bool>(type: "bit", nullable: false),
                    GpuType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GpuBrand = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GpuModel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GpuVramGB = table.Column<int>(type: "int", nullable: false),
                    DisplaySizeInches = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    DisplayResolution = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayPanelType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DisplayRefreshRateHz = table.Column<int>(type: "int", nullable: false),
                    DisplayTouchscreen = table.Column<bool>(type: "bit", nullable: false),
                    BatteryCapacityWh = table.Column<int>(type: "int", nullable: false),
                    WeightKg = table.Column<decimal>(type: "decimal(4,2)", precision: 4, scale: 2, nullable: false),
                    Dimensions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Color = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Ports = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WiFi6Support = table.Column<bool>(type: "bit", nullable: false),
                    BluetoothSupport = table.Column<bool>(type: "bit", nullable: false),
                    BluetoothVersion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    WarrantyPeriod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetAudience = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Laptops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Laptops_Products_Id",
                        column: x => x.Id,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProductImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AltText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    ImageId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeleteUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductImages_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false),
                    Street = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Ward = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "Vit Nam"),
                    PostalCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Addresses_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlogPosts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Excerpt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FeaturedImageUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MetaTitle = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "draft"),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    LikeCount = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: true),
                    AuthorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPosts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlogPosts_BlogCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "BlogCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BlogPosts_Users_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OAuth2Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ProviderUserId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    ProfilePictureUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OAuth2Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OAuth2Accounts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OrderNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingStreet = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShippingCity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShippingProvince = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShippingPostalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ShippingCountry = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InventoryReserved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PasswordResets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ResetToken = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsUsed = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestIpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RequestUserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    UsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UsedIpAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordResets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PasswordResets_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RevokedReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reviews",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsVerifiedPurchase = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reviews_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Reviews_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecurityEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "medium"),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    AdminUserId = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "{}"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false, defaultValue: "new"),
                    InvestigatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvestigatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Resolution = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Endpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequestMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RequiresReview = table.Column<bool>(type: "bit", nullable: false),
                    Tags = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RiskScore = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false, defaultValue: "0"),
                    WasBlocked = table.Column<bool>(type: "bit", nullable: false),
                    InvestigationNotes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ShoppingCarts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", maxLength: 450, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DiscountCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    ShippingCost = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    SubTotal = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingCarts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShoppingCarts_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SystemAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangedFields = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    AdminUserId = table.Column<int>(type: "int", nullable: true),
                    UserEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    UserRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Endpoint = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    HttpMethod = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    ExecutionTimeMs = table.Column<int>(type: "int", nullable: true),
                    RiskLevel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false, defaultValue: "low"),
                    RequiresReview = table.Column<bool>(type: "bit", nullable: false),
                    ComplianceNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalMetadata = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SystemAuditLogs_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_SystemAuditLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "UserActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EntityId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserActivityLogs_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WishlistItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WishlistItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WishlistItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WishlistItems_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BundleItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BundleId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    DiscountPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BundleItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BundleItems_Bundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "Bundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BundleItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InventoryId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Inventories_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "Inventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BlogComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Content = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AuthorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AuthorEmail = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AuthorWebsite = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsApproved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BlogPostId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlogComments_BlogPosts_BlogPostId",
                        column: x => x.BlogPostId,
                        principalTable: "BlogPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlogComments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "BlogPostTags",
                columns: table => new
                {
                    BlogPostId = table.Column<int>(type: "int", nullable: false),
                    BlogTagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogPostTags", x => new { x.BlogPostId, x.BlogTagId });
                    table.ForeignKey(
                        name: "FK_BlogPostTags_BlogPosts_BlogPostId",
                        column: x => x.BlogPostId,
                        principalTable: "BlogPosts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlogPostTags_BlogTags_BlogTagId",
                        column: x => x.BlogTagId,
                        principalTable: "BlogTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrderAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    OldStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "int", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderAudits_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderAudits_Users_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrderEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OldValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    NewValue = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    AdminUserId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderEvents_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderEvents_Users_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Payments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Gateway = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Method = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "VND"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    GatewayResponse = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Payments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PaymentGateway = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    GatewayResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProcessedByAdminId = table.Column<int>(type: "int", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Refunds_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Refunds_Users_ProcessedByAdminId",
                        column: x => x.ProcessedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CartItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShoppingCartId = table.Column<int>(type: "int", nullable: true),
                    CartSessionId = table.Column<int>(type: "int", nullable: true),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    ItemDiscount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    AddedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ConfigurationOptions = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsBundle = table.Column<bool>(type: "bit", nullable: false),
                    ParentBundleItemId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CartItems", x => x.Id);
                    table.CheckConstraint("CK_CartItem_CartReference", "(ShoppingCartId IS NOT NULL AND CartSessionId IS NULL) OR (ShoppingCartId IS NULL AND CartSessionId IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_CartItems_CartItems_ParentBundleItemId",
                        column: x => x.ParentBundleItemId,
                        principalTable: "CartItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CartItems_CartSessions_CartSessionId",
                        column: x => x.CartSessionId,
                        principalTable: "CartSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CartItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CartItems_ShoppingCarts_ShoppingCartId",
                        column: x => x.ShoppingCartId,
                        principalTable: "ShoppingCarts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaymentId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Gateway = table.Column<int>(type: "int", nullable: false),
                    TransactionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RequestData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseData = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ProcessingTimeMs = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Metadata = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentAudits_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Coupons",
                columns: new[] { "Id", "Code", "CreatedAt", "Description", "IsActive", "MaximumDiscountAmount", "MinimumOrderAmount", "Name", "Type", "UsageLimit", "UsedCount", "ValidFrom", "ValidTo", "Value" },
                values: new object[] { 1, "WELCOME10", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "10% off for new customers", true, 200m, 100m, "Welcome Discount", 1, 1000, 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 12, 12, 0, 0, 0, 0, DateTimeKind.Utc), 10m });

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "Module", "Name" },
                values: new object[,]
                {
                    { 1, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "View dashboard", "dashboard", "dashboard:read" },
                    { 2, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "View users", "users", "users:read" },
                    { 3, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Create and edit users", "users", "users:write" },
                    { 4, "delete", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Delete users", "users", "users:delete" },
                    { 5, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Full user management", "users", "users:manage" },
                    { 6, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "View roles", "roles", "roles:read" },
                    { 7, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Create and edit roles", "roles", "roles:write" },
                    { 8, "delete", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Delete roles", "roles", "roles:delete" },
                    { 9, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Full role management", "roles", "roles:manage" },
                    { 10, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "View products", "products", "products:read" },
                    { 11, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Create and edit products", "products", "products:write" },
                    { 12, "delete", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Delete products", "products", "products:delete" },
                    { 13, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Full product management", "products", "products:manage" },
                    { 14, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "View orders", "orders", "orders:read" },
                    { 15, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Create and edit orders", "orders", "orders:write" },
                    { 16, "delete", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Delete orders", "orders", "orders:delete" },
                    { 17, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Full order management", "orders", "orders:manage" },
                    { 18, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "View promotions", "promotions", "promotions:read" },
                    { 19, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Create and edit promotions", "promotions", "promotions:write" },
                    { 20, "delete", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Delete promotions", "promotions", "promotions:delete" },
                    { 21, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Full promotion management", "promotions", "promotions:manage" },
                    { 22, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "View settings", "settings", "settings:read" },
                    { 23, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Edit settings", "settings", "settings:write" },
                    { 24, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Full settings management", "settings", "settings:manage" },
                    { 25, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "View logs", "logs", "logs:read" },
                    { 26, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Full log management", "logs", "logs:manage" },
                    { 27, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "View security settings", "security", "security:read" },
                    { 28, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Edit security settings", "security", "security:write" },
                    { 29, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Full security management", "security", "security:manage" }
                });

            migrationBuilder.InsertData(
                table: "ProductBrands",
                columns: new[] { "Id", "ContactEmail", "CreatedAt", "Description", "IsActive", "LogoUrl", "Name", "Slug", "UpdatedAt", "Website" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "Dell", "dell", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 2, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "ASUS", "asus", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 3, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "Logitech", "logitech", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 4, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "Apple", "apple", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 5, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "HP", "hp", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 6, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "Lenovo", "lenovo", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 7, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "Razer", "razer", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 8, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "Microsoft", "microsoft", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 9, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "Acer", "acer", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 10, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "LG", "lg", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 11, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "Samsung", "samsung", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 12, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "Framework", "framework", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 13, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "MSI", "msi", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 14, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "CalDigit", "caldigit", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 15, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "Anker", "anker", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 16, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "Sony", "sony", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 17, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "Cooler Master", "cooler-master", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 18, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "Tomtoc", "tomtoc", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null },
                    { 19, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, true, null, "TechStore", "techstore", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.InsertData(
                table: "ProductCategories",
                columns: new[] { "Id", "CreatedAt", "Description", "ImageUrl", "IsActive", "Name", "ParentId", "Slug", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "All kinds of laptops", null, true, "Laptops", null, "laptops", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Peripherals and accessories for your setup", null, true, "Accessories", null, "accessories", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Value-packed product bundles", null, true, "Bundles", null, "bundles", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Brand", "BrandId", "CategoryId", "CategoryId1", "CreatedAt", "Description", "IsActive", "Model", "Name", "Price", "ProductBrandId", "SKU", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Dell", null, null, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Premium ultrabook with latest Intel processor", true, "XPS 13 Plus", "Dell XPS 13 Plus", 1299.99m, null, "DELL-XPS13-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "ASUS", null, null, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "High-performance gaming laptop with RTX graphics", true, "ROG Strix G15", "ASUS ROG Strix G15", 1599.99m, null, "ASUS-ROG-G15-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "Logitech", null, null, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Advanced wireless mouse for productivity", true, "MX Master 3S", "Logitech MX Master 3S", 99.99m, null, "LOG-MX3S-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "Dell", null, null, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Universal docking station with multiple ports", true, "WD19", "Dell WD19 Docking Station", 199.99m, null, "DELL-WD19-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 100, "TechStore", null, null, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Complete gaming setup with Dell XPS 13 Plus and essential accessories", true, "STARTER-001", "Gaming Starter Bundle", 1399.99m, null, "BUNDLE-STARTER-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "Name", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Regular customer", "Customer", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "IsAdminRole", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 100, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "System administrator with all permissions", true, "SuperAdmin", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 101, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Manages products, categories, brands", true, "ProductManager", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 102, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Manages orders, payments, shipping", true, "OrderManager", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 103, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Manages campaigns, coupons, blog", true, "MarketingManager", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 104, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Manages security settings and logs", true, "SecurityOfficer", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "EmailConfirmed", "FailedLoginAttempts", "FirstName", "IsActive", "IsAdminRole", "LastLoginAt", "LastLoginIP", "LastName", "LastPasswordChangeDate", "LockedUntil", "Notes", "PasswordHash", "PhoneNumber", "ProfilePictureUrl", "TotalSpent", "UpdatedAt", "UserType", "VipTierId", "VipTierUpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "admin@ecommerce.com", false, 0, "Admin", true, true, null, null, "User", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "$2a$12$K4r7dDD9BpGz.NlGfQnnPOeBjeSx/ALRb4o54eAFPX.wbdpSTqYEi", "+84901234567", null, 0m, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Customer", null, null },
                    { 2, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "superadmin@ecommerce.com", false, 0, "Super", true, true, null, null, "Admin", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "$2a$11$3fcX7/.wsNyTvVA38EwoVeSgsVX2Ov7h6Ga.dOE5kSRzmF3rFuB6y", "+84901112222", null, 0m, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Customer", null, null }
                });

            migrationBuilder.InsertData(
                table: "Accessories",
                columns: new[] { "Id", "AccessoryType", "Color", "Compatibility", "Connectivity", "Specifications" },
                values: new object[,]
                {
                    { 3, "Mouse", "Graphite", "Universal", "Wireless", "{\"dpi\":\"8000\",\"battery\":\"70 days\",\"connectivity\":\"Bluetooth/USB-C\"}" },
                    { 4, "Docking Station", "Black", "Dell Laptops", "USB-C", "{\"ports\":\"USB-C, USB-A, HDMI, DisplayPort, Ethernet\",\"power\":\"90W\"}" }
                });

            migrationBuilder.InsertData(
                table: "Bundles",
                columns: new[] { "Id", "BundleType", "DiscountPercentage", "ValidFrom", "ValidTo" },
                values: new object[] { 100, "Gaming", 15.0m, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 10, 12, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Inventories",
                columns: new[] { "Id", "LastStockUpdate", "MaxStockLevel", "ProductId", "QuantityInStock", "ReorderLevel", "ReservedQuantity", "WarehouseLocation" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 100, 1, 50, 10, 0, "WH-A-001" },
                    { 2, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 50, 2, 30, 5, 0, "WH-A-002" },
                    { 3, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 200, 3, 100, 20, 0, "WH-B-001" },
                    { 4, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 50, 4, 25, 5, 0, "WH-B-002" },
                    { 100, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 20, 100, 10, 2, 0, "WH-BUNDLE-001" }
                });

            migrationBuilder.InsertData(
                table: "Laptops",
                columns: new[] { "Id", "BatteryCapacityWh", "BluetoothSupport", "BluetoothVersion", "Color", "CpuBaseClockGHz", "CpuBoostClockGHz", "CpuBrand", "CpuCache", "CpuCores", "CpuGeneration", "CpuModel", "Dimensions", "DisplayPanelType", "DisplayRefreshRateHz", "DisplayResolution", "DisplaySizeInches", "DisplayTouchscreen", "GpuBrand", "GpuModel", "GpuType", "GpuVramGB", "NvMeSupport", "Ports", "RamCapacityGB", "RamSlots", "RamSpeed", "RamType", "RamUpgradeable", "Series", "StorageCapacityGB", "StorageInterface", "StorageType", "TargetAudience", "WarrantyPeriod", "WeightKg", "WiFi6Support" },
                values: new object[,]
                {
                    { 1, 55, true, "5.2", "Platinum Silver", 2.2m, 5.0m, "Intel", "18MB", 12, "13th Gen", "Core i7-1360P", "295.3 x 199.04 x 15.28 mm", "IPS", 60, "1920x1200", 13.4m, true, "Intel", "Iris Xe Graphics", "Integrated", 0, true, "2x Thunderbolt 4, 1x Audio Jack", 16, 1, 5200, "LPDDR5", false, "XPS", 512, "NVMe", "SSD", "Business", "1 Year", 1.26m, true },
                    { 2, 90, true, "5.2", "Eclipse Gray", 3.2m, 4.7m, "AMD", "20MB", 8, "6000 Series", "Ryzen 7 6800H", "354 x 259 x 22.8 mm", "IPS", 144, "1920x1080", 15.6m, false, "NVIDIA", "RTX 3070 Ti", "Discrete", 8, true, "1x USB-C, 3x USB-A, 1x HDMI, 1x Audio Jack, 1x Ethernet", 16, 2, 4800, "DDR5", true, "ROG", 1000, "NVMe", "SSD", "Gaming", "2 Years", 2.3m, true }
                });

            migrationBuilder.InsertData(
                table: "ProductCategories",
                columns: new[] { "Id", "CreatedAt", "Description", "ImageUrl", "IsActive", "Name", "ParentId", "Slug", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { 4, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "High-performance laptops for gaming", null, true, "Gaming Laptops", 1, "gaming-laptops", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Thin and light laptops for portability", null, true, "Ultrabooks", 1, "ultrabooks", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Reliable and secure laptops for work", null, true, "Business Laptops", 1, "business-laptops", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Powerful laptops for creative professionals", null, true, "Creator Laptops", 1, "creator-laptops", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Gaming and productivity mice", null, true, "Mice", 2, "mice", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Mechanical and membrane keyboards", null, true, "Keyboards", 2, "keyboards", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "High-resolution and high-refresh-rate displays", null, true, "Monitors", 2, "monitors", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Expand your laptop's connectivity", null, true, "Docking Stations", 2, "docking-stations", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Noise-canceling and gaming headsets", null, true, "Headphones", 2, "headphones", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Portable SSDs and HDDs", null, true, "External Storage", 2, "external-storage", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Keep your laptop running cool", null, true, "Laptop Coolers", 2, "laptop-coolers", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Protective cases for your laptop", null, true, "Laptop Sleeves", 2, "laptop-sleeves", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 16, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Charge your devices on the go", null, true, "Power Banks", 2, "power-banks", 0, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, 100 },
                    { 2, 100 },
                    { 3, 100 },
                    { 4, 100 },
                    { 5, 100 },
                    { 6, 100 },
                    { 7, 100 },
                    { 8, 100 },
                    { 9, 100 },
                    { 10, 100 },
                    { 11, 100 },
                    { 12, 100 },
                    { 13, 100 },
                    { 14, 100 },
                    { 15, 100 },
                    { 16, 100 },
                    { 17, 100 },
                    { 18, 100 },
                    { 19, 100 },
                    { 20, 100 },
                    { 21, 100 },
                    { 22, 100 },
                    { 23, 100 },
                    { 24, 100 },
                    { 25, 100 },
                    { 26, 100 },
                    { 27, 100 },
                    { 28, 100 },
                    { 29, 100 }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[,]
                {
                    { 101, 1 },
                    { 100, 2 }
                });

            migrationBuilder.InsertData(
                table: "BundleItems",
                columns: new[] { "Id", "BundleId", "DiscountPercentage", "ProductId", "Quantity" },
                values: new object[,]
                {
                    { 1, 100, 10.0m, 1, 1 },
                    { 2, 100, 20.0m, 3, 1 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_UserId",
                table: "Addresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogCategories_Name",
                table: "BlogCategories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_BlogCategories_ParentId",
                table: "BlogCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogCategories_Slug",
                table: "BlogCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlogComments_BlogPostId",
                table: "BlogComments",
                column: "BlogPostId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogComments_CreatedAt",
                table: "BlogComments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlogComments_IsApproved",
                table: "BlogComments",
                column: "IsApproved");

            migrationBuilder.CreateIndex(
                name: "IX_BlogComments_UserId",
                table: "BlogComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_AuthorId",
                table: "BlogPosts",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_CategoryId",
                table: "BlogPosts",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_CreatedAt",
                table: "BlogPosts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_PublishedAt",
                table: "BlogPosts",
                column: "PublishedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Slug",
                table: "BlogPosts",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlogPosts_Status",
                table: "BlogPosts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BlogPostTags_BlogTagId",
                table: "BlogPostTags",
                column: "BlogTagId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogTags_Name",
                table: "BlogTags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BlogTags_Slug",
                table: "BlogTags",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BundleItems_BundleId",
                table: "BundleItems",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_BundleItems_ProductId",
                table: "BundleItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignProducts_CampaignId",
                table: "CampaignProducts",
                column: "CampaignId");

            migrationBuilder.CreateIndex(
                name: "IX_CampaignProducts_ProductId",
                table: "CampaignProducts",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartSessionId",
                table: "CartItems",
                column: "CartSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_CartSessionId_ProductId",
                table: "CartItems",
                columns: new[] { "CartSessionId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ParentBundleItemId",
                table: "CartItems",
                column: "ParentBundleItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ProductId",
                table: "CartItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ShoppingCartId",
                table: "CartItems",
                column: "ShoppingCartId");

            migrationBuilder.CreateIndex(
                name: "IX_CartItems_ShoppingCartId_ProductId",
                table: "CartItems",
                columns: new[] { "ShoppingCartId", "ProductId" });

            migrationBuilder.CreateIndex(
                name: "IX_CartSessions_ExpiresAt",
                table: "CartSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_CartSessions_LastAccessedAt",
                table: "CartSessions",
                column: "LastAccessedAt");

            migrationBuilder.CreateIndex(
                name: "IX_CartSessions_SessionId",
                table: "CartSessions",
                column: "SessionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CartSessions_SessionId_IsActive",
                table: "CartSessions",
                columns: new[] { "SessionId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Coupons_Code",
                table: "Coupons",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_ProductId",
                table: "Inventories",
                column: "ProductId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_InventoryId",
                table: "InventoryTransactions",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_IPBlockRules_ExpiresAt",
                table: "IPBlockRules",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_IPBlockRules_IPAddress",
                table: "IPBlockRules",
                column: "IPAddress",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IPBlockRules_IsActive",
                table: "IPBlockRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_IPBlockRules_ThreatLevel",
                table: "IPBlockRules",
                column: "ThreatLevel");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_AttemptedAt",
                table: "LoginAttempts",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Email",
                table: "LoginAttempts",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Email_Success_AttemptedAt",
                table: "LoginAttempts",
                columns: new[] { "Email", "Success", "AttemptedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_IPAddress",
                table: "LoginAttempts",
                column: "IPAddress");

            migrationBuilder.CreateIndex(
                name: "IX_LoginAttempts_Success",
                table: "LoginAttempts",
                column: "Success");

            migrationBuilder.CreateIndex(
                name: "IX_OAuth2Accounts_Provider_ProviderUserId",
                table: "OAuth2Accounts",
                columns: new[] { "Provider", "ProviderUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OAuth2Accounts_UserId",
                table: "OAuth2Accounts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAudits_ChangedAt",
                table: "OrderAudits",
                column: "ChangedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAudits_ChangedByUserId",
                table: "OrderAudits",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderAudits_OrderId",
                table: "OrderAudits",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_AdminUserId",
                table: "OrderEvents",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_CreatedAt",
                table: "OrderEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_EventType",
                table: "OrderEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_OrderEvents_OrderId",
                table: "OrderEvents",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ProductId",
                table: "OrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_OrderNumber",
                table: "Orders",
                column: "OrderNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_UserId",
                table: "Orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResets_ExpiresAt",
                table: "PasswordResets",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResets_ResetToken",
                table: "PasswordResets",
                column: "ResetToken");

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResets_UserId_IsUsed_ExpiresAt",
                table: "PasswordResets",
                columns: new[] { "UserId", "IsUsed", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAudits_CreatedAt",
                table: "PaymentAudits",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAudits_Gateway_Action",
                table: "PaymentAudits",
                columns: new[] { "Gateway", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAudits_PaymentId",
                table: "PaymentAudits",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAudits_TransactionId",
                table: "PaymentAudits",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodConfigurations_Gateway_Method",
                table: "PaymentMethodConfigurations",
                columns: new[] { "Gateway", "Method" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentMethodConfigurations_IsEnabled",
                table: "PaymentMethodConfigurations",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Gateway_Status",
                table: "Payments",
                columns: new[] { "Gateway", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_OrderId",
                table: "Payments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TransactionId",
                table: "Payments",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Module_Action",
                table: "Permissions",
                columns: new[] { "Module", "Action" });

            migrationBuilder.CreateIndex(
                name: "IX_Permissions_Name",
                table: "Permissions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductBrands_IsActive",
                table: "ProductBrands",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProductBrands_Name",
                table: "ProductBrands",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductBrands_Slug",
                table: "ProductBrands",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_IsActive",
                table: "ProductCategories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Name",
                table: "ProductCategories",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_ParentId",
                table: "ProductCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductCategories_Slug",
                table: "ProductCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_BrandId",
                table: "Products",
                column: "BrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId",
                table: "Products",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_CategoryId1",
                table: "Products",
                column: "CategoryId1");

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductBrandId",
                table: "Products",
                column: "ProductBrandId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitRules_Endpoint_HttpMethod",
                table: "RateLimitRules",
                columns: new[] { "Endpoint", "HttpMethod" });

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitRules_IsActive",
                table: "RateLimitRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitRules_Priority",
                table: "RateLimitRules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitViolations_CreatedAt",
                table: "RateLimitViolations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitViolations_Endpoint",
                table: "RateLimitViolations",
                column: "Endpoint");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitViolations_IPAddress",
                table: "RateLimitViolations",
                column: "IPAddress");

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitViolations_IPAddress_Endpoint_HttpMethod",
                table: "RateLimitViolations",
                columns: new[] { "IPAddress", "Endpoint", "HttpMethod" });

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitViolations_RateLimitRuleId",
                table: "RateLimitViolations",
                column: "RateLimitRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_CreatedAt",
                table: "Refunds",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_OrderId",
                table: "Refunds",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_ProcessedByAdminId",
                table: "Refunds",
                column: "ProcessedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_Status",
                table: "Refunds",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_ProductId",
                table: "Reviews",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_UserId",
                table: "Reviews",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_AdminUserId",
                table: "SecurityEvents",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_CorrelationId",
                table: "SecurityEvents",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_CreatedAt",
                table: "SecurityEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_EventType",
                table: "SecurityEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_IPAddress",
                table: "SecurityEvents",
                column: "IPAddress");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_Severity",
                table: "SecurityEvents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityEvents_UserId",
                table: "SecurityEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingRates_IsActive",
                table: "ShippingRates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingRates_Provider_FromProvince_ToProvince",
                table: "ShippingRates",
                columns: new[] { "Provider", "FromProvince", "ToProvince" });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_UserId",
                table: "ShoppingCarts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_UserId_IsActive",
                table: "ShoppingCarts",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_Action",
                table: "SystemAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_AdminUserId",
                table: "SystemAuditLogs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_CreatedAt",
                table: "SystemAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_EntityType",
                table: "SystemAuditLogs",
                column: "EntityType");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_EntityType_EntityId",
                table: "SystemAuditLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_IPAddress",
                table: "SystemAuditLogs",
                column: "IPAddress");

            migrationBuilder.CreateIndex(
                name: "IX_SystemAuditLogs_UserId",
                table: "SystemAuditLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Category",
                table: "SystemSettings",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Category_SettingKey",
                table: "SystemSettings",
                columns: new[] { "Category", "SettingKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_CreatedAt",
                table: "UserActivityLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_EntityType_EntityId",
                table: "UserActivityLogs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_UserActivityLogs_UserId",
                table: "UserActivityLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_VipTierId",
                table: "Users",
                column: "VipTierId");

            migrationBuilder.CreateIndex(
                name: "IX_UserVipTiers_IsActive",
                table: "UserVipTiers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_UserVipTiers_MinSpendAmount",
                table: "UserVipTiers",
                column: "MinSpendAmount");

            migrationBuilder.CreateIndex(
                name: "IX_UserVipTiers_Name",
                table: "UserVipTiers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_CreatedAt",
                table: "WishlistItems",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_ProductId",
                table: "WishlistItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_WishlistItems_UserId_ProductId",
                table: "WishlistItems",
                columns: new[] { "UserId", "ProductId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Accessories");

            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "BlogComments");

            migrationBuilder.DropTable(
                name: "BlogPostTags");

            migrationBuilder.DropTable(
                name: "BundleItems");

            migrationBuilder.DropTable(
                name: "CampaignProducts");

            migrationBuilder.DropTable(
                name: "CartItems");

            migrationBuilder.DropTable(
                name: "Coupons");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "IPBlockRules");

            migrationBuilder.DropTable(
                name: "Laptops");

            migrationBuilder.DropTable(
                name: "LoginAttempts");

            migrationBuilder.DropTable(
                name: "OAuth2Accounts");

            migrationBuilder.DropTable(
                name: "OrderAudits");

            migrationBuilder.DropTable(
                name: "OrderEvents");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "PasswordResets");

            migrationBuilder.DropTable(
                name: "PaymentAudits");

            migrationBuilder.DropTable(
                name: "PaymentMethodConfigurations");

            migrationBuilder.DropTable(
                name: "ProductImages");

            migrationBuilder.DropTable(
                name: "RateLimitViolations");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "SecurityEvents");

            migrationBuilder.DropTable(
                name: "ShippingRates");

            migrationBuilder.DropTable(
                name: "SystemAuditLogs");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "UserActivityLogs");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "WishlistItems");

            migrationBuilder.DropTable(
                name: "BlogPosts");

            migrationBuilder.DropTable(
                name: "BlogTags");

            migrationBuilder.DropTable(
                name: "Bundles");

            migrationBuilder.DropTable(
                name: "Campaigns");

            migrationBuilder.DropTable(
                name: "CartSessions");

            migrationBuilder.DropTable(
                name: "ShoppingCarts");

            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropTable(
                name: "Payments");

            migrationBuilder.DropTable(
                name: "RateLimitRules");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropTable(
                name: "BlogCategories");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "ProductBrands");

            migrationBuilder.DropTable(
                name: "ProductCategories");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "UserVipTiers");
        }
    }
}
