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
public class BlogTagsController : BaseApiController
{
    private readonly IBlogService _blogService;

    public BlogTagsController(IBlogService blogService, ILogger<BlogTagsController> logger)
        : base(logger)
    {
        _blogService = blogService;
    }

    /// <summary>
    /// Get all blog tags
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetBlogTags([FromQuery] bool? activeOnly = true)
    {
        try
        {
            var result = await _blogService.GetBlogTagsAsync(activeOnly ?? true);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blog tags");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all blog tags for dropdowns/autocomplete (no pagination)
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllBlogTags()
    {
        try
        {
            var result = await _blogService.GetBlogTagsAsync();

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            // Flatten to list for dropdown/autocomplete
            var allTags = result.Data ?? new List<BlogTag>();
            return Ok(allTags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all blog tags");
            return StatusCode(500, new { error = "Error retrieving all blog tags" });
        }
    }

    /// <summary>
    /// Get blog tag by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBlogTag(int id)
    {
        try
        {
            var result = await _blogService.GetBlogTagByIdAsync(id);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            if (result.Data == null)
                return NotFound(new { error = "Tag not found" });

            return Ok(result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving blog tag with ID {TagId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new blog tag (Admin only)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBlogTag([FromBody] CreateBlogTagRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

            return CreatedAtAction(nameof(GetBlogTag), new { id = result.Data.Id }, result.Data);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating blog tag");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update an existing blog tag (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateBlogTag(int id, [FromBody] CreateBlogTagRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating blog tag with ID {TagId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a blog tag (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteBlogTag(int id)
    {
        try
        {
            var result = await _blogService.DeleteBlogTagAsync(id);

            if (!result.IsSuccess)
                return BadRequest(new { error = result.ErrorMessage });

            return Ok(new { message = "Tag deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blog tag with ID {TagId}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
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