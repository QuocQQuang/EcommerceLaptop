'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { useAuth } from '@/hooks/useAuth';
import { CurrencyService, formatCurrencyPrice } from '@/lib/currency';
import { orderService } from '@/services/orderService';
import { paymentService } from '@/services/paymentService';
import { PaymentGateway, PaymentMethod } from '@/types/api';
import { OrderResponse } from '@/types/order';
import { PayPalButtons, PayPalScriptProvider } from '@paypal/react-paypal-js';
import { ArrowLeft, Lock } from 'lucide-react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { toast } from 'sonner';

function PayPalPaymentContent() {
    const { selectedCurrency } = useCurrencyContext();
    const searchParams = useSearchParams();
    const router = useRouter();
    const { user } = useAuth();
    const orderId = parseInt(searchParams?.get('orderId') || '0');
    const env = (searchParams?.get('env') || 'sandbox') as 'sandbox' | 'production';
    const [order, setOrder] = useState<OrderResponse | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [paypalOrderId, setPaypalOrderId] = useState<string>('');

    useEffect(() => {
        if (orderId > 0) {
            orderService.getOrderStatus(orderId)
                .then(setOrder)
                .catch(() => toast.error('Không thể tải thông tin đơn hàng'));
        } else {
            toast.error('ID đơn hàng không hợp lệ');
            router.push('/checkout');
        }
        setIsLoading(false);
    }, [orderId, router]);

    const paypalOptions = {
        clientId: process.env.NEXT_PUBLIC_PAYPAL_CLIENT_ID!,
        currency: 'USD',
        intent: 'capture',
        environment: env
    };

    const convertToUSD = (vndAmount: number): string => {
        const usdAmount = CurrencyService.convertAmount(vndAmount, 'VND', 'USD');
        return usdAmount.toFixed(2);
    };

    const createOrder = async (): Promise<string> => {
        try {
            if (!order) {
                throw new Error('Order information not available');
            }

            const result = await paymentService.initializePayment({
                orderId: orderId,
                gateway: PaymentGateway.PayPal,
                method: PaymentMethod.CreditCard,
                environment: env,
                returnUrl: `${window.location.origin}/checkout/confirmation?orderId=${orderId}`,
                cancelUrl: `${window.location.origin}/checkout`
            });

            if (result.isSuccess && result.paymentUrl) {
                const paypalOrderId = result.transactionId || extractOrderIdFromUrl(result.paymentUrl);
                if (paypalOrderId) {
                    setPaypalOrderId(paypalOrderId);
                    return paypalOrderId;
                } else {
                    throw new Error('No PayPal order ID received from backend');
                }
            } else {
                throw new Error(result.errorMessage || 'Failed to create PayPal order');
            }
        } catch (error) {
            console.error('Error creating PayPal order:', error);
            toast.error('Không thể tạo đơn hàng PayPal');
            throw error;
        }
    };

    const extractOrderIdFromUrl = (url: string): string | null => {
        try {
            const urlParams = new URLSearchParams(new URL(url).search);
            return urlParams.get('token') || null;
        } catch {
            return null;
        }
    };

    const onApprove = async (data: any) => {
        try {
            const result = await paymentService.capturePayPalPayment(data.orderID, orderId);

            if (result.isSuccess) {
                toast.success('Thanh toán thành công!');
                router.push(`/checkout/confirmation?orderId=${orderId}`);
            } else {
                throw new Error(result.errorMessage || 'Payment capture failed');
            }
        } catch (error) {
            console.error('Error capturing PayPal payment:', error);
            toast.error('Lỗi khi xử lý thanh toán PayPal');
        }
    };

    const onError = (error: any) => {
        console.error('PayPal error:', error);
        toast.error('Đã xảy ra lỗi với PayPal');
    };

    const onCancel = () => {
        toast.info('Thanh toán đã bị hủy');
        router.push('/checkout');
    };

    if (isLoading) {
        return (
            <div className="min-h-[60vh] flex items-center justify-center">
                <div className="text-center">
                    <LoadingSpinner size="lg" />
                    <p className="mt-4 text-sm text-gray-500">Đang tải thông tin đơn hàng...</p>
                </div>
            </div>
        );
    }

    if (!order) {
        return (
            <div className="min-h-[60vh] flex items-center justify-center">
                <div className="text-center">
                    <p className="text-gray-500">Không tìm thấy thông tin đơn hàng</p>
                    <Button asChild className="mt-4" variant="outline">
                        <Link href="/checkout">Quay li</Link>
                    </Button>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50">
            <div className="max-w-5xl mx-auto px-4 py-8">
                {/* Header */}
                <div className="mb-8">
                    <Link href="/checkout" className="inline-flex items-center text-sm text-gray-500 hover:text-gray-900 transition-colors">
                        <ArrowLeft className="h-4 w-4 mr-1" />
                        Quay li
                    </Link>
                    <h1 className="text-2xl font-semibold text-gray-900 mt-4">Thanh toán</h1>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-5 gap-12">
                    {/* PayPal Form  Left, larger */}
                    <div className="lg:col-span-3 order-2 lg:order-1">
                        <div className="bg-white rounded-lg border border-gray-200 p-6 space-y-6">
                            {/* Amount in USD */}
                            <div className="text-center pb-2">
                                <p className="text-sm text-gray-500">Số tiền thanh toán</p>
                                <p className="text-2xl font-semibold text-gray-900 mt-1">${convertToUSD(order.totalAmount)} USD</p>
                                <p className="text-xs text-gray-400 mt-1">
                                    Tương đương {formatCurrencyPrice(order.totalAmount, selectedCurrency)}
                                </p>
                            </div>

                            <Separator />

                            {/* PayPal Buttons */}
                            <PayPalScriptProvider options={paypalOptions}>
                                <PayPalButtons
                                    style={{
                                        layout: 'vertical',
                                        color: 'blue',
                                        shape: 'rect',
                                        label: 'paypal',
                                        height: 48,
                                    }}
                                    createOrder={createOrder}
                                    onApprove={onApprove}
                                    onError={onError}
                                    onCancel={onCancel}
                                    fundingSource="paypal"
                                />
                            </PayPalScriptProvider>

                            {/* Secured by PayPal */}
                            <div className="flex items-center justify-center gap-1.5 text-xs text-gray-400">
                                <Lock className="h-3 w-3" />
                                <span>được bảo mật bởi PayPal</span>
                            </div>
                        </div>
                    </div>

                    {/* Order Summary  Right, narrower */}
                    <div className="lg:col-span-2 order-1 lg:order-2">
                        <div className="bg-white rounded-lg border border-gray-200 p-6 lg:sticky lg:top-8">
                            <h2 className="text-base font-semibold text-gray-900 mb-4">Đơn hàng #{order.orderNumber}</h2>

                            {/* Items */}
                            <div className="space-y-3 mb-4">
                                {order.items.map((item) => (
                                    <div key={item.id} className="flex justify-between items-start gap-3">
                                        <div className="flex-1 min-w-0">
                                            <p className="text-sm text-gray-900 truncate">{item.productName}</p>
                                            <p className="text-xs text-gray-500">SL: {item.quantity}</p>
                                        </div>
                                        <p className="text-sm text-gray-900 whitespace-nowrap">
                                            {formatCurrencyPrice(item.totalPrice, selectedCurrency)}
                                        </p>
                                    </div>
                                ))}
                            </div>

                            <Separator className="my-4" />

                            {/* Totals */}
                            <div className="space-y-2">
                                <div className="flex justify-between text-sm">
                                    <span className="text-gray-500">Tạm tính</span>
                                    <span className="text-gray-900">{formatCurrencyPrice(order.subtotal, selectedCurrency)}</span>
                                </div>
                                <div className="flex justify-between text-sm">
                                    <span className="text-gray-500">Phí vận chuyển</span>
                                    <span className="text-gray-900">{formatCurrencyPrice(order.shippingFee, selectedCurrency)}</span>
                                </div>
                                {order.discount > 0 && (
                                    <div className="flex justify-between text-sm">
                                        <span className="text-gray-500">Giảm giá</span>
                                        <span className="text-green-600">-{formatCurrencyPrice(order.discount, selectedCurrency)}</span>
                                    </div>
                                )}
                            </div>

                            <Separator className="my-4" />

                            <div className="flex justify-between">
                                <span className="text-base font-semibold text-gray-900">Tổng cộng</span>
                                <span className="text-base font-semibold text-gray-900">{formatCurrencyPrice(order.totalAmount, selectedCurrency)}</span>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

export default function PayPalPaymentPage() {
    return (
        <Suspense fallback={
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <LoadingSpinner />
            </div>
        }>
            <PayPalPaymentContent />
        </Suspense>
    );
}