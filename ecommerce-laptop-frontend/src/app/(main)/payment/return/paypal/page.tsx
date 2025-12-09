'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { CheckCircle, XCircle } from 'lucide-react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { toast } from 'sonner';

interface PaymentResult {
    success: boolean;
    orderId?: string;
    paymentId?: string;
    message: string;
}

function PayPalReturnContent() {
    const searchParams = useSearchParams();
    const [result, setResult] = useState<PaymentResult | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const processReturn = async () => {
            try {
                // Get URL parameters from PayPal return
                const paymentId = searchParams?.get('paymentId');
                const token = searchParams?.get('token');
                const PayerID = searchParams?.get('PayerID');
                const orderId = searchParams?.get('orderId');

                if (!paymentId && !token) {
                    setResult({
                        success: false,
                        message: 'Thiu thng tin thanh ton t PayPal'
                    });
                    setIsLoading(false);
                    return;
                }

                // Check if payment was successful
                if (paymentId || (token && PayerID)) {
                    setResult({
                        success: true,
                        orderId: orderId || 'N/A',
                        paymentId: paymentId || token || 'N/A',
                        message: 'Thanh ton PayPal thnh cng!'
                    });
                    toast.success('Thanh ton thnh cng!');
                } else {
                    setResult({
                        success: false,
                        message: 'Thanh ton PayPal khng thnh cng'
                    });
                    toast.error('Thanh ton tht bi!');
                }
            } catch (error) {
                console.error('Error processing PayPal return:', error);
                setResult({
                    success: false,
                    message: 'C li xy ra khi x l kt qu thanh ton'
                });
                toast.error('C li xy ra!');
            } finally {
                setIsLoading(false);
            }
        };

        processReturn();
    }, [searchParams]);

    if (isLoading) {
        return (
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <Card className="w-full max-w-md">
                    <CardContent className="p-8 text-center">
                        <LoadingSpinner size="lg" />
                        <p className="mt-4 text-muted-foreground">ang x l kt qu thanh ton...</p>
                    </CardContent>
                </Card>
            </div>
        );
    }

    return (
        <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
            <Card className="w-full max-w-md">
                <CardHeader className="text-center">
                    <div className="mx-auto mb-4">
                        {result?.success ? (
                            <CheckCircle className="h-16 w-16 text-green-500" />
                        ) : (
                            <XCircle className="h-16 w-16 text-red-500" />
                        )}
                    </div>
                    <CardTitle className={result?.success ? 'text-green-700' : 'text-red-700'}>
                        {result?.success ? 'Thanh ton thnh cng!' : 'Thanh ton tht bi!'}
                    </CardTitle>
                    <CardDescription>
                        {result?.message}
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    {result?.success && (
                        <div className="space-y-2 text-sm">
                            {result.orderId && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">M n hng:</span>
                                    <span className="font-medium">#{result.orderId}</span>
                                </div>
                            )}
                            {result.paymentId && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">M giao dch:</span>
                                    <span className="font-medium text-xs">{result.paymentId}</span>
                                </div>
                            )}
                        </div>
                    )}

                    <div className="flex flex-col gap-2">
                        {result?.success ? (
                            <>
                                <Button asChild className="w-full">
                                    <Link href="/account/orders">
                                        Xem n hng
                                    </Link>
                                </Button>
                                <Button variant="outline" asChild className="w-full">
                                    <Link href="/">
                                        Tip tc mua sm
                                    </Link>
                                </Button>
                            </>
                        ) : (
                            <>
                                <Button asChild className="w-full">
                                    <Link href="/checkout">
                                        Th li thanh ton
                                    </Link>
                                </Button>
                                <Button variant="outline" asChild className="w-full">
                                    <Link href="/cart">
                                        Quay li gi hng
                                    </Link>
                                </Button>
                            </>
                        )}
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}

export default function PayPalReturnPage() {
    return (
        <Suspense fallback={
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <LoadingSpinner size="lg" />
            </div>
        }>
            <PayPalReturnContent />
        </Suspense>
    );
}