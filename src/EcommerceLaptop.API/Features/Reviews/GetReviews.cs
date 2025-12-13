using MediatR;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Features.Reviews;

// Get Product Reviews
public record GetProductReviewsQuery(int ProductId, ReviewFilterDto Filter, int? CurrentUserId = null, bool IsAdmin = false) : IRequest<ReviewsPagedDto>;

public class GetProductReviewsHandler : IRequestHandler<GetProductReviewsQuery, ReviewsPagedDto>
{
    private readonly ApplicationDbContext _context;
    private readonly ISender _sender; // To call GetProductReviewSummary

    public GetProductReviewsHandler(ApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<ReviewsPagedDto> Handle(GetProductReviewsQuery request, CancellationToken cancellationToken)
    {
        var filter = request.Filter;
        var query = _context.Reviews
            .Include(r => r.User)
            .Where(r => r.ProductId == request.ProductId);

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

        var totalCount = await query.CountAsync(cancellationToken);
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
                request.CurrentUserId.HasValue && (r.UserId == request.CurrentUserId.Value),
                request.CurrentUserId.HasValue && (r.UserId == request.CurrentUserId.Value || request.IsAdmin)
            ))
            .ToListAsync(cancellationToken);

        var summary = await _sender.Send(new GetProductReviewSummaryQuery(request.ProductId), cancellationToken);

        return new ReviewsPagedDto(
            reviews,
            totalCount,
            filter.Page,
            filter.PageSize,
            totalPages,
            summary
        );
    }
}

// Get Review By Id
public record GetReviewByIdQuery(int ReviewId, int? CurrentUserId = null) : IRequest<ReviewDto?>;

public class GetReviewByIdHandler : IRequestHandler<GetReviewByIdQuery, ReviewDto?>
{
    private readonly ApplicationDbContext _context;

    public GetReviewByIdHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewDto?> Handle(GetReviewByIdQuery request, CancellationToken cancellationToken)
    {
        var review = await _context.Reviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == request.ReviewId, cancellationToken);

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
            request.CurrentUserId.HasValue && (review.UserId == request.CurrentUserId.Value),
            request.CurrentUserId.HasValue && (review.UserId == request.CurrentUserId.Value)
        );
    }
}

// Get Product Review Summary
public record GetProductReviewSummaryQuery(int ProductId) : IRequest<ReviewSummaryDto>;

public class GetProductReviewSummaryHandler : IRequestHandler<GetProductReviewSummaryQuery, ReviewSummaryDto>
{
    private readonly ApplicationDbContext _context;

    public GetProductReviewSummaryHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewSummaryDto> Handle(GetProductReviewSummaryQuery request, CancellationToken cancellationToken)
    {
        var reviews = await _context.Reviews
            .Where(r => r.ProductId == request.ProductId)
            .ToListAsync(cancellationToken);

        var totalReviews = reviews.Count;
        var averageRating = totalReviews > 0 ? reviews.Average(r => r.Rating) : 0;
        var verifiedPurchaseCount = reviews.Count(r => r.IsVerifiedPurchase);

        var ratingDistribution = new Dictionary<int, int>();
        for (int i = 1; i <= 5; i++)
        {
            ratingDistribution[i] = reviews.Count(r => r.Rating == i);
        }

        return new ReviewSummaryDto(
            request.ProductId,
            totalReviews,
            Math.Round(averageRating, 1),
            ratingDistribution,
            verifiedPurchaseCount
        );
    }
}

// Get User Reviews
public record GetUserReviewsQuery(int UserId, int Page = 1, int PageSize = 10) : IRequest<List<ReviewDto>>;

public class GetUserReviewsHandler : IRequestHandler<GetUserReviewsQuery, List<ReviewDto>>
{
    private readonly ApplicationDbContext _context;

    public GetUserReviewsHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ReviewDto>> Handle(GetUserReviewsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Reviews
            .Include(r => r.Product)
            .Where(r => r.UserId == request.UserId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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
            .ToListAsync(cancellationToken);
    }
}

// Can User Review Product
public record CanUserReviewProductQuery(int ProductId, int UserId) : IRequest<bool>;

public class CanUserReviewProductHandler : IRequestHandler<CanUserReviewProductQuery, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly ISender _sender;

    public CanUserReviewProductHandler(ApplicationDbContext context, ISender sender)
    {
        _context = context;
        _sender = sender;
    }

    public async Task<bool> Handle(CanUserReviewProductQuery request, CancellationToken cancellationToken)
    {
        var hasReviewed = await _sender.Send(new HasUserReviewedProductQuery(request.ProductId, request.UserId), cancellationToken);
        var productExists = await _context.Products.AnyAsync(p => p.Id == request.ProductId, cancellationToken);

        return !hasReviewed && productExists;
    }
}

// Has User Reviewed Product
public record HasUserReviewedProductQuery(int ProductId, int UserId) : IRequest<bool>;

public class HasUserReviewedProductHandler : IRequestHandler<HasUserReviewedProductQuery, bool>
{
    private readonly ApplicationDbContext _context;

    public HasUserReviewedProductHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(HasUserReviewedProductQuery request, CancellationToken cancellationToken)
    {
        return await _context.Reviews
            .AnyAsync(r => r.ProductId == request.ProductId && r.UserId == request.UserId, cancellationToken);
    }
}

// Get User Review For Product
public record GetUserReviewForProductQuery(int ProductId, int UserId) : IRequest<Review?>;

public class GetUserReviewForProductHandler : IRequestHandler<GetUserReviewForProductQuery, Review?>
{
    private readonly ApplicationDbContext _context;

    public GetUserReviewForProductHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> Handle(GetUserReviewForProductQuery request, CancellationToken cancellationToken)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.ProductId == request.ProductId && r.UserId == request.UserId, cancellationToken);
    }
}

// Validate Review Ownership
public record ValidateReviewOwnershipQuery(int ReviewId, int UserId) : IRequest<bool>;

public class ValidateReviewOwnershipHandler : IRequestHandler<ValidateReviewOwnershipQuery, bool>
{
    private readonly ApplicationDbContext _context;

    public ValidateReviewOwnershipHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(ValidateReviewOwnershipQuery request, CancellationToken cancellationToken)
    {
        return await _context.Reviews
            .AnyAsync(r => r.Id == request.ReviewId && r.UserId == request.UserId, cancellationToken);
    }
}
