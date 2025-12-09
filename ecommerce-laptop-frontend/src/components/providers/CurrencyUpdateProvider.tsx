'use client';

import { useCurrencyUpdate } from '@/hooks/useCurrencyUpdate';
import { ReactNode } from 'react';

interface CurrencyUpdateProviderProps {
    children: ReactNode;
}

/**
 * Provider that forces re-render of all currency-dependent components
 * when currency changes. This ensures real-time updates without page reload.
 */
export function CurrencyUpdateProvider({ children }: CurrencyUpdateProviderProps) {
    // This hook will trigger re-renders when currency changes
    useCurrencyUpdate();

    return <>{children}</>;
}
