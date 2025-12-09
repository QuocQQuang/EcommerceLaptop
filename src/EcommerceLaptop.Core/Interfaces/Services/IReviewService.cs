using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Interfaces.Services;

public interface IReviewService
{
    Task<ReviewsPagedDto> GetProductReviewsAsync(int productId, ReviewFilterDto filter);
    Task<ReviewDto?> GetReviewByIdAsync(int reviewId, int? currentUserId = null);
    Task<ReviewSummaryDto> GetProductReviewSummaryAsync(int productId);
    Task<ReviewDto> CreateReviewAsync(CreateReviewDto createReviewDto, int userId);
    Task<ReviewDto> UpdateReviewAsync(int reviewId, UpdateReviewDto updateReviewDto, int userId);
    Task<bool> DeleteReviewAsync(int reviewId, int userId, bool isAdmin = false);
    Task<bool> CanUserReviewProductAsync(int productId, int userId);
    Task<bool> HasUserReviewedProductAsync(int productId, int userId);
    Task<Review?> GetUserReviewForProductAsync(int productId, int userId);
    Task<List<ReviewDto>> GetUserReviewsAsync(int userId, int page = 1, int pageSize = 10);
    Task<bool> ValidateReviewOwnershipAsync(int reviewId, int userId);
}