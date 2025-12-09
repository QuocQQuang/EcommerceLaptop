'use client';

import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { Separator } from '@/components/ui/separator';
import { AlertTriangle, Clock, Eye, Shield, User } from 'lucide-react';

interface SecurityAuditCardProps {
    lastAccessedBy?: string;
    lastAccessedAt?: string;
    accessCount?: number;
    orderId: number;
    orderNumber: string;
    className?: string;
}

export function SecurityAuditCard({
    lastAccessedBy,
    lastAccessedAt,
    accessCount = 0,
    orderId,
    orderNumber,
    className
}: SecurityAuditCardProps) {
    const formatDate = (dateString: string) => {
        return new Intl.DateTimeFormat('vi-VN', {
            year: 'numeric',
            month: '2-digit',
            day: '2-digit',
            hour: '2-digit',
            minute: '2-digit'
        }).format(new Date(dateString));
    };

    const getSecurityLevel = (count: number) => {
        if (count === 0) return { level: 'LOW', color: 'bg-green-100 text-green-800', icon: Shield };
        if (count < 5) return { level: 'MEDIUM', color: 'bg-yellow-100 text-yellow-800', icon: Eye };
        return { level: 'HIGH', color: 'bg-red-100 text-red-800', icon: AlertTriangle };
    };

    const securityInfo = getSecurityLevel(accessCount);
    const SecurityIcon = securityInfo.icon;

    return (
        <Card className={className}>
            <CardHeader>
                <CardTitle className="flex items-center gap-2">
                    <Shield className="h-5 w-5" />
                    Bo mt & Audit
                    <Badge variant="outline" className="ml-auto">
                        ID: {orderId}
                    </Badge>
                </CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
                {/* Security Level Indicator */}
                <div className="flex items-center justify-between p-3 rounded-lg bg-muted">
                    <div className="flex items-center gap-2">
                        <SecurityIcon className="h-4 w-4" />
                        <span className="text-sm font-medium">Mc  truy cp</span>
                    </div>
                    <Badge className={securityInfo.color}>
                        {securityInfo.level}
                    </Badge>
                </div>

                {/* Access Statistics */}
                <div className="grid grid-cols-2 gap-4 text-sm">
                    <div>
                        <Label className="text-xs font-medium text-muted-foreground">Truy cp cui</Label>
                        <p className="font-medium flex items-center gap-1">
                            <User className="h-3 w-3" />
                            {lastAccessedBy || 'N/A'}
                        </p>
                    </div>
                    <div>
                        <Label className="text-xs font-medium text-muted-foreground">Thi gian</Label>
                        <p className="text-xs flex items-center gap-1">
                            <Clock className="h-3 w-3" />
                            {lastAccessedAt ? formatDate(lastAccessedAt) : 'N/A'}
                        </p>
                    </div>
                    <div>
                        <Label className="text-xs font-medium text-muted-foreground">S ln truy cp</Label>
                        <p className="font-medium">{accessCount}</p>
                    </div>
                    <div>
                        <Label className="text-xs font-medium text-muted-foreground">M n hng</Label>
                        <p className="font-mono text-xs">{orderNumber}</p>
                    </div>
                </div>

                <Separator />

                {/* Security Warnings */}
                {accessCount > 10 && (
                    <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
                        <div className="flex items-center gap-2 text-red-800">
                            <AlertTriangle className="h-4 w-4" />
                            <span className="text-sm font-medium">Cnh bo bo mt</span>
                        </div>
                        <p className="text-xs text-red-700 mt-1">
                            n hng ny  c truy cp qu nhiu ln. Vui lng kim tra tnh hp l.
                        </p>
                    </div>
                )}

                {accessCount === 0 && (
                    <div className="p-3 bg-green-50 border border-green-200 rounded-lg">
                        <div className="flex items-center gap-2 text-green-800">
                            <Shield className="h-4 w-4" />
                            <span className="text-sm font-medium">Bo mt tt</span>
                        </div>
                        <p className="text-xs text-green-700 mt-1">
                            n hng cha c truy cp trc . Thng tin c bo v tt.
                        </p>
                    </div>
                )}

                {/* Security Notice */}
                <div className="text-xs text-muted-foreground space-y-1">
                    <div className="flex items-center gap-1">
                        <Shield className="h-3 w-3" />
                        <span>Thng tin nhy cm - Ch dnh cho Admin</span>
                    </div>
                    <p>Mi thao tc u c ghi li  m bo bo mt.</p>
                    <p>Khng chia s thng tin ny vi ngi khng c quyn.</p>
                </div>
            </CardContent>
        </Card>
    );
}
