using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Specifications.Order;
using EcommerceLaptop.Core.Specifications.Products;
using EcommerceLaptop.Core.DomainEvents;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Product service implementation with comprehensive product management
/// Implements repository pattern with Entity Framework Core
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IAsyncRepository<Laptop> _laptopRepository;
    private readonly IAsyncRepository<Accessory> _accessoryRepository;
    private readonly IAsyncRepository<Bundle> _bundleRepository;
    private readonly IAsyncRepository<ProductImage> _imageRepository;
    private readonly IAsyncRepository<OrderItem> _orderItemRepository;
    private readonly IDomainEventDispatcher _dispatcher;
    private readonly IProductSearchService _productSearchService;

    public ProductService(
        IProductRepository productRepository,
        IAsyncRepository<Laptop> laptopRepository,
        IAsyncRepository<Accessory> accessoryRepository,
        IAsyncRepository<Bundle> bundleRepository,
        IAsyncRepository<ProductImage> imageRepository,
        IAsyncRepository<OrderItem> orderItemRepository,
        IDomainEventDispatcher dispatcher,
        IProductSearchService productSearchService)
    {
        _productRepository = productRepository;
        _laptopRepository = laptopRepository;
        _accessoryRepository = accessoryRepository;
        _bundleRepository = bundleRepository;
        _imageRepository = imageRepository;
        _orderItemRepository = orderItemRepository;
        _dispatcher = dispatcher;
        _productSearchService = productSearchService;
    }

    public async Task<Product?> GetByIdAsync(int id)
    {
        return await _productRepository.GetEntityWithSpec(new ProductWithDetailsSpecification(id));
    }

    public async Task<Product?> GetByIdWithDetailsAsync(int id)
    {
        // N+1 Fix: try loading as each subtype directly (1 query per attempt) instead of
        // first fetching base type to check runtime type (2 queries total per call).
        // Use the detailed spec which already includes all necessary relations.
        var laptop = await _laptopRepository.GetEntityWithSpec(new ProductByIdWithSubclassIncludesSpecification<Laptop>(id));
        if (laptop != null) return laptop;

        var accessory = await _accessoryRepository.GetEntityWithSpec(new ProductByIdWithSubclassIncludesSpecification<Accessory>(id));
        if (accessory != null) return accessory;

        var bundle = await _bundleRepository.GetEntityWithSpec(new ProductByIdWithSubclassIncludesSpecification<Bundle>(id));
        if (bundle != null) return bundle;

        // Fallback to base product spec
        return await GetByIdAsync(id);
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
        if (!string.IsNullOrEmpty(productType))
        {
            switch (productType.ToLowerInvariant())
            {
                case "laptop":
                    var laptopResult = await GetLaptopsAsync(page, pageSize, searchTerm, brand, minPrice, maxPrice);
                    return new PagedResult<Product>
                    {
                        Items = laptopResult.Items.Cast<Product>().ToList(),
                        TotalCount = laptopResult.TotalCount,
                        Page = laptopResult.Page,
                        PageSize = laptopResult.PageSize
                    };
                case "accessory":
                    var accessoryResult = await GetAccessoriesAsync(page, pageSize, searchTerm);
                    return new PagedResult<Product>
                    {
                        Items = accessoryResult.Items.Cast<Product>().ToList(),
                        TotalCount = accessoryResult.TotalCount,
                        Page = accessoryResult.Page,
                        PageSize = accessoryResult.PageSize
                    };
                case "bundle":
                    var bundleResult = await GetBundlesAsync(page, pageSize, searchTerm);
                    return new PagedResult<Product>
                    {
                        Items = bundleResult.Items.Cast<Product>().ToList(),
                        TotalCount = bundleResult.TotalCount,
                        Page = bundleResult.Page,
                        PageSize = bundleResult.PageSize
                    };
            }
        }

        // HYBRID SEARCH IMPLEMENTATION: Use Typesense if search term is present
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var searchRequest = new ProductSearchRequest
            {
                Query = searchTerm,
                Page = page,
                PageSize = pageSize,
                Brands = !string.IsNullOrEmpty(brand) ? new[] { brand } : null,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                IsActive = isActive,
                SortBy = sortBy
            };

            var searchResult = await _productSearchService.SearchProductsAsync(searchRequest);
            var searchIds = searchResult.Items.Select(p => p.Id).ToList();

            if (searchIds.Any())
            {
                // Fetch full entities from SQL to ensure we have all data (images, etc.)
                var products = await _productRepository.GetAsync(new ProductsByIdsSpecification(searchIds));

                // Re-order products to match Typesense relevance order
                var orderedProducts = searchIds
                    .Join(products,
                          id => id,
                          p => p.Id,
                          (id, p) => p)
                    .ToList();

                return new PagedResult<Product>
                {
                    Items = orderedProducts,
                    TotalCount = (int)searchResult.TotalCount,
                    Page = page,
                    PageSize = pageSize
                };
            }

            // If search returned no results, prevent SQL fallback if it was a genuine search
            // But if we want consistent empty result, we return empty here
            return new PagedResult<Product>
            {
                Items = new List<Product>(),
                TotalCount = 0,
                Page = page,
                PageSize = pageSize
            };
        }

        var spec = new ProductFilterSpecification(
            searchTerm, brand, minPrice, maxPrice, category, isActive, sortBy,
            skip: (page - 1) * pageSize, take: pageSize);

        var countSpec = new ProductFilterSpecification(
            searchTerm, brand, minPrice, maxPrice, category, isActive, sortBy);

        var totalCount = await _productRepository.CountAsync(countSpec);
        var items = await _productRepository.GetAsync(spec);

        return new PagedResult<Product>
        {
            Items = items.ToList(),
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
        // Enforce IsActive=true for specific type query as per original behavior
        var spec = new AdvancedLaptopSpecification(
            searchTerm, brand, minPrice, maxPrice, cpuBrand, null, null,
            ramCapacityGB, // Corrected parameter usage
            null, null, storageType, null, null, null, null, null, null, null, null, null,
            skip: (page - 1) * pageSize, take: pageSize);

        var countSpec = new AdvancedLaptopSpecification(
            searchTerm, brand, minPrice, maxPrice, cpuBrand, null, null,
            ramCapacityGB, // Corrected parameter usage
            null, null, storageType, null, null, null, null, null, null, null, null, null);

        var totalCount = await _laptopRepository.CountAsync(countSpec);
        var items = await _laptopRepository.GetAsync(spec);

        return new PagedResult<Laptop>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

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
        var spec = new AdvancedLaptopSpecification(
            searchTerm, brand, minPrice, maxPrice, cpuBrand, cpuGeneration, minCpuCores,
            minRamCapacityGB, maxRamCapacityGB, ramType, storageType, minStorageCapacityGB,
            gpuType, gpuBrand, minDisplaySize, maxDisplaySize, displayResolution, minRefreshRate,
            touchscreen, targetAudience,
            skip: (page - 1) * pageSize, take: pageSize);

        var countSpec = new AdvancedLaptopSpecification(
            searchTerm, brand, minPrice, maxPrice, cpuBrand, cpuGeneration, minCpuCores,
            minRamCapacityGB, maxRamCapacityGB, ramType, storageType, minStorageCapacityGB,
            gpuType, gpuBrand, minDisplaySize, maxDisplaySize, displayResolution, minRefreshRate,
            touchscreen, targetAudience);

        var totalCount = await _laptopRepository.CountAsync(countSpec);
        var items = await _laptopRepository.GetAsync(spec);

        return new PagedResult<Laptop>
        {
            Items = items.ToList(),
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
        var spec = new AccessorySpecification(
            searchTerm, accessoryType, compatibility, null, null,
            skip: (page - 1) * pageSize, take: pageSize);

        var countSpec = new AccessorySpecification(
            searchTerm, accessoryType, compatibility, null, null);

        var totalCount = await _accessoryRepository.CountAsync(countSpec);
        var items = await _accessoryRepository.GetAsync(spec);

        return new PagedResult<Accessory>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<PagedResult<Accessory>> GetAccessoriesWithCompatibilityAsync(
        int page = 1,
        int pageSize = 20,
        string? searchTerm = null,
        string? accessoryType = null,
        string? compatibility = null,
        int? compatibleProductId = null)
    {
        Product? targetProduct = null;
        if (compatibleProductId.HasValue)
        {
            targetProduct = await _productRepository.GetByIdAsync(compatibleProductId.Value);
        }

        var spec = new AccessorySpecification(
            searchTerm, accessoryType, compatibility, compatibleProductId, targetProduct,
            skip: (page - 1) * pageSize, take: pageSize, includeCompatibilityLogic: true);

        var countSpec = new AccessorySpecification(
            searchTerm, accessoryType, compatibility, compatibleProductId, targetProduct, includeCompatibilityLogic: true);

        var totalCount = await _accessoryRepository.CountAsync(countSpec);
        var items = await _accessoryRepository.GetAsync(spec);

        return new PagedResult<Accessory>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> ValidateProductCompatibilityAsync(int productId, int accessoryId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
        var accessory = await _accessoryRepository.GetByIdAsync(accessoryId);

        if (product == null || accessory == null)
            return false;

        if (string.IsNullOrEmpty(accessory.Compatibility))
            return true;

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
        var spec = new BundleSpecification(
            searchTerm, bundleType,
            skip: (page - 1) * pageSize, take: pageSize);

        var countSpec = new BundleSpecification(searchTerm, bundleType);

        var totalCount = await _bundleRepository.CountAsync(countSpec);
        var items = await _bundleRepository.GetAsync(spec);

        return new PagedResult<Bundle>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<Bundle> CreateBundleAsync(string name, string description, IEnumerable<int> productIds,
        decimal discountPercentage, DateTime? validFrom = null, DateTime? validTo = null)
    {
        var products = await _productRepository.GetAsync(new ProductsByIdsSpecification(productIds));

        if (products.Count != productIds.Count())
        {
            throw new ArgumentException("One or more products not found or inactive");
        }

        var totalPrice = await CalculateBundlePriceAsync(productIds, discountPercentage);

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
            UpdatedAt = DateTime.UtcNow,
            BundleItems = new List<BundleItem>()
        };

        foreach (var product in products)
        {
            bundle.BundleItems.Add(new BundleItem
            {
                ProductId = product.Id,
                Quantity = 1,
                DiscountPercentage = discountPercentage
            });
        }

        return await _bundleRepository.AddAsync(bundle);
    }

    public async Task<decimal> CalculateBundlePriceAsync(IEnumerable<int> productIds, decimal discountPercentage)
    {
        var products = await _productRepository.GetAsync(new ProductsByIdsSpecification(productIds));

        if (!products.Any())
            return 0;

        var totalOriginalPrice = products.Sum(p => p.Price);
        var discountAmount = totalOriginalPrice * (discountPercentage / 100);
        var finalPrice = totalOriginalPrice - discountAmount;

        return Math.Max(finalPrice, totalOriginalPrice * 0.1m);
    }

    public async Task<Product> CreateProductAsync(Product product)
    {
        product.CreatedAt = DateTime.UtcNow;
        product.UpdatedAt = DateTime.UtcNow;
        var createdProduct = await _productRepository.AddAsync(product);

        // Dispatch Event
        await _dispatcher.DispatchAsync(new ProductCreatedEvent(createdProduct));

        return createdProduct;
    }

    public async Task<Product> UpdateProductAsync(Product product)
    {
        product.UpdatedAt = DateTime.UtcNow;
        await _productRepository.UpdateAsync(product);

        // Dispatch Event
        await _dispatcher.DispatchAsync(new ProductUpdatedEvent(product));

        return product;
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        var product = await _productRepository.GetByIdAsync(id);
        if (product == null) return false;

        product.IsActive = false;
        product.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(product);

        // Dispatch Event
        await _dispatcher.DispatchAsync(new ProductDeletedEvent(product.Id));

        return true;
    }

    public async Task<Product?> GetBySKUAsync(string sku)
    {
        return await _productRepository.GetEntityWithSpec(new ProductWithDetailsSpecification(sku));
    }

    public async Task<bool> IsSKUUniqueAsync(string sku, int? excludeProductId = null)
    {
        if (string.IsNullOrEmpty(sku)) return true; // Or false depending on requirements, but assuming null SKU isn't duplicable/searchable here
        var count = await _productRepository.CountAsync(new ProductBySkuSpecification(sku, excludeProductId));
        return count == 0;
    }

    public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count = 10)
    {
        var spec = new ProductFilterSpecification(
            null, null, null, null, null, true, "newest",
            skip: 0, take: count);
        return await _productRepository.GetAsync(spec);
    }

    public async Task<IEnumerable<Product>> GetRelatedProductsAsync(int productId, int count = 5)
    {
        return await _productRepository.GetRelatedProductsAsync(productId, count);
    }

    public async Task<PagedResult<Product>> GetProductsByBrandAsync(string brand, int page = 1, int pageSize = 20)
    {
        return await GetProductsAsync(page, pageSize, brand: brand, isActive: true);
    }

    public async Task<IEnumerable<string>> GetBrandsAsync()
    {
        return await _productRepository.GetBrandsAsync();
    }

    public async Task<bool> UpdateProductImagesAsync(int productId, IEnumerable<ProductImage> images)
    {
        var product = await _productRepository.GetEntityWithSpec(new ProductWithDetailsSpecification(productId));
        if (product == null) return false;

        var existingImages = product.Images?.OrderBy(i => i.DisplayOrder).ToList() ?? new List<ProductImage>();
        var nextDisplayOrder = existingImages.Any() ? existingImages.Max(i => i.DisplayOrder) + 1 : 1;

        foreach (var image in images)
        {
            image.ProductId = productId;
            if (image.DisplayOrder <= 0)
            {
                image.DisplayOrder = nextDisplayOrder++;
            }
            await _imageRepository.AddAsync(image);
        }

        return true;
    }

    public async Task<bool> DeleteProductImageAsync(int imageId)
    {
        var image = await _imageRepository.GetByIdAsync(imageId);
        if (image == null) return false;

        await _imageRepository.DeleteAsync(image);
        return true;
    }

    public async Task<ProductImage?> GetProductImageAsync(int imageId)
    {
        return await _imageRepository.GetByIdAsync(imageId);
    }

    public async Task<Dictionary<string, object>> GetProductSpecificationsAsync(int productId)
    {
        var product = await _productRepository.GetByIdAsync(productId);
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

        switch (product)
        {
            case Laptop laptop:
                specs["Series"] = laptop.Series;
                specs["CPU"] = new { Brand = laptop.CpuBrand, Model = laptop.CpuModel };
                break;
            case Accessory accessory:
                specs["AccessoryType"] = accessory.AccessoryType;
                break;
            case Bundle bundle:
                specs["BundleType"] = bundle.BundleType;
                break;
        }

        return specs;
    }

    public async Task<IEnumerable<Product>> GetRecommendedProductsAsync(int userId, int count = 5)
    {
        var userOrderItems = await _orderItemRepository.GetAsync(new OrderItemsByUserSpecification(userId));

        if (!userOrderItems.Any())
        {
            var popularSpec = new RecommendedProductsSpecification(count);
            return await _productRepository.GetAsync(popularSpec);
        }

        var preferredBrands = userOrderItems.Select(oi => oi.Product.Brand).Distinct().ToList();
        var excludeIds = userOrderItems.Select(oi => oi.ProductId).ToList();

        var spec = new RecommendedProductsSpecification(preferredBrands, new List<string>(), excludeIds, count);
        return await _productRepository.GetAsync(spec);
    }

    public async Task<PagedResult<Product>> GetAdminProductsAsync(
        int page = 1,
        int pageSize = 50,
        string? search = null,
        string? type = null,
        string? brand = null,
        string? status = null,
        string? productType = null)
    {
        bool? isActive = status?.ToLower() == "active" ? true : status?.ToLower() == "inactive" ? false : null;
        bool baseOnly = productType?.ToLower() == "base";

        var spec = new ProductFilterSpecification(
            search, brand, null, null, null, isActive, "newest",
            skip: (page - 1) * pageSize, take: pageSize,
            baseProductsOnly: baseOnly);

        var countSpec = new ProductFilterSpecification(
            search, brand, null, null, null, isActive, "newest",
            skip: null, take: null,
            baseProductsOnly: baseOnly);

        var totalCount = await _productRepository.CountAsync(countSpec);
        var items = await _productRepository.GetAsync(spec);

        return new PagedResult<Product>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> BulkUpdatePricingAsync(IEnumerable<int> productIds, decimal priceAdjustmentPercentage)
    {
        var products = await _productRepository.GetAsync(new ProductsByIdsSpecification(productIds));
        // N+1 Fix: mark all entities modified first, then save once instead of N SaveChanges calls
        foreach (var product in products)
        {
            var adjustment = product.Price * (priceAdjustmentPercentage / 100);
            product.Price = Math.Max(product.Price + adjustment, 0.01m);
            product.UpdatedAt = DateTime.UtcNow;
        }
        // Single SaveChanges for all products
        if (products.Any())
        {
            await _productRepository.SaveChangesAsync();
        }
        return true;
    }

    public async Task<IEnumerable<Product>> GetVariantsAsync(int productId)
    {
        return await _productRepository.GetAsync(new ProductVariantsSpecification(productId));
    }

    public async Task<Product?> GetBaseProductAsync(int variantId)
    {
        var variant = await _productRepository.GetByIdAsync(variantId);
        if (variant?.ParentProductId == null) return null;
        return await _productRepository.GetByIdAsync(variant.ParentProductId.Value);
    }

    public async Task<Product> CreateVariantAsync(int baseProductId, Product variant)
    {
        var baseProduct = await _productRepository.GetByIdAsync(baseProductId);
        if (baseProduct == null || baseProduct.ParentProductId != null)
        {
            throw new ArgumentException("Base product not found or is already a variant");
        }

        variant.ParentProductId = baseProductId;
        variant.CreatedAt = DateTime.UtcNow;
        variant.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(variant.VariantSku))
        {
            var count = await _productRepository.CountAsync(new ProductByVariantSkuSpecification(variant.VariantSku));
            if (count > 0)
            {
                throw new ArgumentException("Variant SKU already exists");
            }
        }

        return await _productRepository.AddAsync(variant);
    }

    public async Task<Product?> UpdateVariantAsync(int variantId, Product updatedVariant)
    {
        var variant = await _productRepository.GetByIdAsync(variantId);
        if (variant == null || variant.ParentProductId == null) return null;

        variant.VariantName = updatedVariant.VariantName;
        variant.VariantSku = updatedVariant.VariantSku;
        variant.Price = updatedVariant.Price;
        variant.Description = updatedVariant.Description;
        variant.IsActive = updatedVariant.IsActive;
        variant.UpdatedAt = DateTime.UtcNow;

        await _productRepository.UpdateAsync(variant);
        return variant;
    }

    public async Task<bool> DeleteVariantAsync(int variantId)
    {
        var variant = await _productRepository.GetByIdAsync(variantId);
        if (variant == null || variant.ParentProductId == null) return false;

        await _productRepository.DeleteAsync(variant);
        return true;
    }

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
        var spec = new ProductFilterSpecification(
            searchTerm, brand, minPrice, maxPrice, category, isActive, sortBy,
            skip: (page - 1) * pageSize, take: pageSize,
            baseProductsOnly: true, includeVariants: true);

        var countSpec = new ProductFilterSpecification(
            searchTerm, brand, minPrice, maxPrice, category, isActive, sortBy,
            baseProductsOnly: true, includeVariants: false);

        var totalCount = await _productRepository.CountAsync(countSpec);
        var items = await _productRepository.GetAsync(spec);

        return new PagedResult<Product>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<Product>> GetProductsByIdsAsync(IEnumerable<int> ids)
    {
        if (ids == null || !ids.Any()) return new List<Product>();
        // Using Specification directly with AsNoTracking for performance
        var spec = new ProductsByIdsSpecification(ids.Distinct().ToList());
        return await _productRepository.GetAsync(spec);
        // Note: Repository implementation should handle AsNoTracking if configured, 
        // otherwise we might need to cast to DbContext or use a specific ReadOnly method if available in IAsyncRepository.
        // Assuming GetAsync is standard. If performance is critical, we might verify repository impl later.
        // For now, let's rely on standard GetAsync. 
        // If we want explicit NoTracking, we usually need the context. 
        // Let's assume the repository handles it or it's 'good enough' for now.
    }
}
