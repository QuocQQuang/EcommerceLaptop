'use client';

import { StarRating } from '@/components/ui/StarRating';
import { reviewService } from '@/services/reviewService';
import { ReviewSummary as ReviewSummaryType } from '@/types/review';
import { Star } from 'lucide-react';
import React from 'react';

interface ReviewSummaryProps {
    summary: ReviewSummaryType;
    onFilterByRating?: (rating: number | null) => void;
    className?: string;
}

export const ReviewSummary: React.FC<ReviewSummaryProps> = ({
    summary,
    onFilterByRating,
    className = ''
}) => {
    const ratingBreakdown = reviewService.formatRatingDistribution(summary.ratingDistribution);

    const handleRatingClick = (rating: number) => {
        onFilterByRating?.(rating);
    };

    const handleShowAllClick = () => {
        onFilterByRating?.(null);
    };

    return (
        <div className={`bg-white rounded-lg border p-6 ${className}`}>
            <div className="flex items-start justify-between mb-6">
                <div>
                    <h3 className="text-xl font-semibold text-gray-900 mb-2">
                        nh gi sn phm
                    </h3>
                    <div className="flex items-center gap-3 mb-2">
                        <div className="text-3xl font-bold text-gray-900">
                            {summary.averageRating.toFixed(1)}
                        </div>
                        <div>
                            <StarRating rating={summary.averageRating} size="lg" showValue={false} />
                            <div className="text-sm text-gray-500 mt-1">
                                {summary.totalReviews} nh gi
                            </div>
                        </div>
                    </div>
                    {summary.verifiedPurchaseCount > 0 && (
                        <div className="text-sm text-green-600">
                            {summary.verifiedPurchaseCount} nh gi t ngi mua  xc thc
                        </div>
                    )}
                </div>
            </div>

            {/* Rating Breakdown */}
            <div className="space-y-2 mb-6">
                <h4 className="font-medium text-gray-900 mb-3">Phn b nh gi</h4>
                {ratingBreakdown.map(({ rating, count, percentage }) => (
                    <div
                        key={rating}
                        className="flex items-center gap-3 text-sm cursor-pointer hover:bg-gray-50 p-2 rounded"
                        onClick={() => handleRatingClick(rating)}
                    >
                        <div className="flex items-center gap-1 w-12">
                            <span className="text-gray-700">{rating}</span>
                            <Star className="w-3 h-3 fill-yellow-400 text-yellow-400" />
                        </div>

                        <div className="flex-1 bg-gray-200 rounded-full h-2 relative overflow-hidden">
                            <div
                                className="bg-yellow-400 h-full rounded-full transition-all duration-300"
                                style={{ width: `${percentage}%` }}
                            />
                        </div>

                        <div className="text-gray-600 w-12 text-right">
                            {count}
                        </div>

                        <div className="text-gray-500 w-12 text-right">
                            {percentage.toFixed(0)}%
                        </div>
                    </div>
                ))}
            </div>

            {/* Action Buttons */}
            <div className="flex gap-2">
                <button
                    onClick={handleShowAllClick}
                    className="px-4 py-2 text-sm bg-gray-100 hover:bg-gray-200 text-gray-700 rounded-md transition-colors"
                >
                    Xem tt c
                </button>
                <button
                    onClick={() => onFilterByRating?.(null)}
                    className="px-4 py-2 text-sm bg-blue-100 hover:bg-blue-200 text-blue-700 rounded-md transition-colors"
                >
                    Ch nh gi  xc thc
                </button>
            </div>
        </div>
    );
};