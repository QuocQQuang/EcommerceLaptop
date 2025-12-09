import apiClient from '@/lib/api';

export interface WishlistItem {
    id: string;
    productId: number;
    product: {
        id: number;
        name: string;
        price: number;
        sku: string;
        brand: string;
        isActive: boolean;
        images: Array<{
            id: number;
            imageUrl: string;
            altText: string;
            isPrimary: boolean;
        }>;
    };
    addedAt: string;
}

export interface WishlistResponse {
    items: WishlistItem[];
    totalCount: number;
}

export const wishlistService = {
    // Get user's wishlist
    async getWishlist(): Promise<WishlistResponse> {
        const { data } = await apiClient.get('/wishlist');
        return data;
    },

    // Add product to wishlist
    async addToWishlist(productId: number): Promise<void> {
        await apiClient.post(`/wishlist/${productId}`);
    },

    // Remove product from wishlist
    async removeFromWishlist(productId: number): Promise<void> {
        await apiClient.delete(`/wishlist/${productId}`);
    },

    // Clear entire wishlist
    async clearWishlist(): Promise<void> {
        await apiClient.post('/wishlist/clear');
    },

    // Check if product is in wishlist
    async checkInWishlist(productId: number): Promise<boolean> {
        const { data } = await apiClient.get(`/wishlist/check/${productId}`);
        return data.isInWishlist;
    },

    // Get wishlist count
    async getWishlistCount(): Promise<number> {
        const { data } = await apiClient.get('/wishlist/count');
        return data.count;
    }
};