using Nest;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Infrastructure.Search.Models;

/// <summary>
/// Elasticsearch document model for product search
/// Optimized for search performance and relevancy
/// </summary>
[ElasticsearchType(RelationName = "product")]
public class ProductSearchDocument
{
    /// <summary>
    /// Product ID
    /// </summary>
    [Keyword]
    public int Id { get; set; }

    /// <summary>
    /// Product name with enhanced search capabilities
    /// </summary>
    [Text(Analyzer = "standard", SearchAnalyzer = "standard")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Product description for full-text search
    /// </summary>
    [Text(Analyzer = "standard")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Product brand
    /// </summary>
    [Keyword]
    public string Brand { get; set; } = string.Empty;

    /// <summary>
    /// Product model
    /// </summary>
    [Keyword]
    public string Model { get; set; } = string.Empty;

    /// <summary>
    /// Product price for range queries
    /// </summary>
    [Number(NumberType.Double)]
    public decimal Price { get; set; }

    /// <summary>
    /// Product SKU
    /// </summary>
    [Keyword]
    public string SKU { get; set; } = string.Empty;

    /// <summary>
    /// Whether product is active
    /// </summary>
    [Boolean]
    public bool IsActive { get; set; }

    /// <summary>
    /// Product type discriminator
    /// </summary>
    [Keyword]
    public string ProductType { get; set; } = string.Empty;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [Date]
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last update timestamp
    /// </summary>
    [Date]
    public DateTime UpdatedAt { get; set; }

    // Laptop-specific properties
    /// <summary>
    /// Laptop series
    /// </summary>
    [Keyword]
    public string? Series { get; set; }

    /// <summary>
    /// CPU brand (Intel, AMD)
    /// </summary>
    [Keyword]
    public string? CpuBrand { get; set; }

    /// <summary>
    /// CPU model
    /// </summary>
    [Keyword]
    public string? CpuModel { get; set; }

    /// <summary>
    /// CPU generation
    /// </summary>
    [Keyword]
    public string? CpuGeneration { get; set; }

    /// <summary>
    /// Number of CPU cores
    /// </summary>
    [Number(NumberType.Integer)]
    public int? CpuCores { get; set; }

    /// <summary>
    /// RAM type (DDR4, DDR5)
    /// </summary>
    [Keyword]
    public string? RamType { get; set; }

    /// <summary>
    /// RAM capacity in GB
    /// </summary>
    [Number(NumberType.Integer)]
    public int? RamCapacityGB { get; set; }

    /// <summary>
    /// Storage type (SSD, HDD, Hybrid)
    /// </summary>
    [Keyword]
    public string? StorageType { get; set; }

    /// <summary>
    /// Storage capacity in GB
    /// </summary>
    [Number(NumberType.Integer)]
    public int? StorageCapacityGB { get; set; }

    /// <summary>
    /// GPU type (Integrated, Discrete)
    /// </summary>
    [Keyword]
    public string? GpuType { get; set; }

    /// <summary>
    /// GPU brand (NVIDIA, AMD, Intel)
    /// </summary>
    [Keyword]
    public string? GpuBrand { get; set; }

    /// <summary>
    /// GPU model
    /// </summary>
    [Keyword]
    public string? GpuModel { get; set; }

    /// <summary>
    /// Screen size in inches
    /// </summary>
    [Number(NumberType.Double)]
    public decimal? ScreenSizeInches { get; set; }

    /// <summary>
    /// Screen resolution
    /// </summary>
    [Keyword]
    public string? ScreenResolution { get; set; }

    /// <summary>
    /// Weight in kg
    /// </summary>
    [Number(NumberType.Double)]
    public decimal? WeightKg { get; set; }

    /// <summary>
    /// Operating system
    /// </summary>
    [Keyword]
    public string? OperatingSystem { get; set; }

    /// <summary>
    /// Available colors
    /// </summary>
    [Keyword]
    public string[]? AvailableColors { get; set; }

    /// <summary>
    /// Product tags for enhanced searchability
    /// </summary>
    [Text(Analyzer = "keyword")]
    public string[]? Tags { get; set; }

    /// <summary>
    /// Average rating
    /// </summary>
    [Number(NumberType.Double)]
    public decimal? AverageRating { get; set; }

    /// <summary>
    /// Review count
    /// </summary>
    [Number(NumberType.Integer)]
    public int ReviewCount { get; set; }

    /// <summary>
    /// Stock quantity
    /// </summary>
    [Number(NumberType.Integer)]
    public int StockQuantity { get; set; }

    /// <summary>
    /// Whether product is in stock
    /// </summary>
    [Boolean]
    public bool InStock { get; set; }

    /// <summary>
    /// Converts a Product entity to ProductSearchDocument
    /// </summary>
    public static ProductSearchDocument FromProduct(Product product)
    {
        var document = new ProductSearchDocument
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Brand = product.Brand,
            Model = product.Model,
            Price = product.Price,
            SKU = product.SKU,
            IsActive = product.IsActive,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            ProductType = product.GetType().Name,
            StockQuantity = product.Inventory?.QuantityInStock ?? 0,
            InStock = (product.Inventory?.QuantityInStock ?? 0) > 0,
            ReviewCount = product.Reviews?.Count ?? 0
        };

        // Map laptop-specific properties
        if (product is Laptop laptop)
        {
            document.Series = laptop.Series;
            document.CpuBrand = laptop.CpuBrand;
            document.CpuModel = laptop.CpuModel;
            document.CpuGeneration = laptop.CpuGeneration;
            document.CpuCores = laptop.CpuCores;
            document.RamType = laptop.RamType;
            document.RamCapacityGB = laptop.RamCapacityGB;
            document.StorageType = laptop.StorageType;
            document.StorageCapacityGB = laptop.StorageCapacityGB;
            document.GpuType = laptop.GpuType;
            document.GpuBrand = laptop.GpuBrand;
            document.GpuModel = laptop.GpuModel;
            document.ScreenSizeInches = laptop.DisplaySizeInches;
            document.ScreenResolution = laptop.DisplayResolution;
            document.WeightKg = laptop.WeightKg;
            // Note: OperatingSystem and AvailableColors properties don't exist in the current Laptop entity
            // document.OperatingSystem = laptop.OperatingSystem;
            // document.AvailableColors = laptop.AvailableColors?.ToArray();
        }

        // Calculate average rating if reviews exist
        if (product.Reviews?.Any() == true)
        {
            document.AverageRating = (decimal)product.Reviews.Average(r => r.Rating);
        }

        return document;
    }

    /// <summary>
    /// Creates search tags based on product properties
    /// </summary>
    public void GenerateSearchTags()
    {
        var tags = new List<string>();
        if (!string.IsNullOrEmpty(Brand)) tags.Add(Brand.ToLower());
        if (!string.IsNullOrEmpty(Model)) tags.Add(Model.ToLower());
        if (!string.IsNullOrEmpty(ProductType)) tags.Add(ProductType.ToLower());

        if (!string.IsNullOrEmpty(CpuBrand))
            tags.Add($"cpu-{CpuBrand.ToLower()}");

        if (!string.IsNullOrEmpty(GpuBrand))
            tags.Add($"gpu-{GpuBrand.ToLower()}");

        if (RamCapacityGB.HasValue)
            tags.Add($"ram-{RamCapacityGB}gb");

        if (StorageCapacityGB.HasValue)
            tags.Add($"storage-{StorageCapacityGB}gb");

        if (!string.IsNullOrEmpty(StorageType))
            tags.Add($"storage-{StorageType.ToLower()}");

        Tags = tags.Where(t => !string.IsNullOrEmpty(t)).ToArray();
    }
}
