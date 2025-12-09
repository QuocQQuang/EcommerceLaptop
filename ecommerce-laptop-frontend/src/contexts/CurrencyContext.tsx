'use client';

import {
    Currency,
    fetchLatestExchangeRates,
    getExchangeRates,
    getSelectedCurrency,
    setSelectedCurrency
} from '@/lib/currency';
import { createContext, ReactNode, useContext, useEffect, useState } from 'react';

interface CurrencyContextType {
    selectedCurrency: Currency;
    setSelectedCurrency: (currency: Currency) => void;
    exchangeRates: Record<Currency, number>;
    refreshRates: () => Promise<void>;
    isLoading: boolean;
}

const CurrencyContext = createContext<CurrencyContextType | undefined>(undefined);

interface CurrencyProviderProps {
    children: ReactNode;
}

export function CurrencyProvider({ children }: CurrencyProviderProps) {
    const [selectedCurrency, setSelectedCurrencyState] = useState<Currency>('USD');
    const [exchangeRates, setExchangeRatesState] = useState<Record<Currency, number>>({
        USD: 1,
        VND: 24000,
    });
    const [isLoading, setIsLoading] = useState(false);

    useEffect(() => {
        // Load currency settings on mount
        setSelectedCurrencyState(getSelectedCurrency());
        setExchangeRatesState(getExchangeRates());

        // Listen for storage changes (when currency is changed in another tab)
        const handleStorageChange = (e: StorageEvent) => {
            if (e.key === 'selected_currency' && e.newValue) {
                setSelectedCurrencyState(e.newValue as Currency);
            }
            if (e.key === 'exchange_rates' && e.newValue) {
                try {
                    setExchangeRatesState(JSON.parse(e.newValue));
                } catch (error) {
                    console.error('Failed to parse exchange rates from storage:', error);
                }
            }
        };

        // Listen for custom events (when currency is changed in same tab)
        const handleCurrencyChange = (e: CustomEvent) => {
            setSelectedCurrencyState(e.detail.currency);
        };

        const handleExchangeRatesChange = (e: CustomEvent) => {
            setExchangeRatesState(e.detail.rates);
        };

        window.addEventListener('storage', handleStorageChange);
        window.addEventListener('currencyChanged', handleCurrencyChange as EventListener);
        window.addEventListener('exchangeRatesChanged', handleExchangeRatesChange as EventListener);

        return () => {
            window.removeEventListener('storage', handleStorageChange);
            window.removeEventListener('currencyChanged', handleCurrencyChange as EventListener);
            window.removeEventListener('exchangeRatesChanged', handleExchangeRatesChange as EventListener);
        };
    }, []);

    const handleSetSelectedCurrency = (currency: Currency) => {
        setSelectedCurrency(currency);
        setSelectedCurrencyState(currency);
    };

    const refreshRates = async () => {
        setIsLoading(true);
        try {
            const newRates = await fetchLatestExchangeRates();
            setExchangeRatesState(newRates);
        } catch (error) {
            console.error('Failed to refresh exchange rates:', error);
        } finally {
            setIsLoading(false);
        }
    };

    const value: CurrencyContextType = {
        selectedCurrency,
        setSelectedCurrency: handleSetSelectedCurrency,
        exchangeRates,
        refreshRates,
        isLoading,
    };

    return (
        <CurrencyContext.Provider value={value}>
            {children}
        </CurrencyContext.Provider>
    );
}

export function useCurrencyContext() {
    const context = useContext(CurrencyContext);
    if (context === undefined) {
        throw new Error('useCurrencyContext must be used within a CurrencyProvider');
    }
    return context;
}
