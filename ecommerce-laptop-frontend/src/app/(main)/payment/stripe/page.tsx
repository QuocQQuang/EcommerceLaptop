'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { formatCurrencyPrice } from '@/lib/currency';
import { orderService } from '@/services/orderService';
import { paymentService } from '@/services/paymentService';
import { OrderResponse } from '@/types/order';
import { Elements, PaymentElement, useElements, useStripe } from '@stripe/react-stripe-js';
import { loadStripe, StripeElementsOptions } from '@stripe/stripe-js';
import { AlertCircle, ArrowLeft, Clock, CreditCard, Info, Lock, Mail, Phone, Shield } from 'lucide-react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { toast } from 'sonner';

import { useAuth } from '@/hooks/useAuth';

const stripePromise = loadStripe(process.env.NEXT_PUBLIC_STRIPE_PUBLISHABLE_KEY!);

function StripePaymentContent() {
    const { selectedCurrency } = useCurrencyContext();
    const searchParams = useSearchParams();
    const router = useRouter();
    const orderId = parseInt(searchParams?.get('orderId') || '0');
    const env = (searchParams?.get('env') || 'sandbox') as 'sandbox' | 'production';
    const [order, setOrder] = useState<OrderResponse | null>(null);
    const [clientSecret, setClientSecret] = useState('');
    const [isLoading, setIsLoading] = useState(true);
    const [isProcessing, setIsProcessing] = useState(false);

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

    const createPaymentIntent = async () => {
        try {
            const result = await paymentService.createStripePaymentIntent(orderId, env);
            if (result.isSuccess) {
                setClientSecret(result.clientSecret);
            } else {
                toast.error(result.errorMessage || 'Khng th to thanh ton Stripe');
            }
        } catch (error) {
            toast.error('Li khi to thanh ton Stripe');
        }
    };

    useEffect(() => {
        if (orderId > 0) {
            createPaymentIntent();
        }
    }, [orderId]);

    const options: StripeElementsOptions = {
        clientSecret,
        appearance: {
            theme: 'stripe',
            variables: {
                colorPrimary: '#2563eb',
                colorText: '#1f2937',
                colorBackground: '#ffffff',
                colorDanger: '#dc2626',
                colorSuccess: '#059669',
                colorWarning: '#d97706',
                fontFamily: 'system-ui, -apple-system, sans-serif',
                borderRadius: '8px',
                spacingUnit: '4px',
            },
            rules: {
                '.Input': {
                    border: '1px solid #d1d5db',
                    borderRadius: '8px',
                    padding: '12px',
                    fontSize: '16px',
                    transition: 'all 0.2s ease-in-out',
                },
                '.Input:focus': {
                    borderColor: '#2563eb',
                    boxShadow: '0 0 0 3px rgba(37, 99, 235, 0.1)',
                },
                '.Input--invalid': {
                    borderColor: '#dc2626',
                },
                '.Label': {
                    fontWeight: '600',
                    color: '#374151',
                    marginBottom: '6px',
                },
                '.Tab': {
                    borderRadius: '8px',
                    padding: '12px 16px',
                    fontWeight: '500',
                },
                '.Tab--selected': {
                    backgroundColor: '#2563eb',
                    color: '#ffffff',
                },
            },
        },
    };

    if (isLoading || !clientSecret) {
        return (
            <div className="container mx-auto px-4 py-8 flex justify-center items-center min-h-[60vh]">
                <Card className="w-full max-w-md">
                    <CardContent className="p-8 text-center">
                        <LoadingSpinner size="lg" />
                        <p className="mt-4 text-muted-foreground">
                            {isLoading ? 'ang ti thng tin n hng...' : 'ang khi to thanh ton...'}
                        </p>
                    </CardContent>
                </Card>
            </div>
        );
    }

    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <Link href="/checkout" className="flex items-center text-muted-foreground hover:text-foreground mb-6">
                <ArrowLeft className="h-4 w-4 mr-2" />
                Quay li thanh ton
            </Link>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                {/* Order Summary */}
                <div className="lg:col-span-1">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center text-lg">
                                <CreditCard className="h-5 w-5 mr-2" />
                                Tm tt n hng
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-2">
                                <div className="flex justify-between">
                                    <span className="text-sm text-muted-foreground">M n hng:</span>
                                    <span className="font-medium">#{order?.orderNumber}</span>
                                </div>
                                <div className="flex justify-between">
                                    <span className="text-sm text-muted-foreground">Ngy t:</span>
                                    <span className="text-sm">{order?.createdAt ? new Date(order.createdAt).toLocaleDateString('vi-VN') : 'N/A'}</span>
                                </div>
                                <div className="flex justify-between">
                                    <span className="text-sm text-muted-foreground">Trng thi:</span>
                                    <Badge variant="outline" className="text-xs">
                                        {order?.status || 'Ch thanh ton'}
                                    </Badge>
                                </div>
                            </div>

                            <Separator />

                            <div className="space-y-2">
                                <div className="flex justify-between text-sm">
                                    <span>Tm tnh:</span>
                                    <span>{formatCurrencyPrice((order?.totalAmount || 0) - (order?.shippingFee || 0), selectedCurrency)}</span>
                                </div>
                                <div className="flex justify-between text-sm">
                                    <span>Ph vn chuyn:</span>
                                    <span>{formatCurrencyPrice(order?.shippingFee || 0, selectedCurrency)}</span>
                                </div>
                                <Separator />
                                <div className="flex justify-between font-semibold text-lg">
                                    <span>Tng cng:</span>
                                    <span className="text-primary">{formatCurrencyPrice(order?.totalAmount || 0, selectedCurrency)}</span>
                                </div>
                            </div>

                            {/* Order Items */}
                            {order?.items && order.items.length > 0 && (
                                <div className="space-y-3">
                                    <h4 className="font-medium text-sm">Sn phm trong n hng</h4>
                                    <div className="space-y-2 max-h-40 overflow-y-auto">
                                        {order.items.map((item: any, index: number) => (
                                            <div key={index} className="flex items-center space-x-3 p-2 bg-gray-50 rounded-lg">
                                                <div className="w-12 h-12 bg-gray-200 rounded-md flex items-center justify-center">
                                                    <span className="text-xs text-gray-500">IMG</span>
                                                </div>
                                                <div className="flex-1 min-w-0">
                                                    <p className="text-sm font-medium truncate">{item.productName || item.name}</p>
                                                    <p className="text-xs text-muted-foreground">
                                                        S lng: {item.quantity}  {formatCurrencyPrice(item.unitPrice || item.price, selectedCurrency)}
                                                    </p>
                                                </div>
                                                <div className="text-sm font-medium">
                                                    {formatCurrencyPrice((item.unitPrice || item.price) * item.quantity, selectedCurrency)}
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                </div>
                            )}

                            {/* Security Info */}
                            <div className="mt-6 p-4 bg-green-50 rounded-lg border border-green-200">
                                <div className="flex items-start space-x-2">
                                    <Shield className="h-5 w-5 text-green-600 mt-0.5" />
                                    <div className="space-y-1">
                                        <p className="text-sm font-medium text-green-800">Bo mt thanh ton</p>
                                        <p className="text-xs text-green-700">
                                            Thng tin th ca bn c m ha v bo mt bi Stripe.
                                            Chng ti khng lu tr thng tin th tn dng.
                                        </p>
                                    </div>
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                </div>

                {/* Payment Form */}
                <div className="lg:col-span-2">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center text-xl">
                                <CreditCard className="h-6 w-6 mr-2" />
                                Thng tin thanh ton
                            </CardTitle>
                            <CardDescription>
                                Nhp thng tin th tn dng  hon tt thanh ton
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-6">
                            <Elements stripe={stripePromise} options={options}>
                                <StripeForm order={order} clientSecret={clientSecret} />
                            </Elements>
                        </CardContent>
                    </Card>
                </div>
            </div>
        </div>
    );
}

function StripeForm({ order, clientSecret }: { order: OrderResponse | null, clientSecret: string }) {
    const { user } = useAuth();
    const stripe = useStripe();
    const elements = useElements();
    const router = useRouter();
    const [error, setError] = useState('');
    const [isProcessing, setIsProcessing] = useState(false);
    const [isComplete, setIsComplete] = useState(false);
    const [paymentMethod, setPaymentMethod] = useState<string>('');

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();

        if (!stripe || !elements || !order) {
            setError('Stripe.js cha c ti hoc thiu thng tin n hng.');
            return;
        }

        setIsProcessing(true);
        setError('');

        try {
            // Use confirmPayment with PaymentElement instead of CardElement
            const { error: stripeError } = await stripe.confirmPayment({
                elements,
                confirmParams: {
                    return_url: `${window.location.origin}/payment/return/stripe?orderId=${order.id}`,
                    payment_method_data: {
                        billing_details: {
                            name: user?.email || 'Customer Name',
                            email: user?.email || 'customer@example.com',
                        }
                    }
                }
            });

            if (stripeError) {
                setError(stripeError.message || 'Thanh ton tht bi. Vui lng th li.');
                setIsProcessing(false);
            }
            // If successful, Stripe will redirect to return_url
        } catch (err) {
            setError('C li xy ra khi x l thanh ton. Vui lng th li.');
            setIsProcessing(false);
        }
    };

    return (
        <form onSubmit={handleSubmit} className="space-y-6">
            {/* Payment Method Selection */}
            <div className="space-y-4">
                <Label className="text-base font-semibold">Phng thc thanh ton</Label>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    <div className="flex items-center space-x-3 p-4 border rounded-lg hover:bg-gray-50 cursor-pointer">
                        <CreditCard className="h-5 w-5 text-blue-600" />
                        <div>
                            <p className="font-medium">Th tn dng</p>
                            <p className="text-sm text-muted-foreground">Visa, Mastercard, American Express</p>
                        </div>
                    </div>
                    <div className="flex items-center space-x-3 p-4 border rounded-lg hover:bg-gray-50 cursor-pointer">
                        <Lock className="h-5 w-5 text-green-600" />
                        <div>
                            <p className="font-medium">Bo mt cao</p>
                            <p className="text-sm text-muted-foreground">M ha SSL 256-bit</p>
                        </div>
                    </div>
                </div>

                {/* Accepted Cards */}
                <div className="flex items-center space-x-2 text-sm text-muted-foreground">
                    <span>Chp nhn:</span>
                    <div className="flex space-x-1">
                        <div className="w-8 h-5 bg-blue-600 rounded text-white text-xs flex items-center justify-center font-bold">V</div>
                        <div className="w-8 h-5 bg-red-600 rounded text-white text-xs flex items-center justify-center font-bold">M</div>
                        <div className="w-8 h-5 bg-blue-800 rounded text-white text-xs flex items-center justify-center font-bold">A</div>
                    </div>
                </div>
            </div>

            {/* Payment Element */}
            <div className="space-y-4">
                <Label className="text-base font-semibold">Thng tin th</Label>
                <div className="p-4 border rounded-lg bg-gray-50">
                    <PaymentElement
                        options={{
                            layout: 'tabs',
                            fields: {
                                billingDetails: {
                                    name: 'auto',
                                    email: 'auto',
                                    phone: 'auto',
                                    address: 'auto'
                                }
                            }
                        }}
                        onChange={(event) => {
                            setIsComplete(event.complete);
                            // Clear any previous errors when user starts typing
                            if (event.complete) {
                                setError('');
                            }
                        }}
                    />
                </div>
            </div>

            {/* Error Display */}
            {error && (
                <div className="flex items-start space-x-2 p-4 bg-red-50 border border-red-200 rounded-lg">
                    <AlertCircle className="h-5 w-5 text-red-600 mt-0.5" />
                    <div>
                        <p className="text-sm font-medium text-red-800">Li thanh ton</p>
                        <p className="text-sm text-red-700">{error}</p>
                    </div>
                </div>
            )}

            {/* Security Notice */}
            <div className="flex items-start space-x-2 p-4 bg-blue-50 border border-blue-200 rounded-lg">
                <Info className="h-5 w-5 text-blue-600 mt-0.5" />
                <div className="space-y-1">
                    <p className="text-sm font-medium text-blue-800">Thng tin bo mt</p>
                    <ul className="text-xs text-blue-700 space-y-1">
                        <li> Thng tin th c m ha v x l bi Stripe</li>
                        <li> Chng ti khng lu tr thng tin th tn dng</li>
                        <li> Giao dch c bo v bi chng ch SSL</li>
                    </ul>
                </div>
            </div>

            {/* Submit Button */}
            <div className="space-y-4">
                <Button
                    type="submit"
                    disabled={!stripe || isProcessing || !isComplete}
                    className="w-full h-12 text-lg font-semibold"
                >
                    {isProcessing ? (
                        <div className="flex items-center space-x-2">
                            <LoadingSpinner size="sm" />
                            <span>ang x l thanh ton...</span>
                        </div>
                    ) : (
                        <div className="flex items-center space-x-2">
                            <CreditCard className="h-5 w-5" />
                            <span>Thanh ton {formatCurrencyPrice(order?.totalAmount || 0, 'VND')}</span>
                        </div>
                    )}
                </Button>

                {/* Payment Progress */}
                {isProcessing && (
                    <div className="space-y-2">
                        <div className="flex items-center space-x-2 text-sm text-muted-foreground">
                            <Clock className="h-4 w-4" />
                            <span>Vui lng khng ng trnh duyt trong qu trnh thanh ton</span>
                        </div>
                        <div className="w-full bg-gray-200 rounded-full h-2">
                            <div className="bg-blue-600 h-2 rounded-full animate-pulse" style={{ width: '60%' }}></div>
                        </div>
                    </div>
                )}

                {/* Support Info */}
                <div className="text-center space-y-2">
                    <p className="text-sm text-muted-foreground">
                        Cn h tr? Lin h chng ti
                    </p>
                    <div className="flex justify-center space-x-4 text-sm">
                        <div className="flex items-center space-x-1">
                            <Phone className="h-4 w-4" />
                            <span>1900-xxxx</span>
                        </div>
                        <div className="flex items-center space-x-1">
                            <Mail className="h-4 w-4" />
                            <span>support@example.com</span>
                        </div>
                    </div>
                </div>
            </div>
        </form>
    );
}

export default function StripePaymentPage() {
    return (
        <Suspense fallback={
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <LoadingSpinner />
            </div>
        }>
            <StripePaymentContent />
        </Suspense>
    );
}