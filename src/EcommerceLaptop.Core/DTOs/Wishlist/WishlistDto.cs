namespace EcommerceLaptop.Core.DTOs.Wishlist;

public class WishlistItemDto
{
    public Guid Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public List<WishlistProductImageDto> Images { get; set; } = new();
    public DateTime AddedAt { get; set; }
}

public class WishlistProductImageDto
{
    public int Id { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class WishlistResultDto
{
    public List<WishlistItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
}
