namespace EcommerceLaptop.Core.DTOs;

public record ReviewDto(
    int Id,
    int ProductId,
    int UserId,
    string UserName,
    string UserEmail,
    string? UserProfilePictureUrl,
    int Rating,
    string Title,
    string Comment,
    DateTime CreatedAt,
    bool IsVerifiedPurchase,
    bool CanEdit,
    bool CanDelete);

public record CreateReviewDto(
    int ProductId,
    int Rating,
    string Title,
    string Comment);

public record UpdateReviewDto(
    int Rating,
    string Title,
    string Comment);

public record ReviewSummaryDto(
    int ProductId,
    int TotalReviews,
    double AverageRating,
    Dictionary<int, int> RatingDistribution,
    int VerifiedPurchaseCount);

public record ReviewsPagedDto(
    List<ReviewDto> Reviews,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    ReviewSummaryDto Summary);

public record ReviewFilterDto(
    int? Rating = null,
    bool? VerifiedPurchaseOnly = null,
    string SortBy = "CreatedAt",
    string SortOrder = "DESC",
    int Page = 1,
    int PageSize = 10);