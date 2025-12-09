using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.DTOs;

#region Product DTOs

/// <summary>
/// Product specification data transfer object
/// </summary>
public class ProductSpecificationDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Base product data transfer object
/// </summary>
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string SKU { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string ProductType { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? ImageUrl { get; set; } // Primary image URL for backward compatibility
    public List<ProductImageDto> Images { get; set; } = new();
    public List<ProductSpecificationDto> Specifications { get; set; } = new();
    public InventoryDto? Inventory { get; set; }

    // Variant support
    public int? ParentProductId { get; set; }
    public string? VariantName { get; set; }
    public string? VariantSku { get; set; }
    public bool IsVariant { get; set; }
    public bool IsBaseProduct { get; set; }
    public List<ProductDto> Variants { get; set; } = new();
}

/// <summary>
/// Laptop-specific DTO with detailed specifications
/// </summary>
public class LaptopDto : ProductDto
{
    public string? Series { get; set; }

    // CPU Specifications
    public string? CpuBrand { get; set; }
    public string? CpuModel { get; set; }
    public string? CpuGeneration { get; set; }
    public int? CpuCores { get; set; }
    public decimal? CpuBaseClockGHz { get; set; }
    public decimal? CpuBoostClockGHz { get; set; }
    public string? CpuCache { get; set; }

    // RAM Specifications
    public string? RamType { get; set; }
    public int? RamCapacityGB { get; set; }
    public int? RamSlots { get; set; }
    public int? RamSpeed { get; set; }
    public bool? RamUpgradeable { get; set; }

    // Storage Specifications
    public string? StorageType { get; set; }
    public int? StorageCapacityGB { get; set; }
    public string? StorageInterface { get; set; }
    public bool? NvMeSupport { get; set; }

    // GPU Specifications
    public string? GpuType { get; set; }
    public string? GpuBrand { get; set; }
    public string? GpuModel { get; set; }
    public int? GpuVramGB { get; set; }

    // Display Specifications
    public decimal? DisplaySizeInches { get; set; }
    public string? DisplayResolution { get; set; }
    public string? DisplayPanelType { get; set; }
    public int? DisplayRefreshRateHz { get; set; }
    public bool? DisplayTouchscreen { get; set; }

    // Physical Specifications
    public int? BatteryCapacityWh { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Dimensions { get; set; }
    public string? Color { get; set; }
    public string? Ports { get; set; }

    // Connectivity
    public bool? WiFi6Support { get; set; }
    public bool? BluetoothSupport { get; set; }
    public string? BluetoothVersion { get; set; }

    // Other
    public string? WarrantyPeriod { get; set; }
    public string? TargetAudience { get; set; }
}

/// <summary>
/// DTO for creating product variants
/// </summary>
public class CreateVariantDto
{
    public string VariantName { get; set; } = string.Empty;
    public string VariantSku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? Description { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; } = true;

    // Laptop-specific variant properties - M RNG TT C THNG S
    public string? Series { get; set; }

    // CPU Specifications
    public string? CpuBrand { get; set; }
    public string? CpuModel { get; set; }
    public string? CpuGeneration { get; set; }
    public int? CpuCores { get; set; }
    public decimal? CpuBaseClockGHz { get; set; }
    public decimal? CpuBoostClockGHz { get; set; }
    public string? CpuCache { get; set; }

    // RAM Specifications
    public string? RamType { get; set; }
    public int? RamCapacityGB { get; set; }
    public int? RamSlots { get; set; }
    public int? RamSpeed { get; set; }
    public bool? RamUpgradeable { get; set; }

    // Storage Specifications
    public string? StorageType { get; set; }
    public int? StorageCapacityGB { get; set; }
    public string? StorageInterface { get; set; }
    public bool? NvMeSupport { get; set; }

    // GPU Specifications
    public string? GpuType { get; set; }
    public string? GpuBrand { get; set; }
    public string? GpuModel { get; set; }
    public int? GpuVramGB { get; set; }

    // Display Specifications
    public decimal? DisplaySizeInches { get; set; }
    public string? DisplayResolution { get; set; }
    public string? DisplayPanelType { get; set; }
    public int? DisplayRefreshRateHz { get; set; }
    public bool? DisplayTouchscreen { get; set; }

    // Physical Specifications
    public int? BatteryCapacityWh { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Dimensions { get; set; }
    public string? Color { get; set; }
    public string? Ports { get; set; }

    // Connectivity
    public bool? WiFi6Support { get; set; }
    public bool? BluetoothSupport { get; set; }
    public string? BluetoothVersion { get; set; }

    // Business info
    public string? WarrantyPeriod { get; set; }
    public string? TargetAudience { get; set; }
}

/// <summary>
/// DTO for updating product variants
/// </summary>
public class UpdateVariantDto
{
    public string? VariantName { get; set; }
    public string? VariantSku { get; set; }
    public decimal? Price { get; set; }
    public string? Description { get; set; }
    public int? StockQuantity { get; set; }
    public bool? IsActive { get; set; }

    // Laptop-specific variant properties - M RNG TT C THNG S
    public string? Series { get; set; }

    // CPU Specifications
    public string? CpuBrand { get; set; }
    public string? CpuModel { get; set; }
    public string? CpuGeneration { get; set; }
    public int? CpuCores { get; set; }
    public decimal? CpuBaseClockGHz { get; set; }
    public decimal? CpuBoostClockGHz { get; set; }
    public string? CpuCache { get; set; }

    // RAM Specifications
    public string? RamType { get; set; }
    public int? RamCapacityGB { get; set; }
    public int? RamSlots { get; set; }
    public int? RamSpeed { get; set; }
    public bool? RamUpgradeable { get; set; }

    // Storage Specifications
    public string? StorageType { get; set; }
    public int? StorageCapacityGB { get; set; }
    public string? StorageInterface { get; set; }
    public bool? NvMeSupport { get; set; }

    // GPU Specifications
    public string? GpuType { get; set; }
    public string? GpuBrand { get; set; }
    public string? GpuModel { get; set; }
    public int? GpuVramGB { get; set; }

    // Display Specifications
    public decimal? DisplaySizeInches { get; set; }
    public string? DisplayResolution { get; set; }
    public string? DisplayPanelType { get; set; }
    public int? DisplayRefreshRateHz { get; set; }
    public bool? DisplayTouchscreen { get; set; }

    // Physical Specifications
    public int? BatteryCapacityWh { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Dimensions { get; set; }
    public string? Color { get; set; }
    public string? Ports { get; set; }

    // Connectivity
    public bool? WiFi6Support { get; set; }
    public bool? BluetoothSupport { get; set; }
    public string? BluetoothVersion { get; set; }

    // Business info
    public string? WarrantyPeriod { get; set; }
    public string? TargetAudience { get; set; }
}

/// <summary>
/// DTO for variant summary (used in product listings)
/// </summary>
public class VariantSummaryDto
{
    public int Id { get; set; }
    public string VariantName { get; set; } = string.Empty;
    public string VariantSku { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }

    // Key differentiators
    public int? RamCapacityGB { get; set; }
    public int? StorageCapacityGB { get; set; }
    public string? Color { get; set; }
    public string? GpuModel { get; set; }
}

/// <summary>
/// Accessory DTO
/// </summary>
public class AccessoryDto : ProductDto
{
    public string? AccessoryType { get; set; }
    public string? Compatibility { get; set; }
    public string? SpecificationDetails { get; set; }
    public string? Color { get; set; }
    public string? Connectivity { get; set; }
}

/// <summary>
/// Bundle DTO
/// </summary>
public class BundleDto : ProductDto
{
    public string? BundleType { get; set; }
    public decimal? DiscountPercentage { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
    public List<BundleItemDto> BundleItems { get; set; } = new();
}

/// <summary>
/// Bundle item DTO with detailed pricing and availability
/// </summary>
public class BundleItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public string? ProductImageUrl { get; set; }
    public string Brand { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public int Quantity { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal DiscountedPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsAvailable { get; set; } = true;
    public int StockQuantity { get; set; }
}

/// <summary>
/// Product image DTO
/// </summary>
public class ProductImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }

    // ImgBB Integration fields
    public string? ImageId { get; set; } // ImgBB image ID for deletion
    public string? DeleteUrl { get; set; } // ImgBB delete URL
    public int DisplayOrder { get; set; } // Additional ordering field for ImgBB images
}

#endregion

#region Product Request DTOs

/// <summary>
/// Create product request DTO
/// </summary>
public class CreateProductRequest
{
    [Required]
    [StringLength(255)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Brand { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; set; }

    [Required]
    [StringLength(50)]
    public string SKU { get; set; } = string.Empty;

    [Required]
    public string ProductType { get; set; } = string.Empty; // "Laptop", "Accessory", "Bundle"
}

/// <summary>
/// Create laptop request DTO
/// </summary>
public class CreateLaptopRequest : CreateProductRequest
{
    public string? Series { get; set; }
    public string? CpuBrand { get; set; }
    public string? CpuModel { get; set; }
    public string? CpuGeneration { get; set; }
    public int? CpuCores { get; set; }
    public decimal? CpuBaseClockGHz { get; set; }
    public decimal? CpuBoostClockGHz { get; set; }
    public string? CpuCache { get; set; }
    public string? RamType { get; set; }
    public int? RamCapacityGB { get; set; }
    public int? RamSlots { get; set; }
    public int? RamSpeed { get; set; }
    public bool? RamUpgradeable { get; set; }
    public string? StorageType { get; set; }
    public int? StorageCapacityGB { get; set; }
    public string? StorageInterface { get; set; }
    public bool? NvMeSupport { get; set; }
    public string? GpuType { get; set; }
    public string? GpuBrand { get; set; }
    public string? GpuModel { get; set; }
    public int? GpuVramGB { get; set; }
    public decimal? DisplaySizeInches { get; set; }
    public string? DisplayResolution { get; set; }
    public string? DisplayPanelType { get; set; }
    public int? DisplayRefreshRateHz { get; set; }
    public bool? DisplayTouchscreen { get; set; }
    public int? BatteryCapacityWh { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Dimensions { get; set; }
    public string? Color { get; set; }
    public string? Ports { get; set; }
    public bool? WiFi6Support { get; set; }
    public bool? BluetoothSupport { get; set; }
    public string? BluetoothVersion { get; set; }
    public string? WarrantyPeriod { get; set; }
    public string? TargetAudience { get; set; }
}

/// <summary>
/// Create accessory request DTO
/// </summary>
public class CreateAccessoryRequest : CreateProductRequest
{
    public string? AccessoryType { get; set; }
    public string? Compatibility { get; set; }
    public string? SpecificationDetails { get; set; }
    public string? Color { get; set; }
    public string? Connectivity { get; set; }
}

/// <summary>
/// Update product request DTO
/// </summary>
public class UpdateProductRequest
{
    [StringLength(255)]
    public string? Name { get; set; }

    [StringLength(2000)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Brand { get; set; }

    [StringLength(100)]
    public string? Model { get; set; }

    [Range(0.01, double.MaxValue)]
    public decimal? Price { get; set; }

    public bool? IsActive { get; set; }

    // Add ProductType to help differentiate update requests
    public string? ProductType { get; set; }
}

/// <summary>
/// Update laptop request DTO
/// </summary>
public class UpdateLaptopRequest : UpdateProductRequest
{
    public string? Series { get; set; }
    public string? CpuBrand { get; set; }
    public string? CpuModel { get; set; }
    public string? CpuGeneration { get; set; }
    public int? CpuCores { get; set; }
    public decimal? CpuBaseClockGHz { get; set; }
    public decimal? CpuBoostClockGHz { get; set; }
    public string? CpuCache { get; set; }
    public string? RamType { get; set; }
    public int? RamCapacityGB { get; set; }
    public int? RamSlots { get; set; }
    public int? RamSpeed { get; set; }
    public bool? RamUpgradeable { get; set; }
    public string? StorageType { get; set; }
    public int? StorageCapacityGB { get; set; }
    public string? StorageInterface { get; set; }
    public bool? NvMeSupport { get; set; }
    public string? GpuType { get; set; }
    public string? GpuBrand { get; set; }
    public string? GpuModel { get; set; }
    public int? GpuVramGB { get; set; }
    public decimal? DisplaySizeInches { get; set; }
    public string? DisplayResolution { get; set; }
    public string? DisplayPanelType { get; set; }
    public int? DisplayRefreshRateHz { get; set; }
    public bool? DisplayTouchscreen { get; set; }
    public int? BatteryCapacityWh { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Dimensions { get; set; }
    public string? Color { get; set; }
    public string? Ports { get; set; }
    public bool? WiFi6Support { get; set; }
    public bool? BluetoothSupport { get; set; }
    public string? BluetoothVersion { get; set; }
    public string? WarrantyPeriod { get; set; }
    public string? TargetAudience { get; set; }
}

/// <summary>
/// Update accessory request DTO
/// </summary>
public class UpdateAccessoryRequest : UpdateProductRequest
{
    public string? AccessoryType { get; set; }
    public string? Compatibility { get; set; }
    public string? SpecificationDetails { get; set; }
    public string? Color { get; set; }
    public string? Connectivity { get; set; }
}

#endregion

#region T005 Enhanced Request DTOs

/// <summary>
/// Request DTO for creating product bundles
/// </summary>
public class CreateBundleRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    public List<int> ProductIds { get; set; } = new();

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

/// <summary>
/// Request DTO for calculating bundle pricing
/// </summary>
public class CalculateBundlePriceRequest
{
    [Required]
    public List<int> ProductIds { get; set; } = new();

    [Range(0, 100)]
    public decimal DiscountPercentage { get; set; }
}

/// <summary>
/// Request DTO for bulk pricing updates
/// </summary>
public class BulkPricingUpdateRequest
{
    [Required]
    public List<int> ProductIds { get; set; } = new();

    [Range(-100, 1000)]
    public decimal PriceAdjustmentPercentage { get; set; }
}

#endregion
