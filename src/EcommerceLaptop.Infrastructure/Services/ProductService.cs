using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Product service implementation with comprehensive product management
/// Implements repository pattern with Entity Framework Core
/// </summary>
public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;

    public ProductService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        // TPT Optimization: Use single query with appropriate includes for better performance
        // EF Core will generate optimal JOIN queries based on TPT structure
        return await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Inventory)
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    /// <summary>
    /// TPT Optimized version with type-specific details
    /// Use this when you need full type-specific information
    /// </summary>
    public async Task<Product?> GetByIdWithDetailsAsync(int id)
    {
        // This method provides type-specific optimization for TPT
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null) return null;

        // Load type-specific details based on actual type
        return product switch
        {
            Laptop => await _context.Laptops
                .Include(l => l.Images)
                .Include(l => l.Inventory)
                .Include(l => l.Category)
                .FirstOrDefaultAsync(l => l.Id == id),
            Accessory => await _context.Accessories
                .Include(a => a.Images)
                .Include(a => a.Inventory)
                .Include(a => a.Category)
                .FirstOrDefaultAsync(a => a.Id == id),
            Bundle => await _context.Bundles
                .Include(b => b.Images)
                .Include(b => b.Inventory)
                .Include(b => b.Category)
                .Include(b => b.BundleItems)
                    .ThenInclude(bi => bi.Product)
                .FirstOrDefaultAsync(b => b.Id == id),
            _ => product
        };
    }

    public async Task<PagedResult<Product>> GetProductsAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? productType = null,
        string? brand = null,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isActive = null,
        string? sortBy = null)
    {
        // TPT Optimization: Use specific type queries when productType is specified
        // This leverages TPT's table-per-type structure for better performance
        if (!string.IsNullOrEmpty(productType))
        {
            switch (productType.ToLowerInvariant())
            {
                case "laptop":
                    return await GetLaptopsPagedAsync(page, pageSize, searchTerm, brand, minPrice, maxPrice, isActive, category);
                case "accessory":
                    return await GetAccessoriesPagedAsync(page, pageSize, searchTerm, brand, minPrice, maxPrice, isActive, category);
                case "bundle":
                    return await GetBundlesPagedAsync(page, pageSize, searchTerm, brand, minPrice, maxPrice, isActive, category);
            }
        }

        // General query when no specific type filter is applied
        var query = _context.Products
            .Include(p => p.Images)
            .Include(p => p.Inventory)
            .Include(p => p.ProductBrand)
            .Include(p => p.Category)
            .AsQueryable();

        // Apply common filters
        query = ApplyCommonFilters(query, searchTerm, brand, minPrice, maxPrice, isActive, category);

        // Apply sorting
        query = sortBy?.ToLower() switch
        {
            "price_asc" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "popularity" => query.OrderByDescending(p => p.OrderItems.Count),
            "newest" => query.OrderByDescending(p => p.CreatedAt),
            _ => query.OrderBy(p => p.Name) // Default sort by name
        };

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Laptop>> GetLaptopsAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? brand = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? cpuBrand = null,
        int? ramCapacityGB = null,
        string? storageType = null)
    {
        var query = _context.Laptops
            .Include(l => l.Images.Where(pi => pi.IsPrimary))
            .Include(l => l.Inventory)
            .Include(l => l.Category)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(l =>
                l.Name.Contains(searchTerm) ||
                l.Brand.Contains(searchTerm) ||
                l.Model.Contains(searchTerm) ||
                l.Series.Contains(searchTerm) ||
                l.CpuModel.Contains(searchTerm) ||
                l.Description.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(brand))
        {
            query = query.Where(l => l.Brand == brand);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(l => l.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(l => l.Price <= maxPrice.Value);
        }

        if (!string.IsNullOrEmpty(cpuBrand))
        {
            query = query.Where(l => l.CpuBrand == cpuBrand);
        }

        if (ramCapacityGB.HasValue)
        {
            query = query.Where(l => l.RamCapacityGB >= ramCapacityGB.Value);
        }

        if (!string.IsNullOrEmpty(storageType))
        {
            query = query.Where(l => l.StorageType == storageType);
        }

        query = query.Where(l => l.IsActive);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(l => l.Brand)
            .ThenBy(l => l.Price)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Laptop>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Advanced laptop filtering with comprehensive hardware specifications
    /// Implements T005 requirement for detailed laptop specification filtering
    /// </summary>
    public async Task<PagedResult<Laptop>> GetLaptopsWithAdvancedFilteringAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? brand = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        string? cpuBrand = null,
        string? cpuGeneration = null,
        int? minCpuCores = null,
        int? minRamCapacityGB = null,
        int? maxRamCapacityGB = null,
        string? ramType = null,
        string? storageType = null,
        int? minStorageCapacityGB = null,
        string? gpuType = null,
        string? gpuBrand = null,
        decimal? minDisplaySize = null,
        decimal? maxDisplaySize = null,
        string? displayResolution = null,
        int? minRefreshRate = null,
        bool? touchscreen = null,
        string? targetAudience = null)
    {
        var query = _context.Laptops
            .Include(l => l.Images.Where(pi => pi.IsPrimary))
            .Include(l => l.Inventory)
            .Include(l => l.Category)
            .Include(l => l.Reviews)
            .AsQueryable();

        // Basic filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(l =>
                l.Name.Contains(searchTerm) ||
                l.Brand.Contains(searchTerm) ||
                l.Model.Contains(searchTerm) ||
                l.Series.Contains(searchTerm) ||
                l.CpuModel.Contains(searchTerm) ||
                l.Description.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(brand))
            query = query.Where(l => l.Brand == brand);

        if (minPrice.HasValue)
            query = query.Where(l => l.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(l => l.Price <= maxPrice.Value);

        // CPU filters
        if (!string.IsNullOrEmpty(cpuBrand))
            query = query.Where(l => l.CpuBrand == cpuBrand);

        if (!string.IsNullOrEmpty(cpuGeneration))
            query = query.Where(l => l.CpuGeneration == cpuGeneration);

        if (minCpuCores.HasValue)
            query = query.Where(l => l.CpuCores >= minCpuCores.Value);

        // RAM filters
        if (minRamCapacityGB.HasValue)
            query = query.Where(l => l.RamCapacityGB >= minRamCapacityGB.Value);

        if (maxRamCapacityGB.HasValue)
            query = query.Where(l => l.RamCapacityGB <= maxRamCapacityGB.Value);

        if (!string.IsNullOrEmpty(ramType))
            query = query.Where(l => l.RamType == ramType);

        // Storage filters
        if (!string.IsNullOrEmpty(storageType))
            query = query.Where(l => l.StorageType == storageType);

        if (minStorageCapacityGB.HasValue)
            query = query.Where(l => l.StorageCapacityGB >= minStorageCapacityGB.Value);

        // GPU filters
        if (!string.IsNullOrEmpty(gpuType))
            query = query.Where(l => l.GpuType == gpuType);

        if (!string.IsNullOrEmpty(gpuBrand))
            query = query.Where(l => l.GpuBrand == gpuBrand);

        // Display filters
        if (minDisplaySize.HasValue)
            query = query.Where(l => l.DisplaySizeInches >= minDisplaySize.Value);

        if (maxDisplaySize.HasValue)
            query = query.Where(l => l.DisplaySizeInches <= maxDisplaySize.Value);

        if (!string.IsNullOrEmpty(displayResolution))
            query = query.Where(l => l.DisplayResolution == displayResolution);

        if (minRefreshRate.HasValue)
            query = query.Where(l => l.DisplayRefreshRateHz >= minRefreshRate.Value);

        if (touchscreen.HasValue)
            query = query.Where(l => l.DisplayTouchscreen == touchscreen.Value);

        // Target audience filter
        if (!string.IsNullOrEmpty(targetAudience))
            query = query.Where(l => l.TargetAudience == targetAudience);

        // Only active products
        query = query.Where(l => l.IsActive);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(l => l.Brand)
            .ThenBy(l => l.Price)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Laptop>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Accessory>> GetAccessoriesAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? accessoryType = null,
        string? compatibility = null)
    {
        var query = _context.Accessories
            .Include(a => a.Images.Where(pi => pi.IsPrimary))
            .Include(a => a.Inventory)
            .Include(a => a.Category)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(a =>
                a.Name.Contains(searchTerm) ||
                a.Brand.Contains(searchTerm) ||
                a.Model.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(accessoryType))
        {
            query = query.Where(a => a.AccessoryType == accessoryType);
        }

        if (!string.IsNullOrEmpty(compatibility))
        {
            query = query.Where(a => a.Compatibility != null && a.Compatibility.Contains(compatibility));
        }

        query = query.Where(a => a.IsActive);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.AccessoryType)
            .ThenBy(a => a.Brand)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Accessory>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Advanced accessory filtering with compatibility matrix
    /// Implements T005 requirement for accessory compatibility management
    /// </summary>
    public async Task<PagedResult<Accessory>> GetAccessoriesWithCompatibilityAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? accessoryType = null,
        string? compatibility = null,
        int? compatibleProductId = null)
    {
        var query = _context.Accessories
            .Include(a => a.Images.Where(pi => pi.IsPrimary))
            .Include(a => a.Inventory)
            .Include(a => a.Category)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(a =>
                a.Name.Contains(searchTerm) ||
                a.Brand.Contains(searchTerm) ||
                a.Model.Contains(searchTerm) ||
                a.Description.Contains(searchTerm) ||
                a.AccessoryType.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(accessoryType))
        {
            query = query.Where(a => a.AccessoryType == accessoryType);
        }

        if (!string.IsNullOrEmpty(compatibility))
        {
            query = query.Where(a => a.Compatibility != null && a.Compatibility.Contains(compatibility));
        }

        // If looking for accessories compatible with a specific product
        if (compatibleProductId.HasValue)
        {
            var targetProduct = await _context.Products.FindAsync(compatibleProductId.Value);
            if (targetProduct != null)
            {
                // Check compatibility based on product type, brand, or model
                query = query.Where(a =>
                    a.Compatibility == null || // Universal compatibility
                    a.Compatibility.Contains(targetProduct.Brand) ||
                    a.Compatibility.Contains(targetProduct.Model) ||
                    a.Compatibility.Contains(targetProduct.GetType().Name));
            }
        }

        query = query.Where(a => a.IsActive);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.AccessoryType)
            .ThenBy(a => a.Brand)
            .ThenBy(a => a.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Accessory>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Validates if an accessory is compatible with a specific product
    /// </summary>
    public async Task<bool> ValidateProductCompatibilityAsync(int productId, int accessoryId)
    {
        var product = await _context.Products.FindAsync(productId);
        var accessory = await _context.Accessories.FindAsync(accessoryId);

        if (product == null || accessory == null)
            return false;

        // If no compatibility restrictions, it's universal
        if (string.IsNullOrEmpty(accessory.Compatibility))
            return true;

        // Check if compatibility includes product brand, model, or type
        return accessory.Compatibility.Contains(product.Brand) ||
               accessory.Compatibility.Contains(product.Model) ||
               accessory.Compatibility.Contains(product.GetType().Name);
    }

    public async Task<PagedResult<Bundle>> GetBundlesAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? bundleType = null)
    {
        var query = _context.Bundles
            .Include(b => b.Images.Where(pi => pi.IsPrimary))
            .Include(b => b.BundleItems)
                .ThenInclude(bi => bi.Product)
            .Include(b => b.Category)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(b =>
                b.Name.Contains(searchTerm) ||
                b.Brand.Contains(searchTerm) ||
                b.Description.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(bundleType))
        {
            query = query.Where(b => b.BundleType == bundleType);
        }

        query = query.Where(b => b.IsActive);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(b => b.BundleType)
            .ThenBy(b => b.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Bundle>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Creates a product bundle with dynamic pricing calculation
    /// Implements T005 requirement for bundle creation with automatic discount calculations
    /// </summary>
    public async Task<Bundle> CreateBundleAsync(string name, string description, IEnumerable<int> productIds,
        decimal discountPercentage, DateTime? validFrom = null, DateTime? validTo = null)
    {
        // Validate products exist and are active
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .Include(p => p.Inventory)
            .ToListAsync();

        if (products.Count != productIds.Count())
        {
            throw new ArgumentException("One or more products not found or inactive");
        }

        // Calculate bundle pricing
        var totalPrice = await CalculateBundlePriceAsync(productIds, discountPercentage);

        // Create bundle
        var bundle = new Bundle
        {
            Name = name,
            Description = description,
            Price = totalPrice,
            SKU = $"BUNDLE-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            IsActive = true,
            BundleType = "Standard",
            DiscountPercentage = discountPercentage,
            ValidFrom = validFrom,
            ValidTo = validTo,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Bundles.Add(bundle);
        await _context.SaveChangesAsync();

        // Create bundle items
        foreach (var product in products)
        {
            var bundleItem = new BundleItem
            {
                BundleId = bundle.Id,
                ProductId = product.Id,
                Quantity = 1, // Default quantity, can be customized
                DiscountPercentage = discountPercentage
            };
            _context.BundleItems.Add(bundleItem);
        }

        await _context.SaveChangesAsync();

        // Reload bundle with items
        return await _context.Bundles
            .Include(b => b.BundleItems)
                .ThenInclude(bi => bi.Product)
            .FirstAsync(b => b.Id == bundle.Id);
    }

    /// <summary>
    /// Calculates bundle price dynamically based on included products and discount
    /// </summary>
    public async Task<decimal> CalculateBundlePriceAsync(IEnumerable<int> productIds, decimal discountPercentage)
    {
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        if (!products.Any())
            return 0;

        var totalOriginalPrice = products.Sum(p => p.Price);
        var discountAmount = totalOriginalPrice * (discountPercentage / 100);
        var finalPrice = totalOriginalPrice - discountAmount;

        // Ensure minimum price
        return Math.Max(finalPrice, totalOriginalPrice * 0.1m); // Minimum 10% of original price
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return product;
    }

    public async Task<Product> UpdateProductAsync(Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;

        _context.Products.Update(product);
        await _context.SaveChangesAsync();

        return product;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null) return false;

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Product?> GetBySKUAsync(string sku)
    {
        return await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Inventory)
            .FirstOrDefaultAsync(p => p.SKU == sku);
    }

    public async Task<bool> IsSKUUniqueAsync(string sku, int? excludeProductId = null)
    {
        var query = _context.Products.Where(p => p.SKU == sku);

        if (excludeProductId.HasValue)
        {
            query = query.Where(p => p.Id != excludeProductId.Value);
        }

        return !await query.AnyAsync();
    }

    public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count = 10)
    {
        return await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Inventory)
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<IEnumerable<Product>> GetRelatedProductsAsync(int productId, int count = 5)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null) return new List<Product>();

        return await _context.Products
            .Include(p => p.Images.Where(pi => pi.IsPrimary))
            .Include(p => p.Inventory)
            .Where(p => p.IsActive &&
                       p.Id != productId &&
                       (p.Brand == product.Brand || p.GetType() == product.GetType()))
            .OrderBy(p => Guid.NewGuid()) // Random order
            .Take(count)
            .ToListAsync();
    }

    public async Task<PagedResult<Product>> GetProductsByBrandAsync(string brand, int page = 1, int pageSize = 20)
    {
        return await GetProductsAsync(page, pageSize, brand: brand, isActive: true);
    }

    public async Task<IEnumerable<string>> GetBrandsAsync()
    {
        return await _context.Products
            .Where(p => p.IsActive)
            .Select(p => p.Brand)
            .Distinct()
            .OrderBy(b => b)
            .ToListAsync();
    }

    public async Task<bool> UpdateProductImagesAsync(int productId, IEnumerable<ProductImage> images)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product == null) return false;

        // Append behavior: keep existing images and add the new ones
        // Ensure DisplayOrder continues after the last existing image
        var existingImages = product.Images?.OrderBy(i => i.DisplayOrder).ToList() ?? new List<ProductImage>();
        var nextDisplayOrder = existingImages.Any() ? existingImages.Max(i => i.DisplayOrder) + 1 : 1;

        foreach (var image in images)
        {
            image.ProductId = productId;
            // If incoming image has default/zero DisplayOrder, assign sequential order
            if (image.DisplayOrder <= 0)
            {
                image.DisplayOrder = nextDisplayOrder++;
            }
            _context.ProductImages.Add(image);
        }

        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteProductImageAsync(int imageId)
    {
        var image = await _context.ProductImages.FindAsync(imageId);
        if (image == null) return false;

        _context.ProductImages.Remove(image);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ProductImage?> GetProductImageAsync(int imageId)
    {
        return await _context.ProductImages.FindAsync(imageId);
    }

    /// <summary>
    /// Gets structured product specifications as key-value pairs
    /// Implements T005 requirement for detailed product specification access
    /// </summary>
    public async Task<Dictionary<string, object>> GetProductSpecificationsAsync(int productId)
    {
        var product = await _context.Products.FindAsync(productId);
        if (product == null)
            return new Dictionary<string, object>();

        var specs = new Dictionary<string, object>
        {
            ["Id"] = product.Id,
            ["Name"] = product.Name,
            ["Brand"] = product.Brand,
            ["Model"] = product.Model,
            ["Price"] = product.Price,
            ["SKU"] = product.SKU,
            ["Type"] = product.GetType().Name
        };

        // Add type-specific specifications
        switch (product)
        {
            case Laptop laptop:
                specs["Series"] = laptop.Series;
                specs["CPU"] = new
                {
                    Brand = laptop.CpuBrand,
                    Model = laptop.CpuModel,
                    Generation = laptop.CpuGeneration,
                    Cores = laptop.CpuCores,
                    BaseClockGHz = laptop.CpuBaseClockGHz,
                    BoostClockGHz = laptop.CpuBoostClockGHz,
                    Cache = laptop.CpuCache
                };
                specs["RAM"] = new
                {
                    Type = laptop.RamType,
                    CapacityGB = laptop.RamCapacityGB,
                    Slots = laptop.RamSlots,
                    Speed = laptop.RamSpeed,
                    Upgradeable = laptop.RamUpgradeable
                };
                specs["Storage"] = new
                {
                    Type = laptop.StorageType,
                    CapacityGB = laptop.StorageCapacityGB,
                    Interface = laptop.StorageInterface,
                    NVMeSupport = laptop.NvMeSupport
                };
                specs["GPU"] = new
                {
                    Type = laptop.GpuType,
                    Brand = laptop.GpuBrand,
                    Model = laptop.GpuModel,
                    VramGB = laptop.GpuVramGB
                };
                specs["Display"] = new
                {
                    SizeInches = laptop.DisplaySizeInches,
                    Resolution = laptop.DisplayResolution,
                    PanelType = laptop.DisplayPanelType,
                    RefreshRateHz = laptop.DisplayRefreshRateHz,
                    Touchscreen = laptop.DisplayTouchscreen
                };
                specs["Physical"] = new
                {
                    BatteryCapacityWh = laptop.BatteryCapacityWh,
                    WeightKg = laptop.WeightKg,
                    Dimensions = laptop.Dimensions,
                    Color = laptop.Color
                };
                specs["Connectivity"] = new
                {
                    Ports = laptop.Ports,
                    WiFi6Support = laptop.WiFi6Support,
                    BluetoothSupport = laptop.BluetoothSupport,
                    BluetoothVersion = laptop.BluetoothVersion
                };
                break;

            case Accessory accessory:
                specs["AccessoryType"] = accessory.AccessoryType;
                specs["Compatibility"] = accessory.Compatibility ?? "";
                specs["Specifications"] = accessory.Specifications ?? "";
                specs["Color"] = accessory.Color;
                specs["Connectivity"] = accessory.Connectivity;
                break;

            case Bundle bundle:
                specs["BundleType"] = bundle.BundleType;
                specs["DiscountPercentage"] = bundle.DiscountPercentage;
                specs["ValidFrom"] = bundle.ValidFrom?.ToString() ?? "";
                specs["ValidTo"] = bundle.ValidTo?.ToString() ?? "";
                break;
        }

        return specs;
    }

    /// <summary>
    /// Gets product recommendations based on purchase history and preferences
    /// </summary>
    public async Task<IEnumerable<Product>> GetRecommendedProductsAsync(int userId, int count = 5)
    {
        // Get user's order history to understand preferences
        var userOrderItems = await _context.OrderItems
            .Include(oi => oi.Product)
            .Where(oi => oi.Order.UserId == userId)
            .ToListAsync();

        if (!userOrderItems.Any())
        {
            // New user - return popular products
            return await _context.Products
                .Include(p => p.Images.Where(pi => pi.IsPrimary))
                .Include(p => p.Reviews)
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Reviews.Count)
                .ThenBy(p => p.Price)
                .Take(count)
                .ToListAsync();
        }

        // Get brands and types user has purchased
        var preferredBrands = userOrderItems.Select(oi => oi.Product.Brand).Distinct();
        var productTypes = userOrderItems.Select(oi => oi.Product.GetType().Name).Distinct();

        // Recommend similar products or complementary accessories
        var recommendations = await _context.Products
            .Include(p => p.Images.Where(pi => pi.IsPrimary))
            .Include(p => p.Reviews)
            .Where(p => p.IsActive &&
                       (preferredBrands.Contains(p.Brand) ||
                        productTypes.Contains(p.GetType().Name)))
            .Where(p => !userOrderItems.Select(oi => oi.ProductId).Contains(p.Id)) // Exclude already purchased
            .OrderByDescending(p => p.Reviews.Average(r => (double?)r.Rating) ?? 0)
            .ThenBy(p => p.Price)
            .Take(count)
            .ToListAsync();

        return recommendations;
    }

    /// <summary>
    /// Bulk updates pricing with percentage adjustment
    /// </summary>
    public async Task<bool> BulkUpdatePricingAsync(IEnumerable<int> productIds, decimal priceAdjustmentPercentage)
    {
        var products = await _context.Products
            .Where(p => productIds.Contains(p.Id))
            .ToListAsync();

        foreach (var product in products)
        {
            var adjustment = product.Price * (priceAdjustmentPercentage / 100);
            product.Price = Math.Max(product.Price + adjustment, 0.01m); // Minimum price of 1 cent
            product.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    #region TPT Optimized Helper Methods

    /// <summary>
    /// TPT optimized method for querying laptops directly from Laptops table
    /// </summary>
    private async Task<PagedResult<Product>> GetLaptopsPagedAsync(
        int page, int pageSize, string? searchTerm, string? brand,
        decimal? minPrice, decimal? maxPrice, bool? isActive, string? category = null)
    {
        var query = _context.Laptops
            .Include(l => l.Images.Where(pi => pi.IsPrimary))
            .Include(l => l.Inventory)
            .Include(l => l.ProductBrand)
            .Include(l => l.Category)
            .AsQueryable();

        query = ApplyCommonFilters(query.Cast<Product>(), searchTerm, brand, minPrice, maxPrice, isActive, category)
            .Cast<Laptop>();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(l => l.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Cast<Product>()
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// TPT optimized method for querying accessories directly from Accessories table
    /// </summary>
    private async Task<PagedResult<Product>> GetAccessoriesPagedAsync(
        int page, int pageSize, string? searchTerm, string? brand,
        decimal? minPrice, decimal? maxPrice, bool? isActive, string? category = null)
    {
        var query = _context.Accessories
            .Include(a => a.Images.Where(pi => pi.IsPrimary))
            .Include(a => a.Inventory)
            .Include(a => a.ProductBrand)
            .Include(a => a.Category)
            .AsQueryable();

        query = ApplyCommonFilters(query.Cast<Product>(), searchTerm, brand, minPrice, maxPrice, isActive, category)
            .Cast<Accessory>();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(a => a.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Cast<Product>()
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// TPT optimized method for querying bundles directly from Bundles table
    /// </summary>
    private async Task<PagedResult<Product>> GetBundlesPagedAsync(
        int page, int pageSize, string? searchTerm, string? brand,
        decimal? minPrice, decimal? maxPrice, bool? isActive, string? category = null)
    {
        var query = _context.Bundles
            .Include(b => b.Images.Where(pi => pi.IsPrimary))
            .Include(b => b.Inventory)
            .Include(b => b.ProductBrand)
            .Include(b => b.Category)
            .Include(b => b.BundleItems)
                .ThenInclude(bi => bi.Product)
            .AsQueryable();

        query = ApplyCommonFilters(query.Cast<Product>(), searchTerm, brand, minPrice, maxPrice, isActive, category)
            .Cast<Bundle>();

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(b => b.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Cast<Product>()
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Common filter method for all product types
    /// </summary>
    private IQueryable<Product> ApplyCommonFilters(
        IQueryable<Product> query, string? searchTerm, string? brand,
        decimal? minPrice, decimal? maxPrice, bool? isActive, string? category = null)
    {
        if (!string.IsNullOrEmpty(searchTerm))
        {
            query = query.Where(p =>
                p.Name.Contains(searchTerm) ||
                p.Brand.Contains(searchTerm) ||
                p.Model.Contains(searchTerm) ||
                p.Description.Contains(searchTerm));
        }

        if (!string.IsNullOrEmpty(brand))
        {
            query = query.Where(p => p.Brand == brand);
        }

        if (minPrice.HasValue)
        {
            query = query.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= maxPrice.Value);
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        if (!string.IsNullOrEmpty(category))
        {
            query = query.Where(p => p.Category != null &&
                (p.Category.Name == category || p.Category.Slug == category));
        }

        return query;
    }

    /// <summary>
    /// Gets products for admin management with full details
    /// </summary>
    public async Task<PagedResult<Product>> GetAdminProductsAsync(
        int page = 1,
        int pageSize = 50,
        string? search = null,
        string? type = null,
        string? brand = null,
        string? status = null,
        string? productType = null)
    {
        // For admin products, we want to return all products with full details
        // Use specific type queries when type is specified for better performance
        if (!string.IsNullOrEmpty(type))
        {
            switch (type.ToLowerInvariant())
            {
                case "laptop":
                    return await GetLaptopsPagedAsync(page, pageSize, search, brand, null, null,
                        status?.ToLower() == "active" ? true : status?.ToLower() == "inactive" ? false : true, null);
                case "accessory":
                    return await GetAccessoriesPagedAsync(page, pageSize, search, brand, null, null,
                        status?.ToLower() == "active" ? true : status?.ToLower() == "inactive" ? false : true, null);
                case "bundle":
                    return await GetBundlesPagedAsync(page, pageSize, search, brand, null, null,
                        status?.ToLower() == "active" ? true : status?.ToLower() == "inactive" ? false : true, null);
            }
        }

        // General query for all products when no specific type filter
        var query = _context.Products
            .Include(p => p.Images)
            .Include(p => p.Inventory)
            .AsQueryable();

        // Apply search filter
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p =>
                p.Name.Contains(search) ||
                p.Brand.Contains(search) ||
                p.Model.Contains(search) ||
                p.SKU.Contains(search));
        }

        // Apply brand filter
        if (!string.IsNullOrEmpty(brand))
        {
            query = query.Where(p => p.Brand == brand);
        }

        // Apply status filter (default: only active)
        if (string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.IsActive);
        }
        else
        {
            bool isActive = status.ToLower() == "active";
            query = query.Where(p => p.IsActive == isActive);
        }

        // Apply product type filter (base, variant, all)
        if (!string.IsNullOrEmpty(productType))
        {
            switch (productType.ToLowerInvariant())
            {
                case "base":
                    query = query.Where(p => p.ParentProductId == null);
                    break;
                case "variant":
                    query = query.Where(p => p.ParentProductId != null);
                    break;
                case "all":
                default:
                    // No filter - show all products
                    break;
            }
        }

        // Get total count
        var totalCount = await query.CountAsync();

        // Apply pagination
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Gets variants for a specific product
    /// </summary>
    public async Task<IEnumerable<Product>> GetVariantsAsync(int productId)
    {
        // Get base product to determine type
        var baseProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (baseProduct == null)
            return new List<Product>();

        // Load variants based on product type
        var query = _context.Products
            .Include(p => p.Images)
            .Include(p => p.Inventory)
            .Where(p => p.ParentProductId == productId);

        // For laptop variants, we need to load laptop-specific data
        if (baseProduct is Laptop)
        {
            query = query.OfType<Laptop>();
        }
        else if (baseProduct is Accessory)
        {
            query = query.OfType<Accessory>();
        }
        else if (baseProduct is Bundle)
        {
            query = query.OfType<Bundle>();
        }

        return await query
            .OrderBy(p => p.VariantName)
            .ToListAsync();
    }

    /// <summary>
    /// Gets base product for a variant
    /// </summary>
    public async Task<Product?> GetBaseProductAsync(int variantId)
    {
        var variant = await _context.Products
            .Include(p => p.ParentProduct)
            .FirstOrDefaultAsync(p => p.Id == variantId);

        return variant?.ParentProduct;
    }

    /// <summary>
    /// Creates a new variant for a product
    /// </summary>
    public async Task<Product> CreateVariantAsync(int baseProductId, Product variant)
    {
        // Validate base product exists and is not a variant itself
        var baseProduct = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == baseProductId && p.ParentProductId == null);

        if (baseProduct == null)
        {
            throw new ArgumentException("Base product not found or is already a variant");
        }

        // Set variant properties
        variant.ParentProductId = baseProductId;
        variant.CreatedAt = DateTime.UtcNow;
        variant.UpdatedAt = DateTime.UtcNow;

        // Validate variant SKU is unique
        if (await _context.Products.AnyAsync(p => p.VariantSku == variant.VariantSku))
        {
            throw new ArgumentException("Variant SKU already exists");
        }

        _context.Products.Add(variant);
        await _context.SaveChangesAsync();

        return variant;
    }

    /// <summary>
    /// Updates a variant
    /// </summary>
    public async Task<Product?> UpdateVariantAsync(int variantId, Product updatedVariant)
    {
        var variant = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == variantId && p.ParentProductId.HasValue);

        if (variant == null)
        {
            return null;
        }

        // Update variant properties
        variant.VariantName = updatedVariant.VariantName;
        variant.VariantSku = updatedVariant.VariantSku;
        variant.Price = updatedVariant.Price;
        variant.Description = updatedVariant.Description;
        variant.IsActive = updatedVariant.IsActive;
        variant.UpdatedAt = DateTime.UtcNow;

        // Update laptop-specific properties if it's a laptop variant
        if (variant is Laptop laptopVariant && updatedVariant is Laptop updatedLaptop)
        {
            laptopVariant.RamCapacityGB = updatedLaptop.RamCapacityGB;
            laptopVariant.StorageCapacityGB = updatedLaptop.StorageCapacityGB;
            laptopVariant.Color = updatedLaptop.Color;
            laptopVariant.GpuModel = updatedLaptop.GpuModel;
        }

        await _context.SaveChangesAsync();
        return variant;
    }

    /// <summary>
    /// Deletes a variant
    /// </summary>
    public async Task<bool> DeleteVariantAsync(int variantId)
    {
        var variant = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == variantId && p.ParentProductId.HasValue);

        if (variant == null)
        {
            return false;
        }

        _context.Products.Remove(variant);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Gets products with their variants (for product listings)
    /// </summary>
    public async Task<PagedResult<Product>> GetProductsWithVariantsAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? productType = null,
        string? brand = null,
        string? category = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool? isActive = null,
        string? sortBy = null)
    {
        // Get base products only (not variants)
        var query = _context.Products
            .Include(p => p.Images)
            .Include(p => p.Inventory)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Images)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Inventory)
            .Where(p => p.ParentProductId == null); // Only base products

        // Apply filters
        query = ApplyCommonFilters(query, searchTerm, brand, minPrice, maxPrice, isActive, category);

        // Apply product type filter
        if (!string.IsNullOrEmpty(productType))
        {
            switch (productType.ToLowerInvariant())
            {
                case "laptop":
                    query = query.OfType<Laptop>();
                    break;
                case "accessory":
                    query = query.OfType<Accessory>();
                    break;
                case "bundle":
                    query = query.OfType<Bundle>();
                    break;
            }
        }

        // Apply sorting
        query = ApplySorting(query, sortBy);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<Product>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private static IQueryable<Product> ApplySorting(IQueryable<Product> query, string? sortBy)
    {
        return sortBy?.ToLower() switch
        {
            "name" => query.OrderBy(p => p.Name),
            "name_desc" => query.OrderByDescending(p => p.Name),
            "price" => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "created" => query.OrderBy(p => p.CreatedAt),
            "created_desc" => query.OrderByDescending(p => p.CreatedAt),
            "updated" => query.OrderBy(p => p.UpdatedAt),
            "updated_desc" => query.OrderByDescending(p => p.UpdatedAt),
            _ => query.OrderBy(p => p.Name)
        };
    }

    #endregion
}
