import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { adminLogsService } from '@/services/adminLogsService';
import { 
  LogsSearchParams, 
  LogsResponse, 
  AdminAuditLog, 
  LogExportRequest,
  LogStats 
} from '@/types/admin/logs';

// Query Keys
export const adminLogsQueryKeys = {
  all: ['admin-logs'] as const,
  lists: () => [...adminLogsQueryKeys.all, 'list'] as const,
  list: (params: LogsSearchParams) => [...adminLogsQueryKeys.lists(), params] as const,
  search: (searchTerm: string, params?: LogsSearchParams) => 
    [...adminLogsQueryKeys.all, 'search', searchTerm, params] as const,
  userLogs: (userId: number, days: number) => 
    [...adminLogsQueryKeys.all, 'user', userId, days] as const,
  adminLogs: (adminId?: number, params?: LogsSearchParams) => 
    [...adminLogsQueryKeys.all, 'admin', adminId, params] as const,
  stats: (days: number) => [...adminLogsQueryKeys.all, 'stats', days] as const,
  details: (logId: number) => [...adminLogsQueryKeys.all, 'details', logId] as const,
};

/**
 * Hook: useAdminLogs
 * Fetches admin audit logs with filtering and pagination
 */
export function useAdminLogs(params: LogsSearchParams = {}) {
  return useQuery({
    queryKey: adminLogsQueryKeys.list(params),
    queryFn: () => adminLogsService.getLogs(params),
    staleTime: 30000, // 30 seconds
    refetchOnWindowFocus: false,
    refetchInterval: 60000, // Refresh every minute for real-time logs
  });
}

/**
 * Hook: useAdminLogsSearch
 * Search audit logs with specific search term
 */
export function useAdminLogsSearch(
  searchTerm: string, 
  params: Omit<LogsSearchParams, 'searchQuery'> = {},
  enabled: boolean = true
) {
  return useQuery({
    queryKey: adminLogsQueryKeys.search(searchTerm, params),
    queryFn: () => adminLogsService.searchLogs(searchTerm, params),
    enabled: enabled && searchTerm.length > 0,
    staleTime: 30000,
  });
}

/**
 * Hook: useUserLogs
 * Fetches logs for a specific user
 */
export function useUserLogs(userId: number, days: number = 30) {
  return useQuery({
    queryKey: adminLogsQueryKeys.userLogs(userId, days),
    queryFn: () => adminLogsService.getUserLogs(userId, days),
    staleTime: 60000, // 1 minute
  });
}

/**
 * Hook: useAdminUserLogs
 * Fetches logs for admin users
 */
export function useAdminUserLogs(adminId?: number, params: LogsSearchParams = {}) {
  return useQuery({
    queryKey: adminLogsQueryKeys.adminLogs(adminId, params),
    queryFn: () => adminLogsService.getAdminLogs(adminId, params),
    staleTime: 30000,
  });
}

/**
 * Hook: useLogStats
 * Fetches log statistics and analytics
 */
export function useLogStats(days: number = 30) {
  return useQuery({
    queryKey: adminLogsQueryKeys.stats(days),
    queryFn: () => adminLogsService.getLogStats(days),
    staleTime: 300000, // 5 minutes
  });
}

/**
 * Hook: useLogDetails
 * Fetches detailed information for a specific log entry
 */
export function useLogDetails(logId: number, enabled: boolean = true) {
  return useQuery({
    queryKey: adminLogsQueryKeys.details(logId),
    queryFn: () => adminLogsService.getLogDetails(logId),
    enabled,
    staleTime: Infinity, // Log details don't change
  });
}

/**
 * Hook: useExportLogs
 * Mutation for exporting logs
 */
export function useExportLogs() {
  return useMutation({
    mutationFn: (request: LogExportRequest) => adminLogsService.exportLogs(request),
    onSuccess: (blob, variables) => {
      // Auto-download the exported file
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      
      const timestamp = new Date().toISOString().slice(0, 10);
      const extension = variables.format === 'excel' ? 'xlsx' : variables.format;
      link.download = `admin-logs-${timestamp}.${extension}`;
      
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    },
  });
}

/**
 * Hook: useDeleteOldLogs
 * Mutation for cleaning up old logs
 */
export function useDeleteOldLogs() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: (olderThanDays: number) => adminLogsService.deleteOldLogs(olderThanDays),
    onSuccess: () => {
      // Invalidate all logs queries to refresh data
      queryClient.invalidateQueries({ queryKey: adminLogsQueryKeys.all });
    },
  });
}

/**
 * Hook: useRefreshLogs
 * Utility for manually refreshing logs data
 */
export function useRefreshLogs() {
  const queryClient = useQueryClient();
  
  return () => {
    queryClient.invalidateQueries({ queryKey: adminLogsQueryKeys.all });
  };
}

/**
 * Prefetch functions for performance optimization
 */
export const adminLogsPrefetch = {
  logs: (queryClient: any, params: LogsSearchParams) => {
    return queryClient.prefetchQuery({
      queryKey: adminLogsQueryKeys.list(params),
      queryFn: () => adminLogsService.getLogs(params),
      staleTime: 30000,
    });
  },
  
  stats: (queryClient: any, days: number = 30) => {
    return queryClient.prefetchQuery({
      queryKey: adminLogsQueryKeys.stats(days),
      queryFn: () => adminLogsService.getLogStats(days),
      staleTime: 300000,
    });
  },
  
  userLogs: (queryClient: any, userId: number, days: number = 30) => {
    return queryClient.prefetchQuery({
      queryKey: adminLogsQueryKeys.userLogs(userId, days),
      queryFn: () => adminLogsService.getUserLogs(userId, days),
      staleTime: 60000,
    });
  },
};