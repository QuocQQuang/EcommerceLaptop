import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

// Centralized currency formatting
// Defaults to USD unless overridden by NEXT_PUBLIC_CURRENCY and NEXT_PUBLIC_LOCALE
const DEFAULT_CURRENCY = process.env.NEXT_PUBLIC_CURRENCY || 'USD';
const DEFAULT_LOCALE = process.env.NEXT_PUBLIC_LOCALE || 'en-US';

export function formatPrice(price: number, options?: { currency?: string; locale?: string }): string {
  const currency = options?.currency || DEFAULT_CURRENCY;
  const locale = options?.locale || DEFAULT_LOCALE;
  return new Intl.NumberFormat(locale, {
    style: "currency",
    currency,
  }).format(price);
}

// Legacy function - use formatCurrencyPrice from currency.ts instead
export function formatPriceLegacy(price: number, options?: { currency?: string; locale?: string }): string {
  return formatPrice(price, options);
}

export function getApiUrl(path = "") {
  return `${process.env.NEXT_PUBLIC_API_URL || "http://localhost:5129"}${path}`;
}
