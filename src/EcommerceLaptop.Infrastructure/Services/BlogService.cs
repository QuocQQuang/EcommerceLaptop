using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Infrastructure.Data;
using System.Security.Claims;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Blog service implementation for blog management
/// </summary>
public class BlogService : IBlogService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<BlogService> _logger;

    public BlogService(ApplicationDbContext context, ILogger<BlogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    #region Blog Posts

    public async Task<ServiceResult<PaginatedList<BlogPost>>> GetBlogPostsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        int? categoryId = null,
        string? searchTerm = null,
        bool? isPublished = null,
        bool? isFeatured = null)
    {
        try
        {
            var query = _context.BlogPosts.AsQueryable();

            // Apply filters
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(p =>
                    p.Title.ToLower().Contains(searchTerm) ||
                    p.Content.ToLower().Contains(searchTerm) ||
                    p.Excerpt!.ToLower().Contains(searchTerm));
            }

            if (isPublished.HasValue)
                query = query.Where(p => p.Status == (isPublished.Value ? "published" : "draft"));

            if (isFeatured.HasValue)
                query = query.Where(p => p.IsFeatured == isFeatured.Value);

            // Order by published date (newest first) for published posts, created date for drafts
            query = query.OrderByDescending(p => p.Status == "published" ? p.PublishedAt : p.CreatedAt);

            var totalCount = await query.CountAsync();
            var posts = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<BlogPost>(posts, totalCount, pageNumber, pageSize);

            return ServiceResult<PaginatedList<BlogPost>>.Success(paginatedList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog posts");
            return ServiceResult<PaginatedList<BlogPost>>.Failure("Error retrieving blog posts");
        }
    }

    public async Task<ServiceResult<BlogPost?>> GetBlogPostByIdAsync(int id)
    {
        try
        {
            var post = await _context.BlogPosts
                .FirstOrDefaultAsync(p => p.Id == id);

            return ServiceResult<BlogPost?>.Success(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog post by ID {Id}", id);
            return ServiceResult<BlogPost?>.Failure("Error retrieving blog post");
        }
    }

    public async Task<ServiceResult<BlogPost?>> GetBlogPostBySlugAsync(string slug)
    {
        try
        {
            var post = await _context.BlogPosts
                .FirstOrDefaultAsync(p => p.Slug == slug);

            return ServiceResult<BlogPost?>.Success(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog post by slug {Slug}", slug);
            return ServiceResult<BlogPost?>.Failure("Error retrieving blog post");
        }
    }

    public async Task<ServiceResult<BlogPost>> CreateBlogPostAsync(BlogPost blogPost, List<int>? tagIds = null)
    {
        try
        {
            // Generate unique slug if not provided
            if (string.IsNullOrEmpty(blogPost.Slug))
            {
                blogPost.Slug = await GenerateUniqueSlugAsync(blogPost.Title);
            }
            else
            {
                // Ensure slug is unique
                blogPost.Slug = await EnsureUniqueSlugAsync(blogPost.Slug, blogPost.Id);
            }

            if (blogPost.Status == "published" && !blogPost.PublishedAt.HasValue)
            {
                blogPost.PublishedAt = DateTime.UtcNow;
            }

            blogPost.CreatedAt = DateTime.UtcNow;
            blogPost.UpdatedAt = DateTime.UtcNow;

            _context.BlogPosts.Add(blogPost);
            await _context.SaveChangesAsync();

            // Associate tags if provided
            if (tagIds != null && tagIds.Any())
            {
                await AssociateTagsWithPostAsync(blogPost.Id, tagIds);
            }

            // Reload to include tags
            await _context.Entry(blogPost).ReloadAsync();

            return ServiceResult<BlogPost>.Success(blogPost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating blog post");
            return ServiceResult<BlogPost>.Failure("Error creating blog post");
        }
    }

    public async Task<ServiceResult<BlogPost>> UpdateBlogPostAsync(int id, UpdateBlogPostRequest request, ClaimsPrincipal user)
    {
        try
        {
            var existingPost = await _context.BlogPosts
                .FirstOrDefaultAsync(p => p.Id == id);

            if (existingPost == null)
            {
                return ServiceResult<BlogPost>.Failure("Blog post not found");
            }

            var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                // Fallback or handle error if user is not found, depending on auth policy
                // For now, let's assume a default or fail
                return ServiceResult<BlogPost>.Failure("User not authenticated");
            }

            // Only admins or the author can edit
            if (!user.IsInRole("Admin") && existingPost.AuthorId != userId)
            {
                return ServiceResult<BlogPost>.Failure("User not authorized to edit this post");
            }


            // Update fields only if they are provided in the request
            if (request.Title != null) existingPost.Title = request.Title;
            if (request.Slug != null) existingPost.Slug = await EnsureUniqueSlugAsync(request.Slug, id);
            if (request.Excerpt != null) existingPost.Excerpt = request.Excerpt;
            if (request.Content != null) existingPost.Content = request.Content;
            if (request.FeaturedImageUrl != null) existingPost.FeaturedImageUrl = request.FeaturedImageUrl;
            if (request.MetaTitle != null) existingPost.MetaTitle = request.MetaTitle;
            if (request.MetaDescription != null) existingPost.MetaDescription = request.MetaDescription;
            if (request.CategoryId.HasValue) existingPost.CategoryId = request.CategoryId.Value;
            if (request.IsFeatured.HasValue) existingPost.IsFeatured = request.IsFeatured.Value;

            existingPost.UpdatedAt = DateTime.UtcNow;

            // Handle publishing status
            if (request.IsPublished.HasValue)
            {
                var newStatus = request.IsPublished.Value ? "published" : "draft";
                if (newStatus == "published" && existingPost.Status != "published")
                {
                    existingPost.PublishedAt = DateTime.UtcNow;
                }
                else if (newStatus != "published")
                {
                    existingPost.PublishedAt = null;
                }
                existingPost.Status = newStatus;
            }

            await _context.SaveChangesAsync();

            // Update tags if provided
            if (request.TagIds != null)
            {
                await UpdatePostTagsAsync(existingPost.Id, request.TagIds);
            }

            // Reload to get the updated tags
            await _context.Entry(existingPost).Collection(p => p.BlogPostTags).LoadAsync();
            foreach (var bpt in existingPost.BlogPostTags)
            {
                await _context.Entry(bpt).Reference(t => t.BlogTag).LoadAsync();
            }

            return ServiceResult<BlogPost>.Success(existingPost);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating blog post {Id}", id);
            return ServiceResult<BlogPost>.Failure("Error updating blog post");
        }
    }

    public async Task<ServiceResult<bool>> DeleteBlogPostAsync(int id)
    {
        try
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return ServiceResult<bool>.Failure("Blog post not found");
            }

            _context.BlogPosts.Remove(post);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blog post {Id}", id);
            return ServiceResult<bool>.Failure("Error deleting blog post");
        }
    }

    public async Task<ServiceResult<bool>> PublishBlogPostAsync(int id)
    {
        try
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return ServiceResult<bool>.Failure("Blog post not found");
            }

            post.Status = "published";
            post.PublishedAt = DateTime.UtcNow;
            post.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing blog post {Id}", id);
            return ServiceResult<bool>.Failure("Error publishing blog post");
        }
    }

    public async Task<ServiceResult<bool>> UnpublishBlogPostAsync(int id)
    {
        try
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return ServiceResult<bool>.Failure("Blog post not found");
            }

            post.Status = "draft";
            post.PublishedAt = null;
            post.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unpublishing blog post {Id}", id);
            return ServiceResult<bool>.Failure("Error unpublishing blog post");
        }
    }

    public async Task<ServiceResult<bool>> IncrementViewCountAsync(int id)
    {
        try
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return ServiceResult<bool>.Failure("Blog post not found");
            }

            post.ViewCount++;
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error incrementing view count for blog post {Id}", id);
            return ServiceResult<bool>.Failure("Error updating view count");
        }
    }

    #endregion

    #region Blog Categories

    public async Task<ServiceResult<List<BlogCategory>>> GetBlogCategoriesAsync(bool activeOnly = true)
    {
        try
        {
            var query = _context.BlogCategories.AsQueryable();

            if (activeOnly)
                query = query.Where(c => c.IsActive);

            var categories = await query
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return ServiceResult<List<BlogCategory>>.Success(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog categories");
            return ServiceResult<List<BlogCategory>>.Failure("Error retrieving blog categories");
        }
    }

    public async Task<ServiceResult<BlogCategory?>> GetBlogCategoryByIdAsync(int id)
    {
        try
        {
            var category = await _context.BlogCategories
                .Include(c => c.BlogPosts)
                .FirstOrDefaultAsync(c => c.Id == id);

            return ServiceResult<BlogCategory?>.Success(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog category by ID {Id}", id);
            return ServiceResult<BlogCategory?>.Failure("Error retrieving blog category");
        }
    }

    public async Task<ServiceResult<BlogCategory?>> GetBlogCategoryBySlugAsync(string slug)
    {
        try
        {
            var category = await _context.BlogCategories
                .Include(c => c.BlogPosts.Where(p => p.Status == "published"))
                .FirstOrDefaultAsync(c => c.Slug == slug);

            return ServiceResult<BlogCategory?>.Success(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog category by slug {Slug}", slug);
            return ServiceResult<BlogCategory?>.Failure("Error retrieving blog category");
        }
    }

    public async Task<ServiceResult<BlogCategory>> CreateBlogCategoryAsync(BlogCategory category)
    {
        try
        {
            // Generate unique slug if not provided
            if (string.IsNullOrEmpty(category.Slug))
            {
                category.Slug = await GenerateUniqueCategorySlugAsync(category.Name);
            }
            else
            {
                // Ensure slug is unique
                category.Slug = await EnsureUniqueCategorySlugAsync(category.Slug, category.Id);
            }

            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;

            _context.BlogCategories.Add(category);
            await _context.SaveChangesAsync();

            return ServiceResult<BlogCategory>.Success(category);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating blog category");
            return ServiceResult<BlogCategory>.Failure("Error creating blog category");
        }
    }

    public async Task<ServiceResult<BlogCategory>> UpdateBlogCategoryAsync(BlogCategory category)
    {
        try
        {
            var existingCategory = await _context.BlogCategories.FindAsync(category.Id);
            if (existingCategory == null)
            {
                return ServiceResult<BlogCategory>.Failure("Blog category not found");
            }

            // Update fields
            existingCategory.Name = category.Name;
            existingCategory.Slug = await EnsureUniqueCategorySlugAsync(category.Slug, category.Id);
            existingCategory.Description = category.Description;
            existingCategory.MetaTitle = category.MetaTitle;
            existingCategory.MetaDescription = category.MetaDescription;
            existingCategory.IsActive = category.IsActive;
            existingCategory.SortOrder = category.SortOrder;
            existingCategory.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<BlogCategory>.Success(existingCategory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating blog category {Id}", category.Id);
            return ServiceResult<BlogCategory>.Failure("Error updating blog category");
        }
    }

    public async Task<ServiceResult<bool>> DeleteBlogCategoryAsync(int id)
    {
        try
        {
            var category = await _context.BlogCategories
                .Include(c => c.BlogPosts)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return ServiceResult<bool>.Failure("Blog category not found");
            }

            // Check if category has blog posts
            if (category.BlogPosts.Any())
            {
                return ServiceResult<bool>.Failure("Cannot delete category with existing blog posts");
            }

            _context.BlogCategories.Remove(category);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blog category {Id}", id);
            return ServiceResult<bool>.Failure("Error deleting blog category");
        }
    }

    #endregion

    #region Blog Comments

    public async Task<ServiceResult<PaginatedList<BlogComment>>> GetBlogCommentsAsync(
        int blogPostId,
        int pageNumber = 1,
        int pageSize = 10,
        bool? isApproved = null)
    {
        try
        {
            var query = _context.BlogComments
                .Include(c => c.User)
                .Where(c => c.BlogPostId == blogPostId);

            if (isApproved.HasValue)
                query = query.Where(c => c.IsApproved == isApproved.Value);

            query = query.OrderByDescending(c => c.CreatedAt);

            var totalCount = await query.CountAsync();
            var comments = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var paginatedList = new PaginatedList<BlogComment>(comments, totalCount, pageNumber, pageSize);

            return ServiceResult<PaginatedList<BlogComment>>.Success(paginatedList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog comments for post {BlogPostId}", blogPostId);
            return ServiceResult<PaginatedList<BlogComment>>.Failure("Error retrieving blog comments");
        }
    }

    public async Task<ServiceResult<BlogComment>> CreateBlogCommentAsync(BlogComment comment)
    {
        try
        {
            comment.CreatedAt = DateTime.UtcNow;
            comment.IsApproved = true; // Auto-approve comments by default per policy

            _context.BlogComments.Add(comment);
            await _context.SaveChangesAsync();

            return ServiceResult<BlogComment>.Success(comment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating blog comment");
            return ServiceResult<BlogComment>.Failure("Error creating blog comment");
        }
    }

    public async Task<ServiceResult<int>> LikeBlogPostAsync(int id)
    {
        try
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return ServiceResult<int>.Failure("Blog post not found");
            }

            post.LikeCount++;
            post.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return ServiceResult<int>.Success(post.LikeCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error liking blog post {Id}", id);
            return ServiceResult<int>.Failure("Error liking blog post");
        }
    }

    public async Task<ServiceResult<int>> UnlikeBlogPostAsync(int id)
    {
        try
        {
            var post = await _context.BlogPosts.FindAsync(id);
            if (post == null)
            {
                return ServiceResult<int>.Failure("Blog post not found");
            }

            if (post.LikeCount > 0)
            {
                post.LikeCount--;
                post.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return ServiceResult<int>.Success(post.LikeCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unliking blog post {Id}", id);
            return ServiceResult<int>.Failure("Error unliking blog post");
        }
    }

    public async Task<ServiceResult<bool>> ApproveBlogCommentAsync(int id)
    {
        try
        {
            var comment = await _context.BlogComments.FindAsync(id);
            if (comment == null)
            {
                return ServiceResult<bool>.Failure("Blog comment not found");
            }

            comment.IsApproved = true;
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving blog comment {Id}", id);
            return ServiceResult<bool>.Failure("Error approving blog comment");
        }
    }

    public async Task<ServiceResult<bool>> DeleteBlogCommentAsync(int id)
    {
        try
        {
            var comment = await _context.BlogComments.FindAsync(id);
            if (comment == null)
            {
                return ServiceResult<bool>.Failure("Blog comment not found");
            }

            _context.BlogComments.Remove(comment);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blog comment {Id}", id);
            return ServiceResult<bool>.Failure("Error deleting blog comment");
        }
    }

    #endregion

    #region Statistics

    public async Task<ServiceResult<BlogStatistics>> GetBlogStatisticsAsync()
    {
        try
        {
            var totalPosts = await _context.BlogPosts.CountAsync();
            var publishedPosts = await _context.BlogPosts.CountAsync(p => p.Status == "published");
            var draftPosts = totalPosts - publishedPosts;
            var totalCategories = await _context.BlogCategories.CountAsync(c => c.IsActive);
            var totalComments = await _context.BlogComments.CountAsync();
            var pendingComments = await _context.BlogComments.CountAsync(c => !c.IsApproved);
            var totalViews = await _context.BlogPosts.SumAsync(p => p.ViewCount);

            var statistics = new BlogStatistics
            {
                TotalPosts = totalPosts,
                PublishedPosts = publishedPosts,
                DraftPosts = draftPosts,
                TotalCategories = totalCategories,
                TotalComments = totalComments,
                PendingComments = pendingComments,
                TotalViews = totalViews
            };

            return ServiceResult<BlogStatistics>.Success(statistics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog statistics");
            return ServiceResult<BlogStatistics>.Failure("Error retrieving blog statistics");
        }
    }

    #endregion

    #region Helper Methods

    private async Task<string> GenerateUniqueSlugAsync(string title)
    {
        var baseSlug = GenerateSlug(title);
        return await EnsureUniqueSlugAsync(baseSlug, 0);
    }

    private async Task<string> EnsureUniqueSlugAsync(string slug, int excludeId)
    {
        var originalSlug = GenerateSlug(slug);
        var currentSlug = originalSlug;
        var counter = 1;

        while (await _context.BlogPosts.AnyAsync(p => p.Slug == currentSlug && p.Id != excludeId))
        {
            currentSlug = $"{originalSlug}-{counter}";
            counter++;
        }

        return currentSlug;
    }

    private async Task<string> GenerateUniqueCategorySlugAsync(string name)
    {
        var baseSlug = GenerateSlug(name);
        return await EnsureUniqueCategorySlugAsync(baseSlug, 0);
    }

    private async Task<string> EnsureUniqueCategorySlugAsync(string slug, int excludeId)
    {
        var originalSlug = GenerateSlug(slug);
        var currentSlug = originalSlug;
        var counter = 1;

        while (await _context.BlogCategories.AnyAsync(c => c.Slug == currentSlug && c.Id != excludeId))
        {
            currentSlug = $"{originalSlug}-{counter}";
            counter++;
        }

        return currentSlug;
    }

    private static string GenerateSlug(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Convert to lowercase and replace spaces with hyphens
        var slug = input.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace(".", "-")
            .Replace(",", "")
            .Replace(":", "")
            .Replace(";", "");

        // Remove any characters that aren't alphanumeric or hyphens
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // Remove multiple consecutive hyphens
        slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");

        // Trim hyphens from start and end
        slug = slug.Trim('-');

        return slug;
    }

    #endregion

    #region Helper Methods for Tags

    private async Task AssociateTagsWithPostAsync(int postId, List<int> tagIds)
    {
        // Remove existing tags for this post
        var existingTags = await _context.BlogPostTags.Where(bpt => bpt.BlogPostId == postId).ToListAsync();
        _context.BlogPostTags.RemoveRange(existingTags);

        // Add new tags if they exist
        foreach (var tagId in tagIds.Distinct())
        {
            if (await _context.BlogTags.AnyAsync(t => t.Id == tagId))
            {
                _context.BlogPostTags.Add(new BlogPostTag { BlogPostId = postId, BlogTagId = tagId });
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task UpdatePostTagsAsync(int postId, List<int> tagIds)
    {
        // Remove existing tags for this post
        var existingTags = await _context.BlogPostTags.Where(bpt => bpt.BlogPostId == postId).ToListAsync();
        _context.BlogPostTags.RemoveRange(existingTags);

        // Add new tags if they exist
        if (tagIds != null && tagIds.Any())
        {
            foreach (var tagId in tagIds.Distinct())
            {
                if (await _context.BlogTags.AnyAsync(t => t.Id == tagId))
                {
                    _context.BlogPostTags.Add(new BlogPostTag { BlogPostId = postId, BlogTagId = tagId });
                }
            }
        }

        await _context.SaveChangesAsync();
    }

    #endregion

    #region Blog Tags

    public async Task<ServiceResult<List<BlogTag>>> GetBlogTagsAsync(bool activeOnly = true)
    {
        try
        {
            var query = _context.BlogTags.AsQueryable();

            if (activeOnly)
                query = query.Where(t => t.IsActive);

            var tags = await query
                .OrderBy(t => t.Name)
                .ToListAsync();

            return ServiceResult<List<BlogTag>>.Success(tags);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog tags");
            return ServiceResult<List<BlogTag>>.Failure("Error retrieving blog tags");
        }
    }

    public async Task<ServiceResult<BlogTag?>> GetBlogTagByIdAsync(int id)
    {
        try
        {
            var tag = await _context.BlogTags
                .FirstOrDefaultAsync(t => t.Id == id);

            return ServiceResult<BlogTag?>.Success(tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog tag by ID {Id}", id);
            return ServiceResult<BlogTag?>.Failure("Error retrieving blog tag");
        }
    }

    public async Task<ServiceResult<BlogTag?>> GetBlogTagBySlugAsync(string slug)
    {
        try
        {
            var tag = await _context.BlogTags
                .FirstOrDefaultAsync(t => t.Slug == slug);

            return ServiceResult<BlogTag?>.Success(tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting blog tag by slug {Slug}", slug);
            return ServiceResult<BlogTag?>.Failure("Error retrieving blog tag");
        }
    }

    public async Task<ServiceResult<BlogTag>> CreateBlogTagAsync(BlogTag tag)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(tag.Name))
                return ServiceResult<BlogTag>.Failure("Tag name is required");

            // Generate slug if not provided
            if (string.IsNullOrWhiteSpace(tag.Slug))
                tag.Slug = await GenerateUniqueTagSlugAsync(tag.Name);
            else
            {
                // Ensure slug is unique
                var existingTag = await _context.BlogTags
                    .FirstOrDefaultAsync(t => t.Slug == tag.Slug);
                if (existingTag != null)
                    return ServiceResult<BlogTag>.Failure("A tag with this slug already exists");
            }

            // Set default color if not provided
            if (string.IsNullOrWhiteSpace(tag.Color))
                tag.Color = "#6B7280";

            tag.CreatedAt = DateTime.UtcNow;

            _context.BlogTags.Add(tag);
            await _context.SaveChangesAsync();

            return ServiceResult<BlogTag>.Success(tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating blog tag");
            return ServiceResult<BlogTag>.Failure("Error creating blog tag");
        }
    }

    public async Task<ServiceResult<BlogTag>> UpdateBlogTagAsync(BlogTag tag)
    {
        try
        {
            var existingTag = await _context.BlogTags.FindAsync(tag.Id);
            if (existingTag == null)
                return ServiceResult<BlogTag>.Failure("Tag not found");

            // Validate required fields
            if (string.IsNullOrWhiteSpace(tag.Name))
                return ServiceResult<BlogTag>.Failure("Tag name is required");

            // Check slug uniqueness if changed
            if (tag.Slug != existingTag.Slug)
            {
                var duplicateTag = await _context.BlogTags
                    .FirstOrDefaultAsync(t => t.Slug == tag.Slug && t.Id != tag.Id);
                if (duplicateTag != null)
                    return ServiceResult<BlogTag>.Failure("A tag with this slug already exists");
            }

            // Update properties
            existingTag.Name = tag.Name;
            existingTag.Slug = tag.Slug;
            existingTag.Description = tag.Description;
            existingTag.Color = tag.Color ?? "#6B7280";
            existingTag.IsActive = tag.IsActive;
            existingTag.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return ServiceResult<BlogTag>.Success(existingTag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating blog tag with ID {Id}", tag.Id);
            return ServiceResult<BlogTag>.Failure("Error updating blog tag");
        }
    }

    public async Task<ServiceResult<bool>> DeleteBlogTagAsync(int id)
    {
        try
        {
            var tag = await _context.BlogTags.FindAsync(id);
            if (tag == null)
                return ServiceResult<bool>.Failure("Tag not found");

            // Check if tag is being used by any blog posts
            var isUsed = await _context.BlogPostTags.AnyAsync(bpt => bpt.BlogTagId == id);
            if (isUsed)
                return ServiceResult<bool>.Failure("Cannot delete tag that is being used by blog posts");

            _context.BlogTags.Remove(tag);
            await _context.SaveChangesAsync();

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting blog tag with ID {Id}", id);
            return ServiceResult<bool>.Failure("Error deleting blog tag");
        }
    }

    private async Task<string> GenerateUniqueTagSlugAsync(string name)
    {
        var baseSlug = GenerateSlug(name);
        return await EnsureUniqueTagSlugAsync(baseSlug, 0);
    }

    private async Task<string> EnsureUniqueTagSlugAsync(string slug, int excludeId)
    {
        var originalSlug = GenerateSlug(slug);
        var currentSlug = originalSlug;
        var counter = 1;

        while (await _context.BlogTags.AnyAsync(t => t.Slug == currentSlug && t.Id != excludeId))
        {
            currentSlug = $"{originalSlug}-{counter}";
            counter++;
        }

        return currentSlug;
    }

    #endregion
}