using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.Entities;

/// <summary>
/// Blog category entity for organizing blog posts
/// </summary>
public class BlogCategory
{
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    [MaxLength(200)]
    public string? MetaTitle { get; set; }

    [MaxLength(500)]
    public string? MetaDescription { get; set; }

    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 0;
    public int? ParentId { get; set; } // For hierarchical categories

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties for hierarchical structure
    public virtual BlogCategory? Parent { get; set; }
    public virtual ICollection<BlogCategory> Children { get; set; } = new List<BlogCategory>();
    public virtual ICollection<BlogPost> BlogPosts { get; set; } = new List<BlogPost>();
}