using System;
using System.Text.Json.Serialization;

namespace EcommerceLaptop.Infrastructure.Search.Models;

/// <summary>
/// Typesense document model for product search
/// </summary>
public class ProductSearchDocument
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("brand")]
    public string Brand { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("sku")]
    public string SKU { get; set; } = string.Empty;

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; }

    [JsonPropertyName("product_type")]
    public string ProductType { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public long CreatedAt { get; set; } // Typesense prefers int64 timestamp

    [JsonPropertyName("updated_at")]
    public long UpdatedAt { get; set; }

    // Laptop specific (optional)
    [JsonPropertyName("cpu_brand")]
    public string? CpuBrand { get; set; }
    
    [JsonPropertyName("ram_gb")]
    public int? RamCapacityGB { get; set; }

    [JsonPropertyName("storage_gb")]
    public int? StorageCapacityGB { get; set; }
    
    [JsonPropertyName("screen_size")]
    public decimal? ScreenSizeInches { get; set; }

    // Aggregate fields
    [JsonPropertyName("average_rating")]
    public decimal? AverageRating { get; set; }

    [JsonPropertyName("review_count")]
    public int ReviewCount { get; set; }

    [JsonPropertyName("in_stock")]
    public bool InStock { get; set; }

    public static ProductSearchDocument FromProduct(EcommerceLaptop.Core.Entities.Product product)
    {
        var doc = new ProductSearchDocument
        {
            Id = product.Id.ToString(),
            Name = product.Name,
            Description = product.Description,
            Brand = product.Brand,
            Model = product.Model,
            Price = product.Price,
            SKU = product.SKU,
            IsActive = product.IsActive,
            ProductType = product.GetType().Name,
            CreatedAt = new DateTimeOffset(product.CreatedAt).ToUnixTimeSeconds(),
            UpdatedAt = new DateTimeOffset(product.UpdatedAt).ToUnixTimeSeconds(),
            InStock = (product.Inventory?.QuantityInStock ?? 0) > 0,
            ReviewCount = product.Reviews?.Count ?? 0
        };

        if (product is EcommerceLaptop.Core.Entities.Laptop laptop)
        {
            doc.CpuBrand = laptop.CpuBrand;
            doc.RamCapacityGB = laptop.RamCapacityGB;
            doc.StorageCapacityGB = laptop.StorageCapacityGB;
            doc.ScreenSizeInches = laptop.DisplaySizeInches;
        }

        if (product.Reviews?.Any() == true)
        {
            doc.AverageRating = (decimal)product.Reviews.Average(r => r.Rating);
        }

        return doc;
    }
}
