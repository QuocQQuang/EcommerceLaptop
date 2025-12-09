'use client';

import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { formatCurrencyPrice } from '@/lib/currency';
import {
    Calendar,
    CheckCircle,
    Clock,
    CreditCard,
    XCircle
} from 'lucide-react';

interface PaymentInfoProps {
    paymentStatus: string;
    paymentMethod: string;
    totalAmount: number;
    subtotal: number;
    shippingCost: number;
    taxAmount: number;
    paidAt?: string;
    className?: string;
}

export function PaymentInfo({
    paymentStatus,
    paymentMethod,
    totalAmount,
    subtotal,
    shippingCost,
    taxAmount,
    paidAt,
    className
}: PaymentInfoProps) {
    const { selectedCurrency } = useCurrencyContext();

    const getPaymentStatusInfo = (status: string) => {
        const statusMap: Record<string, { label: string; color: string; icon: React.ReactNode }> = {
            'pending': {
                label: 'Ch thanh ton',
                color: 'bg-yellow-100 text-yellow-800',
                icon: <Clock className="h-4 w-4" />
            },
            'paid': {
                label: ' thanh ton',
                color: 'bg-green-100 text-green-800',
                icon: <CheckCircle className="h-4 w-4" />
            },
            'failed': {
                label: 'Thanh ton tht bi',
                color: 'bg-red-100 text-red-800',
                icon: <XCircle className="h-4 w-4" />
            },
            'refunded': {
                label: ' hon tin',
                color: 'bg-blue-100 text-blue-800',
                icon: <CheckCircle className="h-4 w-4" />
            }
        };
        return statusMap[status] || {
            label: status,
            color: 'bg-gray-100 text-gray-800',
            icon: <Clock className="h-4 w-4" />
        };
    };

    const paymentStatusInfo = getPaymentStatusInfo(paymentStatus);

    return (
        <div className={`space-y-4 ${className}`}>
            {/* Payment Status Card */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center space-x-2">
                        <CreditCard className="h-5 w-5" />
                        <span>Thng tin thanh ton</span>
                    </CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                    {/* Payment Status */}
                    <div className="flex items-center justify-between">
                        <span className="text-sm text-gray-600">Trng thi:</span>
                        <Badge className={`${paymentStatusInfo.color} flex items-center space-x-1`}>
                            {paymentStatusInfo.icon}
                            <span>{paymentStatusInfo.label}</span>
                        </Badge>
                    </div>

                    {/* Payment Method */}
                    <div className="flex items-center justify-between">
                        <span className="text-sm text-gray-600">Phng thc:</span>
                        <span className={`text-sm font-medium ${paymentMethod === 'Cha xc nh' ? 'text-gray-500 italic' : ''}`}>
                            {paymentMethod}
                        </span>
                    </div>

                    {/* Payment Date */}
                    {paidAt && (
                        <div className="flex items-center justify-between">
                            <span className="text-sm text-gray-600">Ngy thanh ton:</span>
                            <div className="flex items-center space-x-1">
                                <Calendar className="h-4 w-4 text-gray-500" />
                                <span className="text-sm">
                                    {new Date(paidAt).toLocaleDateString('vi-VN')}
                                </span>
                            </div>
                        </div>
                    )}
                </CardContent>
            </Card>

            {/* Payment Breakdown */}
            <Card>
                <CardHeader>
                    <CardTitle>Tng kt thanh ton</CardTitle>
                </CardHeader>
                <CardContent className="space-y-3">
                    <div className="flex justify-between">
                        <span className="text-sm text-gray-600">Tm tnh:</span>
                        <span className="text-sm">{formatCurrencyPrice(subtotal, selectedCurrency)}</span>
                    </div>
                    <div className="flex justify-between">
                        <span className="text-sm text-gray-600">Ph vn chuyn:</span>
                        <span className="text-sm">{formatCurrencyPrice(shippingCost, selectedCurrency)}</span>
                    </div>
                    <div className="flex justify-between">
                        <span className="text-sm text-gray-600">Thu:</span>
                        <span className="text-sm">{formatCurrencyPrice(taxAmount, selectedCurrency)}</span>
                    </div>
                    <hr className="my-2" />
                    <div className="flex justify-between">
                        <span className="font-medium">Tng cng:</span>
                        <span className="font-bold text-lg">{formatCurrencyPrice(totalAmount, selectedCurrency)}</span>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
