// Admin API Types - Based on backend ADMIN_API_COMPLETE_GUIDE.md

// =============================================================================
//  DASHBOARD TYPES
// =============================================================================

export interface KpiMetric {
  value: number;
  change: number;
  isPositive: boolean;
  unit: string;
  period: string;
}

export interface KpiValue {
  value: number;
  label: string;
}

export interface DashboardKPIs {
  todaySales: KpiMetric;
  newOrders: KpiMetric;
  lowStock: KpiValue;
  visitors: KpiMetric;
  revenue: KpiMetric;
  totalCustomers: KpiValue;
}

export interface SalesTrendDataPoint {
  name: string;
  sales: number;
  orders: number;
  date: string;
}

export interface RecentOrder {
  id: number;
  orderNumber?: string;
  customerName: string;
  customerEmail: string;
  total: number;
  status: 'pending' | 'confirmed' | 'processing' | 'shipped' | 'delivered' | 'cancelled';
  statusColor?: string;
  createdAt: string;
  itemCount: number;
}

export interface DashboardKpiParams {
  dateFrom?: string;
  dateTo?: string;
}

export interface SalesTrendParams {
  days?: number;
}

export interface RecentOrdersParams {
  limit?: number;
  page?: number;
  status?: string;
}

// =============================================================================
//  USER MANAGEMENT TYPES
// =============================================================================

export interface AdminUser {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string;
  isActive: boolean;
  roleId: number;
  roleName: string;
  createdAt: string;
  updatedAt: string;
  permissions?: string[];
}

export interface AdminAuthRequest {
  email: string;
  password: string;
}

export interface AdminUserListParams {
  page?: number;
  limit?: number;
  search?: string;
}

export interface AdminUserListResponse {
  users: AdminUser[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
  pageSize: number;
}

export interface CreateAdminUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  roleId: number;
  isActive: boolean;
}

export interface UpdateAdminUserRequest {
  firstName: string;
  lastName: string;
  email: string;
  roleId: number;
  isActive: boolean;
}

// =============================================================================
//  SECURITY MANAGEMENT TYPES  
// =============================================================================

export interface IPBlockRule {
  id?: number;
  ipAddress: string;
  type: 'blacklist' | 'whitelist';
  reason: string;
  isActive: boolean;
  expiresAt?: string;
  threatLevel: 'low' | 'medium' | 'high' | 'critical';
  countryCode?: string;
  violationCount?: number;
  lastViolation?: string;
  createdBy?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateIPBlockRuleRequest {
  ipAddress: string;
  type: 'blacklist' | 'whitelist';
  reason: string;
  isActive: boolean;
  expiresAt?: string;
  threatLevel: 'low' | 'medium' | 'high' | 'critical';
  countryCode?: string;
}

export interface RateLimitRule {
  id?: number;
  name: string;
  endpoint: string;
  method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'ALL';
  requestsPerMinute: number;
  requestsPerHour: number;
  requestsPerDay: number;
  isActive: boolean;
  priority?: number;
  description?: string;
  ipWhitelist?: string[] | string;
  userRoleExceptions?: string[] | string;
  apiKeyExceptions?: string[] | string;
  cooldownSeconds?: number;
  customErrorMessage?: string;
  createdBy?: string;
  createdAt?: string;
  updatedAt?: string;
}

export interface CreateRateLimitRuleRequest {
  name: string;
  endpoint: string;
  method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'ALL';
  requestsPerMinute: number;
  requestsPerHour: number;
  requestsPerDay: number;
  isActive: boolean;
  priority?: number;
  description?: string;
  ipWhitelist?: string[] | string;
  userRoleExceptions?: string[] | string;
  apiKeyExceptions?: string[] | string;
  cooldownSeconds?: number;
  customErrorMessage?: string;
}

export interface SecurityEventsParams {
  page?: number;
  limit?: number;
  eventType?: string;
  severity?: 'low' | 'medium' | 'high' | 'critical';
  from?: string;
  to?: string;
  userId?: number;
  adminUserId?: number;
}

export interface SecurityEventsResponse {
  items: SecurityEvent[];       //  Changed from 'events' to 'items' to match API
  totalCount: number;
  page: number;                 //  Changed from 'currentPage' to 'page' to match API  
  totalPages: number;
  pageSize: number;
}

export interface InvestigateEventRequest {
  notes: string;
}

export interface ResolveEventRequest {
  resolution: string;
  actionTaken?: string;
}

// Audit Log types removed as SystemAuditLogs table is deprecated.

export interface SecurityEvent {
  id: number;
  eventType: string;
  severity: 'low' | 'medium' | 'high' | 'critical';
  ipAddress?: string;
  userId?: number;                    //  Changed from string to number
  adminUserId?: number;               //  Changed from string to number  
  description: string;
  details?: string;
  status: 'new' | 'investigating' | 'resolved' | 'logged';  //  Added 'logged' status
  investigatedBy?: string;
  investigatedAt?: string;
  createdAt: string;
  //  Added missing fields from API response
  correlationId?: string;
  requiresReview?: boolean;
  riskScore?: string;
  wasBlocked?: boolean;
}

export interface ApiResponse<T = any> {
  success: boolean;
  data?: T;
  message?: string;
}

export interface AdminErrorResponse {
  error: string;
  message: string;
  statusCode: number;
  timestamp?: string;
  validationErrors?: Record<string, string[]>;
  requiredPermission?: string;
}

// =============================================================================
//  ROLE MANAGEMENT TYPES
// =============================================================================

export interface Permission {
  id: number;
  name: string;
  description: string;
  category: string;
}

export interface Role {
  id: number;
  name: string;
  description: string;
  permissions: Permission[];
  userCount: number;
  isSystem: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateRoleRequest {
  name: string;
  description: string;
  permissionIds: number[];
  isSystem?: boolean;
}

export interface UpdateRoleRequest {
  name?: string;
  description?: string;
  permissionIds?: number[];
  isSystem?: boolean;
}

// =============================================================================
//  SECURITY METRICS DTO
// =============================================================================

export interface SecurityMetrics {
  totalBlocked: number;
  rateLimitViolations: number;
  activeRules: number;
  suspiciousIPs: number;
  todayBlocked: number;
  topBlockedIPs: Array<{ ip: string; count: number; lastSeen: string | Date }>;
  rateLimitStats: Array<{ endpoint: string; violations: number; lastViolation: string | Date }>;
}
