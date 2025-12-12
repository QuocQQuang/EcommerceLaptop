namespace EcommerceLaptop.Core.DTOs;

public record BlogTagDto(
    int Id,
    string Name,
    string Slug,
    string? Description,
    string Color = "#6B7280",
    bool IsActive = true,
    int PostCount = 0);

public record CreateBlogTagRequest(
    string Name,
    string Slug,
    string? Description,
    string Color = "#6B7280",
    bool IsActive = true);