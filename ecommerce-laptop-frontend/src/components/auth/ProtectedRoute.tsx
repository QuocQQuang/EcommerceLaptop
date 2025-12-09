'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import logger from '@/lib/logger';
import type { Session } from 'next-auth';
import { useSession } from 'next-auth/react';
import { useRouter } from 'next/navigation';
import { ReactNode, useEffect } from 'react';

interface ProtectedRouteProps {
    children: ReactNode;
    /**
     * Custom redirect path. Defaults to '/auth/login'
     */
    redirectTo?: string;
    /**
     * Whether to show loading state while checking authentication
     */
    showLoading?: boolean;
    /**
     * Custom loading component
     */
    loadingComponent?: ReactNode;
    /**
     * Optional role-based access control
     */
    requiredRole?: string;
    /**
     * Custom access check function
     */
    customAccessCheck?: (session: Session | null) => boolean;
}

/**
 * ProtectedRoute component ensures users are authenticated before accessing content.
 * Provides client-side route protection with flexible configuration options.
 * 
 * Note: This should be used in conjunction with middleware.ts for complete protection.
 * Middleware provides server-side protection, while this component provides client-side UX.
 */
export function ProtectedRoute({
    children,
    redirectTo = '/auth/login',
    showLoading = true,
    loadingComponent,
    requiredRole,
    customAccessCheck
}: ProtectedRouteProps) {
    const { data: session, status } = useSession();
    const router = useRouter();

    useEffect(() => {
        // Don't do anything while loading
        if (status === 'loading') {
            return;
        }

        // If not authenticated, redirect to login
        if (status === 'unauthenticated') {
            logger.warn(' Unauthenticated user trying to access protected route', {
                redirectTo,
                currentUrl: window.location.pathname
            });
            
            const currentPath = window.location.pathname;
            const redirectUrl = `${redirectTo}?callbackUrl=${encodeURIComponent(currentPath)}`;
            router.replace(redirectUrl);
            return;
        }

        // If authenticated, perform additional checks
        if (status === 'authenticated' && session) {
            // Custom access check
            if (customAccessCheck && !customAccessCheck(session)) {
                logger.warn(' User failed custom access check', {
                    userId: session.user?.id,
                    email: session.user?.email
                });
                router.replace('/unauthorized');
                return;
            }

            // Role-based access control
            if (requiredRole && !session.user?.roles?.includes(requiredRole)) {
                logger.warn(' User does not have required role', {
                    userId: session.user?.id,
                    email: session.user?.email,
                    userRoles: session.user?.roles,
                    requiredRole
                });
                router.replace('/unauthorized');
                return;
            }

            logger.debug(' User authorized for protected route', {
                userId: session.user?.id,
                email: session.user?.email,
                roles: session.user?.roles
            });
        }
    }, [status, session, router, redirectTo, requiredRole, customAccessCheck]);

    // Show loading state while checking authentication
    if (status === 'loading') {
        if (!showLoading) {
            return null;
        }

        if (loadingComponent) {
            return <>{loadingComponent}</>;
        }

        return (
            <div className="flex items-center justify-center min-h-screen">
                <div className="text-center">
                    <LoadingSpinner size="lg" />
                    <p className="mt-4 text-sm text-gray-600 dark:text-gray-400">
                        ang kim tra quyn truy cp...
                    </p>
                </div>
            </div>
        );
    }

    // Don't render children until authenticated
    if (status !== 'authenticated' || !session) {
        return null;
    }

    // Render children if all checks pass
    return <>{children}</>;
}

/**
 * Higher-order component version for easier wrapping
 */
export function withProtectedRoute<P extends object>(
    Component: React.ComponentType<P>,
    options?: Omit<ProtectedRouteProps, 'children'>
) {
    return function ProtectedComponent(props: P) {
        return (
            <ProtectedRoute {...options}>
                <Component {...props} />
            </ProtectedRoute>
        );
    };
}