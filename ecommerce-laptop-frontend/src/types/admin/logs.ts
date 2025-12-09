export type LogLevel = 'ERROR' | 'WARNING' | 'INFO' | 'DEBUG';

export type LogCategory = 
  | 'AUTHENTICATION' 
  | 'USER_ACTION' 
  | 'SYSTEM' 
  | 'SECURITY'
  | 'ORDER'
  | 'PAYMENT'
  | 'EMAIL'
  | 'API';

export interface AdminAuditLog {
  id: number;
  action: string; // e.g., "users:create", "products:delete"
  entity: string; // e.g., "User", "Product"
  entityId?: string; // ID of the affected entity
  details?: string; // JSON string with additional details
  oldValues?: string; // JSON string with old values (for updates)
  newValues?: string; // JSON string with new values (for creates/updates)
  ipAddress: string;
  userAgent?: string;
  createdAt: string; // DateTime from backend
  adminUserId: number;
  adminUser?: {
    id: number;
    email: string;
    displayName?: string;
  };
}

export interface LogsSearchParams {
  searchQuery?: string;
  fromDate?: string;
  toDate?: string;
  entityType?: string; // e.g., "User", "Product"
  eventCategory?: string; // e.g., "users:create", "products:delete"  
  userId?: string;
  page?: number;
  pageSize?: number;
}

export interface LogsResponse {
  data: AdminAuditLog[];
  pagination: {
    page: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
    hasNext: boolean;
    hasPrevious: boolean;
  };
  success: boolean;
  message?: string;
}

export interface LogExportRequest {
  format: 'csv' | 'json' | 'excel';
  fromDate?: string;
  toDate?: string;
  category?: LogCategory;
  level?: LogLevel;
  searchQuery?: string;
}

export interface LogStats {
  totalLogs: number;
  errorCount: number;
  warningCount: number;
  infoCount: number;
  debugCount: number;
  categoryCounts: Record<LogCategory, number>;
  recentActivity: AdminAuditLog[];
  topUsers: Array<{
    userId: number;
    userName: string;
    logCount: number;
  }>;
  topIPs: Array<{
    ipAddress: string;
    logCount: number;
    location?: string;
  }>;
}