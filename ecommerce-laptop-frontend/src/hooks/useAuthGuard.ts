'use client';

import { useSession } from 'next-auth/react';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';
import logger from '@/lib/logger';

export interface UseAuthGuardOptions {
    /**
     * Path to redirect to if not authenticated
     */
    redirectTo?: string;
    /**
     * Required role for access
     */
    requiredRole?: string;
    /**
     * Custom access check function
     */
    customAccessCheck?: (session: any) => boolean;
    /**
     * Whether to redirect immediately if not authenticated
     */
    redirectOnUnauthenticated?: boolean;
}

export interface UseAuthGuardReturn {
    /**
     * Whether the user is authenticated
     */
    isAuthenticated: boolean;
    /**
     * Whether we're still loading the session
     */
    isLoading: boolean;
    /**
     * Whether the user has the required access
     */
    hasAccess: boolean;
    /**
     * The current session data
     */
    session: any;
    /**
     * Function to manually redirect to login
     */
    redirectToLogin: () => void;
}

/**
 * Hook for protecting routes and checking authentication status
 * 
 * @example
 * ```tsx
 * function MyProtectedPage() {
 *   const { isAuthenticated, isLoading, hasAccess } = useAuthGuard({
 *     redirectTo: '/auth/login',
 *     requiredRole: 'admin'
 *   });
 * 
 *   if (isLoading) return <LoadingSpinner />;
 *   if (!isAuthenticated || !hasAccess) return null;
 * 
 *   return <div>Protected content</div>;
 * }
 * ```
 */
export function useAuthGuard(options: UseAuthGuardOptions = {}): UseAuthGuardReturn {
    const {
        redirectTo = '/auth/login',
        requiredRole,
        customAccessCheck,
        redirectOnUnauthenticated = true
    } = options;

    const { data: session, status } = useSession();
    const router = useRouter();

    const isLoading = status === 'loading';
    const isAuthenticated = status === 'authenticated' && !!session;

    // Check if user has required access
    const hasAccess = isAuthenticated && (() => {
        // Custom access check
        if (customAccessCheck && !customAccessCheck(session)) {
            return false;
        }

        // Role-based access check
        if (requiredRole && !session?.user?.roles?.includes(requiredRole)) {
            return false;
        }

        return true;
    })();

    const redirectToLogin = () => {
        const currentPath = window.location.pathname;
        const redirectUrl = `${redirectTo}?callbackUrl=${encodeURIComponent(currentPath)}`;
        router.replace(redirectUrl);
    };

    useEffect(() => {
        // Don't do anything while loading
        if (isLoading) {
            return;
        }

        // If not authenticated and should redirect
        if (!isAuthenticated && redirectOnUnauthenticated) {
            logger.warn(' Unauthenticated user detected, redirecting to login', {
                redirectTo,
                currentUrl: window.location.pathname
            });
            redirectToLogin();
            return;
        }

        // If authenticated but doesn't have access
        if (isAuthenticated && !hasAccess) {
            logger.warn(' User does not have required access', {
                userId: session?.user?.id,
                email: session?.user?.email,
                userRoles: session?.user?.roles,
                requiredRole
            });
            router.replace('/unauthorized');
            return;
        }

        // Log successful access
        if (isAuthenticated && hasAccess) {
            logger.debug(' User has valid access', {
                userId: session?.user?.id,
                email: session?.user?.email,
                roles: session?.user?.roles
            });
        }

    }, [isLoading, isAuthenticated, hasAccess, redirectOnUnauthenticated, redirectTo, requiredRole, router, session]);

    return {
        isAuthenticated,
        isLoading,
        hasAccess,
        session,
        redirectToLogin
    };
}

/**
 * Simplified hook for basic authentication check
 */
export function useRequireAuth(redirectTo: string = '/auth/login') {
    return useAuthGuard({ redirectTo, redirectOnUnauthenticated: true });
}

/**
 * Hook for role-based access control
 */
export function useRequireRole(role: string, redirectTo: string = '/auth/login') {
    return useAuthGuard({ 
        redirectTo, 
        requiredRole: role,
        redirectOnUnauthenticated: true 
    });
}