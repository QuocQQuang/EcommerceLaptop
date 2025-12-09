import api from '@/lib/api';
import { PaymentGateway, PaymentMethod } from '@/types/api';

export interface InitializePaymentRequest {
    orderId: number;
    gateway: PaymentGateway;
    method: PaymentMethod;
    environment: 'sandbox' | 'production';
    returnUrl?: string;
    cancelUrl?: string;
}

export interface SePayConfig {
    environment: string;
    qrBaseUrl: string;
    defaultAccount?: {
        accountNumber: string;
        bankCode: string;
        bankName: string;
        isDefault: boolean;
        description?: string;
    };
    accounts: Array<{
        accountNumber: string;
        bankCode: string;
        bankName: string;
        isDefault: boolean;
        description?: string;
    }>;
}

export interface PaymentInitializationResponse {
    isSuccess: boolean;      // Updated to match backend camelCase serialization
    paymentUrl?: string;
    qrCodeUrl?: string;
    transactionId?: string;
    bankAccount?: string;
    bankName?: string;
    description?: string;
    expiresAt?: string;
    errorMessage?: string;
    additionalData?: {
        client_secret?: string;
        payment_intent_id?: string;
        publishable_key?: string;
        [key: string]: any;
    };
}

export interface SePayQRResponse {
    orderId: string;
    qrCodeUrl: string;
    amount: number;
    bankAccount: string;
    bankCode: string;
    bankName: string;
    description: string;
    expiresAt: string;
}

export interface PayPalPaymentResponse {
    isSuccess: boolean;      // Updated to match backend camelCase serialization
    paypalOrderId?: string;
    approvalUrl?: string;
    errorMessage?: string;
}

export const paymentService = {
    /**
     * Initialize payment for an order with specific gateway and environment
     */
    initializePayment: async (request: InitializePaymentRequest): Promise<PaymentInitializationResponse> => {
        try {
            // Validate request
            if (!request.orderId || request.orderId <= 0) {
                throw new Error('Order ID is required and must be greater than 0');
            }

            if (!request.gateway) {
                throw new Error('Payment gateway is required');
            }

            const response = await api.post('/payment/initialize', request);

            // Validate response
            if (!response.data) {
                throw new Error('Invalid response from payment service');
            }

            return response.data;
        } catch (error: any) {
            console.error('Payment initialization error:', error);

            // Return structured error response
            return {
                isSuccess: false,
                errorMessage: error.response?.data?.detail ||
                    error.response?.data?.message ||
                    error.message ||
                    'Failed to initialize payment'
            };
        }
    },

    /**
     * Build SePay QR URL client-side using order data and config
     * @param order - Order details including amount and ID
     * @param config - SePay configuration from /api/sepay/config
     * @param environment - Environment (sandbox/production)
     * @returns SePayQRResponse object with generated QR data
     */
    buildSePayQRUrl: (order: any, config: SePayConfig, environment: 'sandbox' | 'production'): SePayQRResponse => {
        const account = config.defaultAccount || config.accounts[0];

        if (!account) {
            throw new Error('No bank account configuration available');
        }

        // Format description as DH{orderId} per Vietnamese convention
        const description = `DH${order.id}`;

        // Use fixed test amount for sandbox (2000 VND)
        const amount = environment === 'sandbox' ? 10000 : order.totalAmount;

        // Build official SePay QR URL: https://qr.sepay.vn/img?acc=...&bank=...&amount=...&des=...
        const qrCodeUrl = `${config.qrBaseUrl}?` +
            `acc=${encodeURIComponent(account.accountNumber)}&` +
            `bank=${encodeURIComponent(account.bankCode)}&` +
            `amount=${Math.round(amount)}&` +
            `des=${encodeURIComponent(description)}`;

        return {
            orderId: order.id.toString(),
            qrCodeUrl,
            amount,
            bankAccount: account.accountNumber,
            bankCode: account.bankCode,
            bankName: account.bankName,
            description,
            expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString() // 15 minutes expiry
        };
    },

    /**
     * Get payment status for order (if separate endpoint exists)
     */
    getPaymentStatus: async (orderId: number): Promise<{ status: string; paymentStatus: string }> => {
        const response = await api.get(`/payment/${orderId}/status`);
        return response.data;
    },

    /**
     * Create a Stripe payment intent for an order
     */
    createStripePaymentIntent: async (orderId: number, environment: 'sandbox' | 'production'): Promise<{ isSuccess: boolean, clientSecret: string, errorMessage?: string }> => {
        try {
            // Backend will handle amount and currency conversion
            // Frontend only sends order ID and gateway info
            const request = {
                orderId: orderId,
                gateway: PaymentGateway.Stripe,
                method: PaymentMethod.CreditCard,
                environment: environment,
                returnUrl: `${window.location.origin}/payment/return/stripe`,
                cancelUrl: `${window.location.origin}/checkout`
            };

            console.log(' Payment request debug:', {
                orderId,
                gateway: request.gateway,
                method: request.method
            });

            const response = await api.post('/payment/initialize', request);
            const result = response.data;

            console.log(' Payment response debug:', {
                isSuccess: result.isSuccess,
                hasAdditionalData: !!result.additionalData,
                clientSecret: result.additionalData?.client_secret,
                transactionId: result.transactionId,
                fullResponse: result
            });

            // Transform response to match expected format
            return {
                isSuccess: result.isSuccess || false,
                clientSecret: result.additionalData?.client_secret || '',
                errorMessage: result.errorMessage
            };
        } catch (error: any) {
            return {
                isSuccess: false,
                clientSecret: '',
                errorMessage: error.response?.data?.message || error.message || 'Failed to create payment intent'
            };
        }
    },

    /**
     * Capture PayPal payment after user approval
     */
    capturePayPalPayment: async (paypalOrderId: string, orderId: number): Promise<{ isSuccess: boolean, errorMessage?: string }> => {
        const response = await api.post('/payment/paypal/capture', { paypalOrderId, orderId });
        return response.data;
    },

    /**
     * Verify payment for various payment methods
     */
    verifyPayment: async (paymentMethod: string, queryString: string): Promise<any> => {
        try {
            const response = await api.post('/payment/verify', {
                paymentMethod,
                queryString
            });
            return response;
        } catch (error: any) {
            throw error;
        }
    },
};
