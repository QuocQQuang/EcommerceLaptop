using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using System;
using System.Collections.Generic;

namespace EcommerceLaptop.Infrastructure.Data
{
    public static class DataSeeder
    {
        public static void Seed(this ModelBuilder modelBuilder)
        {
            // Use a fixed date to avoid dynamic values in HasData
            var seedDate = new DateTime(2025, 9, 12, 0, 0, 0, DateTimeKind.Utc);

            // Seed roles
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "Customer", Description = "Regular customer", CreatedAt = seedDate, UpdatedAt = seedDate, IsAdminRole = false },
                new Role { Id = 100, Name = "SuperAdmin", Description = "System administrator with all permissions", CreatedAt = seedDate, UpdatedAt = seedDate, IsAdminRole = true },
                new Role { Id = 101, Name = "ProductManager", Description = "Manages products, categories, brands", CreatedAt = seedDate, UpdatedAt = seedDate, IsAdminRole = true },
                new Role { Id = 102, Name = "OrderManager", Description = "Manages orders, payments, shipping", CreatedAt = seedDate, UpdatedAt = seedDate, IsAdminRole = true },
                new Role { Id = 103, Name = "MarketingManager", Description = "Manages campaigns, coupons, blog", CreatedAt = seedDate, UpdatedAt = seedDate, IsAdminRole = true },
                new Role { Id = 104, Name = "SecurityOfficer", Description = "Manages security settings and logs", CreatedAt = seedDate, UpdatedAt = seedDate, IsAdminRole = true }
            );

            // Seed sample admin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Email = "admin@ecommerce.com",
                    PasswordHash = "$2a$12$K4r7dDD9BpGz.NlGfQnnPOeBjeSx/ALRb4o54eAFPX.wbdpSTqYEi", // BCrypt hash for "Admin123!"
                    FirstName = "Admin",
                    LastName = "User",
                    PhoneNumber = "+84901234567",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    LastPasswordChangeDate = seedDate,
                    IsActive = true,
                    IsAdminRole = true
                },
                new User
                {
                    Id = 2,
                    Email = "superadmin@ecommerce.com",
                    PasswordHash = "$2a$11$3fcX7/.wsNyTvVA38EwoVeSgsVX2Ov7h6Ga.dOE5kSRzmF3rFuB6y", // BCrypt hash for "SuperAdmin123!"
                    FirstName = "Super",
                    LastName = "Admin",
                    PhoneNumber = "+84901112222",
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    LastPasswordChangeDate = seedDate,
                    IsActive = true,
                    IsAdminRole = true
                }
            );

            // Assign admin role to admin user
            modelBuilder.Entity<UserRole>().HasData(
                new UserRole { UserId = 1, RoleId = 101 }, // ProductManager
                new UserRole { UserId = 2, RoleId = 100 }  // SuperAdmin
            );

            // --- Seed Permissions ---
            int permId = 1;

            // Use exact permissions from AdminPermissions.cs
            var permissionTemplates = new[]
            {
                // Dashboard permissions
                new { Name = "dashboard:read", Module = "dashboard", Action = "read", Description = "View dashboard" },
                
                // User management permissions
                new { Name = "users:read", Module = "users", Action = "read", Description = "View users" },
                new { Name = "users:write", Module = "users", Action = "write", Description = "Create and edit users" },
                new { Name = "users:delete", Module = "users", Action = "delete", Description = "Delete users" },
                new { Name = "users:manage", Module = "users", Action = "manage", Description = "Full user management" },
                
                // Role management permissions
                new { Name = "roles:read", Module = "roles", Action = "read", Description = "View roles" },
                new { Name = "roles:write", Module = "roles", Action = "write", Description = "Create and edit roles" },
                new { Name = "roles:delete", Module = "roles", Action = "delete", Description = "Delete roles" },
                new { Name = "roles:manage", Module = "roles", Action = "manage", Description = "Full role management" },
                
                // Product management permissions
                new { Name = "products:read", Module = "products", Action = "read", Description = "View products" },
                new { Name = "products:write", Module = "products", Action = "write", Description = "Create and edit products" },
                new { Name = "products:delete", Module = "products", Action = "delete", Description = "Delete products" },
                new { Name = "products:manage", Module = "products", Action = "manage", Description = "Full product management" },
                
                // Order management permissions
                new { Name = "orders:read", Module = "orders", Action = "read", Description = "View orders" },
                new { Name = "orders:write", Module = "orders", Action = "write", Description = "Create and edit orders" },
                new { Name = "orders:delete", Module = "orders", Action = "delete", Description = "Delete orders" },
                new { Name = "orders:manage", Module = "orders", Action = "manage", Description = "Full order management" },
                
                // Promotion management permissions
                new { Name = "promotions:read", Module = "promotions", Action = "read", Description = "View promotions" },
                new { Name = "promotions:write", Module = "promotions", Action = "write", Description = "Create and edit promotions" },
                new { Name = "promotions:delete", Module = "promotions", Action = "delete", Description = "Delete promotions" },
                new { Name = "promotions:manage", Module = "promotions", Action = "manage", Description = "Full promotion management" },
                
                // Settings permissions
                new { Name = "settings:read", Module = "settings", Action = "read", Description = "View settings" },
                new { Name = "settings:write", Module = "settings", Action = "write", Description = "Edit settings" },
                new { Name = "settings:manage", Module = "settings", Action = "manage", Description = "Full settings management" },
                
                // Logs and audit permissions
                new { Name = "logs:read", Module = "logs", Action = "read", Description = "View logs" },
                new { Name = "logs:manage", Module = "logs", Action = "manage", Description = "Full log management" },
                
                // Security permissions
                new { Name = "security:read", Module = "security", Action = "read", Description = "View security settings" },
                new { Name = "security:write", Module = "security", Action = "write", Description = "Edit security settings" },
                new { Name = "security:manage", Module = "security", Action = "manage", Description = "Full security management" }
            };

            var permissionList = new List<Permission>();
            
            foreach (var perm in permissionTemplates)
            {
                permissionList.Add(new Permission
                {
                    Id = permId++,
                    Module = perm.Module,
                    Action = perm.Action,
                    Name = perm.Name,
                    Description = perm.Description,
                    CreatedAt = seedDate
                });
            }
            
            modelBuilder.Entity<Permission>().HasData(permissionList);

            // --- Assign all permissions to SuperAdmin role ---
            var rolePermissions = new List<RolePermission>();
            for (int i = 1; i < permId; i++)
            {
                rolePermissions.Add(new RolePermission { RoleId = 100, PermissionId = i });
            }

            modelBuilder.Entity<RolePermission>().HasData(rolePermissions);

            // Seed Product Brands
            modelBuilder.Entity<ProductBrand>().HasData(
                new ProductBrand { Id = 1, Name = "Dell", Slug = "dell", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 2, Name = "ASUS", Slug = "asus", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 3, Name = "Logitech", Slug = "logitech", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 4, Name = "Apple", Slug = "apple", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 5, Name = "HP", Slug = "hp", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 6, Name = "Lenovo", Slug = "lenovo", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 7, Name = "Razer", Slug = "razer", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 8, Name = "Microsoft", Slug = "microsoft", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 9, Name = "Acer", Slug = "acer", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 10, Name = "LG", Slug = "lg", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 11, Name = "Samsung", Slug = "samsung", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 12, Name = "Framework", Slug = "framework", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 13, Name = "MSI", Slug = "msi", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 14, Name = "CalDigit", Slug = "caldigit", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 15, Name = "Anker", Slug = "anker", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 16, Name = "Sony", Slug = "sony", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 17, Name = "Cooler Master", Slug = "cooler-master", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 18, Name = "Tomtoc", Slug = "tomtoc", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductBrand { Id = 19, Name = "TechStore", Slug = "techstore", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate }
            );

            // Seed Product Categories
            modelBuilder.Entity<ProductCategory>().HasData(
                // Main Categories
                new ProductCategory { Id = 1, Name = "Laptops", Slug = "laptops", Description = "All kinds of laptops", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductCategory { Id = 2, Name = "Accessories", Slug = "accessories", Description = "Peripherals and accessories for your setup", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductCategory { Id = 3, Name = "Bundles", Slug = "bundles", Description = "Value-packed product bundles", IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },

                // Laptop Sub-categories (ParentId = 1)
                new ProductCategory { Id = 4, Name = "Gaming Laptops", Slug = "gaming-laptops", Description = "High-performance laptops for gaming", ParentId = 1, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductCategory { Id = 5, Name = "Ultrabooks", Slug = "ultrabooks", Description = "Thin and light laptops for portability", ParentId = 1, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductCategory { Id = 6, Name = "Business Laptops", Slug = "business-laptops", Description = "Reliable and secure laptops for work", ParentId = 1, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductCategory { Id = 7, Name = "Creator Laptops", Slug = "creator-laptops", Description = "Powerful laptops for creative professionals", ParentId = 1, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },

                // Accessories Sub-categories (ParentId = 2)
                new ProductCategory { Id = 8, Name = "Mice", Slug = "mice", Description = "Gaming and productivity mice", ParentId = 2, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductCategory { Id = 9, Name = "Keyboards", Slug = "keyboards", Description = "Mechanical and membrane keyboards", ParentId = 2, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductCategory { Id = 10, Name = "Monitors", Slug = "monitors", Description = "High-resolution and high-refresh-rate displays", ParentId = 2, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductCategory { Id = 11, Name = "Docking Stations", Slug = "docking-stations", Description = "Expand your laptop's connectivity", ParentId = 2, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductCategory { Id = 12, Name = "Headphones", Slug = "headphones", Description = "Noise-canceling and gaming headsets", ParentId = 2, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductCategory { Id = 13, Name = "External Storage", Slug = "external-storage", Description = "Portable SSDs and HDDs", ParentId = 2, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductCategory { Id = 14, Name = "Laptop Coolers", Slug = "laptop-coolers", Description = "Keep your laptop running cool", ParentId = 2, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductCategory { Id = 15, Name = "Laptop Sleeves", Slug = "laptop-sleeves", Description = "Protective cases for your laptop", ParentId = 2, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate },
                new ProductCategory { Id = 16, Name = "Power Banks", Slug = "power-banks", Description = "Charge your devices on the go", ParentId = 2, IsActive = true, CreatedAt = seedDate, UpdatedAt = seedDate }
            );

            // Seed sample laptop products
            modelBuilder.Entity<Laptop>().HasData(
                new Laptop
                {
                    Id = 1,
                    Name = "Dell XPS 13 Plus",
                    Description = "Premium ultrabook with latest Intel processor",
                    Brand = "Dell",
                    BrandId = 1, // Dell ProductBrand ID
                    CategoryId = 5, // Ultrabooks category
                    Model = "XPS 13 Plus",
                    Series = "XPS",
                    Price = 1299.99m,
                    SKU = "DELL-XPS13-001",
                    IsActive = true,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,

                    // CPU Specifications
                    CpuBrand = "Intel",
                    CpuModel = "Core i7-1360P",
                    CpuGeneration = "13th Gen",
                    CpuCores = 12,
                    CpuBaseClockGHz = 2.2m,
                    CpuBoostClockGHz = 5.0m,
                    CpuCache = "18MB",

                    // RAM Specifications
                    RamType = "LPDDR5",
                    RamCapacityGB = 16,
                    RamSlots = 1,
                    RamSpeed = 5200,
                    RamUpgradeable = false,

                    // Storage Specifications
                    StorageType = "SSD",
                    StorageCapacityGB = 512,
                    StorageInterface = "NVMe",
                    NvMeSupport = true,

                    // GPU Specifications
                    GpuType = "Integrated",
                    GpuBrand = "Intel",
                    GpuModel = "Iris Xe Graphics",
                    GpuVramGB = 0,

                    // Display Specifications
                    DisplaySizeInches = 13.4m,
                    DisplayResolution = "1920x1200",
                    DisplayPanelType = "IPS",
                    DisplayRefreshRateHz = 60,
                    DisplayTouchscreen = true,

                    // Battery & Physical
                    BatteryCapacityWh = 55,
                    WeightKg = 1.26m,
                    Dimensions = "295.3 x 199.04 x 15.28 mm",
                    Color = "Platinum Silver",

                    // Ports & Connectivity
                    Ports = "2x Thunderbolt 4, 1x Audio Jack",
                    WiFi6Support = true,
                    BluetoothSupport = true,
                    BluetoothVersion = "5.2",

                    // Business Information
                    WarrantyPeriod = "1 Year",
                    TargetAudience = "Business"
                },
                new Laptop
                {
                    Id = 2,
                    Name = "ASUS ROG Strix G15",
                    Description = "High-performance gaming laptop with RTX graphics",
                    Brand = "ASUS",
                    BrandId = 2, // ASUS ProductBrand ID
                    CategoryId = 4, // Gaming Laptops category
                    Model = "ROG Strix G15",
                    Series = "ROG",
                    Price = 1599.99m,
                    SKU = "ASUS-ROG-G15-001",
                    IsActive = true,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,

                    // CPU Specifications
                    CpuBrand = "AMD",
                    CpuModel = "Ryzen 7 6800H",
                    CpuGeneration = "6000 Series",
                    CpuCores = 8,
                    CpuBaseClockGHz = 3.2m,
                    CpuBoostClockGHz = 4.7m,
                    CpuCache = "20MB",

                    // RAM Specifications
                    RamType = "DDR5",
                    RamCapacityGB = 16,
                    RamSlots = 2,
                    RamSpeed = 4800,
                    RamUpgradeable = true,

                    // Storage Specifications
                    StorageType = "SSD",
                    StorageCapacityGB = 1000,
                    StorageInterface = "NVMe",
                    NvMeSupport = true,

                    // GPU Specifications
                    GpuType = "Discrete",
                    GpuBrand = "NVIDIA",
                    GpuModel = "RTX 3070 Ti",
                    GpuVramGB = 8,

                    // Display Specifications
                    DisplaySizeInches = 15.6m,
                    DisplayResolution = "1920x1080",
                    DisplayPanelType = "IPS",
                    DisplayRefreshRateHz = 144,
                    DisplayTouchscreen = false,

                    // Battery & Physical
                    BatteryCapacityWh = 90,
                    WeightKg = 2.3m,
                    Dimensions = "354 x 259 x 22.8 mm",
                    Color = "Eclipse Gray",

                    // Ports & Connectivity
                    Ports = "1x USB-C, 3x USB-A, 1x HDMI, 1x Audio Jack, 1x Ethernet",
                    WiFi6Support = true,
                    BluetoothSupport = true,
                    BluetoothVersion = "5.2",

                    // Business Information
                    WarrantyPeriod = "2 Years",
                    TargetAudience = "Gaming"
                }
            );

            // Seed sample accessories
            modelBuilder.Entity<Accessory>().HasData(
                new Accessory
                {
                    Id = 3,
                    Name = "Logitech MX Master 3S",
                    Description = "Advanced wireless mouse for productivity",
                    Brand = "Logitech",
                    BrandId = 3, // Logitech ProductBrand ID
                    CategoryId = 8, // Mice category
                    Model = "MX Master 3S",
                    Price = 99.99m,
                    SKU = "LOG-MX3S-001",
                    IsActive = true,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    AccessoryType = "Mouse",
                    Compatibility = "Universal",
                    Specifications = "{\"dpi\":\"8000\",\"battery\":\"70 days\",\"connectivity\":\"Bluetooth/USB-C\"}",
                    Color = "Graphite",
                    Connectivity = "Wireless"
                },
                new Accessory
                {
                    Id = 4,
                    Name = "Dell WD19 Docking Station",
                    Description = "Universal docking station with multiple ports",
                    Brand = "Dell",
                    BrandId = 1, // Dell ProductBrand ID
                    CategoryId = 11, // Docking Stations category
                    Model = "WD19",
                    Price = 199.99m,
                    SKU = "DELL-WD19-001",
                    IsActive = true,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    AccessoryType = "Docking Station",
                    Compatibility = "Dell Laptops",
                    Specifications = "{\"ports\":\"USB-C, USB-A, HDMI, DisplayPort, Ethernet\",\"power\":\"90W\"}",
                    Color = "Black",
                    Connectivity = "USB-C"
                }
            );

            // Seed sample bundle
            modelBuilder.Entity<Bundle>().HasData(
                new Bundle
                {
                    Id = 100,
                    Name = "Gaming Starter Bundle",
                    Description = "Complete gaming setup with Dell XPS 13 Plus and essential accessories",
                    Brand = "TechStore",
                    BrandId = 19, // TechStore ProductBrand ID
                    CategoryId = 3, // Bundles category
                    Model = "STARTER-001",
                    Price = 1399.99m,
                    SKU = "BUNDLE-STARTER-001",
                    IsActive = true,
                    CreatedAt = seedDate,
                    UpdatedAt = seedDate,
                    BundleType = "Gaming",
                    DiscountPercentage = 15.0m,
                    ValidFrom = seedDate,
                    ValidTo = new DateTime(2025, 10, 12, 0, 0, 0, DateTimeKind.Utc) // Fixed date instead of dynamic calculation
                }
            );

            // Seed bundle items
            modelBuilder.Entity<BundleItem>().HasData(
                new BundleItem { Id = 1, BundleId = 100, ProductId = 1, Quantity = 1, DiscountPercentage = 10.0m }, // Dell XPS 13 Plus
                new BundleItem { Id = 2, BundleId = 100, ProductId = 3, Quantity = 1, DiscountPercentage = 20.0m }  // Logitech MX Master 3S
            );

            // Seed inventory records
            modelBuilder.Entity<Inventory>().HasData(
                new Inventory { Id = 1, ProductId = 1, QuantityInStock = 50, ReservedQuantity = 0, ReorderLevel = 10, MaxStockLevel = 100, WarehouseLocation = "WH-A-001", LastStockUpdate = seedDate },
                new Inventory { Id = 2, ProductId = 2, QuantityInStock = 30, ReservedQuantity = 0, ReorderLevel = 5, MaxStockLevel = 50, WarehouseLocation = "WH-A-002", LastStockUpdate = seedDate },
                new Inventory { Id = 3, ProductId = 3, QuantityInStock = 100, ReservedQuantity = 0, ReorderLevel = 20, MaxStockLevel = 200, WarehouseLocation = "WH-B-001", LastStockUpdate = seedDate },
                new Inventory { Id = 4, ProductId = 4, QuantityInStock = 25, ReservedQuantity = 0, ReorderLevel = 5, MaxStockLevel = 50, WarehouseLocation = "WH-B-002", LastStockUpdate = seedDate },
                new Inventory { Id = 100, ProductId = 100, QuantityInStock = 10, ReservedQuantity = 0, ReorderLevel = 2, MaxStockLevel = 20, WarehouseLocation = "WH-BUNDLE-001", LastStockUpdate = seedDate }
            );

            SeedMoreProducts(modelBuilder, seedDate);

            // Seed sample coupon
            modelBuilder.Entity<Coupon>().HasData(
                new Coupon
                {
                    Id = 1,
                    Code = "WELCOME10",
                    Name = "Welcome Discount",
                    Description = "10% off for new customers",
                    Type = CouponType.Percentage,
                    Value = 10,
                    MinimumOrderAmount = 100,
                    MaximumDiscountAmount = 200,
                    UsageLimit = 1000,
                    UsedCount = 0,
                    ValidFrom = seedDate,
                    ValidTo = new DateTime(2025, 12, 12, 0, 0, 0, DateTimeKind.Utc), // Fixed date instead of dynamic calculation
                    IsActive = true,
                    CreatedAt = seedDate
                }
            );
        }

        private static void SeedMoreProducts(ModelBuilder modelBuilder, DateTime seedDate)
        {
            // This is a placeholder for potentially more complex product seeding logic
        }
    }
}
