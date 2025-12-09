// Currency conversion utilities
export type Currency = 'USD' | 'VND';

export interface CurrencyConfig {
    code: Currency;
    symbol: string;
    locale: string;
    exchangeRate: number; // Rate from USD to this currency
}

// Default exchange rates (can be updated from API)
const DEFAULT_EXCHANGE_RATES: Record<Currency, number> = {
    USD: 1,
    VND: 24000, // 1 USD = 24,000 VND (approximate)
};

// Currency service for better rate management
export class CurrencyService {
    private static rates: Record<Currency, number> = { ...DEFAULT_EXCHANGE_RATES };

    static getExchangeRate(from: Currency, to: Currency): number {
        if (from === to) return 1;

        // Convert through USD as base currency
        if (from === 'USD') {
            return this.rates[to];
        } else if (to === 'USD') {
            return 1 / this.rates[from];
        } else {
            // Convert from -> USD -> to
            const toUSD = 1 / this.rates[from];
            return toUSD * this.rates[to];
        }
    }

    static convertAmount(amount: number, from: Currency, to: Currency): number {
        if (from === to) return amount;

        const rate = this.getExchangeRate(from, to);
        return Math.round(amount * rate * 100) / 100; // Round to 2 decimal places
    }

    static updateRates(newRates: Partial<Record<Currency, number>>): void {
        this.rates = { ...this.rates, ...newRates };
    }

    static getRates(): Record<Currency, number> {
        return { ...this.rates };
    }
}

// Currency configurations
export const CURRENCY_CONFIGS: Record<Currency, CurrencyConfig> = {
    USD: {
        code: 'USD',
        symbol: '$',
        locale: 'en-US',
        exchangeRate: 1,
    },
    VND: {
        code: 'VND',
        symbol: '',
        locale: 'vi-VN',
        exchangeRate: 24000,
    },
};

// Storage keys
const STORAGE_KEYS = {
    SELECTED_CURRENCY: 'selected_currency',
    EXCHANGE_RATES: 'exchange_rates',
    LAST_UPDATED: 'exchange_rates_last_updated',
} as const;

// Cache duration: 1 hour
const CACHE_DURATION = 60 * 60 * 1000;

/**
 * Get the currently selected currency from localStorage
 */
export function getSelectedCurrency(): Currency {
    if (typeof window === 'undefined') return 'USD';

    const stored = localStorage.getItem(STORAGE_KEYS.SELECTED_CURRENCY);
    return (stored as Currency) || 'USD';
}

/**
 * Set the selected currency in localStorage
 */
export function setSelectedCurrency(currency: Currency): void {
    if (typeof window === 'undefined') return;

    localStorage.setItem(STORAGE_KEYS.SELECTED_CURRENCY, currency);

    // Dispatch custom event for same-tab updates
    window.dispatchEvent(new CustomEvent('currencyChanged', {
        detail: { currency }
    }));
}

/**
 * Get exchange rates from localStorage or return defaults
 */
export function getExchangeRates(): Record<Currency, number> {
    if (typeof window === 'undefined') return DEFAULT_EXCHANGE_RATES;

    try {
        const stored = localStorage.getItem(STORAGE_KEYS.EXCHANGE_RATES);
        const lastUpdated = localStorage.getItem(STORAGE_KEYS.LAST_UPDATED);

        if (stored && lastUpdated) {
            const timeDiff = Date.now() - parseInt(lastUpdated);
            if (timeDiff < CACHE_DURATION) {
                return JSON.parse(stored);
            }
        }
    } catch (error) {
        console.warn('Failed to load exchange rates from localStorage:', error);
    }

    return DEFAULT_EXCHANGE_RATES;
}

/**
 * Update exchange rates in localStorage
 */
export function setExchangeRates(rates: Record<Currency, number>): void {
    if (typeof window === 'undefined') return;

    try {
        localStorage.setItem(STORAGE_KEYS.EXCHANGE_RATES, JSON.stringify(rates));
        localStorage.setItem(STORAGE_KEYS.LAST_UPDATED, Date.now().toString());

        // Dispatch custom event for same-tab updates
        window.dispatchEvent(new CustomEvent('exchangeRatesChanged', {
            detail: { rates }
        }));
    } catch (error) {
        console.warn('Failed to save exchange rates to localStorage:', error);
    }
}

/**
 * Convert USD amount to target currency
 */
export function convertCurrency(amountUSD: number, targetCurrency: Currency): number {
    return CurrencyService.convertAmount(amountUSD, 'USD', targetCurrency);
}

/**
 * Format price in the specified currency
 */
export function formatCurrencyPrice(
    amountUSD: number,
    currency: Currency = getSelectedCurrency(),
    options?: {
        showSymbol?: boolean;
        maximumFractionDigits?: number;
    }
): string {
    const convertedAmount = convertCurrency(amountUSD, currency);
    const config = CURRENCY_CONFIGS[currency];

    const formatOptions: Intl.NumberFormatOptions = {
        style: 'currency',
        currency: config.code,
        maximumFractionDigits: options?.maximumFractionDigits ?? (currency === 'VND' ? 0 : 2),
    };

    return new Intl.NumberFormat(config.locale, formatOptions).format(convertedAmount);
}

/**
 * Get currency symbol
 */
export function getCurrencySymbol(currency: Currency): string {
    return CURRENCY_CONFIGS[currency].symbol;
}

/**
 * Format price with both USD and VND (for comparison)
 */
export function formatPriceComparison(amountUSD: number): {
    usd: string;
    vnd: string;
    usdSymbol: string;
    vndSymbol: string;
} {
    return {
        usd: formatCurrencyPrice(amountUSD, 'USD'),
        vnd: formatCurrencyPrice(amountUSD, 'VND'),
        usdSymbol: getCurrencySymbol('USD'),
        vndSymbol: getCurrencySymbol('VND'),
    };
}

/**
 * Fetch latest exchange rates from free API
 */
export async function fetchLatestExchangeRates(): Promise<Record<Currency, number>> {
    try {
        // Using free API from exchangerate-api.com
        const response = await fetch('https://api.exchangerate-api.com/v4/latest/USD', {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
            },
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();

        const rates: Record<Currency, number> = {
            USD: 1,
            VND: data.rates?.VND || 24000,
        };

        setExchangeRates(rates);
        return rates;
    } catch (error) {
        console.warn('Failed to fetch latest exchange rates, using cached rates:', error);

        // Fallback to alternative free API
        try {
            const fallbackResponse = await fetch('https://api.fxratesapi.com/latest?base=USD&symbols=VND', {
                method: 'GET',
                headers: {
                    'Accept': 'application/json',
                },
            });

            if (fallbackResponse.ok) {
                const fallbackData = await fallbackResponse.json();
                const rates: Record<Currency, number> = {
                    USD: 1,
                    VND: fallbackData.rates?.VND || 24000,
                };

                setExchangeRates(rates);
                return rates;
            }
        } catch (fallbackError) {
            console.warn('Fallback API also failed:', fallbackError);
        }

        // Return cached rates as last resort
        return getExchangeRates();
    }
}

/**
 * Hook-like function to get current currency state
 */
export function useCurrencyState() {
    return {
        selectedCurrency: getSelectedCurrency(),
        setSelectedCurrency,
        exchangeRates: getExchangeRates(),
        formatPrice: (amount: number) => formatCurrencyPrice(amount),
        convertPrice: (amount: number, targetCurrency: Currency) => convertCurrency(amount, targetCurrency),
    };
}
