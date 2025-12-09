'use client';

import { StarRating } from '@/components/ui/StarRating';
import { CreateReviewRequest, Review, UpdateReviewRequest } from '@/types/review';
import { X } from 'lucide-react';
import React, { useEffect, useState } from 'react';

interface ReviewFormProps {
    productId: number;
    existingReview?: Review;
    isOpen: boolean;
    onClose: () => void;
    onSubmit: (data: CreateReviewRequest | UpdateReviewRequest) => Promise<void>;
    isLoading?: boolean;
}

export const ReviewForm: React.FC<ReviewFormProps> = ({
    productId,
    existingReview,
    isOpen,
    onClose,
    onSubmit,
    isLoading = false
}) => {
    const [rating, setRating] = useState<number>(existingReview?.rating || 0);
    const [title, setTitle] = useState<string>(existingReview?.title || '');
    const [comment, setComment] = useState<string>(existingReview?.comment || '');
    const [errors, setErrors] = useState<Record<string, string>>({});

    const isEditMode = !!existingReview;

    useEffect(() => {
        if (existingReview) {
            setRating(existingReview.rating);
            setTitle(existingReview.title);
            setComment(existingReview.comment);
        } else {
            setRating(0);
            setTitle('');
            setComment('');
        }
        setErrors({});
    }, [existingReview, isOpen]);

    const validateForm = (): boolean => {
        const newErrors: Record<string, string> = {};

        if (rating === 0) {
            newErrors.rating = 'Vui lng chn s sao nh gi';
        }
        if (title.trim().length < 5) {
            newErrors.title = 'Tiu  phi c t nht 5 k t';
        }
        if (comment.trim().length < 10) {
            newErrors.comment = 'Ni dung nh gi phi c t nht 10 k t';
        }

        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!validateForm()) {
            return;
        }

        try {
            const reviewData = {
                rating,
                title: title.trim(),
                comment: comment.trim(),
                ...(isEditMode ? {} : { productId })
            };

            await onSubmit(reviewData);
            onClose();
        } catch (error) {
            console.error('Error submitting review:', error);
        }
    };

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 bg-black bg-opacity-50 flex items-center justify-center z-50 p-4">
            <div className="bg-white rounded-lg shadow-xl max-w-md w-full max-h-[90vh] overflow-y-auto">
                {/* Header */}
                <div className="flex items-center justify-between p-6 border-b">
                    <h2 className="text-xl font-semibold text-gray-900">
                        {isEditMode ? 'Chnh sa nh gi' : 'Vit nh gi'}
                    </h2>
                    <button
                        onClick={onClose}
                        className="text-gray-500 hover:text-gray-700 transition-colors"
                        disabled={isLoading}
                    >
                        <X className="w-6 h-6" />
                    </button>
                </div>

                {/* Form */}
                <form onSubmit={handleSubmit} className="p-6 space-y-6">
                    {/* Rating */}
                    <div>
                        <label className="block text-sm font-medium text-gray-700 mb-2">
                            nh gi sao *
                        </label>
                        <StarRating
                            rating={rating}
                            interactive
                            onRatingChange={setRating}
                            size="lg"
                            className="mb-2"
                        />
                        {errors.rating && (
                            <p className="text-red-600 text-sm">{errors.rating}</p>
                        )}
                    </div>

                    {/* Title */}
                    <div>
                        <label htmlFor="title" className="block text-sm font-medium text-gray-700 mb-2">
                            Tiu  nh gi *
                        </label>
                        <input
                            id="title"
                            type="text"
                            value={title}
                            onChange={(e) => setTitle(e.target.value)}
                            placeholder="Nhp tiu  cho nh gi ca bn..."
                            className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 ${errors.title ? 'border-red-500' : 'border-gray-300'
                                }`}
                            disabled={isLoading}
                            maxLength={100}
                        />
                        <div className="flex justify-between items-center mt-1">
                            {errors.title && (
                                <p className="text-red-600 text-sm">{errors.title}</p>
                            )}
                            <span className="text-gray-500 text-sm ml-auto">
                                {title.length}/100
                            </span>
                        </div>
                    </div>

                    {/* Comment */}
                    <div>
                        <label htmlFor="comment" className="block text-sm font-medium text-gray-700 mb-2">
                            Ni dung nh gi *
                        </label>
                        <textarea
                            id="comment"
                            value={comment}
                            onChange={(e) => setComment(e.target.value)}
                            placeholder="Chia s tri nghim ca bn v sn phm ny..."
                            rows={4}
                            className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none ${errors.comment ? 'border-red-500' : 'border-gray-300'
                                }`}
                            disabled={isLoading}
                            maxLength={1000}
                        />
                        <div className="flex justify-between items-center mt-1">
                            {errors.comment && (
                                <p className="text-red-600 text-sm">{errors.comment}</p>
                            )}
                            <span className="text-gray-500 text-sm ml-auto">
                                {comment.length}/1000
                            </span>
                        </div>
                    </div>

                    {/* Actions */}
                    <div className="flex gap-3 pt-4">
                        <button
                            type="button"
                            onClick={onClose}
                            className="flex-1 px-4 py-2 text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-md transition-colors"
                            disabled={isLoading}
                        >
                            Hy
                        </button>
                        <button
                            type="submit"
                            className="flex-1 px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-md transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                            disabled={isLoading}
                        >
                            {isLoading ? 'ang x l...' : (isEditMode ? 'Cp nht' : 'ng nh gi')}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};