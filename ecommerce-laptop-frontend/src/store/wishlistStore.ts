import { Product } from '@/types/api';
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface WishlistItem {
    product: Product;
    note?: string; // user-saved note for "save for later"
    addedAt: string;
    lastSyncedAt?: string | null; // ISO string for multi-device sync indicator
}

interface WishlistState {
    items: WishlistItem[];

    // Actions
    addItem: (product: Product, note?: string) => void;
    removeItem: (productId: number) => void;
    clearWishlist: () => void;
    isInWishlist: (productId: number) => boolean;
    toggleItem: (product: Product) => void;
    setNote: (productId: number, note: string) => void;
    setLastSynced: (productId: number, isoString: string | null) => void;
    getItem: (productId: number) => WishlistItem | undefined;
}

export const useWishlistStore = create<WishlistState>()(
    persist(
        (set, get) => ({
            items: [],

            addItem: (product: Product, note?: string) => {
                const items = get().items;
                const exists = items.some(item => item.product.id === product.id);

                if (!exists) {
                    set(state => ({
                        items: [
                            ...state.items,
                            {
                                product,
                                note,
                                addedAt: new Date().toISOString(),
                                lastSyncedAt: null
                            }
                        ]
                    }));
                }
            },

            removeItem: (productId: number) => {
                set(state => ({
                    items: state.items.filter(item => item.product.id !== productId)
                }));
            },

            clearWishlist: () => {
                set({ items: [] });
            },

            isInWishlist: (productId: number) => {
                return get().items.some(item => item.product.id === productId);
            },

            toggleItem: (product: Product) => {
                const isInList = get().isInWishlist(product.id);
                if (isInList) {
                    get().removeItem(product.id);
                } else {
                    get().addItem(product);
                }
            },

            setNote: (productId: number, note: string) => {
                set(state => ({
                    items: state.items.map(item =>
                        item.product.id === productId ? { ...item, note } : item
                    )
                }));
            },

            setLastSynced: (productId: number, isoString: string | null) => {
                set(state => ({
                    items: state.items.map(item =>
                        item.product.id === productId ? { ...item, lastSyncedAt: isoString } : item
                    )
                }));
            },

            getItem: (productId: number) => {
                return get().items.find(i => i.product.id === productId);
            }
        }),
        {
            name: 'wishlist-storage',
        }
    )
);