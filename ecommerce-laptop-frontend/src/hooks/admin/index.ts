// Admin Hooks - Centralized exports for all admin-related React Query hooks

/**
 * Admin Dashboard Hooks
 */
export {
  adminDashboardQueryKeys,
  invalidateAdminDashboard, useAdminDashboardKPIs, useAdminDashboardOverview,
  useAdminLowStock,
  useAdminProductPerformance, useAdminRecentOrders, useAdminSalesTrends
} from './useAdminDashboard';

/**
 * Admin User Management Hooks
 */
export {
  adminUserQueryKeys,
  invalidateAdminUsers, useAdminUser,
  useAdminUserActivity, useAdminUsers, useCreateAdminUser, useDeleteAdminUser, useSearchAdminUsers, useToggleAdminUserStatus, useUpdateAdminUser
} from './useAdminUsers';

/**
 * Admin Security Management Hooks
 */
export {
  adminSecurityQueryKeys,
  invalidateAdminSecurity, useAuditLog, useAuditLogs, useCreateIPBlockRule, useCreateRateLimitRule, useDeleteIPBlockRule, useInvestigateSecurityEvent, useIPBlockRules, useRateLimitRules, useResolveSecurityEvent, useSecurityEvent, useSecurityEvents, useSecurityMetrics
} from './useAdminSecurity';

// Import query keys and invalidation functions
import { adminDashboardQueryKeys, invalidateAdminDashboard } from './useAdminDashboard';
import { adminSecurityQueryKeys, invalidateAdminSecurity } from './useAdminSecurity';
import { adminUserQueryKeys, invalidateAdminUsers } from './useAdminUsers';

/**
 * Combined Query Key Utils
 */
export const adminQueryKeys = {
  dashboard: adminDashboardQueryKeys,
  users: adminUserQueryKeys,
  security: adminSecurityQueryKeys,
};

/**
 * Global Admin Cache Invalidation
 */
export const invalidateAllAdminQueries = (queryClient: any) => {
  return Promise.all([
    invalidateAdminDashboard.all(queryClient),
    invalidateAdminUsers.all(queryClient),
    invalidateAdminSecurity.all(queryClient),
  ]);
};