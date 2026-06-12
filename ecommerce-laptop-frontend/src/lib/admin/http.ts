import logger from '@/lib/logger';
import axios from 'axios';

const isDev = process.env.NODE_ENV === 'development';
const logLevel = process.env.NEXT_PUBLIC_LOG_LEVEL || 'info';

export const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5129/api';

export interface ApiResponse<T = any> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

export const unwrapApiData = <T>(raw: any): T => {
  return (raw && typeof raw === 'object' && 'data' in raw) ? raw.data as T : raw as T;
};

export const unwrapApiArray = <T>(raw: any): T[] => {
  const data = unwrapApiData<any>(raw);
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.items)) return data.items;
  if (Array.isArray(data?.data)) return data.data;
  return [];
};

export const api = axios.create({
  baseURL: API_BASE,
  headers: {
    'Content-Type': 'application/json',
  },
});

let authToken: string | null = null;
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: any) => void;
  reject: (error?: any) => void;
}> = [];

if (typeof window !== 'undefined') {
  const storedToken = localStorage.getItem('adminToken');
  if (storedToken) {
    authToken = storedToken;
    localStorage.removeItem('adminToken');
  }
}

export const tokenManager = {
  getAccessToken(): string | null {
    if (isDev && logLevel === 'debug') {
      logger.info(' GET ACCESS TOKEN:', {
        hasToken: !!authToken,
        tokenLength: authToken?.length || 0,
        tokenPrefix: authToken ? authToken.substring(0, 20) + '...' : 'null'
      });
    }
    return authToken;
  },

  setAccessToken(token: string | null): void {
    if (isDev && logLevel === 'debug') {
      logger.info(' SET ACCESS TOKEN:', {
        hasToken: !!token,
        tokenLength: token?.length || 0,
        tokenPrefix: token ? token.substring(0, 20) + '...' : 'null'
      });
    }
    authToken = token;
  },

  getRefreshToken(): string | null {
    if (typeof window === 'undefined') return null;
    const refreshToken = localStorage.getItem('adminRefreshToken');
    if (isDev && logLevel === 'debug') {
      logger.info(' GET REFRESH TOKEN:', {
        hasToken: !!refreshToken,
        tokenLength: refreshToken?.length || 0
      });
    }
    return refreshToken;
  },

  setRefreshToken(token: string | null): void {
    if (typeof window === 'undefined') return;

    if (isDev && logLevel === 'debug') {
      logger.info(' SET REFRESH TOKEN:', {
        hasToken: !!token,
        tokenLength: token?.length || 0
      });
    }

    if (token) {
      localStorage.setItem('adminRefreshToken', token);
    } else {
      localStorage.removeItem('adminRefreshToken');
    }
  },

  clearTokens(): void {
    if (isDev && logLevel === 'debug') {
      logger.info(' CLEARING ALL TOKENS');
    }
    authToken = null;
    if (typeof window !== 'undefined') {
      localStorage.removeItem('adminToken');
      localStorage.removeItem('adminRefreshToken');
      localStorage.removeItem('adminUser');
    }
  },

  isAuthenticated(): boolean {
    const hasToken = !!authToken;
    if (isDev && logLevel === 'debug') {
      logger.info(' CHECK AUTHENTICATION:', { isAuthenticated: hasToken });
    }
    return hasToken;
  },

  isTokenExpired(): boolean {
    return false;
  }
};

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) {
      reject(error);
    } else {
      resolve(token);
    }
  });

  failedQueue = [];
};

const refreshAuthToken = async (): Promise<string | null> => {
  try {
    const refreshToken = tokenManager.getRefreshToken();
    if (!refreshToken) {
      throw new Error('No refresh token available');
    }

    const response = await axios.post(`${API_BASE}/auth/refresh`, {
      refreshToken,
      context: 'admin'
    });

    if (response.data.success) {
      const { accessToken, refreshToken: newRefreshToken } = response.data.data;

      tokenManager.setAccessToken(accessToken);
      if (newRefreshToken) {
        tokenManager.setRefreshToken(newRefreshToken);
      }

      return accessToken;
    }

    throw new Error('Token refresh failed');
  } catch {
    tokenManager.clearTokens();
    return null;
  }
};

api.interceptors.request.use(
  async (config) => {
    let token = tokenManager.getAccessToken();
    if (!token && typeof window !== 'undefined') {
      try {
        const resp = await fetch('/api/admin/token', { credentials: 'include' });
        if (resp.ok) {
          const data = await resp.json();
          if (data?.success && data?.token) {
            token = data.token as string;
            tokenManager.setAccessToken(token);
          }
        }
      } catch {
        // Proceed without auth header.
      }
    }

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
      if (isDev) {
        const requestSize = config.data ? JSON.stringify(config.data).length : 0;
        const isLargeRequest = requestSize > 5000;

        logger.info(' BACKEND REQUEST:', {
          url: config.url,
          method: config.method?.toUpperCase(),
          hasAuthHeader: !!config.headers.Authorization,
          contentType: config.headers['Content-Type'],
          requestSize: requestSize ? `${requestSize} bytes` : 'no body',
          isLargeRequest,
          timestamp: new Date().toISOString(),
          requestData: isLargeRequest ?
            `Large request (${requestSize} bytes) - use network tab for details` :
            config.data
        });
      }
    } else if (isDev) {
      logger.warn(' BACKEND REQUEST (NO AUTH):', {
        url: config.url,
        method: config.method?.toUpperCase(),
        hasAuthHeader: false,
        timestamp: new Date().toISOString()
      });
    }

    return config;
  },
  (error) => {
    logger.error(' REQUEST INTERCEPTOR ERROR:', error);
    return Promise.reject(error);
  }
);

api.interceptors.response.use(
  (response) => {
    if (isDev) {
      const responseSize = JSON.stringify(response.data).length;
      const isLargeResponse = responseSize > 10000;

      logger.info(' BACKEND RESPONSE SUCCESS:', {
        url: response.config.url,
        method: response.config.method?.toUpperCase(),
        status: response.status,
        statusText: response.statusText,
        responseSize: `${responseSize} bytes`,
        isLargeResponse,
        hasAuthHeader: !!response.config.headers.Authorization,
        contentType: response.headers['content-type'],
        timestamp: new Date().toISOString(),
        responseData: isLargeResponse ?
          `Large response (${responseSize} bytes) - use network tab for details` :
          response.data
      });
    }
    return response;
  },
  async (error) => {
    const originalRequest = error.config;

    if (isDev) {
      const responseSize = error.response?.data ? JSON.stringify(error.response.data).length : 0;

      logger.error(' BACKEND RESPONSE ERROR:', {
        url: error.config?.url || 'unknown',
        method: error.config?.method?.toUpperCase() || 'unknown',
        status: error.response?.status || 'no status',
        statusText: error.response?.statusText || 'no status text',
        hasAuthHeader: !!error.config?.headers?.Authorization,
        isRetry: !!originalRequest?._retry,
        errorMessage: error.message || 'no message',
        responseSize: responseSize ? `${responseSize} bytes` : 'unknown',
        contentType: error.response?.headers?.['content-type'],
        timestamp: new Date().toISOString(),
        responseData: error.response?.data
      });
    }

    if (error.response?.status === 401 && !originalRequest._retry) {
      if (isDev && logLevel === 'debug') {
        logger.info(' 401 DETECTED - Starting token refresh flow...');
      }

      if (isRefreshing) {
        if (isDev && logLevel === 'debug') {
          logger.info(' Already refreshing - queuing request...');
        }
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then(token => {
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return api(originalRequest);
        }).catch(err => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        if (isDev && logLevel === 'debug') {
          logger.info(' Attempting token refresh...');
        }
        const newToken = await refreshAuthToken();

        if (newToken) {
          if (isDev && logLevel === 'debug') {
            logger.info(' Token refresh successful');
          }
          processQueue(null, newToken);
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return api(originalRequest);
        }

        logger.error(' Token refresh failed - redirecting to login');
        processQueue(new Error('Token refresh failed'), null);
        if (typeof window !== 'undefined') {
          window.location.href = '/admin-login';
        }
        return Promise.reject(error);
      } catch (refreshError) {
        logger.error(' Token refresh error:', refreshError);
        processQueue(refreshError, null);
        if (typeof window !== 'undefined') {
          window.location.href = '/admin-login';
        }
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
