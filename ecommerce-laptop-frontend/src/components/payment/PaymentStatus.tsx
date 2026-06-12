'use client';

import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import {
    AlertCircle,
    Banknote,
    CheckCircle,
    Clock,
    CreditCard,
    QrCode,
    RefreshCw,
    XCircle
} from 'lucide-react';

interface PaymentStatusProps {
    status: string;
    gateway?: string;
    method?: string;
    transactionId?: string;
    amount?: number;
    currency?: string;
    retryCount?: number;
    maxRetries?: number;
    onRetry?: () => void;
    isRetrying?: boolean;
}

export function PaymentStatus({
    status,
    gateway,
    method,
    transactionId,
    amount,
    currency,
    retryCount = 0,
    maxRetries = 3,
    onRetry,
    isRetrying = false
}: PaymentStatusProps) {
    const getStatusConfig = (status: string) => {
        switch (status.toLowerCase()) {
            case 'completed':
            case 'confirmed':
                return {
                    icon: CheckCircle,
                    label: 'Thành công',
                    variant: 'default' as const,
                    color: 'text-green-600',
                    bgColor: 'bg-green-50',
                    borderColor: 'border-green-200'
                };
            case 'pending':
            case 'processing':
                return {
                    icon: Clock,
                    label: 'Đang xử lý',
                    variant: 'secondary' as const,
                    color: 'text-yellow-600',
                    bgColor: 'bg-yellow-50',
                    borderColor: 'border-yellow-200'
                };
            case 'failed':
            case 'cancelled':
                return {
                    icon: XCircle,
                    label: 'Tht bi',
                    variant: 'destructive' as const,
                    color: 'text-red-600',
                    bgColor: 'bg-red-50',
                    borderColor: 'border-red-200'
                };
            default:
                return {
                    icon: AlertCircle,
                    label: status,
                    variant: 'outline' as const,
                    color: 'text-gray-600',
                    bgColor: 'bg-gray-50',
                    borderColor: 'border-gray-200'
                };
        }
    };

    const getMethodIcon = (method?: string) => {
        switch (method?.toLowerCase()) {
            case 'card':
            case 'creditcard':
            case 'debitcard':
                return <CreditCard className="h-4 w-4" />;
            case 'qr':
            case 'qrcode':
                return <QrCode className="h-4 w-4" />;
            case 'banktransfer':
            case 'bank_transfer':
                return <Banknote className="h-4 w-4" />;
            default:
                return <CreditCard className="h-4 w-4" />;
        }
    };

    const statusConfig = getStatusConfig(status);
    const Icon = statusConfig.icon;
    const progress = status === 'completed' ? 100 : status === 'processing' ? 50 : 0;

    return (
        <Card className={`${statusConfig.bgColor} ${statusConfig.borderColor} border-2`}>
            <CardHeader className="pb-3">
                <CardTitle className="flex items-center gap-2 text-lg">
                    <Icon className={`h-5 w-5 ${statusConfig.color}`} />
                    Trạng thái thanh toán
                </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
                {/* Status Badge */}
                <div className="flex items-center justify-between">
                    <Badge variant={statusConfig.variant} className="text-sm">
                        {statusConfig.label}
                    </Badge>
                    {retryCount > 0 && (
                        <span className="text-xs text-muted-foreground">
                            Th li: {retryCount}/{maxRetries}
                        </span>
                    )}
                </div>

                {/* Progress Bar */}
                <div className="space-y-2">
                    <div className="flex justify-between text-sm">
                        <span>Tiến độ</span>
                        <span>{progress}%</span>
                    </div>
                    <Progress value={progress} className="h-2" />
                </div>

                {/* Payment Details */}
                <div className="space-y-2 text-sm">
                    {gateway && (
                        <div className="flex justify-between">
                            <span className="text-muted-foreground">Cổng thanh toán:</span>
                            <span className="font-medium">{gateway}</span>
                        </div>
                    )}

                    {method && (
                        <div className="flex justify-between items-center">
                            <span className="text-muted-foreground">Phương thức:</span>
                            <div className="flex items-center gap-1">
                                {getMethodIcon(method)}
                                <span className="font-medium capitalize">
                                    {method.replace('_', ' ')}
                                </span>
                            </div>
                        </div>
                    )}

                    {transactionId && (
                        <div className="flex justify-between">
                            <span className="text-muted-foreground">Mã giao dịch:</span>
                            <span className="font-mono text-xs">{transactionId}</span>
                        </div>
                    )}

                    {amount && currency && (
                        <div className="flex justify-between">
                            <span className="text-muted-foreground">Số tiền:</span>
                            <span className="font-semibold">
                                {amount.toLocaleString('vi-VN')} {currency}
                            </span>
                        </div>
                    )}
                </div>

                {/* Retry Button */}
                {status === 'failed' && onRetry && retryCount < maxRetries && (
                    <div className="pt-2">
                        <button
                            onClick={onRetry}
                            disabled={isRetrying}
                            className="w-full flex items-center justify-center gap-2 px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
                        >
                            {isRetrying ? (
                                <>
                                    <RefreshCw className="h-4 w-4 animate-spin" />
                                    đang thử lại...
                                </>
                            ) : (
                                <>
                                    <RefreshCw className="h-4 w-4" />
                                    Thử lại thanh toán
                                </>
                            )}
                        </button>
                    </div>
                )}

                {/* Status Messages */}
                <div className="text-xs text-muted-foreground">
                    {status === 'completed' && (
                        <p className="text-green-600">
                            Thanh toán đã được xác nhận thành công
                        </p>
                    )}
                    {status === 'processing' && (
                        <p className="text-yellow-600">
                            đang xử lý thanh toán, vui lòng đợi...
                        </p>
                    )}
                    {status === 'failed' && (
                        <p className="text-red-600">
                            Thanh toán thất bại. Vui lòng thử lại hoặc chọn phương thức khác.
                        </p>
                    )}
                </div>
            </CardContent>
        </Card>
    );
}
