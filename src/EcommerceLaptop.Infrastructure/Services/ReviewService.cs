using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Infrastructure.Data;

namespace EcommerceLaptop.Infrastructure.Services;

public class ReviewService : IReviewService
{
    private readonly ApplicationDbContext _context;

    public ReviewService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewsPagedDto> GetProductReviewsAsync(int productId, ReviewFilterDto filter)
    {
        var query = _context.Reviews
            .Include(r => r.User)
            .Where(r => r.ProductId == productId);

        // Apply filters
        if (filter.Rating.HasValue)
        {
            query = query.Where(r => r.Rating == filter.Rating.Value);
        }

        if (filter.VerifiedPurchaseOnly == true)
        {
            query = query.Where(r => r.IsVerifiedPurchase);
        }

        // Apply sorting
        query = filter.SortBy.ToLower() switch
        {
            "rating" => filter.SortOrder.ToUpper() == "ASC"
                ? query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt)
                : query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
            "createdat" or _ => filter.SortOrder.ToUpper() == "ASC"
                ? query.OrderBy(r => r.CreatedAt)
                : query.OrderByDescending(r => r.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

        var reviews = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                UserId = r.UserId,
                UserName = $"{r.User.FirstName} {r.User.LastName}",
                UserEmail = r.User.Email,
                UserProfilePictureUrl = r.User.ProfilePictureUrl,
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                CanEdit = false, // Will be set based on current user
                CanDelete = false // Will be set based on current user
            })
            .ToListAsync();

        var summary = await GetProductReviewSummaryAsync(productId);

        return new ReviewsPagedDto
        {
            Reviews = reviews,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = totalPages,
            Summary = summary
        };
    }

    public async Task<ReviewDto?> GetReviewByIdAsync(int reviewId, int? currentUserId = null)
    {
        var review = await _context.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null) return null;

        return new ReviewDto
        {
            Id = review.Id,
            ProductId = review.ProductId,
            UserId = review.UserId,
            UserName = $"{review.User.FirstName} {review.User.LastName}",
            UserEmail = review.User.Email,
            UserProfilePictureUrl = review.User.ProfilePictureUrl,
            Rating = review.Rating,
            Title = review.Title,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt,
            IsVerifiedPurchase = review.IsVerifiedPurchase,
            CanEdit = currentUserId.HasValue && (review.UserId == currentUserId.Value),
            CanDelete = currentUserId.HasValue && (review.UserId == currentUserId.Value)
        };
    }

    public async Task<ReviewSummaryDto> GetProductReviewSummaryAsync(int productId)
    {
        var reviews = await _context.Reviews
            .Where(r => r.ProductId == productId)
            .ToListAsync();

        var totalReviews = reviews.Count;
        var averageRating = totalReviews > 0 ? reviews.Average(r => r.Rating) : 0;
        var verifiedPurchaseCount = reviews.Count(r => r.IsVerifiedPurchase);

        var ratingDistribution = new Dictionary<int, int>();
        for (int i = 1; i <= 5; i++)
        {
            ratingDistribution[i] = reviews.Count(r => r.Rating == i);
        }

        return new ReviewSummaryDto
        {
            ProductId = productId,
            TotalReviews = totalReviews,
            AverageRating = Math.Round(averageRating, 1),
            RatingDistribution = ratingDistribution,
            VerifiedPurchaseCount = verifiedPurchaseCount
        };
    }

    public async Task<ReviewDto> CreateReviewAsync(CreateReviewDto createReviewDto, int userId)
    {
        // Check if user already reviewed this product
        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == createReviewDto.ProductId && r.UserId == userId);

        if (existingReview != null)
        {
            throw new InvalidOperationException("User has already reviewed this product");
        }

        // Check if user has purchased this product
        var hasVerifiedPurchase = await HasVerifiedPurchaseAsync(createReviewDto.ProductId, userId);

        var review = new Review
        {
            ProductId = createReviewDto.ProductId,
            UserId = userId,
            Rating = createReviewDto.Rating,
            Title = createReviewDto.Title,
            Comment = createReviewDto.Comment,
            CreatedAt = DateTime.UtcNow,
            IsVerifiedPurchase = hasVerifiedPurchase
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return await GetReviewByIdAsync(review.Id, userId) ?? throw new InvalidOperationException("Failed to create review");
    }

    public async Task<ReviewDto> UpdateReviewAsync(int reviewId, UpdateReviewDto updateReviewDto, int userId)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null)
        {
            throw new InvalidOperationException("Review not found");
        }

        if (review.UserId != userId)
        {
            throw new UnauthorizedAccessException("User can only update their own reviews");
        }

        review.Rating = updateReviewDto.Rating;
        review.Title = updateReviewDto.Title;
        review.Comment = updateReviewDto.Comment;

        await _context.SaveChangesAsync();

        return await GetReviewByIdAsync(reviewId, userId) ?? throw new InvalidOperationException("Failed to update review");
    }

    public async Task<bool> DeleteReviewAsync(int reviewId, int userId, bool isAdmin = false)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null) return false;

        if (!isAdmin && review.UserId != userId)
        {
            throw new UnauthorizedAccessException("User can only delete their own reviews");
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> CanUserReviewProductAsync(int productId, int userId)
    {
        // User can review if they haven't reviewed already and product exists
        var hasReviewed = await HasUserReviewedProductAsync(productId, userId);
        var productExists = await _context.Products.AnyAsync(p => p.Id == productId);

        return !hasReviewed && productExists;
    }

    public async Task<bool> HasUserReviewedProductAsync(int productId, int userId)
    {
        return await _context.Reviews
            .AnyAsync(r => r.ProductId == productId && r.UserId == userId);
    }

    public async Task<Review?> GetUserReviewForProductAsync(int productId, int userId)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
    }

    public async Task<List<ReviewDto>> GetUserReviewsAsync(int userId, int page = 1, int pageSize = 10)
    {
        return await _context.Reviews
            .Include(r => r.Product)
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                ProductId = r.ProductId,
                UserId = r.UserId,
                UserName = "", // Not needed for user's own reviews
                UserEmail = "",
                Rating = r.Rating,
                Title = r.Title,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                IsVerifiedPurchase = r.IsVerifiedPurchase,
                CanEdit = true,
                CanDelete = true
            })
            .ToListAsync();
    }

    public async Task<bool> ValidateReviewOwnershipAsync(int reviewId, int userId)
    {
        return await _context.Reviews
            .AnyAsync(r => r.Id == reviewId && r.UserId == userId);
    }

    private async Task<bool> HasVerifiedPurchaseAsync(int productId, int userId)
    {
        // Check if user has successfully purchased this product
        return await _context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Delivered)
            .SelectMany(o => o.OrderItems)
            .AnyAsync(item => item.ProductId == productId);
    }
}