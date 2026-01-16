'use client';

import { useCart } from '@/hooks/useCart';
import { useCartStore } from '@/store/cartStore';
import { useEffect } from 'react';

export function CartSyncProvider({ children }: { children: React.ReactNode }) {
    const { refreshCart } = useCart();
    const { sessionId } = useCartStore();

    // Initial sync when sessionId is available
    useEffect(() => {
        if (sessionId) {
            refreshCart();
        }
    }, [sessionId, refreshCart]);

    // Re-sync when window gains focus (user returns to tab) for multi-tab support
    useEffect(() => {
        if (!sessionId) return;

        const handleVisibilityChange = () => {
            if (document.visibilityState === 'visible') {
                // User returned to tab - sync to get latest changes from other tabs/devices
                refreshCart();
            }
        };

        document.addEventListener('visibilitychange', handleVisibilityChange);

        return () => {
            document.removeEventListener('visibilitychange', handleVisibilityChange);
        };
    }, [sessionId, refreshCart]);

    return <>{children}</>;
}
