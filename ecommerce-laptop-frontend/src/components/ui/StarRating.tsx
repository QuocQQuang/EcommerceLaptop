'use client';

import { Star } from 'lucide-react';
import React, { useState } from 'react';

interface StarRatingProps {
    rating: number;
    maxRating?: number;
    size?: 'sm' | 'md' | 'lg';
    interactive?: boolean;
    onRatingChange?: (rating: number) => void;
    showValue?: boolean;
    className?: string;
}

export const StarRating: React.FC<StarRatingProps> = ({
    rating,
    maxRating = 5,
    size = 'md',
    interactive = false,
    onRatingChange,
    showValue = false,
    className = ''
}) => {
    const [hoverRating, setHoverRating] = useState<number>(0);
    const [isHovering, setIsHovering] = useState(false);

    const sizeClasses = {
        sm: 'w-4 h-4',
        md: 'w-5 h-5',
        lg: 'w-6 h-6'
    };

    const handleMouseEnter = (starIndex: number) => {
        if (interactive) {
            setHoverRating(starIndex);
            setIsHovering(true);
        }
    };

    const handleMouseLeave = () => {
        if (interactive) {
            setHoverRating(0);
            setIsHovering(false);
        }
    };

    const handleClick = (starIndex: number) => {
        if (interactive && onRatingChange) {
            onRatingChange(starIndex);
        }
    };

    const getStarFill = (starIndex: number) => {
        const currentRating = isHovering ? hoverRating : rating;

        if (starIndex <= currentRating) {
            return 'fill-yellow-400 text-yellow-400';
        } else if (starIndex - 0.5 <= currentRating) {
            return 'fill-yellow-400/50 text-yellow-400';
        } else {
            return 'fill-gray-200 text-gray-200';
        }
    };

    return (
        <div className={`flex items-center gap-1 ${className}`}>
            <div className="flex items-center">
                {Array.from({ length: maxRating }, (_, index) => {
                    const starIndex = index + 1;
                    return (
                        <Star
                            key={starIndex}
                            className={`
                                ${sizeClasses[size]} 
                                ${getStarFill(starIndex)}
                                ${interactive ? 'cursor-pointer hover:scale-110 transition-transform' : ''}
                            `}
                            onMouseEnter={() => handleMouseEnter(starIndex)}
                            onMouseLeave={handleMouseLeave}
                            onClick={() => handleClick(starIndex)}
                        />
                    );
                })}
            </div>
            {showValue && (
                <span className="text-sm text-gray-600 ml-1">
                    {isHovering ? hoverRating : rating.toFixed(1)}
                </span>
            )}
        </div>
    );
};