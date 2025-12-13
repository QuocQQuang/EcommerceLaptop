using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.API.Features.Brands;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Controller for managing product brands
/// </summary>
[Route("api/admin/[controller]")]
[ApiController]
public class BrandsController(ISender sender, ILogger<BrandsController> logger) : BaseApiController(logger)
{
    private readonly ISender _sender = sender;

    /// <summary>
    /// Get all brands with product counts (Admin only)
    /// </summary>
    [HttpGet("with-counts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrandsWithCounts()
    {
        var result = await _sender.Send(new GetBrandsWithProductCountQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get all brands
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetBrands()
    {
        var result = await _sender.Send(new GetAllBrandsQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get brands for select dropdown
    /// </summary>
    [HttpGet("for-select")]
    public async Task<IActionResult> GetBrandsForSelect()
    {
        var result = await _sender.Send(new GetBrandsForSelectQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get brand by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBrand(int id)
    {
        var result = await _sender.Send(new GetBrandByIdQuery(id));
        if (result == null)
            return NotFound(new { error = "Brand not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get brand by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBrandBySlug(string slug)
    {
        var result = await _sender.Send(new GetBrandBySlugQuery(slug));
        if (result == null)
            return NotFound(new { error = "Brand not found" });

        return Ok(result);
    }

    /// <summary>
    /// Create a new brand (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequirePermission:products:write")]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrandRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var brand = new ProductBrand
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            Website = request.WebsiteUrl,
            IsActive = request.IsActive
        };

        var result = await _sender.Send(new CreateBrandCommand(brand));
        return CreatedAtAction(nameof(GetBrand), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing brand (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequirePermission:products:write")]
    public async Task<IActionResult> UpdateBrand(int id, [FromBody] CreateBrandRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var brand = new ProductBrand
        {
            Id = id,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            LogoUrl = request.LogoUrl,
            Website = request.WebsiteUrl,
            IsActive = request.IsActive
        };

        var result = await _sender.Send(new UpdateBrandCommand(brand));
        return Ok(result);
    }

    /// <summary>
    /// Delete a brand (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = "RequirePermission:products:delete")]
    public async Task<IActionResult> DeleteBrand(int id)
    {
        await _sender.Send(new DeleteBrandCommand(id));
        return Ok(new { message = "Brand deleted successfully" });
    }

    /// <summary>
    /// Check if brand slug is unique (Admin only)
    /// </summary>
    [HttpGet("check-slug/{slug}")]
    [Authorize(Policy = "RequirePermission:products:read")]
    public async Task<IActionResult> CheckSlugUnique(string slug, [FromQuery] int? excludeId = null)
    {
        var isUnique = await _sender.Send(new CheckBrandSlugUniqueQuery(slug, excludeId));
        return Ok(new { isUnique });
    }

    /// <summary>
    /// Reassign products from one brand to another, then delete the original brand (Admin only)
    /// </summary>
    [HttpPost("{brandIdToDelete}/reassign-and-delete")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    public async Task<IActionResult> ReassignAndDeleteBrand(int brandIdToDelete, [FromBody] ReassignBrandRequest request)
    {
        await _sender.Send(new ReassignProductsAndDeleteBrandCommand(brandIdToDelete, request.NewBrandId));
        return Ok(new { message = "Products reassigned and brand deleted successfully" });
    }

    /// <summary>
    /// Force delete a brand (deactivate all associated products) (Admin only)
    /// </summary>
    [HttpDelete("{id}/force")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    public async Task<IActionResult> ForceDeleteBrand(int id)
    {
        await _sender.Send(new ForceDeleteBrandCommand(id));
        return Ok(new { message = "Brand force deleted and associated products deactivated successfully" });
    }
}

/// <summary>
/// DTO for creating/updating brands
/// </summary>
public class CreateBrandRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? LogoUrl { get; set; }
    public string? WebsiteUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}

/// <summary>
/// DTO for reassigning products to another brand
/// </summary>
public class ReassignBrandRequest
{
    public int NewBrandId { get; set; }
}