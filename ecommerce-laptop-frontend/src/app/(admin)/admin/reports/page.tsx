'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle
} from '@/components/ui/card';
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import {
  PermissionGuard,
  useAdminAuth
} from '@/contexts/AdminAuthContext';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { PERMISSIONS } from '@/lib/admin-api';
import { formatCurrencyPrice } from '@/lib/currency';
import { useQuery } from '@tanstack/react-query';
import { endOfMonth, format, startOfMonth, subDays } from 'date-fns';
import { vi } from 'date-fns/locale';
import {
  Activity,
  ArrowDownRight,
  ArrowUpRight,
  BarChart3,
  Calendar as CalendarIcon,
  DollarSign,
  Download,
  Filter,
  Minus,
  Package,
  PieChart as PieChartIcon,
  ShoppingCart,
  Users
} from 'lucide-react';
import { useState } from 'react';
import {
  Area,
  AreaChart,
  Bar,
  BarChart,
  CartesianGrid,
  Cell,
  Line,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis
} from 'recharts';

interface ReportData {
  salesOverview: {
    totalRevenue: number;
    totalOrders: number;
    totalCustomers: number;
    averageOrderValue: number;
    revenueChange: number;
    ordersChange: number;
    customersChange: number;
    aovChange: number;
  };
  salesTrend: Array<{
    date: string;
    revenue: number;
    orders: number;
    customers: number;
  }>;
  topProducts: Array<{
    id: number;
    name: string;
    revenue: number;
    quantity: number;
    orders: number;
  }>;
  orderStatus: Array<{
    status: string;
    count: number;
    percentage: number;
  }>;
  revenueByCategory: Array<{
    category: string;
    revenue: number;
    percentage: number;
  }>;
  customerSegments: Array<{
    segment: string;
    customers: number;
    revenue: number;
    percentage: number;
  }>;
}

// Mock API call - Replace with real API
const useReportsQuery = (dateRange: { from: Date; to: Date }) => {
  return useQuery({
    queryKey: ['admin', 'reports', dateRange],
    queryFn: async (): Promise<ReportData> => {
      await new Promise(resolve => setTimeout(resolve, 1500));

      // Mock data - replace with real API call
      return {
        salesOverview: {
          totalRevenue: 2750000000,
          totalOrders: 1234,
          totalCustomers: 856,
          averageOrderValue: 2230000,
          revenueChange: 12.5,
          ordersChange: -3.2,
          customersChange: 8.7,
          aovChange: 15.8
        },
        salesTrend: [
          { date: '2025-01-16', revenue: 85000000, orders: 42, customers: 35 },
          { date: '2025-01-17', revenue: 92000000, orders: 48, customers: 38 },
          { date: '2025-01-18', revenue: 78000000, orders: 35, customers: 29 },
          { date: '2025-01-19', revenue: 105000000, orders: 52, customers: 44 },
          { date: '2025-01-20', revenue: 118000000, orders: 61, customers: 51 },
          { date: '2025-01-21', revenue: 134000000, orders: 68, customers: 57 },
          { date: '2025-01-22', revenue: 142000000, orders: 73, customers: 62 }
        ],
        topProducts: [
          { id: 1, name: 'MacBook Pro M3 16"', revenue: 580000000, quantity: 23, orders: 20 },
          { id: 2, name: 'Dell XPS 13 Plus', revenue: 420000000, quantity: 35, orders: 32 },
          { id: 3, name: 'ThinkPad X1 Carbon', revenue: 380000000, quantity: 28, orders: 25 },
          { id: 4, name: 'MacBook Air M3', revenue: 340000000, quantity: 31, orders: 28 },
          { id: 5, name: 'ASUS ROG Strix', revenue: 280000000, quantity: 18, orders: 16 }
        ],
        orderStatus: [
          { status: 'delivered', count: 520, percentage: 42.1 },
          { status: 'processing', count: 285, percentage: 23.1 },
          { status: 'shipped', count: 198, percentage: 16.0 },
          { status: 'confirmed', count: 142, percentage: 11.5 },
          { status: 'pending', count: 61, percentage: 4.9 },
          { status: 'cancelled', count: 28, percentage: 2.3 }
        ],
        revenueByCategory: [
          { category: 'Laptop Gaming', revenue: 920000000, percentage: 33.5 },
          { category: 'Laptop Văn phòng', revenue: 780000000, percentage: 28.4 },
          { category: 'MacBook', revenue: 650000000, percentage: 23.6 },
          { category: 'Phụ kiện', revenue: 280000000, percentage: 10.2 },
          { category: 'Linh kiện', revenue: 120000000, percentage: 4.4 }
        ],
        customerSegments: [
          { segment: 'VIP', customers: 45, revenue: 890000000, percentage: 32.4 },
          { segment: 'Thường xuyên', customers: 178, revenue: 1250000000, percentage: 45.5 },
          { segment: 'Mi', customers: 633, revenue: 610000000, percentage: 22.2 }
        ]
      };
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
  });
};

// Vietnamese formatting utilities - moved inside component

const formatNumber = (num: number) => {
  return new Intl.NumberFormat('vi-VN').format(num);
};

const formatDate = (dateString: string) => {
  return format(new Date(dateString), 'dd/MM', { locale: vi });
};

const formatPercent = (value: number) => {
  return `${value > 0 ? '+' : ''}${value.toFixed(1)}%`;
};

const getChangeIcon = (change: number) => {
  if (change > 0) return <ArrowUpRight className="h-4 w-4 text-green-600" />;
  if (change < 0) return <ArrowDownRight className="h-4 w-4 text-red-600" />;
  return <Minus className="h-4 w-4 text-gray-500" />;
};

const getChangeColor = (change: number) => {
  if (change > 0) return 'text-green-600';
  if (change < 0) return 'text-red-600';
  return 'text-gray-500';
};

const COLORS = ['#3b82f6', '#ef4444', '#10b981', '#f59e0b', '#8b5cf6', '#06b6d4'];

const getStatusLabel = (status: string) => {
  const statusLabels = {
    delivered: 'Đã giao',
    processing: 'Đang xử lý',
    shipped: 'Đang giao',
    confirmed: 'Đã xác nhận',
    pending: 'Chờ xác nhận',
    cancelled: 'Đã hủy'
  };
  return statusLabels[status as keyof typeof statusLabels] || status;
};

export default function ReportsPage() {
  const { selectedCurrency } = useCurrencyContext();
  const { user } = useAdminAuth();

  const [dateRange, setDateRange] = useState({
    from: startOfMonth(new Date()),
    to: endOfMonth(new Date())
  });

  // Helper functions
  const formatCurrency = (amount: number) => {
    return formatCurrencyPrice(amount, selectedCurrency);
  };
  const [reportType, setReportType] = useState('overview');

  const {
    data: reportData,
    isLoading,
    error,
    refetch
  } = useReportsQuery(dateRange);

  const handleDateRangeChange = (range: { from: Date; to: Date }) => {
    setDateRange(range);
  };

  const handleExport = (format: 'csv' | 'pdf') => {
    // Mock export functionality
    console.log(`Exporting report in ${format} format`);
  };

  if (error) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <Activity className="h-12 w-12 text-red-500 mx-auto mb-4" />
          <h3 className="text-lg font-semibold">Có lỗi xảy ra</h3>
          <p className="text-muted-foreground mb-4">
            Không thể tải dữ liệu báo cáo. Vui lòng thử lại.
          </p>
          <Button onClick={() => refetch()}>Thử lại</Button>
        </div>
      </div>
    );
  }

  return (
    <PermissionGuard permission={PERMISSIONS.ORDERS_READ}>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-3xl font-bold">Báo cáo & Thống kê</h1>
            <p className="text-muted-foreground">
              Phân tích doanh số và hiệu suất kinh doanh
            </p>
          </div>

          <div className="flex items-center gap-2">
            <Button variant="outline" onClick={() => handleExport('csv')}>
              <Download className="h-4 w-4 mr-2" />
              Xut CSV
            </Button>
            <Button variant="outline" onClick={() => handleExport('pdf')}>
              <Download className="h-4 w-4 mr-2" />
              Xut PDF
            </Button>
          </div>
        </div>

        {/* Filters */}
        <Card>
          <CardHeader>
            <CardTitle className="flex items-center gap-2">
              <Filter className="h-5 w-5" />
              Bộ lọc báo cáo
            </CardTitle>
          </CardHeader>
          <CardContent>
            <div className="flex flex-col sm:flex-row gap-4">
              <div className="flex-1">
                <label className="text-sm font-medium mb-2 block">Loại báo cáo</label>
                <Select value={reportType} onValueChange={setReportType}>
                  <SelectTrigger>
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="overview">Tổng quan</SelectItem>
                    <SelectItem value="sales">Doanh số</SelectItem>
                    <SelectItem value="products">Sản phẩm</SelectItem>
                    <SelectItem value="customers">Khách hàng</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <div>
                <label className="text-sm font-medium mb-2 block">Khong thi gian</label>
                <Popover>
                  <PopoverTrigger asChild>
                    <Button variant="outline" className="w-64">
                      <CalendarIcon className="h-4 w-4 mr-2" />
                      {format(dateRange.from, 'dd/MM/yyyy', { locale: vi })} - {format(dateRange.to, 'dd/MM/yyyy', { locale: vi })}
                    </Button>
                  </PopoverTrigger>
                  <PopoverContent className="w-auto p-0" align="end">
                    <div className="p-4 space-y-4">
                      <div className="grid grid-cols-2 gap-2">
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleDateRangeChange({
                            from: subDays(new Date(), 7),
                            to: new Date()
                          })}
                        >
                          7 ngy qua
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleDateRangeChange({
                            from: subDays(new Date(), 30),
                            to: new Date()
                          })}
                        >
                          30 ngy qua
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleDateRangeChange({
                            from: startOfMonth(new Date()),
                            to: endOfMonth(new Date())
                          })}
                        >
                          Thng ny
                        </Button>
                        <Button
                          variant="outline"
                          size="sm"
                          onClick={() => handleDateRangeChange({
                            from: startOfMonth(subDays(new Date(), 30)),
                            to: endOfMonth(subDays(new Date(), 30))
                          })}
                        >
                          Thng trc
                        </Button>
                      </div>
                    </div>
                  </PopoverContent>
                </Popover>
              </div>
            </div>
          </CardContent>
        </Card>

        {isLoading ? (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            {[...Array(8)].map((_, i) => (
              <div key={i} className="h-32 bg-muted animate-pulse rounded" />
            ))}
          </div>
        ) : reportData ? (
          <div className="space-y-6">
            {/* KPI Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Tổng doanh thu</CardTitle>
                  <DollarSign className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">
                    {formatCurrency(reportData.salesOverview.totalRevenue)}
                  </div>
                  <div className={`text-xs flex items-center gap-1 ${getChangeColor(reportData.salesOverview.revenueChange)}`}>
                    {getChangeIcon(reportData.salesOverview.revenueChange)}
                    {formatPercent(reportData.salesOverview.revenueChange)} so với tháng trước
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Tổng đơn hàng</CardTitle>
                  <ShoppingCart className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">
                    {formatNumber(reportData.salesOverview.totalOrders)}
                  </div>
                  <div className={`text-xs flex items-center gap-1 ${getChangeColor(reportData.salesOverview.ordersChange)}`}>
                    {getChangeIcon(reportData.salesOverview.ordersChange)}
                    {formatPercent(reportData.salesOverview.ordersChange)} so vi thng trc
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Khách hàng mới</CardTitle>
                  <Users className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">
                    {formatNumber(reportData.salesOverview.totalCustomers)}
                  </div>
                  <div className={`text-xs flex items-center gap-1 ${getChangeColor(reportData.salesOverview.customersChange)}`}>
                    {getChangeIcon(reportData.salesOverview.customersChange)}
                    {formatPercent(reportData.salesOverview.customersChange)} so vi thng trc
                  </div>
                </CardContent>
              </Card>

              <Card>
                <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                  <CardTitle className="text-sm font-medium">Giá trị đơn TB</CardTitle>
                  <Package className="h-4 w-4 text-muted-foreground" />
                </CardHeader>
                <CardContent>
                  <div className="text-2xl font-bold">
                    {formatCurrency(reportData.salesOverview.averageOrderValue)}
                  </div>
                  <div className={`text-xs flex items-center gap-1 ${getChangeColor(reportData.salesOverview.aovChange)}`}>
                    {getChangeIcon(reportData.salesOverview.aovChange)}
                    {formatPercent(reportData.salesOverview.aovChange)} so vi thng trc
                  </div>
                </CardContent>
              </Card>
            </div>

            {/* Charts Row 1 */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Sales Trend */}
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <BarChart3 className="h-5 w-5" />
                    Xu hướng doanh số
                  </CardTitle>
                  <CardDescription>
                    Doanh thu và đơn hàng theo ngày
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <ResponsiveContainer width="100%" height={300}>
                    <AreaChart data={reportData.salesTrend}>
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis
                        dataKey="date"
                        tickFormatter={formatDate}
                      />
                      <YAxis
                        yAxisId="revenue"
                        orientation="left"
                        tickFormatter={(value) => formatCurrency(value).replace('', '')}
                      />
                      <YAxis
                        yAxisId="orders"
                        orientation="right"
                      />
                      <Tooltip
                        formatter={(value, name) => [
                          name === 'revenue' ? formatCurrency(value as number) : value,
                          name === 'revenue' ? 'Doanh thu' : 'Đơn hàng'
                        ]}
                        labelFormatter={(value) => `Ngày ${formatDate(value)}`}
                      />
                      <Area
                        yAxisId="revenue"
                        type="monotone"
                        dataKey="revenue"
                        stackId="1"
                        stroke="#3b82f6"
                        fill="#3b82f6"
                        fillOpacity={0.6}
                      />
                      <Line
                        yAxisId="orders"
                        type="monotone"
                        dataKey="orders"
                        stroke="#ef4444"
                        strokeWidth={2}
                        dot={{ r: 4 }}
                      />
                    </AreaChart>
                  </ResponsiveContainer>
                </CardContent>
              </Card>

              {/* Order Status */}
              <Card>
                <CardHeader>
                  <CardTitle className="flex items-center gap-2">
                    <PieChartIcon className="h-5 w-5" />
                    Trạng thái đơn hàng
                  </CardTitle>
                  <CardDescription>
                    Phân bổ đơn hàng theo trạng thái
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <ResponsiveContainer width="100%" height={300}>
                    <PieChart>
                      <Pie
                        data={reportData.orderStatus}
                        cx="50%"
                        cy="50%"
                        labelLine={false}
                        label={({ status, percentage }) => `${getStatusLabel(status)} (${percentage}%)`}
                        outerRadius={80}
                        fill="#8884d8"
                        dataKey="count"
                      >
                        {reportData.orderStatus.map((entry, index) => (
                          <Cell key={`cell-${index}`} fill={COLORS[index % COLORS.length]} />
                        ))}
                      </Pie>
                      <Tooltip
                        formatter={(value, name) => [formatNumber(value as number), 'S n']}
                        labelFormatter={(label) => getStatusLabel(label)}
                      />
                    </PieChart>
                  </ResponsiveContainer>
                </CardContent>
              </Card>
            </div>

            {/* Charts Row 2 */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Top Products */}
              <Card>
                <CardHeader>
                    <CardTitle>Sản phẩm bán chạy</CardTitle>
                    <CardDescription>
                      Top 5 sản phẩm có doanh thu cao nhất
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <div className="space-y-4">
                    {reportData.topProducts.map((product, index) => (
                      <div key={product.id} className="flex items-center justify-between">
                        <div className="flex items-center gap-3">
                          <Badge variant="outline" className="w-8 h-8 flex items-center justify-center">
                            {index + 1}
                          </Badge>
                          <div>
                            <div className="font-medium">{product.name}</div>
                            <div className="text-sm text-muted-foreground">
                              {formatNumber(product.quantity)} sản phẩm • {formatNumber(product.orders)} đơn hàng
                            </div>
                          </div>
                        </div>
                        <div className="text-right">
                          <div className="font-semibold">{formatCurrency(product.revenue)}</div>
                        </div>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>

              {/* Revenue by Category */}
              <Card>
                <CardHeader>
                  <CardTitle>Doanh thu theo danh mục</CardTitle>
                  <CardDescription>
                    Phân bổ doanh thu theo danh mục sản phẩm
                  </CardDescription>
                </CardHeader>
                <CardContent>
                  <ResponsiveContainer width="100%" height={300}>
                    <BarChart data={reportData.revenueByCategory} layout="horizontal">
                      <CartesianGrid strokeDasharray="3 3" />
                      <XAxis
                        type="number"
                        tickFormatter={(value) => formatCurrency(value).replace('', '')}
                      />
                      <YAxis
                        type="category"
                        dataKey="category"
                        width={100}
                      />
                      <Tooltip
                        formatter={(value) => [formatCurrency(value as number), 'Doanh thu']}
                      />
                      <Bar dataKey="revenue" fill="#3b82f6" />
                    </BarChart>
                  </ResponsiveContainer>
                </CardContent>
              </Card>
            </div>

            {/* Customer Segments */}
            <Card>
              <CardHeader>
                <CardTitle>Phân khúc khách hàng</CardTitle>
                <CardDescription>
                  Phân tích khách hàng theo mức độ mua sắm
                </CardDescription>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                  {reportData.customerSegments.map((segment) => (
                    <div key={segment.segment} className="text-center p-6 border rounded-lg">
                      <div className="text-2xl font-bold mb-2">
                        {formatNumber(segment.customers)}
                      </div>
                      <div className="text-sm text-muted-foreground mb-2">
                        Khách hàng {segment.segment}
                      </div>
                      <div className="text-lg font-semibold text-blue-600">
                        {formatCurrency(segment.revenue)}
                      </div>
                      <div className="text-xs text-muted-foreground">
                        {segment.percentage}% tổng doanh thu
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          </div>
        ) : null}
      </div>
    </PermissionGuard>
  );
}