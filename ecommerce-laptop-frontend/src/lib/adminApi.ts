import axios, { AxiosError, AxiosInstance, AxiosResponse } from 'axios';
import { tokenManager } from './admin-api';

// Import the refresh function from admin-api.ts  
const refreshAuthToken = async (): Promise<string | null> => {
    try {
        const refreshToken = tokenManager.getRefreshToken();
        if (!refreshToken) {
            throw new Error('No refresh token available');
        }

        const apiBaseURL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5129/api';
        const response = await axios.post(`${apiBaseURL}/auth/refresh`, {
            refreshToken,
            context: 'admin'
        });

        if (response.data.success) {
            const { accessToken, refreshToken: newRefreshToken } = response.data.data;

            // Use secure token manager
            tokenManager.setAccessToken(accessToken);
            if (newRefreshToken) {
                tokenManager.setRefreshToken(newRefreshToken);
            }

            return accessToken;
        } else {
            throw new Error('Token refresh failed');
        }
    } catch (error) {
        // Refresh failed, clear all tokens securely
        tokenManager.clearTokens();
        return null;
    }
};

/**
 * Admin API Client for secure admin operations
 * Handles JWT Bearer token authentication and admin-specific error handling
 * Uses the same tokenManager as admin-api.ts for consistency
 */
export class AdminApiClient {
    private instance: AxiosInstance;

    constructor() {
        // Get API base URL from environment
        const apiBaseURL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5129/api';

        console.log(' Initializing Admin API Client', {
            baseURL: apiBaseURL,
            environment: process.env.NODE_ENV
        });

        this.instance = axios.create({
            baseURL: apiBaseURL,
            headers: {
                'Content-Type': 'application/json',
            },
            timeout: 30000, // 30 second timeout
        });

        this.setupInterceptors();
    }

    private setupInterceptors() {
        // Request interceptor - Add admin authentication using tokenManager
        this.instance.interceptors.request.use(
            (config) => {
                try {
                    const token = tokenManager.getAccessToken();

                    if (token) {
                        config.headers.Authorization = `Bearer ${token}`;

                        console.log(` [AdminAPI-Fixed] ${config.method?.toUpperCase()} ${config.url}`, {
                            hasToken: true,
                            tokenPrefix: token.substring(0, 20) + '...',
                            timestamp: new Date().toISOString()
                        });
                    } else {
                        console.warn(' [AdminAPI-Fixed] No token found for request:', config.url);
                    }
                } catch (error) {
                    console.error(' [AdminAPI-Fixed] Token retrieval error:', error);
                }

                return config;
            },
            (error) => {
                console.error(' [AdminAPI-Fixed] Request interceptor error:', error);
                return Promise.reject(error);
            }
        );

        // Response interceptor - Handle admin-specific errors
        this.instance.interceptors.response.use(
            (response: AxiosResponse) => {
                // Log successful admin API responses
                console.log(` [AdminAPI] ${response.status} ${response.config.method?.toUpperCase()} ${response.config.url}`, {
                    status: response.status,
                    dataSize: JSON.stringify(response.data).length
                });
                return response;
            },
            async (error: AxiosError) => {
                const { response, config } = error;

                // Handle admin authentication errors - use tokenManager refresh
                if (response?.status === 401) {
                    console.warn(' [AdminAPI] Token refresh needed for:', config?.url);

                    try {
                        // Try to refresh the token using refreshAuthToken
                        const newToken = await refreshAuthToken();
                        if (newToken && config) {
                            // Retry the original request with new token
                            config.headers.Authorization = `Bearer ${newToken}`;
                            console.log(' [AdminAPI] Token refreshed, retrying request:', config.url);
                            return this.instance.request(config);
                        }
                    } catch (refreshError) {
                        console.error(' [AdminAPI] Token refresh failed for', config?.url, refreshError);
                        // Clear tokens and redirect to login
                        tokenManager.clearTokens();
                        window.location.href = '/admin-login';
                        return Promise.reject(new Error('Admin session expired. Please login again.'));
                    }

                    console.error(' [AdminAPI] Token refresh failed - no new token available');
                    return Promise.reject(new Error('Admin session expired. Please login again.'));
                }

                // Log non-401 errors immediately (these are actual failures)
                console.error(` [AdminAPI] ${response?.status} ${config?.method?.toUpperCase()} ${config?.url}`, {
                    status: response?.status,
                    statusText: response?.statusText,
                    data: response?.data
                });

                // Handle admin permission errors
                if (response?.status === 403) {
                    const errorData = response?.data as any;
                    const requiredPermission = errorData?.requiredPermission;

                    console.warn(' [AdminAPI] Forbidden - Insufficient admin permissions', {
                        requiredPermission,
                        endpoint: config?.url
                    });

                    return Promise.reject(new Error(
                        `Insufficient permissions. Required: ${requiredPermission || 'admin privileges'}`
                    ));
                }

                // Handle rate limiting for admin endpoints
                if (response?.status === 429) {
                    const retryAfter = response.headers['retry-after'] || 60;
                    console.warn(` [AdminAPI] Rate limited - Retry after ${retryAfter}s`);

                    return Promise.reject(new Error(
                        `Admin rate limit exceeded. Please wait ${retryAfter} seconds before retrying.`
                    ));
                }

                // Handle validation errors
                if (response?.status === 400) {
                    const errorData = response?.data as any;
                    if (errorData?.validationErrors) {
                        return Promise.reject(new Error(`Validation failed: ${JSON.stringify(errorData.validationErrors)}`));
                    }
                }

                // Handle server errors
                if (response?.status && response.status >= 500) {
                    console.error(' [AdminAPI] Server error detected', {
                        status: response.status,
                        url: config?.url
                    });
                    return Promise.reject(new Error('Server error. Please try again later or contact support.'));
                }

                return Promise.reject(error);
            }
        );
    }

    // Expose standard HTTP methods
    public get<T = any>(url: string, config?: any): Promise<AxiosResponse<T>> {
        return this.instance.get(url, config);
    }

    public post<T = any>(url: string, data?: any, config?: any): Promise<AxiosResponse<T>> {
        return this.instance.post(url, data, config);
    }

    public put<T = any>(url: string, data?: any, config?: any): Promise<AxiosResponse<T>> {
        return this.instance.put(url, data, config);
    }

    public delete<T = any>(url: string, config?: any): Promise<AxiosResponse<T>> {
        return this.instance.delete(url, config);
    }

    public patch<T = any>(url: string, data?: any, config?: any): Promise<AxiosResponse<T>> {
        return this.instance.patch(url, data, config);
    }

    // Check if user has admin access using token-based auth
    public async hasAdminAccess(): Promise<boolean> {
        try {
            // Prefer local Next API which forwards secure cookies
            const localResp = await fetch('/api/admin/me', { credentials: 'include' });
            if (localResp.ok) {
                const json = await localResp.json();
                const user = json?.user || json?.data || {};
                const roles: string[] = user.roles || [];
                return !!(roles.includes('Admin') || roles.includes('admin') || roles.includes('SystemAdmin'));
            }

            // Fallback: use backend with in-memory token
            if (!tokenManager.isAuthenticated()) return false;
            const response = await this.get('/auth/me');
            const user = response.data?.data?.user || response.data?.data || response.data;
            const roles: string[] = user?.roles || [];
            return !!(roles.includes('Admin') || roles.includes('admin') || roles.includes('SystemAdmin'));
        } catch (error) {
            console.error(' [AdminAPI-Fixed] Error checking admin access:', error);
            return false;
        }
    }

    // Get current admin user info from API
    public async getAdminUser(): Promise<any> {
        try {
            // Prefer local Next API so HTTP-only cookie is forwarded
            const localResp = await fetch('/api/admin/me', { credentials: 'include' });
            if (localResp.ok) {
                const json = await localResp.json();
                return json?.user || json?.data || json || null;
            }

            // Fallback: backend call with in-memory token via interceptor
            if (!tokenManager.isAuthenticated()) return null;
            const response = await this.get('/auth/me');
            return response.data?.data?.user || response.data?.data || null;
        } catch (error) {
            console.error(' [AdminAPI-Fixed] Error getting admin user:', error);
            return null;
        }
    }
}

// Create singleton instance
export const adminApiClient = new AdminApiClient();

// Default export for convenience
export default adminApiClient;