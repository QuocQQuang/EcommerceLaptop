'use client';

import { paymentService } from '@/services/paymentService';
import { PaymentGateway, PaymentMethod } from '@/types/api';
import React, { createContext, useCallback, useContext, useMemo, useReducer } from 'react';
import { toast } from 'sonner';

// Payment State Types
export interface PaymentState {
    // Current payment session
    currentPayment: {
        orderId: number | null;
        gateway: PaymentGateway | null;
        method: PaymentMethod | null;
        amount: number;
        currency: string;
        status: 'idle' | 'initializing' | 'processing' | 'completed' | 'failed' | 'cancelled';
        transactionId: string | null;
        error: string | null;
    };

    // Available options
    availableGateways: PaymentGateway[];
    availableMethods: PaymentMethod[];

    // UI state
    isLoading: boolean;
    retryCount: number;
    lastRetryAt: Date | null;

    // Configuration
    config: {
        maxRetries: number;
        retryDelay: number; // in milliseconds
        pollingInterval: number; // in milliseconds
        timeout: number; // in milliseconds
    };
}

// Payment Actions
export type PaymentAction =
    | { type: 'INITIALIZE_PAYMENT'; payload: { orderId: number; gateway: PaymentGateway; method: PaymentMethod; amount: number; currency: string } }
    | { type: 'PAYMENT_INITIALIZED'; payload: { transactionId: string; status: string } }
    | { type: 'PAYMENT_PROCESSING' }
    | { type: 'PAYMENT_COMPLETED' }
    | { type: 'PAYMENT_FAILED'; payload: { error: string } }
    | { type: 'PAYMENT_CANCELLED' }
    | { type: 'RESET_PAYMENT' }
    | { type: 'SET_LOADING'; payload: boolean }
    | { type: 'SET_AVAILABLE_OPTIONS'; payload: { gateways: PaymentGateway[]; methods: PaymentMethod[] } }
    | { type: 'INCREMENT_RETRY' }
    | { type: 'RESET_RETRY' };

// Initial state
const initialState: PaymentState = {
    currentPayment: {
        orderId: null,
        gateway: null,
        method: null,
        amount: 0,
        currency: 'VND',
        status: 'idle',
        transactionId: null,
        error: null,
    },
    availableGateways: [],
    availableMethods: [],
    isLoading: false,
    retryCount: 0,
    lastRetryAt: null,
    config: {
        maxRetries: 3,
        retryDelay: 5000, // 5 seconds
        pollingInterval: 3000, // 3 seconds
        timeout: 300000, // 5 minutes
    },
};

// Reducer
function paymentReducer(state: PaymentState, action: PaymentAction): PaymentState {
    switch (action.type) {
        case 'INITIALIZE_PAYMENT':
            return {
                ...state,
                currentPayment: {
                    ...action.payload,
                    status: 'initializing',
                    transactionId: null,
                    error: null,
                },
                isLoading: true,
            };

        case 'PAYMENT_INITIALIZED':
            return {
                ...state,
                currentPayment: {
                    ...state.currentPayment,
                    status: 'processing',
                    transactionId: action.payload.transactionId,
                },
                isLoading: false,
            };

        case 'PAYMENT_PROCESSING':
            return {
                ...state,
                currentPayment: {
                    ...state.currentPayment,
                    status: 'processing',
                },
            };

        case 'PAYMENT_COMPLETED':
            return {
                ...state,
                currentPayment: {
                    ...state.currentPayment,
                    status: 'completed',
                },
                isLoading: false,
                retryCount: 0,
                lastRetryAt: null,
            };

        case 'PAYMENT_FAILED':
            return {
                ...state,
                currentPayment: {
                    ...state.currentPayment,
                    status: 'failed',
                    error: action.payload.error,
                },
                isLoading: false,
            };

        case 'PAYMENT_CANCELLED':
            return {
                ...state,
                currentPayment: {
                    ...state.currentPayment,
                    status: 'cancelled',
                },
                isLoading: false,
            };

        case 'RESET_PAYMENT':
            return {
                ...state,
                currentPayment: {
                    orderId: null,
                    gateway: null,
                    method: null,
                    amount: 0,
                    currency: 'VND',
                    status: 'idle',
                    transactionId: null,
                    error: null,
                },
                retryCount: 0,
                lastRetryAt: null,
            };

        case 'SET_LOADING':
            return {
                ...state,
                isLoading: action.payload,
            };

        case 'SET_AVAILABLE_OPTIONS':
            return {
                ...state,
                availableGateways: action.payload.gateways,
                availableMethods: action.payload.methods,
            };

        case 'INCREMENT_RETRY':
            return {
                ...state,
                retryCount: state.retryCount + 1,
                lastRetryAt: new Date(),
            };

        case 'RESET_RETRY':
            return {
                ...state,
                retryCount: 0,
                lastRetryAt: null,
            };

        default:
            return state;
    }
}

// Context
const PaymentContext = createContext<{
    state: PaymentState;
    actions: {
        initializePayment: (orderId: number, gateway: PaymentGateway, method: PaymentMethod, amount: number, currency: string) => Promise<any>;
        retryPayment: () => Promise<void>;
        cancelPayment: () => void;
        resetPayment: () => void;
        loadAvailableOptions: (amount: number, currency: string) => Promise<void>;
    };
} | null>(null);

// Provider
export function PaymentProvider({ children }: { children: React.ReactNode }) {
    const [state, dispatch] = useReducer(paymentReducer, initialState);

    // Initialize payment
    const initializePayment = useCallback(async (
        orderId: number,
        gateway: PaymentGateway,
        method: PaymentMethod,
        amount: number,
        currency: string
    ) => {
        try {
            dispatch({ type: 'INITIALIZE_PAYMENT', payload: { orderId, gateway, method, amount, currency } });

            console.log('Initializing payment with:', {
                orderId,
                gateway,
                method,
                amount,
                currency,
                environment: 'sandbox'
            });

            const response = await paymentService.initializePayment({
                orderId,
                gateway: gateway as any,
                method: method as any,
                environment: 'sandbox', // Use sandbox for testing
                returnUrl: `${window.location.origin}/payment/success`,
                cancelUrl: `${window.location.origin}/payment/cancel`,
            });

            console.log('Payment initialization response:', response);

            if (response.isSuccess) {
                dispatch({
                    type: 'PAYMENT_INITIALIZED',
                    payload: {
                        transactionId: response.transactionId || '',
                        status: 'processing'
                    }
                });

                // Handle different payment types
                if (response.paymentUrl && response.paymentUrl.trim() !== '') {
                    // Check if this is PayPal payment
                    if (gateway === PaymentGateway.PayPal) {
                        // Open PayPal in new window
                        console.log('Opening PayPal in new window:', response.paymentUrl);
                        const paypalWindow = window.open(
                            response.paymentUrl,
                            'paypal_payment',
                            'width=800,height=600,scrollbars=yes,resizable=yes'
                        );

                        if (!paypalWindow) {
                            toast.error('Không thể mở cửa sổ PayPal. Vui lòng cho phép popup và thử lại.');
                        } else {
                            toast.success('Đã mở cửa sổ PayPal. Vui lòng hoàn thành thanh toán trong cửa sổ mới.');
                        }
                    } else {
                        // Redirect to other external payment gateways (VNPay)
                        console.log('Redirecting to payment URL:', response.paymentUrl);
                        window.location.href = response.paymentUrl;
                    }
                } else if (response.qrCodeUrl && response.qrCodeUrl.trim() !== '') {
                    // Show QR code for SePay (direct qrCodeUrl)
                    toast.success('Mã QR đã được tạo. Vui lòng quét mã để thanh toán.');
                } else if (response.additionalData?.qrCodeUrl && response.additionalData.qrCodeUrl.trim() !== '') {
                    // Handle SePay payment with QR code in additionalData - redirect to SePay page
                    console.log('SePay QR code detected in additionalData:', response.additionalData.qrCodeUrl);
                    const sepayUrl = `/payment/sepay?orderId=${orderId}&env=sandbox`;
                    console.log('Redirecting to SePay page:', sepayUrl);
                    window.location.href = sepayUrl;
                } else if (response.additionalData?.client_secret) {
                    // Handle Stripe payment - redirect to Stripe page
                    const stripeUrl = `/payment/stripe?orderId=${orderId}&env=sandbox`;
                    console.log('Redirecting to Stripe page:', stripeUrl);
                    window.location.href = stripeUrl;
                } else {
                    // No payment URL or QR code - this might be an issue
                    console.error('No payment URL or QR code received:', response);
                    toast.error('Không nhận được liên kết thanh toán. Vui lòng thử lại.');
                }

                // Return response for caller to handle
                return response;
            } else {
                dispatch({
                    type: 'PAYMENT_FAILED',
                    payload: { error: response.errorMessage || 'Không thể khởi tạo thanh toán' }
                });
                toast.error(response.errorMessage || 'Không thể khởi tạo thanh toán');
                return response;
            }
        } catch (error: any) {
            dispatch({
                type: 'PAYMENT_FAILED',
                payload: { error: error.message || 'Có lỗi xảy ra khi khởi tạo thanh toán' }
            });
            toast.error('Có lỗi xảy ra khi khởi tạo thanh toán');
            return { isSuccess: false, errorMessage: error.message || 'Có lỗi xảy ra khi khởi tạo thanh toán' };
        }
    }, []);

    // Retry payment
    const retryPayment = useCallback(async () => {
        if (!state.currentPayment.orderId || !state.currentPayment.gateway || !state.currentPayment.method) {
            toast.error('Không có thông tin thanh toán để thử lại');
            return;
        }

        if (state.retryCount >= state.config.maxRetries) {
            toast.error('Đã vượt quá số lần thử lại cho phép');
            return;
        }

        const timeSinceLastRetry = state.lastRetryAt
            ? Date.now() - state.lastRetryAt.getTime()
            : Infinity;

        if (timeSinceLastRetry < state.config.retryDelay) {
            const remainingTime = Math.ceil((state.config.retryDelay - timeSinceLastRetry) / 1000);
            toast.error(`Vui lòng đợi ${remainingTime} giây trước khi thử lại`);
            return;
        }

        dispatch({ type: 'INCREMENT_RETRY' });

        await initializePayment(
            state.currentPayment.orderId,
            state.currentPayment.gateway,
            state.currentPayment.method,
            state.currentPayment.amount,
            state.currentPayment.currency
        );
    }, [state, initializePayment]);

    // Cancel payment
    const cancelPayment = useCallback(() => {
        dispatch({ type: 'PAYMENT_CANCELLED' });
        toast.info('Đã hủy thanh toán');
    }, []);

    // Reset payment
    const resetPayment = useCallback(() => {
        dispatch({ type: 'RESET_PAYMENT' });
    }, []);

    // Load available payment options
    const loadAvailableOptions = useCallback(async (amount: number, currency: string) => {
        try {
            dispatch({ type: 'SET_LOADING', payload: true });

            // This would typically call an API to get available options
            // For retry modal, only show PayPal and Stripe (removed VNPay and SePay)
            const gateways: PaymentGateway[] = [
                PaymentGateway.PayPal,
                PaymentGateway.Stripe,
            ];

            const methods: PaymentMethod[] = [
                PaymentMethod.CreditCard,
                PaymentMethod.DebitCard,
                PaymentMethod.EWallet,
                PaymentMethod.BankTransfer,
                PaymentMethod.QRCode,
                PaymentMethod.Installment,
                PaymentMethod.CashOnDelivery,
            ];

            dispatch({
                type: 'SET_AVAILABLE_OPTIONS',
                payload: { gateways, methods }
            });
        } catch (error) {
            toast.error('Không thể tải danh sách phương thức thanh toán');
        } finally {
            dispatch({ type: 'SET_LOADING', payload: false });
        }
    }, []);

    const actions = useMemo(() => ({
        initializePayment,
        retryPayment,
        cancelPayment,
        resetPayment,
        loadAvailableOptions,
    }), [initializePayment, retryPayment, cancelPayment, resetPayment, loadAvailableOptions]);

    return (
        <PaymentContext.Provider value={{ state, actions }}>
            {children}
        </PaymentContext.Provider>
    );
}

// Hook
export function usePayment() {
    const context = useContext(PaymentContext);
    if (!context) {
        throw new Error('usePayment must be used within a PaymentProvider');
    }
    return context;
}
