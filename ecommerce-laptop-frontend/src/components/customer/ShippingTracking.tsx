'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
    ExternalLink,
    MapPin,
    Package,
    Truck
} from 'lucide-react';

interface ShippingTrackingProps {
    trackingNumber?: string;
    shippingAddress: string;
    estimatedDeliveryDate?: string;
    className?: string;
}

export function ShippingTracking({
    trackingNumber,
    shippingAddress,
    estimatedDeliveryDate,
    className
}: ShippingTrackingProps) {
    const getTrackingUrl = (trackingNumber: string) => {
        // This would be configured based on the shipping provider
        // For now, we'll use a generic tracking service
        return `https://www.17track.net/en/track?nums=${trackingNumber}`;
    };

    return (
        <Card className={className}>
            <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                    <Truck className="h-5 w-5" />
                    <span>Thông tin vận chuyển</span>
                </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
                {/* Delivery Address */}
                <div className="flex items-start space-x-3">
                    <MapPin className="h-5 w-5 text-gray-500 mt-0.5" />
                    <div>
                        <p className="font-medium text-gray-900 mb-1">a ch giao hng</p>
                        <p className="text-sm text-gray-700">{shippingAddress}</p>
                    </div>
                </div>

                {/* Tracking Number */}
                {trackingNumber && (
                    <div className="flex items-center space-x-3">
                        <Package className="h-5 w-5 text-gray-500" />
                        <div className="flex-1">
                            <p className="font-medium text-gray-900 mb-1">M vn n</p>
                            <div className="flex items-center space-x-2">
                                <code className="text-sm bg-gray-100 px-2 py-1 rounded font-mono">
                                    {trackingNumber}
                                </code>
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={() => window.open(getTrackingUrl(trackingNumber), '_blank')}
                                >
                                    <ExternalLink className="h-4 w-4 mr-1" />
                                    Theo di
                                </Button>
                            </div>
                        </div>
                    </div>
                )}

                {/* Estimated Delivery */}
                {estimatedDeliveryDate && (
                    <div className="p-3 bg-blue-50 rounded-lg">
                        <div className="flex items-center space-x-2">
                            <Badge variant="outline" className="bg-blue-100 text-blue-800">
                                Đang được giao hàng
                            </Badge>
                            <span className="text-sm font-medium text-blue-900">
                                {new Date(estimatedDeliveryDate).toLocaleDateString('vi-VN')}
                            </span>
                        </div>
                    </div>
                )}

                {/* No tracking number message */}
                {!trackingNumber && (
                    <div className="p-3 bg-yellow-50 rounded-lg">
                        <p className="text-sm text-yellow-800">
                            Mã vận đơn sẽ được cập nhật khi đơn hàng được giao cho đơn vị vận chuyển.
                        </p>
                    </div>
                )}
            </CardContent>
        </Card>
    );
}
