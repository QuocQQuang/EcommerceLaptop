namespace EcommerceLaptop.Core.DTOs;

public class BlogTagDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#6B7280";
    public bool IsActive { get; set; } = true;
    public int PostCount { get; set; }
}

public class CreateBlogTagRequest
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#6B7280";
    public bool IsActive { get; set; } = true;
}