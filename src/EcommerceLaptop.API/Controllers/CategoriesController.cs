using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.API.Features.Categories;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Controller for managing product categories
/// </summary>
[Route("api/admin/[controller]")]
[ApiController]
public class CategoriesController(ISender sender, ILogger<CategoriesController> logger) : BaseApiController(logger)
{
    private readonly ISender _sender = sender;

    /// <summary>
    /// Get all categories in hierarchical tree structure
    /// </summary>
    [HttpGet("tree")]
    public async Task<IActionResult> GetCategoryTree()
    {
        var result = await _sender.Send(new GetCategoryTreeQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get all categories as flat list
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllCategories()
    {
        var result = await _sender.Send(new GetAllCategoriesQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get categories with product counts (Admin only)
    /// </summary>
    [HttpGet("with-counts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCategoriesWithProductCount()
    {
        var result = await _sender.Send(new GetCategoriesWithProductCountQuery());
        return Ok(result);
    }

    /// <summary>
    /// Get category by ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCategoryById(int id)
    {
        var result = await _sender.Send(new GetCategoryByIdQuery(id));
        if (result == null)
            return NotFound(new { error = "Category not found" });

        return Ok(result);
    }

    /// <summary>
    /// Get category by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetCategoryBySlug(string slug)
    {
        var result = await _sender.Send(new GetCategoryBySlugQuery(slug));
        if (result == null)
            return NotFound(new { error = "Category not found" });

        return Ok(result);
    }

    /// <summary>
    /// Create a new category (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "RequirePermission:products:write")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
    {
        var category = new ProductCategory
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description ?? string.Empty,
            ImageUrl = request.ImageUrl,
            IsActive = request.IsActive,
            ParentId = request.ParentId,
            SortOrder = request.SortOrder
        };

        var result = await _sender.Send(new CreateCategoryCommand(category));
        return CreatedAtAction(nameof(GetCategoryById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Update an existing category (Admin only)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = "RequirePermission:products:write")]
    public async Task<IActionResult> UpdateCategory(int id, [FromBody] CreateCategoryRequest request)
    {
        var category = new ProductCategory
        {
            Id = id,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description ?? string.Empty,
            ImageUrl = request.ImageUrl,
            IsActive = request.IsActive,
            ParentId = request.ParentId,
            SortOrder = request.SortOrder
        };

        var result = await _sender.Send(new UpdateCategoryCommand(category));
        return Ok(result);
    }

    /// <summary>
    /// Delete a category (Admin only)
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = "RequirePermission:products:delete")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        await _sender.Send(new DeleteCategoryCommand(id));
        return Ok(new { message = "Category deleted successfully" });
    }

    /// <summary>
    /// Reorder categories (Admin only)
    /// </summary>
    [HttpPut("reorder")]
    [Authorize(Policy = "RequirePermission:products:write")]
    public async Task<IActionResult> ReorderCategories([FromBody] List<CategoryReorderRequest> reorderRequests)
    {
        await _sender.Send(new ReorderCategoriesCommand(reorderRequests));
        return Ok(new { message = "Categories reordered successfully" });
    }

    /// <summary>
    /// Reassign products from one category to another, then delete the original category (Admin only)
    /// </summary>
    [HttpPost("{categoryIdToDelete}/reassign-and-delete")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    public async Task<IActionResult> ReassignAndDeleteCategory(int categoryIdToDelete, [FromBody] ReassignCategoryRequest request)
    {
        await _sender.Send(new ReassignProductsAndDeleteCategoryCommand(categoryIdToDelete, request.NewCategoryId));
        return Ok(new { message = "Products reassigned and category deleted successfully" });
    }

    /// <summary>
    /// Force delete a category (deactivate all associated products and move children to parent) (Admin only)
    /// </summary>
    [HttpDelete("{id}/force")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    public async Task<IActionResult> ForceDeleteCategory(int id)
    {
        await _sender.Send(new ForceDeleteCategoryCommand(id));
        return Ok(new { message = "Category force deleted, child categories moved to parent level, and associated products deactivated successfully" });
    }
}

/// <summary>
/// DTO for creating/updating categories
/// </summary>
public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public int? ParentId { get; set; }
    public int SortOrder { get; set; } = 0;
}

/// <summary>
/// DTO for reassigning products to another category
/// </summary>
public class ReassignCategoryRequest
{
    public int NewCategoryId { get; set; }
}