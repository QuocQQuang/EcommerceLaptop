'use client';

import { useEffect, useState } from 'react';

/**
 * Hook to force re-render when currency changes
 * This ensures all components using currency context update immediately
 */
export function useCurrencyUpdate() {
    const [updateTrigger, setUpdateTrigger] = useState(0);

    useEffect(() => {
        const handleCurrencyChange = () => {
            setUpdateTrigger(prev => prev + 1);
        };

        const handleExchangeRatesChange = () => {
            setUpdateTrigger(prev => prev + 1);
        };

        // Listen for custom events
        window.addEventListener('currencyChanged', handleCurrencyChange);
        window.addEventListener('exchangeRatesChanged', handleExchangeRatesChange);

        // Also listen for storage changes (cross-tab)
        const handleStorageChange = (e: StorageEvent) => {
            if (e.key === 'selected_currency' || e.key === 'exchange_rates') {
                setUpdateTrigger(prev => prev + 1);
            }
        };

        window.addEventListener('storage', handleStorageChange);

        return () => {
            window.removeEventListener('currencyChanged', handleCurrencyChange);
            window.removeEventListener('exchangeRatesChanged', handleExchangeRatesChange);
            window.removeEventListener('storage', handleStorageChange);
        };
    }, []);

    return updateTrigger;
}
