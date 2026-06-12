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
    '07': 'Trả tiền thành công. Giao dịch bị nghi ngờ (liên quan tới lừa đảo, giao dịch bất thường).',
    '09': 'Thẻ/Tài khoản chưa đăng ký dịch vụ InternetBanking tại ngân hàng.',
    '10': 'Xác thực thông tin thẻ/tài khoản không đúng quá 3 lần.',
    '11': 'Đã hết hạn chờ thanh toán.',
    '12': 'Thẻ/Tài khoản bị khóa.',
    '13': 'Nhập sai mật khẩu xác thực giao dịch (OTP).',
    '24': 'Khách hàng hủy giao dịch.',
    '51': 'Tài khoản không đủ số dư.',
    '65': 'Tài khoản đã vượt quá hạn mức giao dịch trong ngày.',
    '75': 'Ngân hàng thanh toán đang bảo trì.',
    '79': 'Nhập sai mật khẩu thanh toán quá số lần quy định.',
};

// MoMo Error Code Mappings
const MOMO_ERROR_CODES: Record<string, string> = {
    '9000': 'Giao dịch được khởi tạo, chờ người dùng xác nhận thanh toán.',
    '8000': 'Giao dịch đang được xử lý.',
    '7000': 'Giao dịch bị từ chối bởi người dùng.',
    '6000': 'Giao dịch bị từ chối do vượt quá số lần nhập sai mật khẩu.',
    '5000': 'Giao dịch bị từ chối do OTP sai.',
    '4000': 'Giao dịch bị từ chối do quá hạn thanh toán.',
    '3000': 'Giao dịch bị hủy.',
    '2000': 'Giao dịch thất bại do lỗi nghiệp vụ.',
    '1000': 'Giao dịch thất bại do lỗi kỹ thuật.',
    '11': 'Truy cập bị từ chối.',
    '49': 'Checksum failed.',
};

// ZaloPay Error Code Mappings
const ZALOPAY_ERROR_CODES: Record<string, string> = {
    '-1': 'Giao dịch thất bại.',
    '2': 'Giao dịch bị từ chối.',
};

// Gateway handler functions
function processVNPayReturn(searchParams: URLSearchParams): PaymentResult {
    const vnpResponseCode = searchParams.get('vnp_ResponseCode');
    const vnpTxnRef = searchParams.get('vnp_TxnRef');
    const vnpTransactionNo = searchParams.get('vnp_TransactionNo');
    const vnpAmount = searchParams.get('vnp_Amount');
    const vnpTransactionStatus = searchParams.get('vnp_TransactionStatus');

    if (!vnpResponseCode) {
        return { success: false, message: 'Thiếu thông tin phản hồi từ VNPay' };
    }

    const isSuccess = vnpResponseCode === '00' && vnpTransactionStatus === '00';

    if (isSuccess) {
        const formattedAmount = vnpAmount ? (parseInt(vnpAmount) / 100).toLocaleString('vi-VN') + ' VN' : 'N/A';
        return {
            success: true,
            orderId: vnpTxnRef || 'N/A',
            transactionId: vnpTransactionNo || 'N/A',
            amount: formattedAmount,
            message: 'Thanh toán VNPay thành công!',
            responseCode: vnpResponseCode
        };
    }

    const errorMessage = VNPAY_ERROR_CODES[vnpResponseCode] || `Thanh toán thất bại. Mã lỗi: ${vnpResponseCode}`;
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
        return { success: false, message: 'Thiếu thông tin phản hồi từ MoMo' };
    }

    const isSuccess = resultCode === '0';

    if (isSuccess) {
        return {
            success: true,
            orderId: orderId || 'N/A',
            transactionId: transId || 'N/A',
            amount: amount ? parseInt(amount).toLocaleString('vi-VN') + ' VN' : 'N/A',
            message: localMessage || 'Thanh toán MoMo thành công!',
            responseCode: resultCode
        };
    }

    const errorMessage = MOMO_ERROR_CODES[resultCode] || localMessage || `Thanh toán thất bại. Mã lỗi: ${resultCode}`;
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
        return { success: false, message: 'Thiếu thông tin phản hồi từ ZaloPay' };
    }

    const isSuccess = status === '1';

    if (isSuccess) {
        return {
            success: true,
            orderId: apptransid || 'N/A',
            amount: amount ? parseInt(amount).toLocaleString('vi-VN') + ' VN' : 'N/A',
            message: 'Thanh toán ZaloPay thành công!',
            responseCode: status
        };
    }

    const errorMessage = ZALOPAY_ERROR_CODES[status] || `Thanh toán thất bại. Mã lỗi: ${status}`;
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
        return { success: false, message: 'Thiếu thông tin thanh toán từ PayPal' };
    }

    if (paymentId || (token && PayerID)) {
        return {
            success: true,
            orderId: orderId || 'N/A',
            transactionId: paymentId || token || 'N/A',
            message: 'Thanh toán PayPal thành công!'
        };
    }

    return { success: false, message: 'Thanh toán PayPal không thành công' };
}

function processSepayReturn(searchParams: URLSearchParams): PaymentResult {
    const status = searchParams.get('status');
    const orderId = searchParams.get('orderId');
    const transactionId = searchParams.get('transactionId');
    const amount = searchParams.get('amount');

    if (!status) {
        return { success: false, message: 'Thiếu thông tin phản hồi từ SePay' };
    }

    const isSuccess = status === 'success' || status === '1';

    if (isSuccess) {
        return {
            success: true,
            orderId: orderId || 'N/A',
            transactionId: transactionId || 'N/A',
            amount: amount ? parseInt(amount).toLocaleString('vi-VN') + ' VN' : 'N/A',
            message: 'Thanh toán SePay thành công!'
        };
    }

    return {
        success: false,
        orderId: orderId || 'N/A',
        message: 'Thanh toán SePay thất bại'
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
                        setResult({ success: false, message: 'Thiếu thông tin thanh toán hoặc đơn hàng' });
                        setIsLoading(false);
                        return;
                    }

                    if (redirectStatus === 'succeeded') {
                        const orderRes = await orderService.getOrderStatus(orderId);
                        const orderUserId = orderRes.customerId;

                        if (!orderUserId || !hasOrderAccess(orderUserId)) {
                            toast.error('Bạn không có quyền truy cập đơn hàng này.');
                            router.push('/account/orders');
                            return;
                        }

                        setResult({
                            success: true,
                            orderId: orderIdStr || 'N/A',
                            transactionId: paymentIntent,
                            message: 'Thanh toán Stripe thành công!'
                        });
                        toast.success('Thanh toán thành công!');
                    } else {
                        setResult({
                            success: false,
                                message: redirectStatus === 'failed' ? 'Thanh toán Stripe thất bại' : 'Trạng thái thanh toán không xác định'
                        });
                        toast.error('Thanh toán thất bại!');
                    }
                } else {
                    // Use gateway handler for other gateways
                    const handler = gatewayHandlers[gateway];
                    const paymentResult = handler(params);
                    setResult(paymentResult);

                    if (paymentResult.success) {
                        toast.success('Thanh toán thành công!');
                    } else {
                        toast.error('Thanh toán thất bại!');
                    }
                }
            } catch (error) {
                logger.error(` ${gatewayNames[gateway]} Return: Exception during processing`, {
                    error: error instanceof Error ? error.message : 'Unknown error'
                });
                setResult({ success: false, message: 'Có lỗi xảy ra khi xử lý kết quả thanh toán' });
                toast.error('Có lỗi xảy ra!');
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
                        <p className="mt-4 text-muted-foreground">Đang xử lý kết quả thanh toán...</p>
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
                        {result?.success ? 'Thanh toán thành công!' : 'Thanh toán thất bại!'}
                    </CardTitle>
                    <CardDescription>{result?.message}</CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    {result && (
                        <div className="space-y-2 text-sm">
                            {result.orderId && result.orderId !== 'N/A' && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">Mã đơn hàng:</span>
                                    <span className="font-medium">#{result.orderId}</span>
                                </div>
                            )}
                            {result.transactionId && result.transactionId !== 'N/A' && result.success && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">Mã giao dịch:</span>
                                    <span className="font-medium text-xs">{result.transactionId}</span>
                                </div>
                            )}
                            {result.amount && result.amount !== 'N/A' && result.success && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">Số tiền:</span>
                                    <span className="font-medium">{result.amount}</span>
                                </div>
                            )}
                            {result.responseCode && (
                                <div className="flex justify-between">
                                    <span className="text-muted-foreground">Mã phản hồi:</span>
                                    <span className="font-medium">{result.responseCode}</span>
                                </div>
                            )}
                        </div>
                    )}

                    <div className="flex flex-col gap-2">
                        {result?.success ? (
                            <>
                                <Button asChild className="w-full">
                                    <Link href="/account/orders">Xem đơn hàng</Link>
                                </Button>
                                <Button variant="outline" asChild className="w-full">
                                    <Link href="/">Tiếp tục mua sắm</Link>
                                </Button>
                            </>
                        ) : (
                            <>
                                <Button asChild className="w-full">
                                    <Link href="/checkout">Thử lại thanh toán</Link>
                                </Button>
                                <Button variant="outline" asChild className="w-full">
                                    <Link href="/cart">Quay lại giỏ hàng</Link>
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
