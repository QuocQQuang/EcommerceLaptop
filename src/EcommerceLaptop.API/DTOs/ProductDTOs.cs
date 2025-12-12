using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.DTOs;

#region Product DTOs

/// <summary>
/// Product specification data transfer object
/// </summary>
public record ProductSpecificationDto(
    int Id,
    string Name,
    string Value,
    string Category,
    int DisplayOrder);

/// <summary>
/// Base product data transfer object
/// </summary>
public record ProductDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string Brand { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string SKU { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public string ProductType { get; init; } = string.Empty;
    public int StockQuantity { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public string? ImageUrl { get; init; } // Primary image URL for backward compatibility
    public List<ProductImageDto> Images { get; init; } = new();
    public List<ProductSpecificationDto> Specifications { get; init; } = new();
    public InventoryDto? Inventory { get; init; }

    // Variant support
    public int? ParentProductId { get; init; }
    public string? VariantName { get; init; }
    public string? VariantSku { get; init; }
    public bool IsVariant { get; init; }
    public bool IsBaseProduct { get; init; }
    public List<ProductDto> Variants { get; init; } = new();
}

/// <summary>
/// Laptop-specific DTO with detailed specifications
/// </summary>
public record LaptopDto : ProductDto
{
    public string? Series { get; init; }

    // CPU Specifications
    public string? CpuBrand { get; init; }
    public string? CpuModel { get; init; }
    public string? CpuGeneration { get; init; }
    public int? CpuCores { get; init; }
    public decimal? CpuBaseClockGHz { get; init; }
    public decimal? CpuBoostClockGHz { get; init; }
    public string? CpuCache { get; init; }

    // RAM Specifications
    public string? RamType { get; init; }
    public int? RamCapacityGB { get; init; }
    public int? RamSlots { get; init; }
    public int? RamSpeed { get; init; }
    public bool? RamUpgradeable { get; init; }

    // Storage Specifications
    public string? StorageType { get; init; }
    public int? StorageCapacityGB { get; init; }
    public string? StorageInterface { get; init; }
    public bool? NvMeSupport { get; init; }

    // GPU Specifications
    public string? GpuType { get; init; }
    public string? GpuBrand { get; init; }
    public string? GpuModel { get; init; }
    public int? GpuVramGB { get; init; }

    // Display Specifications
    public decimal? DisplaySizeInches { get; init; }
    public string? DisplayResolution { get; init; }
    public string? DisplayPanelType { get; init; }
    public int? DisplayRefreshRateHz { get; init; }
    public bool? DisplayTouchscreen { get; init; }

    // Physical Specifications
    public int? BatteryCapacityWh { get; init; }
    public decimal? WeightKg { get; init; }
    public string? Dimensions { get; init; }
    public string? Color { get; init; }
    public string? Ports { get; init; }

    // Connectivity
    public bool? WiFi6Support { get; init; }
    public bool? BluetoothSupport { get; init; }
    public string? BluetoothVersion { get; init; }

    // Other
    public string? WarrantyPeriod { get; init; }
    public string? TargetAudience { get; init; }
}

/// <summary>
/// DTO for creating product variants
/// </summary>
public record CreateVariantDto
{
    public string VariantName { get; init; } = string.Empty;
    public string VariantSku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public string? Description { get; init; }
    public int StockQuantity { get; init; }
    public bool IsActive { get; init; } = true;

    // Laptop-specific variant properties - M RNG TT C THNG S
    public string? Series { get; init; }

    // CPU Specifications
    public string? CpuBrand { get; init; }
    public string? CpuModel { get; init; }
    public string? CpuGeneration { get; init; }
    public int? CpuCores { get; init; }
    public decimal? CpuBaseClockGHz { get; init; }
    public decimal? CpuBoostClockGHz { get; init; }
    public string? CpuCache { get; init; }

    // RAM Specifications
    public string? RamType { get; init; }
    public int? RamCapacityGB { get; init; }
    public int? RamSlots { get; init; }
    public int? RamSpeed { get; init; }
    public bool? RamUpgradeable { get; init; }

    // Storage Specifications
    public string? StorageType { get; init; }
    public int? StorageCapacityGB { get; init; }
    public string? StorageInterface { get; init; }
    public bool? NvMeSupport { get; init; }

    // GPU Specifications
    public string? GpuType { get; init; }
    public string? GpuBrand { get; init; }
    public string? GpuModel { get; init; }
    public int? GpuVramGB { get; init; }

    // Display Specifications
    public decimal? DisplaySizeInches { get; init; }
    public string? DisplayResolution { get; init; }
    public string? DisplayPanelType { get; init; }
    public int? DisplayRefreshRateHz { get; init; }
    public bool? DisplayTouchscreen { get; init; }

    // Physical Specifications
    public int? BatteryCapacityWh { get; init; }
    public decimal? WeightKg { get; init; }
    public string? Dimensions { get; init; }
    public string? Color { get; init; }
    public string? Ports { get; init; }

    // Connectivity
    public bool? WiFi6Support { get; init; }
    public bool? BluetoothSupport { get; init; }
    public string? BluetoothVersion { get; init; }

    // Business info
    public string? WarrantyPeriod { get; init; }
    public string? TargetAudience { get; init; }
}

/// <summary>
/// DTO for updating product variants
/// </summary>
public record UpdateVariantDto
{
    public string? VariantName { get; init; }
    public string? VariantSku { get; init; }
    public decimal? Price { get; init; }
    public string? Description { get; init; }
    public int? StockQuantity { get; init; }
    public bool? IsActive { get; init; }

    // Laptop-specific variant properties - M RNG TT C THNG S
    public string? Series { get; init; }

    // CPU Specifications
    public string? CpuBrand { get; init; }
    public string? CpuModel { get; init; }
    public string? CpuGeneration { get; init; }
    public int? CpuCores { get; init; }
    public decimal? CpuBaseClockGHz { get; init; }
    public decimal? CpuBoostClockGHz { get; init; }
    public string? CpuCache { get; init; }

    // RAM Specifications
    public string? RamType { get; init; }
    public int? RamCapacityGB { get; init; }
    public int? RamSlots { get; init; }
    public int? RamSpeed { get; init; }
    public bool? RamUpgradeable { get; init; }

    // Storage Specifications
    public string? StorageType { get; init; }
    public int? StorageCapacityGB { get; init; }
    public string? StorageInterface { get; init; }
    public bool? NvMeSupport { get; init; }

    // GPU Specifications
    public string? GpuType { get; init; }
    public string? GpuBrand { get; init; }
    public string? GpuModel { get; init; }
    public int? GpuVramGB { get; init; }

    // Display Specifications
    public decimal? DisplaySizeInches { get; init; }
    public string? DisplayResolution { get; init; }
    public string? DisplayPanelType { get; init; }
    public int? DisplayRefreshRateHz { get; init; }
    public bool? DisplayTouchscreen { get; init; }

    // Physical Specifications
    public int? BatteryCapacityWh { get; init; }
    public decimal? WeightKg { get; init; }
    public string? Dimensions { get; init; }
    public string? Color { get; init; }
    public string? Ports { get; init; }

    // Connectivity
    public bool? WiFi6Support { get; init; }
    public bool? BluetoothSupport { get; init; }
    public string? BluetoothVersion { get; init; }

    // Business info
    public string? WarrantyPeriod { get; init; }
    public string? TargetAudience { get; init; }
}

/// <summary>
/// DTO for variant summary (used in product listings)
/// </summary>
public record VariantSummaryDto
{
    public int Id { get; init; }
    public string VariantName { get; init; } = string.Empty;
    public string VariantSku { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int StockQuantity { get; init; }
    public bool IsActive { get; init; }
    public string? ImageUrl { get; init; }

    // Key differentiators
    public int? RamCapacityGB { get; init; }
    public int? StorageCapacityGB { get; init; }
    public string? Color { get; init; }
    public string? GpuModel { get; init; }
}

/// <summary>
/// Accessory DTO
/// </summary>
public record AccessoryDto : ProductDto
{
    public string? AccessoryType { get; init; }
    public string? Compatibility { get; init; }
    public string? SpecificationDetails { get; init; }
    public string? Color { get; init; }
    public string? Connectivity { get; init; }
}

/// <summary>
/// Bundle DTO
/// </summary>
public record BundleDto : ProductDto
{
    public string? BundleType { get; init; }
    public decimal? DiscountPercentage { get; init; }
    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
    public List<BundleItemDto> BundleItems { get; init; } = new();
}

/// <summary>
/// Bundle item DTO with detailed pricing and availability
/// </summary>
public record BundleItemDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string ProductSku { get; init; } = string.Empty;
    public string? ProductImageUrl { get; init; }
    public string Brand { get; init; } = string.Empty;
    public decimal OriginalPrice { get; init; }
    public int Quantity { get; init; }
    public decimal DiscountPercentage { get; init; }
    public decimal DiscountedPrice { get; init; }
    public decimal TotalPrice { get; init; }
    public bool IsAvailable { get; init; } = true;
    public int StockQuantity { get; init; }
}

/// <summary>
/// Product image DTO
/// </summary>
public record ProductImageDto
{
    public int Id { get; init; }
    public string ImageUrl { get; init; } = string.Empty;
    public string AltText { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public bool IsPrimary { get; init; }

    // ImgBB Integration fields
    public string? ImageId { get; init; } // ImgBB image ID for deletion
    public string? DeleteUrl { get; init; } // ImgBB delete URL
    public int DisplayOrder { get; init; } // Additional ordering field for ImgBB images
}

#endregion

#region Product Request DTOs

/// <summary>
/// Create product request DTO
/// </summary>
public record CreateProductRequest
{
    [Required]
    [StringLength(255)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(2000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Brand { get; init; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Model { get; init; } = string.Empty;

    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal Price { get; init; }

    [Required]
    [StringLength(50)]
    public string SKU { get; init; } = string.Empty;

    [Required]
    public string ProductType { get; init; } = string.Empty; // "Laptop", "Accessory", "Bundle"
}

/// <summary>
/// Create laptop request DTO
/// </summary>
public record CreateLaptopRequest : CreateProductRequest
{
    public string? Series { get; init; }
    public string? CpuBrand { get; init; }
    public string? CpuModel { get; init; }
    public string? CpuGeneration { get; init; }
    public int? CpuCores { get; init; }
    public decimal? CpuBaseClockGHz { get; init; }
    public decimal? CpuBoostClockGHz { get; init; }
    public string? CpuCache { get; init; }
    public string? RamType { get; init; }
    public int? RamCapacityGB { get; init; }
    public int? RamSlots { get; init; }
    public int? RamSpeed { get; init; }
    public bool? RamUpgradeable { get; init; }
    public string? StorageType { get; init; }
    public int? StorageCapacityGB { get; init; }
    public string? StorageInterface { get; init; }
    public bool? NvMeSupport { get; init; }
    public string? GpuType { get; init; }
    public string? GpuBrand { get; init; }
    public string? GpuModel { get; init; }
    public int? GpuVramGB { get; init; }
    public decimal? DisplaySizeInches { get; init; }
    public string? DisplayResolution { get; init; }
    public string? DisplayPanelType { get; init; }
    public int? DisplayRefreshRateHz { get; init; }
    public bool? DisplayTouchscreen { get; init; }
    public int? BatteryCapacityWh { get; init; }
    public decimal? WeightKg { get; init; }
    public string? Dimensions { get; init; }
    public string? Color { get; init; }
    public string? Ports { get; init; }
    public bool? WiFi6Support { get; init; }
    public bool? BluetoothSupport { get; init; }
    public string? BluetoothVersion { get; init; }
    public string? WarrantyPeriod { get; init; }
    public string? TargetAudience { get; init; }
}

/// <summary>
/// Create accessory request DTO
/// </summary>
public record CreateAccessoryRequest : CreateProductRequest
{
    public string? AccessoryType { get; init; }
    public string? Compatibility { get; init; }
    public string? SpecificationDetails { get; init; }
    public string? Color { get; init; }
    public string? Connectivity { get; init; }
}

/// <summary>
/// Update product request DTO
/// </summary>
public record UpdateProductRequest
{
    [StringLength(255)]
    public string? Name { get; init; }

    [StringLength(2000)]
    public string? Description { get; init; }

    [StringLength(100)]
    public string? Brand { get; init; }

    [StringLength(100)]
    public string? Model { get; init; }

    [Range(0.01, double.MaxValue)]
    public decimal? Price { get; init; }

    public bool? IsActive { get; init; }

    // Add ProductType to help differentiate update requests
    public string? ProductType { get; init; }
}

/// <summary>
/// Update laptop request DTO
/// </summary>
public record UpdateLaptopRequest : UpdateProductRequest
{
    public string? Series { get; init; }
    public string? CpuBrand { get; init; }
    public string? CpuModel { get; init; }
    public string? CpuGeneration { get; init; }
    public int? CpuCores { get; init; }
    public decimal? CpuBaseClockGHz { get; init; }
    public decimal? CpuBoostClockGHz { get; init; }
    public string? CpuCache { get; init; }
    public string? RamType { get; init; }
    public int? RamCapacityGB { get; init; }
    public int? RamSlots { get; init; }
    public int? RamSpeed { get; init; }
    public bool? RamUpgradeable { get; init; }
    public string? StorageType { get; init; }
    public int? StorageCapacityGB { get; init; }
    public string? StorageInterface { get; init; }
    public bool? NvMeSupport { get; init; }
    public string? GpuType { get; init; }
    public string? GpuBrand { get; init; }
    public string? GpuModel { get; init; }
    public int? GpuVramGB { get; init; }
    public decimal? DisplaySizeInches { get; init; }
    public string? DisplayResolution { get; init; }
    public string? DisplayPanelType { get; init; }
    public int? DisplayRefreshRateHz { get; init; }
    public bool? DisplayTouchscreen { get; init; }
    public int? BatteryCapacityWh { get; init; }
    public decimal? WeightKg { get; init; }
    public string? Dimensions { get; init; }
    public string? Color { get; init; }
    public string? Ports { get; init; }
    public bool? WiFi6Support { get; init; }
    public bool? BluetoothSupport { get; init; }
    public string? BluetoothVersion { get; init; }
    public string? WarrantyPeriod { get; init; }
    public string? TargetAudience { get; init; }
}

/// <summary>
/// Update accessory request DTO
/// </summary>
public record UpdateAccessoryRequest : UpdateProductRequest
{
    public string? AccessoryType { get; init; }
    public string? Compatibility { get; init; }
    public string? SpecificationDetails { get; init; }
    public string? Color { get; init; }
    public string? Connectivity { get; init; }
}

#endregion

#region T005 Enhanced Request DTOs

/// <summary>
/// Request DTO for creating product bundles
/// </summary>
public record CreateBundleRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [StringLength(1000)]
    public string Description { get; init; } = string.Empty;

    [Required]
    public List<int> ProductIds { get; init; } = new();

    [Range(0, 100)]
    public decimal DiscountPercentage { get; init; }

    public DateTime? ValidFrom { get; init; }
    public DateTime? ValidTo { get; init; }
}

/// <summary>
/// Request DTO for calculating bundle pricing
/// </summary>
public record CalculateBundlePriceRequest
{
    [Required]
    public List<int> ProductIds { get; init; } = new();

    [Range(0, 100)]
    public decimal DiscountPercentage { get; init; }
}

/// <summary>
/// Request DTO for bulk pricing updates
/// </summary>
public record BulkPricingUpdateRequest
{
    [Required]
    public List<int> ProductIds { get; init; } = new();

    [Range(-100, 1000)]
    public decimal PriceAdjustmentPercentage { get; init; }
}

#endregion
