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
import { Order } from '@/types/api';
import { api, tokenManager } from './admin/http';
import type { ApiResponse } from './admin/http';

const logLevel = process.env.NEXT_PUBLIC_LOG_LEVEL || 'info';

// =====================================================
// API Response Types
// =====================================================

export type { ApiResponse } from './admin/http';
export { API_BASE, api, tokenManager } from './admin/http';

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

// Product, catalog, image, variant, and inventory APIs live in focused domain modules.
// Keep these re-exports so existing imports from '@/lib/admin-api' continue to work during migration.
export type {
  AdminProductsParams,
  AdminProductsResponse,
  ImageUploadResult,
  ProductFormData,
  ProductUpdateDto
} from '@/features/admin/products/types';
export {
  adjustStock,
  createInventory,
  createProduct,
  createVariant,
  deleteProduct,
  deleteProductImage,
  deleteVariant,
  deleteVariantImage,
  getAdminProduct,
  getAdminProducts,
  getProductVariants,
  updateInventoryById,
  updateProduct,
  updateVariant,
  uploadProductImages,
  uploadVariantImages
} from '@/features/admin/products/api';
export {
  mapFormToCreatePayload,
  mapFormToUpdatePayload,
  mapProductToForm
} from '@/features/admin/products/mappers';
export type {
  Brand,
  BrandFormData,
  Category,
  CategoryFormData
} from '@/features/admin/catalog/types';
export {
  createBrand,
  createCategory,
  deleteBrand,
  deleteCategory,
  forceDeleteBrand,
  forceDeleteCategory,
  getBrands,
  getBrandsWithCounts,
  getCategories,
  getCategoriesWithCounts,
  reassignAndDeleteBrand,
  reassignAndDeleteCategory,
  updateBrand,
  updateCategory
} from '@/features/admin/catalog/api';
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

// Export the configured axios instance
export default api;

// Backward compatibility alias for services migrating from adminApi.ts
// This allows services to import { adminApiClient } from '@/lib/admin-api'
export const adminApiClient = api;
