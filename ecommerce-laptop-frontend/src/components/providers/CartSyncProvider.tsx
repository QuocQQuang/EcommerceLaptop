'use client';

import { useCart } from '@/hooks/useCart';
import { useCartStore } from '@/store/cartStore';
import { useEffect } from 'react';

export function CartSyncProvider({ children }: { children: React.ReactNode }) {
    const { refreshCart } = useCart();
    const { sessionId } = useCartStore();

    useEffect(() => {
        if (sessionId) {
            refreshCart();
        }
    }, [sessionId, refreshCart]);

    return <>{children}</>;
}
