using MediatR;
using EcommerceLaptop.API.Features.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;
using EcommerceLaptop.API.DTOs;
using System.Text.Json;
using AutoMapper;
using System.Security.Claims;

namespace EcommerceLaptop.API.Controllers;

public class ProductsController(
    ILogger<ProductsController> logger,
    IMemoryCache cache,
    IMapper mapper,
    ISender sender)
    : BaseApiController(logger)
{
    private readonly IMemoryCache _cache = cache;
    private readonly IMapper _mapper = mapper;
    private readonly ISender _sender = sender;

    /// <summary>
    /// Gets paginated list of products with filtering
    /// </summary> 
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
        // Generate cache key
        var cacheKey = $"products_{page}_{pageSize}_{search ?? ""}_{type ?? ""}_{brand ?? ""}_{category ?? ""}_{minPrice?.ToString("F2") ?? "0"}_{maxPrice?.ToString("F2") ?? "0"}_{sortBy ?? ""}";

        return (await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SetSlidingExpiration(TimeSpan.FromMinutes(5));
            entry.SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

            var query = new GetProductsQuery(page, pageSize, search, type, brand, category, minPrice, maxPrice, sortBy);
            var result = await _sender.Send(query);
            
            return PaginatedResponse(result.Items, result.TotalCount, result.Page, result.PageSize);
        }))!;
    }

    /// <summary>
    /// Gets specific product by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProduct(int id)
    {
        var query = new GetProductByIdQuery(id) 
        { 
            AuthenticatedUserId = GetCurrentUserId() 
        };
        var productDto = await _sender.Send(query);
        return SuccessResponse(productDto);
    }

    /// <summary>
    /// Creates a new product (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequirePermission:products:write")]
    public async Task<IActionResult> CreateProduct([FromBody] JsonElement request)
    {
        var userId = GetCurrentUserId();
        var command = new CreateProductCommand(request) { AuthenticatedUserId = userId };
        var productDto = await _sender.Send(command);

        return CreatedAtAction(nameof(GetProduct), new { id = productDto.Id },
            SuccessResponse(productDto, "Product created successfully"));
    }

    #region Enhanced Product Catalog Operations (Refactored)

    /// <summary>
    /// Gets laptops with advanced hardware filtering
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
        var query = new GetAdvancedLaptopsQuery(
            page, pageSize, search, brand, minPrice, maxPrice,
            cpuBrand, cpuGeneration, minCpuCores, minRamGB, maxRamGB,
            ramType, storageType, minStorageGB, gpuType, gpuBrand,
            minDisplaySize, maxDisplaySize, displayResolution,
            minRefreshRate, touchscreen, targetAudience);

        var result = await _sender.Send(query);

         return SuccessResponse(new
        {
            Items = result.Items,
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
        var query = new GetCompatibleAccessoriesQuery(page, pageSize, search, accessoryType, compatibility, productId);
        var result = await _sender.Send(query);

         return SuccessResponse(new
        {
            Items = result.Items,
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
    public async Task<IActionResult> CreateBundle([FromBody] CreateBundleRequest request)
    {
        var command = new CreateBundleCommand(request);
        var bundleDto = await _sender.Send(command);

        return SuccessResponse(bundleDto, "Bundle created successfully");
    }

    /// <summary>
    /// Calculates bundle pricing for given products
    /// </summary>
    [HttpPost("bundles/calculate-price")]
    [Authorize(Policy = "RequirePermission:products:read")]
    public async Task<IActionResult> CalculateBundlePrice([FromBody] CalculateBundlePriceRequest request)
    {
        var query = new CalculateBundlePriceQuery(request);
        var result = await _sender.Send(query);

        return SuccessResponse(result);
    }

    /// <summary>
    /// Gets product specifications as structured data
    /// </summary>
    [HttpGet("{id}/specifications")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProductSpecifications(int id)
    {
        var query = new GetProductSpecificationsQuery(id);
        var specifications = await _sender.Send(query);

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
        var query = new ValidateCompatibilityQuery(productId, accessoryId);
        var result = await _sender.Send(query);

        return SuccessResponse(result);
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

        var query = new GetRecommendationsQuery(userId.Value, count);
        var recommendationDtos = await _sender.Send(query);

        return SuccessResponse(recommendationDtos);
    }

    /// <summary>
    /// Bulk updates product pricing
    /// </summary>
    [HttpPut("bulk/pricing")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    public async Task<IActionResult> BulkUpdatePricing([FromBody] BulkPricingUpdateRequest request)
    {
        var command = new BulkUpdatePricingCommand(request);
        var success = await _sender.Send(command);

        if (success)
            return SuccessResponse<object?>(null, "Pricing updated successfully");
        else
            return ErrorResponse("Failed to update pricing");
    }

    #endregion

    #region Variant Image Upload Operations (Refactored)

    /// <summary>
    /// Uploads variant images to ImgBB cloud hosting
    /// </summary>
    [HttpPost("{id}/variants/{variantId}/images")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadVariantImages(int id, int variantId, IFormFileCollection files)
    {
        var command = new UploadVariantImagesCommand(id, variantId, files);
        var result = await _sender.Send(command);

        if (result.Success)
        {
            return Ok(new 
            {
               success = true,
               message = result.Message,
               data = result.Data
            });
        }
        else
        {
            return StatusCode(result.StatusCode, new { success = false, message = result.Message });
        }
    }
    #endregion


}
