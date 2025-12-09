using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.API.DTOs;
using EcommerceLaptop.Infrastructure.Services.Security;
using EcommerceLaptop.Infrastructure.Data;
using System.Text.Json;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Products controller handling all product-related operations
/// Implements RESTful API design with comprehensive CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;
    private readonly IMemoryCache _cache;
    private readonly IImageHostingService _imageHostingService;
    private readonly IAuditLoggingService _auditLoggingService;
    private readonly ApplicationDbContext _context;

    public ProductsController(IProductService productService, IImageHostingService imageHostingService, IAuditLoggingService auditLoggingService, ILogger<ProductsController> logger, IMemoryCache cache, ApplicationDbContext context)
        : base(logger)
    {
        _productService = productService;
        _imageHostingService = imageHostingService;
        _auditLoggingService = auditLoggingService;
        _cache = cache;
        _context = context;
    }

    /// <summary>
    /// Gets paginated list of products with filtering
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Items per page (default: 20)</param>
    /// <param name="search">Search term for name, brand, model</param>
    /// <param name="type">Product type filter (Laptop, Accessory, Bundle)</param>
    /// <param name="brand">Brand filter</param>
    /// <param name="minPrice">Minimum price filter</param>
    /// <param name="maxPrice">Maximum price filter</param>
    /// <returns>Paginated product list</returns>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] string? brand = null,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? sortBy = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            // Generate cache key based on parameters to avoid duplicate queries
            var cacheKey = $"products_{page}_{pageSize}_{search ?? ""}_{type ?? ""}_{brand ?? ""}_{category ?? ""}_{minPrice?.ToString("F2") ?? "0"}_{maxPrice?.ToString("F2") ?? "0"}_{sortBy ?? ""}";

            IActionResult? resultAction;

            if (!_cache.TryGetValue(cacheKey, out resultAction) || resultAction == null)
            {
                var result = await _productService.GetProductsAsync(
                    page, pageSize, search, type, brand, category, minPrice, maxPrice, true, sortBy);

                var productDtos = result.Items.Select(product => product switch
                {
                    Laptop laptop => MapToLaptopDto(laptop),
                    Accessory accessory => MapToAccessoryDto(accessory),
                    Bundle bundle => MapToBundleDto(bundle),
                    _ => MapToProductDto(product)
                }).ToList();
                resultAction = PaginatedResponse(productDtos, result.TotalCount, page, pageSize);

                // Cache for 5 minutes
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

                _cache.Set(cacheKey, resultAction, cacheOptions);
            }

            return resultAction;
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetProducts));
        }
    }

    /// <summary>
    /// Gets specific product by ID
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Product details</returns>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProduct(int id)
    {
        try
        {
            var product = await _productService.GetByIdAsync(id);
            if (product is null)
                return ErrorResponse("Product not found", 404);
            if (!product.IsActive)
                return ErrorResponse("Product not found", 404);

            // Load variants if this is a base product
            if (product.IsBaseProduct)
            {
                var variants = await _productService.GetVariantsAsync(id);
                product.Variants = variants.ToList();
            }

            // Log product view activity
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                await _auditLoggingService.LogUserActivityAsync(
                    userId.Value,
                    "product_view",
                    $"User viewed product: {product.Name} (ID: {id})",
                    ipAddress ?? "Unknown",
                    userAgent ?? "Unknown"
                );
            }

            // TPT Optimization: Return detailed DTO based on the actual product type
            // This provides the frontend with all necessary fields without extra calls
            object productDto = product switch
            {
                Laptop laptop => MapToLaptopDto(laptop),
                Accessory accessory => MapToAccessoryDto(accessory),
                Bundle bundle => MapToBundleDto(bundle),
                _ => MapToProductDto(product) // Fallback for base product type
            };

            return SuccessResponse(productDto);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetProduct));
        }
    }

    /// <summary>
    /// Gets specific product by slug (searches by name)
    /// </summary>
    /// <param name="slug">Product slug</param>
    /// <returns>Product details</returns>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductBySlug(string slug)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(slug))
                return ErrorResponse("Invalid slug", 400);

            // Convert slug back to potential search terms
            var searchTerms = slug.Replace("-", " ");

            // First try exact search
            var result = await _productService.GetProductsAsync(
                page: 1,
                pageSize: 1,
                searchTerm: searchTerms,
                isActive: true);

            Product? product = null;

            if (result.Items.Any())
            {
                product = result.Items.First();
            }
            else
            {
                // Try partial search with individual words
                var words = searchTerms.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length > 1)
                {
                    // Try searching with first few words
                    var partialSearch = string.Join(" ", words.Take(2));
                    result = await _productService.GetProductsAsync(
                        page: 1,
                        pageSize: 5,
                        searchTerm: partialSearch,
                        isActive: true);

                    if (result.Items.Any())
                    {
                        // Try to find the best match by comparing with the slug
                        product = result.Items.FirstOrDefault(p =>
                            GenerateSlugFromName(p.Name).Equals(slug, StringComparison.OrdinalIgnoreCase))
                            ?? result.Items.First();
                    }
                }
            }

            if (product == null)
                return ErrorResponse("Product not found", 404);

            // Load variants if this is a base product
            if (product.IsBaseProduct)
            {
                var variants = await _productService.GetVariantsAsync(product.Id);
                product.Variants = variants.ToList();
            }

            // TPT Optimization: Return detailed DTO based on the actual product type
            object productDto = product switch
            {
                Laptop laptop => MapToLaptopDto(laptop),
                Accessory accessory => MapToAccessoryDto(accessory),
                Bundle bundle => MapToBundleDto(bundle),
                _ => MapToProductDto(product)
            };

            return SuccessResponse(productDto);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetProductBySlug));
        }
    }

    /// <summary>
    /// Helper method to generate slug from product name (matches frontend logic)
    /// </summary>
    private static string GenerateSlugFromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "product";

        return name.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("&", "and")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("{", "")
            .Replace("}", "")
            .Replace(".", "")
            .Replace(",", "")
            .Replace(":", "")
            .Replace(";", "")
            .Replace("'", "")
            .Replace("\"", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace("@", "")
            .Replace("#", "")
            .Replace("$", "")
            .Replace("%", "")
            .Replace("^", "")
            .Replace("*", "")
            .Replace("+", "")
            .Replace("=", "")
            .Replace("|", "")
            .Replace("~", "")
            .Replace("`", "")
            .Replace("<", "")
            .Replace(">", "")
            .Replace("\t", "")
            .Replace("\n", "")
            .Replace("\r", "")
            .Replace("--", "-")
            .Replace("---", "-")
            .Trim('-');
    }

    /// <summary>
    /// Gets laptops with specific filtering
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="search">Search term</param>
    /// <param name="brand">Brand filter</param>
    /// <param name="minPrice">Minimum price</param>
    /// <param name="maxPrice">Maximum price</param>
    /// <param name="cpuBrand">CPU brand filter</param>
    /// <param name="ramCapacity">RAM capacity filter</param>
    /// <param name="storageType">Storage type filter</param>
    /// <returns>Paginated laptop list</returns>
    [HttpGet("laptops")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLaptops(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? brand = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? cpuBrand = null,
        [FromQuery] int? ramCapacity = null,
        [FromQuery] string? storageType = null)
    {
        try
        {
            var result = await _productService.GetLaptopsAsync(
                page, pageSize, search, brand, minPrice, maxPrice,
                cpuBrand, ramCapacity, storageType);

            var laptopDtos = result.Items.Select(MapToLaptopDto).ToList();

            return PaginatedResponse(laptopDtos, result.TotalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetLaptops));
        }
    }

    /// <summary>
    /// Gets accessories with filtering
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="search">Search term</param>
    /// <param name="accessoryType">Accessory type filter</param>
    /// <param name="compatibility">Compatibility filter</param>
    /// <returns>Paginated accessory list</returns>
    [HttpGet("accessories")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAccessories(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? accessoryType = null,
        [FromQuery] string? compatibility = null)
    {
        try
        {
            var result = await _productService.GetAccessoriesAsync(
                page, pageSize, search, accessoryType, compatibility);

            var accessoryDtos = result.Items.Select(MapToAccessoryDto).ToList();

            return PaginatedResponse(accessoryDtos, result.TotalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetAccessories));
        }
    }

    /// <summary>
    /// Gets bundles with filtering
    /// </summary>
    /// <param name="page">Page number</param>
    /// <param name="pageSize">Items per page</param>
    /// <param name="search">Search term</param>
    /// <param name="bundleType">Bundle type filter</param>
    /// <returns>Paginated bundle list</returns>
    [HttpGet("bundles")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBundles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? bundleType = null)
    {
        try
        {
            var result = await _productService.GetBundlesAsync(
                page, pageSize, search, bundleType);

            var bundleDtos = result.Items.Select(MapToBundleDto).ToList();

            return PaginatedResponse(bundleDtos, result.TotalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetBundles));
        }
    }

    /// <summary>
    /// Gets featured products
    /// </summary>
    /// <param name="count">Number of products to return</param>
    /// <returns>Featured products</returns>
    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeaturedProducts([FromQuery] int count = 10)
    {
        try
        {
            if (count < 1 || count > 50) count = 10;

            var products = await _productService.GetFeaturedProductsAsync(count);
            var productDtos = products.Select(MapToProductDto).ToList();

            return SuccessResponse(productDtos);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetFeaturedProducts));
        }
    }

    /// <summary>
    /// Gets related products
    /// </summary>
    /// <param name="id">Base product ID</param>
    /// <param name="count">Number of related products to return</param>
    /// <returns>Related products</returns>
    [HttpGet("{id}/related")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRelatedProducts(int id, [FromQuery] int count = 5)
    {
        try
        {
            if (count < 1 || count > 20) count = 5;

            var products = await _productService.GetRelatedProductsAsync(id, count);
            var productDtos = products.Select(MapToProductDto).ToList();

            return SuccessResponse(productDtos);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetRelatedProducts));
        }
    }

    /// <summary>
    /// Gets all brands
    /// </summary>
    /// <returns>List of brands</returns>
    [HttpGet("brands")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrands()
    {
        try
        {
            var brands = await _productService.GetBrandsAsync();
            return SuccessResponse(brands);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetBrands));
        }
    }

    /// <summary>
    /// Creates a new product (Admin only)
    /// </summary>
    /// <param name="request">Product creation request</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [Authorize(Policy = "RequirePermission:products:write")]
    public async Task<IActionResult> CreateProduct([FromBody] JsonElement request)
    {
        try
        {
            // Deserialize to base request first to get ProductType
            var baseRequest = JsonSerializer.Deserialize<CreateProductRequest>(request.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (baseRequest == null || string.IsNullOrEmpty(baseRequest.ProductType))
            {
                return ErrorResponse("ProductType is required.", 400);
            }

            var modelStateErrors = ValidateModelState();
            if (modelStateErrors != null) return modelStateErrors;

            // Check SKU uniqueness
            if (!await _productService.IsSKUUniqueAsync(baseRequest.SKU))
                return ErrorResponse("SKU already exists");

            Product product;
            try
            {
                product = MapToProduct(request, baseRequest.ProductType);
            }
            catch (ArgumentException ex)
            {
                return ErrorResponse(ex.Message, 400);
            }
            catch (JsonException ex)
            {
                return ErrorResponse($"Invalid JSON for ProductType '{baseRequest.ProductType}': {ex.Message}", 400);
            }


            var createdProduct = await _productService.CreateProductAsync(product);
            var productDto = MapToProductDto(createdProduct);

            // Log admin product creation activity
            var adminUserId = GetCurrentUserId();
            if (adminUserId.HasValue)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                await _auditLoggingService.LogAdminActivityAsync(
                    adminUserId.Value,
                    "product_created",
                    $"Admin created product: {createdProduct.Name} (ID: {createdProduct.Id}, SKU: {createdProduct.SKU})",
                    ipAddress ?? "Unknown",
                    userAgent ?? "Unknown",
                    targetResource: $"Product:{createdProduct.Id}"
                );
            }

            return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id },
                SuccessResponse(productDto, "Product created successfully"));
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(CreateProduct));
        }
    }

    /// <summary>
    /// Updates existing product (Admin only)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="request">Product update request</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequirePermission:products:write")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] JsonElement request)
    {
        try
        {
            var modelStateErrors = ValidateModelState();
            if (modelStateErrors != null) return modelStateErrors;

            var existingProduct = await _productService.GetByIdWithDetailsAsync(id);
            if (existingProduct == null)
                return ErrorResponse("Product not found", 404);

            // Apply updates using the new flexible mapping
            ApplyProductUpdates(existingProduct, request);

            var updatedProduct = await _productService.UpdateProductAsync(existingProduct);

            // Align base product inventory update with variant behavior when StockQuantity is provided
            if (request.ValueKind == JsonValueKind.Object && request.TryGetProperty("StockQuantity", out var stockElement))
            {
                if (stockElement.TryGetInt32(out var stockQuantity))
                {
                    var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == id);
                    if (inventory == null)
                    {
                        inventory = new Inventory
                        {
                            ProductId = id,
                            QuantityInStock = stockQuantity,
                            ReservedQuantity = 0,
                            ReorderLevel = 10,
                            LastStockUpdate = DateTime.UtcNow
                        };
                        _context.Inventories.Add(inventory);
                    }
                    else
                    {
                        var originalQuantity = inventory.QuantityInStock;
                        inventory.QuantityInStock = stockQuantity;
                        inventory.LastStockUpdate = DateTime.UtcNow;

                        var quantityDifference = stockQuantity - originalQuantity;
                        if (quantityDifference != 0)
                        {
                            var transactionRecord = new InventoryTransaction
                            {
                                InventoryId = inventory.Id,
                                Type = quantityDifference > 0 ? InventoryTransactionType.Adjustment : InventoryTransactionType.Sale,
                                Quantity = Math.Abs(quantityDifference),
                                Reference = "PRODUCT_UPDATE",
                                Notes = $"Base product inventory update: {originalQuantity}  {stockQuantity}",
                                CreatedAt = DateTime.UtcNow,
                                CreatedBy = 1
                            };
                            _context.InventoryTransactions.Add(transactionRecord);
                        }
                    }

                    await _context.SaveChangesAsync();
                }
            }

            var productDto = MapToProductDto(updatedProduct);

            // Log admin product update activity
            var adminUserId = GetCurrentUserId();
            if (adminUserId.HasValue)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                await _auditLoggingService.LogAdminActivityAsync(
                    adminUserId.Value,
                    "product_updated",
                    $"Admin updated product: {updatedProduct.Name} (ID: {id})",
                    ipAddress ?? "Unknown",
                    userAgent ?? "Unknown",
                    targetResource: $"Product:{id}"
                );
            }

            return SuccessResponse(productDto, "Product updated successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(UpdateProduct));
        }
    }

    /// <summary>
    /// Deletes product (Admin only)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequirePermission:products:delete")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            // Get product info before deletion for logging
            var existingProduct = await _productService.GetByIdAsync(id);
            var productName = existingProduct?.Name ?? "Unknown";

            var success = await _productService.DeleteProductAsync(id);
            if (!success)
                return ErrorResponse("Product not found", 404);

            // Log admin product deletion activity
            var adminUserId = GetCurrentUserId();
            if (adminUserId.HasValue)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                await _auditLoggingService.LogAdminActivityAsync(
                    adminUserId.Value,
                    "product_deleted",
                    $"Admin deleted product: {productName} (ID: {id})",
                    ipAddress ?? "Unknown",
                    userAgent ?? "Unknown",
                    targetResource: $"Product:{id}"
                );
            }

            return SuccessResponse(new { deleted = true }, "Product deleted successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(DeleteProduct));
        }
    }

    #region Mapping Methods

    private ProductDto MapToProductDto(Product product)
    {
        var images = product.Images?.Select(MapToProductImageDto).ToList() ?? new List<ProductImageDto>();
        var primaryImage = images.FirstOrDefault(img => img.IsPrimary) ?? images.FirstOrDefault();

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Brand = product.Brand,
            Model = product.Model,
            Price = product.Price,
            SKU = product.SKU,
            IsActive = product.IsActive,
            ProductType = product.GetType().Name,
            StockQuantity = product.Inventory?.AvailableQuantity ?? 0,
            CreatedAt = product.CreatedAt,
            UpdatedAt = product.UpdatedAt,
            ImageUrl = primaryImage?.ImageUrl, // Set primary image URL
            Images = images,
            Specifications = GenerateProductSpecifications(product),
            Inventory = product.Inventory != null ? MapToInventoryDto(product.Inventory) : null,

            // Variant support
            ParentProductId = product.ParentProductId,
            VariantName = product.VariantName,
            VariantSku = product.VariantSku,
            IsVariant = product.IsVariant,
            IsBaseProduct = product.IsBaseProduct,
            Variants = product.Variants?.Select(v => v switch
            {
                Laptop laptop => MapToLaptopDto(laptop),
                Accessory accessory => MapToAccessoryDto(accessory),
                Bundle bundle => MapToBundleDto(bundle),
                _ => MapToProductDto(v)
            }).ToList() ?? new List<ProductDto>()
        };
    }

    private LaptopDto MapToLaptopDto(Laptop laptop)
    {
        var baseDto = MapToProductDto(laptop);
        return new LaptopDto
        {
            Id = baseDto.Id,
            Name = baseDto.Name,
            Description = baseDto.Description,
            Brand = baseDto.Brand,
            Model = baseDto.Model,
            Price = baseDto.Price,
            SKU = baseDto.SKU,
            IsActive = baseDto.IsActive,
            ProductType = baseDto.ProductType,
            StockQuantity = baseDto.StockQuantity,
            CreatedAt = baseDto.CreatedAt,
            UpdatedAt = baseDto.UpdatedAt,
            Images = baseDto.Images,
            Specifications = baseDto.Specifications,
            Inventory = baseDto.Inventory,
            Series = laptop.Series,
            CpuBrand = laptop.CpuBrand,
            CpuModel = laptop.CpuModel,
            CpuGeneration = laptop.CpuGeneration,
            CpuCores = laptop.CpuCores,
            CpuBaseClockGHz = laptop.CpuBaseClockGHz,
            CpuBoostClockGHz = laptop.CpuBoostClockGHz,
            CpuCache = laptop.CpuCache,
            RamType = laptop.RamType,
            RamCapacityGB = laptop.RamCapacityGB,
            RamSlots = laptop.RamSlots,
            RamSpeed = laptop.RamSpeed,
            RamUpgradeable = laptop.RamUpgradeable,
            StorageType = laptop.StorageType,
            StorageCapacityGB = laptop.StorageCapacityGB,
            StorageInterface = laptop.StorageInterface,
            NvMeSupport = laptop.NvMeSupport,
            GpuType = laptop.GpuType,
            GpuBrand = laptop.GpuBrand,
            GpuModel = laptop.GpuModel,
            GpuVramGB = laptop.GpuVramGB,
            DisplaySizeInches = laptop.DisplaySizeInches,
            DisplayResolution = laptop.DisplayResolution,
            DisplayPanelType = laptop.DisplayPanelType,
            DisplayRefreshRateHz = laptop.DisplayRefreshRateHz,
            DisplayTouchscreen = laptop.DisplayTouchscreen,
            BatteryCapacityWh = laptop.BatteryCapacityWh,
            WeightKg = laptop.WeightKg,
            Dimensions = laptop.Dimensions,
            Color = laptop.Color,
            Ports = laptop.Ports,
            WiFi6Support = laptop.WiFi6Support,
            BluetoothSupport = laptop.BluetoothSupport,
            BluetoothVersion = laptop.BluetoothVersion,
            WarrantyPeriod = laptop.WarrantyPeriod,
            TargetAudience = laptop.TargetAudience,
            // Variant properties
            ParentProductId = baseDto.ParentProductId,
            VariantName = baseDto.VariantName,
            VariantSku = baseDto.VariantSku,
            IsVariant = baseDto.IsVariant,
            IsBaseProduct = baseDto.IsBaseProduct,
            Variants = baseDto.Variants
        };
    }

    private AccessoryDto MapToAccessoryDto(Accessory accessory)
    {
        var baseDto = MapToProductDto(accessory);
        return new AccessoryDto
        {
            Id = baseDto.Id,
            Name = baseDto.Name,
            Description = baseDto.Description,
            Brand = baseDto.Brand,
            Model = baseDto.Model,
            Price = baseDto.Price,
            SKU = baseDto.SKU,
            IsActive = baseDto.IsActive,
            ProductType = baseDto.ProductType,
            StockQuantity = baseDto.StockQuantity,
            CreatedAt = baseDto.CreatedAt,
            UpdatedAt = baseDto.UpdatedAt,
            Images = baseDto.Images,
            Specifications = baseDto.Specifications,
            Inventory = baseDto.Inventory,
            AccessoryType = accessory.AccessoryType,
            Compatibility = accessory.Compatibility,
            SpecificationDetails = accessory.Specifications,
            Color = accessory.Color,
            Connectivity = accessory.Connectivity,
            // Variant properties
            ParentProductId = baseDto.ParentProductId,
            VariantName = baseDto.VariantName,
            VariantSku = baseDto.VariantSku,
            IsVariant = baseDto.IsVariant,
            IsBaseProduct = baseDto.IsBaseProduct,
            Variants = baseDto.Variants
        };
    }

    private BundleDto MapToBundleDto(Bundle bundle)
    {
        var baseDto = MapToProductDto(bundle);
        return new BundleDto
        {
            Id = baseDto.Id,
            Name = baseDto.Name,
            Description = baseDto.Description,
            Brand = baseDto.Brand,
            Model = baseDto.Model,
            Price = baseDto.Price,
            SKU = baseDto.SKU,
            IsActive = baseDto.IsActive,
            ProductType = baseDto.ProductType,
            StockQuantity = baseDto.StockQuantity,
            CreatedAt = baseDto.CreatedAt,
            UpdatedAt = baseDto.UpdatedAt,
            Images = baseDto.Images,
            Specifications = baseDto.Specifications,
            Inventory = baseDto.Inventory,
            BundleType = bundle.BundleType,
            DiscountPercentage = bundle.DiscountPercentage,
            ValidFrom = bundle.ValidFrom,
            ValidTo = bundle.ValidTo,
            BundleItems = bundle.BundleItems?.Select(bi => new BundleItemDto
            {
                Id = bi.Id,
                ProductId = bi.ProductId,
                ProductName = bi.Product?.Name ?? "",
                ProductSku = bi.Product?.SKU ?? "",
                ProductImageUrl = bi.Product?.Images?.FirstOrDefault(img => img.IsPrimary)?.ImageUrl
                                ?? bi.Product?.Images?.FirstOrDefault()?.ImageUrl,
                Brand = bi.Product?.Brand ?? "",
                OriginalPrice = bi.Product?.Price ?? 0,
                Quantity = bi.Quantity,
                DiscountPercentage = bi.DiscountPercentage,
                DiscountedPrice = CalculateDiscountedPrice(bi.Product?.Price ?? 0, bi.DiscountPercentage),
                TotalPrice = CalculateDiscountedPrice(bi.Product?.Price ?? 0, bi.DiscountPercentage) * bi.Quantity,
                IsAvailable = bi.Product?.IsActive ?? false,
                StockQuantity = bi.Product?.Inventory?.AvailableQuantity ?? 0
            }).ToList() ?? new List<BundleItemDto>(),
            // Variant properties
            ParentProductId = baseDto.ParentProductId,
            VariantName = baseDto.VariantName,
            VariantSku = baseDto.VariantSku,
            IsVariant = baseDto.IsVariant,
            IsBaseProduct = baseDto.IsBaseProduct,
            Variants = baseDto.Variants
        };
    }

    /// <summary>
    /// Helper method to calculate discounted price
    /// </summary>
    private static decimal CalculateDiscountedPrice(decimal originalPrice, decimal discountPercentage)
    {
        return originalPrice * (1 - discountPercentage / 100);
    }

    private ProductImageDto MapToProductImageDto(ProductImage image)
    {
        return new ProductImageDto
        {
            Id = image.Id,
            ImageUrl = image.ImageUrl,
            AltText = image.AltText,
            SortOrder = image.SortOrder,
            IsPrimary = image.IsPrimary,
            ImageId = image.ImageId,
            DeleteUrl = image.DeleteUrl,
            DisplayOrder = image.DisplayOrder
        };
    }

    private InventoryDto MapToInventoryDto(Inventory inventory)
    {
        return new InventoryDto
        {
            Id = inventory.Id,
            ProductId = inventory.ProductId,
            ProductName = "", // This would need to be loaded separately
            QuantityInStock = inventory.QuantityInStock,
            ReservedQuantity = inventory.ReservedQuantity,
            ReorderLevel = inventory.ReorderLevel,
            MaxStockLevel = inventory.MaxStockLevel,
            WarehouseLocation = inventory.WarehouseLocation,
            LastStockUpdate = inventory.LastStockUpdate
        };
    }

    private Product MapToProduct(JsonElement request, string productType)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rawJson = request.GetRawText();

        return productType.ToLower() switch
        {
            "laptop" => JsonSerializer.Deserialize<Laptop>(rawJson, options) ?? throw new JsonException("Failed to deserialize to Laptop."),
            "accessory" => JsonSerializer.Deserialize<Accessory>(rawJson, options) ?? throw new JsonException("Failed to deserialize to Accessory."),
            "bundle" => JsonSerializer.Deserialize<Bundle>(rawJson, options) ?? throw new JsonException("Failed to deserialize to Bundle."),
            _ => throw new ArgumentException("Invalid product type specified.")
        };
    }

    private void ApplyProductUpdates(Product product, JsonElement request)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var rawJson = request.GetRawText();

        // Use a temporary JObject to avoid applying nulls over existing values
        var updateData = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(rawJson, options);

        if (updateData == null) return;

        // Update base product properties
        if (updateData.TryGetValue(nameof(UpdateProductRequest.Name), out var name) && name.ValueKind == JsonValueKind.String) product.Name = name.GetString()!;
        if (updateData.TryGetValue(nameof(UpdateProductRequest.Description), out var desc) && desc.ValueKind == JsonValueKind.String) product.Description = desc.GetString()!;
        if (updateData.TryGetValue(nameof(UpdateProductRequest.Brand), out var brand) && brand.ValueKind == JsonValueKind.String) product.Brand = brand.GetString()!;
        if (updateData.TryGetValue(nameof(UpdateProductRequest.Model), out var model) && model.ValueKind == JsonValueKind.String) product.Model = model.GetString()!;
        if (updateData.TryGetValue(nameof(UpdateProductRequest.Price), out var price) && price.TryGetDecimal(out var priceValue)) product.Price = priceValue;
        if (updateData.TryGetValue(nameof(UpdateProductRequest.IsActive), out var isActive))
        {
            if (isActive.ValueKind == JsonValueKind.True) product.IsActive = true;
            else if (isActive.ValueKind == JsonValueKind.False) product.IsActive = false;
        }


        // Update type-specific properties
        switch (product)
        {
            case Laptop laptop:
                var laptopUpdate = JsonSerializer.Deserialize<UpdateLaptopRequest>(rawJson, options);
                if (laptopUpdate == null) break;
                // This is a bit verbose, but ensures we only update non-null values from the request
                laptop.Series = laptopUpdate.Series ?? laptop.Series;
                laptop.CpuBrand = laptopUpdate.CpuBrand ?? laptop.CpuBrand;
                laptop.CpuModel = laptopUpdate.CpuModel ?? laptop.CpuModel;
                laptop.CpuGeneration = laptopUpdate.CpuGeneration ?? laptop.CpuGeneration;
                laptop.CpuCores = laptopUpdate.CpuCores ?? laptop.CpuCores;
                laptop.CpuBaseClockGHz = laptopUpdate.CpuBaseClockGHz ?? laptop.CpuBaseClockGHz;
                laptop.CpuBoostClockGHz = laptopUpdate.CpuBoostClockGHz ?? laptop.CpuBoostClockGHz;
                laptop.CpuCache = laptopUpdate.CpuCache ?? laptop.CpuCache;
                laptop.RamType = laptopUpdate.RamType ?? laptop.RamType;
                laptop.RamCapacityGB = laptopUpdate.RamCapacityGB ?? laptop.RamCapacityGB;
                laptop.RamSlots = laptopUpdate.RamSlots ?? laptop.RamSlots;
                laptop.RamSpeed = laptopUpdate.RamSpeed ?? laptop.RamSpeed;
                laptop.RamUpgradeable = laptopUpdate.RamUpgradeable ?? laptop.RamUpgradeable;
                laptop.StorageType = laptopUpdate.StorageType ?? laptop.StorageType;
                laptop.StorageCapacityGB = laptopUpdate.StorageCapacityGB ?? laptop.StorageCapacityGB;
                laptop.StorageInterface = laptopUpdate.StorageInterface ?? laptop.StorageInterface;
                laptop.NvMeSupport = laptopUpdate.NvMeSupport ?? laptop.NvMeSupport;
                laptop.GpuType = laptopUpdate.GpuType ?? laptop.GpuType;
                laptop.GpuBrand = laptopUpdate.GpuBrand ?? laptop.GpuBrand;
                laptop.GpuModel = laptopUpdate.GpuModel ?? laptop.GpuModel;
                laptop.GpuVramGB = laptopUpdate.GpuVramGB ?? laptop.GpuVramGB;
                laptop.DisplaySizeInches = laptopUpdate.DisplaySizeInches ?? laptop.DisplaySizeInches;
                laptop.DisplayResolution = laptopUpdate.DisplayResolution ?? laptop.DisplayResolution;
                laptop.DisplayPanelType = laptopUpdate.DisplayPanelType ?? laptop.DisplayPanelType;
                laptop.DisplayRefreshRateHz = laptopUpdate.DisplayRefreshRateHz ?? laptop.DisplayRefreshRateHz;
                laptop.DisplayTouchscreen = laptopUpdate.DisplayTouchscreen ?? laptop.DisplayTouchscreen;
                laptop.BatteryCapacityWh = laptopUpdate.BatteryCapacityWh ?? laptop.BatteryCapacityWh;
                laptop.WeightKg = laptopUpdate.WeightKg ?? laptop.WeightKg;
                laptop.Dimensions = laptopUpdate.Dimensions ?? laptop.Dimensions;
                laptop.Color = laptopUpdate.Color ?? laptop.Color;
                laptop.Ports = laptopUpdate.Ports ?? laptop.Ports;
                laptop.WiFi6Support = laptopUpdate.WiFi6Support ?? laptop.WiFi6Support;
                laptop.BluetoothSupport = laptopUpdate.BluetoothSupport ?? laptop.BluetoothSupport;
                laptop.BluetoothVersion = laptopUpdate.BluetoothVersion ?? laptop.BluetoothVersion;
                laptop.WarrantyPeriod = laptopUpdate.WarrantyPeriod ?? laptop.WarrantyPeriod;
                laptop.TargetAudience = laptopUpdate.TargetAudience ?? laptop.TargetAudience;
                break;

            case Accessory accessory:
                var accessoryUpdate = JsonSerializer.Deserialize<UpdateAccessoryRequest>(rawJson, options);
                if (accessoryUpdate == null) break;
                accessory.AccessoryType = accessoryUpdate.AccessoryType ?? accessory.AccessoryType;
                accessory.Compatibility = accessoryUpdate.Compatibility ?? accessory.Compatibility;
                accessory.Specifications = accessoryUpdate.SpecificationDetails ?? accessory.Specifications;
                accessory.Color = accessoryUpdate.Color ?? accessory.Color;
                accessory.Connectivity = accessoryUpdate.Connectivity ?? accessory.Connectivity;
                break;
        }
    }

    private List<ProductSpecificationDto> GenerateProductSpecifications(Product product)
    {
        var specifications = new List<ProductSpecificationDto>();
        int displayOrder = 0;

        switch (product)
        {
            case Laptop laptop:
                // CPU Specifications
                if (!string.IsNullOrEmpty(laptop.CpuBrand))
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "CPU Brand", Value = laptop.CpuBrand, Category = "Processor", DisplayOrder = displayOrder });
                if (!string.IsNullOrEmpty(laptop.CpuModel))
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "CPU Model", Value = laptop.CpuModel, Category = "Processor", DisplayOrder = displayOrder });
                if (laptop.CpuCores > 0)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "CPU Cores", Value = laptop.CpuCores.ToString(), Category = "Processor", DisplayOrder = displayOrder });
                if (laptop.CpuBaseClockGHz > 0)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Base Clock", Value = $"{laptop.CpuBaseClockGHz} GHz", Category = "Processor", DisplayOrder = displayOrder });
                if (laptop.CpuBoostClockGHz > 0)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Boost Clock", Value = $"{laptop.CpuBoostClockGHz} GHz", Category = "Processor", DisplayOrder = displayOrder });

                // RAM Specifications
                if (!string.IsNullOrEmpty(laptop.RamType))
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "RAM Type", Value = laptop.RamType, Category = "Memory", DisplayOrder = displayOrder });
                if (laptop.RamCapacityGB > 0)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "RAM Capacity", Value = $"{laptop.RamCapacityGB} GB", Category = "Memory", DisplayOrder = displayOrder });
                if (laptop.RamSpeed > 0)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "RAM Speed", Value = $"{laptop.RamSpeed} MHz", Category = "Memory", DisplayOrder = displayOrder });

                // Storage Specifications
                if (!string.IsNullOrEmpty(laptop.StorageType))
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Storage Type", Value = laptop.StorageType, Category = "Storage", DisplayOrder = displayOrder });
                if (laptop.StorageCapacityGB > 0)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Storage Capacity", Value = $"{laptop.StorageCapacityGB} GB", Category = "Storage", DisplayOrder = displayOrder });

                // GPU Specifications
                if (!string.IsNullOrEmpty(laptop.GpuBrand))
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "GPU Brand", Value = laptop.GpuBrand, Category = "Graphics", DisplayOrder = displayOrder });
                if (!string.IsNullOrEmpty(laptop.GpuModel))
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "GPU Model", Value = laptop.GpuModel, Category = "Graphics", DisplayOrder = displayOrder });
                if (laptop.GpuVramGB > 0)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "VRAM", Value = $"{laptop.GpuVramGB} GB", Category = "Graphics", DisplayOrder = displayOrder });

                // Display Specifications
                if (laptop.DisplaySizeInches > 0)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Display Size", Value = $"{laptop.DisplaySizeInches}\"", Category = "Display", DisplayOrder = displayOrder });
                if (!string.IsNullOrEmpty(laptop.DisplayResolution))
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Resolution", Value = laptop.DisplayResolution, Category = "Display", DisplayOrder = displayOrder });
                if (laptop.DisplayRefreshRateHz > 0)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Refresh Rate", Value = $"{laptop.DisplayRefreshRateHz} Hz", Category = "Display", DisplayOrder = displayOrder });

                // Physical Specifications
                if (laptop.WeightKg > 0)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Weight", Value = $"{laptop.WeightKg} kg", Category = "Physical", DisplayOrder = displayOrder });
                if (!string.IsNullOrEmpty(laptop.Dimensions))
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Dimensions", Value = laptop.Dimensions, Category = "Physical", DisplayOrder = displayOrder });
                if (laptop.BatteryCapacityWh > 0)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Battery", Value = $"{laptop.BatteryCapacityWh} Wh", Category = "Physical", DisplayOrder = displayOrder });
                break;

            case Accessory accessory:
                if (!string.IsNullOrEmpty(accessory.AccessoryType))
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Type", Value = accessory.AccessoryType, Category = "General", DisplayOrder = displayOrder });
                if (!string.IsNullOrEmpty(accessory.Compatibility))
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Compatibility", Value = accessory.Compatibility, Category = "General", DisplayOrder = displayOrder });
                if (!string.IsNullOrEmpty(accessory.Specifications))
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Details", Value = accessory.Specifications, Category = "General", DisplayOrder = displayOrder });
                if (!string.IsNullOrEmpty(accessory.Color))
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Color", Value = accessory.Color, Category = "Design", DisplayOrder = displayOrder });
                if (!string.IsNullOrEmpty(accessory.Connectivity))
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Connectivity", Value = accessory.Connectivity, Category = "Technical", DisplayOrder = displayOrder });
                break;

            case Bundle bundle:
                if (bundle.DiscountPercentage > 0)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Discount", Value = $"{bundle.DiscountPercentage}%", Category = "Bundle", DisplayOrder = displayOrder });
                if (bundle.ValidFrom.HasValue)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Valid From", Value = bundle.ValidFrom.Value.ToString("yyyy-MM-dd"), Category = "Bundle", DisplayOrder = displayOrder });
                if (bundle.ValidTo.HasValue)
                    specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Valid To", Value = bundle.ValidTo.Value.ToString("yyyy-MM-dd"), Category = "Bundle", DisplayOrder = displayOrder });
                break;
        }

        // Common specifications for all products
        specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Brand", Value = product.Brand, Category = "General", DisplayOrder = displayOrder });
        specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "Model", Value = product.Model, Category = "General", DisplayOrder = displayOrder });
        specifications.Add(new ProductSpecificationDto { Id = ++displayOrder, Name = "SKU", Value = product.SKU, Category = "General", DisplayOrder = displayOrder });

        return specifications.OrderBy(s => s.Category).ThenBy(s => s.DisplayOrder).ToList();
    }

    #endregion

    #region T005 Enhanced Product Catalog Operations

    /// <summary>
    /// Gets laptops with advanced hardware filtering
    /// Implements T005 requirement for detailed laptop specification filtering
    /// </summary>
    [HttpGet("laptops/advanced")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLaptopsAdvanced(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? brand = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? cpuBrand = null,
        [FromQuery] string? cpuGeneration = null,
        [FromQuery] int? minCpuCores = null,
        [FromQuery] int? minRamGB = null,
        [FromQuery] int? maxRamGB = null,
        [FromQuery] string? ramType = null,
        [FromQuery] string? storageType = null,
        [FromQuery] int? minStorageGB = null,
        [FromQuery] string? gpuType = null,
        [FromQuery] string? gpuBrand = null,
        [FromQuery] decimal? minDisplaySize = null,
        [FromQuery] decimal? maxDisplaySize = null,
        [FromQuery] string? displayResolution = null,
        [FromQuery] int? minRefreshRate = null,
        [FromQuery] bool? touchscreen = null,
        [FromQuery] string? targetAudience = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _productService.GetLaptopsWithAdvancedFilteringAsync(
                page, pageSize, search, brand, minPrice, maxPrice,
                cpuBrand, cpuGeneration, minCpuCores, minRamGB, maxRamGB,
                ramType, storageType, minStorageGB, gpuType, gpuBrand,
                minDisplaySize, maxDisplaySize, displayResolution,
                minRefreshRate, touchscreen, targetAudience);

            var laptopDtos = result.Items.Select(MapToLaptopDto);

            return SuccessResponse(new
            {
                Items = laptopDtos,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = (int)Math.Ceiling((double)result.TotalCount / result.PageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting advanced laptop filters");
            return ErrorResponse("Failed to retrieve laptops");
        }
    }

    /// <summary>
    /// Gets accessories with compatibility filtering
    /// </summary>
    [HttpGet("accessories/compatible")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompatibleAccessories(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? accessoryType = null,
        [FromQuery] string? compatibility = null,
        [FromQuery] int? productId = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _productService.GetAccessoriesWithCompatibilityAsync(
                page, pageSize, search, accessoryType, compatibility, productId);

            var accessoryDtos = result.Items.Select(MapToAccessoryDto);

            return SuccessResponse(new
            {
                Items = accessoryDtos,
                TotalCount = result.TotalCount,
                Page = result.Page,
                PageSize = result.PageSize,
                TotalPages = (int)Math.Ceiling((double)result.TotalCount / result.PageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting compatible accessories");
            return ErrorResponse("Failed to retrieve accessories");
        }
    }

    /// <summary>
    /// Creates a product bundle with dynamic pricing
    /// </summary>
    [HttpPost("bundles")]
    [Authorize(Policy = "RequirePermission:products:write")]
    public async Task<IActionResult> CreateBundle([FromBody] CreateBundleRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var bundle = await _productService.CreateBundleAsync(
                request.Name,
                request.Description,
                request.ProductIds,
                request.DiscountPercentage,
                request.ValidFrom,
                request.ValidTo);

            return SuccessResponse(MapToBundleDto(bundle), "Bundle created successfully");
        }
        catch (ArgumentException ex)
        {
            return ErrorResponse(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bundle");
            return ErrorResponse("Failed to create bundle");
        }
    }

    /// <summary>
    /// Calculates bundle pricing for given products
    /// </summary>
    [HttpPost("bundles/calculate-price")]
    [Authorize(Policy = "RequirePermission:products:read")]
    public async Task<IActionResult> CalculateBundlePrice([FromBody] CalculateBundlePriceRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var price = await _productService.CalculateBundlePriceAsync(request.ProductIds, request.DiscountPercentage);

            return SuccessResponse(new { CalculatedPrice = price });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating bundle price");
            return ErrorResponse("Failed to calculate bundle price");
        }
    }

    /// <summary>
    /// Gets product specifications as structured data
    /// </summary>
    [HttpGet("{id}/specifications")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductSpecifications(int id)
    {
        try
        {
            var specifications = await _productService.GetProductSpecificationsAsync(id);

            if (!specifications.Any())
                return NotFound("Product not found");

            return SuccessResponse(specifications);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product specifications for ID: {ProductId}", id);
            return ErrorResponse("Failed to retrieve product specifications");
        }
    }

    /// <summary>
    /// Validates compatibility between a product and accessory
    /// </summary>
    [HttpGet("{productId}/compatibility/{accessoryId}")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateCompatibility(int productId, int accessoryId)
    {
        try
        {
            var isCompatible = await _productService.ValidateProductCompatibilityAsync(productId, accessoryId);

            return SuccessResponse(new { IsCompatible = isCompatible });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating compatibility between {ProductId} and {AccessoryId}", productId, accessoryId);
            return ErrorResponse("Failed to validate compatibility");
        }
    }

    /// <summary>
    /// Gets product recommendations for a user
    /// </summary>
    [HttpGet("recommendations")]
    [Authorize]
    public async Task<IActionResult> GetRecommendations([FromQuery] int count = 5)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var recommendations = await _productService.GetRecommendedProductsAsync(userId.Value, count);
            var recommendationDtos = recommendations.Select(MapToProductDto);

            return SuccessResponse<IEnumerable<ProductDto>>(recommendationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recommendations");
            return ErrorResponse("Failed to retrieve recommendations");
        }
    }

    /// <summary>
    /// Bulk updates product pricing
    /// </summary>
    [HttpPut("bulk/pricing")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    public async Task<IActionResult> BulkUpdatePricing([FromBody] BulkPricingUpdateRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _productService.BulkUpdatePricingAsync(request.ProductIds, request.PriceAdjustmentPercentage);

            if (success)
                return SuccessResponse<object?>(null, "Pricing updated successfully");
            else
                return ErrorResponse("Failed to update pricing");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bulk pricing");
            return ErrorResponse("Failed to update pricing");
        }
    }

    #endregion

    #region Variant Image Upload Operations

    /// <summary>
    /// Uploads variant images to ImgBB cloud hosting
    /// </summary>
    /// <param name="id">Base product ID</param>
    /// <param name="variantId">Variant ID</param>
    /// <param name="files">Image files to upload</param>
    /// <returns>Uploaded image URLs and details</returns>
    [HttpPost("{id}/variants/{variantId}/images")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadVariantImages(int id, int variantId, IFormFileCollection files)
    {
        try
        {
            // Debug logging
            _logger.LogInformation("UploadVariantImages called with {FileCount} files for variant {VariantId} of product {ProductId}",
                files?.Count ?? 0, variantId, id);

            if (files != null)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    _logger.LogInformation("File {Index}: {FileName}, Size: {Size} bytes",
                        i + 1, file.FileName, file.Length);
                }
            }

            if (files == null || files.Count == 0)
                return ErrorResponse("No files provided", 400);

            // Verify variant exists and belongs to the base product
            var variant = await _productService.GetByIdAsync(variantId);
            if (variant == null || variant.ParentProductId != id)
                return ErrorResponse("Variant not found", 404);

            var uploadResults = new List<ImageUploadResult>();
            var productImages = new List<ProductImage>();

            _logger.LogInformation("Starting upload of {FileCount} files for variant {VariantId}",
                files.Count, variantId);

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                _logger.LogInformation("Uploading file {Index}/{Total}: {FileName} ({Size} bytes)",
                    i + 1, files.Count, file.FileName, file.Length);

                // Validate file
                if (!_imageHostingService.IsValidImage(file.FileName, file.ContentType, file.Length))
                {
                    return ErrorResponse($"Invalid image file: {file.FileName}", 400);
                }

                try
                {
                    // Upload to ImgBB
                    using var stream = file.OpenReadStream();
                    var uploadResult = await _imageHostingService.UploadImageAsync(stream, file.FileName, ImageCategory.Products);
                    uploadResults.Add(uploadResult);

                    _logger.LogInformation("Successfully uploaded file {Index}/{Total}: {FileName}",
                        i + 1, files.Count, file.FileName);

                    // Prepare ProductImage entity
                    productImages.Add(new ProductImage
                    {
                        ProductId = variantId,
                        ImageUrl = uploadResult.Url,
                        AltText = $"{variant.Name} - Image",
                        DisplayOrder = productImages.Count + 1,
                        ImageId = uploadResult.ImageId,
                        DeleteUrl = uploadResult.DeleteUrl
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload image {FileName} for variant {VariantId}", file.FileName, variantId);

                    // Cleanup already uploaded images on error
                    foreach (var uploadedResult in uploadResults)
                    {
                        try
                        {
                            await _imageHostingService.DeleteImageAsync(uploadedResult.ImageId);
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogError(cleanupEx, "Failed to cleanup uploaded image {ImageId}", uploadedResult.ImageId);
                        }
                    }

                    return ErrorResponse($"Failed to upload image: {file.FileName}", 500);
                }
            }

            // Save all images to database
            _context.ProductImages.AddRange(productImages);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully uploaded and saved {ImageCount} images for variant {VariantId}",
                productImages.Count, variantId);

            return Ok(new
            {
                success = true,
                message = $"Successfully uploaded {productImages.Count} images",
                data = new
                {
                    variantId = variantId,
                    uploadedImages = uploadResults.Select((r, idx) => new
                    {
                        imageId = r.ImageId,
                        imageUrl = r.Url,
                        displayOrder = idx + 1,
                        message = "Upload successful"
                    }).ToList()
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UploadVariantImages for variant {VariantId}", variantId);
            return HandleException(ex, nameof(UploadVariantImages));
        }
    }

    /// <summary>
    /// Deletes a variant image
    /// </summary>
    /// <param name="id">Base product ID</param>
    /// <param name="variantId">Variant ID</param>
    /// <param name="imageId">Image ID to delete</param>
    /// <returns>Success response</returns>
    [HttpDelete("{id}/variants/{variantId}/images/{imageId}")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    public async Task<IActionResult> DeleteVariantImage(int id, int variantId, string imageId)
    {
        try
        {
            // Verify variant exists and belongs to the base product
            var variant = await _productService.GetByIdAsync(variantId);
            if (variant == null || variant.ParentProductId != id)
                return ErrorResponse("Variant not found", 404);

            // Find the image - try ImageId first (ImgBB ID), then Id (database ID)
            var image = await _context.ProductImages
                .FirstOrDefaultAsync(img => img.ProductId == variantId && img.ImageId == imageId)
                ?? await _context.ProductImages
                .FirstOrDefaultAsync(img => img.ProductId == variantId && img.Id.ToString() == imageId);

            if (image == null)
                return ErrorResponse("Image not found", 404);

            // Delete from ImgBB
            if (!string.IsNullOrEmpty(image.DeleteUrl))
            {
                try
                {
                    await _imageHostingService.DeleteImageAsync(image.DeleteUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete image {ImageId} from ImgBB, but continuing with database cleanup", imageId);
                }
            }

            // Remove from database
            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            // Log admin activity for image deletion
            var adminUserId = GetCurrentUserId();
            if (adminUserId.HasValue)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                await _auditLoggingService.LogAdminActivityAsync(
                    adminUserId.Value,
                    "variant_image_deleted",
                    $"Admin deleted variant image {imageId} from variant {variantId} of product {id}",
                    ipAddress ?? "Unknown",
                    userAgent ?? "Unknown",
                    targetResource: $"Product:{id}/Variant:{variantId}/Image:{imageId}"
                );
            }

            return Ok(new
            {
                success = true,
                message = "Image deleted successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting variant image {ImageId} for variant {VariantId}", imageId, variantId);
            return HandleException(ex, nameof(DeleteVariantImage));
        }
    }

    #endregion

    #region Image Upload Operations

    /// <summary>
    /// Uploads product images to ImgBB cloud hosting
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="files">Image files to upload</param>
    /// <returns>Uploaded image URLs and details</returns>
    [HttpPost("{id}/images")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadProductImages(int id, IFormFileCollection files)
    {
        try
        {
            // Debug logging
            _logger.LogInformation("UploadProductImages called with {FileCount} files for product {ProductId}",
                files?.Count ?? 0, id);

            if (files != null)
            {
                for (int i = 0; i < files.Count; i++)
                {
                    var file = files[i];
                    _logger.LogInformation("File {Index}: {FileName}, Size: {Size} bytes",
                        i + 1, file.FileName, file.Length);
                }
            }

            if (files == null || files.Count == 0)
                return ErrorResponse("No files provided", 400);

            var product = await _productService.GetByIdAsync(id);
            if (product == null)
                return ErrorResponse("Product not found", 404);

            var uploadResults = new List<ImageUploadResult>();
            var productImages = new List<ProductImage>();

            _logger.LogInformation("Starting upload of {FileCount} files for product {ProductId}",
                files.Count, id);

            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                _logger.LogInformation("Uploading file {Index}/{Total}: {FileName} ({Size} bytes)",
                    i + 1, files.Count, file.FileName, file.Length);

                // Validate file
                if (!_imageHostingService.IsValidImage(file.FileName, file.ContentType, file.Length))
                {
                    return ErrorResponse($"Invalid image file: {file.FileName}", 400);
                }

                try
                {
                    // Upload to ImgBB
                    using var stream = file.OpenReadStream();
                    var uploadResult = await _imageHostingService.UploadImageAsync(stream, file.FileName, ImageCategory.Products);
                    uploadResults.Add(uploadResult);

                    _logger.LogInformation("Successfully uploaded file {Index}/{Total}: {FileName}",
                        i + 1, files.Count, file.FileName);

                    // Prepare ProductImage entity
                    productImages.Add(new ProductImage
                    {
                        ProductId = id,
                        ImageUrl = uploadResult.Url,
                        AltText = $"{product.Name} - Image",
                        DisplayOrder = productImages.Count + 1,
                        ImageId = uploadResult.ImageId,
                        DeleteUrl = uploadResult.DeleteUrl
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to upload image {FileName} for product {ProductId}", file.FileName, id);

                    // Cleanup already uploaded images on error
                    foreach (var result in uploadResults)
                    {
                        try
                        {
                            await _imageHostingService.DeleteImageAsync(result.DeleteUrl);
                        }
                        catch (Exception cleanupEx)
                        {
                            _logger.LogError(cleanupEx, "Failed to cleanup image {ImageId} during error handling", result.ImageId);
                        }
                    }

                    return ErrorResponse($"Failed to upload image {file.FileName}: {ex.Message}", 500);
                }
            }

            // Update product images in database
            var success = await _productService.UpdateProductImagesAsync(id, productImages);
            if (!success)
            {
                // Cleanup uploaded images if database update fails
                foreach (var result in uploadResults)
                {
                    try
                    {
                        await _imageHostingService.DeleteImageAsync(result.DeleteUrl);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Failed to cleanup image {ImageId} after database error", result.ImageId);
                    }
                }

                return ErrorResponse("Failed to update product images", 500);
            }

            _logger.LogInformation("Successfully uploaded {Count} images for product {ProductId}", uploadResults.Count, id);

            return SuccessResponse(new
            {
                productId = id,
                uploadedImages = uploadResults.Select(r => new
                {
                    url = r.Url,
                    imageId = r.ImageId,
                    fileName = r.FileName,
                    size = r.Size,
                    dimensions = new { width = r.Width, height = r.Height }
                }),
                message = $"Successfully uploaded {uploadResults.Count} image(s)"
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(UploadProductImages));
        }
    }

    /// <summary>
    /// Deletes a specific product image
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="imageId">Image ID to delete</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}/images/{imageId}")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    public async Task<IActionResult> DeleteProductImage(int id, string imageId)
    {
        try
        {
            var product = await _productService.GetByIdAsync(id);
            if (product == null)
                return ErrorResponse("Product not found", 404);

            // Try to find image by ImageId first (ImgBB ID), then by Id (database ID)
            var productImage = product.Images?.FirstOrDefault(i => i.ImageId == imageId)
                             ?? product.Images?.FirstOrDefault(i => i.Id.ToString() == imageId);
            if (productImage == null)
                return ErrorResponse("Image not found", 404);

            // Delete from ImgBB
            if (!string.IsNullOrEmpty(productImage.DeleteUrl))
            {
                var deleteSuccess = await _imageHostingService.DeleteImageAsync(productImage.DeleteUrl);
                if (!deleteSuccess)
                {
                    _logger.LogWarning("Failed to delete image {ImageId} from ImgBB for product {ProductId}", imageId, id);
                    // Continue with database deletion even if ImgBB deletion fails
                }
            }


            // Remove from database
            if (product.Images != null)
            {
                var imageToRemove = product.Images.FirstOrDefault(i =>
                    (i.ImageId == imageId) || (i.Id.ToString() == imageId));

                if (imageToRemove != null)
                {
                    _context.ProductImages.Remove(imageToRemove);
                    await _context.SaveChangesAsync();
                }
            }


            _logger.LogInformation("Successfully deleted image {ImageId} from product {ProductId}", imageId, id);

            // Log admin activity for image deletion
            var adminUserId = GetCurrentUserId();
            if (adminUserId.HasValue)
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                await _auditLoggingService.LogAdminActivityAsync(
                    adminUserId.Value,
                    "product_image_deleted",
                    $"Admin deleted image {imageId} from product {id}",
                    ipAddress ?? "Unknown",
                    userAgent ?? "Unknown",
                    targetResource: $"Product:{id}/Image:{imageId}"
                );
            }

            return SuccessResponse(new
            {
                productId = id,
                deletedImageId = imageId,
                message = "Image deleted successfully"
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(DeleteProductImage));
        }
    }

    #endregion

    #region Admin Endpoints

    /// <summary>
    /// Get all products for admin management (includes all product data)
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Policy = "RequirePermission:products:read")]
    public async Task<IActionResult> GetAdminProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] string? brand = null,
        [FromQuery] string? status = null,
        [FromQuery] string? productType = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var result = await _productService.GetAdminProductsAsync(page, pageSize, search, type, brand, status, productType);

            return Ok(new
            {
                products = result.Items ?? new List<Product>(),
                totalCount = result.TotalCount,
                currentPage = result.Page,
                totalPages = result.TotalPages,
                pageSize = result.PageSize
            });
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetAdminProducts));
        }
    }

    #endregion

    #region Variant Management Endpoints

    /// <summary>
    /// Gets variants for a specific product
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>List of variants</returns>
    [HttpGet("{id}/variants")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVariants(int id)
    {
        try
        {
            var variants = await _productService.GetVariantsAsync(id);
            var variantDtos = variants.Select(v => v switch
            {
                Laptop laptop => MapToLaptopDto(laptop),
                Accessory accessory => MapToAccessoryDto(accessory),
                Bundle bundle => MapToBundleDto(bundle),
                _ => MapToProductDto(v)
            }).ToList();

            return SuccessResponse(variantDtos);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetVariants));
        }
    }

    /// <summary>
    /// Gets base product for a variant
    /// </summary>
    /// <param name="id">Variant ID</param>
    /// <returns>Base product</returns>
    [HttpGet("{id}/base-product")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBaseProduct(int id)
    {
        try
        {
            var baseProduct = await _productService.GetBaseProductAsync(id);
            if (baseProduct == null)
            {
                return ErrorResponse("Base product not found", 404);
            }

            var baseProductDto = baseProduct switch
            {
                Laptop laptop => MapToLaptopDto(laptop),
                Accessory accessory => MapToAccessoryDto(accessory),
                Bundle bundle => MapToBundleDto(bundle),
                _ => MapToProductDto(baseProduct)
            };

            return SuccessResponse(baseProductDto);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetBaseProduct));
        }
    }

    /// <summary>
    /// Creates a new variant for a product
    /// </summary>
    /// <param name="id">Base product ID</param>
    /// <param name="createVariantDto">Variant data</param>
    /// <returns>Created variant</returns>
    [HttpPost("{id}/variants")]
    [Authorize(Policy = "RequirePermission:products:write")]
    public async Task<IActionResult> CreateVariant(int id, [FromBody] CreateVariantDto createVariantDto)
    {
        try
        {
            // Get base product to determine type
            var baseProduct = await _productService.GetByIdAsync(id);
            if (baseProduct == null)
            {
                return ErrorResponse("Base product not found", 404);
            }

            // Create variant based on base product type
            Product variant = baseProduct switch
            {
                Laptop => new Laptop
                {
                    Name = $"{baseProduct.Name} - {createVariantDto.VariantName}",
                    Description = createVariantDto.Description ?? baseProduct.Description,
                    Brand = baseProduct.Brand,
                    Model = baseProduct.Model,
                    Price = createVariantDto.Price,
                    SKU = createVariantDto.VariantSku,
                    IsActive = createVariantDto.IsActive,
                    VariantName = createVariantDto.VariantName,
                    VariantSku = createVariantDto.VariantSku,
                    ParentProductId = id,

                    // M RNG: S dng gi tr t DTO hoc fallback v base product
                    Series = createVariantDto.Series ?? ((Laptop)baseProduct).Series,

                    // CPU Specifications - C TH THAY I
                    CpuBrand = createVariantDto.CpuBrand ?? ((Laptop)baseProduct).CpuBrand,
                    CpuModel = createVariantDto.CpuModel ?? ((Laptop)baseProduct).CpuModel,
                    CpuGeneration = createVariantDto.CpuGeneration ?? ((Laptop)baseProduct).CpuGeneration,
                    CpuCores = createVariantDto.CpuCores ?? ((Laptop)baseProduct).CpuCores,
                    CpuBaseClockGHz = createVariantDto.CpuBaseClockGHz ?? ((Laptop)baseProduct).CpuBaseClockGHz,
                    CpuBoostClockGHz = createVariantDto.CpuBoostClockGHz ?? ((Laptop)baseProduct).CpuBoostClockGHz,
                    CpuCache = createVariantDto.CpuCache ?? ((Laptop)baseProduct).CpuCache,

                    // RAM Specifications - C TH THAY I
                    RamType = createVariantDto.RamType ?? ((Laptop)baseProduct).RamType,
                    RamCapacityGB = createVariantDto.RamCapacityGB ?? ((Laptop)baseProduct).RamCapacityGB,
                    RamSlots = createVariantDto.RamSlots ?? ((Laptop)baseProduct).RamSlots,
                    RamSpeed = createVariantDto.RamSpeed ?? ((Laptop)baseProduct).RamSpeed,
                    RamUpgradeable = createVariantDto.RamUpgradeable ?? ((Laptop)baseProduct).RamUpgradeable,

                    // Storage Specifications - C TH THAY I
                    StorageType = createVariantDto.StorageType ?? ((Laptop)baseProduct).StorageType,
                    StorageCapacityGB = createVariantDto.StorageCapacityGB ?? ((Laptop)baseProduct).StorageCapacityGB,
                    StorageInterface = createVariantDto.StorageInterface ?? ((Laptop)baseProduct).StorageInterface,
                    NvMeSupport = createVariantDto.NvMeSupport ?? ((Laptop)baseProduct).NvMeSupport,

                    // GPU Specifications - C TH THAY I
                    GpuType = createVariantDto.GpuType ?? ((Laptop)baseProduct).GpuType,
                    GpuBrand = createVariantDto.GpuBrand ?? ((Laptop)baseProduct).GpuBrand,
                    GpuModel = createVariantDto.GpuModel ?? ((Laptop)baseProduct).GpuModel,
                    GpuVramGB = createVariantDto.GpuVramGB ?? ((Laptop)baseProduct).GpuVramGB,

                    // Display Specifications - C TH THAY I
                    DisplaySizeInches = createVariantDto.DisplaySizeInches ?? ((Laptop)baseProduct).DisplaySizeInches,
                    DisplayResolution = createVariantDto.DisplayResolution ?? ((Laptop)baseProduct).DisplayResolution,
                    DisplayPanelType = createVariantDto.DisplayPanelType ?? ((Laptop)baseProduct).DisplayPanelType,
                    DisplayRefreshRateHz = createVariantDto.DisplayRefreshRateHz ?? ((Laptop)baseProduct).DisplayRefreshRateHz,
                    DisplayTouchscreen = createVariantDto.DisplayTouchscreen ?? ((Laptop)baseProduct).DisplayTouchscreen,

                    // Physical Specifications - C TH THAY I
                    BatteryCapacityWh = createVariantDto.BatteryCapacityWh ?? ((Laptop)baseProduct).BatteryCapacityWh,
                    WeightKg = createVariantDto.WeightKg ?? ((Laptop)baseProduct).WeightKg,
                    Dimensions = createVariantDto.Dimensions ?? ((Laptop)baseProduct).Dimensions,
                    Color = createVariantDto.Color ?? ((Laptop)baseProduct).Color,
                    Ports = createVariantDto.Ports ?? ((Laptop)baseProduct).Ports,

                    // Connectivity - C TH THAY I
                    WiFi6Support = createVariantDto.WiFi6Support ?? ((Laptop)baseProduct).WiFi6Support,
                    BluetoothSupport = createVariantDto.BluetoothSupport ?? ((Laptop)baseProduct).BluetoothSupport,
                    BluetoothVersion = createVariantDto.BluetoothVersion ?? ((Laptop)baseProduct).BluetoothVersion,

                    // Business info - C TH THAY I
                    WarrantyPeriod = createVariantDto.WarrantyPeriod ?? ((Laptop)baseProduct).WarrantyPeriod,
                    TargetAudience = createVariantDto.TargetAudience ?? ((Laptop)baseProduct).TargetAudience,

                    // Copy relationships
                    CategoryId = baseProduct.CategoryId,
                    BrandId = baseProduct.BrandId
                },
                _ => throw new ArgumentException("Only laptop variants are currently supported")
            };

            var createdVariant = await _productService.CreateVariantAsync(id, variant);

            // Create inventory for variant
            var inventory = new Inventory
            {
                ProductId = createdVariant.Id,
                QuantityInStock = createVariantDto.StockQuantity,
                ReservedQuantity = 0,
                ReorderLevel = 10,
                LastStockUpdate = DateTime.UtcNow
            };

            _context.Inventories.Add(inventory);
            await _context.SaveChangesAsync();

            var variantDto = createdVariant switch
            {
                Laptop laptop => MapToLaptopDto(laptop),
                _ => MapToProductDto(createdVariant)
            };

            return CreatedAtAction(nameof(GetProduct), new { id = createdVariant.Id }, variantDto);
        }
        catch (ArgumentException ex)
        {
            return ErrorResponse(ex.Message, 400);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(CreateVariant));
        }
    }

    /// <summary>
    /// Updates a variant
    /// </summary>
    /// <param name="id">Base product ID</param>
    /// <param name="variantId">Variant ID</param>
    /// <param name="updateVariantDto">Updated variant data</param>
    /// <returns>Updated variant</returns>
    [HttpPut("{id}/variants/{variantId}")]
    [Authorize(Policy = "RequirePermission:products:write")]
    public async Task<IActionResult> UpdateVariant(int id, int variantId, [FromBody] UpdateVariantDto updateVariantDto)
    {
        try
        {
            // Get existing variant
            var existingVariant = await _productService.GetByIdAsync(variantId);
            if (existingVariant == null || existingVariant.ParentProductId != id)
            {
                return ErrorResponse("Variant not found", 404);
            }

            // Update variant properties
            if (!string.IsNullOrEmpty(updateVariantDto.VariantName))
            {
                existingVariant.VariantName = updateVariantDto.VariantName;
                existingVariant.Name = $"{existingVariant.ParentProduct?.Name} - {updateVariantDto.VariantName}";
            }

            if (!string.IsNullOrEmpty(updateVariantDto.VariantSku))
                existingVariant.VariantSku = updateVariantDto.VariantSku;

            if (updateVariantDto.Price.HasValue)
                existingVariant.Price = updateVariantDto.Price.Value;

            if (!string.IsNullOrEmpty(updateVariantDto.Description))
                existingVariant.Description = updateVariantDto.Description;

            if (updateVariantDto.IsActive.HasValue)
                existingVariant.IsActive = updateVariantDto.IsActive.Value;

            // Update laptop-specific properties
            if (existingVariant is Laptop laptop)
            {
                if (updateVariantDto.RamCapacityGB.HasValue)
                    laptop.RamCapacityGB = updateVariantDto.RamCapacityGB.Value;

                if (updateVariantDto.StorageCapacityGB.HasValue)
                    laptop.StorageCapacityGB = updateVariantDto.StorageCapacityGB.Value;

                if (!string.IsNullOrEmpty(updateVariantDto.Color))
                    laptop.Color = updateVariantDto.Color;

                if (!string.IsNullOrEmpty(updateVariantDto.GpuModel))
                    laptop.GpuModel = updateVariantDto.GpuModel;
            }

            var updatedVariant = await _productService.UpdateVariantAsync(variantId, existingVariant);

            // Update inventory stock quantity if provided
            if (updateVariantDto.StockQuantity.HasValue)
            {
                var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == variantId);
                if (inventory == null)
                {
                    // Create inventory if missing (edge case)
                    inventory = new Inventory
                    {
                        ProductId = variantId,
                        QuantityInStock = updateVariantDto.StockQuantity.Value,
                        ReservedQuantity = 0,
                        ReorderLevel = 10,
                        LastStockUpdate = DateTime.UtcNow
                    };
                    _context.Inventories.Add(inventory);
                }
                else
                {
                    var originalQuantity = inventory.QuantityInStock;
                    inventory.QuantityInStock = updateVariantDto.StockQuantity.Value;
                    inventory.LastStockUpdate = DateTime.UtcNow;

                    // Record transaction for audit trail
                    var quantityDifference = updateVariantDto.StockQuantity.Value - originalQuantity;
                    if (quantityDifference != 0)
                    {
                        var transactionRecord = new InventoryTransaction
                        {
                            InventoryId = inventory.Id,
                            Type = quantityDifference > 0 ? InventoryTransactionType.Adjustment : InventoryTransactionType.Sale,
                            Quantity = Math.Abs(quantityDifference),
                            Reference = "VARIANT_UPDATE",
                            Notes = $"Variant inventory update: {originalQuantity}  {updateVariantDto.StockQuantity.Value}",
                            CreatedAt = DateTime.UtcNow,
                            CreatedBy = 1
                        };
                        _context.InventoryTransactions.Add(transactionRecord);
                    }
                }

                await _context.SaveChangesAsync();
            }
            if (updatedVariant == null)
            {
                return ErrorResponse("Variant not found", 404);
            }

            var variantDto = updatedVariant switch
            {
                Laptop updatedLaptop => MapToLaptopDto(updatedLaptop),
                _ => MapToProductDto(updatedVariant)
            };

            return SuccessResponse(variantDto);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(UpdateVariant));
        }
    }

    /// <summary>
    /// Deletes a variant
    /// </summary>
    /// <param name="id">Base product ID</param>
    /// <param name="variantId">Variant ID</param>
    /// <returns>Success response</returns>
    [HttpDelete("{id}/variants/{variantId}")]
    [Authorize(Policy = "RequirePermission:products:write")]
    public async Task<IActionResult> DeleteVariant(int id, int variantId)
    {
        try
        {
            var success = await _productService.DeleteVariantAsync(variantId);
            if (!success)
            {
                return ErrorResponse("Variant not found", 404);
            }

            return SuccessResponse(new { message = "Variant deleted successfully" });
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(DeleteVariant));
        }
    }

    /// <summary>
    /// Gets products with their variants (for product listings)
    /// </summary>
    [HttpGet("with-variants")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductsWithVariants(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        [FromQuery] string? brand = null,
        [FromQuery] string? category = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string? sortBy = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 20;

            var result = await _productService.GetProductsWithVariantsAsync(
                page, pageSize, search, type, brand, category, minPrice, maxPrice, true, sortBy);

            var productDtos = result.Items.Select(product => product switch
            {
                Laptop laptop => MapToLaptopDto(laptop),
                Accessory accessory => MapToAccessoryDto(accessory),
                Bundle bundle => MapToBundleDto(bundle),
                _ => MapToProductDto(product)
            }).ToList();

            return PaginatedResponse(productDtos, result.TotalCount, page, pageSize);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetProductsWithVariants));
        }
    }

    #endregion
}
