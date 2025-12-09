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
    transactionId?: string;
    amount?: string;
    message: string;
    resultCode?: string;
}

function MoMoReturnContent() {
    const searchParams = useSearchParams();
    const [result, setResult] = useState<PaymentResult | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const processReturn = async () => {
            try {
                // Get URL parameters from MoMo return
                const resultCode = searchParams?.get('resultCode');
                const orderId = searchParams?.get('orderId');
                const transId = searchParams?.get('transId');
                const amount = searchParams?.get('amount');
                const message = searchParams?.get('message');
                const localMessage = searchParams?.get('localMessage');

                if (!resultCode) {
                    setResult({
                        success: false,
                        message: 'Thiu thng tin phn hi t MoMo'
                    });
                    setIsLoading(false);
                    return;
                }

                // MoMo result codes
                const isSuccess = resultCode === '0';

                if (isSuccess) {
                    setResult({
                        success: true,
                        orderId: orderId || 'N/A',
                        transactionId: transId || 'N/A',
                        amount: amount ? parseInt(amount).toLocaleString('vi-VN') + ' VN' : 'N/A',
                        message: localMessage || message || 'Thanh ton MoMo thnh cng!',
                        resultCode: resultCode
                    });
                    toast.success('Thanh ton thnh cng!');
                } else {
                    // Map MoMo error codes to user-friendly messages
                    let errorMessage = localMessage || message || 'Thanh ton MoMo tht bi';

                    switch (resultCode) {
                        case '9000':
                            errorMessage = 'Giao dch c khi to, ch ngi dng xc nhn thanh ton';
                            break;
                        case '8000':
                            errorMessage = 'Giao dch ang c x l';
                            break;
                        case '7000':
                            errorMessage = 'Giao dch b t chi bi ngi dng';
                            break;
                        case '6000':
                            errorMessage = 'Giao dch b t chi bi ngi dng do vt qu s ln nhp sai mt khu';
                            break;
                        case '5000':
                            errorMessage = 'Giao dch b t chi bi ngi dng do OTP sai';
                            break;
                        case '4000':
                            errorMessage = 'Giao dch b t chi do qu hn thanh ton';
                            break;
                        case '3000':
                            errorMessage = 'Giao dch b hy';
                            break;
                        case '2000':
                            errorMessage = 'Giao dch tht bi do li nghip v';
                            break;
                        case '1000':
                            errorMessage = 'Giao dch tht bi do li k thut';
                            break;
                        case '11':
                            errorMessage = 'Truy cp b t chi';
                            break;
                        case '12':
                            errorMessage = 'Phin bn api khng c h tr cho yu cu ny';
                            break;
                        case '13':
                            errorMessage = 'Merchant authentication failed';
                            break;
                        case '20':
                            errorMessage = 'Yu cu sai nh dng';
                            break;
                        case '21':
                            errorMessage = 'S tin khng hp l';
                            break;
                        case '40':
                            errorMessage = 'RequestId b trng';
                            break;
                        case '41':
                            errorMessage = 'OrderId b trng';
                            break;
                        case '42':
                            errorMessage = 'OrderId khng hp l hoc khng tn ti';
                            break;
                        case '43':
                            errorMessage = 'Yu cu b t chi do thng tin th khng hp l';
                            break;
                        case '49':
                            errorMessage = 'Checksum failed';
                            break;
                        default:
                            errorMessage = `Thanh ton tht bi. M li: ${resultCode}`;
                    }

                    setResult({
                        success: false,
                        orderId: orderId || 'N/A',
                        message: errorMessage,
                        resultCode: resultCode
                    });
                    toast.error('Thanh ton tht bi!');
                }
            } catch (error) {
                console.error('Error processing MoMo return:', error);
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
                    {result && (
                        <div className="space-y-2 text-sm">
                            {result.orderId && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">M n hng:</span>
                                    <span className="font-medium">#{result.orderId}</span>
                                </div>
                            )}
                            {result.transactionId && result.transactionId !== 'N/A' && result.success && (
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
                            {result.resultCode && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">M kt qu:</span>
                                    <span className="font-medium">{result.resultCode}</span>
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

export default function MoMoReturnPage() {
    return (
        <Suspense fallback={
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <LoadingSpinner size="lg" />
            </div>
        }>
            <MoMoReturnContent />
        </Suspense>
    );
}