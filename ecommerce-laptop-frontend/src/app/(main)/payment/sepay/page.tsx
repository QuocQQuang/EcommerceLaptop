'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { useAuth } from '@/hooks/useAuth';
import api from '@/lib/api';
import { CurrencyService } from '@/lib/currency';
import logger from '@/lib/logger';
import { orderService } from '@/services/orderService';
import { paymentService, SePayConfig, SePayQRResponse } from '@/services/paymentService';
import { OrderResponse } from '@/types/order';
import { formatAddressForDisplay } from '@/utils/addressUtils';
import { hasOrderAccess, parseUserId } from '@/utils/userUtils';
import { ArrowLeft, Banknote, QrCode } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useCallback, useEffect, useState } from 'react';
import { toast } from 'sonner';

function SePayPaymentContent() {
    const { selectedCurrency } = useCurrencyContext();
    const searchParams = useSearchParams();
    const router = useRouter();
    const orderId = parseInt(searchParams!.get('orderId') || '0');
    const env = searchParams!.get('env') || 'sandbox';
    const [environment, setEnvironment] = useState<'sandbox' | 'production'>(env as 'sandbox' | 'production');
    const [isLoading, setIsLoading] = useState(false);
    const [isPolling, setIsPolling] = useState(false);
    const [order, setOrder] = useState<OrderResponse | null>(null);
    const [qrData, setQrData] = useState<SePayQRResponse | null>(null);
    const [status, setStatus] = useState('Pending');
    const [timeLeft, setTimeLeft] = useState(0);
    const [intervalId, setIntervalId] = useState<NodeJS.Timeout | null>(null);
    const { user } = useAuth();

    // Fetch order details and load payment data on mount (deduped)
    useEffect(() => {
        logger.info(' SEPAY Payment: Page loaded', {
            orderId,
            environment,
            pathname: window.location.pathname,
            search: window.location.search
        });

        if (orderId > 0) {
            // Avoid duplicate fetch if already loaded
            if (order && order.id === orderId) {
                return;
            }

            // Wait for user to be loaded
            if (!user) {
                logger.info(' SEPAY: Waiting for user to be loaded', { user });
                return;
            }

            logger.info(' SEPAY: User loaded', {
                userId: user?.id,
                userEmail: user?.email,
                userRoles: user?.roles
            });

            // Load order details
            logger.info(' SEPAY: Loading order details', { orderId });
            orderService.getOrderStatus(orderId)
                .then(order => {
                    setOrder(order);

                    // Ownership check using utility function
                    const orderUserId = Number(order.customerId);
                    const parsedUserId = parseUserId(user?.id);
                    const hasAccess = Number.isFinite(orderUserId) && orderUserId > 0 && hasOrderAccess(user?.id, orderUserId);

                    logger.info(' SEPAY: Debug ownership check', {
                        orderId,
                        userId: user?.id,
                        parsedUserId: parsedUserId,
                        orderUserId: orderUserId,
                        customerId: order.customerId,
                        hasAccess: hasAccess,
                        userObject: user,
                        comparison: {
                            'parsedUserId === orderUserId': parsedUserId === orderUserId,
                            'parsedUserId > 0': parsedUserId > 0,
                            'orderUserId > 0': orderUserId > 0
                        }
                    });

                    if (!hasAccess) {
                        logger.warn(' SEPAY: Unauthorized access to order', {
                            orderId,
                            userId: user?.id,
                            orderUserId: orderUserId,
                            customerId: order.customerId,
                            orderUserIdField: order.userId,
                            hasAccess: hasAccess,
                            parsedUserId: parsedUserId
                        });
                        toast.error('Bạn không có quyền truy cập đơn hàng này.');
                        router.push('/account/orders');
                        return;
                    }

                    // Status validation
                    if (order.paymentStatus === 'Completed' || order.status?.toLowerCase() === 'confirmed') {
                        logger.info(' SEPAY: Order confirmed, redirecting to dashboard', {
                            orderId,
                            status: order.status,
                            paymentStatus: order.paymentStatus
                        });
                        toast.success('Đơn hàng đã được xác nhận thành công! Chuyển đến trang tài khoản.');
                        router.push('/account');
                        return;
                    }

                    if (order.status.toLowerCase() !== 'pending') {
                        logger.warn(' SEPAY: Invalid order status for payment', {
                            orderId,
                            status: order.status,
                            paymentStatus: order.paymentStatus
                        });
                        toast.error('Trạng thái đơn hàng không hợp lệ cho thanh toán.');
                        router.push('/account/orders');
                        return;
                    }

                    logger.info(' SEPAY: Order details loaded', {
                        orderId: order.id,
                        orderNumber: order.orderNumber,
                        status: order.status,
                        totalAmount: order.totalAmount
                    });
                })
                .catch(error => {
                    logger.error(' SEPAY: Failed to load order', {
                        orderId,
                        error: error instanceof Error ? error.message : 'Unknown error'
                    });
                    toast.error('Không thể tải thông tin đơn hàng');
                });

            // Load SEPAY payment data from localStorage (guard repeated parsing)
            const storedPaymentData = localStorage.getItem('sepayPaymentData');
            logger.info(' SEPAY: Checking stored payment data', {
                hasStoredData: !!storedPaymentData,
                storageKey: 'sepayPaymentData'
            });

            if (storedPaymentData) {
                try {
                    const paymentData = JSON.parse(storedPaymentData);
                    logger.info(' SEPAY: Payment data parsed', {
                        orderId: paymentData.orderId,
                        transactionId: paymentData.transactionId,
                        hasQrCodeUrl: !!paymentData.qrCodeUrl,
                        bankAccount: paymentData.bankAccount,
                        bankName: paymentData.bankName
                    });

                    if (paymentData.orderId === orderId) {
                        setQrData({
                            orderId: paymentData.orderId.toString(),
                            bankAccount: paymentData.bankAccount,
                            bankName: paymentData.bankName,
                            bankCode: '',  // Default value
                            amount: paymentData.amount,
                            qrCodeUrl: paymentData.qrCodeUrl,
                            description: paymentData.instructions,
                            expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString() // 15 minutes from now
                        });

                        // Start polling for payment status (if not already)
                        setIsPolling(prev => prev || true);

                        // Clear the localStorage data
                        localStorage.removeItem('sepayPaymentData');

                        logger.info(' SEPAY: QR payment initialized', {
                            orderId,
                            isPolling: true,
                            expiresIn: '15 minutes',
                            qrCodeUrl: paymentData.qrCodeUrl
                        });

                        toast.success('Vui lòng quét mã QR để thanh toán!');
                    } else {
                        logger.warn(' SEPAY: Order ID mismatch', {
                            expectedOrderId: orderId,
                            storedOrderId: paymentData.orderId
                        });
                    }
                } catch (error) {
                    logger.error(' SEPAY: Error parsing payment data', {
                        error: error instanceof Error ? error.message : 'Unknown error'
                    });
                    console.error('Error parsing payment data:', error);
                }
            } else {
                logger.warn(' SEPAY: No stored payment data found - user may have refreshed page');
            }
        } else {
            logger.error(' SEPAY: Invalid order ID', { orderId });
            toast.error('ID đơn hàng không hợp lệ');
            router.push('/checkout');
        }
    }, [orderId, router, user, order, environment]);

    // Cleanup polling interval
    useEffect(() => {
        return () => {
            if (intervalId) {
                clearInterval(intervalId);
            }
        };
    }, [intervalId]);

    // Countdown timer
    useEffect(() => {
        if (qrData && qrData.expiresAt) {
            const expiresAt = new Date(qrData.expiresAt).getTime();
            const timer = setInterval(() => {
                const now = Date.now();
                const left = Math.max(0, (expiresAt - now) / 1000);
                setTimeLeft(left);
                if (left <= 0) {
                    clearInterval(timer);
                    toast.warning('Mã QR đã hết hạn. Vui lòng tạo mới.');
                    setQrData(null);
                    stopPolling();
                }
            }, 1000);
            return () => clearInterval(timer);
        }
    }, [qrData]);

    // Poll payment status
    const pollStatus = useCallback(() => {
        if (orderId && isPolling && !intervalId) {
            logger.info(' SEPAY: Starting payment status polling', {
                orderId,
                interval: '3 seconds'
            });

            const id = setInterval(() => {
                logger.debug(' SEPAY: Polling payment status', { orderId });

                orderService.getOrderStatus(orderId).then((res) => {
                    logger.info(' SEPAY: Status poll response', {
                        orderId,
                        currentStatus: res.status,
                        previousStatus: status
                    });

                    setStatus(res.status);
                    if (res.status?.toLowerCase() === 'confirmed') {
                        logger.info(' SEPAY: Payment CONFIRMED - stopping polling', {
                            orderId,
                            finalStatus: res.status
                        });

                        stopPolling();
                        toast.success('Thanh toán thành công! Đang chuyển đến trang xác nhận.');
                        router.push(`/checkout/confirmation?orderId=${orderId}`);
                    }
                }).catch((error) => {
                    logger.error(' SEPAY: Status polling error', {
                        orderId,
                        error: error instanceof Error ? error.message : 'Unknown error'
                    });
                    clearInterval(id);
                });
            }, 3000);
            setIntervalId(id);
        }
    }, [orderId, isPolling, router, status, intervalId]);

    // Auto-start polling when enabled but interval not yet set (covers resume from localStorage)
    useEffect(() => {
        if (isPolling && !intervalId) {
            pollStatus();
        }
    }, [isPolling, intervalId, pollStatus]);

    const stopPolling = () => {
        if (intervalId) {
            logger.info(' SEPAY: Stopping payment status polling', {
                orderId,
                hadInterval: !!intervalId
            });
            clearInterval(intervalId);
            setIntervalId(null);
            setIsPolling(false);
        }
    };

    const handleGenerateQR = async () => {
        if (!orderId || !order) {
            toast.error('Thông tin đơn hàng không hợp lệ');
            return;
        }

        setIsLoading(true);
        const toastId = toast.loading('Đang lấy cấu hình SePay...');

        try {
            // Fetch SePay config from backend
            const configResponse = await api.get(`/sepay/config?environment=${environment}`);
            const config: SePayConfig = configResponse.data;

            logger.info(' SEPAY: Config loaded', {
                environment,
                accountsCount: config.accounts.length,
                defaultAccount: config.defaultAccount?.accountNumber
            });

            // Build QR data client-side
            const result: SePayQRResponse = paymentService.buildSePayQRUrl(order, config, environment);

            setQrData(result);
            setIsPolling(true);
            pollStatus();
            toast.success('Mã QR đã được tạo thành công!', { id: toastId });
        } catch (error: any) {
            logger.error(' SEPAY: Error generating QR', {
                orderId,
                environment,
                error: error.response?.data?.error || error.message
            });
            toast.error(error.response?.data?.error || 'Lỗi khi tạo mã QR. Vui lòng thử lại.', { id: toastId });
        } finally {
            setIsLoading(false);
        }
    };

    const handleEnvironmentChange = (checked: boolean) => {
        setEnvironment(checked ? 'production' : 'sandbox');
        if (qrData) {
                                    toast.warning('Mã QR sẽ được tạo lại khi thay đổi chế độ.');
            setQrData(null);
            stopPolling();
        }
    };

    if (!order) {
        return (
            <div className="container mx-auto px-4 py-8 flex justify-center items-center min-h-[60vh]">
                <LoadingSpinner size="lg" />
            </div>
        );
    }

    return (
        <div className="container mx-auto px-4 py-8">
            <Link href="/checkout" className="flex items-center text-muted-foreground hover:text-foreground mb-6">
                <ArrowLeft className="h-4 w-4 mr-2" />
                Quay lại thanh toán
            </Link>

            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                <div className="lg:col-span-2">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center">
                                <QrCode className="h-5 w-5 mr-2 text-green-500" />
                                Thanh toán qua Chuyển khoản (SePay)
                            </CardTitle>
                            <CardDescription>
                                 Đơn hàng #{order.orderNumber} - {CurrencyService.convertAmount(order.totalAmount, 'USD', 'VND').toLocaleString('vi-VN')} 
                                <Badge variant="outline" className="ml-2">
                                    {environment}
                                </Badge>
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-6">
                            <div className="space-y-2">
                                <h3 className="font-semibold">Thông tin đơn hàng</h3>
                                <div className="text-sm text-muted-foreground">
                                    <p>Trạng thái: <Badge variant={status === 'Pending' ? 'secondary' : status === 'Confirmed' ? 'default' : 'destructive'}>{status}</Badge></p>
                                    <p>Phương thức: Chuyển khoản ngân hàng</p>
                                    <p>Địa chỉ: {formatAddressForDisplay(order.shippingAddress)}</p>
                                </div>
                            </div>

                            <div className="space-y-4">
                                <h3 className="font-semibold">Chế độ thanh toán</h3>
                                <div className="flex items-center space-x-2 p-3 border rounded-md">
                                    <Switch
                                        id="sepay-environment"
                                        checked={environment === 'production'}
                                        onCheckedChange={handleEnvironmentChange}
                                    />
                                    <label
                                        htmlFor="sepay-environment"
                                        className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"
                                    >
                                        {environment === 'sandbox' ? 'Chế độ Test' : 'Chế độ Thực tế'}
                                    </label>
                                </div>
                                <p className="text-sm text-muted-foreground">
                                    {environment === 'sandbox' ? 'Tài khoản test: VPBank 0908752170' : 'Tài khoản thực: VPBank 1234567890'}
                                </p>
                            </div>

                            {!qrData ? (
                                <div className="space-y-4">
                                    <h3 className="font-semibold">Hướng dẫn thanh toán</h3>
                                    <p className="text-sm text-muted-foreground">
                                         1. Nhấn nút &quot;Tạo mã QR&quot; bên dưới
                                    </p>
                                    <p className="text-sm text-muted-foreground">
                                         2. Quét mã QR bằng app ngân hàng (VietQR)
                                    </p>
                                    <p className="text-sm text-muted-foreground">
                                         3. Hoặc chuyển khoản thủ công với nội dung: <strong>{order.orderNumber}</strong>
                                    </p>
                                    <p className="text-sm text-muted-foreground">
                                         4. Sau khi chuyển khoản, hệ thống sẽ tự động xác nhận trong 3-5 phút
                                    </p>
                                    <Button onClick={handleGenerateQR} className="w-full" disabled={isLoading}>
                                        {isLoading ? <LoadingSpinner /> : 'Tạo mã QR thanh toán'}
                                    </Button>
                                </div>
                            ) : (
                                <div className="space-y-6">
                                    <div className="text-center">
                                        <div className="relative w-64 h-64 mx-auto mb-4">
                                            <Image
                                                src={qrData.qrCodeUrl}
                                                alt="Mã QR thanh toán SePay"
                                                width={256}
                                                height={256}
                                                className="object-contain"
                                            />
                                            <div className="absolute top-0 left-1/2 transform -translate-x-1/2 -translate-y-1/2 bg-white rounded-full p-1">
                                                <QrCode className="h-4 w-4 text-green-600" />
                                            </div>
                                        </div>
                                        <div className="space-y-2">
                                            <p className="text-sm text-muted-foreground">
                                                Thời gian còn lại: <span className="font-bold text-lg">{Math.floor(timeLeft / 60)}:{((timeLeft % 60) / 10).toFixed(0).padStart(2, '0')}:{((timeLeft % 60) % 60).toFixed(0).padStart(2, '0')}
                                                </span>
                                            </p>
                                        </div>
                                    </div>

                                    <div className="bg-muted/50 rounded-lg p-4">
                                        <h3 className="font-semibold mb-3 flex items-center">
                                            <Banknote className="h-4 w-4 mr-1" />
                                            Thông tin chuyển khoản
                                        </h3>
                                        <div className="space-y-2 text-sm">
                                            <p><strong>Ngân hàng:</strong> {qrData.bankName}</p>
                                            <p><strong>Số tài khoản:</strong> {qrData.bankAccount}</p>
                                            <p><strong>Số tiền:</strong> {qrData.amount.toLocaleString('vi-VN')} </p>
                                            <p className="text-xs text-muted-foreground">
                                                <strong>Nội dung chuyển khoản:</strong> {qrData.description} <br />
                                                <span className="text-red-600">Quan trọng: Nội dung chuyển khoản phải chính xác để hệ thống nhận diện đơn hàng!</span>
                                            </p>
                                        </div>
                                    </div>

                                    <div className="text-sm text-muted-foreground space-y-2">
                                        <p> Quét mã QR bằng ứng dụng ngân hàng của bạn</p>
                                        <p> Hoặc chuyển khoản thủ công đến tài khoản trên</p>
                                        <p> Sau khi chuyển khoản, hệ thống sẽ tự động xác nhận trong 3-5 phút</p>
                                        <p> Nếu không thấy xác nhận sau 5 phút, vui lòng liên hệ hỗ trợ</p>
                                    </div>

                                    <div className="flex space-x-2">
                                        <Button variant="outline" size="sm" onClick={() => {
                                            setQrData(null);
                                            stopPolling();
                                        }}>
                                            Tạo mã QR mới
                                        </Button>
                                        <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() => router.push(`/checkout/confirmation?orderId=${orderId}`)}
                                            disabled={status !== 'Confirmed'}
                                        >
                                            Xác nhận thủ công
                                        </Button>
                                    </div>
                                </div>
                            )}
                        </CardContent>
                    </Card>
                </div>

                <div className="lg:col-span-1">
                    <Card className="sticky top-24">
                        <CardHeader>
                            <CardTitle>Tóm tắt đơn hàng</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-2">
                            <div className="flex justify-between">
                                <span>Tạm tính</span>
                                <span>{CurrencyService.convertAmount(order.subtotal, 'USD', 'VND').toLocaleString('vi-VN')} </span>
                            </div>
                            <div className="flex justify-between">
                                <span>Phí vận chuyển</span>
                                <span>Miễn phí</span>
                            </div>
                            <Separator />
                            <div className="flex justify-between text-lg font-bold">
                                <span>Tổng cộng</span>
                                <span>{CurrencyService.convertAmount(order.totalAmount, 'USD', 'VND').toLocaleString('vi-VN')} </span>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            </div>
        </div>
    );
}

export default function SePayPaymentPage() {
    return (
        <Suspense fallback={
            <div className="container mx-auto px-4 py-8 flex justify-center items-center min-h-[60vh]">
                <LoadingSpinner size="lg" />
            </div>
        }>
            <SePayPaymentContent />
        </Suspense>
    );
}