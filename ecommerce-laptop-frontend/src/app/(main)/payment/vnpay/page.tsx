'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import { Separator } from '@/components/ui/separator';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { formatCurrencyPrice } from '@/lib/currency';
import { orderService } from '@/services/orderService';
import { InitializePaymentRequest, paymentService } from '@/services/paymentService';
import { PaymentGateway, PaymentMethod } from '@/types/api';
import { OrderResponse } from '@/types/order';
import { formatAddressForDisplay } from '@/utils/addressUtils';
import { ArrowLeft, CreditCard } from 'lucide-react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { toast } from 'sonner';

function VNPayPaymentContent() {
    const { selectedCurrency } = useCurrencyContext();
    const searchParams = useSearchParams();
    const router = useRouter();
    const orderId = parseInt(searchParams?.get('orderId') || '0');
    const env = searchParams?.get('env') || 'sandbox';
    const [environment, setEnvironment] = useState<'sandbox' | 'production'>(env as 'sandbox' | 'production');
    const [isLoading, setIsLoading] = useState(false);
    const [order, setOrder] = useState<OrderResponse | null>(null);

    // Fetch order details on mount
    useEffect(() => {
        if (orderId > 0) {
            orderService.getOrderStatus(orderId)
                .then(orderData => {
                    console.log('VNPay Order Data:', orderData);
                    console.log('Shipping Address:', orderData.shippingAddress);
                    console.log('Shipping Address Type:', typeof orderData.shippingAddress);
                    setOrder(orderData);
                })
                .catch(() => toast.error('Khng th ti thng tin n hng'));
        } else {
            toast.error('ID đơn hàng không hợp lệ');
            router.push('/checkout');
        }
    }, [orderId, router]);

    const handlePayment = async () => {
        if (!orderId || !order) {
            toast.error('Thông tin đơn hàng không hợp lệ');
            return;
        }

        setIsLoading(true);
        const toastId = toast.loading('ang to lin kt thanh ton VNPAY...');

        const request: InitializePaymentRequest = {
            orderId,
            gateway: PaymentGateway.VnPay,
            method: PaymentMethod.CreditCard,
            environment,
            returnUrl: `${window.location.origin}/payment/return`,
            cancelUrl: `${window.location.origin}/payment/cancel`,
        };

        try {
            const result = await paymentService.initializePayment(request);

            if (result.isSuccess && result.paymentUrl) {
                toast.success('Lin kt thanh ton  sn sng!', { id: toastId });
                window.location.href = result.paymentUrl;
            } else {
                toast.error(result.errorMessage || 'Khng th to lin kt thanh ton', { id: toastId });
            }
        } catch (error) {
            toast.error('Li khi to thanh ton. Vui lng th li.', { id: toastId });
        } finally {
            setIsLoading(false);
        }
    };

    if (!order) {
        return (
            <div className="container mx-auto px-4 py-8 flex justify-center items-center min-h-[60vh]">
                <LoadingSpinner size="lg" />
            </div>
        );
    }

    return (
        <div className="container mx-auto px-4 py-8">
            <Link href="/checkout" className="flex items-center text-muted-foreground hover:text-foreground mb-6">
                <ArrowLeft className="h-4 w-4 mr-2" />
                Quay lại thanh toán
            </Link>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                <div className="lg:col-span-2">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center">
                                <CreditCard className="h-5 w-5 mr-2" />
                                Thanh ton qua VNPAY
                            </CardTitle>
                            <CardDescription>
                                n hng #{order.orderNumber} - {formatCurrencyPrice(order.totalAmount, selectedCurrency)}
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-6">
                            <div className="space-y-2">
                                <h3 className="font-semibold">Thông tin đơn hàng</h3>
                                <div className="text-sm text-muted-foreground">
                                    <p>Trạng thái: <Badge variant={order.status === 'Pending' ? 'secondary' : 'default'}>{order.status}</Badge></p>
                                    <p>Phương thức: VNPAY</p>
                                    <p>Địa chỉ: {formatAddressForDisplay(order.shippingAddress)}</p>
                                </div>
                            </div>

                            <div className="space-y-4">
                                <h3 className="font-semibold">Chế độ thanh toán</h3>
                                <div className="space-y-2">
                                    <RadioGroup value={environment} onValueChange={(value) => setEnvironment(value as 'sandbox' | 'production')}>
                                        <div className="flex items-center space-x-2 p-3 border rounded-md">
                                            <RadioGroupItem value="sandbox" id="sandbox" />
                                            <label htmlFor="sandbox" className="text-sm font-medium">
                                                Chế độ Test (Sandbox)
                                            </label>
                                        </div>
                                        <div className="flex items-center space-x-2 p-3 border rounded-md">
                                            <RadioGroupItem value="production" id="production" />
                                            <label htmlFor="production" className="text-sm font-medium">
                                                Chế độ Thực tế (Production)
                                            </label>
                                        </div>
                                    </RadioGroup>
                                </div>
                                <p className="text-sm text-muted-foreground">
                                    {environment === 'sandbox' ? 'S dng VNPAY Sandbox - Khng tr tin tht. S dng th test: 9704198526191432198 (thnh cng)' : 'Thanh ton thc t vi th tht.'}
                                </p>
                            </div>

                            <Button
                                onClick={handlePayment}
                                className="w-full"
                                size="lg"
                                disabled={isLoading}
                            >
                                {isLoading ? <LoadingSpinner /> : 'Thanh ton vi VNPAY'}
                            </Button>

                            <div className="text-xs text-muted-foreground text-center">
                                Bằng cách nhấp vào nút trên, bạn đồng ý với <Link href="/terms" className="underline">Điều khoản dịch vụ</Link> và <Link href="/privacy" className="underline">Chính sách bảo mật</Link>
                            </div>
                        </CardContent>
                    </Card>
                </div>

                <div className="lg:col-span-1">
                    <Card className="sticky top-24">
                        <CardHeader>
                            <CardTitle>Tóm tắt đơn hàng</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-2">
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
                        </CardContent>
                    </Card>
                </div>
            </div>
        </div>
    );
}

export default function VNPayPaymentPage() {
    return (
        <Suspense fallback={
            <div className="container mx-auto px-4 py-8 flex justify-center items-center min-h-[60vh]">
                <LoadingSpinner size="lg" />
            </div>
        }>
            <VNPayPaymentContent />
        </Suspense>
    );
}