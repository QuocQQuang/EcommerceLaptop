'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { CheckCircle, Clock, XCircle } from 'lucide-react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { toast } from 'sonner';

interface PaymentResult {
    success: boolean;
    orderId?: string;
    transactionId?: string;
    amount?: string;
    message: string;
    status?: string;
}

function SePayReturnContent() {
    const searchParams = useSearchParams();
    const [result, setResult] = useState<PaymentResult | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const processReturn = async () => {
            try {
                // Get URL parameters from SePay return
                const orderId = searchParams?.get('orderId') || searchParams?.get('order_id');
                const status = searchParams?.get('status');
                const transactionId = searchParams?.get('transaction_id') || searchParams?.get('ref_code');
                const amount = searchParams?.get('amount');
                const message = searchParams?.get('message');

                if (!orderId) {
                    setResult({
                        success: false,
                        message: 'Thiu thng tin n hng t SePay'
                    });
                    setIsLoading(false);
                    return;
                }

                // SePay status handling
                if (status === 'success' || status === 'completed') {
                    setResult({
                        success: true,
                        orderId: orderId,
                        transactionId: transactionId || 'N/A',
                        amount: amount ? parseInt(amount).toLocaleString('vi-VN') + ' VN' : 'N/A',
                        message: message || 'Thanh ton SePay thnh cng!',
                        status: status
                    });
                    toast.success('Thanh ton thnh cng!');
                } else if (status === 'pending' || status === 'processing') {
                    setResult({
                        success: false,
                        orderId: orderId,
                        transactionId: transactionId || 'N/A',
                        message: 'Giao dch ang c x l. Vui lng kim tra li sau.',
                        status: status
                    });
                    toast.warning('Giao dch ang c x l');
                } else if (status === 'failed' || status === 'error') {
                    setResult({
                        success: false,
                        orderId: orderId,
                        message: message || 'Thanh ton SePay tht bi',
                        status: status
                    });
                    toast.error('Thanh ton tht bi!');
                } else if (status === 'cancelled') {
                    setResult({
                        success: false,
                        orderId: orderId,
                        message: 'Giao dch  b hy',
                        status: status
                    });
                    toast.warning('Giao dch  b hy');
                } else {
                    // Default handling for unclear status
                    setResult({
                        success: false,
                        orderId: orderId,
                        message: 'Trng thi giao dch khng xc nh. Vui lng lin h h tr.',
                        status: status || 'unknown'
                    });
                    toast.warning('Trng thi giao dch khng r rng');
                }
            } catch (error) {
                console.error('Error processing SePay return:', error);
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

    const getStatusIcon = () => {
        if (result?.success) {
            return <CheckCircle className="h-16 w-16 text-green-500" />;
        } else if (result?.status === 'pending' || result?.status === 'processing') {
            return <Clock className="h-16 w-16 text-yellow-500" />;
        } else {
            return <XCircle className="h-16 w-16 text-red-500" />;
        }
    };

    const getStatusColor = () => {
        if (result?.success) {
            return 'text-green-700';
        } else if (result?.status === 'pending' || result?.status === 'processing') {
            return 'text-yellow-700';
        } else {
            return 'text-red-700';
        }
    };

    const getTitle = () => {
        if (result?.success) {
            return 'Thanh ton thnh cng!';
        } else if (result?.status === 'pending' || result?.status === 'processing') {
            return 'ang x l thanh ton';
        } else if (result?.status === 'cancelled') {
            return 'Thanh ton  b hy';
        } else {
            return 'Thanh ton tht bi!';
        }
    };

    return (
        <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
            <Card className="w-full max-w-md">
                <CardHeader className="text-center">
                    <div className="mx-auto mb-4">
                        {getStatusIcon()}
                    </div>
                    <CardTitle className={getStatusColor()}>
                        {getTitle()}
                    </CardTitle>
                    <CardDescription>
                        {result?.message}
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    {result && (
                        <div className="space-y-2 text-sm">
                            {result.orderId && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">M n hng:</span>
                                    <span className="font-medium">#{result.orderId}</span>
                                </div>
                            )}
                            {result.transactionId && result.transactionId !== 'N/A' && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">M giao dch:</span>
                                    <span className="font-medium">{result.transactionId}</span>
                                </div>
                            )}
                            {result.amount && result.amount !== 'N/A' && result.success && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">S tin:</span>
                                    <span className="font-medium">{result.amount}</span>
                                </div>
                            )}
                            {result.status && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">Trng thi:</span>
                                    <span className="font-medium capitalize">{result.status}</span>
                                </div>
                            )}
                        </div>
                    )}

                    {/* Thng tin thm cho trng thi pending */}
                    {result?.status === 'pending' || result?.status === 'processing' ? (
                        <div className="bg-yellow-50 border border-yellow-200 rounded-lg p-4">
                            <h4 className="font-medium text-yellow-800 mb-2">Lu  quan trng:</h4>
                            <ul className="text-sm text-yellow-700 space-y-1">
                                <li> Giao dch ang c x l</li>
                                <li> Vui lng kim tra li sau 5-10 pht</li>
                                <li> n hng s c cp nht t ng</li>
                            </ul>
                        </div>
                    ) : null}

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
                        ) : result?.status === 'pending' || result?.status === 'processing' ? (
                            <>
                                <Button asChild className="w-full">
                                    <Link href="/account/orders">
                                        Kim tra n hng
                                    </Link>
                                </Button>
                                <Button variant="outline" asChild className="w-full">
                                    <Link href="/">
                                        V trang ch
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

export default function SePayReturnPage() {
    return (
        <Suspense fallback={
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <LoadingSpinner size="lg" />
            </div>
        }>
            <SePayReturnContent />
        </Suspense>
    );
}