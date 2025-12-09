import { cn } from '@/lib/utils';
import { Star } from 'lucide-react';

interface RatingProps {
    rating: number;
    maxRating?: number;
    size?: 'xs' | 'sm' | 'md' | 'lg';
    showCount?: boolean;
    reviewCount?: number;
    className?: string;
    readonly?: boolean;
    onRatingChange?: (rating: number) => void;
}

export function Rating({
    rating,
    maxRating = 5,
    size = 'md',
    showCount = false,
    reviewCount = 0,
    className,
    readonly = true,
    onRatingChange,
}: RatingProps) {
    const sizeClasses = {
        xs: 'w-2.5 h-2.5',
        sm: 'w-3 h-3',
        md: 'w-4 h-4',
        lg: 'w-5 h-5',
    };

    const handleStarClick = (index: number) => {
        if (!readonly && onRatingChange) {
            onRatingChange(index + 1);
        }
    };

    return (
        <div className={cn('flex items-center gap-1', className)}>
            <div className="flex items-center">
                {Array.from({ length: maxRating }, (_, index) => {
                    const isFilled = index < Math.floor(rating);
                    const isHalfFilled = index === Math.floor(rating) && rating % 1 !== 0;

                    return (
                        <button
                            key={index}
                            className={cn(
                                'relative',
                                !readonly && 'hover:scale-110 transition-transform',
                                readonly && 'cursor-default'
                            )}
                            onClick={() => handleStarClick(index)}
                            disabled={readonly}
                        >
                            <Star
                                className={cn(
                                    sizeClasses[size],
                                    isFilled || isHalfFilled
                                        ? 'fill-yellow-400 text-yellow-400'
                                        : 'text-gray-300'
                                )}
                            />
                            {isHalfFilled && (
                                <Star
                                    className={cn(
                                        'absolute top-0 left-0 fill-yellow-400 text-yellow-400',
                                        sizeClasses[size]
                                    )}
                                    style={{
                                        clipPath: 'inset(0 50% 0 0)',
                                    }}
                                />
                            )}
                        </button>
                    );
                })}
            </div>

            {showCount && (
                <span className="text-sm text-gray-600 ml-1">
                    ({reviewCount})
                </span>
            )}
        </div>
    );
}