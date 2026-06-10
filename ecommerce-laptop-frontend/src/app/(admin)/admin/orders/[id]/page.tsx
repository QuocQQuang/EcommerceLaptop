'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle
} from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Textarea } from '@/components/ui/textarea';
import {
  PermissionGuard,
  useAdminAuth
} from '@/contexts/AdminAuthContext';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { OrderStatusUpdateError, PERMISSIONS, getAdminOrderDetails, updateOrderStatus } from '@/lib/admin-api';
import { formatCurrencyPrice } from '@/lib/currency';
import { formatAddressForDisplay } from '@/utils/addressUtils';
import {
  ORDER_WORKFLOW,
  OrderStatus,
  getTransitionErrorMessage,
  getValidNextStatuses,
  isFinalStatus,
  isValidTransition
} from '@/utils/orderWorkflow';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  AlertCircle,
  ArrowLeft,
  CheckCircle,
  Clock,
  CreditCard,
  Edit,
  Info,
  Mail,
  MapPin,
  MessageSquare,
  Package,
  Phone,
  Truck,
  User,
  XCircle
} from 'lucide-react';
import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { useState } from 'react';
import { toast } from 'sonner';

interface OrderItem {
  id: number;
  productId: number;
  productName: string;
  productSku: string;
  productImage?: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
}

interface OrderDetail {
  id: number;
  orderCode: string;
  customerName: string;
  customerEmail: string;
  customerPhone: string;
  total: number;
  subtotal: number;
  shippingFee: number;
  discount: number;
  tax: number;
  status: OrderStatus;
  paymentStatus: 'pending' | 'paid' | 'failed' | 'refunded';
  paymentMethod: string;
  shippingAddress: string;
  shippingMethod: string;
  trackingNumber?: string;
  notes?: string;
  items: OrderItem[];
  statusHistory: Array<{
    status: string;
    timestamp: string;
    note?: string;
    updatedBy: string;
  }>;
  createdAt: string;
  updatedAt: string;
}

// Real API calls using admin-api
const useOrderDetailQuery = (orderId: string) => {
  return useQuery({
    queryKey: ['admin', 'orders', orderId],
    queryFn: async (): Promise<OrderDetail> => {
      const apiOrder = await getAdminOrderDetails(parseInt(orderId));

      // Debug logging to see actual backend response
      console.log('Admin Order API Response:', apiOrder);
      console.log('Shipping Address:', apiOrder.shippingAddress);
      console.log('Available fields:', Object.keys(apiOrder));

      // Map API response to OrderDetail interface
      // Note: Admin API returns AdminOrderDto with extended fields
      const apiOrderAny = apiOrder as any;

      // Helper function to normalize status
      const normalizeStatus = (status: string): OrderStatus => {
        const lowerStatus = status.toLowerCase();
        const validStatuses: OrderStatus[] = ['pending', 'confirmed', 'processing', 'shipped', 'delivered', 'cancelled'];
        return validStatuses.includes(lowerStatus as OrderStatus) ? lowerStatus as OrderStatus : 'pending';
      };

      // Helper function to normalize payment status
      const normalizePaymentStatus = (status: string): OrderDetail['paymentStatus'] => {
        const lowerStatus = status.toLowerCase();
        const validStatuses: OrderDetail['paymentStatus'][] = ['pending', 'paid', 'failed', 'refunded'];
        return validStatuses.includes(lowerStatus as OrderDetail['paymentStatus']) ? lowerStatus as OrderDetail['paymentStatus'] : 'pending';
      };

      // Format shipping address from backend AddressDto to string
      const formatAddress = (address: any): string => {
        if (typeof address === 'string') {
          return formatAddressForDisplay(address);
        }

        if (!address) return 'Chưa có địa chỉ';

        // Backend returns AddressDto with: Street, City, Province, PostalCode, Country
        const addressParts = [
          address.street,
          address.city,
          address.province,
          address.postalCode,
          address.country
        ].filter(part => part && part.trim() !== '');

        return addressParts.length > 0 ? addressParts.join(', ') : 'Chưa có địa chỉ';
      };

      return {
        id: apiOrderAny.id,
        orderCode: apiOrderAny.orderNumber || `ORD-${apiOrderAny.id}`,
        customerName: apiOrderAny.customerName || `Customer #${apiOrderAny.customerId}`,
        customerEmail: apiOrderAny.customerEmail || 'N/A',
        customerPhone: 'N/A', // Not available in OrderDto; requires separate user fetch or backend update
        total: apiOrderAny.totalAmount,
        subtotal: apiOrderAny.totalAmount, // Backend might not have separate subtotal
        shippingFee: 0, // Default to 0 if not in API
        discount: 0, // Default to 0 if not in API
        tax: 0, // Default to 0 if not in API
        status: normalizeStatus(apiOrderAny.status),
        paymentStatus: normalizePaymentStatus(apiOrderAny.paymentStatus || 'pending'),
        paymentMethod: apiOrderAny.paymentMethod || 'Card',
        shippingAddress: formatAddress(apiOrderAny.shippingAddress),
        shippingMethod: 'Giao hàng tiêu chuẩn', // Default value
        trackingNumber: apiOrderAny.trackingNumber,
        notes: apiOrderAny.notes,
        items: apiOrderAny.items?.map((item: any) => ({
          id: item.id,
          productId: item.productId,
          productName: item.product?.name || item.productName || 'Unknown Product',
          productSku: item.product?.sku || item.productSku || `SKU-${item.productId}`,
          productImage: item.product?.images?.[0]?.imageUrl || item.product?.imageUrl || item.productImage,
          quantity: item.quantity,
          unitPrice: item.unitPrice,
          totalPrice: item.totalPrice
        })) || [],
        statusHistory: [], // Backend might not have status history yet
        createdAt: apiOrderAny.createdAt,
        updatedAt: apiOrderAny.updatedAt || apiOrderAny.createdAt
      };
    },
    staleTime: 30 * 1000,
  });
};

const useUpdateOrderStatusMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({
      orderId,
      status,
      note
    }: {
      orderId: string;
      status: string;
      note?: string;
    }) => {
      // Call real API using admin-api updateOrderStatus function
      const result = await updateOrderStatus(parseInt(orderId), status, note);
      return result;
    },
    onSuccess: (_, variables) => {
      // Show success toast
      const statusLabel = ORDER_WORKFLOW[variables.status as OrderStatus]?.label || variables.status;
      toast.success('Cập nhật thành công', {
        description: `Trạng thái đơn hàng đã được chuyển sang "${statusLabel}"`,
        duration: 4000,
      });

      // Refresh the order details and orders list after successful update
      queryClient.invalidateQueries({ queryKey: ['admin', 'orders', variables.orderId] });
      queryClient.invalidateQueries({ queryKey: ['admin', 'orders'] });
    },
    onError: (error: any) => {
      console.error('Error updating order status:', error);

      // Enhanced error handling with detailed toast notifications
      if (error.workflowError) {
        const workflowError = error.workflowError as OrderStatusUpdateError;
        toast.error('Không thể cập nhật trạng thái', {
          description: workflowError.message || workflowError.details,
          duration: 6000,
        });
      } else if (error.response?.status === 404) {
        toast.error('Lỗi workflow', {
          description: 'Chuyển đổi trạng thái không hợp lệ. Vui lòng kiểm tra quy trình đơn hàng.',
          duration: 5000,
        });
      } else if (error.response?.status === 401) {
        toast.error('Lỗi xác thực', {
          description: 'Bạn không có quyền thực hiện thao tác này.',
          duration: 4000,
        });
      } else {
        toast.error('Lỗi hệ thống', {
          description: error.message || 'Đã xảy ra lỗi khi cập nhật trạng thái đơn hàng.',
          duration: 4000,
        });
      }
    }
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
    pending: { label: 'Chờ xác nhận', variant: 'outline' as const, icon: Clock },
    confirmed: { label: 'Đã xác nhận', variant: 'secondary' as const, icon: CheckCircle },
    processing: { label: 'Đang xử lý', variant: 'default' as const, icon: Package },
    shipped: { label: 'Đang giao', variant: 'default' as const, icon: Truck },
    delivered: { label: 'Đã giao', variant: 'default' as const, icon: CheckCircle },
    cancelled: { label: 'Đã hủy', variant: 'destructive' as const, icon: XCircle }
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
    pending: { label: 'Chờ thanh toán', variant: 'outline' as const },
    paid: { label: 'Đã thanh toán', variant: 'default' as const },
    failed: { label: 'Thất bại', variant: 'destructive' as const },
    refunded: { label: 'Đã hoàn tiền', variant: 'secondary' as const }
  };

  const config = statusConfig[status as keyof typeof statusConfig];

  return (
    <Badge variant={config?.variant || 'outline'}>
      {config?.label || status}
    </Badge>
  );
};

export default function OrderDetailPage() {
  const { selectedCurrency } = useCurrencyContext();
  const params = useParams();
  const router = useRouter();
  const { user } = useAdminAuth();
  const orderId = params?.id as string;

  const [showUpdateStatusDialog, setShowUpdateStatusDialog] = useState(false);
  const [newStatus, setNewStatus] = useState('');
  const [updateNote, setUpdateNote] = useState('');

  // Helper functions
  const formatCurrency = (amount: number) => {
    return formatCurrencyPrice(amount, selectedCurrency);
  };

  // Get valid next statuses for current order
  const getValidStatusOptions = (currentStatus: OrderStatus) => {
    return getValidNextStatuses(currentStatus);
  };

  const {
    data: order,
    isLoading,
    error,
    refetch
  } = useOrderDetailQuery(orderId);

  const updateStatusMutation = useUpdateOrderStatusMutation();

  const handleUpdateStatus = async () => {
    if (!newStatus || !order) return;

    // Client-side validation before API call
    if (!isValidTransition(order.status, newStatus as OrderStatus)) {
      const errorMessage = getTransitionErrorMessage(order.status, newStatus as OrderStatus);
      toast.error('Chuyển đổi không hợp lệ', {
        description: errorMessage,
        duration: 6000,
      });
      return;
    }

    try {
      await updateStatusMutation.mutateAsync({
        orderId,
        status: newStatus,
        note: updateNote
      });

      setShowUpdateStatusDialog(false);
      setNewStatus('');
      setUpdateNote('');
    } catch (error) {
      // Error handling is done in the mutation onError callback
      console.error('Error updating order status:', error);
    }
  };

  const handleOpenUpdateDialog = () => {
    if (!order) return;

    setNewStatus('');
    setUpdateNote('');
    setShowUpdateStatusDialog(true);
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <div className="h-10 w-20 bg-muted animate-pulse rounded" />
          <div className="h-8 w-48 bg-muted animate-pulse rounded" />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-6">
            <div className="h-64 bg-muted animate-pulse rounded" />
            <div className="h-96 bg-muted animate-pulse rounded" />
          </div>
          <div className="space-y-6">
            <div className="h-48 bg-muted animate-pulse rounded" />
            <div className="h-32 bg-muted animate-pulse rounded" />
          </div>
        </div>
      </div>
    );
  }

  if (error || !order) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <AlertCircle className="h-12 w-12 text-red-500 mx-auto mb-4" />
          <h3 className="text-lg font-semibold">Không tìm thấy đơn hàng</h3>
          <p className="text-muted-foreground mb-4">
            Đơn hàng không tồn tại hoặc đã bị xóa.
          </p>
          <Link href="/admin/orders">
            <Button>Quay lại danh sách</Button>
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link href="/admin/orders">
            <Button variant="outline" size="sm">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Quay lại
            </Button>
          </Link>
          <div>
            <h1 className="text-3xl font-bold">{order.orderCode}</h1>
            <p className="text-muted-foreground">
              Tạo lúc {formatDate(order.createdAt)}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          {getStatusBadge(order.status)}
          {getPaymentStatusBadge(order.paymentStatus)}

          <PermissionGuard permission={PERMISSIONS.ORDERS_WRITE}>
            <Button
              variant="outline"
              onClick={handleOpenUpdateDialog}
              disabled={isFinalStatus(order.status)}
            >
              <Edit className="h-4 w-4 mr-2" />
              {isFinalStatus(order.status) ? 'Trạng thái cuối' : 'Cập nhật trạng thái'}
            </Button>
          </PermissionGuard>
        </div>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Main Content */}
        <div className="lg:col-span-2 space-y-6">
          {/* Order Items */}
          <Card>
            <CardHeader>
              <CardTitle>Sản phẩm đặt hàng</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {order.items.map((item) => (
                  <div key={item.id} className="flex items-center justify-between p-4 border rounded-lg">
                    <div className="flex items-center gap-4">
                      <div className="w-16 h-16 bg-muted rounded-lg flex items-center justify-center">
                        <Package className="h-6 w-6 text-muted-foreground" />
                      </div>
                      <div>
                        <h4 className="font-medium">{item.productName}</h4>
                        <p className="text-sm text-muted-foreground">SKU: {item.productSku}</p>
                        <p className="text-sm text-muted-foreground">
                          {formatCurrency(item.unitPrice)}  {item.quantity}
                        </p>
                      </div>
                    </div>
                    <div className="text-right">
                      <p className="font-medium">{formatCurrency(item.totalPrice)}</p>
                    </div>
                  </div>
                ))}
              </div>

              <Separator className="my-4" />

              {/* Order Summary */}
              <div className="space-y-2">
                <div className="flex justify-between">
                  <span>Tạm tính:</span>
                  <span>{formatCurrency(order.subtotal)}</span>
                </div>
                <div className="flex justify-between">
                  <span>Phí vận chuyển:</span>
                  <span>{formatCurrency(order.shippingFee)}</span>
                </div>
                {order.discount > 0 && (
                  <div className="flex justify-between text-red-600">
                    <span>Giảm giá:</span>
                    <span>-{formatCurrency(order.discount)}</span>
                  </div>
                )}
                <div className="flex justify-between">
                  <span>Thuế:</span>
                  <span>{formatCurrency(order.tax)}</span>
                </div>
                <Separator />
                <div className="flex justify-between font-semibold text-lg">
                  <span>Tổng cộng:</span>
                  <span>{formatCurrency(order.total)}</span>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Workflow Status */}
          <Card>
            <CardHeader>
              <CardTitle>Quy trình đơn hàng</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {(() => {
                  const workflowSteps = [
                    { status: 'pending', label: 'Chờ xác nhận' },
                    { status: 'confirmed', label: 'Đã xác nhận' },
                    { status: 'processing', label: 'Đang xử lý' },
                    { status: 'shipped', label: 'Đang giao' },
                    { status: 'delivered', label: 'Đã giao' }
                  ];

                  const currentIndex = workflowSteps.findIndex(step => step.status === order.status);
                  const isCancelled = order.status === 'cancelled';

                  return workflowSteps.map((step, index) => {
                    const isCompleted = !isCancelled && index <= currentIndex;
                    const isCurrent = step.status === order.status && !isCancelled;
                    const isUpcoming = !isCancelled && index > currentIndex;

                    return (
                      <div key={step.status} className="flex items-center gap-3">
                        <div className={`w-3 h-3 rounded-full flex-shrink-0 ${isCompleted ? 'bg-green-500' :
                          isCurrent ? 'bg-blue-500' :
                            'bg-gray-300'
                          }`} />
                        <div className="flex-1">
                          <div className={`text-sm font-medium ${isCompleted ? 'text-green-700' :
                            isCurrent ? 'text-blue-700' :
                              'text-gray-500'
                            }`}>
                            {step.label}
                          </div>
                        </div>
                        {isCurrent && (
                          <Badge variant="outline" className="text-xs">
                            Hiện tại
                          </Badge>
                        )}
                      </div>
                    );
                  });
                })()}

                {order.status === 'cancelled' && (
                  <div className="flex items-center gap-3 mt-2 p-2 bg-red-50 rounded-lg">
                    <XCircle className="w-3 h-3 text-red-500 flex-shrink-0" />
                    <div className="flex-1">
                      <div className="text-sm font-medium text-red-700">
                        Đơn hàng đã bị hủy
                      </div>
                    </div>
                  </div>
                )}
              </div>
            </CardContent>
          </Card>

          {/* Status History */}
          <Card>
            <CardHeader>
              <CardTitle>Lịch sử trạng thái</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-4">
                {order.statusHistory.length > 0 ? (
                  order.statusHistory.map((history, index) => (
                    <div key={index} className="flex items-start gap-4">
                      <div className="w-2 h-2 bg-primary rounded-full mt-2" />
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          {getStatusBadge(history.status)}
                          <span className="text-sm text-muted-foreground">
                            {formatDate(history.timestamp)}
                          </span>
                        </div>
                        {history.note && (
                          <p className="text-sm text-muted-foreground mt-1">
                            {history.note}
                          </p>
                        )}
                        <p className="text-xs text-muted-foreground">
                          Bi {history.updatedBy}
                        </p>
                      </div>
                    </div>
                  ))
                ) : (
                  <div className="text-sm text-muted-foreground text-center py-4">
                    Chưa có lịch sử thay đổi trạng thái
                  </div>
                )}
              </div>
            </CardContent>
          </Card>
        </div>

        {/* Sidebar */}
        <div className="space-y-6">
          {/* Customer Info */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <User className="h-5 w-5" />
                Thông tin khách hàng
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div>
                  <Label className="text-sm font-medium">Họ tên</Label>
                <p>{order.customerName}</p>
              </div>
              <div>
                <Label className="text-sm font-medium">Email</Label>
                <div className="flex items-center gap-2">
                  <Mail className="h-4 w-4 text-muted-foreground" />
                  <a href={`mailto:${order.customerEmail}`} className="text-blue-600 hover:underline">
                    {order.customerEmail}
                  </a>
                </div>
              </div>
              <div>
                  <Label className="text-sm font-medium">Số điện thoại</Label>
                <div className="flex items-center gap-2">
                  <Phone className="h-4 w-4 text-muted-foreground" />
                  <a href={`tel:${order.customerPhone}`} className="text-blue-600 hover:underline">
                    {order.customerPhone}
                  </a>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Shipping Info */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <MapPin className="h-5 w-5" />
                Thông tin giao hàng
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div>
                  <Label className="text-sm font-medium">Địa chỉ</Label>
                <p>{order.shippingAddress}</p>
              </div>
              <div>
                <Label className="text-sm font-medium">Phng thc</Label>
                <p>{order.shippingMethod}</p>
              </div>
              {order.trackingNumber && (
                <div>
                  <Label className="text-sm font-medium">Mã vận đơn</Label>
                  <p className="font-mono text-sm">{order.trackingNumber}</p>
                </div>
              )}
            </CardContent>
          </Card>

          {/* Payment Info */}
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center gap-2">
                <CreditCard className="h-5 w-5" />
                Thông tin thanh toán
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div>
                  <Label className="text-sm font-medium">Phương thức</Label>
                <p>{order.paymentMethod}</p>
              </div>
              <div>
                  <Label className="text-sm font-medium">Trạng thái</Label>
                <div>{getPaymentStatusBadge(order.paymentStatus)}</div>
              </div>
            </CardContent>
          </Card>

          {/* Next Actions */}
          {!isFinalStatus(order.status) && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <Info className="h-5 w-5" />
                  Bước tiếp theo
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  {(() => {
                    const validNext = getValidNextStatuses(order.status);

                    if (validNext.length === 0) {
                      return (
                        <p className="text-sm text-muted-foreground">
                          Không có bước tiếp theo từ trạng thái hiện tại.
                        </p>
                      );
                    }

                    return validNext.map((next) => (
                      <div key={next.status} className="p-2 border rounded-lg">
                        <div className="font-medium text-sm">{next.label}</div>
                        <div className="text-xs text-muted-foreground">
                          {next.description}
                        </div>
                      </div>
                    ));
                  })()}
                </div>
              </CardContent>
            </Card>
          )}

          {/* Notes */}
          {order.notes && (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center gap-2">
                  <MessageSquare className="h-5 w-5" />
                  Ghi chú
                </CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-sm">{order.notes}</p>
              </CardContent>
            </Card>
          )}
        </div>
      </div>

      {/* Update Status Dialog */}
      <Dialog open={showUpdateStatusDialog} onOpenChange={setShowUpdateStatusDialog}>
        <DialogContent className="max-w-md">
          <DialogHeader>
            <DialogTitle>Cập nhật trạng thái đơn hàng</DialogTitle>
            <DialogDescription>
              Thay đổi trạng thái của đơn hàng {order.orderCode}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            {/* Current Status Info */}
            <div className="p-3 bg-muted rounded-lg">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">Trạng thái hiện tại:</span>
                {getStatusBadge(order.status)}
              </div>
            </div>

            {/* Valid Next Statuses */}
            <div className="space-y-2">
                  <Label>Trạng thái mới</Label>
              {(() => {
                const validOptions = getValidStatusOptions(order.status);

                if (validOptions.length === 0) {
                  return (
                    <div className="text-sm text-muted-foreground p-3 bg-muted rounded-lg flex items-center gap-2">
                      <Info className="h-4 w-4" />
                      Không có trạng thái nào có thể chuyển đổi từ trạng thái hiện tại.
                    </div>
                  );
                }

                return (
                  <>
                    <Select value={newStatus} onValueChange={setNewStatus}>
                      <SelectTrigger>
                        <SelectValue placeholder="Chọn trạng thái tiếp theo" />
                      </SelectTrigger>
                      <SelectContent>
                        {validOptions.map((option) => (
                          <SelectItem key={option.status} value={option.status}>
                            <div className="flex items-center gap-2">
                              <span>{option.label}</span>
                              <span className="text-xs text-muted-foreground">
                                - {option.description}
                              </span>
                            </div>
                          </SelectItem>
                        ))}
                      </SelectContent>
                    </Select>

                    {/* Workflow Info */}
                    <div className="text-xs text-muted-foreground">
                      <Info className="h-3 w-3 inline mr-1" />
                      Chỉ hiển thị các trạng thái hợp lệ theo quy trình đơn hàng
                    </div>
                  </>
                );
              })()}
            </div>

            <div className="space-y-2">
              <Label>Ghi chú (tùy chọn)</Label>
              <Textarea
                value={updateNote}
                onChange={(e) => setUpdateNote(e.target.value)}
                placeholder="Nhập ghi chú về việc thay đổi trạng thái..."
                rows={3}
              />
            </div>
          </div>

          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setShowUpdateStatusDialog(false)}
              disabled={updateStatusMutation.isPending}
            >
              Hủy bỏ
            </Button>
            <Button
              onClick={handleUpdateStatus}
              disabled={!newStatus || updateStatusMutation.isPending || getValidStatusOptions(order.status).length === 0}
            >
              {updateStatusMutation.isPending ? 'Đang cập nhật...' : 'Cập nhật'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}