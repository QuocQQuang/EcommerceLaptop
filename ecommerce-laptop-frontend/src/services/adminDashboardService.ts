// Admin Dashboard Service - Handles all dashboard-related API operations

import { adminApiClient } from '@/lib/admin-api';
import {
  ApiResponse,
  DashboardKPIs,
  DashboardKpiParams,
  RecentOrder,
  RecentOrdersParams,
  SalesTrendDataPoint,
  SalesTrendParams
} from '@/types/admin';

/**
 * Admin Dashboard Service Class
 * 
 * Provides methods for:
 * - Dashboard KPI metrics with comparison data
 * - Sales trend analytics with customizable periods
 * - Recent order management with real-time updates
 * 
 * All methods use AdminApiClient for authenticated requests
 * with proper error handling and caching strategies.
 */
export class AdminDashboardService {
  private readonly client = adminApiClient;

  /**
   * Get dashboard KPI metrics
   * 
   * Fetches key performance indicators including:
   * - Today's sales with percentage change
   * - New orders with growth metrics  
   * - Low stock alerts with item counts
   * - Visitor analytics with trends
   * - Revenue metrics with comparisons
   * - Customer analytics
   * 
   * @param params Optional date range filters
   * @returns Promise<DashboardKPIs> KPI metrics with change indicators
   */
  async getDashboardKPIs(params: DashboardKpiParams = {}): Promise<DashboardKPIs> {
    // Use Next.js proxy route instead of direct backend call
    // This ensures cookie-based authentication is properly forwarded
    const searchParams = new URLSearchParams();
    if (params.dateFrom) searchParams.set('dateFrom', params.dateFrom);
    if (params.dateTo) searchParams.set('dateTo', params.dateTo);

    const url = `/api/admin/dashboard/kpis${searchParams.toString() ? `?${searchParams.toString()}` : ''}`;

    const response = await fetch(url, {
      method: 'GET',
      credentials: 'include', // Include HTTP-only cookies
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Authentication failed');
      }
      throw new Error(`Failed to fetch dashboard KPIs: ${response.status}`);
    }

    const data = await response.json();

    if (!data.success || !data.data) {
      throw new Error(data.message || 'Failed to fetch dashboard KPIs');
    }

    return data.data;
  }

  /**
   * Get sales trend data for charts
   * 
   * Retrieves time-series data for:
   * - Daily/weekly/monthly sales trends
   * - Order volume analytics 
   * - Revenue progression over time
   * - Comparative performance metrics
   * 
   * @param params Trend analysis parameters (days, period)
   * @returns Promise<SalesTrendDataPoint[]> Chart-ready trend data
   */
  async getSalesTrends(params: SalesTrendParams = {}): Promise<SalesTrendDataPoint[]> {
    // Use Next.js proxy route instead of direct backend call
    const searchParams = new URLSearchParams();
    if (params.days) searchParams.set('days', params.days.toString());

    const url = `/api/admin/dashboard/sales-trend${searchParams.toString() ? `?${searchParams.toString()}` : ''}`;

    const response = await fetch(url, {
      method: 'GET',
      credentials: 'include', // Include HTTP-only cookies
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Authentication failed');
      }
      throw new Error(`Failed to fetch sales trends: ${response.status}`);
    }

    const data = await response.json();

    if (!data.success || !data.data) {
      throw new Error(data.message || 'Failed to fetch sales trends');
    }

    return data.data;
  }

  /**
   * Get recent orders with basic details
   * 
   * Fetches latest order activity including:
   * - Customer information (name, email)
   * - Order totals and status
   * - Item counts and timestamps
   * - Payment and shipping status
   * 
   * @param params Query parameters (limit, filters)
   * @returns Promise<RecentOrder[]> Recent order list
   */
  async getRecentOrders(params: RecentOrdersParams = {}): Promise<RecentOrder[]> {
    // Use Next.js proxy route instead of direct backend call
    const searchParams = new URLSearchParams();
    if (params.limit) searchParams.set('limit', params.limit.toString());
    if (params.page) searchParams.set('page', params.page.toString());
    if (params.status) searchParams.set('status', params.status);

    const url = `/api/admin/dashboard/recent-orders${searchParams.toString() ? `?${searchParams.toString()}` : ''}`;

    const response = await fetch(url, {
      method: 'GET',
      credentials: 'include', // Include HTTP-only cookies
      headers: {
        'Content-Type': 'application/json',
      },
    });

    if (!response.ok) {
      if (response.status === 401) {
        throw new Error('Authentication failed');
      }
      throw new Error(`Failed to fetch recent orders: ${response.status}`);
    }

    const data = await response.json();

    if (!data.success || !data.data) {
      throw new Error(data.message || 'Failed to fetch recent orders');
    }

    return data.data;
  }

  /**
   * Get low stock products
   */
  async getLowStock(threshold = 10) {
    const url = `/api/admin/dashboard/low-stock?threshold=${encodeURIComponent(threshold)}`;
    const response = await fetch(url, { method: 'GET', credentials: 'include', headers: { 'Content-Type': 'application/json' } });
    if (!response.ok) throw new Error(`Failed to fetch low stock: ${response.status}`);
    const data = await response.json();
    if (!data.success || !data.data) throw new Error(data.message || 'Failed to fetch low stock');
    return data.data as any[];
  }

  /**
   * Get product performance data
   */
  async getProductPerformance(days = 30) {
    const url = `/api/admin/dashboard/product-performance?days=${encodeURIComponent(days)}`;
    const response = await fetch(url, { method: 'GET', credentials: 'include', headers: { 'Content-Type': 'application/json' } });
    if (!response.ok) throw new Error(`Failed to fetch product performance: ${response.status}`);
    const data = await response.json();
    if (!data.success || !data.data) throw new Error(data.message || 'Failed to fetch product performance');
    return data.data as any[];
  }

  /**
   * Export dashboard report as CSV or PDF
   */
  async exportReport(format: 'csv' | 'pdf' = 'csv', params: DashboardKpiParams = {}) {
    const searchParams = new URLSearchParams();
    searchParams.set('format', format);
    if (params.dateFrom) searchParams.set('dateFrom', params.dateFrom);
    if (params.dateTo) searchParams.set('dateTo', params.dateTo);
    const url = `/api/admin/dashboard/export-report?${searchParams.toString()}`;
    const response = await fetch(url, { method: 'POST', credentials: 'include' });
    if (!response.ok) throw new Error(`Failed to export report: ${response.status}`);
    const blob = await response.blob();
    return blob;
  }

  /**
   * Get dashboard overview data (combined endpoint)
   * 
   * Single request that fetches all dashboard data:
   * - KPI metrics
   * - Sales trends (last 30 days)
   * - Recent orders (last 10)
   * 
   * Optimized for dashboard page load performance.
   * 
   * @param kpiParams Optional KPI date filters
   * @returns Promise<DashboardOverview> Complete dashboard data
   */
  async getDashboardOverview(kpiParams: DashboardKpiParams = {}): Promise<{
    kpis: DashboardKPIs;
    salesTrends: SalesTrendDataPoint[];
    recentOrders: RecentOrder[];
  }> {
    // Use Next.js proxy route to ensure cookie session is forwarded
    const searchParams = new URLSearchParams();
    if (kpiParams.dateFrom) searchParams.set('dateFrom', kpiParams.dateFrom);
    if (kpiParams.dateTo) searchParams.set('dateTo', kpiParams.dateTo);

    const url = `/api/admin/dashboard/overview${searchParams.toString() ? `?${searchParams.toString()}` : ''}`;
    const response = await fetch(url, { method: 'GET', credentials: 'include' });
    if (!response.ok) throw new Error(`Failed to fetch dashboard overview: ${response.status}`);
    const data = await response.json();
    if (!data.success || !data.data) throw new Error(data.message || 'Failed to fetch dashboard overview');
    return data.data as { kpis: DashboardKPIs; salesTrends: SalesTrendDataPoint[]; recentOrders: RecentOrder[] };
  }

  /**
   * Refresh dashboard data (force cache invalidation)
   * 
   * Triggers a manual refresh of all dashboard metrics.
   * Useful for real-time updates when critical events occur.
   * 
   * @returns Promise<boolean> Success status
   */
  async refreshDashboard(): Promise<boolean> {
    try {
      const response = await this.client.post<ApiResponse<boolean>>(
        '/api/admin/dashboard/refresh'
      );

      return response.data.success === true;
    } catch (error) {
      console.error('Dashboard refresh failed:', error);
      return false;
    }
  }
}

/**
 * Singleton instance for consistent usage across the application
 * 
 * Usage examples:
 * ```typescript
 * import { adminDashboardService } from '@/services/adminDashboardService';
 * 
 * // Get KPIs for current month
 * const kpis = await adminDashboardService.getDashboardKPIs({
 *   dateFrom: '2024-01-01',
 *   dateTo: '2024-01-31'
 * });
 * 
 * // Get sales trends for last 7 days  
 * const trends = await adminDashboardService.getSalesTrends({ days: 7 });
 * 
 * // Get recent orders (limit 5)
 * const orders = await adminDashboardService.getRecentOrders({ limit: 5 });
 * ```
 */
export const adminDashboardService = new AdminDashboardService();