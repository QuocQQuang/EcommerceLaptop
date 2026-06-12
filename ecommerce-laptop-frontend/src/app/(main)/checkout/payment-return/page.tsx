'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { paymentService } from '@/services/paymentService';
import { AlertCircle, CheckCircle } from 'lucide-react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';

function PaymentReturnContent() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const [status, setStatus] = useState<'loading' | 'success' | 'failed'>(
        'loading',
    );
    const [message, setMessage] = useState('Đang xác thực thanh toán...');
    const [orderId, setOrderId] = useState<string | null>(null);

    useEffect(() => {
        const verifyPayment = async () => {
            const queryString = searchParams?.toString() || '';
            const paymentMethod = window.location.pathname.includes('vnpay')
                ? 'VNPAY'
                : 'MOMO';

            try {
                const { data } = await paymentService.verifyPayment(
                    paymentMethod,
                    queryString,
                );
                setStatus('success');
                setMessage(data.message || 'Thanh toán thành công!');
                setOrderId(data.orderId);
            } catch (error: any) {
                setStatus('failed');
                setMessage(
                    error.response?.data?.message ||
                    'Xác thực thanh toán thất bại. Vui lòng thử lại hoặc liên hệ hỗ trợ.',
                );
                setOrderId(error.response?.data?.orderId || null);
            }
        };

        verifyPayment();
    }, [searchParams]);

    return (
        <div className="container mx-auto px-4 py-16 flex justify-center">
            <Card className="w-full max-w-lg text-center">
                <CardHeader>
                    {status === 'loading' && (
                        <div className="mx-auto">
                            <LoadingSpinner size="lg" />
                        </div>
                    )}
                    {status === 'success' && (
                        <div className="mx-auto bg-green-100 rounded-full h-20 w-20 flex items-center justify-center">
                            <CheckCircle className="h-12 w-12 text-green-600" />
                        </div>
                    )}
                    {status === 'failed' && (
                        <div className="mx-auto bg-red-100 rounded-full h-20 w-20 flex items-center justify-center">
                            <AlertCircle className="h-12 w-12 text-red-600" />
                        </div>
                    )}
                    <CardTitle className="mt-6 text-2xl font-bold">
                        {status === 'loading' && 'Đang xử lý...'}
                        {status === 'success' && 'Thanh toán thành công!'}
                        {status === 'failed' && 'Thanh toán thất bại'}
                    </CardTitle>
                </CardHeader>
                <CardContent className="space-y-6">
                    <p className="text-muted-foreground">{message}</p>

                    {orderId && (
                        <div className="bg-gray-100 rounded-lg p-3">
                            <p className="text-sm text-gray-600">Mã đơn hàng của bạn:</p>
                            <p className="text-lg font-bold text-primary">#{orderId}</p>
                        </div>
                    )}

                    {status !== 'loading' && (
                        <div className="flex flex-col sm:flex-row gap-4 justify-center">
                            {orderId && (
                                <Button asChild>
                                    <Link href={`/account/orders/${orderId}`}>
                                        Xem chi tiết đơn hàng
                                    </Link>
                                </Button>
                            )}
                            <Button variant="outline" asChild>
                                <Link href="/products">Tiếp tục mua sắm</Link>
                            </Button>
                        </div>
                    )}
                </CardContent>
            </Card>
        </div>
    );
}


export default function PaymentReturnPage() {
    return (
        <Suspense fallback={<div className="container mx-auto px-4 py-16 flex justify-center"><LoadingSpinner size="lg" /></div>}>
            <PaymentReturnContent />
        </Suspense>
    );
}
