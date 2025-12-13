using MediatR;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Features.Reviews;

public static class ReviewHelpers
{
    public static async Task<bool> HasVerifiedPurchaseAsync(ApplicationDbContext context, int productId, int userId, CancellationToken cancellationToken)
    {
        // Check if user has successfully purchased this product
        return await context.Orders
            .Include(o => o.OrderItems)
            .Where(o => o.UserId == userId && o.Status == OrderStatus.Delivered)
            .SelectMany(o => o.OrderItems)
            .AnyAsync(item => item.ProductId == productId, cancellationToken);
    }
}

// Create Review
public record CreateReviewCommand(CreateReviewDto CreateReviewDto, int UserId) : IRequest<ReviewDto>;

public class CreateReviewHandler : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly ILogger<CreateReviewHandler> _logger;

    public CreateReviewHandler(ApplicationDbContext context, ISender sender, ILogger<CreateReviewHandler> logger)
    {
        _context = context;
        _sender = sender;
        _logger = logger;
    }

    public async Task<ReviewDto> Handle(CreateReviewCommand command, CancellationToken cancellationToken)
    {
        var dto = command.CreateReviewDto;
        var userId = command.UserId;

        // Check if user already reviewed this product
        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == dto.ProductId && r.UserId == userId, cancellationToken);

        if (existingReview != null)
        {
            throw new InvalidOperationException("User has already reviewed this product");
        }

        // Check if user has purchased this product
        var hasVerifiedPurchase = await ReviewHelpers.HasVerifiedPurchaseAsync(_context, dto.ProductId, userId, cancellationToken);

        var review = new Review
        {
            ProductId = dto.ProductId,
            UserId = userId,
            Rating = dto.Rating,
            Title = dto.Title,
            Comment = dto.Comment,
            CreatedAt = DateTime.UtcNow,
            IsVerifiedPurchase = hasVerifiedPurchase
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync(cancellationToken);

        var result = await _sender.Send(new GetReviewByIdQuery(review.Id, userId), cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to create review");
    }
}

// Update Review
public record UpdateReviewCommand(int ReviewId, UpdateReviewDto UpdateReviewDto, int UserId) : IRequest<ReviewDto>;

public class UpdateReviewHandler : IRequestHandler<UpdateReviewCommand, ReviewDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ISender _sender;
    private readonly ILogger<UpdateReviewHandler> _logger;

    public UpdateReviewHandler(ApplicationDbContext context, ISender sender, ILogger<UpdateReviewHandler> logger)
    {
        _context = context;
        _sender = sender;
        _logger = logger;
    }

    public async Task<ReviewDto> Handle(UpdateReviewCommand command, CancellationToken cancellationToken)
    {
        var reviewId = command.ReviewId;
        var dto = command.UpdateReviewDto;
        var userId = command.UserId;

        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);

        if (review == null)
        {
            throw new InvalidOperationException("Review not found");
        }

        if (review.UserId != userId)
        {
            throw new UnauthorizedAccessException("User can only update their own reviews");
        }

        review.Rating = dto.Rating;
        review.Title = dto.Title;
        review.Comment = dto.Comment;

        await _context.SaveChangesAsync(cancellationToken);

        var result = await _sender.Send(new GetReviewByIdQuery(reviewId, userId), cancellationToken);
        return result ?? throw new InvalidOperationException("Failed to update review");
    }
}

// Delete Review
public record DeleteReviewCommand(int ReviewId, int UserId, bool IsAdmin = false) : IRequest<bool>;

public class DeleteReviewHandler : IRequestHandler<DeleteReviewCommand, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DeleteReviewHandler> _logger;

    public DeleteReviewHandler(ApplicationDbContext context, ILogger<DeleteReviewHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteReviewCommand command, CancellationToken cancellationToken)
    {
        var review = await _context.Reviews.FirstOrDefaultAsync(r => r.Id == command.ReviewId, cancellationToken);

        if (review == null) return false;

        if (!command.IsAdmin && review.UserId != command.UserId)
        {
            throw new UnauthorizedAccessException("User can only delete their own reviews");
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
