using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.Entities;

/// <summary>
/// Blog post entity for content management - Updated for new admin system
/// </summary>
public class BlogPost
{
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Excerpt { get; set; }

    [Required]
    public string Content { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? FeaturedImageUrl { get; set; }

    [MaxLength(255)]
    public string? MetaTitle { get; set; }

    [MaxLength(500)]
    public string? MetaDescription { get; set; }

    public string Status { get; set; } = "draft"; // draft, published, scheduled
    public bool IsFeatured { get; set; } = false;

    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public int ViewCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;
    public int? CategoryId { get; set; }
    public int AuthorId { get; set; }

    // Navigation properties
    public virtual BlogCategory? Category { get; set; }
    public virtual User Author { get; set; } = null!;
    public virtual ICollection<BlogComment> Comments { get; set; } = new List<BlogComment>();
    public virtual ICollection<BlogPostTag> BlogPostTags { get; set; } = new List<BlogPostTag>();
}

/// <summary>
/// Blog comment entity for user comments on blog posts
/// </summary>
public class BlogComment
{
    public int Id { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string AuthorName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    [EmailAddress]
    public string AuthorEmail { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? AuthorWebsite { get; set; }

    public bool IsApproved { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int BlogPostId { get; set; }
    public int? UserId { get; set; } // Optional - for registered users

    // Navigation properties
    public virtual BlogPost BlogPost { get; set; } = null!;
    public virtual User? User { get; set; }
}