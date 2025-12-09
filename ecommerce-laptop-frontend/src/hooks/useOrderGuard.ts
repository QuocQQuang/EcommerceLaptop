'use client';

import { useAuth } from '@/hooks/useAuth';
import logger from '@/lib/logger';
import { orderService } from '@/services/orderService';
import { UserRoleType } from '@/types/api';
import { OrderStatusType, PaymentStatusType } from '@/types/order';
import { hasOrderAccess } from '@/utils/userUtils';
import { useRouter } from 'next/navigation';
import React, { useEffect, useState } from 'react';
import { toast } from 'sonner';

interface OrderGuardOptions {
    orderId: number;
    allowedStatuses?: OrderStatusType[];
    allowedPaymentStatuses?: PaymentStatusType[];
    allowedRoles?: UserRoleType[];
    redirectOnUnauthorized?: string;
    redirectOnInvalidStatus?: string;
    skipOwnershipCheck?: boolean;
}

interface OrderGuardResult {
    isLoading: boolean;
    isAuthorized: boolean;
    order: any | null;
    error: string | null;
}

/**
 * Custom hook for guarding order-related pages with ownership and status checks
 * 
 * @param options Configuration for the order guard
 * @returns Object containing loading state, authorization status, order data, and error info
 */
export function useOrderGuard(options: OrderGuardOptions): OrderGuardResult {
    const {
        orderId,
        allowedStatuses,
        allowedPaymentStatuses,
        allowedRoles,
        redirectOnUnauthorized = '/account/orders',
        redirectOnInvalidStatus = '/account',
        skipOwnershipCheck = false
    } = options;

    const { user, isAuthenticated, isLoading: authLoading } = useAuth();
    const router = useRouter();
    const [isLoading, setIsLoading] = useState(true);
    const [isAuthorized, setIsAuthorized] = useState(false);
    const [order, setOrder] = useState<any>(null);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        const checkOrderAccess = async () => {
            if (authLoading) return;

            try {
                setIsLoading(true);
                setError(null);

                // Check if user is authenticated
                if (!isAuthenticated || !user) {
                    logger.warn('Order Guard: User not authenticated', { orderId });
                    setError('Bn cn ng nhp  truy cp trang ny.');
                    router.push('/auth/login');
                    return;
                }

                // Check user role if specified
                if (allowedRoles && allowedRoles.length > 0) {
                    const userRole = user.role || (user.roles?.[0] as UserRoleType);
                    if (!userRole || !allowedRoles.includes(userRole)) {
                        logger.warn('Order Guard: User role not allowed', {
                            orderId,
                            userRole,
                            allowedRoles
                        });
                        setError('Bn khng c quyn truy cp trang ny.');
                        router.push(redirectOnUnauthorized);
                        return;
                    }
                }

                // Fetch order details
                const orderData = await orderService.getOrderStatus(orderId);
                setOrder(orderData);

                // Check ownership (unless skipped for admin users)
                if (!skipOwnershipCheck) {
                    const userRole = user.role || (user.roles?.[0] as UserRoleType);
                    const isAdmin = userRole === 'Admin' || userRole === 'SuperAdmin';

                    if (!isAdmin && !hasOrderAccess(user.id, orderData.userId)) {
                        logger.warn('Order Guard: Ownership check failed', {
                            orderId,
                            userId: user.id,
                            orderUserId: orderData.userId,
                            hasAccess: false
                        });
                        setError('Bn khng c quyn truy cp n hng ny.');
                        toast.error('Bn khng c quyn truy cp n hng ny.');
                        router.push(redirectOnUnauthorized);
                        return;
                    }
                }

                // Check order status if specified
                if (allowedStatuses && allowedStatuses.length > 0) {
                    if (!allowedStatuses.includes(orderData.status)) {
                        logger.warn('Order Guard: Order status not allowed', {
                            orderId,
                            currentStatus: orderData.status,
                            allowedStatuses
                        });
                        setError(`Trng thi n hng khng ph hp: ${orderData.status}`);
                        toast.warning(`n hng  ${orderData.status.toLowerCase()}.`);
                        router.push(redirectOnInvalidStatus);
                        return;
                    }
                }

                // Check payment status if specified
                if (allowedPaymentStatuses && allowedPaymentStatuses.length > 0) {
                    if (!allowedPaymentStatuses.includes(orderData.paymentStatus)) {
                        logger.warn('Order Guard: Payment status not allowed', {
                            orderId,
                            currentPaymentStatus: orderData.paymentStatus,
                            allowedPaymentStatuses
                        });
                        setError(`Trng thi thanh ton khng ph hp: ${orderData.paymentStatus}`);
                        toast.warning(`Thanh ton  ${orderData.paymentStatus.toLowerCase()}.`);
                        router.push(redirectOnInvalidStatus);
                        return;
                    }
                }

                // All checks passed
                logger.info('Order Guard: Access granted', {
                    orderId,
                    userId: user.id,
                    status: orderData.status,
                    paymentStatus: orderData.paymentStatus
                });
                setIsAuthorized(true);

            } catch (err: any) {
                logger.error('Order Guard: Error checking order access', {
                    orderId,
                    error: err.message,
                    status: err.response?.status
                });
                setError('C li xy ra khi kim tra quyn truy cp.');
                toast.error('Khng th kim tra quyn truy cp n hng.');
                router.push(redirectOnUnauthorized);
            } finally {
                setIsLoading(false);
            }
        };

        if (orderId > 0) {
            checkOrderAccess();
        }
    }, [orderId, user, isAuthenticated, authLoading, allowedStatuses, allowedPaymentStatuses, allowedRoles, skipOwnershipCheck, redirectOnUnauthorized, redirectOnInvalidStatus, router]);

    return {
        isLoading: isLoading || authLoading,
        isAuthorized,
        order,
        error
    };
}

/**
 * HOC for wrapping components with order guard functionality
 * Note: This should be moved to a separate .tsx file for JSX support
 */
export function withOrderGuard<T extends Record<string, any>>(
    WrappedComponent: React.ComponentType<T>,
    guardOptions: Omit<OrderGuardOptions, 'orderId'> & {
        getOrderId: (props: T) => number;
    }
) {
    return function OrderGuardedComponent(props: T) {
        const orderId = guardOptions.getOrderId(props);
        const { isLoading, isAuthorized, order, error } = useOrderGuard({
            ...guardOptions,
            orderId
        });

        if (isLoading) {
            return React.createElement('div',
                { className: 'min-h-screen flex items-center justify-center' },
                React.createElement('div',
                    { className: 'text-center' },
                    React.createElement('div',
                        { className: 'animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900 mx-auto' }
                    ),
                    React.createElement('p',
                        { className: 'mt-2 text-gray-600' },
                        'ang kim tra quyn truy cp...'
                    )
                )
            );
        }

        if (!isAuthorized || error) {
            return React.createElement('div',
                { className: 'min-h-screen flex items-center justify-center' },
                React.createElement('div',
                    { className: 'text-center' },
                    React.createElement('h2',
                        { className: 'text-xl font-semibold text-red-600 mb-2' },
                        'Truy cp b t chi'
                    ),
                    React.createElement('p',
                        { className: 'text-gray-600' },
                        error || 'Bn khng c quyn truy cp trang ny.'
                    )
                )
            );
        }

        return React.createElement(WrappedComponent, { ...props, order });
    };
}