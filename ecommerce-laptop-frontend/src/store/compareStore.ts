import { Product } from '@/types/api';
import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface CompareState {
    items: Product[]; // up to 3 products
    addItem: (product: Product) => void;
    removeItem: (productId: number) => void;
    clear: () => void;
    isInCompare: (productId: number) => boolean;
    toggleItem: (product: Product) => void;
}

export const useCompareStore = create<CompareState>()(
    persist(
        (set, get) => ({
            items: [],

            addItem: (product: Product) => {
                const items = get().items;
                if (items.some(i => i.id === product.id)) return;
                if (items.length >= 3) {
                    // remove oldest (FIFO) to keep max 3
                    set({ items: [...items.slice(1), product] });
                } else {
                    set({ items: [...items, product] });
                }
            },

            removeItem: (productId: number) => {
                set({ items: get().items.filter(i => i.id !== productId) });
            },

            clear: () => set({ items: [] }),

            isInCompare: (productId: number) => get().items.some(i => i.id === productId),

            toggleItem: (product: Product) => {
                if (get().isInCompare(product.id)) {
                    get().removeItem(product.id);
                } else {
                    get().addItem(product);
                }
            }
        }),
        {
            name: 'compare-storage'
        }
    )
);

export default useCompareStore;
