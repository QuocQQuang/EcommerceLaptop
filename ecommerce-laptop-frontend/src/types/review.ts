// Review Types
export interface Review {
    id: number;
    productId: number;
    userId: number;
    userName: string;
    userEmail: string;
    userProfilePictureUrl?: string;
    rating: number; // 1-5 stars
    title: string;
    comment: string;
    createdAt: string;
    isVerifiedPurchase: boolean;
    canEdit?: boolean;
    canDelete?: boolean;
}

export interface CreateReviewRequest {
    productId: number;
    rating: number;
    title: string;
    comment: string;
}

export interface UpdateReviewRequest {
    rating: number;
    title: string;
    comment: string;
}

export interface ReviewSummary {
    productId: number;
    totalReviews: number;
    averageRating: number;
    ratingDistribution: Record<number, number>; // Rating (1-5) -> Count
    verifiedPurchaseCount: number;
    canUserReview?: boolean;
    userReview?: Review;
}

export interface ReviewsPagedResponse {
    reviews: Review[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
}

export interface ReviewFilterParams {
    rating?: number;
    verifiedOnly?: boolean;
    sortBy?: 'CreatedAt' | 'Rating';
    sortOrder?: 'ASC' | 'DESC';
    page?: number;
    pageSize?: number;
}