'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { ArrowLeft, RefreshCw, XCircle } from 'lucide-react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';

interface CancelInfo {
    orderId?: string;
    paymentMethod?: string;
    reason?: string;
}

function PaymentCancelContent() {
    const searchParams = useSearchParams();
    const [cancelInfo, setCancelInfo] = useState<CancelInfo>({});
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const processCancelInfo = () => {
            // Get information from URL parameters
            const orderId = searchParams?.get('orderId') || searchParams?.get('vnp_TxnRef') || searchParams?.get('order_id');
            const paymentMethod = searchParams?.get('paymentMethod') || searchParams?.get('gateway');
            const reason = searchParams?.get('reason');

            setCancelInfo({
                orderId: orderId || undefined,
                paymentMethod: paymentMethod || undefined,
                reason: reason || undefined
            });

            setIsLoading(false);
        };

        processCancelInfo();
    }, [searchParams]);

    if (isLoading) {
        return (
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <Card className="w-full max-w-md">
                    <CardContent className="p-8 text-center">
                        <LoadingSpinner size="lg" />
                        <p className="mt-4 text-muted-foreground">ang x l thng tin...</p>
                    </CardContent>
                </Card>
            </div>
        );
    }

    const getPaymentMethodName = (method?: string) => {
        if (!method) return 'Khng xc nh';

        switch (method.toLowerCase()) {
            case 'paypal':
                return 'PayPal';
            case 'stripe':
                return 'Stripe';
            case 'vnpay':
                return 'VNPay';
            case 'sepay':
                return 'SePay';
            case 'momo':
                return 'MoMo';
            case 'zalopay':
                return 'ZaloPay';
            default:
                return method;
        }
    };

    return (
        <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
            <Card className="w-full max-w-md">
                <CardHeader className="text-center">
                    <div className="mx-auto mb-4">
                        <XCircle className="h-16 w-16 text-orange-500" />
                    </div>
                    <CardTitle className="text-orange-700">
                        Thanh ton  b hy
                    </CardTitle>
                    <CardDescription>
                        {cancelInfo.reason
                            ? `L do: ${cancelInfo.reason}`
                            : 'Bn  hy qu trnh thanh ton. n hng ca bn cha c x l.'
                        }
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    {(cancelInfo.orderId || cancelInfo.paymentMethod) && (
                        <div className="space-y-2 text-sm">
                            {cancelInfo.orderId && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">M n hng:</span>
                                    <span className="font-medium">#{cancelInfo.orderId}</span>
                                </div>
                            )}
                            {cancelInfo.paymentMethod && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">Phng thc:</span>
                                    <span className="font-medium">{getPaymentMethodName(cancelInfo.paymentMethod)}</span>
                                </div>
                            )}
                        </div>
                    )}

                    <div className="bg-orange-50 border border-orange-200 rounded-lg p-4">
                        <h4 className="font-medium text-orange-800 mb-2">iu g xy ra tip theo?</h4>
                        <ul className="text-sm text-orange-700 space-y-1">
                            <li> n hng cha c to</li>
                            <li> Khng c khon tin no b tr</li>
                            <li> Sn phm vn c trong gi hng</li>
                        </ul>
                    </div>

                    <div className="flex flex-col gap-2">
                        <Button asChild className="w-full">
                            <Link href="/checkout">
                                <RefreshCw className="w-4 h-4 mr-2" />
                                Th li thanh ton
                            </Link>
                        </Button>
                        <Button variant="outline" asChild className="w-full">
                            <Link href="/cart">
                                <ArrowLeft className="w-4 h-4 mr-2" />
                                Quay li gi hng
                            </Link>
                        </Button>
                        <Button variant="ghost" asChild className="w-full">
                            <Link href="/">
                                Tip tc mua sm
                            </Link>
                        </Button>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}

export default function PaymentCancelPage() {
    return (
        <Suspense fallback={
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <LoadingSpinner size="lg" />
            </div>
        }>
            <PaymentCancelContent />
        </Suspense>
    );
}