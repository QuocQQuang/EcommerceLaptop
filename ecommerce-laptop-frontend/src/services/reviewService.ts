import apiClient from '@/lib/api';
import {
    CreateReviewRequest,
    Review,
    ReviewFilterParams,
    ReviewSummary,
    ReviewsPagedResponse,
    UpdateReviewRequest
} from '@/types/review';

export const reviewService = {
    // Get reviews for a product with pagination and filtering
    async getProductReviews(productId: number, filters?: ReviewFilterParams): Promise<ReviewsPagedResponse> {
        const params = new URLSearchParams();

        if (filters?.rating) params.append('rating', filters.rating.toString());
        if (filters?.verifiedOnly !== undefined) params.append('verifiedOnly', filters.verifiedOnly.toString());
        if (filters?.sortBy) params.append('sortBy', filters.sortBy);
        if (filters?.sortOrder) params.append('sortOrder', filters.sortOrder);
        if (filters?.page) params.append('page', filters.page.toString());
        if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString());

        const { data } = await apiClient.get(`/reviews/product/${productId}?${params.toString()}`);
        return data.data;
    },

    // Get review summary for a product
    async getReviewSummary(productId: number): Promise<ReviewSummary> {
        const { data } = await apiClient.get(`/reviews/product/${productId}/summary`);
        return data.data;
    },

    // Get a specific review by ID
    async getReviewById(reviewId: number): Promise<Review> {
        const { data } = await apiClient.get(`/reviews/${reviewId}`);
        return data.data;
    },

    // Create a new review (requires authentication)
    async createReview(reviewData: CreateReviewRequest): Promise<Review> {
        const { data } = await apiClient.post('/reviews', reviewData);
        return data.data;
    },

    // Update an existing review (requires authentication and ownership)
    async updateReview(reviewId: number, reviewData: UpdateReviewRequest): Promise<Review> {
        const { data } = await apiClient.put(`/reviews/${reviewId}`, reviewData);
        return data.data;
    },

    // Delete a review (requires authentication and ownership or admin)
    async deleteReview(reviewId: number): Promise<void> {
        await apiClient.delete(`/reviews/${reviewId}`);
    },

    // Get current user's reviews
    async getUserReviews(page: number = 1, pageSize: number = 10): Promise<ReviewsPagedResponse> {
        const { data } = await apiClient.get(`/reviews/my-reviews?page=${page}&pageSize=${pageSize}`);
        // Backend returns a simple array of ReviewDto, so we need to structure it as a paginated response
        const reviews = data.data || [];
        return {
            reviews: reviews,
            totalCount: reviews.length,
            page: page,
            pageSize: pageSize,
            totalPages: Math.ceil(reviews.length / pageSize)
        };
    },

    // Check if user can review a product
    async canUserReviewProduct(productId: number): Promise<boolean> {
        // Backend endpoint returns { canReview, hasReviewed, existingReview } inside data.data
        const { data } = await apiClient.get(`/reviews/can-review/${productId}`);
        const payload = data.data;
        if (payload && typeof payload.canReview === 'boolean') {
            return payload.canReview;
        }

        // Fallback: if API returns a plain boolean or unexpected shape, coerce truthiness
        return Boolean(payload);
    },

    // Get user's review for a specific product (if exists)
    async getUserReviewForProduct(productId: number): Promise<Review | null> {
        try {
            const { data } = await apiClient.get(`/reviews/product/${productId}/user-review`);
            return data.data;
        } catch (error: any) {
            if (error.response?.status === 404) {
                return null;
            }
            throw error;
        }
    },

    // Helper function to get star rating display
    getStarRating(rating: number): { fullStars: number; halfStar: boolean; emptyStars: number } {
        const fullStars = Math.floor(rating);
        const halfStar = rating % 1 >= 0.5;
        const emptyStars = 5 - fullStars - (halfStar ? 1 : 0);

        return { fullStars, halfStar, emptyStars };
    },

    // Helper function to format rating distribution for display
    formatRatingDistribution(distribution: Record<number, number>): Array<{ rating: number; count: number; percentage: number }> {
        const totalReviews = Object.values(distribution).reduce((sum, count) => sum + count, 0);

        return [5, 4, 3, 2, 1].map(rating => ({
            rating,
            count: distribution[rating] || 0,
            percentage: totalReviews > 0 ? ((distribution[rating] || 0) / totalReviews) * 100 : 0
        }));
    }
};