using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using System.Security.Claims;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Blog controller for managing blog posts, categories, and comments
/// </summary>
[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class BlogController(IBlogService blogService, ILogger<BlogController> logger) : ControllerBase
{
    private readonly IBlogService _blogService = blogService;
    private readonly ILogger<BlogController> _logger = logger;

    #region Blog Posts

    /// <summary>
    /// Get paginated list of blog posts with filtering
    /// </summary>
    [HttpGet("posts")]
    public async Task<IActionResult> GetBlogPosts(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? categoryId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] bool? isPublished = null,
        [FromQuery] bool? isFeatured = null)
    {
        // For public access, only show published posts unless admin
        if (!User.IsInRole("Admin") && !isPublished.HasValue)
        {
            isPublished = true;
        }

        var result = await _blogService.GetBlogPostsAsync(pageNumber, pageSize, categoryId, searchTerm, isPublished, isFeatured);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get blog post by ID
    /// </summary>
    [HttpGet("posts/{id:int}")]
    public async Task<IActionResult> GetBlogPostById(int id)
    {
        var result = await _blogService.GetBlogPostByIdAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        if (result.Data == null)
            return NotFound(new { error = "Blog post not found" });

        // Check if user can view unpublished posts
        if (result.Data.Status != "published" && !User.IsInRole("Admin"))
            return NotFound(new { error = "Blog post not found" });

        // Increment view count for published posts
        if (result.Data.Status == "published")
        {
            await _blogService.IncrementViewCountAsync(id);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Get blog post by slug
    /// </summary>
    [HttpGet("posts/slug/{slug}")]
    public async Task<IActionResult> GetBlogPostBySlug(string slug)
    {
        var result = await _blogService.GetBlogPostBySlugAsync(slug);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        if (result.Data == null)
            return NotFound(new { error = "Blog post not found" });

        // Check if user can view unpublished posts
        if (result.Data.Status != "published" && !User.IsInRole("Admin"))
            return NotFound(new { error = "Blog post not found" });

        // Increment view count for published posts
        if (result.Data.Status == "published")
        {
            await _blogService.IncrementViewCountAsync(result.Data.Id);
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Like a blog post (increments like count)
    /// </summary>
    [HttpPost("posts/{id:int}/like")]
    public async Task<IActionResult> LikeBlogPost(int id)
    {
        var result = await _blogService.LikeBlogPostAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { likeCount = result.Data });
    }

    /// <summary>
    /// Unlike a blog post (decrements like count)
    /// </summary>
    [HttpPost("posts/{id:int}/unlike")]
    public async Task<IActionResult> UnlikeBlogPost(int id)
    {
        var result = await _blogService.UnlikeBlogPostAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { likeCount = result.Data });
    }

    /// <summary>
    /// Create new blog post (Admin only)
    /// </summary>
    [HttpPost("posts")]
    public async Task<IActionResult> CreateBlogPost([FromBody] CreateBlogPostRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        var userId = 1; // Default system admin ID
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var parsedId))
        {
            userId = parsedId;
        }

        var blogPost = new BlogPost
        {
            Title = request.Title,
            Excerpt = request.Excerpt,
            Content = request.Content,
            FeaturedImageUrl = request.FeaturedImageUrl,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription,
            Status = request.IsPublished ? "published" : "draft",
            IsFeatured = request.IsFeatured ?? false,
            CategoryId = request.CategoryId,
            AuthorId = userId
        };

        var result = await _blogService.CreateBlogPostAsync(blogPost, request.TagIds);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return CreatedAtAction(nameof(GetBlogPostById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Update blog post (Admin only)
    /// </summary>
    [HttpPatch("posts/{id:int}")]
    public async Task<IActionResult> UpdateBlogPost(int id, [FromBody] UpdateBlogPostRequest request)
    {
        var result = await _blogService.UpdateBlogPostAsync(id, request, User);

        if (!result.IsSuccess)
        {
            if (result.ErrorMessage == "Blog post not found") return NotFound(new { error = result.ErrorMessage });
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete blog post (Admin only)
    /// </summary>
    [HttpDelete("posts/{id:int}")]
    public async Task<IActionResult> DeleteBlogPost(int id)
    {
        var result = await _blogService.DeleteBlogPostAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Publish blog post (Admin only)
    /// </summary>
    [HttpPatch("posts/{id:int}/publish")]
    public async Task<IActionResult> PublishBlogPost(int id)
    {
        var result = await _blogService.PublishBlogPostAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Unpublish blog post (Admin only)
    /// </summary>
    [HttpPatch("posts/{id:int}/unpublish")]
    public async Task<IActionResult> UnpublishBlogPost(int id)
    {
        var result = await _blogService.UnpublishBlogPostAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { success = true });
    }

    #endregion

    #region Blog Categories

    /// <summary>
    /// Get all blog categories
    /// </summary>
    [HttpGet("categories")]
    public async Task<IActionResult> GetBlogCategories([FromQuery] bool? activeOnly = true)
    {
        var result = await _blogService.GetBlogCategoriesAsync(activeOnly ?? true);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get all blog categories for dropdowns (no pagination)
    /// </summary>
    [HttpGet("categories/all")]
    public async Task<IActionResult> GetAllBlogCategories()
    {
        var result = await _blogService.GetBlogCategoriesAsync(activeOnly: true);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        // Flatten to list for dropdown
        var allCategories = result.Data ?? new List<BlogCategory>();
        return Ok(allCategories);
    }

    /// <summary>
    /// Get blog category by ID
    /// </summary>
    [HttpGet("categories/{id:int}")]
    public async Task<IActionResult> GetBlogCategoryById(int id)
    {
        var result = await _blogService.GetBlogCategoryByIdAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        if (result.Data == null)
            return NotFound(new { error = "Blog category not found" });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get blog category by slug
    /// </summary>
    [HttpGet("categories/slug/{slug}")]
    public async Task<IActionResult> GetBlogCategoryBySlug(string slug)
    {
        var result = await _blogService.GetBlogCategoryBySlugAsync(slug);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        if (result.Data == null)
            return NotFound(new { error = "Blog category not found" });

        return Ok(result.Data);
    }

    /// <summary>
    /// Create blog category (Admin only)
    /// </summary>
    [HttpPost("categories")]
    public async Task<IActionResult> CreateBlogCategory([FromBody] BlogCategoryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var category = new BlogCategory
        {
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder
        };

        var result = await _blogService.CreateBlogCategoryAsync(category);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return CreatedAtAction(nameof(GetBlogCategoryById), new { id = result.Data!.Id }, result.Data);
    }

    /// <summary>
    /// Update blog category (Admin only)
    /// </summary>
    [HttpPut("categories/{id:int}")]
    public async Task<IActionResult> UpdateBlogCategory(int id, [FromBody] BlogCategoryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var category = new BlogCategory
        {
            Id = id,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            MetaTitle = request.MetaTitle,
            MetaDescription = request.MetaDescription,
            IsActive = request.IsActive,
            SortOrder = request.SortOrder
        };

        var result = await _blogService.UpdateBlogCategoryAsync(category);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Delete blog category (Admin only)
    /// </summary>
    [HttpDelete("categories/{id:int}")]
    public async Task<IActionResult> DeleteBlogCategory(int id)
    {
        var result = await _blogService.DeleteBlogCategoryAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { success = true });
    }

    #endregion

    #region Blog Comments

    /// <summary>
    /// Get comments for a blog post
    /// </summary>
    [HttpGet("posts/{blogPostId:int}/comments")]
    public async Task<IActionResult> GetBlogComments(
        int blogPostId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] bool? isApproved = null)
    {
        // For public access, only show approved comments unless admin
        if (!User.IsInRole("Admin") && !isApproved.HasValue)
        {
            isApproved = true;
        }

        var result = await _blogService.GetBlogCommentsAsync(blogPostId, pageNumber, pageSize, isApproved);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Create blog comment
    /// </summary>
    [HttpPost("posts/{blogPostId:int}/comments")]
    public async Task<IActionResult> CreateBlogComment(int blogPostId, [FromBody] CreateBlogCommentRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        int? userId = null;
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var id))
        {
            userId = id;
        }

        var comment = new BlogComment
        {
            Content = request.Content,
            AuthorName = request.AuthorName,
            AuthorEmail = request.AuthorEmail,
            AuthorWebsite = request.AuthorWebsite,
            BlogPostId = blogPostId,
            UserId = userId
        };

        var result = await _blogService.CreateBlogCommentAsync(comment);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Created(string.Empty, result.Data);
    }

    /// <summary>
    /// Approve blog comment (Admin only)
    /// </summary>
    [HttpPatch("comments/{id:int}/approve")]
    public async Task<IActionResult> ApproveBlogComment(int id)
    {
        var result = await _blogService.ApproveBlogCommentAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { success = true });
    }

    /// <summary>
    /// Delete blog comment (Admin only)
    /// </summary>
    [HttpDelete("comments/{id:int}")]
    public async Task<IActionResult> DeleteBlogComment(int id)
    {
        var result = await _blogService.DeleteBlogCommentAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { success = true });
    }

    #endregion

    #region Statistics

    /// <summary>
    /// Get blog statistics (Admin only)
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetBlogStatistics()
    {
        var result = await _blogService.GetBlogStatisticsAsync();

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    #endregion
}

#region DTOs

/// <summary>
/// Blog post request DTO for API - Limited to DB fields
/// </summary>
public class CreateBlogPostRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? FeaturedImageUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public bool IsPublished { get; set; } = false;
    public bool? IsFeatured { get; set; }
    public int CategoryId { get; set; }
    public List<int>? TagIds { get; set; } // For associating tags via junction
}

/// <summary>
/// Blog category request DTO
/// </summary>
public class BlogCategoryRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
}

#endregion
