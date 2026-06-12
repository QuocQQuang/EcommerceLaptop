'use client';

import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
    AlertTriangle,
    Clock,
    Eye,
    Shield,
    X
} from 'lucide-react';
import React, { useState } from 'react';

interface SecurityAlertProps {
    type: 'suspicious_activity' | 'high_access' | 'unauthorized_attempt' | 'data_breach_risk';
    message: string;
    details?: string;
    timestamp: string;
    severity: 'low' | 'medium' | 'high' | 'critical';
    onDismiss?: () => void;
    onInvestigate?: () => void;
}

export function SecurityAlert({
    type,
    message,
    details,
    timestamp,
    severity,
    onDismiss,
    onInvestigate
}: SecurityAlertProps) {
    const [isDismissed, setIsDismissed] = useState(false);

    const getAlertConfig = () => {
        const configs = {
            suspicious_activity: {
                icon: Eye,
                title: 'Hoạt động đáng ngờ',
                color: 'border-yellow-500 bg-yellow-50',
                iconColor: 'text-yellow-600'
            },
            high_access: {
                icon: AlertTriangle,
                title: 'Truy cập quá mức',
                color: 'border-orange-500 bg-orange-50',
                iconColor: 'text-orange-600'
            },
            unauthorized_attempt: {
                icon: Shield,
                title: 'Truy cập trái phép',
                color: 'border-red-500 bg-red-50',
                iconColor: 'text-red-600'
            },
            data_breach_risk: {
                icon: AlertTriangle,
                title: 'Rủi ro rò rỉ dữ liệu',
                color: 'border-red-600 bg-red-100',
                iconColor: 'text-red-700'
            }
        };
        return configs[type];
    };

    const getSeverityConfig = () => {
        const configs = {
            low: { color: 'bg-green-100 text-green-800', label: 'Thấp' },
            medium: { color: 'bg-yellow-100 text-yellow-800', label: 'Trung bình' },
            high: { color: 'bg-orange-100 text-orange-800', label: 'Cao' },
            critical: { color: 'bg-red-100 text-red-800', label: 'Nghiêm trọng' }
        };
        return configs[severity];
    };

    const config = getAlertConfig();
    const severityConfig = getSeverityConfig();
    const Icon = config.icon;

    const handleDismiss = () => {
        setIsDismissed(true);
        onDismiss?.();
    };

    if (isDismissed) {
        return null;
    }

    return (
        <Alert className={`${config.color} border-l-4`}>
            <div className="flex items-start gap-3">
                <Icon className={`h-5 w-5 mt-0.5 ${config.iconColor}`} />
                <div className="flex-1">
                    <div className="flex items-center gap-2 mb-1">
                        <AlertTitle className="text-sm font-semibold">
                            {config.title}
                        </AlertTitle>
                        <Badge className={severityConfig.color}>
                            {severityConfig.label}
                        </Badge>
                        <div className="flex items-center gap-1 text-xs text-muted-foreground">
                            <Clock className="h-3 w-3" />
                            {new Intl.DateTimeFormat('vi-VN', {
                                hour: '2-digit',
                                minute: '2-digit',
                                day: '2-digit',
                                month: '2-digit'
                            }).format(new Date(timestamp))}
                        </div>
                    </div>

                    <AlertDescription className="text-sm">
                        {message}
                        {details && (
                            <div className="mt-2 text-xs text-muted-foreground">
                                {details}
                            </div>
                        )}
                    </AlertDescription>

                    <div className="flex items-center gap-2 mt-3">
                        {onInvestigate && (
                            <Button
                                size="sm"
                                variant="outline"
                                onClick={onInvestigate}
                                className="h-7 text-xs"
                            >
                                <Eye className="h-3 w-3 mr-1" />
                                iu tra
                            </Button>
                        )}
                        <Button
                            size="sm"
                            variant="ghost"
                            onClick={handleDismiss}
                            className="h-7 text-xs"
                        >
                            <X className="h-3 w-3 mr-1" />
                            B qua
                        </Button>
                    </div>
                </div>
            </div>
        </Alert>
    );
}

interface SecurityAlertsProps {
    orderId: number;
    orderNumber: string;
    accessCount: number;
    lastAccessedAt?: string;
    className?: string;
}

export function SecurityAlerts({
    orderId,
    orderNumber,
    accessCount,
    lastAccessedAt,
    className
}: SecurityAlertsProps) {
    const [alerts, setAlerts] = useState<SecurityAlertProps[]>([]);

    // Generate alerts based on access patterns
    const generateAlerts = () => {
        const newAlerts: SecurityAlertProps[] = [];
        const now = new Date();
        const lastAccess = lastAccessedAt ? new Date(lastAccessedAt) : null;

        // High access count alert
        if (accessCount > 10) {
            newAlerts.push({
                type: 'high_access',
                message: `đơn hàng ${orderNumber} đã được truy cập ${accessCount} lần`,
                details: 'Số lần truy cập cao có thể cho thấy hoạt động đáng ngờ',
                timestamp: now.toISOString(),
                severity: accessCount > 20 ? 'critical' : 'high',
                onInvestigate: () => {
                    console.log(`[SECURITY] Investigating high access for order ${orderId}`);
                }
            });
        }

        // Recent access alert
        if (lastAccess && (now.getTime() - lastAccess.getTime()) < 5 * 60 * 1000) {
            newAlerts.push({
                type: 'suspicious_activity',
                message: `đơn hàng ${orderNumber} vừa được truy cập gần đây`,
                details: 'Truy cập trong vòng 5 phút qua có thể cần chú ý',
                timestamp: now.toISOString(),
                severity: 'medium',
                onInvestigate: () => {
                    console.log(`[SECURITY] Investigating recent access for order ${orderId}`);
                }
            });
        }

        // Multiple rapid access alert
        if (accessCount > 5 && lastAccess && (now.getTime() - lastAccess.getTime()) < 30 * 60 * 1000) {
            newAlerts.push({
                type: 'data_breach_risk',
                message: `Nhiều lần truy cập liên tiếp vào đơn hàng ${orderNumber}`,
                details: 'Có thể có rủi ro rò rỉ thông tin khách hàng',
                timestamp: now.toISOString(),
                severity: 'high',
                onInvestigate: () => {
                    console.log(`[SECURITY] Investigating potential data breach for order ${orderId}`);
                }
            });
        }

        setAlerts(newAlerts);
    };

    // Generate alerts on mount
    React.useEffect(() => {
        generateAlerts();
    }, [accessCount, lastAccessedAt]);

    const handleDismissAlert = (index: number) => {
        setAlerts(prev => prev.filter((_, i) => i !== index));
    };

    if (alerts.length === 0) {
        return null;
    }

    return (
        <div className={`space-y-3 ${className}`}>
            {alerts.map((alert, index) => (
                <SecurityAlert
                    key={index}
                    {...alert}
                    onDismiss={() => handleDismissAlert(index)}
                />
            ))}
        </div>
    );
}
