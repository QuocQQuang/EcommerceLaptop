using EcommerceLaptop.Core.DTOs.Wishlist;

namespace EcommerceLaptop.Core.Services;

public interface IWishlistService
{
    Task<WishlistResultDto> GetWishlistAsync(int userId);
    Task AddToWishlistAsync(int userId, int productId);
    Task RemoveFromWishlistAsync(int userId, int productId);
    Task ClearWishlistAsync(int userId);
    Task<bool> CheckInWishlistAsync(int userId, int productId);
    Task<int> GetWishlistCountAsync(int userId);
}
