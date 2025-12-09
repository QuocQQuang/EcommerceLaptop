using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace EcommerceLaptop.Migrations
{
    /// <inheritdoc />
    public partial class ConvertToTPTStrategy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BundleItems_Products_BundleId",
                table: "BundleItems");

            migrationBuilder.DropColumn(
                name: "AccessoryType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Accessory_Color",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BatteryCapacityWh",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BluetoothSupport",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BluetoothVersion",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BundleType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Color",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Compatibility",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Connectivity",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CpuBaseClockGHz",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CpuBoostClockGHz",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CpuBrand",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CpuCache",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CpuCores",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CpuGeneration",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CpuModel",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Dimensions",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DiscountPercentage",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DisplayPanelType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DisplayRefreshRateHz",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DisplayResolution",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DisplaySizeInches",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "DisplayTouchscreen",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "GpuBrand",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "GpuModel",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "GpuType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "GpuVramGB",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "NvMeSupport",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Ports",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RamCapacityGB",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RamSlots",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RamSpeed",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RamType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RamUpgradeable",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Series",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Specifications",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StorageCapacityGB",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StorageInterface",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "StorageType",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "TargetAudience",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ValidFrom",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ValidTo",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WarrantyPeriod",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "WiFi6Support",
                table: "Products");

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

            migrationBuilder.InsertData(
                table: "Accessories",
                columns: new[] { "Id", "AccessoryType", "Color", "Compatibility", "Connectivity", "Specifications" },
                values: new object[,]
                {
                    { 3, "Mouse", "Graphite", "Universal", "Wireless", "{\"dpi\":\"8000\",\"battery\":\"70 days\",\"connectivity\":\"Bluetooth/USB-C\"}" },
                    { 4, "Docking Station", "Black", "Dell Laptops", "USB-C", "{\"ports\":\"USB-C, USB-A, HDMI, DisplayPort, Ethernet\",\"power\":\"90W\"}" }
                });

            migrationBuilder.InsertData(
                table: "Laptops",
                columns: new[] { "Id", "BatteryCapacityWh", "BluetoothSupport", "BluetoothVersion", "Color", "CpuBaseClockGHz", "CpuBoostClockGHz", "CpuBrand", "CpuCache", "CpuCores", "CpuGeneration", "CpuModel", "Dimensions", "DisplayPanelType", "DisplayRefreshRateHz", "DisplayResolution", "DisplaySizeInches", "DisplayTouchscreen", "GpuBrand", "GpuModel", "GpuType", "GpuVramGB", "NvMeSupport", "Ports", "RamCapacityGB", "RamSlots", "RamSpeed", "RamType", "RamUpgradeable", "Series", "StorageCapacityGB", "StorageInterface", "StorageType", "TargetAudience", "WarrantyPeriod", "WeightKg", "WiFi6Support" },
                values: new object[,]
                {
                    { 1, 55, true, "5.2", "Platinum Silver", 2.2m, 5.0m, "Intel", "18MB", 12, "13th Gen", "Core i7-1360P", "295.3 x 199.04 x 15.28 mm", "IPS", 60, "1920x1200", 13.4m, true, "Intel", "Iris Xe Graphics", "Integrated", 0, true, "2x Thunderbolt 4, 1x Audio Jack", 16, 1, 5200, "LPDDR5", false, "XPS", 512, "NVMe", "SSD", "Business", "1 Year", 1.26m, true },
                    { 2, 90, true, "5.2", "Eclipse Gray", 3.2m, 4.7m, "AMD", "20MB", 8, "6000 Series", "Ryzen 7 6800H", "354 x 259 x 22.8 mm", "IPS", 144, "1920x1080", 15.6m, false, "NVIDIA", "RTX 3070 Ti", "Discrete", 8, true, "1x USB-C, 3x USB-A, 1x HDMI, 1x Audio Jack, 1x Ethernet", 16, 2, 4800, "DDR5", true, "ROG", 1000, "NVMe", "SSD", "Gaming", "2 Years", 2.3m, true }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_BundleItems_Bundles_BundleId",
                table: "BundleItems",
                column: "BundleId",
                principalTable: "Bundles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BundleItems_Bundles_BundleId",
                table: "BundleItems");

            migrationBuilder.DropTable(
                name: "Accessories");

            migrationBuilder.DropTable(
                name: "Bundles");

            migrationBuilder.DropTable(
                name: "Laptops");

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Products",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.AddColumn<string>(
                name: "AccessoryType",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Accessory_Color",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BatteryCapacityWh",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "BluetoothSupport",
                table: "Products",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BluetoothVersion",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BundleType",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Color",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Compatibility",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Connectivity",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CpuBaseClockGHz",
                table: "Products",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CpuBoostClockGHz",
                table: "Products",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CpuBrand",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CpuCache",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CpuCores",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CpuGeneration",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CpuModel",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Dimensions",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountPercentage",
                table: "Products",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayPanelType",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayRefreshRateHz",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayResolution",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DisplaySizeInches",
                table: "Products",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "DisplayTouchscreen",
                table: "Products",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GpuBrand",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GpuModel",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GpuType",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GpuVramGB",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "NvMeSupport",
                table: "Products",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Ports",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "Products",
                type: "nvarchar(13)",
                maxLength: 13,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "RamCapacityGB",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RamSlots",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RamSpeed",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RamType",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RamUpgradeable",
                table: "Products",
                type: "bit",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Series",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Specifications",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StorageCapacityGB",
                table: "Products",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageInterface",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StorageType",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetAudience",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidFrom",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ValidTo",
                table: "Products",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyPeriod",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "Products",
                type: "decimal(4,2)",
                precision: 4,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WiFi6Support",
                table: "Products",
                type: "bit",
                nullable: true);

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "BatteryCapacityWh", "BluetoothSupport", "BluetoothVersion", "Brand", "Color", "CpuBaseClockGHz", "CpuBoostClockGHz", "CpuBrand", "CpuCache", "CpuCores", "CpuGeneration", "CpuModel", "CreatedAt", "Description", "Dimensions", "DisplayPanelType", "DisplayRefreshRateHz", "DisplayResolution", "DisplaySizeInches", "DisplayTouchscreen", "GpuBrand", "GpuModel", "GpuType", "GpuVramGB", "IsActive", "Model", "Name", "NvMeSupport", "Ports", "Price", "ProductType", "RamCapacityGB", "RamSlots", "RamSpeed", "RamType", "RamUpgradeable", "SKU", "Series", "StorageCapacityGB", "StorageInterface", "StorageType", "TargetAudience", "UpdatedAt", "WarrantyPeriod", "WeightKg", "WiFi6Support" },
                values: new object[,]
                {
                    { 1, 55, true, "5.2", "Dell", "Platinum Silver", 2.2m, 5.0m, "Intel", "18MB", 12, "13th Gen", "Core i7-1360P", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Premium ultrabook with latest Intel processor", "295.3 x 199.04 x 15.28 mm", "IPS", 60, "1920x1200", 13.4m, true, "Intel", "Iris Xe Graphics", "Integrated", 0, true, "XPS 13 Plus", "Dell XPS 13 Plus", true, "2x Thunderbolt 4, 1x Audio Jack", 1299.99m, "Laptop", 16, 1, 5200, "LPDDR5", false, "DELL-XPS13-001", "XPS", 512, "NVMe", "SSD", "Business", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "1 Year", 1.26m, true },
                    { 2, 90, true, "5.2", "ASUS", "Eclipse Gray", 3.2m, 4.7m, "AMD", "20MB", 8, "6000 Series", "Ryzen 7 6800H", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "High-performance gaming laptop with RTX graphics", "354 x 259 x 22.8 mm", "IPS", 144, "1920x1080", 15.6m, false, "NVIDIA", "RTX 3070 Ti", "Discrete", 8, true, "ROG Strix G15", "ASUS ROG Strix G15", true, "1x USB-C, 3x USB-A, 1x HDMI, 1x Audio Jack, 1x Ethernet", 1599.99m, "Laptop", 16, 2, 4800, "DDR5", true, "ASUS-ROG-G15-001", "ROG", 1000, "NVMe", "SSD", "Gaming", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "2 Years", 2.3m, true }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "AccessoryType", "Brand", "Accessory_Color", "Compatibility", "Connectivity", "CreatedAt", "Description", "IsActive", "Model", "Name", "Price", "ProductType", "SKU", "Specifications", "UpdatedAt" },
                values: new object[,]
                {
                    { 3, "Mouse", "Logitech", "Graphite", "Universal", "Wireless", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Advanced wireless mouse for productivity", true, "MX Master 3S", "Logitech MX Master 3S", 99.99m, "Accessory", "LOG-MX3S-001", "{\"dpi\":\"8000\",\"battery\":\"70 days\",\"connectivity\":\"Bluetooth/USB-C\"}", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4, "Docking Station", "Dell", "Black", "Dell Laptops", "USB-C", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc), "Universal docking station with multiple ports", true, "WD19", "Dell WD19 Docking Station", 199.99m, "Accessory", "DELL-WD19-001", "{\"ports\":\"USB-C, USB-A, HDMI, DisplayPort, Ethernet\",\"power\":\"90W\"}", new DateTime(2025, 9, 12, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.AddForeignKey(
                name: "FK_BundleItems_Products_BundleId",
                table: "BundleItems",
                column: "BundleId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
