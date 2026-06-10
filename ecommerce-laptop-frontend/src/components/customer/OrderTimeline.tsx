'use client';

import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
    CheckCircle,
    Clock,
    Package,
    Truck,
    XCircle
} from 'lucide-react';

interface OrderAudit {
    oldStatus: string;
    newStatus: string;
    changedBy: string;
    reason: string;
    changedAt: string;
}

interface OrderTimelineProps {
    auditTrail: OrderAudit[];
    className?: string;
}

export function OrderTimeline({ auditTrail, className }: OrderTimelineProps) {
    if (!auditTrail || auditTrail.length === 0) {
        return null;
    }

    const getStatusInfo = (status: string) => {
        const statusMap: Record<string, { label: string; color: string; icon: React.ReactNode }> = {
            'pending': { label: 'Chờ xử lý', color: 'bg-yellow-100 text-yellow-800', icon: <Clock className="h-4 w-4" /> },
            'confirmed': { label: 'Đã xác nhận', color: 'bg-blue-100 text-blue-800', icon: <CheckCircle className="h-4 w-4" /> },
            'processing': { label: 'Đang xử lý', color: 'bg-purple-100 text-purple-800', icon: <Package className="h-4 w-4" /> },
            'shipped': { label: 'Đã giao hàng', color: 'bg-green-100 text-green-800', icon: <Truck className="h-4 w-4" /> },
            'delivered': { label: 'Đã nhận hàng', color: 'bg-green-100 text-green-800', icon: <CheckCircle className="h-4 w-4" /> },
            'cancelled': { label: 'Đã hủy', color: 'bg-red-100 text-red-800', icon: <XCircle className="h-4 w-4" /> }
        };
        return statusMap[status] || { label: status, color: 'bg-gray-100 text-gray-800', icon: <Clock className="h-4 w-4" /> };
    };

    return (
        <Card className={className}>
            <CardHeader>
                <CardTitle>Lịch sử đơn hàng</CardTitle>
            </CardHeader>
            <CardContent>
                <div className="space-y-6">
                    {auditTrail.map((audit, index) => {
                        const statusInfo = getStatusInfo(audit.newStatus);
                        const isLast = index === auditTrail.length - 1;

                        return (
                            <div key={index} className="flex items-start space-x-4">
                                {/* Timeline line and dot */}
                                <div className="flex flex-col items-center">
                                    <div className={`
                                        w-3 h-3 rounded-full flex items-center justify-center
                                        ${isLast ? 'bg-blue-500' : 'bg-gray-300'}
                                    `}>
                                        {isLast && <div className="w-1.5 h-1.5 bg-white rounded-full"></div>}
                                    </div>
                                    {!isLast && (
                                        <div className="w-0.5 h-8 bg-gray-200 mt-2"></div>
                                    )}
                                </div>

                                {/* Content */}
                                <div className="flex-1 pb-4">
                                    <div className="flex items-center space-x-2 mb-2">
                                        <Badge className={`${statusInfo.color} flex items-center space-x-1`}>
                                            {statusInfo.icon}
                                            <span>{statusInfo.label}</span>
                                        </Badge>
                                        <span className="text-sm text-gray-500">
                                            {new Date(audit.changedAt).toLocaleString('vi-VN')}
                                        </span>
                                    </div>

                                    {audit.reason && (
                                        <p className="text-sm text-gray-600 mb-1">
                                            <span className="font-medium">Lý do:</span> {audit.reason}
                                        </p>
                                    )}

                                    <p className="text-xs text-gray-500">
                                        Thay đổi bởi: {audit.changedBy}
                                    </p>
                                </div>
                            </div>
                        );
                    })}
                </div>
            </CardContent>
        </Card>
    );
}
