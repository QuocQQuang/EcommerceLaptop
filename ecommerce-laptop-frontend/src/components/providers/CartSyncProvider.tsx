'use client';

import { useCart } from '@/hooks/useCart';
import { useCartStore } from '@/store/cartStore';
import { useEffect, useRef } from 'react';

export function CartSyncProvider({ children }: { children: React.ReactNode }) {
    const { refreshCart } = useCart();
    const { sessionId } = useCartStore();
    const pollingIntervalRef = useRef<NodeJS.Timeout | null>(null);

    // Initial sync when sessionId is available
    useEffect(() => {
        if (sessionId) {
            refreshCart();
        }
    }, [sessionId, refreshCart]);

    // Periodic polling for cart updates (every 10 seconds)
    useEffect(() => {
        if (!sessionId) return;

        const pollCart = () => {
            // Only poll if page is visible to save resources
            if (document.visibilityState === 'visible') {
                refreshCart();
            }
        };

        // Start polling
        pollingIntervalRef.current = setInterval(pollCart, 10000); // 10 seconds

        // Cleanup on unmount
        return () => {
            if (pollingIntervalRef.current) {
                clearInterval(pollingIntervalRef.current);
                pollingIntervalRef.current = null;
            }
        };
    }, [sessionId, refreshCart]);

    return <>{children}</>;
}
