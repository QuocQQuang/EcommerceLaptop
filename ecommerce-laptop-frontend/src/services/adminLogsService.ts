import { deleteOldLogs, exportLogs, getLogDetails, getLogs, getLogStats } from '@/lib/admin-api';
import {
  AdminAuditLog,
  LogExportRequest,
  LogsResponse,
  LogsSearchParams,
  LogStats
} from '@/types/admin/logs';

class AdminLogsService {
  /**
   * Get audit logs with filtering and pagination
   */
  async getLogs(params: LogsSearchParams = {}): Promise<LogsResponse> {
    const apiParams = {
      page: params.page || 1,
      limit: params.pageSize || 50,
      entityType: params.entityType,
      eventType: params.eventCategory,
      userId: params.userId,
      from: params.fromDate,
      to: params.toDate
    };

    const response = await getLogs(apiParams.page, apiParams.limit, apiParams as any);

    const payload = response?.data || { logs: [], pagination: { page: apiParams.page, limit: apiParams.limit, total: 0, totalPages: 0 } };
    const pg = payload.pagination || { page: apiParams.page, limit: apiParams.limit, total: 0, totalPages: 0 };

    return {
      data: (payload.logs || []).map((log: any): AdminAuditLog => ({
        id: log.id || 0,
        action: log.action || log.operation || 'Unknown Action',
        entity: log.entity || log.resource || 'Unknown Entity',
        entityId: log.entityId || log.resource_id,
        details: log.details || log.description || log.message || null,
        oldValues: log.oldValues || log.old_values || null,
        newValues: log.newValues || log.new_values || null,
        ipAddress: log.ipAddress || log.ip_address || log.ip || '0.0.0.0',
        userAgent: log.userAgent || log.user_agent || null,
        createdAt: log.createdAt || log.created_at || log.timestamp || new Date().toISOString(),
        adminUserId: log.adminUserId || log.admin_user_id || log.userId || log.user_id || 0,
        adminUser: log.adminUser || log.admin_user ? {
          id: log.adminUser?.id || log.admin_user?.id || log.adminUserId || log.admin_user_id || log.userId || log.user_id || 0,
          email: log.adminUser?.email || log.admin_user?.email || log.userEmail || log.user_email || 'unknown@example.com',
          displayName: log.adminUser?.displayName || log.admin_user?.display_name || log.userName || log.user_name || undefined
        } : undefined
      })),
      pagination: {
        page: pg.page,
        pageSize: pg.limit,
        totalItems: pg.total,
        totalPages: pg.totalPages,
        hasNext: (pg.page || 1) < (pg.totalPages || 0),
        hasPrevious: (pg.page || 1) > 1
      },
      success: true
    };
  }

  /**
   * Search audit logs with specific term
   * TODO: Implement with real API when available
   */
  async searchLogs(searchTerm: string, params: Omit<LogsSearchParams, 'searchQuery'> = {}): Promise<LogsResponse> {
    // Mock implementation - replace with real API call when available
    return {
      data: [],
      pagination: {
        page: params.page || 1,
        pageSize: params.pageSize || 50,
        totalItems: 0,
        totalPages: 0,
        hasNext: false,
        hasPrevious: false
      },
      success: true
    };
  }

  /**
   * Get user activity logs
   * TODO: Implement with real API when available
   */
  async getUserLogs(userId: number, days: number = 30): Promise<AdminAuditLog[]> {
    // Mock implementation - replace with real API call when available
    return [];
  }

  /**
   * Get admin activity logs
   * TODO: Implement with real API when available
   */
  async getAdminLogs(adminId?: number, params: LogsSearchParams = {}): Promise<LogsResponse> {
    // Mock implementation - replace with real API call when available
    return {
      data: [],
      pagination: {
        page: params.page || 1,
        pageSize: params.pageSize || 50,
        totalItems: 0,
        totalPages: 0,
        hasNext: false,
        hasPrevious: false
      },
      success: true
    };
  }

  /**
   * Export logs
   */
  async exportLogs(request: LogExportRequest): Promise<Blob> {
    // Map format to supported formats
    const format = request.format === 'json' ? 'csv' : request.format === 'excel' ? 'csv' : request.format;

    const filters: Record<string, string | number> = {};
    if (request.category) filters.entityType = request.category;
    if (request.level) filters.eventType = request.level;
    if (request.fromDate) filters.from = request.fromDate;
    if (request.toDate) filters.to = request.toDate;

    return await exportLogs(format as 'csv' | 'pdf', filters);
  }

  /**
   * Get log statistics
   */
  async getLogStats(days: number = 30): Promise<LogStats> {
    return await getLogStats(days);
  }

  /**
   * Delete old logs (admin only)
   */
  async deleteOldLogs(olderThanDays: number): Promise<{ deletedCount: number }> {
    return await deleteOldLogs(olderThanDays);
  }

  /**
   * Get log details by ID
   */
  async getLogDetails(logId: number): Promise<AdminAuditLog> {
    return await getLogDetails(logId);
  }
}

export const adminLogsService = new AdminLogsService();