import type { Address } from './address';

export interface OrderItem {
    id: number;
    productId: number;
    productName: string;
    quantity: number;
    unitPrice: number;
    totalPrice: number;
    imageUrl?: string;
}

// Order Status Enums for better type safety
export const OrderStatus = {
    Pending: 'Pending',
    Confirmed: 'Confirmed',
    Processing: 'Processing',
    Shipped: 'Shipped',
    Delivered: 'Delivered',
    Cancelled: 'Cancelled',
    Returned: 'Returned'
} as const;

export const PaymentStatus = {
    Pending: 'Pending',
    Completed: 'Completed',
    Failed: 'Failed',
    Refunded: 'Refunded'
} as const;

export type OrderStatusType = typeof OrderStatus[keyof typeof OrderStatus];
export type PaymentStatusType = typeof PaymentStatus[keyof typeof PaymentStatus];

export interface OrderResponse {
    id: number;
    orderNumber: string;
    userId: number;
    customerId?: number; // API response field
    buyerId?: number; // Alias for userId - for backward compatibility
    status: OrderStatusType;
    totalAmount: number;
    subtotal: number;
    shippingFee: number;
    tax: number;
    discount: number;
    paymentStatus: PaymentStatusType;
    paymentMethod: string;
    shippingAddress: Address; // Always use Address for better validation
    items: OrderItem[];
    createdAt: string;
    updatedAt: string;
    verified?: boolean; // Security field to track order verification status
    userRole?: 'Customer' | 'Admin'; // Role context for the order
}

export interface AtomicCheckoutRequest {
    orderId?: number; // Added for redirect payments
    cartId?: number;
    customerId?: number;
    shippingAddress?: string;
    shippingCity?: string;
    shippingProvince?: string;
    shippingPostalCode?: string;
    shippingCountry?: string;
    paymentGateway: number; // Payment gateway enum: 1=VNPAY, 2=MOMO, 3=ZALOPAY, 4=SEPAY, 5=COD, 6=Stripe, 7=PayPal
    paymentMethod?: string;
    returnUrl: string;
    cancelUrl?: string;
    amount?: number;
    currency?: string;
}

export interface PaymentStatusResponse {
    status: string;
    paymentStatus: string;
}

// Error Handling Types
export interface OrderError {
    message: string;
    code: string;
    details?: any;
}

// Generic Result Type for API responses
export type OrderResult<T> =
    | { success: true; data: T }
    | { success: false; error: OrderError };

// Order Validation Types
export interface OrderValidationError {
    field: string;
    message: string;
}

export interface OrderValidationResult {
    isValid: boolean;
    errors: OrderValidationError[];
}