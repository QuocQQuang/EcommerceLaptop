'use client';

import { useAuth } from '@/hooks/useAuth';
import { reviewService } from '@/services/reviewService';
import {
    CreateReviewRequest,
    Review,
    ReviewFilterParams,
    ReviewSummary as ReviewSummaryType,
    ReviewsPagedResponse,
    UpdateReviewRequest
} from '@/types/review';
import { ChevronDown, Filter, MessageSquare, Plus } from 'lucide-react';
import React, { useEffect, useState } from 'react';
import { ReviewForm } from './ReviewForm';
import { ReviewItem } from './ReviewItem';
import { ReviewSummary } from './ReviewSummary';

interface ReviewListProps {
    productId: number;
    className?: string;
}

export const ReviewList: React.FC<ReviewListProps> = ({
    productId,
    className = ''
}) => {
    const { user, isAuthenticated } = useAuth();

    // State
    const [summary, setSummary] = useState<ReviewSummaryType | null>(null);
    const [reviews, setReviews] = useState<ReviewsPagedResponse | null>(null);
    const [loading, setLoading] = useState(true);
    const [submitting, setSubmitting] = useState(false);
    const [showReviewForm, setShowReviewForm] = useState(false);
    const [editingReview, setEditingReview] = useState<Review | null>(null);

    // Filter state
    const [filters, setFilters] = useState<ReviewFilterParams>({
        page: 1,
        pageSize: 10,
        sortBy: 'CreatedAt',
        sortOrder: 'DESC'
    });
    const [showFilters, setShowFilters] = useState(false);

    // Load data
    useEffect(() => {
        loadReviewData();
    }, [productId, filters]);

    const loadReviewData = async () => {
        try {
            setLoading(true);
            const [summaryData, reviewsData] = await Promise.all([
                reviewService.getReviewSummary(productId),
                reviewService.getProductReviews(productId, filters)
            ]);

            setSummary(summaryData);
            setReviews(reviewsData);
        } catch (error) {
            console.error('Error loading review data:', error);
        } finally {
            setLoading(false);
        }
    };

    const handleCreateReview = async (data: CreateReviewRequest) => {
        try {
            setSubmitting(true);
            await reviewService.createReview(data);
            await loadReviewData(); // Reload data
        } catch (error) {
            console.error('Error creating review:', error);
            throw error;
        } finally {
            setSubmitting(false);
        }
    };

    const handleUpdateReview = async (data: UpdateReviewRequest) => {
        if (!editingReview) return;

        try {
            setSubmitting(true);
            await reviewService.updateReview(editingReview.id, data);
            await loadReviewData(); // Reload data
            setEditingReview(null);
        } catch (error) {
            console.error('Error updating review:', error);
            throw error;
        } finally {
            setSubmitting(false);
        }
    };

    const handleDeleteReview = async (reviewId: number) => {
        try {
            await reviewService.deleteReview(reviewId);
            await loadReviewData(); // Reload data
        } catch (error) {
            console.error('Error deleting review:', error);
        }
    };

    const handleFilterByRating = (rating: number | null) => {
        setFilters(prev => ({
            ...prev,
            rating: rating || undefined,
            page: 1
        }));
    };

    const handleSortChange = (sortBy: string, sortOrder: string) => {
        setFilters(prev => ({
            ...prev,
            sortBy: sortBy as any,
            sortOrder: sortOrder as any,
            page: 1
        }));
    };

    const handlePageChange = (page: number) => {
        setFilters(prev => ({ ...prev, page }));
    };

    const openReviewForm = () => {
        if (isAuthenticated) {
            setShowReviewForm(true);
        } else {
            // Redirect to login or show login modal
            alert('Vui lòng đăng nhập để viết đánh giá');
        }
    };

    const openEditForm = (review: Review) => {
        setEditingReview(review);
        setShowReviewForm(true);
    };

    const closeReviewForm = () => {
        setShowReviewForm(false);
        setEditingReview(null);
    };

    if (loading) {
        return (
            <div className={`space-y-6 ${className}`}>
                <div className="animate-pulse">
                    <div className="h-48 bg-gray-200 rounded-lg mb-6"></div>
                    <div className="space-y-4">
                        {[1, 2, 3].map(i => (
                            <div key={i} className="h-32 bg-gray-200 rounded-lg"></div>
                        ))}
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className={`space-y-6 ${className}`}>
            {/* Review Summary */}
            {summary && (
                <ReviewSummary
                    summary={summary}
                    onFilterByRating={handleFilterByRating}
                />
            )}

            {/* Actions Header */}
            <div className="flex items-center justify-between">
                <h3 className="text-lg font-semibold text-gray-900">
                    {reviews?.totalCount || 0} đánh giá
                </h3>

                <div className="flex items-center gap-3">
                    {/* Write Review Button */}
                    {isAuthenticated && summary?.canUserReview && (
                        <button
                            onClick={openReviewForm}
                            className="flex items-center gap-2 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md transition-colors"
                        >
                            <Plus className="w-4 h-4" />
                            Viết đánh giá
                        </button>
                    )}

                    {/* Filter & Sort */}
                    <div className="relative">
                        <button
                            onClick={() => setShowFilters(!showFilters)}
                            className="flex items-center gap-2 px-4 py-2 border border-gray-300 hover:border-gray-400 rounded-md transition-colors"
                        >
                            <Filter className="w-4 h-4" />
                            Lọc & Sắp xếp
                            <ChevronDown className="w-4 h-4" />
                        </button>

                        {showFilters && (
                            <div className="absolute right-0 top-12 bg-white border border-gray-200 rounded-lg shadow-lg p-4 z-10 min-w-[240px]">
                                <div className="space-y-3">
                                    <div>
                                        <label className="block text-sm font-medium text-gray-700 mb-2">
                                            Sắp xếp theo
                                        </label>
                                        <select
                                            value={`${filters.sortBy}-${filters.sortOrder}`}
                                            onChange={(e) => {
                                                const [sortBy, sortOrder] = e.target.value.split('-');
                                                handleSortChange(sortBy, sortOrder);
                                            }}
                                            className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm"
                                        >
                                            <option value="CreatedAt-DESC">Mới nhất</option>
                                            <option value="CreatedAt-ASC">Cũ nhất</option>
                                            <option value="Rating-DESC">Điểm cao nhất</option>
                                            <option value="Rating-ASC">Điểm thấp nhất</option>
                                        </select>
                                    </div>

                                    <div>
                                        <label className="flex items-center gap-2 text-sm">
                                            <input
                                                type="checkbox"
                                                checked={filters.verifiedOnly || false}
                                                onChange={(e) => setFilters(prev => ({
                                                    ...prev,
                                                    verifiedOnly: e.target.checked || undefined,
                                                    page: 1
                                                }))}
                                                className="rounded"
                                            />
                                            Chỉ hiển thị đánh giá đã xác thực
                                        </label>
                                    </div>
                                </div>
                            </div>
                        )}
                    </div>
                </div>
            </div>

            {/* Reviews List */}
            {reviews && reviews.reviews.length > 0 ? (
                <div className="space-y-4">
                    {reviews.reviews.map((review) => (
                        <ReviewItem
                            key={review.id}
                            review={review}
                            onEdit={openEditForm}
                            onDelete={handleDeleteReview}
                        />
                    ))}

                    {/* Pagination */}
                    {reviews.totalPages > 1 && (
                        <div className="flex justify-center gap-2 pt-6">
                            {Array.from({ length: reviews.totalPages }, (_, i) => i + 1).map(page => (
                                <button
                                    key={page}
                                    onClick={() => handlePageChange(page)}
                                    className={`px-3 py-2 rounded-md text-sm transition-colors ${filters.page === page
                                            ? 'bg-blue-600 text-white'
                                            : 'bg-gray-100 hover:bg-gray-200 text-gray-700'
                                        }`}
                                >
                                    {page}
                                </button>
                            ))}
                        </div>
                    )}
                </div>
            ) : (
                <div className="text-center py-12">
                    <MessageSquare className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                    <h3 className="text-lg font-medium text-gray-900 mb-2">
                        Chưa có đánh giá nào
                    </h3>
                    <p className="text-gray-500 mb-4">
                        Hãy là người đầu tiên đánh giá sản phẩm này
                    </p>
                    {isAuthenticated && summary?.canUserReview && (
                        <button
                            onClick={openReviewForm}
                            className="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md transition-colors"
                        >
                            Viết đánh giá đầu tiên
                        </button>
                    )}
                </div>
            )}

            {/* Review Form Modal */}
            <ReviewForm
                productId={productId}
                existingReview={editingReview || undefined}
                isOpen={showReviewForm}
                onClose={closeReviewForm}
                onSubmit={editingReview
                    ? (data) => handleUpdateReview(data as UpdateReviewRequest)
                    : (data) => handleCreateReview(data as CreateReviewRequest)
                }
                isLoading={submitting}
            />

            {/* Click outside to close filters */}
            {showFilters && (
                <div
                    className="fixed inset-0 z-5"
                    onClick={() => setShowFilters(false)}
                />
            )}
        </div>
    );
};