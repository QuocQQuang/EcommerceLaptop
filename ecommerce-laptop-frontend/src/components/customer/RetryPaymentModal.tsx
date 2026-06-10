'use client';

import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/components/ui/dialog';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { usePayment } from '@/contexts/PaymentContext';
import { formatCurrencyPrice } from '@/lib/currency';
import { PaymentGateway, PaymentMethod } from '@/types/api';
import {
    AlertCircle,
    CreditCard,
    Info,
    Loader2,
    RefreshCw
} from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { toast } from 'sonner';

// Helper functions to convert enum values to display names
const getGatewayDisplayName = (gateway: PaymentGateway): string => {
    switch (gateway) {
        case PaymentGateway.MoMo: return 'MoMo';
        case PaymentGateway.ZaloPay: return 'ZaloPay';
        case PaymentGateway.Stripe: return 'Stripe';
        default: return 'Unknown';
    }
};

const getMethodDisplayName = (method: PaymentMethod): string => {
    switch (method) {
        case PaymentMethod.CreditCard: return 'Thẻ tín dụng';
        case PaymentMethod.DebitCard: return 'Thẻ ghi nợ';
        case PaymentMethod.EWallet: return 'Ví điện tử';
        case PaymentMethod.BankTransfer: return 'Chuyển khoản ngân hàng';
        case PaymentMethod.QRCode: return 'QR Code';
        case PaymentMethod.Installment: return 'Trả góp';
        case PaymentMethod.CashOnDelivery: return 'Thanh toán khi nhận hàng';
        default: return 'Unknown';
    }
};

// Auto-select payment method based on gateway
const getDefaultMethodForGateway = (gateway: PaymentGateway): PaymentMethod => {
    switch (gateway) {
        case PaymentGateway.Stripe: return PaymentMethod.CreditCard;
        default: return PaymentMethod.CreditCard;
    }
};

interface RetryPaymentModalProps {
    orderId: number;
    orderAmount: number;
    orderNumber: string;
    isOpen: boolean;
    onClose: () => void;
    onSuccess: () => void;
}

export function RetryPaymentModal({
    orderId,
    orderAmount,
    orderNumber,
    isOpen,
    onClose,
    onSuccess
}: RetryPaymentModalProps) {
    const { selectedCurrency } = useCurrencyContext();
    const { state, actions } = usePayment();
    const [selectedGateway, setSelectedGateway] = useState<PaymentGateway>(PaymentGateway.Stripe);
    const [selectedMethod, setSelectedMethod] = useState<PaymentMethod>(PaymentMethod.CreditCard);
    const [retryCount, setRetryCount] = useState(0);
    const [lastRetryTime, setLastRetryTime] = useState<Date | null>(null);

    // Track if options have been loaded to prevent infinite loops
    const optionsLoadedRef = useRef(false);

    // Load available payment options when modal opens
    useEffect(() => {
        if (isOpen && !optionsLoadedRef.current) {
            optionsLoadedRef.current = true;
            actions.loadAvailableOptions(orderAmount, selectedCurrency);
        }

        // Reset when modal closes
        if (!isOpen) {
            optionsLoadedRef.current = false;
        }
    }, [isOpen, orderAmount, selectedCurrency]);

    // Reset state when modal closes
    useEffect(() => {
        if (!isOpen) {
            setRetryCount(0);
            setLastRetryTime(null);
        }
    }, [isOpen]);

    // Auto-select payment method when gateway changes
    useEffect(() => {
        const defaultMethod = getDefaultMethodForGateway(selectedGateway);
        setSelectedMethod(defaultMethod);
    }, [selectedGateway]);

    const handleRetryPayment = async () => {
        // Check retry limits
        if (retryCount >= 3) {
            toast.error('Đã vượt quá số lần thử lại cho phép (3 lần)');
            return;
        }

        // Check time between retries (minimum 30 seconds)
        if (lastRetryTime) {
            const timeSinceLastRetry = Date.now() - lastRetryTime.getTime();
            if (timeSinceLastRetry < 30000) {
                const remainingTime = Math.ceil((30000 - timeSinceLastRetry) / 1000);
                toast.error(`Vui lòng đợi ${remainingTime} giây trước khi thử lại`);
                return;
            }
        }

        setRetryCount(prev => prev + 1);
        setLastRetryTime(new Date());

        try {
            const result = await actions.initializePayment(
                orderId,
                selectedGateway,
                selectedMethod,
                orderAmount,
                selectedCurrency
            );

            // Check if payment was initialized successfully
            if (result && result.isSuccess) {
                if (result.paymentUrl && result.paymentUrl.trim() !== '') {
                    // Other gateways will redirect
                    toast.success('Đang chuyển hướng đến trang thanh toán...');
                } else if (result.additionalData?.client_secret) {
                    // For Stripe payment
                    toast.success('Đang chuyển hướng đến trang thanh toán Stripe...');
                } else {
                    toast.error('Không nhận được liên kết thanh toán. Vui lòng thử lại.');
                }
            } else {
                toast.error('Không thể khởi tạo thanh toán lại');
            }
        } catch (error: any) {
            console.error('Retry payment error:', error);
            toast.error(error.message || 'Có lỗi xảy ra khi thanh toán lại');
        }
    };

    const getGatewayIcon = (gateway: PaymentGateway) => {
        switch (gateway) {
            case PaymentGateway.MoMo:
                return '';
            case PaymentGateway.ZaloPay:
                return '';
            case PaymentGateway.Stripe:
                return '';
            default:
                return '';
        }
    };

    const getMethodIcon = (method: PaymentMethod) => {
        switch (method) {
            case PaymentMethod.CreditCard:
                return <CreditCard className="h-4 w-4" />;
            case PaymentMethod.DebitCard:
                return <CreditCard className="h-4 w-4" />;
            case PaymentMethod.EWallet:
                return '';
            case PaymentMethod.BankTransfer:
                return '';
            case PaymentMethod.QRCode:
                return '';
            case PaymentMethod.Installment:
                return '';
            case PaymentMethod.CashOnDelivery:
                return '';
            default:
                return <CreditCard className="h-4 w-4" />;
        }
    };

    const getRetryStatus = () => {
        if (retryCount === 0) return { status: 'ready', message: 'Sẵn sàng thử lại' };
        if (retryCount < 3) return { status: 'warning', message: `Đã thử ${retryCount}/3 lần` };
        return { status: 'error', message: 'Đã vượt quá số lần thử lại' };
    };

    const retryStatus = getRetryStatus();

    return (
        <Dialog open={isOpen} onOpenChange={onClose}>
            <DialogContent className="max-w-2xl max-h-[90vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle className="flex items-center gap-2">
                        <RefreshCw className="h-5 w-5" />
                        Thanh toán lại đơn hàng
                    </DialogTitle>
                </DialogHeader>

                <div className="space-y-6">
                    {/* Order Information */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="text-lg flex items-center gap-2">
                                <Info className="h-4 w-4" />
                                Thông tin đơn hàng
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-3">
                            <div className="flex justify-between items-center">
                                <span className="text-sm text-muted-foreground">Mã đơn hàng:</span>
                                <Badge variant="outline">{orderNumber}</Badge>
                            </div>
                            <div className="flex justify-between items-center">
                                <span className="text-sm text-muted-foreground">Số tiền:</span>
                                <span className="font-semibold text-lg">
                                    {formatCurrencyPrice(orderAmount, selectedCurrency)}
                                </span>
                            </div>
                            <div className="flex justify-between items-center">
                                <span className="text-sm text-muted-foreground">Trạng thái:</span>
                                <Badge variant="destructive">Chưa thanh toán</Badge>
                            </div>
                        </CardContent>
                    </Card>

                    {/* Retry Status */}
                    <Alert className={retryStatus.status === 'error' ? 'border-red-200 bg-red-50' :
                        retryStatus.status === 'warning' ? 'border-yellow-200 bg-yellow-50' :
                            'border-green-200 bg-green-50'}>
                        <AlertCircle className="h-4 w-4" />
                        <AlertDescription>
                            {retryStatus.message}
                            {lastRetryTime && (
                                <div className="text-xs mt-1 text-muted-foreground">
                                    Lần thử cuối: {lastRetryTime.toLocaleString('vi-VN')}
                                </div>
                            )}
                        </AlertDescription>
                    </Alert>

                    {/* Payment Method Selection */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="text-lg">Chọn phương thức thanh toán</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-4">
                                <div>
                                    <Label htmlFor="gateway">Cổng thanh toán</Label>
                                    <Select
                                        value={selectedGateway.toString()}
                                        onValueChange={(value) => setSelectedGateway(parseInt(value) as PaymentGateway)}
                                    >
                                        <SelectTrigger id="gateway">
                                            <SelectValue />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {state.availableGateways.map((gateway) => (
                                                <SelectItem key={gateway} value={gateway.toString()}>
                                                    <div className="flex items-center gap-2">
                                                        <span>{getGatewayIcon(gateway)}</span>
                                                        <span>{getGatewayDisplayName(gateway)}</span>
                                                    </div>
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                </div>

                                {/* Auto-selected payment method display */}
                                <div>
                                    <Label>Phương thức thanh toán</Label>
                                    <div className="flex items-center gap-2 p-3 border rounded-md bg-muted/50">
                                        {getMethodIcon(selectedMethod)}
                                        <span className="font-medium">{getMethodDisplayName(selectedMethod)}</span>
                                        <span className="text-sm text-muted-foreground ml-auto">
                                            (Tự động chọn)
                                        </span>
                                    </div>
                                </div>
                            </div>

                            <Separator />

                            {/* Payment Summary */}
                            <div className="bg-muted/50 rounded-lg p-4">
                                <h4 className="font-medium mb-2">Tóm tắt thanh toán</h4>
                                <div className="space-y-1 text-sm">
                                    <div className="flex justify-between">
                                        <span>Cổng thanh toán:</span>
                                        <span className="font-medium">{getGatewayDisplayName(selectedGateway)}</span>
                                    </div>
                                    <div className="flex justify-between">
                                        <span>Phương thức:</span>
                                        <span className="font-medium">{getMethodDisplayName(selectedMethod)} (Tự động chọn)</span>
                                    </div>
                                    <div className="flex justify-between">
                                        <span>Số tiền:</span>
                                        <span className="font-semibold">
                                            {formatCurrencyPrice(orderAmount, selectedCurrency)}
                                        </span>
                                    </div>
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    {/* Action Buttons */}
                    <div className="flex gap-3 justify-end">
                        <Button variant="outline" onClick={onClose}>
                            Hủy
                        </Button>
                        <Button
                            onClick={handleRetryPayment}
                            disabled={state.isLoading || retryCount >= 3}
                            className="min-w-[140px]"
                        >
                            {state.isLoading ? (
                                <>
                                    <Loader2 className="mr-2 h-4 w-4 animate-spin" />
                                    Đang xử lý...
                                </>
                            ) : (
                                <>
                                    <RefreshCw className="mr-2 h-4 w-4" />
                                    Thanh toán lại
                                </>
                            )}
                        </Button>
                    </div>
                </div>
            </DialogContent>
        </Dialog>
    );
}
