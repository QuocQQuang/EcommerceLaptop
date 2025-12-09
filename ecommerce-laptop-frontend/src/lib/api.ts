import axios from 'axios';
import type { Session } from 'next-auth';
import { getSession, signOut } from 'next-auth/react';
import logger, { logApiError, logApiRequest, logApiResponse } from './logger';

// Cache session to avoid excessive getSession calls
let sessionCache: Session | null = null;
let lastSessionFetch = 0;
const SESSION_CACHE_TTL = 5000; // Cache for 5 seconds

// Single-flight refresh state
let isRefreshing = false;
let refreshPromise: Promise<string | null> | null = null;

async function getCachedSession() {
    const now = Date.now();
    if (sessionCache && (now - lastSessionFetch) < SESSION_CACHE_TTL) {
        return sessionCache;
    }

    sessionCache = await getSession();
    lastSessionFetch = now;
    return sessionCache;
}

// Normalize API base URL: prefer absolute URL if provided, otherwise accept relative paths
let apiBaseURL = process.env.NEXT_PUBLIC_API_URL;
if (!apiBaseURL) {
    // Default based on environment
    if (process.env.NODE_ENV === 'production') {
        // In production, use Vercel rewrite rule
        apiBaseURL = '/backend/api';
    } else {
        // In development, use direct backend URL
        apiBaseURL = 'http://localhost:5129/api';
    }
} else {
    // If user provided a value without protocol and without leading slash, prefix with '/'
    // so developers can set 'backend/api' in env and it becomes '/backend/api'
    if (!/^https?:\/\//i.test(apiBaseURL) && !apiBaseURL.startsWith('/')) {
        apiBaseURL = `/${apiBaseURL}`;
    }
}

logger.info(' Initializing API Client', {
    baseURL: apiBaseURL,
    environment: process.env.NODE_ENV
});

// Create an Axios instance with a base URL from environment variables
const apiClient = axios.create({
    baseURL: apiBaseURL,
    headers: {
        'Content-Type': 'application/json',
    },
    timeout: 30000, // 30 second timeout
});

// --- Request Interceptor ---
// This runs before each request is sent
apiClient.interceptors.request.use(
    async (config) => {
        const startTime = Date.now();
        (config as typeof config & { metadata: { startTime: number } }).metadata = { startTime };

        // Log the outgoing request
        logApiRequest(
            config.method?.toUpperCase() || 'GET',
            `${config.baseURL}${config.url}`,
            config.data,
            config.headers
        );

        // Get the current session from NextAuth.js with caching
        // Attach token for calls to our backend. Support both absolute and relative base URLs
        const isOurApi = typeof config.baseURL === 'string' && (
            config.baseURL === apiBaseURL ||
            config.baseURL.startsWith('/') ||
            (!!process.env.NEXT_PUBLIC_API_URL && config.baseURL === process.env.NEXT_PUBLIC_API_URL)
        );
        if (isOurApi) {
            const session = await getCachedSession();

            // If a session and access token exist, add the Bearer token to the Authorization header
            if (session?.accessToken) {
                config.headers.Authorization = `Bearer ${session.accessToken}`;
                logger.debug(' Added auth token to request', {
                    hasToken: !!session.accessToken,
                    tokenLength: session.accessToken?.length
                });
            } else {
                logger.warn(' No auth token available for request', {
                    hasSession: !!session,
                    url: `${config.baseURL}${config.url}`
                });
            }
        }
        return config;
    },
    (error) => {
        // Handle any request errors
        logApiError('REQUEST_SETUP', 'unknown', error);
        return Promise.reject(error);
    }
);

// --- Response Interceptor ---
// This runs after a response is received
apiClient.interceptors.response.use(
    (response) => {
        // Calculate request duration
        const startTime = (response.config as typeof response.config & { metadata?: { startTime: number } }).metadata?.startTime;
        const duration = startTime ? Date.now() - startTime : undefined;

        // Log successful response
        logApiResponse(
            response.config.method?.toUpperCase() || 'GET',
            `${response.config.baseURL}${response.config.url}`,
            response.status,
            response.data,
            duration
        );

        // If the request was successful, just return the response
        return response;
    },
    async (error) => {
        const originalRequest = error.config;

        // Calculate request duration
        const startTime = originalRequest?.metadata?.startTime;
        const duration = startTime ? Date.now() - startTime : undefined;

        // Log API error with detailed information
        logApiError(
            originalRequest?.method?.toUpperCase() || 'UNKNOWN',
            `${originalRequest?.baseURL}${originalRequest?.url}`,
            {
                ...error,
                duration,
                requestData: originalRequest?.data,
                responseData: error.response?.data
            }
        );

        // Check if the error is a 401 Unauthorized and if we haven't already retried the request
        if (error.response?.status === 401 && !originalRequest._retry) {
            originalRequest._retry = true; // Mark that we've retried this request

            try {
                logger.warn(" Token expired, attempting refresh", {
                    url: `${originalRequest?.baseURL}${originalRequest?.url}`,
                    method: originalRequest?.method?.toUpperCase()
                });

                const session = await getSession();

                // Guard: no refresh token available
                if (!session?.refreshToken) {
                    throw new Error('No refresh token available');
                }

                // Ensure single-flight refresh
                if (!isRefreshing) {
                    isRefreshing = true;
                    refreshPromise = (async () => {
                        try {
                            const resp = await axios.post(`${apiBaseURL}/auth/refresh`, {
                                refreshToken: session.refreshToken,
                                context: 'Customer'
                            });

                            // Backend returns { success, data: { accessToken, refreshToken, ... } }
                            const newAccessToken: string | undefined = resp?.data?.data?.accessToken;
                            const newRefreshToken: string | undefined = resp?.data?.data?.refreshToken;

                            if (!newAccessToken) {
                                throw new Error('Refresh response missing accessToken');
                            }

                            // Invalidate cached session so next getCachedSession pulls new token via next-auth source
                            sessionCache = null;

                            // Return the new access token for immediate retry usage
                            return newAccessToken;
                        } finally {
                            isRefreshing = false;
                        }
                    })();
                }

                const token = await refreshPromise!;
                if (!token) {
                    throw new Error('Token refresh failed');
                }

                // Retry original request with new token
                originalRequest.headers.Authorization = `Bearer ${token}`;
                return apiClient(originalRequest);

                // If refresh failed or no refresh token, sign out


            } catch (refreshError) {
                logger.error(" Exception during token refresh. Signing out.", refreshError);
                const currentPath = typeof window !== 'undefined' ? window.location.pathname : '/';
                await signOut({
                    redirect: true,
                    callbackUrl: `/auth/login?callbackUrl=${encodeURIComponent(currentPath)}`
                });
                return Promise.reject(refreshError);
            }
        }

        // Network connectivity issues
        if (!error.response) {
            logger.error(" Network Error: Cannot connect to backend", {
                message: error.message,
                code: error.code,
                baseURL: apiBaseURL,
                isNetworkError: true
            });
        }

        // For any other errors, just reject the promise
        return Promise.reject(error);
    }
);

// --- Auth API Methods ---
export const authApi = {
    /**
     * Resend email confirmation for a user
     * @param email - User email address
     * @returns Promise with success status
     */
    async resendEmailConfirmation(email: string) {
        try {
            logApiRequest('POST', '/auth/resend-confirmation', { email });
            const response = await apiClient.post('/auth/resend-confirmation', { email });
            logApiResponse('POST', '/auth/resend-confirmation', response.status, response.data);
            return response.data;
        } catch (error) {
            logApiError('POST', '/auth/resend-confirmation', error);
            throw error;
        }
    },

    /**
     * Confirm email with token
     * @param token - Email confirmation token
     * @returns Promise with confirmation result
     */
    async confirmEmail(token: string) {
        try {
            logApiRequest('GET', '/auth/confirm-email', { token });
            const response = await apiClient.get(`/auth/confirm-email?token=${encodeURIComponent(token)}`);
            logApiResponse('GET', '/auth/confirm-email', response.status, response.data);
            return response.data;
        } catch (error) {
            logApiError('GET', '/auth/confirm-email', error);
            throw error;
        }
    }
};

export default apiClient;