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

    public async Task<ReviewsPagedDto> GetProductReviewsAsync(int productId, ReviewFilterDto filter, int? currentUserId = null, bool isAdmin = false)
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
            .Select(r => new ReviewDto(
                r.Id,
                r.ProductId,
                r.UserId,
                $"{r.User.FirstName} {r.User.LastName}",
                r.User.Email,
                r.User.ProfilePictureUrl,
                r.Rating,
                r.Title,
                r.Comment,
                r.CreatedAt,
                r.IsVerifiedPurchase,
                currentUserId.HasValue && (r.UserId == currentUserId.Value),
                currentUserId.HasValue && (r.UserId == currentUserId.Value || isAdmin)
            ))
            .ToListAsync();

        var summary = await GetProductReviewSummaryAsync(productId);

        return new ReviewsPagedDto(
            reviews,
            totalCount,
            filter.Page,
            filter.PageSize,
            totalPages,
            summary
        );
    }

    public async Task<ReviewDto?> GetReviewByIdAsync(int reviewId, int? currentUserId = null)
    {
        var review = await _context.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null) return null;

        return new ReviewDto(
            review.Id,
            review.ProductId,
            review.UserId,
            $"{review.User.FirstName} {review.User.LastName}",
            review.User.Email,
            review.User.ProfilePictureUrl,
            review.Rating,
            review.Title,
            review.Comment,
            review.CreatedAt,
            review.IsVerifiedPurchase,
            currentUserId.HasValue && (review.UserId == currentUserId.Value),
            currentUserId.HasValue && (review.UserId == currentUserId.Value)
        );
    }

    public async Task<ReviewSummaryDto> GetProductReviewSummaryAsync(int productId)
    {
        // N+1 Fix: compute all aggregates DB-side with a single GroupBy query
        // instead of loading all reviews into memory
        var ratingGroups = await _context.Reviews
            .Where(r => r.ProductId == productId)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync();

        var totalReviews = ratingGroups.Sum(g => g.Count);
        var averageRating = totalReviews > 0
            ? (double)ratingGroups.Sum(g => g.Rating * g.Count) / totalReviews
            : 0;

        var ratingDistribution = new Dictionary<int, int>();
        for (int i = 1; i <= 5; i++)
        {
            ratingDistribution[i] = ratingGroups.FirstOrDefault(g => g.Rating == i)?.Count ?? 0;
        }

        // Separate aggregate for verified purchase count
        var verifiedPurchaseCount = await _context.Reviews
            .CountAsync(r => r.ProductId == productId && r.IsVerifiedPurchase);

        return new ReviewSummaryDto(
            productId,
            totalReviews,
            Math.Round(averageRating, 1),
            ratingDistribution,
            verifiedPurchaseCount
        );
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
            .Select(r => new ReviewDto(
                r.Id,
                r.ProductId,
                r.UserId,
                "", // UserName (not needed)
                "", // UserEmail
                null, // ProfilePictureUrl
                r.Rating,
                r.Title,
                r.Comment,
                r.CreatedAt,
                r.IsVerifiedPurchase,
                true, // CanEdit
                true  // CanDelete
            ))
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