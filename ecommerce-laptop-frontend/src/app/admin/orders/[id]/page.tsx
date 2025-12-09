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

        if (!address) return 'Cha c a ch';

        // Backend returns AddressDto with: Street, City, Province, PostalCode, Country
        const addressParts = [
          address.street,
          address.city,
          address.province,
          address.postalCode,
          address.country
        ].filter(part => part && part.trim() !== '');

        return addressParts.length > 0 ? addressParts.join(', ') : 'Cha c a ch';
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
        shippingMethod: 'Giao hng tiu chun', // Default value
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
      toast.success('Cp nht thnh cng', {
        description: `Trng thi n hng  c chuyn sang "${statusLabel}"`,
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
        toast.error('Khng th cp nht trng thi', {
          description: workflowError.message || workflowError.details,
          duration: 6000,
        });
      } else if (error.response?.status === 404) {
        toast.error('Li workflow', {
          description: 'Chuyn i trng thi khng hp l. Vui lng kim tra quy trnh n hng.',
          duration: 5000,
        });
      } else if (error.response?.status === 401) {
        toast.error('Li xc thc', {
          description: 'Bn khng c quyn thc hin thao tc ny.',
          duration: 4000,
        });
      } else {
        toast.error('Li h thng', {
          description: error.message || ' xy ra li khi cp nht trng thi n hng.',
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
      toast.error('Chuyn i khng hp l', {
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
          <h3 className="text-lg font-semibold">Khng tm thy n hng</h3>
          <p className="text-muted-foreground mb-4">
            n hng khng tn ti hoc  b xa.
          </p>
          <Link href="/admin/orders">
            <Button>Quay li danh sch</Button>
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
              Quay li
            </Button>
          </Link>
          <div>
            <h1 className="text-3xl font-bold">{order.orderCode}</h1>
            <p className="text-muted-foreground">
              To lc {formatDate(order.createdAt)}
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
              {isFinalStatus(order.status) ? 'Trng thi cui' : 'Cp nht trng thi'}
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
              <CardTitle>Sn phm t hng</CardTitle>
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
                  <span>Tm tnh:</span>
                  <span>{formatCurrency(order.subtotal)}</span>
                </div>
                <div className="flex justify-between">
                  <span>Ph vn chuyn:</span>
                  <span>{formatCurrency(order.shippingFee)}</span>
                </div>
                {order.discount > 0 && (
                  <div className="flex justify-between text-red-600">
                    <span>Gim gi:</span>
                    <span>-{formatCurrency(order.discount)}</span>
                  </div>
                )}
                <div className="flex justify-between">
                  <span>Thu:</span>
                  <span>{formatCurrency(order.tax)}</span>
                </div>
                <Separator />
                <div className="flex justify-between font-semibold text-lg">
                  <span>Tng cng:</span>
                  <span>{formatCurrency(order.total)}</span>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Workflow Status */}
          <Card>
            <CardHeader>
              <CardTitle>Quy trnh n hng</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="space-y-3">
                {(() => {
                  const workflowSteps = [
                    { status: 'pending', label: 'Ch xc nhn' },
                    { status: 'confirmed', label: ' xc nhn' },
                    { status: 'processing', label: 'ang x l' },
                    { status: 'shipped', label: 'ang giao' },
                    { status: 'delivered', label: ' giao' }
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
                            Hin ti
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
                        n hng  b hy
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
              <CardTitle>Lch s trng thi</CardTitle>
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
                    Cha c lch s thay i trng thi
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
                Thng tin khch hng
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div>
                <Label className="text-sm font-medium">H tn</Label>
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
                <Label className="text-sm font-medium">S in thoi</Label>
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
                Thng tin giao hng
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div>
                <Label className="text-sm font-medium">a ch</Label>
                <p>{order.shippingAddress}</p>
              </div>
              <div>
                <Label className="text-sm font-medium">Phng thc</Label>
                <p>{order.shippingMethod}</p>
              </div>
              {order.trackingNumber && (
                <div>
                  <Label className="text-sm font-medium">M vn n</Label>
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
                Thng tin thanh ton
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
              <div>
                <Label className="text-sm font-medium">Phng thc</Label>
                <p>{order.paymentMethod}</p>
              </div>
              <div>
                <Label className="text-sm font-medium">Trng thi</Label>
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
                  Bc tip theo
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-2">
                  {(() => {
                    const validNext = getValidNextStatuses(order.status);

                    if (validNext.length === 0) {
                      return (
                        <p className="text-sm text-muted-foreground">
                          Khng c bc tip theo t trng thi hin ti.
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
                  Ghi ch
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
            <DialogTitle>Cp nht trng thi n hng</DialogTitle>
            <DialogDescription>
              Thay i trng thi ca n hng {order.orderCode}
            </DialogDescription>
          </DialogHeader>

          <div className="space-y-4">
            {/* Current Status Info */}
            <div className="p-3 bg-muted rounded-lg">
              <div className="flex items-center justify-between">
                <span className="text-sm font-medium">Trng thi hin ti:</span>
                {getStatusBadge(order.status)}
              </div>
            </div>

            {/* Valid Next Statuses */}
            <div className="space-y-2">
              <Label>Trng thi mi</Label>
              {(() => {
                const validOptions = getValidStatusOptions(order.status);

                if (validOptions.length === 0) {
                  return (
                    <div className="text-sm text-muted-foreground p-3 bg-muted rounded-lg flex items-center gap-2">
                      <Info className="h-4 w-4" />
                      Khng c trng thi no c th chuyn i t trng thi hin ti.
                    </div>
                  );
                }

                return (
                  <>
                    <Select value={newStatus} onValueChange={setNewStatus}>
                      <SelectTrigger>
                        <SelectValue placeholder="Chn trng thi tip theo" />
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
                      Ch hin th cc trng thi hp l theo quy trnh n hng
                    </div>
                  </>
                );
              })()}
            </div>

            <div className="space-y-2">
              <Label>Ghi ch (ty chn)</Label>
              <Textarea
                value={updateNote}
                onChange={(e) => setUpdateNote(e.target.value)}
                placeholder="Nhp ghi ch v vic thay i trng thi..."
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
              Hy b
            </Button>
            <Button
              onClick={handleUpdateStatus}
              disabled={!newStatus || updateStatusMutation.isPending || getValidStatusOptions(order.status).length === 0}
            >
              {updateStatusMutation.isPending ? 'ang cp nht...' : 'Cp nht'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}