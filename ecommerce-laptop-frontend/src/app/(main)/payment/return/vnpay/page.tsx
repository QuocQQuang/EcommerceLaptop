'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { CheckCircle, XCircle } from 'lucide-react';
import Link from 'next/link';
import { useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { toast } from 'sonner';
import logger from '@/lib/logger';

interface PaymentResult {
    success: boolean;
    orderId?: string;
    transactionId?: string;
    amount?: string;
    message: string;
    responseCode?: string;
}

function VNPayReturnContent() {
    const searchParams = useSearchParams();
    const [result, setResult] = useState<PaymentResult | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        const processReturn = async () => {
            logger.info(' VNPay Return: Starting payment return processing', {
                pathname: window.location.pathname,
                search: window.location.search,
                hasSearchParams: searchParams ? !!searchParams.toString() : false
            });

            try {
                // Get URL parameters from VNPay return
                const vnpResponseCode = searchParams?.get('vnp_ResponseCode');
                const vnpTxnRef = searchParams?.get('vnp_TxnRef'); // Order ID
                const vnpTransactionNo = searchParams?.get('vnp_TransactionNo');
                const vnpAmount = searchParams?.get('vnp_Amount');
                const vnpOrderInfo = searchParams?.get('vnp_OrderInfo');
                const vnpTransactionStatus = searchParams?.get('vnp_TransactionStatus');

                logger.info(' VNPay Return: Received payment parameters', {
                    vnpResponseCode,
                    vnpTxnRef,
                    vnpTransactionNo,
                    vnpAmount,
                    vnpOrderInfo,
                    vnpTransactionStatus,
                    allParams: searchParams ? Object.fromEntries(searchParams.entries()) : {}
                });

                if (!vnpResponseCode) {
                    logger.error(' VNPay Return: Missing response code from VNPay');
                    setResult({
                        success: false,
                        message: 'Thiu thng tin phn hi t VNPay'
                    });
                    setIsLoading(false);
                    return;
                }

                // VNPay response codes
                const isSuccess = vnpResponseCode === '00' && vnpTransactionStatus === '00';

                logger.info(' VNPay Return: Payment result determined', {
                    isSuccess,
                    vnpResponseCode,
                    vnpTransactionStatus,
                    criteria: 'Response code 00 AND Transaction status 00'
                });

                if (isSuccess) {
                    const formattedAmount = vnpAmount ? (parseInt(vnpAmount) / 100).toLocaleString('vi-VN') + ' VN' : 'N/A';
                    
                    logger.info(' VNPay Return: Payment SUCCESS detected', {
                        orderId: vnpTxnRef,
                        transactionId: vnpTransactionNo,
                        originalAmount: vnpAmount,
                        formattedAmount,
                        orderInfo: vnpOrderInfo
                    });

                    setResult({
                        success: true,
                        orderId: vnpTxnRef || 'N/A',
                        transactionId: vnpTransactionNo || 'N/A',
                        amount: formattedAmount,
                        message: 'Thanh ton VNPay thnh cng!',
                        responseCode: vnpResponseCode
                    });
                    
                    toast.success('Thanh ton thnh cng!');
                    logger.info(' VNPay Return: Success toast displayed');
                } else {
                    // Map VNPay error codes to user-friendly messages
                    let errorMessage = 'Thanh ton VNPay tht bi';
                    
                    logger.warn(' VNPay Return: Payment FAILED detected', {
                        vnpResponseCode,
                        vnpTransactionStatus,
                        orderId: vnpTxnRef
                    });

                    switch (vnpResponseCode) {
                        case '07':
                            errorMessage = 'Tr tin thnh cng. Giao dch b nghi ng (lin quan ti la o, giao dch bt thng).';
                            logger.warn(' VNPay: Suspicious transaction detected', { code: '07', orderId: vnpTxnRef });
                            break;
                        case '09':
                            errorMessage = 'Giao dch khng thnh cng do: Th/Ti khon ca khch hng cha ng k dch v InternetBanking ti ngn hng.';
                            logger.info(' VNPay: InternetBanking not registered', { code: '09' });
                            break;
                        case '10':
                            errorMessage = 'Giao dch khng thnh cng do: Khch hng xc thc thng tin th/ti khon khng ng qu 3 ln';
                            logger.warn(' VNPay: Authentication failed multiple times', { code: '10' });
                            break;
                        case '11':
                            errorMessage = 'Giao dch khng thnh cng do:  ht hn ch thanh ton. Xin qu khch vui lng thc hin li giao dch.';
                            logger.info(' VNPay: Payment timeout', { code: '11' });
                            break;
                        case '12':
                            errorMessage = 'Giao dch khng thnh cng do: Th/Ti khon ca khch hng b kha.';
                            logger.warn(' VNPay: Account locked', { code: '12' });
                            break;
                        case '13':
                            errorMessage = 'Giao dch khng thnh cng do Qu khch nhp sai mt khu xc thc giao dch (OTP).';
                            logger.info(' VNPay: Incorrect OTP', { code: '13' });
                            break;
                        case '24':
                            errorMessage = 'Giao dch khng thnh cng do: Khch hng hy giao dch';
                            logger.info(' VNPay: Transaction cancelled by user', { code: '24' });
                            break;
                        case '51':
                            errorMessage = 'Giao dch khng thnh cng do: Ti khon ca qu khch khng  s d  thc hin giao dch.';
                            logger.info(' VNPay: Insufficient balance', { code: '51' });
                            break;
                        case '65':
                            errorMessage = 'Giao dch khng thnh cng do: Ti khon ca Qu khch  vt qu hn mc giao dch trong ngy.';
                            logger.info(' VNPay: Daily transaction limit exceeded', { code: '65' });
                            break;
                        case '75':
                            errorMessage = 'Ngn hng thanh ton ang bo tr.';
                            logger.warn(' VNPay: Bank maintenance', { code: '75' });
                            break;
                        case '79':
                            errorMessage = 'Giao dch khng thnh cng do: KH nhp sai mt khu thanh ton qu s ln quy nh.';
                            logger.warn(' VNPay: Password failed multiple times', { code: '79' });
                            break;
                        default:
                            errorMessage = `Thanh ton tht bi. M li: ${vnpResponseCode}`;
                            logger.error(' VNPay: Unknown error code', { code: vnpResponseCode });
                    }

                    setResult({
                        success: false,
                        orderId: vnpTxnRef || 'N/A',
                        message: errorMessage,
                        responseCode: vnpResponseCode
                    });
                    
                    toast.error('Thanh ton tht bi!');
                    logger.error(' VNPay Return: Error toast displayed', { errorMessage });
                }
            } catch (error) {
                logger.error(' VNPay Return: Exception during processing', {
                    error: error instanceof Error ? error.message : 'Unknown error',
                    stack: error instanceof Error ? error.stack : undefined
                });
                console.error('Error processing VNPay return:', error);
                setResult({
                    success: false,
                    message: 'C li xy ra khi x l kt qu thanh ton'
                });
                toast.error('C li xy ra!');
            } finally {
                setIsLoading(false);
                logger.info(' VNPay Return: Processing completed', {
                    isLoading: false,
                    hasResult: !!result
                });
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
                            {result.transactionId && result.success && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">M giao dch:</span>
                                    <span className="font-medium">{result.transactionId}</span>
                                </div>
                            )}
                            {result.amount && result.success && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">S tin:</span>
                                    <span className="font-medium">{result.amount}</span>
                                </div>
                            )}
                            {result.responseCode && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">M phn hi:</span>
                                    <span className="font-medium">{result.responseCode}</span>
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

export default function VNPayReturnPage() {
    return (
        <Suspense fallback={
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <LoadingSpinner size="lg" />
            </div>
        }>
            <VNPayReturnContent />
        </Suspense>
    );
}