using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.API.Features.Reviews;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Reviews Controller - Manages product reviews and ratings
/// Implements CRUD operations following RESTful API design principles
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class ReviewsController(ISender sender, ILogger<ReviewsController> logger)
    : BaseApiController(logger)
{
    private readonly ISender _sender = sender;

    /// <summary>
    /// Get paginated reviews for a specific product
    /// </summary>
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

        var currentUserId = GetCurrentUserId();
        var isAdmin = IsAdmin();

        var result = await _sender.Send(new GetProductReviewsQuery(productId, filter, currentUserId, isAdmin));

        return Ok(new { success = true, data = result });
    }

    /// <summary>
    /// Get review summary statistics for a product
    /// </summary>
    [HttpGet("product/{productId}/summary")]
    [AllowAnonymous]
    public async Task<ActionResult<ReviewSummaryDto>> GetProductReviewSummary(int productId)
    {
        var summary = await _sender.Send(new GetProductReviewSummaryQuery(productId));
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
        var review = await _sender.Send(new GetReviewByIdQuery(reviewId, currentUserId));

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
        var canReview = await _sender.Send(new CanUserReviewProductQuery(createReviewDto.ProductId, currentUserId.Value));
        if (!canReview)
        {
            return BadRequest(new { success = false, message = "You have already reviewed this product or product does not exist" });
        }

        try 
        {
            var review = await _sender.Send(new CreateReviewCommand(createReviewDto, currentUserId.Value));

            _logger.LogInformation("User {UserId} created review for product {ProductId}", currentUserId.Value, createReviewDto.ProductId);

            return CreatedAtAction(nameof(GetReview), new { reviewId = review.Id },
                new { success = true, data = review });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
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

        try
        {
            var review = await _sender.Send(new UpdateReviewCommand(reviewId, updateReviewDto, currentUserId.Value));

            _logger.LogInformation("User {UserId} updated review {ReviewId}", currentUserId.Value, reviewId);

            return Ok(new { success = true, data = review });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
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
        try
        {
            var deleted = await _sender.Send(new DeleteReviewCommand(reviewId, currentUserId.Value, isAdmin));

            if (!deleted)
            {
                return NotFound(new { success = false, message = "Review not found" });
            }

            _logger.LogInformation("User {UserId} deleted review {ReviewId}", currentUserId.Value, reviewId);

            return Ok(new { success = true, message = "Review deleted successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
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

        var reviews = await _sender.Send(new GetUserReviewsQuery(currentUserId.Value, page, Math.Min(pageSize, 50)));

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

        var canReview = await _sender.Send(new CanUserReviewProductQuery(productId, currentUserId.Value));
        var hasReviewed = await _sender.Send(new HasUserReviewedProductQuery(productId, currentUserId.Value));
        
        // We need GetUserReviewForProductQuery to get the review if it exists
        var existingReview = hasReviewed ? await _sender.Send(new GetUserReviewForProductQuery(productId, currentUserId.Value)) : null;

        var result = new
        {
            canReview = canReview,
            hasReviewed = hasReviewed,
            existingReview = existingReview
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