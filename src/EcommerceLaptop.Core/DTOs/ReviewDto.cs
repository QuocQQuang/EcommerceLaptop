namespace EcommerceLaptop.Core.DTOs;

public class ReviewDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? UserProfilePictureUrl { get; set; }
    public int Rating { get; set; } // 1-5 stars
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public bool CanEdit { get; set; } // Computed property based on current user
    public bool CanDelete { get; set; } // Computed property based on current user/admin
}

public class CreateReviewDto
{
    public int ProductId { get; set; }
    public int Rating { get; set; } // 1-5 stars
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}

public class UpdateReviewDto
{
    public int Rating { get; set; } // 1-5 stars
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}

public class ReviewSummaryDto
{
    public int ProductId { get; set; }
    public int TotalReviews { get; set; }
    public double AverageRating { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new(); // Rating (1-5) -> Count
    public int VerifiedPurchaseCount { get; set; }
}

public class ReviewsPagedDto
{
    public List<ReviewDto> Reviews { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public ReviewSummaryDto Summary { get; set; } = null!;
}

public class ReviewFilterDto
{
    public int? Rating { get; set; } // Filter by specific rating
    public bool? VerifiedPurchaseOnly { get; set; }
    public string SortBy { get; set; } = "CreatedAt"; // CreatedAt, Rating, Helpful
    public string SortOrder { get; set; } = "DESC"; // ASC, DESC
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}