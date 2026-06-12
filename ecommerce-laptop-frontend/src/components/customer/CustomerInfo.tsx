'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
    Mail,
    User
} from 'lucide-react';

interface CustomerInfoProps {
    customerName: string;
    customerEmail: string;
    className?: string;
}

export function CustomerInfo({
    customerName,
    customerEmail,
    className
}: CustomerInfoProps) {
    return (
        <Card className={className}>
            <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                    <User className="h-5 w-5" />
                    <span>Thông tin khách hàng</span>
                </CardTitle>
            </CardHeader>
            <CardContent className="space-y-3">
                <div className="flex items-center space-x-3">
                    <User className="h-4 w-4 text-gray-500" />
                    <div>
                        <p className="text-sm font-medium text-gray-900">{customerName}</p>
                        <p className="text-xs text-gray-500">Họ tên</p>
                    </div>
                </div>

                <div className="flex items-center space-x-3">
                    <Mail className="h-4 w-4 text-gray-500" />
                    <div>
                        <p className="text-sm font-medium text-gray-900">{customerEmail}</p>
                        <p className="text-xs text-gray-500">Email</p>
                    </div>
                </div>
            </CardContent>
        </Card>
    );
}
