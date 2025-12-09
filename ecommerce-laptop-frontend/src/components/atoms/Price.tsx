'use client';

import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { useCurrencyUpdate } from '@/hooks/useCurrencyUpdate';
import { Currency, formatCurrencyPrice } from '@/lib/currency';
import { cn } from '@/lib/utils';

interface PriceProps {
    price: number; // Price in USD from database
    discountPrice?: number; // Discount price in USD from database
    currency?: Currency; // Override selected currency
    className?: string;
    size?: 'sm' | 'md' | 'lg';
    showComparison?: boolean; // Show both USD and VND
    forceCurrency?: Currency; // Force specific currency display
}

export function Price({
    price,
    discountPrice,
    currency,
    className,
    size = 'md',
    showComparison = false,
    forceCurrency
}: PriceProps) {
    const { selectedCurrency } = useCurrencyContext();
    const updateTrigger = useCurrencyUpdate(); // Force re-render on currency change
    const displayCurrency = forceCurrency || currency || selectedCurrency;

    const formatPrice = (amount: number) => {
        return formatCurrencyPrice(amount, displayCurrency);
    };

    const sizeClasses = {
        sm: 'text-sm',
        md: 'text-base',
        lg: 'text-lg',
    };

    const hasDiscount = discountPrice && discountPrice < price;

    if (showComparison && displayCurrency === 'USD') {
        // Show both USD and VND when comparison is enabled
        const usdPrice = formatCurrencyPrice(price, 'USD');
        const vndPrice = formatCurrencyPrice(price, 'VND');
        const usdDiscount = discountPrice ? formatCurrencyPrice(discountPrice, 'USD') : null;
        const vndDiscount = discountPrice ? formatCurrencyPrice(discountPrice, 'VND') : null;

        return (
            <div className={cn('flex flex-col gap-1', className)}>
                <div className="flex items-center gap-2">
                    {hasDiscount ? (
                        <>
                            <span className={cn('font-bold text-red-600', sizeClasses[size])}>
                                {usdDiscount}
                            </span>
                            <span className={cn('text-gray-500 line-through', sizeClasses[size === 'lg' ? 'md' : 'sm'])}>
                                {usdPrice}
                            </span>
                        </>
                    ) : (
                        <span className={cn('font-bold text-gray-900', sizeClasses[size])}>
                            {usdPrice}
                        </span>
                    )}
                </div>
                <div className="flex items-center gap-2">
                    {hasDiscount ? (
                        <>
                            <span className={cn('font-bold text-red-600', sizeClasses[size === 'lg' ? 'md' : 'sm'])}>
                                {vndDiscount}
                            </span>
                            <span className={cn('text-gray-500 line-through text-xs', sizeClasses[size === 'lg' ? 'sm' : 'xs'])}>
                                {vndPrice}
                            </span>
                        </>
                    ) : (
                        <span className={cn('font-medium text-gray-600', sizeClasses[size === 'lg' ? 'md' : 'sm'])}>
                            {vndPrice}
                        </span>
                    )}
                </div>
            </div>
        );
    }

    return (
        <div className={cn('flex items-center gap-2', className)}>
            {hasDiscount ? (
                <>
                    <span className={cn('font-bold text-red-600', sizeClasses[size])}>
                        {formatPrice(discountPrice)}
                    </span>
                    <span className={cn('text-gray-500 line-through', sizeClasses[size === 'lg' ? 'md' : 'sm'])}>
                        {formatPrice(price)}
                    </span>
                </>
            ) : (
                <span className={cn('font-bold text-gray-900', sizeClasses[size])}>
                    {formatPrice(price)}
                </span>
            )}
        </div>
    );
}