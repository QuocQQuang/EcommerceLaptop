using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.ValueObjects;
using System.Security.Claims;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Service interface for blog management
/// </summary>
public interface IBlogService
{
    // Blog Posts
    Task<ServiceResult<PaginatedList<BlogPost>>> GetBlogPostsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        int? categoryId = null,
        string? searchTerm = null,
        bool? isPublished = null,
        bool? isFeatured = null);

    Task<ServiceResult<BlogPost?>> GetBlogPostByIdAsync(int id);
    Task<ServiceResult<BlogPost?>> GetBlogPostBySlugAsync(string slug);
    Task<ServiceResult<BlogPost>> CreateBlogPostAsync(BlogPost blogPost, List<int>? tagIds = null);
    Task<ServiceResult<BlogPost>> UpdateBlogPostAsync(int id, UpdateBlogPostRequest request, ClaimsPrincipal user);
    Task<ServiceResult<bool>> DeleteBlogPostAsync(int id);
    Task<ServiceResult<bool>> PublishBlogPostAsync(int id);
    Task<ServiceResult<bool>> UnpublishBlogPostAsync(int id);
    Task<ServiceResult<bool>> IncrementViewCountAsync(int id);
    Task<ServiceResult<int>> LikeBlogPostAsync(int id);
    Task<ServiceResult<int>> UnlikeBlogPostAsync(int id);

    // Blog Categories
    Task<ServiceResult<List<BlogCategory>>> GetBlogCategoriesAsync(bool activeOnly = true);
    Task<ServiceResult<BlogCategory?>> GetBlogCategoryByIdAsync(int id);
    Task<ServiceResult<BlogCategory?>> GetBlogCategoryBySlugAsync(string slug);
    Task<ServiceResult<BlogCategory>> CreateBlogCategoryAsync(BlogCategory category);
    Task<ServiceResult<BlogCategory>> UpdateBlogCategoryAsync(BlogCategory category);
    Task<ServiceResult<bool>> DeleteBlogCategoryAsync(int id);

    // Blog Comments
    Task<ServiceResult<PaginatedList<BlogComment>>> GetBlogCommentsAsync(
        int blogPostId,
        int pageNumber = 1,
        int pageSize = 10,
        bool? isApproved = null);

    Task<ServiceResult<BlogComment>> CreateBlogCommentAsync(BlogComment comment);
    Task<ServiceResult<bool>> ApproveBlogCommentAsync(int id);
    Task<ServiceResult<bool>> DeleteBlogCommentAsync(int id);

    // Blog Tags
    Task<ServiceResult<List<BlogTag>>> GetBlogTagsAsync(bool activeOnly = true);
    Task<ServiceResult<BlogTag?>> GetBlogTagByIdAsync(int id);
    Task<ServiceResult<BlogTag?>> GetBlogTagBySlugAsync(string slug);
    Task<ServiceResult<BlogTag>> CreateBlogTagAsync(BlogTag tag);
    Task<ServiceResult<BlogTag>> UpdateBlogTagAsync(BlogTag tag);
    Task<ServiceResult<bool>> DeleteBlogTagAsync(int id);

    // Statistics
    Task<ServiceResult<BlogStatistics>> GetBlogStatisticsAsync();
}

/// <summary>
/// Blog statistics data transfer object
/// </summary>
public class BlogStatistics
{
    public int TotalPosts { get; set; }
    public int PublishedPosts { get; set; }
    public int DraftPosts { get; set; }
    public int TotalCategories { get; set; }
    public int TotalComments { get; set; }
    public int PendingComments { get; set; }
    public int TotalViews { get; set; }
}

public class UpdateBlogPostRequest
{
    public string? Title { get; set; }
    public string? Excerpt { get; set; }
    public string? Content { get; set; }
    public string? FeaturedImageUrl { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public bool? IsPublished { get; set; }
    public bool? IsFeatured { get; set; }
    public int? CategoryId { get; set; }
    public List<int>? TagIds { get; set; }
    public string? Status { get; set; }
    public string? Slug { get; set; }
}

/// <summary>
/// Blog post request DTO for API
/// </summary>
public class CreateBlogPostRequest
{
    public string Title { get; set; } = string.Empty;
    public string? Excerpt { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? FeaturedImage { get; set; }
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public bool IsPublished { get; set; } = false;
    public bool IsFeatured { get; set; } = false;
    public int CategoryId { get; set; }
}

/// <summary>
/// Blog comment request DTO for API
/// </summary>
public class CreateBlogCommentRequest
{
    public string Content { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string? AuthorWebsite { get; set; }
    public int BlogPostId { get; set; }
}