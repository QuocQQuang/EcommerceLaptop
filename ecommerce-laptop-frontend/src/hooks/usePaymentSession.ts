'use client';

import { useAuth } from '@/hooks/useAuth';
import logger from '@/lib/logger';
import { useEffect, useState } from 'react';

interface PaymentSessionState {
    isReady: boolean;
    isValid: boolean;
    userId: number | null;
    error: string | null;
}

/**
 * Hook for managing session state during payment flows
 * Handles session validation and user ID extraction for payment pages
 */
export function usePaymentSession() {
    const { user, isAuthenticated, isLoading } = useAuth();
    const [sessionState, setSessionState] = useState<PaymentSessionState>({
        isReady: false,
        isValid: false,
        userId: null,
        error: null
    });

    useEffect(() => {
        const validateSession = () => {
            logger.info(' Payment Session: Validating session', {
                isAuthenticated,
                isLoading,
                hasUser: !!user,
                userId: user?.id
            });

            // Still loading
            if (isLoading) {
                setSessionState({
                    isReady: false,
                    isValid: false,
                    userId: null,
                    error: null
                });
                return;
            }

            // Not authenticated - but only set error if we're sure user is not authenticated
            if (!isAuthenticated) {
                logger.warn(' Payment Session: User not authenticated', {
                    isAuthenticated,
                    hasUser: !!user,
                    hasUserId: !!user?.id
                });
                setSessionState({
                    isReady: true,
                    isValid: false,
                    userId: null,
                    error: 'Bạn cần đăng nhập để thực hiện thanh toán'
                });
                return;
            }

            // Still loading user data - don't set error yet
            if (!user || !user.id) {
                logger.info(' Payment Session: Waiting for user data', {
                    isAuthenticated,
                    hasUser: !!user,
                    hasUserId: !!user?.id
                });
                setSessionState({
                    isReady: false,
                    isValid: false,
                    userId: null,
                    error: null
                });
                return;
            }

            // Extract and validate user ID
            const userId = parseInt(user.id || '0');
            if (!userId || userId <= 0) {
                logger.error(' Payment Session: Invalid user ID', {
                    userId: user.id,
                    parsedUserId: userId
                });
                setSessionState({
                    isReady: true,
                    isValid: false,
                    userId: null,
                    error: 'Thông tin người dùng không hợp lệ'
                });
                return;
            }

            // Session is valid
            logger.info(' Payment Session: Session validated successfully', {
                userId,
                userRole: user.role
            });

            setSessionState({
                isReady: true,
                isValid: true,
                userId,
                error: null
            });
        };

        validateSession();
    }, [user, isAuthenticated, isLoading]);

    return {
        ...sessionState,
        // Helper methods
        hasOrderAccess: (orderUserId: number) => {
            return sessionState.isValid && sessionState.userId === orderUserId;
        },
        // Retry validation
        retry: () => {
            logger.info(' Payment Session: Retrying validation');
            setSessionState(prev => ({ ...prev, isReady: false }));
        }
    };
}
