'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { OrderExportButtons } from '@/components/customer/OrderExportButtons';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { useAuth } from '@/hooks/useAuth';
import { formatCurrencyPrice } from '@/lib/currency';
import logger from '@/lib/logger';
import { orderService } from '@/services/orderService';
import { OrderResponse } from '@/types/order';
import { formatAddressParts } from '@/utils/addressUtils';
import { ArrowLeft, PackageCheck, Truck } from 'lucide-react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { toast } from 'sonner';

function OrderConfirmationContent() {
    const { selectedCurrency } = useCurrencyContext();
    const searchParams = useSearchParams();
    const router = useRouter();
    const orderId = parseInt(searchParams?.get('orderId') || '0');
    const [order, setOrder] = useState<OrderResponse | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const { user } = useAuth();

    useEffect(() => {
        logger.info(' Order Confirmation: Page loaded', {
            orderId,
            pathname: window.location.pathname,
            search: window.location.search
        });

        if (orderId > 0) {
            const fetchOrder = async () => {
                logger.info(' Order Confirmation: Fetching order details', { orderId });

                try {
                    const res = await orderService.getOrderStatus(orderId);
                    setOrder(res);

                    // Ownership check (guard against non-numeric user ids)
                    const currentUserId = Number(user?.id);
                    if (!Number.isFinite(currentUserId) || res.customerId !== currentUserId) {
                        logger.warn(' Order Confirmation: Unauthorized access to order', {
                            orderId,
                            userId: user?.id,
                            orderUserId: res.customerId
                        });
                        toast.error('Bạn không có quyền truy cập đơn hàng này.');
                        router.push('/account/orders');
                        return;
                    }

                    // Status validation - redirect if already processed or success
                    if (res.paymentStatus?.toLowerCase() === 'completed' || res.status?.toLowerCase() === 'confirmed') {
                        logger.info(' Order Confirmation: Order processed/success', {
                            orderId,
                            status: res.status,
                            paymentStatus: res.paymentStatus
                        });
                        toast.success('Đơn hàng đã được xác nhận thành công!');
                        // Show success on this page
                        return;
                    }

                    if (res.status?.toLowerCase() !== 'pending' && res.status?.toLowerCase() !== 'confirmed') {
                        logger.warn(' Order Confirmation: Invalid order status for confirmation', {
                            orderId,
                            status: res.status,
                            paymentStatus: res.paymentStatus
                        });
                        toast.error('Trạng thái đơn hàng không hợp lệ cho trang xác nhận.');
                        router.push('/account/orders');
                        return;
                    }

                    logger.info(' Order Confirmation: Order details loaded', {
                        orderId: res.id,
                        orderNumber: res.orderNumber,
                        status: res.status,
                        paymentMethod: res.paymentMethod,
                        totalAmount: res.totalAmount,
                        itemCount: res.items?.length || 0
                    });
                } catch (error) {
                    logger.error(' Order Confirmation: Failed to load order', {
                        orderId,
                        error: error instanceof Error ? error.message : 'Unknown error'
                    });
                    toast.error('Không thể tải thông tin đơn hàng');
                    router.push('/account/orders');
                } finally {
                    setIsLoading(false);
                    logger.info(' Order Confirmation: Loading completed', { isLoading: false });
                }
            };

            fetchOrder();
        } else {
            logger.error(' Order Confirmation: Invalid order ID', { orderId });
            toast.error('ID đơn hàng không hợp lệ');
            router.push('/checkout');
        }
    }, [orderId, router, user]);

    if (isLoading || !order) {
        return (
            <div className="container mx-auto px-4 py-8 flex justify-center items-center min-h-[60vh]">
                <LoadingSpinner size="lg" />
            </div>
        );
    }

    const isSuccess = order.status?.toLowerCase() === 'confirmed' || order.paymentStatus?.toLowerCase() === 'completed';

    logger.info(' Order Confirmation: Final status determined', {
        orderId: order.id,
        orderNumber: order.orderNumber,
        status: order.status,
        isSuccess,
        paymentMethod: order.paymentMethod,
        totalAmount: order.totalAmount
    });

    return (
        <div className="container mx-auto px-4 py-8">
            <Link href="/account/orders" className="flex items-center text-muted-foreground hover:text-foreground mb-6">
                <ArrowLeft className="h-4 w-4 mr-2" />
                Xem tất cả đơn hàng
            </Link>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                <div className="lg:col-span-2">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center">
                                {isSuccess ? <PackageCheck className="h-5 w-5 mr-2 text-green-500" /> : <Truck className="h-5 w-5 mr-2 text-yellow-500" />}
                                Xác nhận đơn hàng
                            </CardTitle>
                            <CardDescription>
                                n hng #{order.orderNumber} - {formatCurrencyPrice(order.totalAmount, selectedCurrency)}
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-6">
                            <div className="space-y-4">
                                <h3 className="font-semibold">Trạng thái đơn hàng</h3>
                                <Badge variant={isSuccess ? 'default' : 'secondary'}>
                                    {order.status}
                                </Badge>
                                <p className="text-sm text-muted-foreground">
                                    {isSuccess ? 'Thanh toán đã được xác nhận thành công!' : 'Đơn hàng đang chờ xử lý. Chúng tôi sẽ cập nhật trạng thái sớm.'}
                                </p>
                                {order.status === 'Pending' && (
                                    <div className="mt-4 p-3 bg-blue-50 rounded-md">
                                        <p className="text-sm font-medium text-blue-800 mb-2">Đơn hàng chưa thanh toán</p>
                                        <Button asChild variant="outline" size="sm">
                                            <Link href={`/payment/${order.paymentMethod.toLowerCase()}?orderId=${order.id}&env=sandbox`}>
                                                Thanh toán lại ({order.paymentMethod})
                                            </Link>
                                        </Button>
                                    </div>
                                )}
                            </div>

                            <div className="space-y-4">
                                <h3 className="font-semibold">Chi tiết đơn hàng</h3>
                                <div className="grid grid-cols-2 gap-4 text-sm">
                                    <div>
                                        <p className="text-muted-foreground">Mã đơn hàng:</p>
                                        <p className="font-medium">{order.orderNumber}</p>
                                    </div>
                                    <div>
                                        <p className="text-muted-foreground">Ngày đặt hàng:</p>
                                        <p>{new Date(order.createdAt).toLocaleDateString('vi-VN')}</p>
                                    </div>
                                    <div>
                                        <p className="text-muted-foreground">Phương thức thanh toán:</p>
                                        <p>{order.paymentMethod}</p>
                                    </div>
                                    <div>
                                        <p className="text-muted-foreground">Địa chỉ giao hàng:</p>
                                        <div className="text-sm">
                                            {formatAddressParts(order.shippingAddress).full}
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div className="space-y-4">
                                <h3 className="font-semibold">Sản phẩm trong đơn hàng</h3>
                                <div className="space-y-3">
                                    {order.items.map((item) => (
                                        <div key={item.id} className="flex items-center space-x-4 p-3 border rounded-md">
                                            <div className="w-16 h-16 bg-muted rounded">
                                                <span className="text-muted-foreground text-xs">{item.productName.substring(0, 10)}...</span>
                                            </div>
                                            <div className="flex-1">
                                                <p className="font-medium">{item.productName}</p>
                                                <p className="text-sm text-muted-foreground">SL: {item.quantity} x {formatCurrencyPrice(item.unitPrice, selectedCurrency)}</p>
                                            </div>
                                            <p className="font-semibold">{formatCurrencyPrice(item.totalPrice, selectedCurrency)}</p>
                                        </div>
                                    ))}
                                </div>
                                <Separator />
                                <div className="space-y-2">
                                    <div className="flex justify-between">
                                        <span>Tạm tính</span>
                                        <span>{formatCurrencyPrice(order.subtotal, selectedCurrency)}</span>
                                    </div>
                                    <div className="flex justify-between">
                                        <span>Phí vận chuyển</span>
                                        <span>Miễn phí</span>
                                    </div>
                                    <Separator />
                                    <div className="flex justify-between text-lg font-bold">
                                        <span>Tổng cộng</span>
                                        <span>{formatCurrencyPrice(order.totalAmount, selectedCurrency)}</span>
                                    </div>
                                </div>
                            </div>

                            <div className="flex space-x-3">
                                <Button asChild>
                                    <Link href={`/account/orders/${order.id}`}>
                                        Theo dõi đơn hàng
                                    </Link>
                                </Button>
                                {isSuccess && (
                                    <OrderExportButtons
                                        orderId={order.id}
                                        orderNumber={order.orderNumber}
                                    />
                                )}
                            </div>
                        </CardContent>
                    </Card>
                </div>

                <div className="lg:col-span-1">
                    <Card className="sticky top-24">
                        <CardHeader>
                            <CardTitle>Hướng dẫn theo dõi</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4 text-sm">
                            <p> Bạn sẽ nhận được email xác nhận trong vài phút</p>
                            <p> Theo dõi trạng thái đơn hàng trong tài khoản của bạn</p>
                            <p> Dự kiến giao hàng: 2-5 ngày làm việc</p>
                            <p> Miễn phí vận chuyển cho tất cả đơn hàng</p>
                        </CardContent>
                    </Card>
                </div>
            </div>
        </div>
    );
}

export default function OrderConfirmationPage() {
    return (
        <Suspense fallback={
            <div className="container mx-auto px-4 py-8 flex justify-center items-center min-h-[60vh]">
                <LoadingSpinner size="lg" />
            </div>
        }>
            <OrderConfirmationContent />
        </Suspense>
    );
}