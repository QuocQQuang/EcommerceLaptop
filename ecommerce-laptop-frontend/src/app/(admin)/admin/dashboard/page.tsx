'use client';

import { DateRangeExport, ExportButtons } from '@/components/admin/ExportButtons';
import { Badge } from '@/components/ui/badge';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle
} from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  PermissionGuard,
  useAdminAuth
} from '@/contexts/AdminAuthContext';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import {
  useAdminDashboardKPIs,
  useAdminProductPerformance,
  useAdminRecentOrders,
  useAdminSalesTrends,
  useSecurityEvents
} from '@/hooks/admin';
import { useExport } from '@/hooks/useExport';
import { PERMISSIONS } from '@/lib/admin-api';
import { formatCurrencyPrice } from '@/lib/currency';
import logger from '@/lib/logger';
import {
  RecentOrder,
  SecurityEventsResponse
} from '@/types/admin';
import {
  Activity, AlertCircle,
  Clock, DollarSign,
  Download,
  FileText, Gift, LucideIcon, Package, Settings, Shield, ShoppingCart, TrendingUp, Users
} from 'lucide-react';
import Link from 'next/link';
import {
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Line,
  LineChart,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts';

// Colors for pie slices
const productColors = ['#0088FE', '#00C49F', '#FFBB28', '#FF8042', '#8884D8', '#34d399', '#f472b6', '#a78bfa'];

const MetricCard = ({
  title,
  value,
  change,
  icon: Icon,
  description,
  loading = false
}: {
  title: string;
  value: string | number;
  change?: number;
  icon: LucideIcon;
  description?: string;
  loading?: boolean;
}) => {
  if (loading) {
    return (
      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
          <Skeleton className="h-4 w-[100px]" />
          <Skeleton className="h-4 w-4" />
        </CardHeader>
        <CardContent>
          <Skeleton className="h-8 w-[60px] mb-2" />
          <Skeleton className="h-3 w-[120px]" />
        </CardContent>
      </Card>
    );
  }

  return (
    <Card>
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
        <CardTitle className="text-sm font-medium">{title}</CardTitle>
        <Icon className="h-4 w-4 text-muted-foreground" />
      </CardHeader>
      <CardContent>
        <div className="text-2xl font-bold">{value.toLocaleString('vi-VN')}</div>
        {change !== undefined && (
          <p className="text-xs text-muted-foreground flex items-center">
            {change > 0 ? (
              <TrendingUp className="h-3 w-3 mr-1 text-green-500" />
            ) : (
              <TrendingUp className="h-3 w-3 mr-1 text-red-500 rotate-180" />
            )}
            {change > 0 ? '+' : ''}{
              (() => {
                const abs = Math.abs(change);
                const rounded = abs >= 1 ? abs.toFixed(1) : abs.toFixed(2);
                return rounded.replace(/\.0$/, '').replace(/0+$/, '').replace(/\.$/, '');
              })()
            }% so với tháng trước
          </p>
        )}
        {description && (
          <p className="text-xs text-muted-foreground mt-1">{description}</p>
        )}
      </CardContent>
    </Card>
  );
};

const QuickActions = () => {
  const { hasPermission } = useAdminAuth();

  const actions = [
    {
      title: 'Quản lý Admin',
      description: 'Thêm, sửa, xóa tài khoản người dùng',
      href: '/admin/users',
      icon: Users,
      permission: PERMISSIONS.USERS_READ,
      color: 'bg-blue-500',
    },
    {
      title: 'Quản lý sản phẩm',
      description: 'Thêm sản phẩm mới và cập nhật kho',
      href: '/admin/products',
      icon: Package,
      permission: PERMISSIONS.PRODUCTS_READ,
      color: 'bg-green-500',
    },
    {
      title: 'Xử lý đơn hàng',
      description: 'Xem và xử lý đơn hàng mới',
      href: '/admin/orders',
      icon: ShoppingCart,
      permission: PERMISSIONS.ORDERS_READ,
      color: 'bg-orange-500',
    },
    {
      title: 'Quản lý khuyến mãi',
      description: 'Tạo và quản lý các chương trình khuyến mãi',
      href: '/admin/promotions',
      icon: Gift,
      permission: PERMISSIONS.PROMOTIONS_READ,
      color: 'bg-purple-500',
    },
    {
      title: 'Cài đặt hệ thống',
      description: 'Cấu hình các thiết lập hệ thống',
      href: '/admin/settings',
      icon: Settings,
      permission: PERMISSIONS.SETTINGS_READ,
      color: 'bg-gray-500',
    },
    {
      title: 'Nhật ký hệ thống',
      description: 'Xem logs và hoạt động của hệ thống',
      href: '/admin/logs',
      icon: FileText,
      permission: PERMISSIONS.LOGS_READ,
      color: 'bg-red-500',
    },
  ];

  const availableActions = actions.filter(action => hasPermission(action.permission));

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center">
          <Activity className="h-5 w-5 mr-2" />
          Thao tác nhanh
        </CardTitle>
        <CardDescription>
          Các chức năng thường dùng
        </CardDescription>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {availableActions.map((action) => {
            const Icon = action.icon;
            return (
              <Link
                key={action.title}
                href={action.href}
                className="flex items-start space-x-3 p-3 rounded-lg border hover:bg-accent transition-colors"
              >
                <div className={`p-2 rounded-lg ${action.color} text-white`}>
                  <Icon className="h-4 w-4" />
                </div>
                <div className="flex-1 space-y-1">
                  <p className="text-sm font-medium">{action.title}</p>
                  <p className="text-xs text-muted-foreground">{action.description}</p>
                </div>
              </Link>
            );
          })}
        </div>
      </CardContent>
    </Card>
  );
};

const RecentActivity = ({
  recentOrders,
  securityEvents,
  isLoadingOrders,
  isLoadingSecurity
}: {
  recentOrders?: RecentOrder[];
  securityEvents?: SecurityEventsResponse;  //  Changed to match new interface
  isLoadingOrders: boolean;
  isLoadingSecurity: boolean;
}) => {
  // Combine recent orders and security events into activity feed
  interface Activity {
    id: string;
    type: string;
    message: string;
    time: string;
    icon: LucideIcon;
    color: string;
  }

  const activities: Activity[] = [];

  // Add recent orders
  if (recentOrders) {
    recentOrders.slice(0, 3).forEach((order) => {
      activities.push({
        id: `order-${order.id}`,
        type: 'order',
        message: `Đơn hàng mới từ ${order.customerName}: ${formatCompactCurrency(order.total)}`,
        time: formatDateTime(order.createdAt),
        icon: ShoppingCart,
        color: getStatusColor(order.status),
      });
    });
  }

  // Add security events
  if (securityEvents && 'items' in securityEvents) {
    (securityEvents as any).items.slice(0, 2).forEach((event: any) => {
      activities.push({
        id: `security-${event.id}`,
        type: 'security',
        message: `event: ${event.description}`,
        time: formatDateTime(event.createdAt),
        icon: Shield,
        color: getSeverityColor(event.severity),
      });
    });
  }

  // Sort by most recent
  activities.sort((a, b) => new Date(b.time).getTime() - new Date(a.time).getTime());

  function getStatusColor(status: string) {
    switch (status) {
      case 'pending': return 'text-yellow-500';
      case 'confirmed': return 'text-blue-500';
      case 'processing': return 'text-purple-500';
      case 'shipped': return 'text-orange-500';
      case 'delivered': return 'text-green-500';
      case 'cancelled': return 'text-red-500';
      default: return 'text-gray-500';
    }
  }

  function getSeverityColor(severity: string) {
    switch (severity) {
      case 'low': return 'text-green-500';
      case 'medium': return 'text-yellow-500';
      case 'high': return 'text-orange-500';
      case 'critical': return 'text-red-500';
      default: return 'text-gray-500';
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center">
          <Clock className="h-5 w-5 mr-2" />
          Hoạt động gần đây
        </CardTitle>
      </CardHeader>
      <CardContent>
        {(isLoadingOrders || isLoadingSecurity) ? (
          <div className="space-y-4">
            {[1, 2, 3, 4].map((i) => (
              <div key={i} className="flex items-start space-x-3">
                <Skeleton className="h-4 w-4 rounded" />
                <div className="flex-1 space-y-2">
                  <Skeleton className="h-4 w-full" />
                  <Skeleton className="h-3 w-16" />
                </div>
              </div>
            ))}
          </div>
        ) : activities.length > 0 ? (
          <div className="space-y-4">
            {activities.slice(0, 5).map((activity) => {
              const Icon = activity.icon;
              return (
                <div key={activity.id} className="flex items-start space-x-3">
                  <div className={`p-1 ${activity.color}`}>
                    <Icon className="h-4 w-4" />
                  </div>
                  <div className="flex-1 space-y-1">
                    <p className="text-sm">{activity.message}</p>
                    <p className="text-xs text-muted-foreground">{activity.time}</p>
                  </div>
                </div>
              );
            })}
          </div>
        ) : (
          <p className="text-sm text-muted-foreground">Không có hoạt động gần đây</p>
        )}
        <div className="mt-4 pt-4 border-t">
          <Link href="/admin/logs" className="text-sm text-primary hover:underline">
            Xem tất cả hoạt động 
          </Link>
        </div>
      </CardContent>
    </Card>
  );
};

// Utility functions for Vietnamese formatting - moved inside component

const formatNumber = (num: number) => {
  return new Intl.NumberFormat('vi-VN').format(num);
};

const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  });
};

const formatDateTime = (dateString: string) => {
  return new Date(dateString).toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit'
  });
};

const formatCompactCurrency = (amount: number) => {
  if (amount >= 1_000_000_000) {
    return `${(amount / 1_000_000_000).toFixed(1)}B VN`;
  } else if (amount >= 1_000_000) {
    return `${(amount / 1_000_000).toFixed(1)}M VN`;
  } else if (amount >= 1_000) {
    return `${(amount / 1_000).toFixed(0)}K VN`;
  }
  // Fallback small values: format directly as USD to avoid referencing component-scoped formatCurrency
  return new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD',
    maximumFractionDigits: 0,
  }).format(amount);
};

export default function DashboardPage() {
  const { selectedCurrency } = useCurrencyContext();
  const { user, isSuperAdmin } = useAdminAuth();
  const { exportRevenueReport, exportInventoryReport, exportSecurityReport } = useExport();

  // Helper functions
  const formatCurrency = (amount: number) => {
    return formatCurrencyPrice(amount, selectedCurrency);
  };

  // Real API calls using our admin hooks with enhanced real-time features
  // Calculate current month range [startOfMonth, endOfMonth]
  const now = new Date();
  const startOfMonth = new Date(now.getFullYear(), now.getMonth(), 1);
  const endOfMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0);

  const {
    data: kpis,
    isLoading: kpisLoading,
    error: kpisError,
    refetch: refetchKpis
  } = useAdminDashboardKPIs({
    dateFrom: startOfMonth.toISOString(),
    dateTo: endOfMonth.toISOString()
  }, {
    refetchInterval: 30000, // Auto-refresh every 30 seconds
    refetchOnWindowFocus: true, // Refetch when user comes back to tab
    refetchIntervalInBackground: true, // Keep refreshing even when tab is not active
    onSuccess: (data: any) => {
      logger.info(' Dashboard KPIs loaded successfully', {
        totalRevenue: data?.totalRevenue,
        totalOrders: data?.totalOrders,
        totalCustomers: data?.totalCustomers,
        totalProducts: data?.totalProducts,
        revenueChange: data?.revenueChange,
        ordersChange: data?.ordersChange,
        customersChange: data?.customersChange,
        productsChange: data?.productsChange,
        timestamp: new Date().toISOString()
      });
    },
    onError: (error: any) => {
      logger.error(' Dashboard KPIs failed to load', {
        message: error.message,
        status: error.response?.status,
        responseData: error.response?.data,
        url: error.config?.url
      });
    }
  } as any);

  const {
    data: salesTrend,
    isLoading: salesTrendLoading,
    error: salesTrendError
  } = useAdminSalesTrends({
    days: 30 // Last 30 days
  }, {
    refetchInterval: 60000, // Auto-refresh every minute
    staleTime: 5 * 60 * 1000, // Consider data stale after 5 minutes
    onSuccess: (data: any) => {
      logger.info(' Sales trend data loaded successfully', {
        dataPoints: data?.data?.length || 0,
        dateRange: data?.data ? {
          first: data.data[0]?.date,
          last: data.data[data.data.length - 1]?.date
        } : null,
        totalRevenue: data?.data?.reduce((sum: number, point: any) => sum + (point.revenue || 0), 0) || 0,
        averageDaily: data?.data?.length ? (data.data.reduce((sum: number, point: any) => sum + (point.revenue || 0), 0) / data.data.length).toFixed(2) : 0,
        timestamp: new Date().toISOString()
      });
    },
    onError: (error: any) => {
      logger.error(' Sales trend data failed to load', {
        message: error.message,
        status: error.response?.status,
        responseData: error.response?.data,
        url: error.config?.url
      });
    }
  } as any);

  const {
    data: recentOrders,
    isLoading: recentOrdersLoading,
    error: recentOrdersError
  } = useAdminRecentOrders({
    limit: 10
  }, {
    refetchInterval: 15000, // Auto-refresh every 15 seconds for new orders
    refetchOnWindowFocus: true,
    onSuccess: (data: any) => {
      logger.info(' Recent orders loaded successfully', {
        ordersCount: data?.orders?.length || 0,
        totalValue: data?.orders?.reduce((sum: number, order: any) => sum + (order.totalAmount || 0), 0) || 0,
        statusBreakdown: data?.orders?.reduce((acc: any, order: any) => {
          acc[order.status] = (acc[order.status] || 0) + 1;
          return acc;
        }, {}) || {},
        recentOrderIds: data?.orders?.slice(0, 3).map((order: any) => order.id) || [],
        timestamp: new Date().toISOString()
      });
    },
    onError: (error: any) => {
      logger.error(' Recent orders failed to load', {
        message: error.message,
        status: error.response?.status,
        responseData: error.response?.data,
        url: error.config?.url
      });
    }
  } as any);

  const {
    data: securityEvents,
    isLoading: securityEventsLoading,
    error: securityEventsError
  } = useSecurityEvents({
    limit: 5,
    page: 1
  }, {
    refetchInterval: 20000, // Auto-refresh every 20 seconds for security monitoring
    refetchOnWindowFocus: true,
    onSuccess: (data: any) => {
      logger.info(' Security events loaded successfully', {
        eventsCount: data?.items?.length || 0,
        severityBreakdown: data?.items?.reduce((acc: any, event: any) => {
          acc[event.severity] = (acc[event.severity] || 0) + 1;
          return acc;
        }, {}) || {},
        recentEventTypes: data?.items?.slice(0, 3).map((event: any) => event.eventType) || [],
        criticalEvents: data?.items?.filter((event: any) => event.severity === 'Critical').length || 0,
        timestamp: new Date().toISOString()
      });
    },
    onError: (error: any) => {
      logger.error(' Security events failed to load', {
        message: error.message,
        status: error.response?.status,
        responseData: error.response?.data,
        url: error.config?.url
      });
    }
  } as any);

  // Top products (real data)
  const { data: productPerf, isLoading: productPerfLoading, error: productPerfError } = useAdminProductPerformance(30 as any, {} as any);
  const topProductsData = (productPerf || []).slice(0, 6).map((p: any, idx: number) => ({
    name: p.productName,
    value: p.revenue ?? 0,
    color: productColors[idx % productColors.length]
  }));

  return (
    <div className="space-y-8">
      {/* Welcome Section */}
      <div>
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold">Chào mừng trở lại, {user?.firstName}!</h1>
            <p className="text-muted-foreground mt-2">
              Đây là tổng quan về hoạt động của hệ thống ecommerce laptop.
            </p>
          </div>

          {/* Refresh Controls */}
          <div className="flex items-center space-x-2">
            {(kpisLoading || salesTrendLoading || recentOrdersLoading || securityEventsLoading) && (
              <div className="flex items-center text-sm text-muted-foreground">
                <Activity className="h-4 w-4 mr-1 animate-pulse" />
                đang cập nhật...
              </div>
            )}
            <button
              onClick={() => {
                refetchKpis();
                // Add other refetch calls when needed
              }}
              className="flex items-center px-3 py-2 text-sm bg-secondary hover:bg-secondary/80 rounded-lg transition-colors"
              disabled={kpisLoading}
            >
              <Activity className={`h-4 w-4 mr-1 ${kpisLoading ? 'animate-spin' : ''}`} />
              Làm mới
            </button>
          </div>
        </div>

        {kpisError && (
          <div className="mt-4 p-3 bg-red-50 dark:bg-red-900/20 rounded-lg border border-red-200 dark:border-red-800">
            <div className="flex items-center">
              <AlertCircle className="h-4 w-4 text-red-500 mr-2" />
              <p className="text-sm text-red-700 dark:text-red-300">
                Không thể tải dữ liệu dashboard. Vui lòng thử lại sau.
              </p>
            </div>
          </div>
        )}
      </div>

      {/* Metrics Cards */}
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        <MetricCard
          title="Doanh số tháng này"
          value={kpis?.todaySales?.value ? formatCompactCurrency(kpis.todaySales.value) : '0 USD'}
          change={kpis?.todaySales?.change}
          icon={DollarSign}
          loading={kpisLoading}
        />
        <MetricCard
          title="Đơn hàng mới (tháng)"
          value={kpis?.newOrders?.value ? formatNumber(kpis.newOrders.value) : 0}
          change={kpis?.newOrders?.change}
          icon={ShoppingCart}
          loading={kpisLoading}
        />
        <MetricCard
          title="Sản phẩm sắp hết"
          value={kpis?.lowStock?.value ? formatNumber(kpis.lowStock.value) : 0}
          description={kpis?.lowStock?.label}
          icon={Package}
          loading={kpisLoading}
        />
        <MetricCard
          title="Lượt truy cập"
          value={kpis?.visitors?.value ? formatNumber(kpis.visitors.value) : 0}
          change={kpis?.visitors?.change}
          icon={Activity}
          loading={kpisLoading}
        />
      </div>

      {/* Charts Section */}
      <div className="grid gap-6 md:grid-cols-2">
        {/* Sales Trend Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Xu hướng doanh số 30 ngày qua</CardTitle>
            <CardDescription>Doanh thu theo ngày (USD)</CardDescription>
          </CardHeader>
          <CardContent>
            {salesTrendLoading ? (
              <div className="h-[300px] flex items-center justify-center">
                <Skeleton className="h-full w-full" />
              </div>
            ) : salesTrendError ? (
              <div className="h-[300px] flex items-center justify-center">
                <div className="text-center">
                  <AlertCircle className="h-8 w-8 text-red-500 mx-auto mb-2" />
                  <p className="text-sm text-muted-foreground">
                    Không thể tải dữ liệu biểu đồ
                  </p>
                </div>
              </div>
            ) : (
              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={salesTrend || []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis
                    dataKey="date"
                    tickFormatter={(value) => {
                      const date = new Date(value);
                      return `${date.getDate()}/${date.getMonth() + 1}`;
                    }}
                  />
                  <YAxis tickFormatter={(value) => formatCompactCurrency(value)} />
                  <Tooltip
                    formatter={(value) => [formatCurrency(value as number), 'Doanh thu']}
                    labelFormatter={(label) => formatDate(label)}
                  />
                  <Bar dataKey="sales" fill="#3b82f6" />
                </BarChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>

        {/* Orders Trend Chart */}
        <Card>
          <CardHeader>
            <CardTitle>Đơn hàng theo ngày</CardTitle>
            <CardDescription>Số lượng đơn hàng trong 30 ngày qua</CardDescription>
          </CardHeader>
          <CardContent>
            {salesTrendLoading ? (
              <div className="h-[300px] flex items-center justify-center">
                <Skeleton className="h-full w-full" />
              </div>
            ) : salesTrendError ? (
              <div className="h-[300px] flex items-center justify-center">
                <div className="text-center">
                  <AlertCircle className="h-8 w-8 text-red-500 mx-auto mb-2" />
                  <p className="text-sm text-muted-foreground">
                    Không thể tải dữ liệu đơn hàng
                  </p>
                </div>
              </div>
            ) : (
              <ResponsiveContainer width="100%" height={300}>
                <LineChart data={salesTrend || []}>
                  <CartesianGrid strokeDasharray="3 3" />
                  <XAxis
                    dataKey="date"
                    tickFormatter={(value) => {
                      const date = new Date(value);
                      return `${date.getDate()}/${date.getMonth() + 1}`;
                    }}
                  />
                  <YAxis />
                  <Tooltip
                    formatter={(value) => [`${formatNumber(value as number)} đơn`, 'Đơn hàng']}
                    labelFormatter={(label) => formatDate(label)}
                  />
                  <Line type="monotone" dataKey="orders" stroke="#10b981" strokeWidth={2} />
                </LineChart>
              </ResponsiveContainer>
            )}
          </CardContent>
        </Card>
      </div>

      {/* Bottom Section */}
      <div className="grid gap-6 lg:grid-cols-3">
        {/* Quick Actions */}
        <div className="lg:col-span-2">
          <QuickActions />
        </div>

        {/* Recent Activity */}
        <div>
          <RecentActivity
            recentOrders={recentOrders}
            securityEvents={securityEvents}
            isLoadingOrders={recentOrdersLoading}
            isLoadingSecurity={securityEventsLoading}
          />
        </div>
      </div>

      {/* Top Products Chart */}
      <Card>
        <CardHeader>
          <CardTitle>Sản phẩm bán chạy</CardTitle>
          <CardDescription>Phân phối theo doanh thu (%) - 30 ngày</CardDescription>
        </CardHeader>
        <CardContent>
          {productPerfLoading ? (
            <div className="h-[300px] flex items-center justify-center">
              <Skeleton className="h-full w-full" />
            </div>
          ) : productPerfError ? (
            <div className="h-[300px] flex items-center justify-center">
              <div className="text-center">
                <AlertCircle className="h-8 w-8 text-red-500 mx-auto mb-2" />
                <p className="text-sm text-muted-foreground">Không thể tải dữ liệu sản phẩm bán chạy</p>
              </div>
            </div>
          ) : (
            <ResponsiveContainer width="100%" height={300}>
              <PieChart>
                <Pie
                  data={topProductsData}
                  cx="50%"
                  cy="50%"
                  labelLine={false}
                  label={({ name, percent }) => `${name} ${(percent * 100).toFixed(0)}%`}
                  outerRadius={80}
                  fill="#8884d8"
                  dataKey="value"
                >
                  {topProductsData.map((entry: { color: string }, index: number) => (
                    <Cell key={`cell-${index}`} fill={entry.color} />
                  ))}
                </Pie>
                <Tooltip />
              </PieChart>
            </ResponsiveContainer>
          )}
        </CardContent>
      </Card>

      {/* Export Tools */}
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center text-blue-600">
            <Download className="h-5 w-5 mr-2" />
            Công cụ xuất file
          </CardTitle>
          <CardDescription>
            Xuất dữ liệu ra các định dạng PDF, Excel và XML
          </CardDescription>
        </CardHeader>
        <CardContent>
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="space-y-2">
              <h4 className="font-medium text-sm">Báo cáo doanh thu</h4>
              <DateRangeExport
                onExport={exportRevenueReport}
                className="flex-col items-start space-y-2"
              />
            </div>
            <div className="space-y-2">
              <h4 className="font-medium text-sm">Báo cáo tồn kho</h4>
              <ExportButtons
                type="inventory"
                className="w-full"
              />
            </div>
            <div className="space-y-2">
              <h4 className="font-medium text-sm">Báo cáo bảo mật</h4>
              <DateRangeExport
                onExport={exportSecurityReport}
                className="flex-col items-start space-y-2"
              />
            </div>
            <div className="space-y-2">
              <h4 className="font-medium text-sm">Dữ liệu khác</h4>
              <div className="flex flex-wrap gap-2">
                <ExportButtons type="orders" className="text-xs" />
                <ExportButtons type="products" className="text-xs" />
                <ExportButtons type="security-events" className="text-xs" />
                <ExportButtons type="ip-block-rules" className="text-xs" />
                <ExportButtons type="rate-limit-rules" className="text-xs" />
              </div>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* System Alerts (Super Admin Only) */}
      <PermissionGuard permission={PERMISSIONS.SECURITY_MANAGE}>
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center text-orange-600">
              <AlertCircle className="h-5 w-5 mr-2" />
              Cảnh báo hệ thống
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="space-y-3">
              <div className="flex items-center justify-between p-3 bg-yellow-50 dark:bg-yellow-900/20 rounded-lg border border-yellow-200 dark:border-yellow-800">
                <div>
                  <p className="text-sm font-medium">Dung lượng đĩa cứng</p>
                  <p className="text-xs text-muted-foreground">Còn 15% dung lượng trống</p>
                </div>
                <Badge variant="outline" className="text-yellow-600">Cảnh báo</Badge>
              </div>
              <div className="flex items-center justify-between p-3 bg-green-50 dark:bg-green-900/20 rounded-lg border border-green-200 dark:border-green-800">
                <div>
                  <p className="text-sm font-medium">Backup tự động</p>
                  <p className="text-xs text-muted-foreground">Hoàn thành lúc 2:00 AM</p>
                </div>
                <Badge variant="outline" className="text-green-600">Hoàn thành</Badge>
              </div>
            </div>
          </CardContent>
        </Card>
      </PermissionGuard>
    </div>
  );
}