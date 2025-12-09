import { CartItem, Product } from '@/types/api';
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface CartState {
    items: CartItem[];
    isOpen: boolean;
    sessionId: string | null;
    total: number;
    itemCount: number;

    // Actions
    addItem: (product: Product, quantity?: number, variantId?: number) => void;
    removeItem: (itemId: number) => void;
    updateQuantity: (itemId: number, quantity: number) => void;
    clearCart: () => void;
    setIsOpen: (isOpen: boolean) => void;
    setSessionId: (sessionId: string) => void;
    setItems: (items: CartItem[]) => void;
    calculateTotals: () => void;
}

const generateSessionId = () => {
    return Math.random().toString(36).substring(2) + Date.now().toString(36);
};

export const useCartStore = create<CartState>()(
    persist(
        (set, get) => ({
            items: [],
            isOpen: false,
            // Ensure a stable guest session ID immediately
            sessionId: generateSessionId(),
            total: 0,
            itemCount: 0,

            addItem: (product: Product, quantity = 1, variantId?: number) => {
                const items = get().items;
                // Handle nested product structure (product.data.id vs product.id)
                const actualProduct = (product as any)?.data || product;
                const productId = actualProduct.id;

                if (!productId) {
                    console.error(' Invalid product structure:', { product, actualProduct });
                    return;
                }

                // For variants, use variant ID as the unique identifier
                const itemKey = variantId ? `${productId}-${variantId}` : productId;
                const existingItem = items.find(item =>
                    variantId
                        ? item.productId === productId && item.variantId === variantId
                        : item.productId === productId && !item.variantId
                );

                if (existingItem) {
                    set(state => ({
                        items: state.items.map(item =>
                            variantId
                                ? (item.productId === productId && item.variantId === variantId)
                                : (item.productId === productId && !item.variantId)
                                    ? {
                                        ...item,
                                        quantity: item.quantity + quantity,
                                        totalPrice: (item.quantity + quantity) * item.unitPrice
                                    }
                                    : item
                        )
                    }));
                } else {
                    const newItem: CartItem = {
                        id: Date.now(), // Temporary ID for local state
                        productId: productId, // Use actual product ID
                        product: actualProduct, // Store the actual product data
                        quantity,
                        unitPrice: actualProduct.discountPrice || actualProduct.price,
                        totalPrice: (actualProduct.discountPrice || actualProduct.price) * quantity,
                        // Variant support
                        variantId: variantId,
                        variantName: variantId ? actualProduct.variantName : undefined,
                        variantSku: variantId ? actualProduct.variantSku : undefined,
                        isVariant: !!variantId,
                    };

                    set(state => ({
                        items: [...state.items, newItem]
                    }));
                }

                get().calculateTotals();
            },

            removeItem: (itemId: number) => {
                set(state => ({
                    items: state.items.filter(item => item.id !== itemId)
                }));
                get().calculateTotals();
            },

            updateQuantity: (itemId: number, quantity: number) => {
                if (quantity <= 0) {
                    get().removeItem(itemId);
                    return;
                }

                set(state => ({
                    items: state.items.map(item =>
                        item.id === itemId
                            ? {
                                ...item,
                                quantity,
                                totalPrice: quantity * item.unitPrice
                            }
                            : item
                    )
                }));
                get().calculateTotals();
            },

            clearCart: () => {
                set({
                    items: [],
                    total: 0,
                    itemCount: 0
                });
            },

            setIsOpen: (isOpen: boolean) => {
                set({ isOpen });
            },

            setSessionId: (sessionId: string) => {
                set({ sessionId });
            },

            setItems: (items: CartItem[]) => {
                set({ items });
                get().calculateTotals();
            },

            calculateTotals: () => {
                const items = get().items;
                const total = items.reduce((sum, item) => sum + item.totalPrice, 0);
                const itemCount = items.reduce((sum, item) => sum + item.quantity, 0);

                set({ total, itemCount });
            },
        }),
        {
            name: 'cart-storage',
            partialize: (state) => ({
                items: state.items,
                sessionId: state.sessionId,
                total: state.total,
                itemCount: state.itemCount,
            }),
        }
    )
);