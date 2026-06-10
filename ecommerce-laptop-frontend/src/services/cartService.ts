import apiClient from '@/lib/api';
import { AddToCartRequest, Cart, UpdateCartItemRequest } from '@/types/api';

export const cartService = {
    async getCart(sessionId?: string): Promise<Cart> {
        const { data } = await apiClient.get('/cart', {
            params: sessionId ? { sessionId } : undefined
        });
        return data;
    },

    async addToCart(request: AddToCartRequest): Promise<Cart> {
        try {
            const { data } = await apiClient.post('/cart/items', request);
            return data;
        } catch (error: any) {
            if (error.response?.status === 409) {
                // Handle inventory conflict
                let errorMessage = 'Sản phẩm không đủ hàng trong kho';

                // Try to extract detailed error message
                if (error.response?.data) {
                    if (typeof error.response.data === 'string') {
                        errorMessage = error.response.data;
                    } else if (error.response.data.message) {
                        errorMessage = error.response.data.message;
                    }
                }

                throw new Error(`409: ${errorMessage}`);
            }
            throw error;
        }
    },

    async updateCartItem(request: UpdateCartItemRequest): Promise<Cart> {
        // Backend expects PUT /cart/items with body containing CartItemId and SessionId
        const { data } = await apiClient.put(`/cart/items`, {
            cartItemId: request.itemId,
            quantity: request.quantity,
            sessionId: request.sessionId,
        });
        return data;
    },

    async removeCartItem(itemId: number, sessionId?: string): Promise<Cart> {
        const { data } = await apiClient.delete(`/cart/items/${itemId}`, {
            params: sessionId ? { sessionId } : undefined
        });
        return data;
    },

    async clearCart(sessionId?: string): Promise<{ success: boolean }> {
        const { data } = await apiClient.delete('/cart', {
            params: sessionId ? { sessionId } : undefined
        });
        return data;
    },

    async getCartItemCount(sessionId?: string): Promise<number> {
        const { data } = await apiClient.get('/cart/count', {
            params: sessionId ? { sessionId } : undefined
        });
        return data;
    },

    async validateCart(sessionId?: string): Promise<unknown> {
        const { data } = await apiClient.get('/cart/validate', {
            params: sessionId ? { sessionId } : undefined
        });
        return data;
    },

    async applyDiscount(discountCode: string, sessionId?: string): Promise<Cart> {
        const { data } = await apiClient.post('/cart/discount', {
            discountCode,
            sessionId
        });
        return data;
    },

    async removeDiscount(sessionId?: string): Promise<Cart> {
        const { data } = await apiClient.delete('/cart/discount', {
            params: sessionId ? { sessionId } : undefined
        });
        return data;
    },
};