// Admin Dashboard React Query Hooks

import { adminDashboardService } from '@/services/adminDashboardService';
import {
  DashboardKpiParams,
  DashboardKPIs,
  RecentOrder,
  RecentOrdersParams,
  SalesTrendDataPoint,
  SalesTrendParams
} from '@/types/admin';
import { useQuery, UseQueryOptions } from '@tanstack/react-query';

/**
 * Query Keys for Admin Dashboard
 * 
 * Organized query keys for efficient caching and invalidation:
 * - Hierarchical structure for related queries
 * - Parameter-based cache keys for dynamic queries
 * - Consistent naming convention across all hooks
 */
export const adminDashboardQueryKeys = {
  all: ['admin', 'dashboard'] as const,
  kpis: (params?: DashboardKpiParams) => [...adminDashboardQueryKeys.all, 'kpis', params] as const,
  salesTrends: (params?: SalesTrendParams) => [...adminDashboardQueryKeys.all, 'sales-trends', params] as const,
  recentOrders: (params?: RecentOrdersParams) => [...adminDashboardQueryKeys.all, 'recent-orders', params] as const,
  overview: (params?: DashboardKpiParams) => [...adminDashboardQueryKeys.all, 'overview', params] as const,
  lowStock: (threshold?: number) => [...adminDashboardQueryKeys.all, 'low-stock', threshold] as const,
  productPerformance: (days?: number) => [...adminDashboardQueryKeys.all, 'product-performance', days] as const,
};

/**
 * Hook: useAdminDashboardKPIs
 * 
 * Fetches dashboard KPI metrics with automatic refresh and caching.
 * 
 * Features:
 * - 5-minute cache with background refetch
 * - Error handling with retry logic
 * - Loading and error states management
 * - Parameter-based cache invalidation
 * 
 * @param params Optional date range filters
 * @param options React Query configuration options
 * @returns Query state with KPI data, loading, and error states
 * 
 * @example
 * ```typescript
 * const { data: kpis, isLoading, error } = useAdminDashboardKPIs({
 *   dateFrom: '2025-01-01',
 *   dateTo: '2025-01-31'
 * });
 * 
 * if (isLoading) return <Spinner />;
 * if (error) return <ErrorMessage error={error} />;
 * 
 * return <KPICards kpis={kpis} />;
 * ```
 */
export function useAdminDashboardKPIs(
  params: DashboardKpiParams = {},
  options?: UseQueryOptions<DashboardKPIs, Error>
) {
  return useQuery({
    queryKey: adminDashboardQueryKeys.kpis(params),
    queryFn: () => adminDashboardService.getDashboardKPIs(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes  
    refetchOnWindowFocus: false,
    retry: 3,
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
    ...options,
  });
}

/**
 * Hook: useAdminSalesTrends
 * 
 * Fetches sales trend data for dashboard charts and analytics.
 * 
 * Features:
 * - Configurable time periods (7, 30, 90 days)
 * - Chart-optimized data format
 * - Background updates for real-time trends
 * - Memory-efficient caching strategy
 * 
 * @param params Trend analysis parameters
 * @param options React Query configuration options
 * @returns Query state with sales trend data points
 * 
 * @example
 * ```typescript
 * const { data: trends, isLoading } = useAdminSalesTrends({ days: 30 });
 * 
 * return (
 *   <Chart
 *     data={trends}
 *     xDataKey="date"
 *     lines={[
 *       { dataKey: 'sales', color: '#8884d8' },
 *       { dataKey: 'orders', color: '#82ca9d' }
 *     ]}
 *   />
 * );
 * ```
 */
export function useAdminSalesTrends(
  params: SalesTrendParams = {},
  options?: UseQueryOptions<SalesTrendDataPoint[], Error>
) {
  return useQuery({
    queryKey: adminDashboardQueryKeys.salesTrends(params),
    queryFn: () => adminDashboardService.getSalesTrends(params),
    staleTime: 3 * 60 * 1000, // 3 minutes
    gcTime: 15 * 60 * 1000, // 15 minutes
    refetchInterval: 5 * 60 * 1000, // Auto-refresh every 5 minutes
    refetchOnWindowFocus: true,
    retry: 2,
    ...options,
  });
}

/**
 * Hook: useAdminRecentOrders
 * 
 * Fetches recent order activity for dashboard monitoring.
 * 
 * Features:
 * - Real-time order updates
 * - Configurable result limits
 * - Status-based filtering support
 * - Optimistic updates for status changes
 * 
 * @param params Query parameters (limit, filters)
 * @param options React Query configuration options
 * @returns Query state with recent order list
 * 
 * @example
 * ```typescript
 * const { data: orders, isLoading } = useAdminRecentOrders({ limit: 10 });
 * 
 * return (
 *   <OrderList>
 *     {orders?.map(order => (
 *       <OrderCard key={order.id} order={order} />
 *     ))}
 *   </OrderList>
 * );
 * ```
 */
export function useAdminRecentOrders(
  params: RecentOrdersParams = {},
  options?: UseQueryOptions<RecentOrder[], Error>
) {
  return useQuery({
    queryKey: adminDashboardQueryKeys.recentOrders(params),
    queryFn: () => adminDashboardService.getRecentOrders(params),
    staleTime: 2 * 60 * 1000, // 2 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
    refetchInterval: 3 * 60 * 1000, // Auto-refresh every 3 minutes
    refetchOnWindowFocus: true,
    retry: 3,
    ...options,
  });
}

/**
 * Hook: useAdminLowStock
 */
export function useAdminLowStock(threshold = 10, options?: UseQueryOptions<any[], Error>) {
  return useQuery({
    queryKey: adminDashboardQueryKeys.lowStock(threshold),
    queryFn: () => adminDashboardService.getLowStock(threshold),
    staleTime: 5 * 60 * 1000,
    gcTime: 15 * 60 * 1000,
    ...options,
  });
}

/**
 * Hook: useAdminProductPerformance
 */
export function useAdminProductPerformance(days = 30, options?: UseQueryOptions<any[], Error>) {
  return useQuery({
    queryKey: adminDashboardQueryKeys.productPerformance(days),
    queryFn: () => adminDashboardService.getProductPerformance(days),
    staleTime: 5 * 60 * 1000,
    gcTime: 15 * 60 * 1000,
    ...options,
  });
}

/**
 * Hook: useAdminDashboardOverview
 * 
 * Fetches complete dashboard data in a single optimized request.
 * 
 * Features:
 * - Combined KPIs, trends, and recent orders
 * - Reduced API calls for faster page loads
 * - Coordinated loading states
 * - Efficient bandwidth usage
 * 
 * @param params KPI date range parameters
 * @param options React Query configuration options
 * @returns Query state with complete dashboard overview
 * 
 * @example
 * ```typescript
 * const { data: overview, isLoading, error } = useAdminDashboardOverview();
 * 
 * if (isLoading) return <DashboardSkeleton />;
 * if (error) return <ErrorState error={error} />;
 * 
 * const { kpis, salesTrends, recentOrders } = overview;
 * 
 * return (
 *   <Dashboard>
 *     <KPISection kpis={kpis} />
 *     <TrendsSection data={salesTrends} />
 *     <OrdersSection orders={recentOrders} />
 *   </Dashboard>
 * );
 * ```
 */
export function useAdminDashboardOverview(
  params: DashboardKpiParams = {},
  options?: UseQueryOptions<{
    kpis: DashboardKPIs;
    salesTrends: SalesTrendDataPoint[];
    recentOrders: RecentOrder[];
  }, Error>
) {
  return useQuery({
    queryKey: adminDashboardQueryKeys.overview(params),
    queryFn: () => adminDashboardService.getDashboardOverview(params),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 15 * 60 * 1000, // 15 minutes
    refetchOnWindowFocus: false,
    retry: 3,
    retryDelay: (attemptIndex) => Math.min(1000 * 2 ** attemptIndex, 30000),
    ...options,
  });
}

/**
 * Utility: Dashboard Query Invalidation
 * 
 * Helper functions for manual cache invalidation and refresh.
 * Useful for real-time updates and data synchronization.
 * 
 * @example
 * ```typescript
 * import { useQueryClient } from '@tanstack/react-query';
 * import { invalidateAdminDashboard } from '@/hooks/admin/useAdminDashboard';
 * 
 * function RefreshButton() {
 *   const queryClient = useQueryClient();
 * 
 *   const handleRefresh = () => {
 *     invalidateAdminDashboard.all(queryClient);
 *   };
 * 
 *   return <Button onClick={handleRefresh}>Refresh Dashboard</Button>;
 * }
 * ```
 */
export const invalidateAdminDashboard = {
  all: (queryClient: any) => {
    return queryClient.invalidateQueries({
      queryKey: adminDashboardQueryKeys.all
    });
  },
  kpis: (queryClient: any, params?: DashboardKpiParams) => {
    return queryClient.invalidateQueries({
      queryKey: adminDashboardQueryKeys.kpis(params)
    });
  },
  salesTrends: (queryClient: any, params?: SalesTrendParams) => {
    return queryClient.invalidateQueries({
      queryKey: adminDashboardQueryKeys.salesTrends(params)
    });
  },
  recentOrders: (queryClient: any, params?: RecentOrdersParams) => {
    return queryClient.invalidateQueries({
      queryKey: adminDashboardQueryKeys.recentOrders(params)
    });
  },
  overview: (queryClient: any, params?: DashboardKpiParams) => {
    return queryClient.invalidateQueries({
      queryKey: adminDashboardQueryKeys.overview(params)
    });
  },
  lowStock: (queryClient: any, threshold?: number) => {
    return queryClient.invalidateQueries({
      queryKey: adminDashboardQueryKeys.lowStock(threshold)
    });
  },
  productPerformance: (queryClient: any, days?: number) => {
    return queryClient.invalidateQueries({
      queryKey: adminDashboardQueryKeys.productPerformance(days)
    });
  },
};