'use client';

import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import {
    ArrowRight,
    Banknote,
    CheckCircle,
    Clock,
    CreditCard,
    Loader2,
    QrCode
} from 'lucide-react';

interface PaymentStep {
    id: string;
    title: string;
    description: string;
    status: 'completed' | 'current' | 'pending' | 'failed';
    icon: React.ComponentType<{ className?: string }>;
}

interface PaymentProgressProps {
    currentStep: string;
    gateway: string;
    method: string;
    isProcessing?: boolean;
    error?: string;
}

export function PaymentProgress({
    currentStep,
    gateway,
    method,
    isProcessing = false,
    error
}: PaymentProgressProps) {
    const getMethodIcon = (method: string) => {
        switch (method.toLowerCase()) {
            case 'card':
            case 'creditcard':
            case 'debitcard':
                return CreditCard;
            case 'qr':
            case 'qrcode':
                return QrCode;
            case 'banktransfer':
            case 'bank_transfer':
                return Banknote;
            default:
                return CreditCard;
        }
    };

    const steps: PaymentStep[] = [
        {
            id: 'init',
            title: 'Khi to',
            description: 'To giao dch thanh ton',
            status: currentStep === 'init' ? 'current' :
                ['processing', 'redirect', 'complete'].includes(currentStep) ? 'completed' : 'pending',
            icon: CheckCircle
        },
        {
            id: 'processing',
            title: 'X l',
            description: isProcessing ? 'ang x l thanh ton...' : 'Ch x l',
            status: currentStep === 'processing' ? 'current' :
                currentStep === 'complete' ? 'completed' : 'pending',
            icon: isProcessing ? Loader2 : Clock
        },
        {
            id: 'redirect',
            title: 'Chuyn hng',
            description: gateway === 'SePay' ? 'Qut m QR  thanh ton' : 'Chuyn n cng thanh ton',
            status: currentStep === 'redirect' ? 'current' :
                currentStep === 'complete' ? 'completed' : 'pending',
            icon: getMethodIcon(method)
        },
        {
            id: 'complete',
            title: 'Hon thnh',
            description: 'Thanh ton thnh cng',
            status: currentStep === 'complete' ? 'completed' : 'pending',
            icon: CheckCircle
        }
    ];

    const getStepStatus = (step: PaymentStep) => {
        if (error && step.id === currentStep) return 'failed';
        return step.status;
    };

    const getStepIcon = (step: PaymentStep) => {
        const Icon = step.icon;
        const status = getStepStatus(step);

        if (status === 'completed') {
            return <Icon className="h-5 w-5 text-green-600" />;
        } else if (status === 'current') {
            return <Icon className={`h-5 w-5 ${isProcessing ? 'animate-spin text-blue-600' : 'text-blue-600'}`} />;
        } else if (status === 'failed') {
            return <Icon className="h-5 w-5 text-red-600" />;
        } else {
            return <Icon className="h-5 w-5 text-gray-400" />;
        }
    };

    const getStepBadgeVariant = (step: PaymentStep) => {
        const status = getStepStatus(step);
        switch (status) {
            case 'completed':
                return 'default';
            case 'current':
                return 'secondary';
            case 'failed':
                return 'destructive';
            default:
                return 'outline';
        }
    };

    const completedSteps = steps.filter(step => getStepStatus(step) === 'completed').length;
    const progress = (completedSteps / steps.length) * 100;

    return (
        <Card className="w-full">
            <CardHeader>
                <CardTitle className="flex items-center gap-2">
                    <CreditCard className="h-5 w-5" />
                    Tin trnh thanh ton
                </CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
                {/* Progress Bar */}
                <div className="space-y-2">
                    <div className="flex justify-between text-sm">
                        <span>Tin  tng th</span>
                        <span>{Math.round(progress)}%</span>
                    </div>
                    <Progress value={progress} className="h-2" />
                </div>

                {/* Steps */}
                <div className="space-y-4">
                    {steps.map((step, index) => (
                        <div key={step.id} className="flex items-start gap-4">
                            {/* Step Icon */}
                            <div className="flex-shrink-0 mt-1">
                                {getStepIcon(step)}
                            </div>

                            {/* Step Content */}
                            <div className="flex-1 min-w-0">
                                <div className="flex items-center gap-2 mb-1">
                                    <h4 className="font-medium text-sm">{step.title}</h4>
                                    <Badge variant={getStepBadgeVariant(step)} className="text-xs">
                                        {getStepStatus(step) === 'completed' ? 'Hon thnh' :
                                            getStepStatus(step) === 'current' ? 'ang x l' :
                                                getStepStatus(step) === 'failed' ? 'Tht bi' : 'Ch x l'}
                                    </Badge>
                                </div>
                                <p className="text-sm text-muted-foreground">
                                    {step.description}
                                </p>
                            </div>

                            {/* Arrow */}
                            {index < steps.length - 1 && (
                                <div className="flex-shrink-0 mt-1">
                                    <ArrowRight className="h-4 w-4 text-gray-400" />
                                </div>
                            )}
                        </div>
                    ))}
                </div>

                {/* Error Message */}
                {error && (
                    <div className="p-3 bg-red-50 border border-red-200 rounded-md">
                        <p className="text-sm text-red-600">
                            <strong>Li:</strong> {error}
                        </p>
                    </div>
                )}

                {/* Gateway Info */}
                <div className="pt-4 border-t">
                    <div className="flex justify-between text-sm">
                        <span className="text-muted-foreground">Cng thanh ton:</span>
                        <span className="font-medium">{gateway}</span>
                    </div>
                    <div className="flex justify-between text-sm">
                        <span className="text-muted-foreground">Phng thc:</span>
                        <span className="font-medium capitalize">
                            {method.replace('_', ' ')}
                        </span>
                    </div>
                </div>
            </CardContent>
        </Card>
    );
}
