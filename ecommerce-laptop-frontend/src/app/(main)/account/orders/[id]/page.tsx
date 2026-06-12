'use client';

import { CustomerInfo } from '@/components/customer/CustomerInfo';
import { OrderExportButtons } from '@/components/customer/OrderExportButtons';
import { OrderItemCard } from '@/components/customer/OrderItemCard';
import { OrderStatusProgress } from '@/components/customer/OrderStatusProgress';
import { OrderTimeline } from '@/components/customer/OrderTimeline';
import { PaymentInfo } from '@/components/customer/PaymentInfo';
import { ShippingTracking } from '@/components/customer/ShippingTracking';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { orderService } from '@/services/orderService';
import {
    ArrowLeft,
    Calendar,
    CheckCircle,
    Clock,
    Package,
    RefreshCw,
    Truck,
    XCircle
} from 'lucide-react';
import { useSession } from 'next-auth/react';
import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

// Order interfaces
interface OrderDetails {
    id: number;
    orderNumber: string;
    status: string;
    subtotal: number;
    shippingCost: number;
    taxAmount: number;
    totalAmount: number;
    createdAt: string;
    estimatedDeliveryDate?: string;
    customerId: number;
    customerName: string;
    customerEmail: string;
    shippingAddress: string;
    trackingNumber?: string;
    paymentStatus?: string;
    paymentMethod?: string;
    items: OrderItem[];
    auditTrail: OrderAudit[];
}

interface OrderItem {
    id: number;
    productId: number;
    productName: string;
    quantity: number;
    unitPrice: number;
    totalPrice: number;
}

interface OrderAudit {
    oldStatus: string;
    newStatus: string;
    changedBy: string;
    reason: string;
    changedAt: string;
}

export default function OrderDetailPage() {
    const { data: session } = useSession();
    const params = useParams();
    const router = useRouter();
    const orderId = params.id as string;

    const [order, setOrder] = useState<OrderDetails | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isRefreshing, setIsRefreshing] = useState(false);

    // Load order details
    const loadOrderDetails = async (showRefresh = false) => {
        if (!session?.user?.id || !orderId) return;

        if (showRefresh) {
            setIsRefreshing(true);
        } else {
            setIsLoading(true);
        }

        try {
            const response = await orderService.getOrderStatus(parseInt(orderId));
            setOrder(response as unknown as OrderDetails);
        } catch (error: any) {
            console.error('Error loading order details:', error);
            toast.error('Không thể tải thông tin đơn hàng');
            if (error.response?.status === 404) {
                router.push('/account/orders');
            }
        } finally {
            setIsLoading(false);
            setIsRefreshing(false);
        }
    };

    useEffect(() => {
        loadOrderDetails();
    }, [session, orderId]);

    // Status mapping
    const getStatusInfo = (status: string) => {
        const statusMap: Record<string, { label: string; color: string; icon: React.ReactNode }> = {
            'pending': { label: 'Ch x l', color: 'bg-yellow-100 text-yellow-800', icon: <Clock className="h-4 w-4" /> },
            'confirmed': { label: ' xc nhn', color: 'bg-blue-100 text-blue-800', icon: <CheckCircle className="h-4 w-4" /> },
            'processing': { label: 'ang x l', color: 'bg-purple-100 text-purple-800', icon: <Package className="h-4 w-4" /> },
            'shipped': { label: ' giao hng', color: 'bg-green-100 text-green-800', icon: <Truck className="h-4 w-4" /> },
            'delivered': { label: ' nhn hng', color: 'bg-green-100 text-green-800', icon: <CheckCircle className="h-4 w-4" /> },
            'cancelled': { label: ' hy', color: 'bg-red-100 text-red-800', icon: <XCircle className="h-4 w-4" /> }
        };
        return statusMap[status] || { label: status, color: 'bg-gray-100 text-gray-800', icon: <Clock className="h-4 w-4" /> };
    };

    const getPaymentStatusInfo = (status: string) => {
        const statusMap: Record<string, { label: string; color: string }> = {
            'pending': { label: 'Ch thanh ton', color: 'bg-yellow-100 text-yellow-800' },
            'paid': { label: ' thanh ton', color: 'bg-green-100 text-green-800' },
            'failed': { label: 'Thanh ton tht bi', color: 'bg-red-100 text-red-800' },
            'refunded': { label: ' hon tin', color: 'bg-blue-100 text-blue-800' }
        };
        return statusMap[status] || { label: status, color: 'bg-gray-100 text-gray-800' };
    };

    // Determine payment status and method from audit trail
    const getPaymentInfo = (order: OrderDetails) => {
        let paymentStatus = order.paymentStatus || 'pending';
        let paymentMethod = order.paymentMethod || 'Cha xc nh';

        // Check audit trail for payment information
        if (order.auditTrail && order.auditTrail.length > 0) {
            const paymentAudit = order.auditTrail.find(audit =>
                audit.reason && (
                    audit.reason.includes('Payment confirmed') ||
                    audit.reason.includes('PayPal') ||
                    audit.reason.includes('VNPay') ||
                    audit.reason.includes('MoMo') ||
                    audit.reason.includes('Stripe') ||
                    audit.reason.includes('ZaloPay')
                )
            );

            if (paymentAudit) {
                paymentStatus = 'paid';

                // Extract payment method from reason
                if (paymentAudit.reason.includes('PayPal')) {
                    paymentMethod = 'PayPal';
                } else if (paymentAudit.reason.includes('VNPay')) {
                    paymentMethod = 'VNPay';
                } else if (paymentAudit.reason.includes('MoMo')) {
                    paymentMethod = 'MoMo';
                } else if (paymentAudit.reason.includes('Stripe')) {
                    paymentMethod = 'Stripe';
                } else if (paymentAudit.reason.includes('ZaloPay')) {
                    paymentMethod = 'ZaloPay';
                } else {
                    paymentMethod = ' thanh ton';
                }
            }
        }

        return { paymentStatus, paymentMethod };
    };

    if (isLoading) {
        return (
            <div className="container mx-auto px-4 py-8">
                <div className="mb-6">
                    <Skeleton className="h-8 w-48 mb-4" />
                    <Skeleton className="h-4 w-32" />
                </div>
                <div className="grid gap-6">
                    <Skeleton className="h-64 w-full" />
                    <Skeleton className="h-48 w-full" />
                    <Skeleton className="h-32 w-full" />
                </div>
            </div>
        );
    }

    if (!order) {
        return (
            <div className="container mx-auto px-4 py-8">
                <div className="text-center">
                    <h1 className="text-2xl font-bold text-gray-900 mb-4">Không tìm thấy đơn hàng</h1>
                    <p className="text-gray-600 mb-6">Đơn hàng bạn tìm kiếm không tồn tại hoặc đã bị xóa.</p>
                    <Link href="/account/orders">
                        <Button>
                            <ArrowLeft className="h-4 w-4 mr-2" />
                            Quay lại danh sách đơn hàng
                        </Button>
                    </Link>
                </div>
            </div>
        );
    }

    const statusInfo = getStatusInfo(order.status);
    const { paymentStatus, paymentMethod } = getPaymentInfo(order);
    const paymentStatusInfo = getPaymentStatusInfo(paymentStatus);

    return (
        <div className="container mx-auto px-4 py-8">
            {/* Header */}
            <div className="mb-6">
                <div className="flex items-center justify-between mb-4">
                    <div className="flex items-center space-x-4">
                        <Link href="/account/orders">
                            <Button variant="outline" size="sm">
                                <ArrowLeft className="h-4 w-4 mr-2" />
                                Quay li
                            </Button>
                        </Link>
                        <div>
                            <h1 className="text-2xl font-bold text-gray-900">
                                Đơn hàng #{order.orderNumber}
                            </h1>
                            <p className="text-gray-600">
                                Đặt ngày {new Date(order.createdAt).toLocaleDateString('vi-VN')}
                            </p>
                        </div>
                    </div>
                    <div className="flex items-center space-x-2">
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={() => loadOrderDetails(true)}
                            disabled={isRefreshing}
                        >
                            <RefreshCw className={`h-4 w-4 mr-2 ${isRefreshing ? 'animate-spin' : ''}`} />
                            Làm mới
                        </Button>
                        {order.status === 'delivered' && (
                            <OrderExportButtons
                                orderId={order.id}
                                orderNumber={order.orderNumber}
                            />
                        )}
                    </div>
                </div>

                {/* Order Status Progress */}
                <OrderStatusProgress status={order.status} className="mb-6" />

                {/* Payment Status */}
                <div className="flex items-center space-x-4">
                    <Badge className={`${paymentStatusInfo.color}`}>
                        {paymentStatusInfo.label}
                    </Badge>
                </div>
            </div>

            <div className="grid gap-6 lg:grid-cols-3">
                {/* Order Items */}
                <div className="lg:col-span-2">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center space-x-2">
                                <Package className="h-5 w-5" />
                                <span>Chi tiết sản phẩm</span>
                            </CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="space-y-4">
                                {order.items.map((item) => (
                                    <OrderItemCard key={item.id} item={item} />
                                ))}
                            </div>
                        </CardContent>
                    </Card>

                    {/* Order Timeline */}
                    <OrderTimeline auditTrail={order.auditTrail} className="mt-6" />
                </div>

                {/* Order Summary */}
                <div className="space-y-6">
                    {/* Customer Information */}
                    <CustomerInfo
                        customerName={order.customerName}
                        customerEmail={order.customerEmail}
                    />

                    {/* Shipping Information */}
                    <ShippingTracking
                        trackingNumber={order.trackingNumber}
                        shippingAddress={order.shippingAddress}
                        estimatedDeliveryDate={order.estimatedDeliveryDate}
                    />

                    {/* Payment Information */}
                    {(() => {
                        const { paymentStatus, paymentMethod } = getPaymentInfo(order);
                        return (
                            <PaymentInfo
                                paymentStatus={paymentStatus}
                                paymentMethod={paymentMethod}
                                totalAmount={order.totalAmount}
                                subtotal={order.subtotal}
                                shippingCost={order.shippingCost}
                                taxAmount={order.taxAmount}
                            />
                        );
                    })()}

                    {/* Delivery Information */}
                    {order.estimatedDeliveryDate && (
                        <Card>
                            <CardHeader>
                                <CardTitle className="flex items-center space-x-2">
                                    <Calendar className="h-5 w-5" />
                                    <span>Thông tin giao hàng</span>
                                </CardTitle>
                            </CardHeader>
                            <CardContent>
                                <p className="text-sm text-gray-700">
                                    Dự kiến giao hàng: {new Date(order.estimatedDeliveryDate).toLocaleDateString('vi-VN')}
                                </p>
                            </CardContent>
                        </Card>
                    )}
                </div>
            </div>
        </div>
    );
}
