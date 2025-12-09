'use client';

import { ExportButtons, InvoiceExportButtons } from '@/components/admin/ExportButtons';
import { SortableTableHeader, SortConfig } from '@/components/admin/SortableTableHeader';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle
} from '@/components/ui/card';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Input } from '@/components/ui/input';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from '@/components/ui/table';
import {
  PermissionGuard,
  useAdminAuth
} from '@/contexts/AdminAuthContext';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { getAdminOrders, PERMISSIONS, updateOrderStatus } from '@/lib/admin-api';
import { formatCurrencyPrice } from '@/lib/currency';
import { useQuery } from '@tanstack/react-query';
import {
  AlertCircle,
  CheckCircle,
  Clock,
  Eye,
  Loader2,
  MoreHorizontal,
  Package,
  Search,
  Truck,
  XCircle
} from 'lucide-react';
import Link from 'next/link';
import { useState } from 'react';

// Types for orders
interface Order {
  id: number;
  orderCode: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  total: number;
  status: 'pending' | 'confirmed' | 'processing' | 'shipped' | 'delivered' | 'cancelled';
  paymentStatus: 'pending' | 'paid' | 'failed' | 'refunded';
  paymentMethod: string;
  shippingAddress: string;
  itemCount: number;
  createdAt: string;
  updatedAt: string;
}

interface OrdersResponse {
  orders: Order[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
  pageSize: number;
}

// Helper function to map API AdminOrderDto to local Order interface  
const mapApiOrderToOrder = (apiOrder: any): Order => {
  // The backend returns OrderStatus enum values, normalize to lowercase
  const normalizeStatus = (status: string): Order['status'] => {
    const lowerStatus = status.toLowerCase();
    // Validate against allowed values
    const validStatuses: Order['status'][] = ['pending', 'confirmed', 'processing', 'shipped', 'delivered', 'cancelled'];
    return validStatuses.includes(lowerStatus as Order['status']) ? lowerStatus as Order['status'] : 'pending';
  };

  // Normalize payment status to match expected values
  const normalizePaymentStatus = (status: string): Order['paymentStatus'] => {
    const lowerStatus = status.toLowerCase();
    const validStatuses: Order['paymentStatus'][] = ['pending', 'paid', 'failed', 'refunded'];
    return validStatuses.includes(lowerStatus as Order['paymentStatus']) ? lowerStatus as Order['paymentStatus'] : 'pending';
  };

  return {
    id: apiOrder.id,
    orderCode: apiOrder.orderNumber,
    customerName: apiOrder.customerName || 'N/A',
    customerEmail: apiOrder.customerEmail || 'N/A',
    customerPhone: apiOrder.customerPhone || 'N/A',
    total: apiOrder.totalAmount,
    status: normalizeStatus(apiOrder.status),
    paymentStatus: normalizePaymentStatus(apiOrder.paymentStatus || 'pending'),
    paymentMethod: apiOrder.paymentMethod || 'Card',
    shippingAddress: apiOrder.shippingAddress || 'N/A',
    itemCount: apiOrder.items?.length || 0,
    createdAt: apiOrder.createdAt,
    updatedAt: apiOrder.createdAt // Use createdAt for updatedAt since AdminOrderDto doesn't have updatedAt
  };
};

// Real API call using admin-api
const useOrdersQuery = (params: {
  page?: number;
  limit?: number;
  search?: string;
  status?: string;
  paymentStatus?: string;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}) => {
  return useQuery({
    queryKey: ['admin', 'orders', params],
    queryFn: async (): Promise<OrdersResponse> => {
      const apiParams = {
        page: params.page || 1,
        limit: params.limit || 20,
        status: params.status,
        sortBy: params.sortBy,
        sortOrder: params.sortOrder,
        // Map search to customerId if it's a number, otherwise it might be used differently
        ...(params.search && /^\d+$/.test(params.search) && { customerId: parseInt(params.search) })
      };

      const response = await getAdminOrders(apiParams);
      return {
        orders: response.orders.map(mapApiOrderToOrder),
        totalCount: response.totalCount,
        currentPage: response.currentPage,
        totalPages: response.totalPages,
        pageSize: response.pageSize
      };
    },
    staleTime: 30 * 1000, // 30 seconds (orders change frequently)
    refetchInterval: 60 * 1000, // 1 minute
  });
};

// Vietnamese formatting utilities - moved inside component

const formatDate = (dateString: string) => {
  return new Intl.DateTimeFormat('vi-VN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(dateString));
};

const getStatusBadge = (status: string) => {
  const statusConfig = {
    pending: { label: 'Ch xc nhn', variant: 'outline' as const, icon: Clock },
    confirmed: { label: ' xc nhn', variant: 'secondary' as const, icon: CheckCircle },
    processing: { label: 'ang x l', variant: 'default' as const, icon: Package },
    shipped: { label: 'ang giao', variant: 'default' as const, icon: Truck },
    delivered: { label: ' giao', variant: 'default' as const, icon: CheckCircle },
    cancelled: { label: ' hy', variant: 'destructive' as const, icon: XCircle }
  };

  const config = statusConfig[status as keyof typeof statusConfig];
  const Icon = config?.icon || Clock;

  return (
    <Badge variant={config?.variant || 'outline'} className="flex items-center gap-1">
      <Icon className="h-3 w-3" />
      {config?.label || status}
    </Badge>
  );
};

const getPaymentStatusBadge = (status: string) => {
  const statusConfig = {
    pending: { label: 'Ch thanh ton', variant: 'outline' as const },
    paid: { label: ' thanh ton', variant: 'default' as const },
    failed: { label: 'Tht bi', variant: 'destructive' as const },
    refunded: { label: ' hon tin', variant: 'secondary' as const }
  };

  const config = statusConfig[status as keyof typeof statusConfig];

  return (
    <Badge variant={config?.variant || 'outline'}>
      {config?.label || status}
    </Badge>
  );
};

export default function OrdersPage() {
  const { selectedCurrency } = useCurrencyContext();
  const { user } = useAdminAuth();
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedStatus, setSelectedStatus] = useState('all');
  const [selectedPaymentStatus, setSelectedPaymentStatus] = useState('all');
  const [updatingOrderId, setUpdatingOrderId] = useState<number | null>(null);
  const [sortConfig, setSortConfig] = useState<SortConfig | null>(null);

  const {
    data: ordersData,
    isLoading,
    error,
    refetch
  } = useOrdersQuery({
    search: searchTerm,
    status: selectedStatus === 'all' ? undefined : selectedStatus,
    paymentStatus: selectedPaymentStatus === 'all' ? undefined : selectedPaymentStatus,
    sortBy: sortConfig?.field,
    sortOrder: sortConfig?.direction || undefined
  });

  // Helper functions
  const formatCurrency = (amount: number) => {
    return formatCurrencyPrice(amount, selectedCurrency);
  };

  const formatDate = (dateString: string) => {
    return new Intl.DateTimeFormat('vi-VN', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit'
    }).format(new Date(dateString));
  };

  const handleUpdateOrderStatus = async (orderId: number, newStatus: string) => {
    try {
      setUpdatingOrderId(orderId);

      await updateOrderStatus(orderId, newStatus);

      // Refresh the orders list after successful update
      await refetch();

      console.log('Order status updated successfully:', orderId, newStatus);
    } catch (error) {
      console.error('Error updating order status:', error);
      // Show error message - for now just console log
      alert('C li xy ra khi cp nht trng thi n hng. Vui lng th li.');
    } finally {
      setUpdatingOrderId(null);
    }
  };

  const handleSort = (field: string) => {
    setSortConfig(prevConfig => {
      if (prevConfig?.field === field) {
        // Toggle direction if same field
        const newDirection = prevConfig.direction === 'asc' ? 'desc' : prevConfig.direction === 'desc' ? null : 'asc';
        return newDirection ? { field, direction: newDirection } : null;
      }
      // New field, start with ascending
      return { field, direction: 'asc' };
    });
  };

  const handleExportOrders = () => {
    if (!ordersData?.orders) {
      alert('Khng c d liu  xut');
      return;
    }

    // Create CSV content
    const headers = ['M n hng', 'Khch hng', 'Email', 'in thoi', 'S sn phm', 'Tng tin', 'Trng thi', 'Thanh ton', 'Phng thc', 'Ngy to'];
    const csvContent = [
      headers.join(','),
      ...ordersData.orders.map(order => [
        order.orderCode,
        `"${order.customerName}"`,
        order.customerEmail,
        order.customerPhone,
        order.itemCount,
        formatCurrency(order.total),
        order.status,
        order.paymentStatus,
        order.paymentMethod,
        new Date(order.createdAt).toLocaleDateString('vi-VN')
      ].join(','))
    ].join('\n');

    // Download CSV file
    const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.setAttribute('href', url);
    link.setAttribute('download', `orders-${new Date().toISOString().split('T')[0]}.csv`);
    link.style.visibility = 'hidden';
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  if (error) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <AlertCircle className="h-12 w-12 text-red-500 mx-auto mb-4" />
          <h3 className="text-lg font-semibold">Li ti d liu</h3>
          <p className="text-muted-foreground mb-4">
            Khng th ti danh sch n hng. Vui lng th li.
          </p>
          <Button onClick={() => refetch()}>Th li</Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Qun l n hng</h1>
          <p className="text-muted-foreground">
            Theo di v x l n hng ca khch hng
          </p>
        </div>

        <div className="flex gap-2">
          <PermissionGuard permission={PERMISSIONS.ORDERS_READ}>
            <ExportButtons
              type="orders"
              options={{ search: searchTerm, status: selectedStatus === 'all' ? undefined : selectedStatus }}
              className="flex items-center"
            />
          </PermissionGuard>
        </div>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Tm kim v lc n hng</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex gap-4 flex-wrap">
            <div className="flex-1 min-w-[200px]">
              <div className="relative">
                <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Tm theo m n hng, tn khch hng, email..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-9"
                />
              </div>
            </div>

            <Select value={selectedStatus} onValueChange={setSelectedStatus}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Trng thi n hng" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tt c trng thi</SelectItem>
                <SelectItem value="pending">Ch xc nhn</SelectItem>
                <SelectItem value="confirmed"> xc nhn</SelectItem>
                <SelectItem value="processing">ang x l</SelectItem>
                <SelectItem value="shipped">ang giao</SelectItem>
                <SelectItem value="delivered"> giao</SelectItem>
                <SelectItem value="cancelled"> hy</SelectItem>
              </SelectContent>
            </Select>

            <Select value={selectedPaymentStatus} onValueChange={setSelectedPaymentStatus}>
              <SelectTrigger className="w-[180px]">
                <SelectValue placeholder="Trng thi thanh ton" />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value="all">Tt c thanh ton</SelectItem>
                <SelectItem value="pending">Ch thanh ton</SelectItem>
                <SelectItem value="paid"> thanh ton</SelectItem>
                <SelectItem value="failed">Tht bi</SelectItem>
                <SelectItem value="refunded"> hon tin</SelectItem>
              </SelectContent>
            </Select>
          </div>
        </CardContent>
      </Card>

      {/* Orders Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>
              Danh sch n hng ({ordersData?.totalCount || 0})
            </CardTitle>
            <Button variant="outline" onClick={() => refetch()}>
              Lm mi
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3">
              {[...Array(5)].map((_, i) => (
                <div key={i} className="flex items-center space-x-4">
                  <Skeleton className="h-12 w-12" />
                  <div className="space-y-2 flex-1">
                    <Skeleton className="h-4 w-[200px]" />
                    <Skeleton className="h-4 w-[150px]" />
                  </div>
                  <Skeleton className="h-8 w-[100px]" />
                  <Skeleton className="h-8 w-[120px]" />
                </div>
              ))}
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>
                    <SortableTableHeader
                      field="orderCode"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      M n hng
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="customerName"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Khch hng
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="itemCount"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Sn phm
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="total"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Tng tin
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="status"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Trng thi
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="paymentStatus"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Thanh ton
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="paymentMethod"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Phng thc
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="createdAt"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Ngy to
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead className="text-right">Thao tc</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {ordersData?.orders?.map((order) => (
                  <TableRow key={order.id}>
                    <TableCell>
                      <div className="font-medium">{order.orderCode}</div>
                    </TableCell>
                    <TableCell>
                      <div>
                        <div className="font-medium">{order.customerName}</div>
                        <div className="text-sm text-muted-foreground">
                          {order.customerEmail}
                        </div>
                        <div className="text-sm text-muted-foreground">
                          {order.customerPhone}
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <Badge variant="outline">{order.itemCount} sn phm</Badge>
                    </TableCell>
                    <TableCell className="font-medium">
                      {formatCurrency(order.total)}
                    </TableCell>
                    <TableCell>{getStatusBadge(order.status)}</TableCell>
                    <TableCell>{getPaymentStatusBadge(order.paymentStatus)}</TableCell>
                    <TableCell>
                      <Badge variant="outline">{order.paymentMethod}</Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {formatDate(order.createdAt)}
                    </TableCell>
                    <TableCell className="text-right">
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" className="h-8 w-8 p-0">
                            <MoreHorizontal className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuItem asChild>
                            <Link href={`/admin/orders/${order.id}`}>
                              <Eye className="h-4 w-4 mr-2" />
                              Xem chi tit
                            </Link>
                          </DropdownMenuItem>
                          <DropdownMenuItem asChild>
                            <div className="w-full">
                              <InvoiceExportButtons
                                orderId={order.id}
                                orderNumber={order.orderCode}
                                className="w-full justify-start h-8 px-2"
                              />
                            </div>
                          </DropdownMenuItem>
                          <PermissionGuard permission={PERMISSIONS.ORDERS_WRITE}>
                            {order.status === 'pending' && (
                              <DropdownMenuItem
                                onClick={() => handleUpdateOrderStatus(order.id, 'confirmed')}
                                disabled={updatingOrderId === order.id}
                              >
                                {updatingOrderId === order.id ? (
                                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                                ) : (
                                  <CheckCircle className="h-4 w-4 mr-2" />
                                )}
                                Xc nhn n hng
                              </DropdownMenuItem>
                            )}
                            {order.status === 'confirmed' && (
                              <DropdownMenuItem
                                onClick={() => handleUpdateOrderStatus(order.id, 'processing')}
                                disabled={updatingOrderId === order.id}
                              >
                                {updatingOrderId === order.id ? (
                                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                                ) : (
                                  <Package className="h-4 w-4 mr-2" />
                                )}
                                Bt u x l
                              </DropdownMenuItem>
                            )}
                            {order.status === 'processing' && (
                              <DropdownMenuItem
                                onClick={() => handleUpdateOrderStatus(order.id, 'shipped')}
                                disabled={updatingOrderId === order.id}
                              >
                                {updatingOrderId === order.id ? (
                                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                                ) : (
                                  <Truck className="h-4 w-4 mr-2" />
                                )}
                                Giao hng
                              </DropdownMenuItem>
                            )}
                            {(order.status === 'pending' || order.status === 'confirmed') && (
                              <DropdownMenuItem
                                onClick={() => handleUpdateOrderStatus(order.id, 'cancelled')}
                                disabled={updatingOrderId === order.id}
                                className="text-red-600"
                              >
                                {updatingOrderId === order.id ? (
                                  <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                                ) : (
                                  <XCircle className="h-4 w-4 mr-2" />
                                )}
                                Hy n hng
                              </DropdownMenuItem>
                            )}
                          </PermissionGuard>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}