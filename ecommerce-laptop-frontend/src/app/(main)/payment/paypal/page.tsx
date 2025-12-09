'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { useAuth } from '@/hooks/useAuth';
import { CurrencyService, formatCurrencyPrice } from '@/lib/currency';
import { orderService } from '@/services/orderService';
import { paymentService } from '@/services/paymentService';
import { PaymentGateway, PaymentMethod } from '@/types/api';
import { OrderResponse } from '@/types/order';
import { formatAddressParts } from '@/utils/addressUtils';
import { PayPalButtons, PayPalScriptProvider } from '@paypal/react-paypal-js';
import { ArrowLeft } from 'lucide-react';
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
                .catch(() => toast.error('Khng th ti thng tin n hng'));
        } else {
            toast.error('ID n hng khng hp l');
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

    // Convert VND to USD using CurrencyService
    const convertToUSD = (vndAmount: number): string => {
        const usdAmount = CurrencyService.convertAmount(vndAmount, 'VND', 'USD');
        return usdAmount.toFixed(2);
    };

    const createOrder = async (): Promise<string> => {
        try {
            // Call backend to initialize PayPal payment and get approval URL
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
                // For PayPal, the backend should return the PayPal order ID that we can use
                // Extract PayPal order ID from payment URL or use transactionId
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
            toast.error('Khng th to n hng PayPal');
            throw error;
        }
    };

    // Helper function to extract PayPal order ID from URL if needed
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
            // Call backend to capture the payment
            const result = await paymentService.capturePayPalPayment(data.orderID, orderId);

            if (result.isSuccess) {
                toast.success('Thanh ton PayPal thnh cng!');
                router.push(`/checkout/confirmation?orderId=${orderId}`);
            } else {
                throw new Error(result.errorMessage || 'Payment capture failed');
            }
        } catch (error) {
            console.error('Error capturing PayPal payment:', error);
            toast.error('Li khi x l thanh ton PayPal');
        }
    };

    const onError = (error: any) => {
        console.error('PayPal error:', error);
        toast.error(' xy ra li vi PayPal');
    };

    const onCancel = () => {
        toast.info('Thanh ton  b hy');
        router.push('/checkout');
    };

    if (isLoading) {
        return (
            <div className="container mx-auto px-4 py-8 flex justify-center items-center min-h-[60vh]">
                <LoadingSpinner size="lg" />
            </div>
        );
    }

    if (!order) {
        return (
            <div className="container mx-auto px-4 py-8">
                <Card>
                    <CardContent className="p-6 text-center">
                        <p className="text-muted-foreground">Khng tm thy thng tin n hng</p>
                        <Button asChild className="mt-4">
                            <Link href="/checkout">Quay li thanh ton</Link>
                        </Button>
                    </CardContent>
                </Card>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-4 py-8">
            <Link href="/checkout" className="flex items-center text-muted-foreground hover:text-foreground mb-6">
                <ArrowLeft className="h-4 w-4 mr-2" />
                Quay li thanh ton
            </Link>

            <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
                <div className="lg:col-span-1">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center">
                                <span className="text-2xl mr-2"></span>
                                Thanh ton qua PayPal
                            </CardTitle>
                            <CardDescription>
                                n hng #{order.orderNumber} - {formatCurrencyPrice(order.totalAmount, selectedCurrency)}
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-6">
                            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                                <div className="flex items-center space-x-2">
                                    <div className="w-8 h-8 bg-blue-600 rounded flex items-center justify-center">
                                        <svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="white" viewBox="0 0 16 16">
                                            <path d="M3.51 6.6c.22-.15.46-.28.7-.4.24-.12.48-.23.72-.33.5-.2 1.04-.35 1.6-.45.28-.05.56-.09.85-.12.28-.03.57-.05.85-.05.87 0 1.6.2 2.2.6.6.4.9.95.9 1.65 0 .4-.1.75-.3 1.05-.2.3-.5.55-.9.75-.3.15-.6.28-.9.4-.5.2-1.04.35-1.6.45-.28.05-.56.09-.85.12-.28.03-.57.05-.85.05-.9 0-1.65-.2-2.25-.6-.6-.4-.9-.95-.9-1.65 0-.4.1-.75.3-1.05.2-.3.5-.55.9-.75z" />
                                        </svg>
                                    </div>
                                    <div>
                                        <h3 className="font-medium text-blue-900">PayPal Sandbox</h3>
                                        <p className="text-sm text-blue-700">
                                            {env === 'sandbox' ? 'Mi trng test - khng tr tin tht' : 'Thanh ton thc t'}
                                        </p>
                                    </div>
                                </div>
                            </div>

                            <div className="space-y-2">
                                <div className="flex justify-between">
                                    <span>Tng tin:</span>
                                    <span className="font-semibold">{formatCurrencyPrice(order.totalAmount, selectedCurrency)}</span>
                                </div>
                                <div className="flex justify-between text-sm text-muted-foreground">
                                    <span>Tng ng (USD):</span>
                                    <span>${convertToUSD(order.totalAmount)}</span>
                                </div>
                            </div>

                            <div className="pt-4">
                                <PayPalScriptProvider options={paypalOptions}>
                                    <PayPalButtons
                                        style={{
                                            layout: 'vertical',
                                            color: 'blue',
                                            shape: 'rect',
                                            label: 'paypal'
                                        }}
                                        createOrder={createOrder}
                                        onApprove={onApprove}
                                        onError={onError}
                                        onCancel={onCancel}
                                        fundingSource="paypal"
                                    />
                                </PayPalScriptProvider>
                            </div>

                            <div className="text-xs text-muted-foreground space-y-1">
                                <p> Thanh ton an ton qua cng PayPal</p>
                                <p> H tr th tn dng, ti khon PayPal</p>
                                <p> Bo mt cao vi m ha SSL</p>
                            </div>
                        </CardContent>
                    </Card>
                </div>

                <div className="lg:col-span-1">
                    <Card className="sticky top-24">
                        <CardHeader>
                            <CardTitle>Thng tin n hng</CardTitle>
                            <CardDescription>
                                Chi tit n hng #{order.orderNumber}
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-3">
                                {order.items.map((item) => (
                                    <div key={item.id} className="flex justify-between items-center">
                                        <div className="flex-1">
                                            <p className="font-medium text-sm">{item.productName}</p>
                                            <p className="text-xs text-muted-foreground">
                                                S lng: {item.quantity}  {formatCurrencyPrice(item.unitPrice, selectedCurrency)}
                                            </p>
                                        </div>
                                        <p className="font-semibold">{formatCurrencyPrice(item.totalPrice, selectedCurrency)}</p>
                                    </div>
                                ))}
                            </div>

                            <div className="border-t pt-3 space-y-2">
                                <div className="flex justify-between">
                                    <span>Tm tnh:</span>
                                    <span>{formatCurrencyPrice(order.subtotal, selectedCurrency)}</span>
                                </div>
                                <div className="flex justify-between">
                                    <span>Ph vn chuyn:</span>
                                    <span>{formatCurrencyPrice(order.shippingFee, selectedCurrency)}</span>
                                </div>
                                {order.discount > 0 && (
                                    <div className="flex justify-between text-green-600">
                                        <span>Gim gi:</span>
                                        <span>-{formatCurrencyPrice(order.discount, selectedCurrency)}</span>
                                    </div>
                                )}
                                <div className="flex justify-between text-lg font-bold border-t pt-2">
                                    <span>Tng cng:</span>
                                    <span>{formatCurrencyPrice(order.totalAmount, selectedCurrency)}</span>
                                </div>
                            </div>

                            <div className="bg-gray-50 rounded-lg p-3 text-sm">
                                <h4 className="font-medium mb-2">a ch giao hng:</h4>
                                <p className="text-muted-foreground">
                                    {formatAddressParts(order.shippingAddress).full}
                                </p>
                            </div>
                        </CardContent>
                    </Card>
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