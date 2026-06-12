import api from '@/lib/api';
import logger from '@/lib/logger';
import type { OrderResponse } from '@/types/order';


export interface CreateOrderRequest {
    cartId: string;
    shippingAddress: string;
    paymentMethod?: string;
    orderStatus?: string;
}

export const orderService = {
    /**
     * Create a pending order from cart
     */
    createOrderFromCart: async (request: CreateOrderRequest): Promise<OrderResponse> => {
        logger.info(' Starting order creation from cart', {
            cartId: request.cartId,
            hasShippingAddress: !!request.shippingAddress,
            shippingAddressLength: request.shippingAddress?.length
        });

        try {
            // Validate request data
            if (!request.cartId) {
                const error = new Error('Cart ID is required');
                logger.error(' Order creation validation failed', { error: error.message, request });
                throw error;
            }

            if (!request.shippingAddress || request.shippingAddress.trim().length === 0) {
                const error = new Error('Shipping address is required');
                logger.error(' Order creation validation failed', { error: error.message, request });
                throw error;
            }

            logger.debug(' Sending create order request', {
                endpoint: `/orders/from-cart/${request.cartId}`,
                payload: {
                    shippingAddress: request.shippingAddress,
                    paymentMethod: request.paymentMethod,
                    orderStatus: request.orderStatus
                }
            });

            const response = await api.post(`/orders/from-cart/${request.cartId}`, {
                shippingAddress: request.shippingAddress,
                paymentMethod: request.paymentMethod,
                orderStatus: request.orderStatus
            });

            logger.info(' Order created successfully', {
                orderId: response.data?.orderId,
                status: response.data?.status,
                responseStatus: response.status
            });

            return response.data;
        } catch (error: any) {
            logger.error(' Order creation failed', {
                cartId: request.cartId,
                error: error.message,
                status: error.response?.status,
                statusText: error.response?.statusText,
                responseData: error.response?.data,
                stack: error.stack
            });

            // Re-throw with more context
            const enhancedError = new Error(
                error.response?.data?.message ||
                error.message ||
                'Tạo đơn hàng thất bại. Vui lòng thử lại.'
            );
            enhancedError.name = 'OrderCreationError';
            (enhancedError as any).originalError = error;
            (enhancedError as any).cartId = request.cartId;

            throw enhancedError;
        }
    },

    /**
     * Get order status and details
     */
    getOrderStatus: async (orderId: number): Promise<OrderResponse> => {
        logger.info(' Fetching order status', { orderId });

        try {
            const response = await api.get(`/orders/${orderId}`);

            logger.info(' Order status retrieved', {
                orderId,
                status: response.data?.status,
                paymentStatus: response.data?.paymentStatus
            });

            return response.data;
        } catch (error: any) {
            logger.error(' Failed to get order status', {
                orderId,
                error: error.message,
                status: error.response?.status,
                responseData: error.response?.data
            });
            throw error;
        }
    },

    /**
     * Get payment status for order (alternative endpoint if available)
     */
    getPaymentStatus: async (orderId: number): Promise<{ status: string; paymentStatus: string }> => {
        logger.info(' Fetching payment status', { orderId });

        try {
            const response = await api.get(`/payment/${orderId}/status`);

            logger.info(' Payment status retrieved', {
                orderId,
                paymentStatus: response.data?.paymentStatus,
                status: response.data?.status
            });

            return response.data;
        } catch (error: any) {
            logger.error(' Failed to get payment status', {
                orderId,
                error: error.message,
                status: error.response?.status,
                responseData: error.response?.data
            });
            throw error;
        }
    },

    /**
     * Get user's orders with pagination
     */
    getUserOrders: async (customerId: number, page: number = 1, pageSize: number = 10): Promise<any> => {
        logger.info(' Fetching user orders', { customerId, page, pageSize });

        try {
            const response = await api.get(`/orders/customer/${customerId}?page=${page}&pageSize=${pageSize}`);

            logger.info(' User orders retrieved', {
                customerId,
                orderCount: response.data?.items?.length || 0,
                totalCount: response.data?.totalCount
            });

            return response.data || { items: [], totalCount: 0 };
        } catch (error: any) {
            logger.error(' Failed to get user orders', {
                customerId,
                error: error.message,
                status: error.response?.status,
                responseData: error.response?.data
            });
            
            // Return empty array instead of throwing to prevent blocking the account page
            return { items: [], totalCount: 0 };
        }
    },

    /**
     * Cancel an order
     */
    cancelOrder: async (orderId: number, reason: string): Promise<void> => {
        logger.info(' Cancelling order', { orderId, reason });

        try {
            await api.delete(`/orders/${orderId}`, {
                data: { reason }
            });

            logger.info(' Order cancelled successfully', { orderId });
        } catch (error: any) {
            logger.error(' Failed to cancel order', {
                orderId,
                error: error.message,
                status: error.response?.status,
                responseData: error.response?.data
            });
            throw error;
        }
    },
};