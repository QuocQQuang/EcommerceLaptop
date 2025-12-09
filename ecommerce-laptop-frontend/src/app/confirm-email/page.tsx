'use client';

import { Alert, AlertDescription } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { AlertTriangle, CheckCircle, Clock, XCircle } from 'lucide-react';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';

interface ConfirmationState {
    status: 'pending' | 'success' | 'error' | 'expired';
    message: string;
    isLoading: boolean;
}

function ConfirmEmailContent() {
    const searchParams = useSearchParams();
    const router = useRouter();
    const [state, setState] = useState<ConfirmationState>({
        status: 'pending',
        message: '',
        isLoading: true
    });

    useEffect(() => {
        const confirmEmail = async () => {
            const token = searchParams.get('token');

            if (!token) {
                setState({
                    status: 'error',
                    message: 'Thiu m xc thc. Vui lng kim tra li email v th lin kt ng.',
                    isLoading: false
                });
                return;
            }

            try {
                // Use the API client for better error handling and logging
                const { authApi } = await import('@/lib/api');
                const data = await authApi.confirmEmail(token);

                if (data.success) {
                    setState({
                        status: 'success',
                        message: data.message || 'Email ca bn  c xc thc thnh cng!',
                        isLoading: false
                    });
                } else {
                    // Determine if it's an expiration error or other error
                    const isExpired = data.message?.toLowerCase().includes('expired') ||
                        data.message?.toLowerCase().includes('invalid');

                    setState({
                        status: isExpired ? 'expired' : 'error',
                        message: data.message || 'Xc thc email tht bi. Vui lng th li.',
                        isLoading: false
                    });
                }
            } catch (error) {
                console.error('Email confirmation error:', error);
                setState({
                    status: 'error',
                    message: ' xy ra li. Vui lng th li sau.',
                    isLoading: false
                });
            }
        };

        confirmEmail();
    }, [searchParams]);

    const handleReturnToLogin = () => {
        router.push('/auth?mode=login');
    };

    const handleReturnToHome = () => {
        router.push('/');
    };

    const handleResendConfirmation = () => {
        router.push('/auth?mode=register&resend=true');
    };

    const renderIcon = () => {
        switch (state.status) {
            case 'pending':
                return <Clock className="h-16 w-16 text-blue-500 animate-pulse" />;
            case 'success':
                return <CheckCircle className="h-16 w-16 text-green-500" />;
            case 'expired':
                return <AlertTriangle className="h-16 w-16 text-orange-500" />;
            case 'error':
                return <XCircle className="h-16 w-16 text-red-500" />;
            default:
                return null;
        }
    };

    const renderContent = () => {
        if (state.isLoading) {
            return (
                <div className="text-center space-y-4">
                    <div className="animate-pulse">
                        <div className="h-4 bg-gray-200 rounded w-3/4 mx-auto mb-2"></div>
                        <div className="h-4 bg-gray-200 rounded w-1/2 mx-auto"></div>
                    </div>
                </div>
            );
        }

        return (
            <div className="space-y-6">
                <Alert className={`${state.status === 'success' ? 'border-green-500 bg-green-50' :
                    state.status === 'expired' ? 'border-orange-500 bg-orange-50' :
                        'border-red-500 bg-red-50'
                    }`}>
                    <AlertDescription className="text-center text-sm">
                        {state.message}
                    </AlertDescription>
                </Alert>

                <div className="flex flex-col space-y-3">
                    {state.status === 'success' && (
                        <>
                            <Button
                                onClick={handleReturnToLogin}
                                className="w-full bg-green-600 hover:bg-green-700"
                            >
                                Tip tc ng nhp
                            </Button>
                            <Button
                                onClick={handleReturnToHome}
                                variant="outline"
                                className="w-full"
                            >
                                V trang ch
                            </Button>
                        </>
                    )}

                    {state.status === 'expired' && (
                        <>
                            <Button
                                onClick={handleResendConfirmation}
                                className="w-full bg-orange-600 hover:bg-orange-700"
                            >
                                Gi li email xc thc
                            </Button>
                            <Button
                                onClick={handleReturnToHome}
                                variant="outline"
                                className="w-full"
                            >
                                V trang ch
                            </Button>
                        </>
                    )}

                    {state.status === 'error' && (
                        <>
                            <Button
                                onClick={handleResendConfirmation}
                                className="w-full"
                            >
                                Th li
                            </Button>
                            <Button
                                onClick={handleReturnToHome}
                                variant="outline"
                                className="w-full"
                            >
                                V trang ch
                            </Button>
                        </>
                    )}
                </div>
            </div>
        );
    };

    return (
        <div className="min-h-screen bg-gray-50 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
            <div className="max-w-md w-full space-y-8">
                <Card className="shadow-lg">
                    <CardHeader className="text-center">
                        <div className="flex justify-center mb-4">
                            {renderIcon()}
                        </div>
                        <CardTitle className="text-2xl font-bold text-gray-900">
                            {state.status === 'pending' && 'Confirming Email...'}
                            {state.status === 'success' && 'Email Confirmed!'}
                            {state.status === 'expired' && 'Link Expired'}
                            {state.status === 'error' && 'Confirmation Failed'}
                        </CardTitle>
                    </CardHeader>
                    <CardContent>
                        {renderContent()}
                    </CardContent>
                </Card>

                {/* Security notice */}
                <div className="text-center text-xs text-gray-500 space-y-2">
                    <p> Your email confirmation is secured with encrypted tokens</p>
                    <p>If you didn't request this confirmation, you can safely ignore this page.</p>
                </div>
            </div>
        </div>
    );
}

export default function ConfirmEmailPage() {
    return (
        <Suspense fallback={<div>Loading...</div>}>
            <ConfirmEmailContent />
        </Suspense>
    )
}