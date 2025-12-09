'use client';

import { Badge } from '@/components/ui/badge';
import { Progress } from '@/components/ui/progress';
import {
    CheckCircle,
    Clock,
    Package,
    Truck,
    XCircle
} from 'lucide-react';

interface OrderStatusProgressProps {
    status: string;
    className?: string;
}

export function OrderStatusProgress({ status, className }: OrderStatusProgressProps) {
    const statusSteps = [
        { key: 'pending', label: 'Ch x l', icon: Clock, color: 'bg-yellow-500' },
        { key: 'confirmed', label: ' xc nhn', icon: CheckCircle, color: 'bg-blue-500' },
        { key: 'processing', label: 'ang x l', icon: Package, color: 'bg-purple-500' },
        { key: 'shipped', label: ' giao hng', icon: Truck, color: 'bg-green-500' },
        { key: 'delivered', label: ' nhn hng', icon: CheckCircle, color: 'bg-green-600' },
        { key: 'cancelled', label: ' hy', icon: XCircle, color: 'bg-red-500' }
    ];

    const currentStepIndex = statusSteps.findIndex(step => step.key === status);
    const progress = currentStepIndex >= 0 ? ((currentStepIndex + 1) / statusSteps.length) * 100 : 0;

    const getStatusInfo = (stepKey: string) => {
        const statusMap: Record<string, { label: string; color: string; icon: React.ReactNode }> = {
            'pending': { label: 'Ch x l', color: 'bg-yellow-100 text-yellow-800', icon: <Clock className="h-4 w-4" /> },
            'confirmed': { label: ' xc nhn', color: 'bg-blue-100 text-blue-800', icon: <CheckCircle className="h-4 w-4" /> },
            'processing': { label: 'ang x l', color: 'bg-purple-100 text-purple-800', icon: <Package className="h-4 w-4" /> },
            'shipped': { label: ' giao hng', color: 'bg-green-100 text-green-800', icon: <Truck className="h-4 w-4" /> },
            'delivered': { label: ' nhn hng', color: 'bg-green-100 text-green-800', icon: <CheckCircle className="h-4 w-4" /> },
            'cancelled': { label: ' hy', color: 'bg-red-100 text-red-800', icon: <XCircle className="h-4 w-4" /> }
        };
        return statusMap[stepKey] || { label: stepKey, color: 'bg-gray-100 text-gray-800', icon: <Clock className="h-4 w-4" /> };
    };

    const currentStatusInfo = getStatusInfo(status);

    return (
        <div className={`space-y-4 ${className}`}>
            {/* Current Status Badge */}
            <div className="flex items-center justify-center">
                <Badge className={`${currentStatusInfo.color} flex items-center space-x-2 px-4 py-2 text-sm`}>
                    {currentStatusInfo.icon}
                    <span>{currentStatusInfo.label}</span>
                </Badge>
            </div>

            {/* Progress Bar */}
            {status !== 'cancelled' && (
                <div className="space-y-2">
                    <Progress value={progress} className="h-2" />
                    <div className="flex justify-between text-xs text-gray-500">
                        <span>Bt u</span>
                        <span>Hon thnh</span>
                    </div>
                </div>
            )}

            {/* Status Steps */}
            <div className="flex justify-between">
                {statusSteps.map((step, index) => {
                    const isActive = index <= currentStepIndex;
                    const isCurrent = step.key === status;
                    const Icon = step.icon;

                    return (
                        <div key={step.key} className="flex flex-col items-center space-y-1">
                            <div className={`
                                w-8 h-8 rounded-full flex items-center justify-center text-white text-xs
                                ${isActive ? step.color : 'bg-gray-300'}
                                ${isCurrent ? 'ring-2 ring-offset-2 ring-blue-500' : ''}
                            `}>
                                <Icon className="h-4 w-4" />
                            </div>
                            <span className={`text-xs text-center max-w-16 ${isActive ? 'text-gray-900' : 'text-gray-400'}`}>
                                {step.label}
                            </span>
                        </div>
                    );
                })}
            </div>
        </div>
    );
}
