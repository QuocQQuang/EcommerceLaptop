'use client';

import { StarRating } from '@/components/ui/StarRating';
import { Review } from '@/types/review';
import { formatDistanceToNow } from 'date-fns';
import { vi } from 'date-fns/locale';
import { Edit, MoreVertical, Shield, Trash2, User } from 'lucide-react';
import React, { useState } from 'react';

interface ReviewItemProps {
    review: Review;
    onEdit?: (review: Review) => void;
    onDelete?: (reviewId: number) => void;
    className?: string;
}

export const ReviewItem: React.FC<ReviewItemProps> = ({
    review,
    onEdit,
    onDelete,
    className = ''
}) => {
    const [showMenu, setShowMenu] = useState(false);

    const handleEdit = () => {
        onEdit?.(review);
        setShowMenu(false);
    };

    const handleDelete = () => {
        if (window.confirm('Bạn có chắc chắn muốn xóa đánh giá này?')) {
            onDelete?.(review.id);
        }
        setShowMenu(false);
    };

    const formatDate = (dateString: string) => {
        return formatDistanceToNow(new Date(dateString), {
            addSuffix: true,
            locale: vi
        });
    };

    return (
        <div className={`bg-white border rounded-lg p-6 ${className}`}>
            {/* Header */}
            <div className="flex items-start justify-between mb-4">
                <div className="flex items-start gap-3">
                    {/* User Avatar */}
                    <div className="w-10 h-10 bg-gray-200 rounded-full flex items-center justify-center overflow-hidden">
                        {review.userProfilePictureUrl ? (
                            <img
                                src={review.userProfilePictureUrl}
                                alt={review.userName}
                                className="w-full h-full object-cover"
                            />
                        ) : (
                            <User className="w-5 h-5 text-gray-500" />
                        )}
                    </div>

                    {/* User Info */}
                    <div>
                        <div className="flex items-center gap-2 mb-1">
                            <h4 className="font-medium text-gray-900">
                                {review.userName}
                            </h4>
                            {review.isVerifiedPurchase && (
                                <div className="flex items-center gap-1 bg-green-100 text-green-800 px-2 py-1 rounded-full text-xs">
                                    <Shield className="w-3 h-3" />
                                    <span>Đã mua hàng</span>
                                </div>
                            )}
                        </div>
                        <div className="flex items-center gap-2 text-sm text-gray-500">
                            <StarRating rating={review.rating} size="sm" />
                            <span></span>
                            <span>{formatDate(review.createdAt)}</span>
                        </div>
                    </div>
                </div>

                {/* Actions Menu */}
                {(review.canEdit || review.canDelete) && (
                    <div className="relative">
                        <button
                            onClick={() => setShowMenu(!showMenu)}
                            className="p-1 hover:bg-gray-100 rounded-full transition-colors"
                        >
                            <MoreVertical className="w-4 h-4 text-gray-500" />
                        </button>

                        {showMenu && (
                            <div className="absolute right-0 top-8 bg-white border rounded-lg shadow-lg py-1 z-10 min-w-[120px]">
                                {review.canEdit && (
                                    <button
                                        onClick={handleEdit}
                                        className="flex items-center gap-2 px-3 py-2 text-sm text-gray-700 hover:bg-gray-50 w-full text-left"
                                    >
                                        <Edit className="w-4 h-4" />
                                        Chỉnh sửa
                                    </button>
                                )}
                                {review.canDelete && (
                                    <button
                                        onClick={handleDelete}
                                        className="flex items-center gap-2 px-3 py-2 text-sm text-red-600 hover:bg-red-50 w-full text-left"
                                    >
                                        <Trash2 className="w-4 h-4" />
                                        Xóa
                                    </button>
                                )}
                            </div>
                        )}
                    </div>
                )}
            </div>

            {/* Review Content */}
            <div className="space-y-3">
                {review.title && (
                    <h5 className="font-medium text-gray-900">
                        {review.title}
                    </h5>
                )}

                {review.comment && (
                    <p className="text-gray-700 leading-relaxed">
                        {review.comment}
                    </p>
                )}
            </div>

            {/* Click outside to close menu */}
            {showMenu && (
                <div
                    className="fixed inset-0 z-5"
                    onClick={() => setShowMenu(false)}
                />
            )}
        </div>
    );
};