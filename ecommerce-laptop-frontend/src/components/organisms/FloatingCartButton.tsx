'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useCartStore } from '@/store/cartStore';
import { useUIStore } from '@/store/uiStore';
import { ShoppingBag } from 'lucide-react';
import { useEffect, useState } from 'react';

export function FloatingCartButton() {
    const { itemCount } = useCartStore();
    const { setCartSidebarOpen } = useUIStore();
    const [mounted, setMounted] = useState(false);

    useEffect(() => {
        setMounted(true);
    }, []);

    if (!mounted) {
        return null;
    }

    return (
        <div className="fixed bottom-6 right-6 z-50 md:hidden">
            <Button
                onClick={() => setCartSidebarOpen(true)}
                className="h-14 w-14 rounded-full bg-primary hover:bg-primary/90 shadow-lg hover:shadow-xl transition-all duration-200 p-0"
                size="lg"
            >
                <div className="relative">
                    <ShoppingBag className="h-6 w-6 text-primary-foreground" />
                    {itemCount > 0 && (
                        <Badge
                            variant="destructive"
                            className="absolute -top-2 -right-2 h-6 w-6 p-0 flex items-center justify-center text-xs font-bold"
                        >
                            {itemCount > 99 ? '99+' : itemCount}
                        </Badge>
                    )}
                </div>
            </Button>
        </div>
    );
}

