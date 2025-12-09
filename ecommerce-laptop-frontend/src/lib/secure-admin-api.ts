import type {
    AdminAuthRequest,
    AdminUser
} from '@/types/admin';
import axios from 'axios';

// Create API instance for admin operations with secure cookie-based auth
const secureAdminApi = axios.create({
    baseURL: '/api/admin',
    timeout: 10000,
    withCredentials: true, // Important: Include cookies in requests
    headers: {
        'Content-Type': 'application/json',
    },
});

// No need for token interceptors since we use HTTP-only cookies
secureAdminApi.interceptors.response.use(
    (response) => response,
    (error) => {
        // We'll let the calling function handle the error
        return Promise.reject(error);
    }
);

// Secure Admin Auth functions using HTTP-only cookies
export const secureAdminAuth = {
    /**
     * Login admin user with secure HTTP-only cookies
     */
    async login(credentials: AdminAuthRequest): Promise<{ user: AdminUser }> {
        try {
            const response = await secureAdminApi.post('/auth', {
                ...credentials,
            });

            if (!response.data.success) {
                throw new Error(response.data.error || 'Login failed');
            }

            return {
                user: response.data.user,
            };
        } catch (error: any) {
            console.error('Secure admin login error:', error);
            if (error.response?.data?.error === 'Ti khon b kha') {
                throw new Error('Ti khon b kha');
            }
            throw new Error(error.response?.data?.error || 'Login failed');
        }
    },

    /**
     * Get current admin user from secure cookie session
     */
    async getCurrentUser(): Promise<AdminUser> {
        try {
            const response = await secureAdminApi.get<{
                success: boolean;
                user: AdminUser;
                error?: string;
            }>('/me');

            if (!response.data.success) {
                throw new Error(response.data.error || 'Failed to get user');
            }

            return response.data.user;
        } catch (error: any) {
            console.error('Get current admin user error:', error);
            if (error.response?.status === 401) {
                throw new Error('Authentication required');
            }
            throw new Error(error.response?.data?.error || 'Failed to get user');
        }
    },

    /**
     * Logout admin user and clear secure cookies
     */
    async logout(): Promise<void> {
        try {
            await secureAdminApi.delete('/auth');
        } catch (error) {
            console.error('Secure admin logout error:', error);
            // Even if logout fails on server, redirect to login
        } finally {
            if (typeof window !== 'undefined') {
                window.location.href = '/admin-login';
            }
        }
    },

    /**
     * Check if admin is authenticated (this will make a request to verify cookie)
     */
    async isAuthenticated(): Promise<boolean> {
        try {
            await this.getCurrentUser();
            return true;
        } catch (error) {
            return false;
        }
    }
};

// Secure API client for making authenticated requests to backend
const secureBackendApi = axios.create({
    baseURL: process.env.NEXT_PUBLIC_API_URL,
    timeout: 10000,
    withCredentials: true,
});

// Secure backend API functions that work with cookie-based auth
export const secureAdminBackendApi = {
    /**
     * Make authenticated request to backend using session cookie
     */
    async authenticatedRequest<T>(config: {
        method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
        url: string;
        data?: any;
        params?: any;
    }): Promise<T> {
        // First, verify we have a valid admin session
        const isAuth = await secureAdminAuth.isAuthenticated();
        if (!isAuth) {
            throw new Error('Authentication required');
        }

        // Get the session token from our secure API to pass to backend
        const tokenResponse = await fetch('/api/admin/token', {
            credentials: 'include'
        });

        if (!tokenResponse.ok) {
            throw new Error('Failed to get session token');
        }

        const { token } = await tokenResponse.json();

        // Make authenticated request to backend
        const backendResponse = await secureBackendApi({
            method: config.method,
            url: config.url,
            data: config.data,
            params: config.params,
            headers: {
                'Authorization': `Bearer ${token}`
            }
        });

        return backendResponse.data;
    }
};

// Legacy functions for backward compatibility - these now use secure methods
export const adminAuth = {
    async login(credentials: AdminAuthRequest): Promise<any> {
        console.warn('Using legacy adminAuth.login - migrated to secure cookie-based auth');
        return secureAdminAuth.login(credentials);
    },

    async getCurrentUser(): Promise<AdminUser> {
        console.warn('Using legacy adminAuth.getCurrentUser - migrated to secure cookie-based auth');
        return secureAdminAuth.getCurrentUser();
    },

    async logout(): Promise<void> {
        console.warn('Using legacy adminAuth.logout - migrated to secure cookie-based auth');
        return secureAdminAuth.logout();
    },

    getCurrentUserFromStorage(): AdminUser | null {
        console.warn('getCurrentUserFromStorage is deprecated - use getCurrentUser() instead');
        return null; // No more client-side storage
    }
};

// Export the secure version as default
// Dashboard API functions using secure backend client
export const getDashboardMetrics = async () => {
    return secureAdminBackendApi.authenticatedRequest({
        method: 'GET',
        url: '/api/admin/dashboard/metrics'
    });
};

export const getDashboardChartData = async (type: string, days = 30) => {
    return secureAdminBackendApi.authenticatedRequest({
        method: 'GET',
        url: `/api/admin/dashboard/charts/${type}`,
        params: { days }
    });
};

// Export default for backward compatibility
export default {
    secureAdminAuth,
    secureAdminBackendApi,
    getDashboardMetrics,
    getDashboardChartData
};

// Permission constants (unchanged)
export const PERMISSIONS = {
    // User Management
    USERS_READ: 'users:read',
    USERS_WRITE: 'users:write',
    USERS_DELETE: 'users:delete',
    USERS_MANAGE: 'users:manage',

    // Role Management
    ROLES_READ: 'roles:read',
    ROLES_WRITE: 'roles:write',
    ROLES_DELETE: 'roles:delete',
    ROLES_MANAGE: 'roles:manage',

    // Product Management
    PRODUCTS_READ: 'products:read',
    PRODUCTS_WRITE: 'products:write',
    PRODUCTS_DELETE: 'products:delete',
    PRODUCTS_MANAGE: 'products:manage',

    // Order Management
    ORDERS_READ: 'orders:read',
    ORDERS_WRITE: 'orders:write',
    ORDERS_DELETE: 'orders:delete',
    ORDERS_MANAGE: 'orders:manage',

    // Promotion Management
    PROMOTIONS_READ: 'promotions:read',
    PROMOTIONS_WRITE: 'promotions:write',
    PROMOTIONS_DELETE: 'promotions:delete',
    PROMOTIONS_MANAGE: 'promotions:manage',

    // Settings Management
    SETTINGS_READ: 'settings:read',
    SETTINGS_WRITE: 'settings:write',
    SETTINGS_MANAGE: 'settings:manage',

    // Logs and Security
    LOGS_READ: 'logs:read',
    LOGS_MANAGE: 'logs:manage',
    SECURITY_READ: 'security:read',
    SECURITY_WRITE: 'security:write',
    SECURITY_MANAGE: 'security:manage',

    // Dashboard
    DASHBOARD_READ: 'dashboard:read'
} as const;

export type Permission = typeof PERMISSIONS[keyof typeof PERMISSIONS];