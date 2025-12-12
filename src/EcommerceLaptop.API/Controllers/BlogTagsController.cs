using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Controller for managing blog tags
/// </summary>
[Route("api/[controller]")]
[ApiController]
[AllowAnonymous]
public class BlogTagsController(IBlogService blogService, ILogger<BlogTagsController> logger)
    : BaseApiController(logger)
{
    private readonly IBlogService _blogService = blogService;

    /// <summary>
    /// Get all blog tags
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBlogTags([FromQuery] bool? activeOnly = true)
    {
        var result = await _blogService.GetBlogTagsAsync(activeOnly ?? true);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all blog tags for dropdowns/autocomplete (no pagination)
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllBlogTags()
    {
        var result = await _blogService.GetBlogTagsAsync();

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        // Flatten to list for dropdown/autocomplete
        var allTags = result.Data ?? new List<BlogTag>();
        return Ok(allTags);
    }

    /// <summary>
    /// Get blog tag by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBlogTag(int id)
    {
        var result = await _blogService.GetBlogTagByIdAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        if (result.Data == null)
            return NotFound(new { error = "Tag not found" });

        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new blog tag (Admin only)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBlogTag([FromBody] CreateBlogTagRequest request)
    {
        var tag = new BlogTag
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Color = request.Color,
            IsActive = request.IsActive
        };

        var result = await _blogService.CreateBlogTagAsync(tag);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return CreatedAtAction(nameof(GetBlogTag), new { id = result.Data?.Id }, result.Data);
    }

    /// <summary>
    /// Update an existing blog tag (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBlogTag(int id, [FromBody] CreateBlogTagRequest request)
    {
        var tag = new BlogTag
        {
            Id = id,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Color = request.Color,
            IsActive = request.IsActive
        };

        var result = await _blogService.UpdateBlogTagAsync(tag);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete a blog tag (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBlogTag(int id)
    {
        var result = await _blogService.DeleteBlogTagAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { message = "Tag deleted successfully" });
    }
}

/// <summary>
/// DTO for creating/updating blog tags
/// </summary>
public class CreateBlogTagRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#6B7280";
    public bool IsActive { get; set; } = true;
}