using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.API.DTOs;
using CoreInventory = EcommerceLaptop.Core.DTOs.Inventory;
using EcommerceLaptop.Infrastructure.Services.Security;
using System.Text.Json;
using AutoMapper;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Products controller handling all product-related operations
/// Implements RESTful API design with comprehensive CRUD operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductsController(
    IProductService productService,
    IImageHostingService imageHostingService,
    IAuditLoggingService auditLoggingService,
    ILogger<ProductsController> logger,
    IMemoryCache cache,
    IInventoryService inventoryService,
    IMapper mapper)
    : BaseApiController(logger)
{
    private readonly IProductService _productService = productService;
    private readonly IMemoryCache _cache = cache;
    private readonly IImageHostingService _imageHostingService = imageHostingService;
    private readonly IAuditLoggingService _auditLoggingService = auditLoggingService;
    private readonly IInventoryService _inventoryService = inventoryService;
    private readonly IMapper _mapper = mapper;

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
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        // Generate cache key based on parameters to avoid duplicate queries
        var cacheKey = $"products_{page}_{pageSize}_{search ?? ""}_{type ?? ""}_{brand ?? ""}_{category ?? ""}_{minPrice?.ToString("F2") ?? "0"}_{maxPrice?.ToString("F2") ?? "0"}_{sortBy ?? ""}";

        IActionResult? resultAction;

        if (!_cache.TryGetValue(cacheKey, out resultAction) || resultAction == null)
        {
            var result = await _productService.GetProductsAsync(
                page, pageSize, search, type, brand, category, minPrice, maxPrice, true, sortBy);

            var productDtos = _mapper.Map<List<ProductDto>>(result.Items);
            resultAction = PaginatedResponse(productDtos, result.TotalCount, page, pageSize);

            // Cache for 5 minutes
            var cacheOptions = new MemoryCacheEntryOptions()
                .SetSlidingExpiration(TimeSpan.FromMinutes(5))
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

            _cache.Set(cacheKey, resultAction, cacheOptions);
        }

        return resultAction;
    }

    /// <summary>
    /// Gets products for admin management
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
        var result = await _productService.GetAdminProductsAsync(
            page, pageSize, search, type, brand, status, productType);

        var productDtos = _mapper.Map<List<ProductDto>>(result.Items);

        return PaginatedResponse(productDtos, result.TotalCount, page, pageSize);
    }

    /// <summary>
    /// Gets specific product by ID for admin management, including inactive products.
    /// </summary>
    [HttpGet("admin/{id:int}")]
    [Authorize(Policy = "RequirePermission:products:read")]
    public async Task<IActionResult> GetAdminProduct(int id)
    {
        var product = await _productService.GetByIdWithDetailsAsync(id);
        if (product is null)
            return ErrorResponse("Product not found", 404);

        if (product.IsBaseProduct)
        {
            var variants = await _productService.GetVariantsAsync(id);
            product.Variants = variants.ToList();
        }

        var productDto = _mapper.Map<ProductDto>(product);
        return SuccessResponse(productDto);
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
        var product = await _productService.GetByIdAsync(id);
        if (product is null)
            // ErrorResponse usually returns IActionResult directly
            throw new KeyNotFoundException("Product not found"); 
            // Or keep return ErrorResponse("Product not found", 404); if I want to keep consistency.
            // But GlobalExceptionHandler handles exceptions. 
            // Let's stick to using standard "return NotFound" or throw Exception.
            // Existing code used "ErrorResponse" which is likely a BaseApiController helper.
            // I will keep ErrorResponse for logic errors, but unexpected errors go to middleware.
            // Actually, "try-catch" removal means *unexpected* errors bubble up.
            // Logic errors like "Not Found" can be handled by logic.
        if (product is null)
             return ErrorResponse("Product not found", 404);
        // Allow admin users to view inactive products for editing
        var isAdmin = User.Identity?.IsAuthenticated == true &&
                       User.HasClaim(c => c.Type == "permission" && c.Value == "products:read");
        if (!product.IsActive && !isAdmin)
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
            var ipAddress = GetClientIpAddress();
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
        // AutoMapper handles polymorphism automatically
        var productDto = _mapper.Map<ProductDto>(product);

        return SuccessResponse(productDto);
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

        var productDto = _mapper.Map<ProductDto>(product);

        return SuccessResponse(productDto);
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
        var result = await _productService.GetLaptopsAsync(
            page, pageSize, search, brand, minPrice, maxPrice,
            cpuBrand, ramCapacity, storageType);

        var laptopDtos = _mapper.Map<List<LaptopDto>>(result.Items);

        return PaginatedResponse(laptopDtos, result.TotalCount, page, pageSize);
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
        var result = await _productService.GetAccessoriesAsync(
            page, pageSize, search, accessoryType, compatibility);

        var accessoryDtos = _mapper.Map<List<AccessoryDto>>(result.Items);

        return PaginatedResponse(accessoryDtos, result.TotalCount, page, pageSize);
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
        var result = await _productService.GetBundlesAsync(
            page, pageSize, search, bundleType);

        var bundleDtos = _mapper.Map<List<BundleDto>>(result.Items);

        return PaginatedResponse(bundleDtos, result.TotalCount, page, pageSize);
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
        if (count < 1 || count > 50) count = 10;

        var products = await _productService.GetFeaturedProductsAsync(count);
        var productDtos = _mapper.Map<List<ProductDto>>(products);

        return SuccessResponse(productDtos);
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
        if (count < 1 || count > 20) count = 5;

        var products = await _productService.GetRelatedProductsAsync(id, count);
        var productDtos = _mapper.Map<List<ProductDto>>(products);

        return SuccessResponse(productDtos);
    }

    /// <summary>
    /// Gets all brands
    /// </summary>
    /// <returns>List of brands</returns>
    [HttpGet("brands")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrands()
    {
        var brands = await _productService.GetBrandsAsync();
        return SuccessResponse(brands);
    }

    /// <summary>
    /// Creates a new product (Admin only)
    /// </summary>
    /// <param name="request">Product creation request</param>
    /// <returns>Created product</returns>
    [HttpPost]
    [Authorize(Policy = "RequirePermission:products:write")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CreateProduct([FromBody] JsonElement request)
    {
        // Deserialize to base request first to get ProductType
        var baseRequest = JsonSerializer.Deserialize<CreateProductRequest>(request.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (baseRequest == null || string.IsNullOrEmpty(baseRequest.ProductType))
        {
            return ErrorResponse("ProductType is required.", 400);
        }

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

        var stockQuantity = ExtractStockQuantity(request);
        if (stockQuantity.HasValue)
        {
            await SyncInventoryAsync(
                createdProduct.Id,
                stockQuantity.Value,
                createdProduct.Price,
                "PRODUCT_CREATE");
        }

        createdProduct = await _productService.GetByIdWithDetailsAsync(createdProduct.Id) ?? createdProduct;
        var productDto = _mapper.Map<ProductDto>(createdProduct);

        // Log admin product creation activity
        var adminUserId = GetCurrentUserId();
        if (adminUserId.HasValue)
        {
            var ipAddress = GetClientIpAddress();
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

        return CreatedAtAction(nameof(GetAdminProduct), new { id = createdProduct.Id },
            new ApiResponse<ProductDto>
            {
                Success = true,
                Data = productDto,
                Message = "Product created successfully"
            });
    }

    /// <summary>
    /// Updates existing product (Admin only)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <param name="request">Product update request</param>
    /// <returns>Updated product</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequirePermission:products:write")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] JsonElement request)
    {
        var existingProduct = await _productService.GetByIdWithDetailsAsync(id);
        if (existingProduct == null)
            return ErrorResponse("Product not found", 404);

        // Apply updates using the new flexible mapping
        ApplyProductUpdates(existingProduct, request);

        var updatedProduct = await _productService.UpdateProductAsync(existingProduct);

        var stockQuantity = ExtractStockQuantity(request);
        if (stockQuantity.HasValue)
            await SyncInventoryAsync(id, stockQuantity.Value, existingProduct.Price, "PRODUCT_UPDATE");

        var productDto = _mapper.Map<ProductDto>(updatedProduct);

        // Log admin product update activity
        var adminUserId = GetCurrentUserId();
        if (adminUserId.HasValue)
        {
            var ipAddress = GetClientIpAddress();
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

    /// <summary>
    /// Deletes product (Admin only)
    /// </summary>
    /// <param name="id">Product ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequirePermission:products:delete")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeleteProduct(int id)
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
            var ipAddress = GetClientIpAddress();
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

    #region Variant CRUD Operations

    /// <summary>
    /// Gets all variants for a product (Admin only)
    /// </summary>
    [HttpGet("{id}/variants")]
    [Authorize(Policy = "RequirePermission:products:read")]
    public async Task<IActionResult> GetVariants(int id)
    {
        var variants = await _productService.GetVariantsAsync(id);
        var variantDtos = _mapper.Map<List<ProductDto>>(variants);
        return SuccessResponse(variantDtos);
    }

    /// <summary>
    /// Creates a variant for a base product (Admin only)
    /// </summary>
    [HttpPost("{id}/variants")]
    [Authorize(Policy = "RequirePermission:products:write")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CreateVariant(int id, [FromBody] CreateVariantDto request)
    {
        var baseProduct = await _productService.GetByIdWithDetailsAsync(id);
        if (baseProduct == null || baseProduct.ParentProductId != null)
            return ErrorResponse("Base product not found", 404);

        var variant = CreateVariantFromBaseProduct(baseProduct, request);

        try
        {
            var createdVariant = await _productService.CreateVariantAsync(id, variant);
            await SyncInventoryAsync(createdVariant.Id, request.StockQuantity, createdVariant.Price, "VARIANT_CREATE");

            createdVariant = await _productService.GetByIdWithDetailsAsync(createdVariant.Id) ?? createdVariant;
            var variantDto = _mapper.Map<ProductDto>(createdVariant);
            return SuccessResponse(variantDto, "Variant created successfully");
        }
        catch (ArgumentException ex)
        {
            return ErrorResponse(ex.Message, 400);
        }
    }

    /// <summary>
    /// Updates a variant (Admin only)
    /// </summary>
    [HttpPut("{id}/variants/{variantId}")]
    [Authorize(Policy = "RequirePermission:products:write")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UpdateVariant(int id, int variantId, [FromBody] UpdateVariantDto request)
    {
        var variant = await _productService.GetByIdAsync(variantId);
        if (variant == null || variant.ParentProductId != id)
            return ErrorResponse("Variant not found", 404);

        // Update variant properties directly on the loaded entity
        if (request.VariantName != null) variant.VariantName = request.VariantName;
        if (request.VariantSku != null) variant.VariantSku = request.VariantSku;
        if (request.Price.HasValue) variant.Price = request.Price.Value;
        if (request.Description != null) variant.Description = request.Description;
        if (request.IsActive.HasValue) variant.IsActive = request.IsActive.Value;
        ApplyVariantSpecificUpdates(variant, request);
        variant.UpdatedAt = DateTime.UtcNow;

        var result = await _productService.UpdateProductAsync(variant);
        if (result == null)
            return ErrorResponse("Variant update failed", 500);

        // Sync inventory if StockQuantity provided
        if (request.StockQuantity.HasValue)
        {
            var inventory = await _inventoryService.GetInventoryByProductIdAsync(variantId);
            if (inventory == null)
            {
                await _inventoryService.CreateInventoryAsync(new CoreInventory.CreateInventoryRequest
                {
                    ProductId = variantId,
                    QuantityInStock = request.StockQuantity.Value,
                    ReorderLevel = 10,
                    MaxStockLevel = 1000,
                    WarehouseLocation = "Main Warehouse",
                    UnitCost = variant.Price * 0.8m
                });
            }
            else
            {
                var diff = request.StockQuantity.Value - inventory.QuantityInStock;
                if (diff != 0)
                {
                    await _inventoryService.AdjustStockAsync(new CoreInventory.StockAdjustmentRequest
                    {
                        ProductId = variantId,
                        Quantity = diff,
                        Reference = "VARIANT_UPDATE",
                        Notes = $"Variant inventory update: {inventory.QuantityInStock} -> {request.StockQuantity.Value}",
                        WarehouseLocation = inventory.WarehouseLocation
                    });
                }
            }
        }

        var variantDto = _mapper.Map<ProductDto>(result);
        return SuccessResponse(variantDto, "Variant updated successfully");
    }

    /// <summary>
    /// Deletes a variant (Admin only)
    /// </summary>
    [HttpDelete("{id}/variants/{variantId}")]
    [Authorize(Policy = "RequirePermission:products:delete")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeleteVariant(int id, int variantId)
    {
        var success = await _productService.DeleteVariantAsync(variantId);
        if (!success)
            return ErrorResponse("Variant not found", 404);

        return SuccessResponse(new { deleted = true }, "Variant deleted successfully");
    }

    #endregion

    #region Product Image Upload Operations

    /// <summary>
    /// Uploads product images to ImgBB cloud hosting (Admin only)
    /// </summary>
    [HttpPost("{id}/images")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    [Consumes("multipart/form-data")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadProductImages(int id, IFormFileCollection files)
    {
        if (files == null || files.Count == 0)
            return ErrorResponse("No files provided", 400);

        var product = await _productService.GetByIdAsync(id);
        if (product == null)
            return ErrorResponse("Product not found", 404);

        var uploadResults = new List<ImageUploadResult>();
        var productImages = new List<ProductImage>();

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            if (!_imageHostingService.IsValidImage(file.FileName, file.ContentType, file.Length))
                return ErrorResponse($"Invalid image file: {file.FileName}", 400);

            try
            {
                using var stream = file.OpenReadStream();
                var uploadResult = await _imageHostingService.UploadImageAsync(stream, file.FileName, ImageCategory.Products);
                uploadResults.Add(uploadResult);

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
                logger.LogError(ex, "Failed to upload image {FileName} for product {ProductId}", file.FileName, id);
                foreach (var uploadedResult in uploadResults)
                {
                    try { await _imageHostingService.DeleteImageAsync(uploadedResult.ImageId); }
                    catch (Exception cleanupEx) { logger.LogError(cleanupEx, "Failed to cleanup uploaded image {ImageId}", uploadedResult.ImageId); }
                }
                return ErrorResponse($"Failed to upload image: {file.FileName}", 500);
            }
        }

        await _productService.UpdateProductImagesAsync(id, productImages);

        return Ok(new
        {
            success = true,
            message = $"Successfully uploaded {productImages.Count} images",
            data = new
            {
                productId = id,
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

    /// <summary>
    /// Deletes a product image (Admin only)
    /// </summary>
    [HttpDelete("{id}/images/{imageId}")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeleteProductImage(int id, string imageId)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product == null)
            return ErrorResponse("Product not found", 404);

        var image = product.Images.FirstOrDefault(i => i.ImageId == imageId);
        if (image == null)
            return ErrorResponse("Image not found", 404);

        try
        {
            await _imageHostingService.DeleteImageAsync(image.ImageId!);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete image from hosting service: {ImageId}", imageId);
        }

        await _productService.DeleteProductImageAsync(image.Id);

        return SuccessResponse(new { deleted = true }, "Image deleted successfully");
    }

    #endregion

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

    private Product CreateVariantFromBaseProduct(Product baseProduct, CreateVariantDto request)
    {
        Product variant = baseProduct switch
        {
            Laptop laptop => new Laptop
            {
                Series = request.Series ?? laptop.Series,
                CpuBrand = request.CpuBrand ?? laptop.CpuBrand,
                CpuModel = request.CpuModel ?? laptop.CpuModel,
                CpuGeneration = request.CpuGeneration ?? laptop.CpuGeneration,
                CpuCores = request.CpuCores ?? laptop.CpuCores,
                CpuBaseClockGHz = request.CpuBaseClockGHz ?? laptop.CpuBaseClockGHz,
                CpuBoostClockGHz = request.CpuBoostClockGHz ?? laptop.CpuBoostClockGHz,
                CpuCache = request.CpuCache ?? laptop.CpuCache,
                RamType = request.RamType ?? laptop.RamType,
                RamCapacityGB = request.RamCapacityGB ?? laptop.RamCapacityGB,
                RamSlots = request.RamSlots ?? laptop.RamSlots,
                RamSpeed = request.RamSpeed ?? laptop.RamSpeed,
                RamUpgradeable = request.RamUpgradeable ?? laptop.RamUpgradeable,
                StorageType = request.StorageType ?? laptop.StorageType,
                StorageCapacityGB = request.StorageCapacityGB ?? laptop.StorageCapacityGB,
                StorageInterface = request.StorageInterface ?? laptop.StorageInterface,
                NvMeSupport = request.NvMeSupport ?? laptop.NvMeSupport,
                GpuType = request.GpuType ?? laptop.GpuType,
                GpuBrand = request.GpuBrand ?? laptop.GpuBrand,
                GpuModel = request.GpuModel ?? laptop.GpuModel,
                GpuVramGB = request.GpuVramGB ?? laptop.GpuVramGB,
                DisplaySizeInches = request.DisplaySizeInches ?? laptop.DisplaySizeInches,
                DisplayResolution = request.DisplayResolution ?? laptop.DisplayResolution,
                DisplayPanelType = request.DisplayPanelType ?? laptop.DisplayPanelType,
                DisplayRefreshRateHz = request.DisplayRefreshRateHz ?? laptop.DisplayRefreshRateHz,
                DisplayTouchscreen = request.DisplayTouchscreen ?? laptop.DisplayTouchscreen,
                BatteryCapacityWh = request.BatteryCapacityWh ?? laptop.BatteryCapacityWh,
                WeightKg = request.WeightKg ?? laptop.WeightKg,
                Dimensions = request.Dimensions ?? laptop.Dimensions,
                Color = request.Color ?? laptop.Color,
                Ports = request.Ports ?? laptop.Ports,
                WiFi6Support = request.WiFi6Support ?? laptop.WiFi6Support,
                BluetoothSupport = request.BluetoothSupport ?? laptop.BluetoothSupport,
                BluetoothVersion = request.BluetoothVersion ?? laptop.BluetoothVersion,
                WarrantyPeriod = request.WarrantyPeriod ?? laptop.WarrantyPeriod,
                TargetAudience = request.TargetAudience ?? laptop.TargetAudience
            },
            Accessory accessory => new Accessory
            {
                AccessoryType = accessory.AccessoryType,
                Compatibility = accessory.Compatibility,
                Specifications = accessory.Specifications,
                Color = request.Color ?? accessory.Color,
                Connectivity = accessory.Connectivity
            },
            Bundle bundle => new Bundle
            {
                BundleType = bundle.BundleType,
                DiscountPercentage = bundle.DiscountPercentage,
                ValidFrom = bundle.ValidFrom,
                ValidTo = bundle.ValidTo
            },
            _ => throw new ArgumentException("Unsupported product type")
        };

        variant.Name = $"{baseProduct.Name} - {request.VariantName}".Trim();
        variant.Description = request.Description ?? baseProduct.Description;
        variant.Brand = baseProduct.Brand;
        variant.Model = baseProduct.Model;
        variant.Price = request.Price;
        variant.SKU = request.VariantSku;
        variant.VariantName = request.VariantName;
        variant.VariantSku = request.VariantSku;
        variant.IsActive = request.IsActive;
        variant.CategoryId = baseProduct.CategoryId;
        variant.BrandId = baseProduct.BrandId;

        return variant;
    }

    private void ApplyVariantSpecificUpdates(Product variant, UpdateVariantDto request)
    {
        if (variant is not Laptop laptop)
            return;

        laptop.Series = request.Series ?? laptop.Series;
        laptop.CpuBrand = request.CpuBrand ?? laptop.CpuBrand;
        laptop.CpuModel = request.CpuModel ?? laptop.CpuModel;
        laptop.CpuGeneration = request.CpuGeneration ?? laptop.CpuGeneration;
        laptop.CpuCores = request.CpuCores ?? laptop.CpuCores;
        laptop.CpuBaseClockGHz = request.CpuBaseClockGHz ?? laptop.CpuBaseClockGHz;
        laptop.CpuBoostClockGHz = request.CpuBoostClockGHz ?? laptop.CpuBoostClockGHz;
        laptop.CpuCache = request.CpuCache ?? laptop.CpuCache;
        laptop.RamType = request.RamType ?? laptop.RamType;
        laptop.RamCapacityGB = request.RamCapacityGB ?? laptop.RamCapacityGB;
        laptop.RamSlots = request.RamSlots ?? laptop.RamSlots;
        laptop.RamSpeed = request.RamSpeed ?? laptop.RamSpeed;
        laptop.RamUpgradeable = request.RamUpgradeable ?? laptop.RamUpgradeable;
        laptop.StorageType = request.StorageType ?? laptop.StorageType;
        laptop.StorageCapacityGB = request.StorageCapacityGB ?? laptop.StorageCapacityGB;
        laptop.StorageInterface = request.StorageInterface ?? laptop.StorageInterface;
        laptop.NvMeSupport = request.NvMeSupport ?? laptop.NvMeSupport;
        laptop.GpuType = request.GpuType ?? laptop.GpuType;
        laptop.GpuBrand = request.GpuBrand ?? laptop.GpuBrand;
        laptop.GpuModel = request.GpuModel ?? laptop.GpuModel;
        laptop.GpuVramGB = request.GpuVramGB ?? laptop.GpuVramGB;
        laptop.DisplaySizeInches = request.DisplaySizeInches ?? laptop.DisplaySizeInches;
        laptop.DisplayResolution = request.DisplayResolution ?? laptop.DisplayResolution;
        laptop.DisplayPanelType = request.DisplayPanelType ?? laptop.DisplayPanelType;
        laptop.DisplayRefreshRateHz = request.DisplayRefreshRateHz ?? laptop.DisplayRefreshRateHz;
        laptop.DisplayTouchscreen = request.DisplayTouchscreen ?? laptop.DisplayTouchscreen;
        laptop.BatteryCapacityWh = request.BatteryCapacityWh ?? laptop.BatteryCapacityWh;
        laptop.WeightKg = request.WeightKg ?? laptop.WeightKg;
        laptop.Dimensions = request.Dimensions ?? laptop.Dimensions;
        laptop.Color = request.Color ?? laptop.Color;
        laptop.Ports = request.Ports ?? laptop.Ports;
        laptop.WiFi6Support = request.WiFi6Support ?? laptop.WiFi6Support;
        laptop.BluetoothSupport = request.BluetoothSupport ?? laptop.BluetoothSupport;
        laptop.BluetoothVersion = request.BluetoothVersion ?? laptop.BluetoothVersion;
        laptop.WarrantyPeriod = request.WarrantyPeriod ?? laptop.WarrantyPeriod;
        laptop.TargetAudience = request.TargetAudience ?? laptop.TargetAudience;
    }

    private int? ExtractStockQuantity(JsonElement request)
    {
        if (request.ValueKind != JsonValueKind.Object)
            return null;

        foreach (var property in request.EnumerateObject())
        {
            if (!string.Equals(property.Name, "StockQuantity", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(property.Name, "Stock", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (property.Value.TryGetInt32(out var stockQuantity))
                return stockQuantity;

            if (property.Value.ValueKind == JsonValueKind.String &&
                int.TryParse(property.Value.GetString(), out stockQuantity))
            {
                return stockQuantity;
            }
        }

        return null;
    }

    private async Task SyncInventoryAsync(int productId, int stockQuantity, decimal productPrice, string reference)
    {
        var inventory = await _inventoryService.GetInventoryByProductIdAsync(productId);
        if (inventory == null)
        {
            await _inventoryService.CreateInventoryAsync(new CoreInventory.CreateInventoryRequest
            {
                ProductId = productId,
                QuantityInStock = stockQuantity,
                ReorderLevel = 10,
                MaxStockLevel = 1000,
                WarehouseLocation = "Main Warehouse",
                UnitCost = productPrice * 0.8m
            });
            return;
        }

        var quantityDifference = stockQuantity - inventory.QuantityInStock;
        if (quantityDifference == 0)
            return;

        await _inventoryService.AdjustStockAsync(new CoreInventory.StockAdjustmentRequest
        {
            ProductId = productId,
            Quantity = quantityDifference,
            Reference = reference,
            Notes = $"Product inventory update: {inventory.QuantityInStock} -> {stockQuantity}",
            WarehouseLocation = inventory.WarehouseLocation
        });
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


        // Update SKU, CategoryId, BrandId (these were missing from updates)
        if (updateData.TryGetValue(nameof(UpdateProductRequest.SKU), out var sku) && sku.ValueKind == JsonValueKind.String)
            product.SKU = sku.GetString()!;
        if (updateData.TryGetValue(nameof(UpdateProductRequest.CategoryId), out var catId) && catId.TryGetInt32(out var catIdVal))
            product.CategoryId = catIdVal;
        if (updateData.TryGetValue(nameof(UpdateProductRequest.BrandId), out var brandId) && brandId.TryGetInt32(out var brandIdVal))
            product.BrandId = brandIdVal;

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
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _productService.GetLaptopsWithAdvancedFilteringAsync(
            page, pageSize, search, brand, minPrice, maxPrice,
            cpuBrand, cpuGeneration, minCpuCores, minRamGB, maxRamGB,
            ramType, storageType, minStorageGB, gpuType, gpuBrand,
            minDisplaySize, maxDisplaySize, displayResolution,
            minRefreshRate, touchscreen, targetAudience);

        var laptopDtos = _mapper.Map<List<LaptopDto>>(result.Items);

        return SuccessResponse(new
        {
            Items = laptopDtos,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = (int)Math.Ceiling((double)result.TotalCount / result.PageSize)
        });
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
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 20;

        var result = await _productService.GetAccessoriesWithCompatibilityAsync(
            page, pageSize, search, accessoryType, compatibility, productId);

        var accessoryDtos = _mapper.Map<List<AccessoryDto>>(result.Items);

        return SuccessResponse(new
        {
            Items = accessoryDtos,
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            TotalPages = (int)Math.Ceiling((double)result.TotalCount / result.PageSize)
        });
    }

    /// <summary>
    /// Creates a product bundle with dynamic pricing
    /// </summary>
    [HttpPost("bundles")]
    [Authorize(Policy = "RequirePermission:products:write")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CreateBundle([FromBody] CreateBundleRequest request)
    {
        var bundle = await _productService.CreateBundleAsync(
            request.Name,
            request.Description,
            request.ProductIds,
            request.DiscountPercentage,
            request.ValidFrom,
            request.ValidTo);

        return SuccessResponse(_mapper.Map<BundleDto>(bundle), "Bundle created successfully");
    }

    /// <summary>
    /// Calculates bundle pricing for given products
    /// </summary>
    [HttpPost("bundles/calculate-price")]
    [Authorize(Policy = "RequirePermission:products:read")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> CalculateBundlePrice([FromBody] CalculateBundlePriceRequest request)
    {
        var price = await _productService.CalculateBundlePriceAsync(request.ProductIds, request.DiscountPercentage);

        return SuccessResponse(new { CalculatedPrice = price });
    }

    /// <summary>
    /// Gets product specifications as structured data
    /// </summary>
    [HttpGet("{id}/specifications")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductSpecifications(int id)
    {
        var specifications = await _productService.GetProductSpecificationsAsync(id);

        if (!specifications.Any())
            return NotFound("Product not found");

        return SuccessResponse(specifications);
    }

    /// <summary>
    /// Validates compatibility between a product and accessory
    /// </summary>
    [HttpGet("{productId}/compatibility/{accessoryId}")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateCompatibility(int productId, int accessoryId)
    {
        var isCompatible = await _productService.ValidateProductCompatibilityAsync(productId, accessoryId);

        return SuccessResponse(new { IsCompatible = isCompatible });
    }

    /// <summary>
    /// Gets product recommendations for a user
    /// </summary>
    [HttpGet("recommendations")]
    [Authorize]
    public async Task<IActionResult> GetRecommendations([FromQuery] int count = 5)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized();

        var recommendations = await _productService.GetRecommendedProductsAsync(userId.Value, count);
        var recommendationDtos = _mapper.Map<IEnumerable<ProductDto>>(recommendations);

        return SuccessResponse(recommendationDtos);
    }

    /// <summary>
    /// Bulk updates product pricing
    /// </summary>
    [HttpPut("bulk/pricing")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> BulkUpdatePricing([FromBody] BulkPricingUpdateRequest request)
    {
        var success = await _productService.BulkUpdatePricingAsync(request.ProductIds, request.PriceAdjustmentPercentage);

        if (success)
            return SuccessResponse<object?>(null, "Pricing updated successfully");
        else
            return ErrorResponse("Failed to update pricing");
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
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> UploadVariantImages(int id, int variantId, IFormFileCollection files)
    {
        // Debug logging
        logger.LogInformation("UploadVariantImages called with {FileCount} files for variant {VariantId} of product {ProductId}",
            files?.Count ?? 0, variantId, id);

        if (files != null)
        {
            for (int i = 0; i < files.Count; i++)
            {
                var file = files[i];
                logger.LogInformation("File {Index}: {FileName}, Size: {Size} bytes",
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

        logger.LogInformation("Starting upload of {FileCount} files for variant {VariantId}",
            files.Count, variantId);

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            logger.LogInformation("Uploading file {Index}/{Total}: {FileName} ({Size} bytes)",
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

                logger.LogInformation("Successfully uploaded file {Index}/{Total}: {FileName}",
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
                logger.LogError(ex, "Failed to upload image {FileName} for variant {VariantId}", file.FileName, variantId);

                // Cleanup already uploaded images on error
                foreach (var uploadedResult in uploadResults)
                {
                    try
                    {
                        await _imageHostingService.DeleteImageAsync(uploadedResult.ImageId);
                    }
                    catch (Exception cleanupEx)
                    {
                        logger.LogError(cleanupEx, "Failed to cleanup uploaded image {ImageId}", uploadedResult.ImageId);
                    }
                }

                return ErrorResponse($"Failed to upload image: {file.FileName}", 500);
            }
        }

        // Save all images to database
        // Save all images to database
        await _productService.UpdateProductImagesAsync(variantId, productImages);

        logger.LogInformation("Successfully uploaded and saved {ImageCount} images for variant {VariantId}",
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

    /// <summary>
    /// Deletes a variant image (Admin only)
    /// </summary>
    [HttpDelete("{id}/variants/{variantId}/images/{imageId}")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> DeleteVariantImage(int id, int variantId, string imageId)
    {
        var variant = await _productService.GetByIdAsync(variantId);
        if (variant == null || variant.ParentProductId != id)
            return ErrorResponse("Variant not found", 404);

        var image = variant.Images.FirstOrDefault(i => i.ImageId == imageId);
        if (image == null)
            return ErrorResponse("Image not found", 404);

        try
        {
            await _imageHostingService.DeleteImageAsync(image.ImageId!);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to delete variant image from hosting service: {ImageId}", imageId);
        }

        await _productService.DeleteProductImageAsync(image.Id);

        return SuccessResponse(new { deleted = true }, "Variant image deleted successfully");
    }
    #endregion
}
