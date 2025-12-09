import api from '@/lib/api';
import { PaymentGateway, PaymentMethod } from '@/types/api';

export interface RetryPaymentRequest {
    orderId: number;
    gateway: PaymentGateway;
    method: PaymentMethod;
    returnUrl?: string;
    cancelUrl?: string;
}

export interface RetryPaymentResponse {
    isSuccess: boolean;
    transactionId?: string;
    paymentUrl?: string;
    qrCodeUrl?: string;
    errorMessage?: string;
    errorCode?: string;
    retryCount?: number;
    maxRetries?: number;
    nextRetryAt?: string;
}

export interface RetryLimits {
    maxRetries: number;
    retryDelay: number; // in milliseconds
    backoffMultiplier: number;
}

export const paymentRetryService = {
    /**
     * Retry payment for a failed order
     */
    retryPayment: async (request: RetryPaymentRequest): Promise<RetryPaymentResponse> => {
        try {
            // Validate request
            if (!request.orderId || request.orderId <= 0) {
                throw new Error('Order ID is required and must be greater than 0');
            }

            if (!request.gateway) {
                throw new Error('Payment gateway is required');
            }

            if (!request.method) {
                throw new Error('Payment method is required');
            }

            const response = await api.post('/payment/retry', {
                orderId: request.orderId,
                gateway: request.gateway,
                method: request.method,
                returnUrl: request.returnUrl || `${window.location.origin}/payment/success`,
                cancelUrl: request.cancelUrl || `${window.location.origin}/payment/cancel`,
            });

            return {
                isSuccess: response.data.isSuccess || false,
                transactionId: response.data.transactionId,
                paymentUrl: response.data.paymentUrl,
                qrCodeUrl: response.data.qrCodeUrl,
                errorMessage: response.data.errorMessage,
                errorCode: response.data.errorCode,
                retryCount: response.data.retryCount,
                maxRetries: response.data.maxRetries,
                nextRetryAt: response.data.nextRetryAt
            };
        } catch (error: any) {
            console.error('Payment retry error:', error);

            return {
                isSuccess: false,
                errorMessage: error.response?.data?.detail ||
                    error.response?.data?.message ||
                    error.message ||
                    'Failed to retry payment',
                errorCode: error.response?.data?.errorCode || 'RETRY_ERROR'
            };
        }
    },

    /**
     * Get retry limits for a specific order
     */
    getRetryLimits: async (orderId: number): Promise<RetryLimits> => {
        try {
            const response = await api.get(`/payment/retry-limits/${orderId}`);
            return response.data;
        } catch (error) {
            // Return default limits if API fails
            return {
                maxRetries: 3,
                retryDelay: 30000, // 30 seconds
                backoffMultiplier: 2
            };
        }
    },

    /**
     * Check if order is eligible for retry
     */
    isEligibleForRetry: async (orderId: number): Promise<{
        eligible: boolean;
        reason?: string;
        retryCount?: number;
        maxRetries?: number;
        nextRetryAt?: string;
    }> => {
        try {
            const response = await api.get(`/payment/retry-eligibility/${orderId}`);
            return response.data;
        } catch (error: any) {
            return {
                eligible: false,
                reason: error.response?.data?.message || 'Unable to check retry eligibility'
            };
        }
    },

    /**
     * Get retry history for an order
     */
    getRetryHistory: async (orderId: number): Promise<{
        retries: Array<{
            id: number;
            gateway: PaymentGateway;
            method: PaymentMethod;
            status: string;
            attemptedAt: string;
            errorMessage?: string;
        }>;
        totalRetries: number;
        lastRetryAt?: string;
    }> => {
        try {
            const response = await api.get(`/payment/retry-history/${orderId}`);
            return response.data;
        } catch (error) {
            return {
                retries: [],
                totalRetries: 0
            };
        }
    },

    /**
     * Calculate next retry time based on retry count and backoff strategy
     */
    calculateNextRetryTime: (retryCount: number, baseDelay: number = 30000, multiplier: number = 2): Date => {
        const delay = baseDelay * Math.pow(multiplier, retryCount);
        return new Date(Date.now() + delay);
    },

    /**
     * Check if enough time has passed since last retry
     */
    canRetryNow: (lastRetryAt: Date | null, retryCount: number, baseDelay: number = 30000): {
        canRetry: boolean;
        remainingTime?: number; // in milliseconds
    } => {
        if (!lastRetryAt) {
            return { canRetry: true };
        }

        const delay = baseDelay * Math.pow(2, retryCount);
        const timeSinceLastRetry = Date.now() - lastRetryAt.getTime();

        if (timeSinceLastRetry >= delay) {
            return { canRetry: true };
        }

        return {
            canRetry: false,
            remainingTime: delay - timeSinceLastRetry
        };
    }
};
