// Admin API Utilities
// Integration with backend JWT authentication and RESTful endpoints

import logger from '@/lib/logger';
import {
  AdminAuthRequest,
  AdminUser,
  CreateRoleRequest,
  Permission,
  Role,
  SecurityEvent,
  UpdateRoleRequest
} from '@/types/admin';
import { AdminAuditLog, LogStats } from '@/types/admin/logs';
import { Order, Product } from '@/types/api';
import axios from 'axios';

// Environment and logging configuration
const isDev = process.env.NODE_ENV === 'development';
const logLevel = process.env.NEXT_PUBLIC_LOG_LEVEL || 'info';

// =====================================================
// API Response Types
// =====================================================

export interface ApiResponse<T = any> {
  success: boolean;
  data: T;
  message?: string;
  errors?: string[];
}

const unwrapApiData = <T>(raw: any): T => {
  return (raw && typeof raw === 'object' && 'data' in raw) ? raw.data as T : raw as T;
};

const unwrapApiArray = <T>(raw: any): T[] => {
  const data = unwrapApiData<any>(raw);
  if (Array.isArray(data)) return data;
  if (Array.isArray(data?.items)) return data.items;
  if (Array.isArray(data?.data)) return data.data;
  return [];
};

export interface PaginatedResponse<T> {
  items: T[];
  totalItems: number;
  totalPages: number;
  currentPage: number;
  pageSize: number;
  hasNextPage: boolean;
  hasPreviousPage: boolean;
}

export interface UserListResponse {
  users: AdminUser[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
  pageSize: number;
}

export interface DashboardMetrics {
  totalUsers: number;
  totalOrders: number;
  totalRevenue: number;
  totalProducts: number;
  recentOrders: Array<{
    id: string;
    customerName: string;
    total: number;
    status: string;
    createdAt: string;
  }>;
  revenueChart: Array<{
    date: string;
    revenue: number;
  }>;
}

export interface ChartDataPoint {
  date: string;
  value: number;
  label?: string;
}

export interface LogEntry {
  id: string;
  timestamp: string;
  level: string;
  message: string;
  module?: string;
  userId?: string;
  adminUserId?: string;
  ipAddress?: string;
  userAgent?: string;
  data?: Record<string, any>;
}

export interface LogsResponse {
  logs: LogEntry[];
  pagination: {
    page: number;
    limit: number;
    total: number;
    totalPages: number;
  };
  filters: {
    level?: string;
    module?: string;
    startDate?: string;
    endDate?: string;
  };
}

export interface SystemSettings {
  siteName: string;
  siteUrl: string;
  adminEmail: string;
  emailSettings: {
    smtpHost: string;
    smtpPort: number;
    smtpUser: string;
    smtpPassword: string;
    fromEmail: string;
    fromName: string;
  };
  paymentGateways: {
    vnpay: {
      enabled: boolean;
      merchantId: string;
      secretKey: string;
    };
    stripe: {
      enabled: boolean;
      publicKey: string;
      secretKey: string;
    };
    paypal: {
      enabled: boolean;
      clientId: string;
      clientSecret: string;
    };
  };
  features: {
    allowRegistration: boolean;
    requireEmailVerification: boolean;
    enableNotifications: boolean;
  };
}

export const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5129/api';

// Create axios instance with default config
export const api = axios.create({
  baseURL: API_BASE,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Secure Token management with memory-only access token
let authToken: string | null = null; // Memory-only storage for access token
let isRefreshing = false;
let failedQueue: Array<{
  resolve: (value?: any) => void;
  reject: (error?: any) => void;
}> = [];

// Initialize from secure storage on app start
if (typeof window !== 'undefined') {
  // Only read from localStorage on initial load, then clear it
  const storedToken = localStorage.getItem('adminToken');
  if (storedToken) {
    authToken = storedToken;
    // Move to memory-only storage for security
    localStorage.removeItem('adminToken');
  }
}

// Secure token management utilities with debugging
export const tokenManager = {
  // Get current access token (memory-only)
  getAccessToken(): string | null {
    // Reduce token logging noise - only log in debug mode
    if (isDev && logLevel === 'debug') {
      logger.info(' GET ACCESS TOKEN:', {
        hasToken: !!authToken,
        tokenLength: authToken?.length || 0,
        tokenPrefix: authToken ? authToken.substring(0, 20) + '...' : 'null'
      });
    }
    return authToken;
  },

  // Set access token (memory-only)
  setAccessToken(token: string | null): void {
    // Reduce token logging noise - only log in debug mode
    if (isDev && logLevel === 'debug') {
      logger.info(' SET ACCESS TOKEN:', {
        hasToken: !!token,
        tokenLength: token?.length || 0,
        tokenPrefix: token ? token.substring(0, 20) + '...' : 'null'
      });
    }
    authToken = token;
    // Never store access token in localStorage for security
  },

  // Get refresh token (from localStorage - less sensitive)
  getRefreshToken(): string | null {
    if (typeof window === 'undefined') return null;
    const refreshToken = localStorage.getItem('adminRefreshToken');
    // Reduce token logging noise - only log in debug mode
    if (isDev && logLevel === 'debug') {
      logger.info(' GET REFRESH TOKEN:', {
        hasToken: !!refreshToken,
        tokenLength: refreshToken?.length || 0
      });
    }
    return refreshToken;
  },

  // Set refresh token (localStorage is acceptable for refresh tokens)
  setRefreshToken(token: string | null): void {
    if (typeof window === 'undefined') return;

    // Reduce token logging noise - only log in debug mode
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

  // Clear all tokens
  clearTokens(): void {
    // Reduce token logging noise - only log in debug mode
    if (isDev && logLevel === 'debug') {
      logger.info(' CLEARING ALL TOKENS');
    }
    authToken = null;
    if (typeof window !== 'undefined') {
      localStorage.removeItem('adminToken'); // Just in case
      localStorage.removeItem('adminRefreshToken');
      localStorage.removeItem('adminUser');
    }
  },

  // Check if user is authenticated
  isAuthenticated(): boolean {
    const hasToken = !!authToken;
    // Reduce auth check logging noise - only log in debug mode
    if (isDev && logLevel === 'debug') {
      logger.info(' CHECK AUTHENTICATION:', { isAuthenticated: hasToken });
    }
    return hasToken;
  },

  // Get token expiration status
  isTokenExpired(): boolean {
    // For now, let the interceptor handle token validation
    // In future, we could decode JWT and check exp claim
    return false;
  }
};

// Process queued requests after successful refresh
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

// Refresh token function with secure storage
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

// Request interceptor to add auth token (secure) with debugging
api.interceptors.request.use(
  async (config) => {
    let token = tokenManager.getAccessToken();
    // If no in-memory token, try to fetch from HTTP-only cookie via local API route
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
        // ignore; will proceed without auth header
      }
    }

    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
      // Log all requests to backend using structured logger
      if (isDev) {
        const requestSize = config.data ? JSON.stringify(config.data).length : 0;
        const isLargeRequest = requestSize > 5000; // > 5KB

        logger.info(' BACKEND REQUEST:', {
          url: config.url,
          method: config.method?.toUpperCase(),
          hasAuthHeader: !!config.headers.Authorization,
          contentType: config.headers['Content-Type'],
          requestSize: requestSize ? `${requestSize} bytes` : 'no body',
          isLargeRequest,
          timestamp: new Date().toISOString(),
          // Include request data for small requests, summary for large ones
          requestData: isLargeRequest ?
            `Large request (${requestSize} bytes) - use network tab for details` :
            config.data
        });
      }
    } else {
      // Log requests without auth token
      if (isDev) {
        logger.warn(' BACKEND REQUEST (NO AUTH):', {
          url: config.url,
          method: config.method?.toUpperCase(),
          hasAuthHeader: false,
          timestamp: new Date().toISOString()
        });
      }
    }
    return config;
  },
  (error) => {
    logger.error(' REQUEST INTERCEPTOR ERROR:', error);
    return Promise.reject(error);
  }
);

// Response interceptor with refresh token logic and debugging
api.interceptors.response.use(
  (response) => {
    // Log all backend responses using structured logger
    if (isDev) {
      const responseSize = JSON.stringify(response.data).length;
      const isLargeResponse = responseSize > 10000; // > 10KB

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
        // Include response data for small responses, summary for large ones
        responseData: isLargeResponse ?
          `Large response (${responseSize} bytes) - use network tab for details` :
          response.data
      });
    }
    return response;
  },
  async (error) => {
    const originalRequest = error.config;

    // Log all backend response errors using structured logger
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
      // Reduce 401 logging noise - only log in debug mode
      if (isDev && logLevel === 'debug') {
        logger.info(' 401 DETECTED - Starting token refresh flow...');
      }

      if (isRefreshing) {
        // Reduce queue logging noise - only log in debug mode
        if (isDev && logLevel === 'debug') {
          logger.info(' Already refreshing - queuing request...');
        }
        // If already refreshing, queue this request
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then(token => {
          originalRequest.headers.Authorization = `Bearer ${token}`;
          return api(originalRequest);
        }).catch(err => {
          return Promise.reject(err);
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try {
        // Reduce refresh attempt logging noise - only log in debug mode
        if (isDev && logLevel === 'debug') {
          logger.info(' Attempting token refresh...');
        }
        const newToken = await refreshAuthToken();

        if (newToken) {
          // Reduce success logging noise - only log in debug mode
          if (isDev && logLevel === 'debug') {
            logger.info(' Token refresh successful');
          }
          // Token refreshed successfully
          processQueue(null, newToken);
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return api(originalRequest);
        } else {
          logger.error(' Token refresh failed - redirecting to login');
          // Refresh failed, redirect to login
          processQueue(new Error('Token refresh failed'), null);
          if (typeof window !== 'undefined') {
            window.location.href = '/admin-login';
          }
          return Promise.reject(error);
        }
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

// Auth functions
export const adminAuth = {
  async login(credentials: AdminAuthRequest): Promise<any> {
    // Use local Next.js API to set secure HTTP-only cookies
    const resp = await fetch('/api/admin/auth', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      credentials: 'include',
      body: JSON.stringify(credentials)
    });

    if (!resp.ok) {
      throw new Error('Login failed');
    }

    const { user: rawUser } = await resp.json();

    // Load access token from cookie into memory for axios
    try {
      const tokenResp = await fetch('/api/admin/token', { credentials: 'include' });
      if (tokenResp.ok) {
        const tokenData = await tokenResp.json();
        if (tokenData?.success && tokenData?.token) {
          tokenManager.setAccessToken(tokenData.token);
        }
      }
    } catch { }

    // Transform backend UnifiedUserDto to frontend AdminUser format
    const user: AdminUser = {
      id: rawUser.id,
      firstName: rawUser.firstName,
      lastName: rawUser.lastName,
      fullName: `${rawUser.firstName} ${rawUser.lastName}`.trim(),
      email: rawUser.email,
      isActive: rawUser.isActive || true,
      roleId: rawUser.roleId || 0,
      roleName: rawUser.roleName || rawUser.primaryRole?.name || rawUser.roles?.[0]?.name || 'User',
      createdAt: rawUser.createdAt || new Date().toISOString(),
      updatedAt: rawUser.updatedAt || new Date().toISOString(),
      permissions: (rawUser.permissions || []).map((p: any) => p.name || p)
    };

    // Use secure token storage with debugging
    if (logLevel === 'debug') {
      logger.info(' LOGIN SUCCESS - Storing tokens:', {
        accessTokenLength: tokenManager.getAccessToken()?.length || 0,
        refreshTokenLength: 0,
        userEmail: user.email,
        permissions: user.permissions?.length || 0
      });
    }

    if (typeof window !== 'undefined') {
      localStorage.setItem('adminUser', JSON.stringify(user));
    }

    return { token: tokenManager.getAccessToken(), user };
  },

  async getCurrentUser(): Promise<AdminUser> {
    // Prefer local Next API so secure cookie is forwarded; fallback to backend
    try {
      const localResp = await fetch('/api/admin/me', { credentials: 'include' });
      if (localResp.ok) {
        const json = await localResp.json();
        const userData = json?.user || json?.data || json;

        return {
          id: userData.id,
          firstName: userData.firstName,
          lastName: userData.lastName,
          fullName: `${userData.firstName} ${userData.lastName}`.trim(),
          email: userData.email,
          isActive: userData.isActive || true,
          roleId: userData.roleId || 0,
          roleName: userData.roleName || userData.primaryRole?.name || userData.roles?.[0]?.name || 'User',
          createdAt: userData.createdAt || new Date().toISOString(),
          updatedAt: userData.updatedAt || new Date().toISOString(),
          permissions: (userData.permissions || []).map((p: any) => p.name || p)
        };
      }
    } catch {
      // ignore and fallback
    }

    const response = await api.get<{ success: boolean, data: any }>('/auth/me?context=admin');
    const userData = response.data.success ? response.data.data : response.data;

    return {
      id: userData.id,
      firstName: userData.firstName,
      lastName: userData.lastName,
      fullName: `${userData.firstName} ${userData.lastName}`.trim(),
      email: userData.email,
      isActive: userData.isActive || true,
      roleId: userData.roleId || 0,
      roleName: userData.roleName || userData.primaryRole?.name || userData.roles?.[0]?.name || 'User',
      createdAt: userData.createdAt || new Date().toISOString(),
      updatedAt: userData.updatedAt || new Date().toISOString(),
      permissions: (userData.permissions || []).map((p: any) => p.name || p)
    };
  },

  async logout(): Promise<void> {
    try {
      // Call local API to clear secure cookies
      await fetch('/api/admin/auth', { method: 'DELETE', credentials: 'include' });
    } finally {
      tokenManager.clearTokens();
    }
  },

  getCurrentUserFromStorage(): AdminUser | null {
    if (typeof window === 'undefined') return null;
    const userStr = localStorage.getItem('adminUser');
    return userStr ? JSON.parse(userStr) : null;
  }
};

// Compatibility wrapper for code migrated from secure-admin-api.ts
// Uses the same api instance with token-based auth
export const secureAdminBackendApi = {
  async authenticatedRequest<T>(config: {
    method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'PATCH';
    url: string;
    data?: unknown;
    params?: unknown;
  }): Promise<T> {
    const response = await api.request<T>({
      method: config.method,
      url: config.url,
      data: config.data,
      params: config.params,
    });
    return response.data;
  }
};

// Permission constants - synchronized with backend schema
export const PERMISSIONS = {
  // Dashboard
  DASHBOARD_READ: 'dashboard:read',

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

  // Permission Management
  PERMISSIONS_READ: 'permissions:read',
  PERMISSIONS_WRITE: 'permissions:write',
  PERMISSIONS_MANAGE: 'permissions:manage',

  // Settings Management
  SETTINGS_READ: 'settings:read',
  SETTINGS_WRITE: 'settings:write',
  SETTINGS_MANAGE: 'settings:manage',

  // Security Management
  SECURITY_READ: 'security:read',
  SECURITY_WRITE: 'security:write',
  SECURITY_MANAGE: 'security:manage',

  // Audit & Logs
  LOGS_READ: 'logs:read',
  LOGS_WRITE: 'logs:write',
  LOGS_DELETE: 'logs:delete',
  LOGS_EXPORT: 'logs:export',
  LOGS_MANAGE: 'logs:manage',

  // Reports (if needed)
  REPORTS_READ: 'reports:read',
  REPORTS_WRITE: 'reports:write',
  REPORTS_EXPORT: 'reports:export',
  REPORTS_MANAGE: 'reports:manage',

  // Content Management (if needed)
  CONTENT_READ: 'content:read',
  CONTENT_WRITE: 'content:write',
  CONTENT_DELETE: 'content:delete',
  CONTENT_MANAGE: 'content:manage',

  // System Administration
  SYSTEM_READ: 'system:read',
  SYSTEM_WRITE: 'system:write',
  SYSTEM_MANAGE: 'system:manage',
} as const;

// Dynamic permission loading with schema sync
let dynamicPermissions: Record<string, string> = {};
let permissionsSynced = false;
let permissionsSyncExpiry: number = 0;
const PERMISSIONS_CACHE_DURATION = 10 * 60 * 1000; // 10 minutes

// Sync permissions with backend schema
export const syncPermissionsWithBackend = async (): Promise<void> => {
  try {
    const now = Date.now();

    // Check cache validity
    if (permissionsSynced && now < permissionsSyncExpiry) {
      return;
    }

    if (logLevel === 'debug') {
      logger.info(' Syncing permissions with backend schema...');
    }
    const schema = await getPermissionSchema();
    const allPermissions = await getAllPermissions();

    // Build dynamic permissions map
    dynamicPermissions = {};
    allPermissions.forEach(permission => {
      const key = permission.name.toUpperCase().replace(/[:\-\.]/g, '_');
      dynamicPermissions[key] = permission.name;
    });

    permissionsSynced = true;
    permissionsSyncExpiry = now + PERMISSIONS_CACHE_DURATION;

    if (logLevel === 'debug') {
      logger.info(` Permissions synced: ${allPermissions.length} permissions from ${schema.modules.length} modules`);
    }

  } catch (error) {
    logger.warn(' Failed to sync permissions with backend:', error);
  }
};

// Get permission by key (with dynamic lookup)
export const getPermission = async (key: string): Promise<string> => {
  await syncPermissionsWithBackend();

  // Try dynamic permissions first
  if (dynamicPermissions[key]) {
    return dynamicPermissions[key];
  }

  // Fallback to static permissions
  const staticPermission = (PERMISSIONS as any)[key];
  if (staticPermission) {
    return staticPermission;
  }

  logger.warn(` Permission not found: ${key}`);
  return key.toLowerCase().replace(/_/g, ':');
};

// Dynamic Role-based permissions - loaded from API
let dynamicRolePermissions: Record<string, string[]> = {};
let rolePermissionsCached = false;
let rolePermissionsCacheExpiry: number = 0;
const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes

// Fallback static permissions (used if API fails)
// Role permissions templates for different user roles  
export const ROLE_PERMISSIONS: Record<string, string[]> = {
  Customer: [PERMISSIONS.PRODUCTS_READ],
  ProductAdmin: [
    PERMISSIONS.DASHBOARD_READ,
    PERMISSIONS.PRODUCTS_READ,
    PERMISSIONS.PRODUCTS_WRITE,
    PERMISSIONS.PRODUCTS_DELETE,
    PERMISSIONS.PRODUCTS_MANAGE,
  ],
  SalesAdmin: [
    PERMISSIONS.DASHBOARD_READ,
    PERMISSIONS.ORDERS_READ,
    PERMISSIONS.ORDERS_WRITE,
    PERMISSIONS.ORDERS_MANAGE,
    PERMISSIONS.USERS_READ,
  ],
  SystemAdmin: [
    ...Object.values(PERMISSIONS) // All permissions for SystemAdmin
  ],
};

// Load role permissions from API
const loadRolePermissions = async (): Promise<Record<string, string[]>> => {
  try {
    const now = Date.now();

    // Check cache validity
    if (rolePermissionsCached && now < rolePermissionsCacheExpiry) {
      return dynamicRolePermissions;
    }

    if (logLevel === 'debug') {
      logger.info(' Loading role permissions from API...');
    }
    const mappings = await getRolePermissionMappings();

    // Transform API response to string array format
    dynamicRolePermissions = {};
    Object.entries(mappings).forEach(([roleName, permissions]) => {
      dynamicRolePermissions[roleName] = permissions.map(p => p.name);
    });

    rolePermissionsCached = true;
    rolePermissionsCacheExpiry = now + CACHE_DURATION;

    if (logLevel === 'debug') {
      logger.info(' Role permissions loaded successfully:', Object.keys(dynamicRolePermissions));
    }
    return dynamicRolePermissions;

  } catch (error) {
    logger.warn(' Failed to load role permissions from API, using fallback:', error);
    // Return fallback permissions if API fails
    return ROLE_PERMISSIONS;
  }
};

// Get role permissions (with caching)
export const getRolePermissions = async (roleName?: string): Promise<string[] | Record<string, string[]>> => {
  const permissions = await loadRolePermissions();

  if (roleName) {
    return permissions[roleName] || [];
  }

  return permissions;
};

// Clear role permissions cache
export const clearRolePermissionsCache = (): void => {
  rolePermissionsCached = false;
  rolePermissionsCacheExpiry = 0;
  dynamicRolePermissions = {};
  if (logLevel === 'debug') {
    logger.info(' Role permissions cache cleared');
  }
};

// Permission utility functions
export const hasPermission = (userPermissions: string[], requiredPermission: string): boolean => {
  // Check if user has the specific permission or if user has security:manage (super admin equivalent)
  return userPermissions.includes(requiredPermission) || userPermissions.includes(PERMISSIONS.SECURITY_MANAGE);
};

export const hasAnyPermission = (userPermissions: string[], requiredPermissions: string[]): boolean => {
  return requiredPermissions.some(permission => hasPermission(userPermissions, permission));
};

export const hasRole = (userRole: string, requiredRoles: string[]): boolean => {
  return requiredRoles.includes(userRole) || userRole === 'SystemAdmin';
};

// User Management API
export const getUsers = async (page = 1, limit = 10, search = ''): Promise<UserListResponse> => {
  const response = await api.get<UserListResponse>('/admin/users', {
    params: { page, limit, search }
  });
  return response.data;
};

export const getAdminUserById = async (userId: number): Promise<AdminUser> => {
  const response = await api.get<AdminUser>(`/admin/users/${userId}`);
  return response.data;
};

export const createUser = async (userData: Partial<AdminUser>): Promise<ApiResponse<AdminUser>> => {
  const response = await api.post<ApiResponse<AdminUser>>('/admin/users', userData);
  return response.data;
};

export const updateUser = async (id: string, userData: Partial<AdminUser>): Promise<ApiResponse<AdminUser>> => {
  const response = await api.put<ApiResponse<AdminUser>>(`/admin/users/${id}`, userData);
  return response.data;
};

export const deleteUser = async (id: string): Promise<void> => {
  await api.delete(`/admin/users/${id}`);
};

export const toggleUserStatus = async (id: string): Promise<AdminUser> => {
  const response = await api.patch<AdminUser>(`/admin/users/${id}/toggle-status`);
  return response.data;
};

// Role Management API
export const getRoles = async (): Promise<Role[]> => {
  const response = await api.get<Role[]>('/admin/roles');
  return response.data;
};

export const createRole = async (roleData: CreateRoleRequest): Promise<Role> => {
  const response = await api.post<Role>('/admin/roles', roleData);
  return response.data;
};

export const updateRole = async (id: string, roleData: UpdateRoleRequest): Promise<Role> => {
  const response = await api.put<Role>(`/admin/roles/${id}`, roleData);
  return response.data;
};

export const deleteRole = async (id: string): Promise<void> => {
  await api.delete(`/admin/roles/${id}`);
};

export const getPermissions = async (): Promise<Permission[]> => {
  const response = await api.get<any>('/admin/permissions');
  const raw = response.data;
  // Normalize to array in case backend wraps under { data } or { items }
  const data = raw?.data ?? raw?.items ?? raw ?? [];
  return Array.isArray(data) ? data : [];
};

export const assignRolePermissions = async (roleId: string, permissionIds: string[]): Promise<Role> => {
  const response = await api.patch<Role>(`/admin/roles/${roleId}/permissions`, { permissionIds });
  return response.data;
};

// Dashboard API
export const getDashboardMetrics = async (): Promise<ApiResponse<DashboardMetrics>> => {
  const response = await api.get<ApiResponse<DashboardMetrics>>('/api/admin/dashboard/metrics');
  return response.data;
};

export const getDashboardChartData = async (type: string, days = 30): Promise<ApiResponse<ChartDataPoint[]>> => {
  const response = await api.get<ApiResponse<ChartDataPoint[]>>(`/api/admin/dashboard/charts/${type}`, {
    params: { days }
  });
  return response.data;
};

// Logs and Audit API
export const getLogs = async (
  page = 1,
  limit = 50,
  filters: Record<string, string | number> = {}
): Promise<ApiResponse<LogsResponse>> => {
  // Map frontend filters to backend query params
  const skip = (page - 1) * limit;
  const take = limit;
  const params: Record<string, string | number> = {
    skip,
    take,
  };

  if (typeof filters.entityType !== 'undefined') params.entityType = filters.entityType as string;
  // Frontend previously used eventType; backend expects eventCategory
  if (typeof filters.eventType !== 'undefined') params.eventCategory = filters.eventType as string;
  if (typeof filters.eventCategory !== 'undefined') params.eventCategory = filters.eventCategory as string;
  if (typeof filters.userId !== 'undefined') params.userId = filters.userId as number;
  if (typeof filters.from !== 'undefined') params.fromDate = filters.from as string;
  if (typeof filters.to !== 'undefined') params.toDate = filters.to as string;

  const response = await api.get('/admin/security/audit-logs', { params });

  const { items = [], totalCount = 0, page: respPage = page, pageSize: respPageSize = limit, totalPages = Math.ceil(totalCount / limit) } = response.data || {};

  const mapped: ApiResponse<LogsResponse> = {
    success: true,
    data: {
      logs: items,
      pagination: {
        page: respPage,
        limit: respPageSize,
        total: totalCount,
        totalPages,
      },
      filters: {
        level: (params.eventCategory as string) || undefined,
        module: (params.entityType as string) || undefined,
        startDate: (params.fromDate as string) || undefined,
        endDate: (params.toDate as string) || undefined,
      }
    }
  };

  return mapped;
};

export const exportLogs = async (format: 'csv' | 'pdf', filters: Record<string, string | number> = {}): Promise<Blob> => {
  const response = await api.get(`/admin/logs/export/${format}`, {
    params: filters,
    responseType: 'blob'
  });
  return response.data;
};

export const deleteLogs = async (ids: string[]): Promise<ApiResponse<{ deletedCount: number }>> => {
  const response = await api.delete<ApiResponse<{ deletedCount: number }>>('/admin/logs', { data: { ids } });
  return response.data;
};

// Settings API
export const getSystemSettings = async (): Promise<ApiResponse<SystemSettings>> => {
  const response = await api.get<ApiResponse<SystemSettings>>('/admin/settings');
  return response.data;
};

export const updateSystemSettings = async (settings: Partial<SystemSettings>): Promise<ApiResponse<SystemSettings>> => {
  const response = await api.put<ApiResponse<SystemSettings>>('/admin/settings', settings);
  return response.data;
};

export const testEmailConfiguration = async (): Promise<{ success: boolean; message: string }> => {
  const response = await api.post<{ success: boolean; message: string }>('/admin/settings/test-email');
  return response.data;
};

// =====================================================
// Dynamic Permission Management API
// =====================================================

export interface DynamicAdminPermission {
  id: number;
  name: string;
  description: string;
  module?: string;
  action?: string;
}

export interface RolePermissionMapping {
  [roleName: string]: DynamicAdminPermission[];
}

export interface PermissionCheckRequest {
  permission: string;
}

export interface PermissionCheckResponse {
  hasPermission: boolean;
  permission: string;
  message?: string;
}

export interface PermissionSyncResponse {
  success: boolean;
  message: string;
  syncedAt: string;
}

export interface PermissionSchema {
  modules: string[];
  actions: string[];
  totalPermissions: number;
  lastUpdated: string;
}

// Get all available permissions in the system
export const getAllPermissions = async (): Promise<DynamicAdminPermission[]> => {
  const response = await api.get<DynamicAdminPermission[]>('/admin/permissions/all');
  return response.data;
};

// Get current user's permissions
export const getMyPermissions = async (): Promise<DynamicAdminPermission[]> => {
  const response = await api.get<DynamicAdminPermission[]>('/admin/permissions/my-permissions');
  return response.data;
};

// Get role permission mappings
export const getRolePermissionMappings = async (): Promise<RolePermissionMapping> => {
  const response = await api.get<RolePermissionMapping>('/admin/permissions/role-mappings');
  return response.data;
};

// Check specific permission
export const checkPermission = async (permission: string): Promise<PermissionCheckResponse> => {
  const response = await api.get<PermissionCheckResponse>(`/auth/check-permission?permission=${permission}`);
  return response.data;
};

// Sync permissions (refresh from database)
export const syncPermissions = async (): Promise<PermissionSyncResponse> => {
  const response = await api.post<PermissionSyncResponse>('/admin/permissions/sync');
  return response.data;
};

// Get permission schema (available modules and actions)
export const getPermissionSchema = async (): Promise<PermissionSchema> => {
  const response = await api.get<PermissionSchema>('/admin/permissions/schema');
  return response.data;
};

// Create unified adminApi object for use in React contexts
export const adminApi = {
  get: async <T = any>(endpoint: string): Promise<T> => {
    const response = await api.get<T>(`/admin${endpoint}`);
    return response.data;
  },

  post: async <T = any>(endpoint: string, data?: any): Promise<T> => {
    const response = await api.post<T>(`/admin${endpoint}`, data);
    return response.data;
  },

  put: async <T = any>(endpoint: string, data?: any): Promise<T> => {
    const response = await api.put<T>(`/admin${endpoint}`, data);
    return response.data;
  },

  delete: async <T = any>(endpoint: string, data?: any): Promise<T> => {
    const response = await api.delete<T>(`/admin${endpoint}`, data);
    return response.data;
  }
};

// Utility functions for API responses
export const handleApiError = (error: any): string => {
  if (error.response?.data?.message) {
    return error.response.data.message;
  }
  if (error.response?.data?.error?.message) {
    return error.response.data.error.message;
  }
  if (error.message) {
    return error.message;
  }
  return 'Đã xảy ra lỗi không xác định';
};

// Order Management API
export interface AdminOrdersParams {
  page?: number;
  limit?: number;
  status?: string;
  customerId?: number;
  from?: string;
  to?: string;
  minAmount?: number;
  maxAmount?: number;
}

export interface AdminOrdersResponse {
  orders: Order[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
  pageSize: number;
  summary: {
    totalAmount: number;
    averageOrderValue: number;
  };
}

export const getAdminOrders = async (params: AdminOrdersParams = {}): Promise<AdminOrdersResponse> => {
  const response = await api.get<AdminOrdersResponse>('/orders/admin', { params });
  return response.data;
};

export const getAdminOrderDetails = async (orderId: number): Promise<Order> => {
  const response = await api.get<Order>(`/orders/${orderId}`);
  return response.data;
};

export interface OrderStatusUpdateError {
  message: string;
  currentStatus: string;
  attemptedStatus: string;
  validTransitions: string[];
  details?: string;
}

export const updateOrderStatus = async (orderId: number, status: string, note?: string): Promise<Order> => {
  try {
    const response = await api.put<Order>(`/orders/${orderId}/status`, {
      newStatus: status,
      reason: note || `Status updated to ${status}`
    });
    return response.data;
  } catch (error: any) {
    // Enhanced error handling for better UX
    if (error.response?.status === 404) {
      const errorMessage = error.response?.data?.message || 'Không thể cập nhật trạng thái đơn hàng';

      // Try to extract workflow validation info from error message
      const workflowError: OrderStatusUpdateError = {
        message: errorMessage,
        currentStatus: 'unknown',
        attemptedStatus: status,
        validTransitions: [],
        details: error.response?.data?.details || 'Chuyển đổi trạng thái không hợp lệ'
      };

      // Enhanced error object for UI handling
      const enhancedError = new Error(errorMessage);
      (enhancedError as any).workflowError = workflowError;
      (enhancedError as any).status = 404;
      throw enhancedError;
    }

    // Re-throw other errors as-is
    throw error;
  }
};

// Product Management API
export interface AdminProductsParams {
  page?: number;
  limit?: number;
  search?: string;
  categoryId?: number;
  brandId?: number;
  isActive?: boolean;
  sortBy?: string;
  sortOrder?: string;
  productType?: 'all' | 'base' | 'variant';
}

export interface AdminProductsResponse {
  products: Product[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
  pageSize: number;
}

// Backend DTO interface for product updates
export interface ProductUpdateDto {
  Name: string;
  SKU: string;
  Description: string;
  Model: string;
  BrandId: number;
  CategoryId?: number;
  Price: number;
  DiscountPrice?: number;
  IsActive: boolean;
  IsFeatured?: boolean;
  StockQuantity: number;
  ProductType: string;
  // Laptop specific fields
  Series?: string; //  FIXED: Add series field to DTO
  CPU?: string;
  RAM?: string;
  Storage?: string;
  GPU?: string;
  Display?: string;
  Battery?: string;
  Weight_Kg?: string;
  OperatingSystem?: string;
  Ports?: string;
  // Accessory specific fields
  Type?: string;
  AccessoryType?: string;
  Compatibility?: string;
  Color?: string;
  Material?: string;
  Warranty?: string;
}

// Form data interface - COMPREHENSIVE VERSION
export interface ProductFormData {
  // === BASIC INFORMATION ===
  name: string;
  sku: string;
  description: string;
  shortDescription?: string;
  categoryId: string;
  brandId: string;
  price: string;
  comparePrice?: string;
  costPrice?: string;
  stock: string;
  weight: string;
  dimensions: string;
  status: 'active' | 'inactive';
  isFeatured?: boolean;
  images: string[];
  productType: 'Laptop' | 'Accessory' | 'Bundle';
  // UI-only/meta fields used by admin edit page
  tags?: string[];
  specifications?: Record<string, string>;

  // === INVENTORY MANAGEMENT ===
  inventory: {
    quantityInStock: string;
    reservedQuantity: string;
    reorderLevel: string;
    maxStockLevel: string;
    warehouseLocation: string;
  };

  // === LAPTOP SPECIFICATIONS - COMPREHENSIVE ===
  // Basic Info
  series?: string;
  model?: string;

  // CPU Specifications - DETAILED
  cpuBrand?: string;
  cpuModel?: string;
  cpuGeneration?: string;
  cpuCores?: string; // int
  cpuBaseClockGHz?: string; // decimal
  cpuBoostClockGHz?: string; // decimal
  cpuCache?: string;

  // RAM Specifications - DETAILED
  ramType?: string;
  ramCapacityGB?: string; // int
  ramSlots?: string; // int
  ramSpeed?: string; // int
  ramUpgradeable?: boolean;

  // Storage Specifications - DETAILED
  storageType?: string;
  storageCapacityGB?: string; // int
  storageInterface?: string;
  nvMeSupport?: boolean;

  // GPU Specifications - DETAILED
  gpuType?: string;
  gpuBrand?: string;
  gpuModel?: string;
  gpuVramGB?: string; // int

  // Display Specifications - DETAILED
  displaySizeInches?: string; // decimal
  displayResolution?: string;
  displayPanelType?: string;
  displayRefreshRateHz?: string; // int
  displayTouchscreen?: boolean;

  // Physical Specifications - DETAILED
  batteryCapacityWh?: string; // int
  weightKg?: string; // decimal
  color?: string;
  ports?: string;

  // Connectivity - DETAILED
  wiFi6Support?: boolean;
  bluetoothSupport?: boolean;
  bluetoothVersion?: string;

  // Business Information - DETAILED
  warrantyPeriod?: string;
  targetAudience?: string;

  // === ACCESSORY SPECIFICATIONS ===
  accessoryType?: string;
  compatibility?: string;
  specificationDetails?: string;
  connectivity?: string;
  material?: string;

  // === BUNDLE SPECIFICATIONS ===
  bundleType?: string;
  discountPercentage?: string;
  validFrom?: string;
  validTo?: string;
  bundleItems?: Array<{
    productId: string;
    quantity: string;
    discountPercentage: string;
  }>;

  // === LEGACY FIELDS (for backward compatibility) ===
  cpu?: string; // Combined field for simple display
  ram?: string; // Combined field for simple display
  storage?: string; // Combined field for simple display
  gpu?: string; // Combined field for simple display
  display?: string; // Combined field for simple display
  battery?: string; // Combined field for simple display
  weight_kg?: string; // Legacy weight field
  operatingSystem?: string;
  warranty?: string;
}

// Helper function to find brand ID by name
const findBrandIdByName = (brands: Brand[], brandName: string): string => {
  const brand = brands?.find(b => b.name.toLowerCase() === brandName.toLowerCase());
  return brand?.id?.toString() || '';
};

const withUnit = (value: unknown, unit: string): string => {
  if (value === null || value === undefined || value === '') return '';
  const text = value.toString().trim();
  return text.toLowerCase().includes(unit.trim().toLowerCase()) ? text : `${text}${unit}`;
};

const cpuCoresLabel = (value: unknown): string => {
  if (value === null || value === undefined || value === '') return '';
  const text = value.toString().trim();
  return /cores?/i.test(text) ? text : `${text} cores`;
};

const displaySizeLabel = (value: unknown): string => {
  if (value === null || value === undefined || value === '') return '';
  const text = value.toString().trim();
  return text.includes('"') ? text : `${text}"`;
};

const normalizeDisplayResolution = (value?: string): string => {
  if (!value) return '';
  const known: Record<string, string> = {
    '1366x768': '1366x768 (HD)',
    '1920x1080': '1920x1080 (FHD)',
    '2560x1440': '2560x1440 (QHD)',
    '2560x1600': '2560x1600 (WQXGA)',
    '2880x1800': '2880x1800 (Retina)',
    '3200x2000': '3200x2000 (3.2K)',
    '3840x2160': '3840x2160 (4K UHD)',
    '5120x2880': '5120x2880 (5K)',
    '6016x3384': '6016x3384 (6K)'
  };
  return known[value] || value;
};

// Map Product to form data - COMPREHENSIVE VERSION
export const mapProductToForm = (product: Product, brands: Brand[] = []): ProductFormData => {
  console.log(' mapProductToForm input:', {
    product,
    brands,
    productKeys: Object.keys(product || {}),
    brandKeys: Object.keys(brands || {})
  });

  const result = {
    // === BASIC INFORMATION ===
    name: product.name || '',
    sku: product.sku || '',
    description: product.description || '',
    categoryId: product.categoryId?.toString() || product.categories?.[0]?.id?.toString() || '',
    brandId: product.brandId?.toString() || findBrandIdByName(brands, product.brand || ''),
    price: product.price?.toString() || '',
    stock: product.stockQuantity?.toString() || '',
    weight: product.weightKg?.toString() || product.weight?.toString() || '',
    dimensions: product.dimensions || '',
    status: (product.isActive ? 'active' : 'inactive') as 'active' | 'inactive',
    images: product.images?.map(i => i.imageUrl) || [],
    productType: (product.productType || product.type || 'Laptop') as 'Laptop' | 'Accessory' | 'Bundle',

    // === INVENTORY MANAGEMENT ===
    inventory: {
      quantityInStock: product.inventory?.quantityInStock?.toString() || product.stockQuantity?.toString() || '',
      reservedQuantity: product.inventory?.reservedQuantity?.toString() || '0',
      reorderLevel: product.inventory?.reorderLevel?.toString() || '5',
      maxStockLevel: product.inventory?.maxStockLevel?.toString() || '100',
      warehouseLocation: product.inventory?.warehouseLocation || ''
    },

    // === LAPTOP SPECIFICATIONS - DETAILED MAPPING ===
    // Basic Info
    series: product.series || '',
    model: product.model || '',

    // CPU Specifications - DETAILED
    cpuBrand: product.cpuBrand || '',
    cpuModel: product.cpuModel || '',
    cpuGeneration: product.cpuGeneration || '',
    cpuCores: cpuCoresLabel(product.cpuCores),
    cpuBaseClockGHz: product.cpuBaseClockGHz?.toString() || '',
    cpuBoostClockGHz: product.cpuBoostClockGHz?.toString() || '',
    cpuCache: product.cpuCache || '',

    // RAM Specifications - DETAILED
    ramType: product.ramType || '',
    ramCapacityGB: withUnit(product.ramCapacityGB, 'GB'),
    ramSlots: product.ramSlots?.toString() || '',
    ramSpeed: withUnit(product.ramSpeed, ' MHz'),
    ramUpgradeable: product.ramUpgradeable || false,

    // Storage Specifications - DETAILED
    storageType: product.storageType || '',
    storageCapacityGB: product.storageCapacityGB
      ? Number(product.storageCapacityGB) >= 1024 && Number(product.storageCapacityGB) % 1024 === 0
        ? `${Number(product.storageCapacityGB) / 1024}TB`
        : `${product.storageCapacityGB}GB`
      : '',
    storageInterface: product.storageInterface || '',
    nvMeSupport: product.nvMeSupport || false,

    // GPU Specifications - DETAILED
    gpuType: product.gpuType || '',
    gpuBrand: product.gpuBrand || '',
    gpuModel: product.gpuModel || '',
    gpuVramGB: withUnit(product.gpuVramGB, 'GB'),

    // Display Specifications - DETAILED
    displaySizeInches: displaySizeLabel(product.displaySizeInches),
    displayResolution: normalizeDisplayResolution(product.displayResolution),
    displayPanelType: product.displayPanelType || '',
    displayRefreshRateHz: withUnit(product.displayRefreshRateHz, 'Hz'),
    displayTouchscreen: product.displayTouchscreen || false,

    // Physical Specifications - DETAILED
    batteryCapacityWh: withUnit(product.batteryCapacityWh, 'Wh'),
    weightKg: product.weightKg?.toString() || '',
    color: product.color || '',
    ports: product.ports || '',

    // Connectivity - DETAILED
    wiFi6Support: product.wiFi6Support || false,
    bluetoothSupport: product.bluetoothSupport || false,
    bluetoothVersion: product.bluetoothVersion || '',

    // Business Information - DETAILED
    warrantyPeriod: product.warrantyPeriod || '',
    targetAudience: product.targetAudience || '',

    // === ACCESSORY SPECIFICATIONS ===
    accessoryType: product.accessoryType || '',
    compatibility: product.compatibility || '',
    specificationDetails: product.specifications_json || '',
    connectivity: product.connectivity || '',

    // === BUNDLE SPECIFICATIONS ===
    bundleType: product.bundleType || '',
    discountPercentage: product.discountPercentage?.toString() || '',
    validFrom: product.validFrom || '',
    validTo: product.validTo || '',
    bundleItems: product.bundleItems?.map(item => ({
      productId: item.productId.toString(),
      quantity: item.quantity.toString(),
      discountPercentage: item.discountPercentage.toString()
    })) || [],

    // === LEGACY FIELDS (for backward compatibility) ===
    cpu: product.cpuBrand && product.cpuModel
      ? `${product.cpuBrand} ${product.cpuModel} ${product.cpuGeneration || ''}`.trim()
      : product.cpuBrand || '',
    ram: product.ramCapacityGB && product.ramType
      ? `${product.ramCapacityGB}GB ${product.ramType} ${product.ramSpeed ? `${product.ramSpeed}MHz` : ''}`.trim()
      : product.ramCapacityGB ? `${product.ramCapacityGB}GB` : '',
    storage: product.storageCapacityGB && product.storageType
      ? `${product.storageCapacityGB}GB ${product.storageType} ${product.storageInterface || ''}`.trim()
      : product.storageCapacityGB ? `${product.storageCapacityGB}GB SSD` : '',
    gpu: product.gpuBrand && product.gpuModel
      ? `${product.gpuBrand} ${product.gpuModel} ${product.gpuVramGB ? `${product.gpuVramGB}GB` : ''}`.trim()
      : product.gpuBrand || '',
    display: product.displaySizeInches && product.displayResolution
      ? `${product.displaySizeInches}" ${product.displayResolution} ${product.displayPanelType || ''} ${product.displayRefreshRateHz ? `${product.displayRefreshRateHz}Hz` : ''}`.trim()
      : product.displaySizeInches ? `${product.displaySizeInches}" Display` : '',
    battery: product.batteryCapacityWh
      ? `${product.batteryCapacityWh}Wh`
      : product.battery?.toString() || '',
    weight_kg: product.weightKg?.toString() || product.weight?.toString() || ''
  };

  console.log(' mapProductToForm result:', result);
  return result;
};

// Map form data to backend DTO
export const mapFormToUpdatePayload = (formData: ProductFormData): ProductUpdateDto => {
  // --- Helpers to parse combined strings into typed fields ---
  const parseCpu = (cpu?: string): { brand?: string; model?: string; generation?: string } => {
    if (!cpu) return {};
    const parts = cpu.split(/\s+/).filter(Boolean);
    // Heuristic: first token brand, rest as model; try to pick generation by pattern like 'Gen' or '\d+(th)'
    const brand = parts[0];
    const genMatch = cpu.match(/(\d+\w*\s*Gen)/i);
    return {
      brand,
      model: parts.slice(1).join(' ').replace(/\s*\d+\w*\s*Gen/i, '').trim() || undefined,
      generation: genMatch ? genMatch[1] : undefined
    };
  };

  const parseRam = (ram?: string): { capacityGB?: number; type?: string; speed?: number } => {
    if (!ram) return {};
    const capacity = ram.match(/(\d+)\s*GB/i);
    const type = ram.match(/(DDR\d|LPDDR\d)/i);
    const speed = ram.match(/(\d{3,5})\s*MHz/i);
    return {
      capacityGB: capacity ? Number(capacity[1]) : undefined,
      type: type ? type[1].toUpperCase() : undefined,
      speed: speed ? Number(speed[1]) : undefined
    };
  };

  const parseStorage = (storage?: string): { capacityGB?: number; type?: string; iface?: string } => {
    if (!storage) return {};
    const capacity = storage.match(/(\d+(?:\.\d+)?)\s*(TB|GB)/i);
    const type = storage.match(/(SSD|HDD)/i);
    const iface = storage.match(/(NVMe|SATA|PCIe)/i);
    let capacityGB: number | undefined;
    if (capacity) {
      const val = Number(capacity[1]);
      capacityGB = capacity[2].toUpperCase() === 'TB' ? Math.round(val * 1024) : Math.round(val);
    }
    return {
      capacityGB,
      type: type ? type[1].toUpperCase() : undefined,
      iface: iface ? iface[1].toUpperCase() : undefined
    };
  };

  const parseDisplay = (display?: string): { sizeInches?: number; resolution?: string; panel?: string; refreshHz?: number } => {
    if (!display) return {};
    const size = display.match(/(\d{1,2}(?:\.\d{1,2})?)\s*"/);
    const resolution = display.match(/(\d{3,4}x\d{3,4})/i);
    const panel = display.match(/\b(IPS|OLED|TN|VA|Mini-LED|QLED)\b/i);
    const refresh = display.match(/(\d{2,3})\s*Hz/i);
    return {
      sizeInches: size ? Number(size[1]) : undefined,
      resolution: resolution ? resolution[1] : undefined,
      panel: panel ? panel[1].toUpperCase() : undefined,
      refreshHz: refresh ? Number(refresh[1]) : undefined
    };
  };

  const parseBatteryWh = (battery?: string): number | undefined => {
    if (!battery) return undefined;
    const m = battery.match(/(\d+(?:\.\d+)?)\s*Wh/i);
    return m ? Number(m[1]) : undefined;
  };

  const parseWeightKg = (w?: string): number | undefined => {
    if (!w) return undefined;
    const kg = w.match(/(\d+(?:\.\d+)?)\s*kg/i);
    if (kg) return Number(kg[1]);
    const just = w.match(/(\d+(?:\.\d+)?)/);
    return just ? Number(just[1]) : undefined;
  };

  const parseCapacityGB = (value?: string): number | undefined => {
    if (!value) return undefined;
    const match = value.match(/(\d+(?:\.\d+)?)\s*(TB|GB)?/i);
    if (!match) return undefined;
    const amount = Number(match[1]);
    return match[2]?.toUpperCase() === 'TB' ? Math.round(amount * 1024) : Math.round(amount);
  };

  const parseGpu = (gpu?: string): { brand?: string; model?: string; vramGB?: number } => {
    if (!gpu) return {};
    const vram = gpu.match(/(\d+)\s*GB/i);
    const parts = gpu.replace(/(\d+)\s*GB/i, '').trim().split(/\s+/);
    const brand = parts[0];
    const model = parts.slice(1).join(' ').trim();
    return {
      brand: brand || undefined,
      model: model || undefined,
      vramGB: vram ? Number(vram[1]) : undefined
    };
  };

  const payload: ProductUpdateDto = {
    Name: formData.name,
    SKU: formData.sku,
    Description: formData.description,
    Model: formData.model || '',
    BrandId: parseInt(formData.brandId) || 0,
    CategoryId: formData.categoryId ? parseInt(formData.categoryId) : undefined,
    Price: parseFloat(formData.price) || 0,
    IsActive: formData.status === 'active',
    // Prefer nested inventory.quantityInStock when available
    StockQuantity: formData.inventory?.quantityInStock ? parseInt(formData.inventory.quantityInStock) : (parseInt(formData.stock) || 0),
    ProductType: formData.productType
  };

  // Add type-specific fields (typed mapping expected by backend)
  if (formData.productType === 'Laptop') {
    // Basic laptop info
    payload.Series = formData.series || '';
    if (formData.model) (payload as any).Model = formData.model;

    // CPU Specifications - DETAILED
    if (formData.cpuBrand) (payload as any).CpuBrand = formData.cpuBrand;
    if (formData.cpuModel) (payload as any).CpuModel = formData.cpuModel;
    if (formData.cpuGeneration) (payload as any).CpuGeneration = formData.cpuGeneration;
    if (formData.cpuCores) (payload as any).CpuCores = parseInt(formData.cpuCores);
    if (formData.cpuBaseClockGHz) (payload as any).CpuBaseClockGHz = parseFloat(formData.cpuBaseClockGHz);
    if (formData.cpuBoostClockGHz) (payload as any).CpuBoostClockGHz = parseFloat(formData.cpuBoostClockGHz);
    if (formData.cpuCache) (payload as any).CpuCache = formData.cpuCache;

    // RAM Specifications - DETAILED
    if (formData.ramType) (payload as any).RamType = formData.ramType;
    if (formData.ramCapacityGB) (payload as any).RamCapacityGB = parseInt(formData.ramCapacityGB);
    if (formData.ramSlots) (payload as any).RamSlots = parseInt(formData.ramSlots);
    if (formData.ramSpeed) (payload as any).RamSpeed = parseInt(formData.ramSpeed);
    if (formData.ramUpgradeable !== undefined) (payload as any).RamUpgradeable = formData.ramUpgradeable;

    // Storage Specifications - DETAILED
    if (formData.storageType) (payload as any).StorageType = formData.storageType;
    if (formData.storageCapacityGB) {
      const storageCapacityGB = parseCapacityGB(formData.storageCapacityGB);
      if (typeof storageCapacityGB === 'number') (payload as any).StorageCapacityGB = storageCapacityGB;
    }
    if (formData.storageInterface) (payload as any).StorageInterface = formData.storageInterface;
    if (formData.nvMeSupport !== undefined) (payload as any).NvMeSupport = formData.nvMeSupport;

    // GPU Specifications - DETAILED
    if (formData.gpuType) (payload as any).GpuType = formData.gpuType;
    if (formData.gpuBrand) (payload as any).GpuBrand = formData.gpuBrand;
    if (formData.gpuModel) (payload as any).GpuModel = formData.gpuModel;
    if (formData.gpuVramGB) (payload as any).GpuVramGB = parseInt(formData.gpuVramGB);

    // Display Specifications - DETAILED
    if (formData.displaySizeInches) (payload as any).DisplaySizeInches = parseFloat(formData.displaySizeInches);
    if (formData.displayResolution) (payload as any).DisplayResolution = formData.displayResolution;
    if (formData.displayPanelType) (payload as any).DisplayPanelType = formData.displayPanelType;
    if (formData.displayRefreshRateHz) (payload as any).DisplayRefreshRateHz = parseInt(formData.displayRefreshRateHz);
    if (formData.displayTouchscreen !== undefined) (payload as any).DisplayTouchscreen = formData.displayTouchscreen;

    // Physical Specifications - DETAILED
    if (formData.batteryCapacityWh) (payload as any).BatteryCapacityWh = parseInt(formData.batteryCapacityWh);
    if (formData.weightKg) (payload as any).WeightKg = parseFloat(formData.weightKg);
    if (formData.color) (payload as any).Color = formData.color;
    if (formData.ports) (payload as any).Ports = formData.ports;

    // Connectivity - DETAILED
    if (formData.wiFi6Support !== undefined) (payload as any).WiFi6Support = formData.wiFi6Support;
    if (formData.bluetoothSupport !== undefined) (payload as any).BluetoothSupport = formData.bluetoothSupport;
    if (formData.bluetoothVersion) (payload as any).BluetoothVersion = formData.bluetoothVersion;

    // Business Information - DETAILED
    if (formData.warrantyPeriod) (payload as any).WarrantyPeriod = formData.warrantyPeriod;
    if (formData.targetAudience) (payload as any).TargetAudience = formData.targetAudience;

    // Legacy fields for backward compatibility
    const cpu = parseCpu(formData.cpu);
    if (cpu.brand) (payload as any).CpuBrand = cpu.brand;
    if (cpu.model) (payload as any).CpuModel = cpu.model;
    if (cpu.generation) (payload as any).CpuGeneration = cpu.generation;

    const ram = parseRam(formData.ram);
    if (typeof ram.capacityGB === 'number') (payload as any).RamCapacityGB = ram.capacityGB;
    if (ram.type) (payload as any).RamType = ram.type;
    if (typeof ram.speed === 'number') (payload as any).RamSpeed = ram.speed;

    const storage = parseStorage(formData.storage);
    if (storage.type) (payload as any).StorageType = storage.type;
    if (typeof storage.capacityGB === 'number') (payload as any).StorageCapacityGB = storage.capacityGB;
    if (storage.iface) (payload as any).StorageInterface = storage.iface;

    const gpu = parseGpu(formData.gpu);
    if (gpu.brand) (payload as any).GpuBrand = gpu.brand;
    if (gpu.model) (payload as any).GpuModel = gpu.model;
    if (typeof gpu.vramGB === 'number') (payload as any).GpuVramGB = gpu.vramGB;

    const disp = parseDisplay(formData.display);
    if (typeof disp.sizeInches === 'number') (payload as any).DisplaySizeInches = disp.sizeInches;
    if (disp.resolution) (payload as any).DisplayResolution = disp.resolution;
    if (disp.panel) (payload as any).DisplayPanelType = disp.panel;
    if (typeof disp.refreshHz === 'number') (payload as any).DisplayRefreshRateHz = disp.refreshHz;

    const batteryWh = parseBatteryWh(formData.battery);
    if (typeof batteryWh === 'number') (payload as any).BatteryCapacityWh = batteryWh;

    const weightKg = parseWeightKg(formData.weight_kg);
    if (typeof weightKg === 'number') (payload as any).WeightKg = weightKg;

  } else if (formData.productType === 'Accessory') {
    if (formData.accessoryType) {
      (payload as any).AccessoryType = formData.accessoryType;
      (payload as any).Type = formData.accessoryType;
    }
    if (formData.compatibility) (payload as any).Compatibility = formData.compatibility;
    if (formData.specificationDetails) (payload as any).SpecificationDetails = formData.specificationDetails;
    if (formData.connectivity) (payload as any).Connectivity = formData.connectivity;
    if (formData.color) (payload as any).Color = formData.color;
  } else if (formData.productType === 'Bundle') {
    if (formData.bundleType) (payload as any).BundleType = formData.bundleType;
    if (formData.discountPercentage) (payload as any).DiscountPercentage = parseFloat(formData.discountPercentage);
    if (formData.validFrom) (payload as any).ValidFrom = formData.validFrom;
    if (formData.validTo) (payload as any).ValidTo = formData.validTo;
    if (formData.bundleItems) (payload as any).BundleItems = formData.bundleItems.map(item => ({
      ProductId: parseInt(item.productId),
      Quantity: parseInt(item.quantity),
      DiscountPercentage: parseFloat(item.discountPercentage)
    }));
  }

  return payload;
};

export const mapFormToCreatePayload = (formData: ProductFormData, brands: Brand[] = []): any => {
  const payload: any = mapFormToUpdatePayload(formData);
  const brand = brands.find(b => b.id === Number(payload.BrandId));
  if (brand?.name) {
    payload.Brand = brand.name;
  }
  return payload;
};

export const getAdminProducts = async (params: AdminProductsParams = {}): Promise<AdminProductsResponse> => {
  const backendParams: any = { ...params };
  if (backendParams.limit && !backendParams.pageSize) {
    backendParams.pageSize = backendParams.limit;
  }
  delete backendParams.limit;

  const response = await api.get<any>('/products/admin', { params: backendParams });
  const raw = response.data;
  return {
    products: raw.data || [],
    totalCount: raw.totalCount || 0,
    currentPage: raw.page || 1,
    totalPages: raw.totalPages || 0,
    pageSize: raw.pageSize || 20
  };
};

export const getAdminProduct = async (productId: number): Promise<Product> => {
  const response = await api.get<ApiResponse<Product>>(`/products/admin/${productId}`);
  console.log(' getAdminProduct response:', {
    productId,
    responseData: response.data,
    product: unwrapApiData<Product>(response.data)
  });
  return unwrapApiData<Product>(response.data);
};

export const getProductVariants = async (productId: number): Promise<Product[]> => {
  const response = await api.get<ApiResponse<Product[]>>(`/products/${productId}/variants`);
  return unwrapApiArray<Product>(response.data);
};

export const createProduct = async (productData: any): Promise<Product> => {
  const response = await api.post<ApiResponse<Product>>('/products', productData);
  return unwrapApiData<Product>(response.data);
};

export const updateProduct = async (productId: number, productData: any): Promise<Product> => {
  // Backend expects Brand as string; if only BrandId is provided, resolve name
  if (productData && productData.BrandId && !productData.Brand) {
    try {
      const brands = await getBrands();
      const brand = brands.find(b => b.id === Number(productData.BrandId));
      if (brand?.name) {
        productData.Brand = brand.name;
      }
    } catch { }
  }

  const response = await api.put<{ success: boolean; message?: string; data: Product }>(`/products/${productId}`, productData);
  return (response.data as any)?.data ?? (response.data as any);
};

export const deleteProduct = async (productId: number): Promise<void> => {
  await api.delete(`/products/${productId}`);
};

// Product Image Management
export interface ImageUploadResult {
  productId: number;
  imageUrl: string;
  imageId: string;
  displayOrder: number;
  message: string;
}

export const uploadProductImages = async (productId: number, files: FileList): Promise<ImageUploadResult[]> => {
  const formData = new FormData();

  console.log('Creating FormData with files:', {
    count: files.length,
    names: Array.from(files).map(f => f.name)
  });

  for (let i = 0; i < files.length; i++) {
    formData.append('files', files[i]);
    console.log(`Added file ${i + 1}:`, files[i].name, files[i].size);
  }

  // Debug FormData contents
  console.log('FormData entries:');
  for (const [key, value] of formData.entries()) {
    console.log(key, value);
  }

  const response = await api.post(
    `/products/${productId}/images`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );

  const raw = response.data;
  const data = raw?.data ?? raw;
  const uploaded = Array.isArray(data)
    ? data
    : (data?.uploadedImages ?? data?.items ?? []);

  const pid = data?.productId ?? productId;

  return uploaded.map((u: any, idx: number) => ({
    productId: pid,
    imageUrl: u.imageUrl ?? u.url,
    imageId: u.imageId ?? u.id,
    displayOrder: (u.displayOrder ?? idx + 1),
    message: u.message ?? ''
  }));
};

export const deleteProductImage = async (productId: number, imageId: string): Promise<void> => {
  await api.delete(`/products/${productId}/images/${imageId}`);
};

// Categories and Brands
export interface Category {
  id: number;
  name: string;
  slug: string;
  description?: string;
  parentId?: number;
  parentName?: string;
  productCount: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Brand {
  id: number;
  name: string;
  slug: string;
  description?: string;
  logoUrl?: string;
  website?: string;
  country?: string;
  productCount: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CategoryFormData {
  name: string;
  description?: string;
  parentId?: string;
  isActive: boolean;
}

export interface BrandFormData {
  name: string;
  slug: string;
  description?: string;
  logoUrl?: string;
  website?: string;
  country?: string;
  isActive: boolean;
}

// Basic category/brand getters (for dropdowns)
export const getCategories = async (): Promise<Category[]> => {
  const response = await api.get<Category[] | ApiResponse<Category[]>>('/admin/categories');
  const categories = unwrapApiArray<Category>(response.data);
  console.log(' getCategories response:', {
    responseData: response.data,
    categoriesCount: categories.length
  });
  return categories;
};

export const getBrands = async (): Promise<Brand[]> => {
  const response = await api.get<Brand[] | ApiResponse<Brand[]>>('/admin/brands');
  const brands = unwrapApiArray<Brand>(response.data);
  console.log(' getBrands response:', {
    responseData: response.data,
    brandsCount: brands.length
  });
  return brands;
};

// Admin API functions with product counts
export const getCategoriesWithCounts = async (): Promise<Category[]> => {
  const response = await adminApi.get<Category[]>('/categories/with-counts');
  console.log(' getCategoriesWithCounts response:', {
    responseData: response,
    categoriesCount: response?.length
  });
  return response;
};

export const getBrandsWithCounts = async (): Promise<Brand[]> => {
  const response = await adminApi.get<Brand[]>('/brands/with-counts');
  console.log(' getBrandsWithCounts response:', {
    responseData: response,
    brandsCount: response?.length
  });
  return response;
};

// Category CRUD operations
export const createCategory = async (categoryData: CategoryFormData): Promise<Category> => {
  const response = await adminApi.post<Category>('/categories', {
    name: categoryData.name,
    description: categoryData.description,
    parentId: categoryData.parentId ? parseInt(categoryData.parentId) : null,
    isActive: categoryData.isActive
  });
  return response;
};

export const updateCategory = async (id: number, categoryData: CategoryFormData): Promise<Category> => {
  const response = await adminApi.put<Category>(`/categories/${id}`, {
    name: categoryData.name,
    description: categoryData.description,
    parentId: categoryData.parentId ? parseInt(categoryData.parentId) : null,
    isActive: categoryData.isActive
  });
  return response;
};

export const deleteCategory = async (id: number): Promise<void> => {
  await adminApi.delete(`/categories/${id}`);
};

export const reassignAndDeleteCategory = async (categoryIdToDelete: number, newCategoryId: number): Promise<void> => {
  await adminApi.post(`/categories/${categoryIdToDelete}/reassign-and-delete`, {
    newCategoryId
  });
};

export const forceDeleteCategory = async (id: number): Promise<void> => {
  await adminApi.delete(`/categories/${id}/force`);
};

// Brand CRUD operations
export const createBrand = async (brandData: BrandFormData): Promise<Brand> => {
  const response = await adminApi.post<Brand>('/brands', {
    name: brandData.name,
    slug: brandData.slug,
    description: brandData.description,
    logoUrl: brandData.logoUrl,
    websiteUrl: brandData.website,
    isActive: brandData.isActive
  });
  return response;
};

export const updateBrand = async (id: number, brandData: BrandFormData): Promise<Brand> => {
  const response = await adminApi.put<Brand>(`/brands/${id}`, {
    name: brandData.name,
    slug: brandData.slug,
    description: brandData.description,
    logoUrl: brandData.logoUrl,
    websiteUrl: brandData.website,
    isActive: brandData.isActive
  });
  return response;
};

export const deleteBrand = async (id: number): Promise<void> => {
  await adminApi.delete(`/brands/${id}`);
};

export const reassignAndDeleteBrand = async (brandIdToDelete: number, newBrandId: number): Promise<void> => {
  await adminApi.post(`/brands/${brandIdToDelete}/reassign-and-delete`, {
    newBrandId
  });
};

export const forceDeleteBrand = async (id: number): Promise<void> => {
  await adminApi.delete(`/brands/${id}/force`);
};

// Dashboard API
export interface DashboardKPIs {
  todaySales: {
    value: number;
    change: number;
    isPositive: boolean;
    unit: string;
    period: string;
  };
  newOrders: {
    value: number;
    change: number;
    isPositive: boolean;
    unit: string;
    period: string;
  };
  lowStock: {
    value: number;
    label: string;
  };
  visitors: {
    value: number;
    change: number;
    isPositive: boolean;
    unit: string;
    period: string;
  };
  revenue: {
    value: number;
    change: number;
    isPositive: boolean;
    unit: string;
    period: string;
  };
  totalCustomers: {
    value: number;
    label: string;
  };
}

export interface SalesTrendData {
  name: string;
  sales: number;
  orders: number;
  date: string;
}

export const getDashboardKPIs = async (dateFrom?: string, dateTo?: string): Promise<DashboardKPIs> => {
  const params = new URLSearchParams();
  if (dateFrom) params.append('dateFrom', dateFrom);
  if (dateTo) params.append('dateTo', dateTo);

  const response = await api.get<{ data: DashboardKPIs }>(`/api/admin/dashboard/kpis?${params}`);
  return response.data.data;
};

export const getSalesTrend = async (days: number = 7): Promise<SalesTrendData[]> => {
  const response = await api.get<{ data: SalesTrendData[] }>(`/api/admin/dashboard/sales-trend?days=${days}`);
  return response.data.data;
};

export const getRecentOrders = async (limit: number = 10): Promise<Order[]> => {
  const response = await api.get<{ data: Order[] }>(`/api/admin/dashboard/recent-orders?limit=${limit}`);
  return response.data.data;
};

// Security API
export interface IPBlockRule {
  id?: number;
  ipAddress: string;
  type: 'blacklist' | 'whitelist';
  reason: string;
  isActive: boolean;
  expiresAt?: string;
  threatLevel: 'low' | 'medium' | 'high' | 'critical';
  countryCode?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface SecurityEventsParams {
  page?: number;
  limit?: number;
  eventType?: string;
  severity?: string;
  from?: string;
  to?: string;
  userId?: number;
  adminUserId?: number;
}

export interface SecurityEventsResponse {
  items: SecurityEvent[];       //  Changed from any[] to SecurityEvent[] and from 'events' to 'items'
  totalCount: number;
  page: number;                 //  Changed from 'currentPage' to 'page'
  totalPages: number;
  pageSize: number;
}

export const getIPBlockRules = async (): Promise<IPBlockRule[]> => {
  const response = await api.get<{ data: IPBlockRule[] }>('/admin/security/ip-blocks');
  return response.data.data;
};

export const createIPBlockRule = async (rule: Omit<IPBlockRule, 'id'>): Promise<IPBlockRule> => {
  const response = await api.post<{ data: IPBlockRule }>('/admin/security/ip-rules', rule);
  return response.data.data;
};

export const updateIPBlockRule = async (id: number, rule: Partial<IPBlockRule>): Promise<IPBlockRule> => {
  const response = await api.put<{ data: IPBlockRule }>(`/admin/security/ip-rules/${id}`, rule);
  return response.data.data;
};

export const deleteIPBlockRule = async (id: number): Promise<void> => {
  await api.delete(`/admin/security/ip-rules/${id}`);
};

export const getSecurityEvents = async (params: SecurityEventsParams = {}): Promise<SecurityEventsResponse> => {
  const response = await api.get<SecurityEventsResponse>('/admin/security/security-events', { params });
  return response.data;
};

// =====================================================
// LOGS API FUNCTIONS
// =====================================================

export const getLogStats = async (days: number = 30): Promise<LogStats> => {
  const response = await api.get<{ data: LogStats }>(`/admin/security/audit-logs/stats?days=${days}`);
  return response.data.data;
};

export const deleteOldLogs = async (olderThanDays: number): Promise<{ deletedCount: number }> => {
  const response = await api.delete<{ data: { deletedCount: number } }>(`/admin/security/audit-logs/cleanup?days=${olderThanDays}`);
  return response.data.data;
};

export const getLogDetails = async (logId: number): Promise<AdminAuditLog> => {
  const response = await api.get<{ data: AdminAuditLog }>(`/admin/security/audit-logs/${logId}`);
  return response.data.data;
};

// Variant management functions
export const createVariant = async (productId: number, variantData: any): Promise<any> => {
  const response = await api.post(`/products/${productId}/variants`, variantData);
  return unwrapApiData(response.data);
};

export const updateVariant = async (productId: number, variantId: number, variantData: any): Promise<any> => {
  const response = await api.put(`/products/${productId}/variants/${variantId}`, variantData);
  return unwrapApiData(response.data);
};

// Variant image management functions
export const uploadVariantImages = async (productId: number, variantId: number, files: FileList): Promise<ImageUploadResult[]> => {
  const formData = new FormData();

  console.log('Creating FormData for variant images with files:', {
    count: files.length,
    names: Array.from(files).map(f => f.name),
    productId,
    variantId
  });

  for (let i = 0; i < files.length; i++) {
    formData.append('files', files[i]);
    console.log(`Added file ${i + 1}:`, files[i].name, files[i].size);
  }

  // Debug FormData contents
  console.log('FormData entries:');
  for (const [key, value] of formData.entries()) {
    console.log(key, value);
  }

  const response = await api.post(
    `/products/${productId}/variants/${variantId}/images`,
    formData,
    {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    }
  );

  const raw = response.data;
  const data = raw?.data ?? raw;
  const uploaded = Array.isArray(data?.uploadedImages)
    ? data.uploadedImages
    : (data?.uploadedImages ?? data?.items ?? []);

  const vid = data?.variantId ?? variantId;

  return uploaded.map((u: any, idx: number) => ({
    productId: vid,
    imageUrl: u.imageUrl ?? u.url,
    imageId: u.imageId ?? u.id,
    displayOrder: (u.displayOrder ?? idx + 1),
    message: u.message ?? ''
  }));
};

export const deleteVariantImage = async (productId: number, variantId: number, imageId: string): Promise<void> => {
  await api.delete(`/products/${productId}/variants/${variantId}/images/${imageId}`);
};

export const deleteVariant = async (productId: number, variantId: number): Promise<any> => {
  const response = await api.delete(`/products/${productId}/variants/${variantId}`);
  return unwrapApiData(response.data);
};

// Inventory management for base products
export interface UpdateInventoryRequest {
  quantityInStock?: number;
  reservedQuantity?: number;
  reorderLevel?: number;
  maxStockLevel?: number;
  warehouseLocation?: string;
}

export interface CreateInventoryRequest extends UpdateInventoryRequest {
  productId: number;
}

export const createInventory = async (payload: CreateInventoryRequest) => {
  const resp = await api.post(`/inventory`, payload);
  return resp.data?.data ?? resp.data;
};

export const updateInventoryById = async (inventoryId: number, payload: UpdateInventoryRequest) => {
  const resp = await api.put(`/inventory/${inventoryId}`, payload);
  return resp.data?.data ?? resp.data;
};

export const adjustStock = async (productId: number, quantityDelta: number) => {
  const resp = await api.post(`/inventory/adjust-stock`, { productId, quantityDelta });
  return resp.data?.data ?? resp.data;
};

// Export the configured axios instance
export default api;

// Backward compatibility alias for services migrating from adminApi.ts
// This allows services to import { adminApiClient } from '@/lib/admin-api'
export const adminApiClient = api;
