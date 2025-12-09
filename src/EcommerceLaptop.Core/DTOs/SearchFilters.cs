namespace EcommerceLaptop.Core.DTOs;

/// <summary>
/// Filter criteria for laptop searches
/// </summary>
public class LaptopSearchFilter
{
    public string? Brand { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? CpuBrand { get; set; }
    public string? CpuModel { get; set; }
    public int? MinCores { get; set; }
    public int? MaxCores { get; set; }
    public int? MinRam { get; set; }
    public string? RamType { get; set; }
    public string? StorageType { get; set; }
    public int? MinStorage { get; set; }
    public string? GpuBrand { get; set; }
    public string? GpuModel { get; set; }
    public decimal? MinDisplaySize { get; set; }
    public decimal? MaxDisplaySize { get; set; }
    public string? DisplayType { get; set; }
    public int? MinRefreshRate { get; set; }
    public bool? TouchScreen { get; set; }
}

/// <summary>
/// Filter criteria for accessory searches
/// </summary>
public class AccessorySearchFilter
{
    public string? Brand { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Category { get; set; }
    public string? Compatibility { get; set; }
    public int? ProductId { get; set; }
}

/// <summary>
/// Filter criteria for bundle searches
/// </summary>
public class BundleSearchFilter
{
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? BundleType { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}

/// <summary>
/// Available filter options for search
/// </summary>
public class SearchFilterOptions
{
    public List<string> Brands { get; set; } = new();
    public List<string> LaptopCpuBrands { get; set; } = new();
    public List<string> LaptopGpuBrands { get; set; } = new();
    public List<string> AccessoryCategories { get; set; } = new();
    public List<string> BundleTypes { get; set; } = new();
}

/// <summary>
/// Price range information
/// </summary>
public class PriceRange
{
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
}