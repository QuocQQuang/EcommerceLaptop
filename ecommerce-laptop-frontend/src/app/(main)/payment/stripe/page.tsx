'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Button } from '@/components/ui/button';
import { Separator } from '@/components/ui/separator';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { formatCurrencyPrice } from '@/lib/currency';
import { orderService } from '@/services/orderService';
import { paymentService } from '@/services/paymentService';
import { OrderResponse } from '@/types/order';
import { Elements, PaymentElement, useElements, useStripe } from '@stripe/react-stripe-js';
import { loadStripe, StripeElementsOptions } from '@stripe/stripe-js';
import { ArrowLeft, Lock } from 'lucide-react';
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
                colorPrimary: '#0a2540',
                colorText: '#30313d',
                colorBackground: '#ffffff',
                colorDanger: '#df1b41',
                fontFamily: 'system-ui, -apple-system, "Segoe UI", Roboto, sans-serif',
                borderRadius: '6px',
                spacingUnit: '4px',
            },
            rules: {
                '.Input': {
                    border: '1px solid #e0e0e0',
                    borderRadius: '6px',
                    padding: '12px',
                    fontSize: '15px',
                    transition: 'border-color 0.15s ease',
                },
                '.Input:focus': {
                    borderColor: '#0a2540',
                    boxShadow: '0 0 0 1px #0a2540',
                },
                '.Input--invalid': {
                    borderColor: '#df1b41',
                },
                '.Label': {
                    fontWeight: '500',
                    color: '#30313d',
                    marginBottom: '6px',
                    fontSize: '14px',
                },
                '.Tab': {
                    borderRadius: '6px',
                    padding: '10px 16px',
                    fontWeight: '500',
                },
                '.Tab--selected': {
                    backgroundColor: '#0a2540',
                    color: '#ffffff',
                },
            },
        },
    };

    if (isLoading || !clientSecret) {
        return (
            <div className="min-h-[60vh] flex items-center justify-center">
                <div className="text-center">
                    <LoadingSpinner size="lg" />
                    <p className="mt-4 text-sm text-gray-500">
                        {isLoading ? 'ang ti thng tin n hng...' : 'ang khi to thanh ton...'}
                    </p>
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
                    <h1 className="text-2xl font-semibold text-gray-900 mt-4">Thanh ton</h1>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-5 gap-12">
                    {/* Payment Form  Left, larger */}
                    <div className="lg:col-span-3 order-2 lg:order-1">
                        <div className="bg-white rounded-lg border border-gray-200 p-6">
                            <Elements stripe={stripePromise} options={options}>
                                <StripeForm order={order} clientSecret={clientSecret} />
                            </Elements>
                        </div>
                    </div>

                    {/* Order Summary  Right, narrower */}
                    <div className="lg:col-span-2 order-1 lg:order-2">
                        <div className="bg-white rounded-lg border border-gray-200 p-6 lg:sticky lg:top-8">
                            <h2 className="text-base font-semibold text-gray-900 mb-4">n hng #{order?.orderNumber}</h2>

                            {/* Items */}
                            {order?.items && order.items.length > 0 && (
                                <div className="space-y-3 mb-4">
                                    {order.items.map((item: any, index: number) => (
                                        <div key={index} className="flex justify-between items-start gap-3">
                                            <div className="flex-1 min-w-0">
                                                <p className="text-sm text-gray-900 truncate">{item.productName || item.name}</p>
                                                <p className="text-xs text-gray-500">SL: {item.quantity}</p>
                                            </div>
                                            <p className="text-sm text-gray-900 whitespace-nowrap">
                                                {formatCurrencyPrice((item.unitPrice || item.price) * item.quantity, selectedCurrency)}
                                            </p>
                                        </div>
                                    ))}
                                </div>
                            )}

                            <Separator className="my-4" />

                            {/* Totals */}
                            <div className="space-y-2">
                                <div className="flex justify-between text-sm">
                                    <span className="text-gray-500">Tm tnh</span>
                                    <span className="text-gray-900">{formatCurrencyPrice((order?.totalAmount || 0) - (order?.shippingFee || 0), selectedCurrency)}</span>
                                </div>
                                <div className="flex justify-between text-sm">
                                    <span className="text-gray-500">Ph vn chuyn</span>
                                    <span className="text-gray-900">{formatCurrencyPrice(order?.shippingFee || 0, selectedCurrency)}</span>
                                </div>
                            </div>

                            <Separator className="my-4" />

                            <div className="flex justify-between">
                                <span className="text-base font-semibold text-gray-900">Tng cng</span>
                                <span className="text-base font-semibold text-gray-900">{formatCurrencyPrice(order?.totalAmount || 0, selectedCurrency)}</span>
                            </div>
                        </div>
                    </div>
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

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();

        if (!stripe || !elements || !order) {
            setError('Stripe cha sn sng. Vui lng th li.');
            return;
        }

        setIsProcessing(true);
        setError('');

        try {
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
        } catch (err) {
            setError('C li xy ra. Vui lng th li.');
            setIsProcessing(false);
        }
    };

    return (
        <form onSubmit={handleSubmit} className="space-y-6">
            {/* Stripe Payment Element */}
            <PaymentElement
                options={{
                    layout: 'tabs',
                    fields: {
                        billingDetails: {
                            name: 'auto',
                            email: 'auto',
                        }
                    }
                }}
                onChange={(event) => {
                    setIsComplete(event.complete);
                    if (event.complete) setError('');
                }}
            />

            {/* Error */}
            {error && (
                <div className="p-3 bg-red-50 border border-red-100 rounded-md">
                    <p className="text-sm text-red-600">{error}</p>
                </div>
            )}

            {/* Submit */}
            <Button
                type="submit"
                disabled={!stripe || isProcessing || !isComplete}
                className="w-full h-11 text-base font-medium bg-[#0a2540] hover:bg-[#0a2540]/90"
            >
                {isProcessing ? (
                    <div className="flex items-center gap-2">
                        <LoadingSpinner size="sm" />
                        <span>ang x l...</span>
                    </div>
                ) : (
                    <span>Thanh ton {formatCurrencyPrice(order?.totalAmount || 0, 'VND')}</span>
                )}
            </Button>

            {/* Powered by Stripe */}
            <div className="flex items-center justify-center gap-1.5 text-xs text-gray-400">
                <Lock className="h-3 w-3" />
                <span>c bo mt bi Stripe</span>
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