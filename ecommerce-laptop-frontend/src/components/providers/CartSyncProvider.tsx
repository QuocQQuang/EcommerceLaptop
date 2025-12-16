'use client';

import { useCart } from '@/hooks/useCart';
import { useEffect } from 'react';

export function CartSyncProvider({ children }: { children: React.ReactNode }) {
    const { refreshCart, itemCount } = useCart();

    // 1. Initial Fetch on Mount
    useEffect(() => {
        refreshCart();
    }, [refreshCart]);

    // 2. Listen for 'cart:refresh' events (from Chat or other sources)
    useEffect(() => {
        const handleRefresh = () => {
            console.log(' CartSync: Refreshing cart from event...');
            refreshCart();
        };

        window.addEventListener('cart:refresh', handleRefresh);
        return () => window.removeEventListener('cart:refresh', handleRefresh);
    }, [refreshCart]);

    return <>{children}</>;
}
