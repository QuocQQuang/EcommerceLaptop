using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.API.Controllers;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Interfaces.Services;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Reviews Controller - Manages product reviews and ratings
/// Implements CRUD operations following RESTful API design principles
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class ReviewsController(IReviewService reviewService, ILogger<ReviewsController> logger)
    : BaseApiController(logger)
{
    private readonly IReviewService _reviewService = reviewService;

    /// <summary>
    /// Get paginated reviews for a specific product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="rating">Filter by rating (1-5)</param>
    /// <param name="verifiedOnly">Show only verified purchase reviews</param>
    /// <param name="sortBy">Sort by: CreatedAt, Rating</param>
    /// <param name="sortOrder">Sort order: ASC, DESC</param>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10)</param>
    [HttpGet("product/{productId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ReviewsPagedDto>> GetProductReviews(
        int productId,
        [FromQuery] int? rating = null,
        [FromQuery] bool? verifiedOnly = null,
        [FromQuery] string sortBy = "CreatedAt",
        [FromQuery] string sortOrder = "DESC",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var filter = new ReviewFilterDto
        {
            Rating = rating,
            VerifiedPurchaseOnly = verifiedOnly,
            SortBy = sortBy,
            SortOrder = sortOrder,
            Page = page,
            PageSize = Math.Min(pageSize, 50) // Limit max page size
        };

        var result = await _reviewService.GetProductReviewsAsync(productId, filter);

        // Set permission flags based on current user
        var currentUserId = GetCurrentUserId();
        var isAdmin = IsAdmin();

        foreach (var review in result.Reviews)
        {
            review.CanEdit = currentUserId.HasValue && review.UserId == currentUserId.Value;
            review.CanDelete = currentUserId.HasValue && (review.UserId == currentUserId.Value || isAdmin);
        }

        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Get review summary statistics for a product
    /// </summary>
    [HttpGet("product/{productId}/summary")]
    [AllowAnonymous]
    public async Task<ActionResult<ReviewSummaryDto>> GetProductReviewSummary(int productId)
    {
        var summary = await _reviewService.GetProductReviewSummaryAsync(productId);
        return Ok(new { success = true, data = summary });
    }

    /// <summary>
    /// Get a specific review by ID
    /// </summary>
    [HttpGet("{reviewId}")]
    [AllowAnonymous]
    public async Task<ActionResult<ReviewDto>> GetReview(int reviewId)
    {
        var currentUserId = GetCurrentUserId();
        var review = await _reviewService.GetReviewByIdAsync(reviewId, currentUserId);

        if (review == null)
        {
            return NotFound(new { success = false, message = "Review not found" });
        }

        return Ok(new { success = true, data = review });
    }

    /// <summary>
    /// Create a new review for a product
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> CreateReview([FromBody] CreateReviewDto createReviewDto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { success = false, message = "User not authenticated" });
        }

        // Validate rating range
        if (createReviewDto.Rating < 1 || createReviewDto.Rating > 5)
        {
            return BadRequest(new { success = false, message = "Rating must be between 1 and 5" });
        }

        // Check if user can review this product
        var canReview = await _reviewService.CanUserReviewProductAsync(createReviewDto.ProductId, currentUserId.Value);
        if (!canReview)
        {
            return BadRequest(new { success = false, message = "You have already reviewed this product or product does not exist" });
        }

        var review = await _reviewService.CreateReviewAsync(createReviewDto, currentUserId.Value);

        _logger.LogInformation("User {UserId} created review for product {ProductId}", currentUserId.Value, createReviewDto.ProductId);

        return CreatedAtAction(nameof(GetReview), new { reviewId = review.Id },
            new { success = true, data = review });
    }

    /// <summary>
    /// Update an existing review
    /// </summary>
    [HttpPut("{reviewId}")]
    [Authorize]
    public async Task<ActionResult<ReviewDto>> UpdateReview(int reviewId, [FromBody] UpdateReviewDto updateReviewDto)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { success = false, message = "User not authenticated" });
        }

        // Validate rating range
        if (updateReviewDto.Rating < 1 || updateReviewDto.Rating > 5)
        {
            return BadRequest(new { success = false, message = "Rating must be between 1 and 5" });
        }

        var review = await _reviewService.UpdateReviewAsync(reviewId, updateReviewDto, currentUserId.Value);

        _logger.LogInformation("User {UserId} updated review {ReviewId}", currentUserId.Value, reviewId);

        return Ok(new { success = true, data = review });
    }

    /// <summary>
    /// Delete a review
    /// </summary>
    [HttpDelete("{reviewId}")]
    [Authorize]
    public async Task<ActionResult> DeleteReview(int reviewId)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { success = false, message = "User not authenticated" });
        }

        var isAdmin = IsAdmin();
        var deleted = await _reviewService.DeleteReviewAsync(reviewId, currentUserId.Value, isAdmin);

        if (!deleted)
        {
            return NotFound(new { success = false, message = "Review not found" });
        }

        _logger.LogInformation("User {UserId} deleted review {ReviewId}", currentUserId.Value, reviewId);

        return Ok(new { success = true, message = "Review deleted successfully" });
    }

    /// <summary>
    /// Get current user's reviews
    /// </summary>
    [HttpGet("my-reviews")]
    [Authorize]
    public async Task<ActionResult<List<ReviewDto>>> GetMyReviews(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { success = false, message = "User not authenticated" });
        }

        var reviews = await _reviewService.GetUserReviewsAsync(currentUserId.Value, page, Math.Min(pageSize, 50));

        return Ok(new { success = true, data = reviews });
    }

    /// <summary>
    /// Check if current user can review a specific product
    /// </summary>
    [HttpGet("can-review/{productId}")]
    [Authorize]
    public async Task<ActionResult<object>> CanReviewProduct(int productId)
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
        {
            return Unauthorized(new { success = false, message = "User not authenticated" });
        }

        var canReview = await _reviewService.CanUserReviewProductAsync(productId, currentUserId.Value);
        var hasReviewed = await _reviewService.HasUserReviewedProductAsync(productId, currentUserId.Value);

        var result = new
        {
            canReview = canReview,
            hasReviewed = hasReviewed,
            existingReview = hasReviewed ? await _reviewService.GetUserReviewForProductAsync(productId, currentUserId.Value) : null
        };

        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Check if current user is admin
    /// </summary>
    private bool IsAdmin()
    {
        return GetCurrentUserRoles().Any(role => role.Equals("Admin", StringComparison.OrdinalIgnoreCase));
    }
}