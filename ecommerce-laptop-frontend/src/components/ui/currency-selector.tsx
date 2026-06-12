'use client';

import { Button } from '@/components/ui/button';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import {
    CURRENCY_CONFIGS,
    Currency
} from '@/lib/currency';
import { Coins, DollarSign, RefreshCw } from 'lucide-react';

interface CurrencySelectorProps {
    onCurrencyChange?: (currency: Currency) => void;
    showRefreshButton?: boolean;
    className?: string;
    variant?: 'toggle' | 'buttons' | 'simple';
}

export function CurrencySelector({
    onCurrencyChange,
    showRefreshButton = true,
    className = '',
    variant = 'toggle'
}: CurrencySelectorProps) {
    const { selectedCurrency, setSelectedCurrency, refreshRates, isLoading } = useCurrencyContext();

    const handleCurrencyChange = (currency: Currency) => {
        setSelectedCurrency(currency);
        onCurrencyChange?.(currency);
    };

    const handleRefreshRates = async () => {
        await refreshRates();
    };

    const getCurrencyIcon = (currency: Currency) => {
        switch (currency) {
            case 'USD':
                return <DollarSign className="h-4 w-4" />;
            case 'VND':
                return <Coins className="h-4 w-4" />;
            default:
                return <DollarSign className="h-4 w-4" />;
        }
    };

    // Render different variants
    if (variant === 'simple') {
        return (
            <div className={`flex items-center gap-2 ${className}`}>
                {Object.entries(CURRENCY_CONFIGS).map(([code, config]) => (
                    <Button
                        key={code}
                        variant={selectedCurrency === code ? "default" : "outline"}
                        size="sm"
                        onClick={() => handleCurrencyChange(code as Currency)}
                        className="flex items-center gap-1"
                    >
                        {getCurrencyIcon(code as Currency)}
                        <span className="font-medium text-xs">
                            {config.code}
                        </span>
                    </Button>
                ))}
                {showRefreshButton && (
                    <Button
                        variant="ghost"
                        size="sm"
                        onClick={handleRefreshRates}
                        disabled={isLoading}
                        className="p-2"
                        title="Cập nhật tỷ giá"
                    >
                        <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
                    </Button>
                )}
            </div>
        );
    }

    if (variant === 'buttons') {
        return (
            <div className={`flex items-center gap-2 ${className}`}>
                {Object.entries(CURRENCY_CONFIGS).map(([code, config]) => (
                    <Button
                        key={code}
                        variant={selectedCurrency === code ? "default" : "secondary"}
                        size="sm"
                        onClick={() => handleCurrencyChange(code as Currency)}
                        className="flex items-center gap-2"
                    >
                        {getCurrencyIcon(code as Currency)}
                        <span className="font-medium">
                            {config.code}
                        </span>
                        <span className="text-xs opacity-70">
                            {config.symbol}
                        </span>
                    </Button>
                ))}
                {showRefreshButton && (
                    <Button
                        variant="ghost"
                        size="sm"
                        onClick={handleRefreshRates}
                        disabled={isLoading}
                        className="p-2"
                        title="Cập nhật tỷ giá"
                    >
                        <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
                    </Button>
                )}
            </div>
        );
    }

    // Default toggle variant
    return (
        <div className={`flex items-center gap-1 ${className}`}>
            {/* Currency Toggle Buttons */}
            <div className="flex items-center border rounded-md">
                {Object.entries(CURRENCY_CONFIGS).map(([code, config]) => (
                    <Button
                        key={code}
                        variant={selectedCurrency === code ? "default" : "ghost"}
                        size="sm"
                        onClick={() => handleCurrencyChange(code as Currency)}
                        className={`flex items-center gap-1 px-3 py-1 h-8 rounded-none first:rounded-l-md last:rounded-r-md ${selectedCurrency === code
                                ? 'bg-primary text-primary-foreground'
                                : 'hover:bg-muted'
                            }`}
                    >
                        {getCurrencyIcon(code as Currency)}
                        <span className="font-medium text-xs">
                            {config.code}
                        </span>
                    </Button>
                ))}
            </div>

            {/* Refresh Button */}
            {showRefreshButton && (
                <Button
                    variant="ghost"
                    size="sm"
                    onClick={handleRefreshRates}
                    disabled={isLoading}
                    className="p-2 h-8 w-8"
                    title="Cập nhật tỷ giá"
                >
                    <RefreshCw className={`h-4 w-4 ${isLoading ? 'animate-spin' : ''}`} />
                </Button>
            )}
        </div>
    );
}

// Hook for using currency state in components - now uses context
export function useCurrency() {
    return useCurrencyContext();
}
