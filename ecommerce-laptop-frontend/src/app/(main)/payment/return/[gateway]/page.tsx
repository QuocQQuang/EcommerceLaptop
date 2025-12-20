'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useAuth } from '@/hooks/useAuth';
import { usePaymentSession } from '@/hooks/usePaymentSession';
import logger from '@/lib/logger';
import { orderService } from '@/services/orderService';
import { CheckCircle, XCircle } from 'lucide-react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { toast } from 'sonner';

// Gateway types
type GatewayType = 'vnpay' | 'momo' | 'zalopay' | 'stripe' | 'paypal' | 'sepay';

interface PaymentResult {
    success: boolean;
    orderId?: string;
    transactionId?: string;
    amount?: string;
    message: string;
    responseCode?: string;
}

// VNPay Error Code Mappings
const VNPAY_ERROR_CODES: Record<string, string> = {
    '07': 'Tr tin thnh cng. Giao dch b nghi ng (lin quan ti la o, giao dch bt thng).',
    '09': 'Th/Ti khon cha ng k dch v InternetBanking ti ngn hng.',
    '10': 'Xc thc thng tin th/ti khon khng ng qu 3 ln.',
    '11': ' ht hn ch thanh ton.',
    '12': 'Th/Ti khon b kha.',
    '13': 'Nhp sai mt khu xc thc giao dch (OTP).',
    '24': 'Khch hng hy giao dch.',
    '51': 'Ti khon khng  s d.',
    '65': 'Ti khon  vt qu hn mc giao dch trong ngy.',
    '75': 'Ngn hng thanh ton ang bo tr.',
    '79': 'Nhp sai mt khu thanh ton qu s ln quy nh.',
};

// MoMo Error Code Mappings
const MOMO_ERROR_CODES: Record<string, string> = {
    '9000': 'Giao dch c khi to, ch ngi dng xc nhn thanh ton.',
    '8000': 'Giao dch ang c x l.',
    '7000': 'Giao dch b t chi bi ngi dng.',
    '6000': 'Giao dch b t chi do vt qu s ln nhp sai mt khu.',
    '5000': 'Giao dch b t chi do OTP sai.',
    '4000': 'Giao dch b t chi do qu hn thanh ton.',
    '3000': 'Giao dch b hy.',
    '2000': 'Giao dch tht bi do li nghip v.',
    '1000': 'Giao dch tht bi do li k thut.',
    '11': 'Truy cp b t chi.',
    '49': 'Checksum failed.',
};

// ZaloPay Error Code Mappings
const ZALOPAY_ERROR_CODES: Record<string, string> = {
    '-1': 'Giao dch tht bi.',
    '2': 'Giao dch b t chi.',
};

// Gateway handler functions
function processVNPayReturn(searchParams: URLSearchParams): PaymentResult {
    const vnpResponseCode = searchParams.get('vnp_ResponseCode');
    const vnpTxnRef = searchParams.get('vnp_TxnRef');
    const vnpTransactionNo = searchParams.get('vnp_TransactionNo');
    const vnpAmount = searchParams.get('vnp_Amount');
    const vnpTransactionStatus = searchParams.get('vnp_TransactionStatus');

    if (!vnpResponseCode) {
        return { success: false, message: 'Thiu thng tin phn hi t VNPay' };
    }

    const isSuccess = vnpResponseCode === '00' && vnpTransactionStatus === '00';

    if (isSuccess) {
        const formattedAmount = vnpAmount ? (parseInt(vnpAmount) / 100).toLocaleString('vi-VN') + ' VN' : 'N/A';
        return {
            success: true,
            orderId: vnpTxnRef || 'N/A',
            transactionId: vnpTransactionNo || 'N/A',
            amount: formattedAmount,
            message: 'Thanh ton VNPay thnh cng!',
            responseCode: vnpResponseCode
        };
    }

    const errorMessage = VNPAY_ERROR_CODES[vnpResponseCode] || `Thanh ton tht bi. M li: ${vnpResponseCode}`;
    return {
        success: false,
        orderId: vnpTxnRef || 'N/A',
        message: errorMessage,
        responseCode: vnpResponseCode
    };
}

function processMoMoReturn(searchParams: URLSearchParams): PaymentResult {
    const resultCode = searchParams.get('resultCode');
    const orderId = searchParams.get('orderId');
    const transId = searchParams.get('transId');
    const amount = searchParams.get('amount');
    const localMessage = searchParams.get('localMessage');

    if (!resultCode) {
        return { success: false, message: 'Thiu thng tin phn hi t MoMo' };
    }

    const isSuccess = resultCode === '0';

    if (isSuccess) {
        return {
            success: true,
            orderId: orderId || 'N/A',
            transactionId: transId || 'N/A',
            amount: amount ? parseInt(amount).toLocaleString('vi-VN') + ' VN' : 'N/A',
            message: localMessage || 'Thanh ton MoMo thnh cng!',
            responseCode: resultCode
        };
    }

    const errorMessage = MOMO_ERROR_CODES[resultCode] || localMessage || `Thanh ton tht bi. M li: ${resultCode}`;
    return {
        success: false,
        orderId: orderId || 'N/A',
        message: errorMessage,
        responseCode: resultCode
    };
}

function processZaloPayReturn(searchParams: URLSearchParams): PaymentResult {
    const status = searchParams.get('status');
    const apptransid = searchParams.get('apptransid');
    const amount = searchParams.get('amount');

    if (!status) {
        return { success: false, message: 'Thiu thng tin phn hi t ZaloPay' };
    }

    const isSuccess = status === '1';

    if (isSuccess) {
        return {
            success: true,
            orderId: apptransid || 'N/A',
            amount: amount ? parseInt(amount).toLocaleString('vi-VN') + ' VN' : 'N/A',
            message: 'Thanh ton ZaloPay thnh cng!',
            responseCode: status
        };
    }

    const errorMessage = ZALOPAY_ERROR_CODES[status] || `Thanh ton tht bi. M li: ${status}`;
    return {
        success: false,
        orderId: apptransid || 'N/A',
        message: errorMessage,
        responseCode: status
    };
}

function processPayPalReturn(searchParams: URLSearchParams): PaymentResult {
    const paymentId = searchParams.get('paymentId');
    const token = searchParams.get('token');
    const PayerID = searchParams.get('PayerID');
    const orderId = searchParams.get('orderId');

    if (!paymentId && !token) {
        return { success: false, message: 'Thiu thng tin thanh ton t PayPal' };
    }

    if (paymentId || (token && PayerID)) {
        return {
            success: true,
            orderId: orderId || 'N/A',
            transactionId: paymentId || token || 'N/A',
            message: 'Thanh ton PayPal thnh cng!'
        };
    }

    return { success: false, message: 'Thanh ton PayPal khng thnh cng' };
}

function processSepayReturn(searchParams: URLSearchParams): PaymentResult {
    const status = searchParams.get('status');
    const orderId = searchParams.get('orderId');
    const transactionId = searchParams.get('transactionId');
    const amount = searchParams.get('amount');

    if (!status) {
        return { success: false, message: 'Thiu thng tin phn hi t SePay' };
    }

    const isSuccess = status === 'success' || status === '1';

    if (isSuccess) {
        return {
            success: true,
            orderId: orderId || 'N/A',
            transactionId: transactionId || 'N/A',
            amount: amount ? parseInt(amount).toLocaleString('vi-VN') + ' VN' : 'N/A',
            message: 'Thanh ton SePay thnh cng!'
        };
    }

    return {
        success: false,
        orderId: orderId || 'N/A',
        message: 'Thanh ton SePay tht bi'
    };
}

// Gateway handler registry
const gatewayHandlers: Record<GatewayType, (params: URLSearchParams) => PaymentResult> = {
    vnpay: processVNPayReturn,
    momo: processMoMoReturn,
    zalopay: processZaloPayReturn,
    paypal: processPayPalReturn,
    sepay: processSepayReturn,
    stripe: () => ({ success: false, message: 'Stripe uses redirect_status' }), // Handled specially
};

// Gateway display names
const gatewayNames: Record<GatewayType, string> = {
    vnpay: 'VNPay',
    momo: 'MoMo',
    zalopay: 'ZaloPay',
    paypal: 'PayPal',
    stripe: 'Stripe',
    sepay: 'SePay',
};

function PaymentReturnContent({ gateway }: { gateway: GatewayType }) {
    const searchParams = useSearchParams();
    const router = useRouter();
    const [result, setResult] = useState<PaymentResult | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [isProcessing, setIsProcessing] = useState(false);
    const { isReady, isValid, userId, error: sessionError, hasOrderAccess } = usePaymentSession();

    useEffect(() => {
        const processReturn = async () => {
            if (isProcessing) return;
            if (!isReady) return;

            // For Stripe, we need session validation
            if (gateway === 'stripe') {
                if (!isValid && sessionError) {
                    setResult({ success: false, message: sessionError });
                    setIsLoading(false);
                    return;
                }
                if (!isValid) return;
            }

            setIsProcessing(true);

            logger.info(` ${gatewayNames[gateway]} Return: Starting payment return processing`, {
                gateway,
                pathname: typeof window !== 'undefined' ? window.location.pathname : '',
                hasSearchParams: !!searchParams
            });

            try {
                const params = searchParams ? new URLSearchParams(searchParams.toString()) : new URLSearchParams();

                // Special handling for Stripe
                if (gateway === 'stripe') {
                    const redirectStatus = params.get('redirect_status');
                    const paymentIntent = params.get('payment_intent');
                    const orderIdStr = params.get('orderId');
                    const orderId = parseInt(orderIdStr || '0');

                    if (!paymentIntent || orderId <= 0) {
                        setResult({ success: false, message: 'Thiu thng tin thanh ton hoc n hng' });
                        setIsLoading(false);
                        return;
                    }

                    if (redirectStatus === 'succeeded') {
                        const orderRes = await orderService.getOrderStatus(orderId);
                        const orderUserId = orderRes.customerId;

                        if (!orderUserId || !hasOrderAccess(orderUserId)) {
                            toast.error('Bn khng c quyn truy cp n hng ny.');
                            router.push('/account/orders');
                            return;
                        }

                        setResult({
                            success: true,
                            orderId: orderIdStr || 'N/A',
                            transactionId: paymentIntent,
                            message: 'Thanh ton Stripe thnh cng!'
                        });
                        toast.success('Thanh ton thnh cng!');
                    } else {
                        setResult({
                            success: false,
                            message: redirectStatus === 'failed' ? 'Thanh ton Stripe tht bi' : 'Trng thi thanh ton khng xc nh'
                        });
                        toast.error('Thanh ton tht bi!');
                    }
                } else {
                    // Use gateway handler for other gateways
                    const handler = gatewayHandlers[gateway];
                    const paymentResult = handler(params);
                    setResult(paymentResult);

                    if (paymentResult.success) {
                        toast.success('Thanh ton thnh cng!');
                    } else {
                        toast.error('Thanh ton tht bi!');
                    }
                }
            } catch (error) {
                logger.error(` ${gatewayNames[gateway]} Return: Exception during processing`, {
                    error: error instanceof Error ? error.message : 'Unknown error'
                });
                setResult({ success: false, message: 'C li xy ra khi x l kt qu thanh ton' });
                toast.error('C li xy ra!');
            } finally {
                setIsLoading(false);
            }
        };

        processReturn();
    }, [searchParams, isReady, isValid, isProcessing, gateway, hasOrderAccess, router, sessionError, userId]);

    if (isLoading || (gateway === 'stripe' && (!isReady || (isReady && !isValid && !sessionError)))) {
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
                    <CardDescription>{result?.message}</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    {result && (
                        <div className="space-y-2 text-sm">
                            {result.orderId && result.orderId !== 'N/A' && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">M n hng:</span>
                                    <span className="font-medium">#{result.orderId}</span>
                                </div>
                            )}
                            {result.transactionId && result.transactionId !== 'N/A' && result.success && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">M giao dch:</span>
                                    <span className="font-medium text-xs">{result.transactionId}</span>
                                </div>
                            )}
                            {result.amount && result.amount !== 'N/A' && result.success && (
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
                                    <Link href="/account/orders">Xem n hng</Link>
                                </Button>
                                <Button variant="outline" asChild className="w-full">
                                    <Link href="/">Tip tc mua sm</Link>
                                </Button>
                            </>
                        ) : (
                            <>
                                <Button asChild className="w-full">
                                    <Link href="/checkout">Th li thanh ton</Link>
                                </Button>
                                <Button variant="outline" asChild className="w-full">
                                    <Link href="/cart">Quay li gi hng</Link>
                                </Button>
                            </>
                        )}
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}

// Validate gateway param
const validGateways: GatewayType[] = ['vnpay', 'momo', 'zalopay', 'stripe', 'paypal', 'sepay'];

export default function PaymentReturnPage({ params }: { params: { gateway: string } }) {
    const gateway = params.gateway.toLowerCase() as GatewayType;

    if (!validGateways.includes(gateway)) {
        return (
            <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
                <Card className="w-full max-w-md">
                    <CardHeader className="text-center">
                        <XCircle className="h-16 w-16 text-red-500 mx-auto mb-4" />
                        <CardTitle className="text-red-700">Cng thanh ton khng hp l</CardTitle>
                        <CardDescription>
                            Cng thanh ton &quot;{params.gateway}&quot; khng c h tr.
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <Button asChild className="w-full">
                            <Link href="/">Quay v trang ch</Link>
                        </Button>
                    </CardContent>
                </Card>
            </div>
        );
    }

    return (
        <Suspense fallback={
            <div className="min-h-screen bg-gray-50 flex items-center justify-center">
                <LoadingSpinner size="lg" />
            </div>
        }>
            <PaymentReturnContent gateway={gateway} />
        </Suspense>
    );
}
