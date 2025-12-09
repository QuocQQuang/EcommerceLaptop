using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcommerceLaptop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialWithAdminSystem : Migration
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlogCategories", x => x.Id);
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
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsAdminRole = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
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
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
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
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false)
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
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Email = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Avatar = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsEmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginIP = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    FailedLoginAttempts = table.Column<int>(type: "int", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastPasswordChangeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminUsers_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
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
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Excerpt = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FeaturedImage = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    MetaTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MetaDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Tags = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ViewCount = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
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
                        onDelete: ReferentialAction.Restrict);
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
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Entity = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Details = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OldValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValues = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: false),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdminUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminAuditLogs_AdminUsers_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "AdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdminRefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Token = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsRevoked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedBy = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IPAddress = table.Column<string>(type: "nvarchar(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    AdminUserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminRefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminRefreshTokens_AdminUsers_AdminUserId",
                        column: x => x.AdminUserId,
                        principalTable: "AdminUsers",
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
                    { 1, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xem dashboard", "dashboard", "dashboard:read" },
                    { 2, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xem danh sch ngi dng", "users", "users:read" },
                    { 3, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "To v chnh sa ngi dng", "users", "users:write" },
                    { 4, "delete", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xa ngi dng", "users", "users:delete" },
                    { 5, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Qun l ton b ngi dng", "users", "users:manage" },
                    { 6, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xem danh sch vai tr", "roles", "roles:read" },
                    { 7, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "To v chnh sa vai tr", "roles", "roles:write" },
                    { 8, "delete", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xa vai tr", "roles", "roles:delete" },
                    { 9, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Qun l ton b vai tr", "roles", "roles:manage" },
                    { 10, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xem danh sch sn phm", "products", "products:read" },
                    { 11, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "To v chnh sa sn phm", "products", "products:write" },
                    { 12, "delete", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xa sn phm", "products", "products:delete" },
                    { 13, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Qun l ton b sn phm", "products", "products:manage" },
                    { 14, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xem danh sch n hng", "orders", "orders:read" },
                    { 15, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Chnh sa n hng", "orders", "orders:write" },
                    { 16, "delete", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xa n hng", "orders", "orders:delete" },
                    { 17, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Qun l ton b n hng", "orders", "orders:manage" },
                    { 18, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xem danh sch khuyn mi", "promotions", "promotions:read" },
                    { 19, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "To v chnh sa khuyn mi", "promotions", "promotions:write" },
                    { 20, "delete", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xa khuyn mi", "promotions", "promotions:delete" },
                    { 21, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Qun l ton b khuyn mi", "promotions", "promotions:manage" },
                    { 22, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xem ci t h thng", "settings", "settings:read" },
                    { 23, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Chnh sa ci t h thng", "settings", "settings:write" },
                    { 24, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Qun l ton b ci t", "settings", "settings:manage" },
                    { 25, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xem nht k h thng", "logs", "logs:read" },
                    { 26, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Qun l nht k h thng", "logs", "logs:manage" },
                    { 27, "read", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Xem ci t bo mt", "security", "security:read" },
                    { 28, "write", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Chnh sa ci t bo mt", "security", "security:write" },
                    { 29, "manage", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Qun l ton b bo mt", "security", "security:manage" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Brand", "CreatedAt", "Description", "IsActive", "Model", "Name", "Price", "SKU", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, "Dell", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Premium ultrabook with latest Intel processor", true, "XPS 13 Plus", "Dell XPS 13 Plus", 1299.99m, "DELL-XPS13-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, "ASUS", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "High-performance gaming laptop with RTX graphics", true, "ROG Strix G15", "ASUS ROG Strix G15", 1599.99m, "ASUS-ROG-G15-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, "Logitech", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Advanced wireless mouse for productivity", true, "MX Master 3S", "Logitech MX Master 3S", 99.99m, "LOG-MX3S-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "Dell", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Universal docking station with multiple ports", true, "WD19", "Dell WD19 Docking Station", 199.99m, "DELL-WD19-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5, "Apple", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "The most powerful MacBook Pro ever.", true, "MacBook Pro 16", "MacBook Pro 16", 2499.00m, "APP-MBP16-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6, "HP", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Stunning 2-in-1 with OLED display.", true, "Spectre x360 14", "HP Spectre x360 14", 1549.99m, "HP-SPEC14-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7, "Lenovo", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Ultralight business powerhouse.", true, "ThinkPad X1 Carbon", "Lenovo ThinkPad X1 Carbon Gen 12", 1899.00m, "LEN-TPX1C12-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8, "Razer", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "The ultimate gaming laptop.", true, "Blade 15", "Razer Blade 15", 2999.99m, "RAZ-BL15-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9, "Microsoft", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Sleek, stylish, and powerful.", true, "Surface Laptop 5", "Microsoft Surface Laptop 5", 1299.99m, "MS-SL5-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10, "Acer", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Affordable OLED ultrabook.", true, "Swift Go 14", "Acer Swift Go 14", 999.99m, "ACR-SG14-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11, "LG", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Incredibly lightweight 17-inch laptop.", true, "Gram 17", "LG Gram 17", 1799.99m, "LG-GRAM17-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12, "Samsung", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "A powerful creator's laptop with a stunning display.", true, "Galaxy Book3 Ultra", "Samsung Galaxy Book3 Ultra", 2399.99m, "SAM-GB3U-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13, "Framework", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Modular, repairable, and customizable.", true, "Laptop 13", "Framework Laptop 13", 1049.00m, "FRW-LP13-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14, "MSI", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Desktop-replacement gaming performance.", true, "Titan GT77 HX", "MSI Titan GT77 HX", 4999.99m, "MSI-GT77-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 15, "Apple", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Sleek and comfortable typing experience.", true, "Magic Keyboard", "Apple Magic Keyboard", 99.00m, "APP-MK-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 16, "Razer", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Optical gaming keyboard with near-zero latency.", true, "Huntsman V2", "Razer Huntsman V2", 189.99m, "RAZ-HV2-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 17, "Logitech", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Ultralight wireless gaming mouse.", true, "G Pro X Superlight", "Logitech G Pro X Superlight", 159.99m, "LOG-GPX-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 18, "Dell", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "4K USB-C Hub Monitor with IPS Black technology.", true, "U2723QE", "Dell UltraSharp U2723QE", 649.99m, "DELL-U2723QE-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 19, "CalDigit", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "The ultimate docking station with 18 ports.", true, "TS4", "CalDigit TS4 Thunderbolt 4 Dock", 399.95m, "CD-TS4-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 20, "Anker", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "High-capacity power bank with 140W output.", true, "737 Power Bank", "Anker 737 Power Bank (PowerCore 24K)", 149.99m, "ANK-737PB-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 21, "Sony", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Industry-leading noise canceling headphones.", true, "WH-1000XM5", "Sony WH-1000XM5 Headphones", 399.99m, "SNY-XM5-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 22, "Samsung", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Rugged and fast portable storage.", true, "T7 Shield", "Samsung T7 Shield Portable SSD", 159.99m, "SAM-T7S-1TB-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 23, "Cooler Master", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Laptop cooling pad with a 200mm blue LED fan.", true, "Notepal X3", "Cooler Master Notepal X3", 39.99m, "CM-NPX3-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 24, "Tomtoc", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Military-grade protection for your laptop.", true, "A13-E2", "Tomtoc 360 Protective Laptop Sleeve", 29.99m, "TOM-A13E2-14-001", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "CreatedAt", "Description", "IsAdminRole", "Name", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Regular customer", false, "Customer", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "System administrator", false, "Admin", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Sales representative", false, "Sales", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Marketing specialist", false, "Marketing", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 100, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Qun tr vin h thng vi ton quyn", true, "SystemAdmin", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 101, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Qun l sn phm v inventory", true, "ProductAdmin", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 102, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Qun l n hng v bn hng", true, "SalesAdmin", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 103, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Qun l khuyn mi v marketing", true, "PromotionManager", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "Email", "EmailConfirmed", "FirstName", "IsActive", "LastName", "LastPasswordChangeDate", "PasswordHash", "PhoneNumber", "ProfilePictureUrl", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "admin@ecommerce.com", false, "Admin", true, "User", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "$2a$12$E4JLQhnSHWye5rV2Fb13tORcco3l1bTwU1FszCA59j.M7Kzk8LeXq", "+84901234567", null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Accessories",
                columns: new[] { "Id", "AccessoryType", "Color", "Compatibility", "Connectivity", "Specifications" },
                values: new object[,]
                {
                    { 3, "Mouse", "Graphite", "Universal", "Wireless", "{\"dpi\":\"8000\",\"battery\":\"70 days\",\"connectivity\":\"Bluetooth/USB-C\"}" },
                    { 4, "Docking Station", "Black", "Dell Laptops", "USB-C", "{\"ports\":\"USB-C, USB-A, HDMI, DisplayPort, Ethernet\",\"power\":\"90W\"}" },
                    { 15, "Keyboard", "Silver", "Mac, iPad", "Wireless", "{\"layout\":\"Compact\",\"switch\":\"Scissor\",\"connectivity\":\"Bluetooth\"}" },
                    { 16, "Keyboard", "Black", "PC", "Wired", "{\"layout\":\"Full-size\",\"switch\":\"Razer Optical\",\"connectivity\":\"Wired\"}" },
                    { 17, "Mouse", "White", "PC", "Wireless", "{\"dpi\":\"25600\",\"battery\":\"70 hours\",\"weight\":\"63g\"}" },
                    { 18, "Monitor", "Silver", "Universal", "Wired", "{\"size\":\"27-inch\",\"resolution\":\"4K\",\"panel\":\"IPS Black\",\"ports\":\"USB-C, HDMI, DP\"}" },
                    { 19, "Docking Station", "Space Grey", "Thunderbolt 4/3, USB4, USB-C", "Thunderbolt 4", "{\"power_delivery\":\"98W\",\"ports\":\"18\",\"ethernet\":\"2.5GbE\"}" },
                    { 20, "Power Bank", "Black", "Universal", "N/A", "{\"capacity\":\"24000mAh\",\"output\":\"140W\",\"ports\":\"2x USB-C, 1x USB-A\"}" },
                    { 21, "Headphones", "Black", "Universal", "Wireless", "{\"noise_canceling\":\"Active\",\"battery\":\"30 hours\",\"connectivity\":\"Bluetooth\"}" },
                    { 22, "External Storage", "Blue", "Universal", "USB-C", "{\"capacity\":\"1TB\",\"speed\":\"1050MB/s\",\"durability\":\"IP65\"}" },
                    { 23, "Laptop Cooler", "Black", "Up to 17-inch laptops", "USB", "{\"fan_size\":\"200mm\",\"fan_speed\":\"500-850 RPM\",\"ports\":\"1x USB passthrough\"}" },
                    { 24, "Laptop Sleeve", "Gray", "14-inch Laptops", "N/A", "{\"material\":\"Cordura Fabric\",\"protection\":\"CornerArmor\",\"lining\":\"Soft fleece\"}" }
                });

            migrationBuilder.InsertData(
                table: "AdminUsers",
                columns: new[] { "Id", "Avatar", "CreatedAt", "Email", "FailedLoginAttempts", "FirstName", "IsActive", "IsEmailVerified", "LastLoginAt", "LastLoginIP", "LastName", "LastPasswordChangeDate", "LockedUntil", "Notes", "PasswordHash", "RoleId", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "admin@example.com", 0, "System", true, true, null, null, "Administrator", null, null, "Default system administrator account", "$2a$11$rqiEwJ5k3qV4y5e5KqT4k.8UX9g2XKJt7f4y5e5KqT4k8UX9g2XKJ", 100, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "product@example.com", 0, "Product", true, true, null, null, "Manager", null, null, "Product management account", "$2a$11$rqiEwJ5k3qV4y5e5KqT4k.8UX9g2XKJt7f4y5e5KqT4k8UX9g2XKJ", 101, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "sales@example.com", 0, "Sales", true, true, null, null, "Manager", null, null, "Sales management account", "$2a$11$rqiEwJ5k3qV4y5e5KqT4k.8UX9g2XKJt7f4y5e5KqT4k8UX9g2XKJ", 102, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, null, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "promo@example.com", 0, "Promotion", true, true, null, null, "Manager", null, null, "Promotion management account", "$2a$11$rqiEwJ5k3qV4y5e5KqT4k.8UX9g2XKJt7f4y5e5KqT4k8UX9g2XKJ", 103, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Inventories",
                columns: new[] { "Id", "LastStockUpdate", "MaxStockLevel", "ProductId", "QuantityInStock", "ReorderLevel", "ReservedQuantity", "WarehouseLocation" },
                values: new object[,]
                {
                    { 1, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 100, 1, 50, 10, 0, "WH-A-001" },
                    { 2, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 50, 2, 30, 5, 0, "WH-A-002" },
                    { 3, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 200, 3, 100, 20, 0, "WH-B-001" },
                    { 4, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 50, 4, 25, 5, 0, "WH-B-002" },
                    { 5, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 40, 5, 20, 5, 0, "WH-C-001" },
                    { 6, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 60, 6, 35, 10, 0, "WH-C-002" },
                    { 7, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 80, 7, 40, 10, 0, "WH-C-003" },
                    { 8, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 30, 8, 15, 5, 0, "WH-C-004" },
                    { 9, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 100, 9, 50, 15, 0, "WH-C-005" },
                    { 10, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 120, 10, 60, 20, 0, "WH-D-001" },
                    { 11, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 50, 11, 25, 5, 0, "WH-D-002" },
                    { 12, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 35, 12, 18, 5, 0, "WH-D-003" },
                    { 13, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 70, 13, 30, 10, 0, "WH-D-004" },
                    { 14, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 20, 14, 10, 2, 0, "WH-D-005" },
                    { 15, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 300, 15, 150, 30, 0, "WH-E-001" },
                    { 16, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 150, 16, 80, 20, 0, "WH-E-002" },
                    { 17, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 250, 17, 120, 25, 0, "WH-E-003" },
                    { 18, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 80, 18, 40, 10, 0, "WH-E-004" },
                    { 19, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 60, 19, 30, 10, 0, "WH-E-005" },
                    { 20, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 200, 20, 90, 20, 0, "WH-F-001" },
                    { 21, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 150, 21, 70, 15, 0, "WH-F-002" },
                    { 22, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 200, 22, 100, 25, 0, "WH-F-003" },
                    { 23, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 400, 23, 200, 50, 0, "WH-F-004" },
                    { 24, new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), 500, 24, 300, 75, 0, "WH-F-005" }
                });

            migrationBuilder.InsertData(
                table: "Laptops",
                columns: new[] { "Id", "BatteryCapacityWh", "BluetoothSupport", "BluetoothVersion", "Color", "CpuBaseClockGHz", "CpuBoostClockGHz", "CpuBrand", "CpuCache", "CpuCores", "CpuGeneration", "CpuModel", "Dimensions", "DisplayPanelType", "DisplayRefreshRateHz", "DisplayResolution", "DisplaySizeInches", "DisplayTouchscreen", "GpuBrand", "GpuModel", "GpuType", "GpuVramGB", "NvMeSupport", "Ports", "RamCapacityGB", "RamSlots", "RamSpeed", "RamType", "RamUpgradeable", "Series", "StorageCapacityGB", "StorageInterface", "StorageType", "TargetAudience", "WarrantyPeriod", "WeightKg", "WiFi6Support" },
                values: new object[,]
                {
                    { 1, 55, true, "5.2", "Platinum Silver", 2.2m, 5.0m, "Intel", "18MB", 12, "13th Gen", "Core i7-1360P", "295.3 x 199.04 x 15.28 mm", "IPS", 60, "1920x1200", 13.4m, true, "Intel", "Iris Xe Graphics", "Integrated", 0, true, "2x Thunderbolt 4, 1x Audio Jack", 16, 1, 5200, "LPDDR5", false, "XPS", 512, "NVMe", "SSD", "Business", "1 Year", 1.26m, true },
                    { 2, 90, true, "5.2", "Eclipse Gray", 3.2m, 4.7m, "AMD", "20MB", 8, "6000 Series", "Ryzen 7 6800H", "354 x 259 x 22.8 mm", "IPS", 144, "1920x1080", 15.6m, false, "NVIDIA", "RTX 3070 Ti", "Discrete", 8, true, "1x USB-C, 3x USB-A, 1x HDMI, 1x Audio Jack, 1x Ethernet", 16, 2, 4800, "DDR5", true, "ROG", 1000, "NVMe", "SSD", "Gaming", "2 Years", 2.3m, true },
                    { 5, 100, true, "5.3", "Space Black", 3.5m, 4.1m, "Apple", "48MB", 16, "M3", "M3 Max", "355.7 x 248.1 x 16.8 mm", "Liquid Retina XDR", 120, "3456x2234", 16.2m, false, "Apple", "M3 Max 40-core", "Integrated", 0, true, "3x Thunderbolt 4, 1x HDMI, 1x SDXC card slot, 1x MagSafe 3", 36, 0, 0, "Unified", false, "MacBook Pro", 1000, "Proprietary", "SSD", "Professionals", "1 Year", 2.15m, true },
                    { 6, 68, true, "5.3", "Nightfall Black", 1.4m, 4.8m, "Intel", "24MB", 16, "Meteor Lake", "Core Ultra 7 155H", "313.7 x 220.4 x 16.9 mm", "OLED", 120, "2880x1800", 14.0m, true, "Intel", "Arc Graphics", "Integrated", 0, true, "2x Thunderbolt 4, 1x USB-A, 1x Audio Jack", 16, 0, 6400, "LPDDR5X", false, "Spectre", 1000, "NVMe", "SSD", "Creatives", "1 Year", 1.35m, true },
                    { 7, 57, true, "5.3", "Deep Black", 1.7m, 4.8m, "Intel", "12MB", 12, "Meteor Lake", "Core Ultra 7 155U", "312.8 x 214.75 x 14.96 mm", "OLED", 120, "2880x1800", 14.0m, false, "Intel", "Arc Graphics", "Integrated", 0, true, "2x Thunderbolt 4, 2x USB-A, 1x HDMI 2.1, 1x Audio Jack", 32, 0, 7500, "LPDDR5X", false, "ThinkPad", 1000, "NVMe", "SSD", "Business", "3 Years", 1.09m, true },
                    { 8, 80, true, "5.2", "Black", 2.6m, 5.4m, "Intel", "24MB", 14, "13th Gen", "Core i9-13900H", "355 x 235 x 16.99 mm", "QHD OLED", 240, "2560x1440", 15.6m, false, "NVIDIA", "RTX 4080", "Discrete", 12, true, "1x Thunderbolt 4, 3x USB-A, 1x HDMI 2.1, 1x SD Card Reader", 32, 2, 5200, "DDR5", true, "Blade", 2000, "NVMe", "SSD", "Gaming", "1 Year", 2.01m, true },
                    { 9, 47, true, "5.1", "Matte Black", 1.7m, 4.7m, "Intel", "12MB", 10, "12th Gen", "Core i7-1255U", "308 x 223 x 14.5 mm", "PixelSense", 60, "2256x1504", 13.5m, true, "Intel", "Iris Xe Graphics", "Integrated", 0, true, "1x Thunderbolt 4, 1x USB-A, 1x Surface Connect, 1x Audio Jack", 16, 0, 5200, "LPDDR5X", false, "Surface", 512, "NVMe", "SSD", "Students", "1 Year", 1.29m, true },
                    { 10, 65, true, "5.2", "Prodigy Pink", 3.3m, 5.1m, "AMD", "16MB", 8, "7000 Series", "Ryzen 7 7840U", "312.9 x 217.9 x 14.9 mm", "OLED", 90, "2880x1800", 14.0m, false, "AMD", "Radeon 780M", "Integrated", 0, true, "2x USB-C, 2x USB-A, 1x HDMI 2.1, 1x MicroSD Reader", 16, 0, 6400, "LPDDR5", false, "Swift", 1000, "NVMe", "SSD", "General Use", "1 Year", 1.25m, true },
                    { 11, 80, true, "5.1", "Obsidian Black", 2.2m, 5.0m, "Intel", "18MB", 12, "13th Gen", "Core i7-1360P", "378.8 x 258.8 x 17.7 mm", "IPS", 60, "2560x1600", 17.0m, false, "Intel", "Iris Xe Graphics", "Integrated", 0, true, "2x Thunderbolt 4, 2x USB-A, 1x HDMI, 1x MicroSD Reader", 16, 0, 6000, "LPDDR5", false, "Gram", 1000, "NVMe", "SSD", "Productivity", "1 Year", 1.35m, true },
                    { 12, 76, true, "5.1", "Graphite", 2.6m, 5.4m, "Intel", "24MB", 14, "13th Gen", "Core i9-13900H", "355.4 x 250.4 x 16.5 mm", "Dynamic AMOLED 2X", 120, "2880x1800", 16.0m, false, "NVIDIA", "RTX 4070", "Discrete", 8, true, "2x Thunderbolt 4, 1x USB-A, 1x HDMI 2.0, 1x MicroSD Reader", 32, 0, 6000, "LPDDR5", false, "Galaxy Book", 1000, "NVMe", "SSD", "Creatives", "1 Year", 1.79m, true },
                    { 13, 61, true, "5.2", "Silver", 3.5m, 4.9m, "AMD", "16MB", 6, "7000 Series", "Ryzen 5 7640U", "296.6 x 229 x 15.85 mm", "IPS", 60, "2256x1504", 13.5m, false, "AMD", "Radeon 760M", "Integrated", 0, true, "4x Expansion Card Slots (USB-C, USB-A, HDMI, etc.)", 16, 2, 5600, "DDR5", true, "Framework", 512, "NVMe", "SSD", "Developers", "2 Years", 1.3m, true },
                    { 14, 99, true, "5.3", "Core Black", 2.2m, 5.6m, "Intel", "36MB", 24, "13th Gen", "Core i9-13980HX", "397 x 330 x 23 mm", "Mini LED", 144, "3840x2160", 17.3m, false, "NVIDIA", "RTX 4090", "Discrete", 16, true, "2x Thunderbolt 4, 3x USB-A, 1x HDMI 2.1, 1x Mini DisplayPort, 1x SD Card Reader", 64, 4, 5600, "DDR5", true, "Titan", 4000, "NVMe RAID 0", "SSD", "Hardcore Gaming", "2 Years", 3.3m, true }
                });

            migrationBuilder.InsertData(
                table: "ProductImages",
                columns: new[] { "Id", "AltText", "ImageUrl", "IsPrimary", "ProductId", "SortOrder" },
                values: new object[,]
                {
                    { 1, "Dell XPS 13 Plus front view", "https://i.dell.com/is/image/DellContent/content/dam/ss2/product-images/dell-client-products/notebooks/xps-notebooks/xps-13-9315/media-gallery/notebook-xps-13-9315-nt-blue-gallery-3.psd?fmt=pjpg&pscan=auto&scl=1&wid=442&hei=295&qlt=100,0&resMode=sharp2&size=442,295&chrss=full", true, 1, 0 },
                    { 2, "ASUS ROG Strix G15 side view", "https://dlcdnwebimgs.asus.com/gain/CF23337A-2E43-444E-A74B-4151A2A355B9/w717/h525", true, 2, 0 },
                    { 3, "Logitech MX Master 3S top view", "https://resource.logitech.com/w_800,c_lpad,ar_1:1,q_auto,f_auto,dpr_1.0/d_transparent.gif/content/dam/logitech/en/products/mice/mx-master-3s/gallery/mx-master-3s-mouse-top-view-graphite.png?v=1", true, 3, 0 },
                    { 4, "Dell WD19 Docking Station", "https://i.dell.com/is/image/DellContent/content/dam/ss2/product-images/peripherals/docks/dell-performance-dock-wd19dcs/wd19dcs-module-gallery-1.jpg?fmt=pjpg&pscan=auto&scl=1&wid=376&hei=281&qlt=100,0&resMode=sharp2&size=376,281&chrss=full", true, 4, 0 },
                    { 5, "MacBook Pro 16 in Space Black", "https://store.storeimages.cdn-apple.com/4982/as-images.apple.com/is/mbp16-spaceblack-select-202310?wid=904&hei=840&fmt=jpeg&qlt=90&.v=1697230830200", true, 5, 0 },
                    { 6, "HP Spectre x360 14 in Nightfall Black", "https://in-media.apjonlinecdn.com/catalog/product/cache/b3b166914d87ce343d4dc5ec5117b502/c/0/c08354191.png", true, 6, 0 },
                    { 7, "Lenovo ThinkPad X1 Carbon Gen 12 front view", "https://p1-ofp.static.pub/medias/bWFzdGVyfHJvb3R8MTI4NTg1fGltYWdlL3BuZ3xoMGEvaDAxLzE3NzU0MTE1MDUyNTc0LnBuZ3wzYjQzMTZkYmY3MDUyZGIzYjVlYjAzM2Y4Y2QzYjQ5YmU0YjAyY2Y4YjI3ZDQzYjJlYjY3MjE3Y2FjODg3Y2Yy/lenovo-thinkpad-x1-carbon-gen-12-14-intel-front-facing.png", true, 7, 0 },
                    { 8, "Razer Blade 15 with OLED display", "https://assets3.razerzone.com/pwa2/gaming-laptops/razer-blade-15-2023/razer-blade-15-2023-OLED-940x570.webp", true, 8, 0 },
                    { 9, "Microsoft Surface Laptop 5 in Matte Black", "https://img-prod-cms-rt-microsoft-com.akamaized.net/cms/api/am/imageFileData/RW14Ghw?ver=9324&q=90&m=6&h=705&w=1253&b=%23FFFFFFFF&f=jpg&o=f&p=140&aim=true", true, 9, 0 },
                    { 10, "Acer Swift Go 14 in Prodigy Pink", "https://images.acer.com/is/image/acer/swift-go-14-sfgo14-72-pink-wallpaper-win11-01?wid=1280&hei=720&fmt=jpeg&qlt=80,0&resMode=sharp2&op_usm=1.5,0.7,1,0", true, 10, 0 },
                    { 11, "LG Gram 17 front view", "https://www.lg.com/content/dam/channel/wcms/uk/images/laptops/17z90r-a_adsf7u1_eail_uk_c/17z90r-a-adsf7u1-450.jpg", true, 11, 0 },
                    { 12, "Samsung Galaxy Book3 Ultra in Graphite", "https://images.samsung.com/is/image/samsung/p6pim/uk/np960xfh-xa1uk/gallery/uk-galaxy-book3-ultra-16-inch-i9-32gb-1tb-np960-np960xfh-xa1uk-535072620?$650_519_PNG$", true, 12, 0 },
                    { 13, "Framework Laptop 13 open view", "https://fs.fr-ns.com/img/2023/fw13_amd_open_3_2_1024.png", true, 13, 0 },
                    { 14, "MSI Titan GT77 HX front view", "https://asset.msi.com/resize/image/global/product/product_16724165056a249a7a3378295a845d39f2a09338a0.png/1024.png", true, 14, 0 },
                    { 15, "Apple Magic Keyboard", "https://store.storeimages.cdn-apple.com/4982/as-images.apple.com/is/MMMR3?wid=1144&hei=1144&fmt=jpeg&qlt=90&.v=1672902354094", true, 15, 0 },
                    { 16, "Razer Huntsman V2 Keyboard", "https://assets3.razerzone.com/Y2K3i-w_3b9qf3_a-APLFDgYyYI=/1500x1000/https%3A%2F%2Fassets3.razerzone.com%2Fpwa2%2Fgaming-keyboards%2Frazer-huntsman-v2%2Frazer-huntsman-v2-gallery-1500x1000-1.jpg", true, 16, 0 },
                    { 17, "Logitech G Pro X Superlight Mouse in White", "https://resource.logitech.com/w_800,c_lpad,ar_1:1,q_auto,f_auto,dpr_1.0/d_transparent.gif/content/dam/logitech-g/en/products/gaming-mice/pro-x-superlight-wireless-mouse/gallery/pro-x-superlight-white-gallery-1.png?v=1", true, 17, 0 },
                    { 18, "Dell UltraSharp U2723QE Monitor", "https://i.dell.com/is/image/DellContent/content/dam/ss2/product-images/peripherals/output-devices/dell/monitors/u-series/u2723qe/media-gallery/monitor-u2723qe-gallery-1.psd?fmt=pjpg&pscan=auto&scl=1&wid=476&hei=395&qlt=100,0&resMode=sharp2&size=476,395&chrss=full", true, 18, 0 },
                    { 19, "CalDigit TS4 Thunderbolt 4 Dock", "https://www.caldigit.com/wp-content/uploads/2022/01/ts4_1-scaled.jpg", true, 19, 0 },
                    { 20, "Anker 737 Power Bank", "https://anker.com.vn/cdn/shop/files/A1291-1_1800x1800.jpg?v=1686131043", true, 20, 0 },
                    { 21, "Sony WH-1000XM5 Headphones", "https://m.media-amazon.com/images/I/61LKGfMc4GL._AC_SL1500_.jpg", true, 21, 0 },
                    { 22, "Samsung T7 Shield Portable SSD", "https://images.samsung.com/is/image/samsung/p6pim/uk/mu-pe1t0r-ww/gallery/uk-t7-shield-usb-3-2-gen-2-1tb-mu-pe1t0r-ww-532295036?$650_519_PNG$", true, 22, 0 },
                    { 23, "Cooler Master Notepal X3", "https://m.media-amazon.com/images/I/71OrhA42QAL._AC_SL1500_.jpg", true, 23, 0 },
                    { 24, "Tomtoc 360 Protective Laptop Sleeve", "https://m.media-amazon.com/images/I/81o8T82bL4L._AC_SL1500_.jpg", true, 24, 0 }
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
                    { 29, 100 },
                    { 1, 101 },
                    { 10, 101 },
                    { 11, 101 },
                    { 12, 101 },
                    { 13, 101 },
                    { 18, 101 },
                    { 1, 102 },
                    { 2, 102 },
                    { 10, 102 },
                    { 14, 102 },
                    { 15, 102 },
                    { 16, 102 },
                    { 17, 102 },
                    { 1, 103 },
                    { 10, 103 },
                    { 18, 103 },
                    { 19, 103 },
                    { 20, 103 },
                    { 21, 103 }
                });

            migrationBuilder.InsertData(
                table: "UserRoles",
                columns: new[] { "RoleId", "UserId" },
                values: new object[] { 2, 1 });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_UserId",
                table: "Addresses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_Action_Entity",
                table: "AdminAuditLogs",
                columns: new[] { "Action", "Entity" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_AdminUserId",
                table: "AdminAuditLogs",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_CreatedAt",
                table: "AdminAuditLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRefreshTokens_AdminUserId",
                table: "AdminRefreshTokens",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRefreshTokens_ExpiresAt",
                table: "AdminRefreshTokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRefreshTokens_Token",
                table: "AdminRefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Email",
                table: "AdminUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_IsActive",
                table: "AdminUsers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_RoleId",
                table: "AdminUsers",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_BlogCategories_Name",
                table: "BlogCategories",
                column: "Name");

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
                name: "IX_BlogPosts_IsPublished",
                table: "BlogPosts",
                column: "IsPublished");

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
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_SKU",
                table: "Products",
                column: "SKU",
                unique: true);

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
                name: "IX_ShoppingCarts_UserId",
                table: "ShoppingCarts",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingCarts_UserId_IsActive",
                table: "ShoppingCarts",
                columns: new[] { "UserId", "IsActive" });

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
                name: "AdminAuditLogs");

            migrationBuilder.DropTable(
                name: "AdminRefreshTokens");

            migrationBuilder.DropTable(
                name: "BlogComments");

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
                name: "Laptops");

            migrationBuilder.DropTable(
                name: "OAuth2Accounts");

            migrationBuilder.DropTable(
                name: "OrderAudits");

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
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "Reviews");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            migrationBuilder.DropTable(
                name: "UserRoles");

            migrationBuilder.DropTable(
                name: "WishlistItems");

            migrationBuilder.DropTable(
                name: "AdminUsers");

            migrationBuilder.DropTable(
                name: "BlogPosts");

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
                name: "Users");
        }
    }
}
