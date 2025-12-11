using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.Products;

public class AdvancedLaptopSpecification : BaseSpecification<Laptop>
{
    public AdvancedLaptopSpecification(
        string? searchTerm,
        string? brand,
        decimal? minPrice,
        decimal? maxPrice,
        string? cpuBrand,
        string? cpuGeneration,
        int? minCpuCores,
        int? minRamCapacityGB,
        int? maxRamCapacityGB,
        string? ramType,
        string? storageType,
        int? minStorageCapacityGB,
        string? gpuType,
        string? gpuBrand,
        decimal? minDisplaySize,
        decimal? maxDisplaySize,
        string? displayResolution,
        int? minRefreshRate,
        bool? touchscreen,
        string? targetAudience,
        int? skip = null,
        int? take = null)
        : base(l => 
            (string.IsNullOrEmpty(searchTerm) || 
             l.Name.Contains(searchTerm) || 
             l.Brand.Contains(searchTerm) || 
             l.Model.Contains(searchTerm) || 
             l.Series.Contains(searchTerm) || 
             l.CpuModel.Contains(searchTerm) || 
             l.Description.Contains(searchTerm)) &&
            (string.IsNullOrEmpty(brand) || l.Brand == brand) &&
            (!minPrice.HasValue || l.Price >= minPrice.Value) &&
            (!maxPrice.HasValue || l.Price <= maxPrice.Value) &&
            (string.IsNullOrEmpty(cpuBrand) || l.CpuBrand == cpuBrand) &&
            (string.IsNullOrEmpty(cpuGeneration) || l.CpuGeneration == cpuGeneration) &&
            (!minCpuCores.HasValue || l.CpuCores >= minCpuCores.Value) &&
            (!minRamCapacityGB.HasValue || l.RamCapacityGB >= minRamCapacityGB.Value) &&
            (!maxRamCapacityGB.HasValue || l.RamCapacityGB <= maxRamCapacityGB.Value) &&
            (string.IsNullOrEmpty(ramType) || l.RamType == ramType) &&
            (string.IsNullOrEmpty(storageType) || l.StorageType == storageType) &&
            (!minStorageCapacityGB.HasValue || l.StorageCapacityGB >= minStorageCapacityGB.Value) &&
            (string.IsNullOrEmpty(gpuType) || l.GpuType == gpuType) &&
            (string.IsNullOrEmpty(gpuBrand) || l.GpuBrand == gpuBrand) &&
            (!minDisplaySize.HasValue || l.DisplaySizeInches >= minDisplaySize.Value) &&
            (!maxDisplaySize.HasValue || l.DisplaySizeInches <= maxDisplaySize.Value) &&
            (string.IsNullOrEmpty(displayResolution) || l.DisplayResolution == displayResolution) &&
            (!minRefreshRate.HasValue || l.DisplayRefreshRateHz >= minRefreshRate.Value) &&
            (!touchscreen.HasValue || l.DisplayTouchscreen == touchscreen.Value) &&
            (string.IsNullOrEmpty(targetAudience) || l.TargetAudience == targetAudience) &&
            l.IsActive
        )
    {
        AddInclude(l => l.Images); // Note: Original query filtered Primary images only, standard Include fetches all. Can refine if needed.
        // Spec Include with filter is supported in EF Core 5+ but BaseSpecification might need update to support Include(x => x.Collection.Where(...))
        // For now, loading all images is safe.
        AddInclude(l => l.Inventory);
        AddInclude(l => l.Category);
        AddInclude(l => l.Reviews);
        
        AddOrderBy(l => l.Brand);
        AddOrderBy(l => l.Price); // ThenBy

        if (skip.HasValue && take.HasValue)
        {
            ApplyPaging(skip.Value, take.Value);
        }
    }
}
