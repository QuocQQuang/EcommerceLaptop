namespace EcommerceLaptop.Core.Entities;

public abstract class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty; // Keep for backward compatibility
    public string Model { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string SKU { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // New relationships for category and brand management
    public int? CategoryId { get; set; }
    public int? BrandId { get; set; }

    // Variant support
    public int? ParentProductId { get; set; }  // NULL = base product, c gi tr = variant
    public string? VariantName { get; set; }   // "16GB RAM", "512GB SSD", etc.
    public string? VariantSku { get; set; }    // "DELL-XPS13-16GB-512GB"

    // Navigation properties
    public ProductCategory? Category { get; set; }
    public ProductBrand? ProductBrand { get; set; }
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public Inventory Inventory { get; set; } = null!;

    // Variant navigation properties
    public Product? ParentProduct { get; set; }
    public ICollection<Product> Variants { get; set; } = new List<Product>();

    // Computed properties
    public bool IsVariant => ParentProductId.HasValue;
    public bool IsBaseProduct => !ParentProductId.HasValue;
}

public class Laptop : Product
{
    public string Series { get; set; } = string.Empty;

    // CPU Specifications
    public string CpuBrand { get; set; } = string.Empty; // Intel, AMD
    public string CpuModel { get; set; } = string.Empty;
    public string CpuGeneration { get; set; } = string.Empty;
    public int CpuCores { get; set; }
    public decimal CpuBaseClockGHz { get; set; }
    public decimal CpuBoostClockGHz { get; set; }
    public string CpuCache { get; set; } = string.Empty;

    // RAM Specifications
    public string RamType { get; set; } = string.Empty; // DDR4, DDR5
    public int RamCapacityGB { get; set; }
    public int RamSlots { get; set; }
    public int RamSpeed { get; set; }
    public bool RamUpgradeable { get; set; }

    // Storage Specifications
    public string StorageType { get; set; } = string.Empty; // SSD, HDD, Hybrid
    public int StorageCapacityGB { get; set; }
    public string StorageInterface { get; set; } = string.Empty; // NVMe, SATA
    public bool NvMeSupport { get; set; }

    // GPU Specifications
    public string GpuType { get; set; } = string.Empty; // Integrated, Discrete
    public string GpuBrand { get; set; } = string.Empty; // NVIDIA, AMD, Intel
    public string GpuModel { get; set; } = string.Empty;
    public int GpuVramGB { get; set; }

    // Display Specifications
    public decimal DisplaySizeInches { get; set; }
    public string DisplayResolution { get; set; } = string.Empty; // 1920x1080, 2560x1440, etc.
    public string DisplayPanelType { get; set; } = string.Empty; // IPS, TN, OLED
    public int DisplayRefreshRateHz { get; set; }
    public bool DisplayTouchscreen { get; set; }

    // Battery & Physical
    public int BatteryCapacityWh { get; set; }
    public decimal WeightKg { get; set; }
    public string Dimensions { get; set; } = string.Empty; // L x W x H
    public string Color { get; set; } = string.Empty;

    // Ports & Connectivity
    public string Ports { get; set; } = string.Empty; // JSON string or comma-separated
    public bool WiFi6Support { get; set; }
    public bool BluetoothSupport { get; set; }
    public string BluetoothVersion { get; set; } = string.Empty;

    // Business Information
    public string WarrantyPeriod { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty; // Gaming, Business, Student, etc.
}

public class Accessory : Product
{
    public string AccessoryType { get; set; } = string.Empty; // Mouse, Keyboard, Headset, etc.
    public string Compatibility { get; set; } = string.Empty; // JSON string of compatible devices
    public string Specifications { get; set; } = string.Empty; // JSON string of specs
    public string Color { get; set; } = string.Empty;
    public string Connectivity { get; set; } = string.Empty; // Wired, Wireless, Bluetooth
}

public class Bundle : Product
{
    public string BundleType { get; set; } = string.Empty;
    public decimal DiscountPercentage { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    // Navigation properties
    public ICollection<BundleItem> BundleItems { get; set; } = new List<BundleItem>();
}

public class BundleItem
{
    public int Id { get; set; }
    public int BundleId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountPercentage { get; set; }

    // Navigation properties
    public Bundle Bundle { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class ProductImage
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }

    // ImgBB Integration fields
    public string? ImageId { get; set; } // ImgBB image ID for deletion
    public string? DeleteUrl { get; set; } // ImgBB delete URL
    public int DisplayOrder { get; set; } // Additional ordering field for ImgBB images

    // Navigation properties
    public Product Product { get; set; } = null!;
}

public class Review
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public int Rating { get; set; } // 1-5 stars
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsVerifiedPurchase { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;
}
