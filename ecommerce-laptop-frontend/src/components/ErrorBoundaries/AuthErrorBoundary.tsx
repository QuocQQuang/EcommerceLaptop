'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { AlertCircle, LogIn, RefreshCw } from 'lucide-react';
import Link from 'next/link';
import React from 'react';

interface AuthErrorBoundaryState {
    hasError: boolean;
    error: Error | null;
    errorInfo: React.ErrorInfo | null;
}

interface AuthErrorBoundaryProps {
    children: React.ReactNode;
    fallback?: React.ComponentType<{
        error: Error | null;
        resetErrorBoundary: () => void;
    }>;
}

/**
 * Error boundary component specifically for authentication-related errors
 */
export class AuthErrorBoundary extends React.Component<
    AuthErrorBoundaryProps,
    AuthErrorBoundaryState
> {
    constructor(props: AuthErrorBoundaryProps) {
        super(props);

        this.state = {
            hasError: false,
            error: null,
            errorInfo: null,
        };
    }

    static getDerivedStateFromError(error: Error): AuthErrorBoundaryState {
        return {
            hasError: true,
            error,
            errorInfo: null,
        };
    }

    componentDidCatch(error: Error, errorInfo: React.ErrorInfo) {
        console.error('AuthErrorBoundary caught an error:', error, errorInfo);

        this.setState({
            error,
            errorInfo,
        });

        // Log authentication errors
        if (error.message.includes('session') ||
            error.message.includes('auth') ||
            error.message.includes('token')) {
            console.error('Authentication error detected:', {
                error: error.message,
                stack: error.stack,
                componentStack: errorInfo.componentStack,
            });
        }
    }

    resetErrorBoundary = () => {
        this.setState({
            hasError: false,
            error: null,
            errorInfo: null,
        });
    };

    render() {
        if (this.state.hasError) {
            // Use custom fallback component if provided
            if (this.props.fallback) {
                const FallbackComponent = this.props.fallback;
                return (
                    <FallbackComponent
                        error={this.state.error}
                        resetErrorBoundary={this.resetErrorBoundary}
                    />
                );
            }

            // Default auth error UI
            const isAuthError = this.state.error?.message.includes('session') ||
                this.state.error?.message.includes('auth') ||
                this.state.error?.message.includes('token') ||
                this.state.error?.message.includes('Unauthorized');

            return (
                <div className="min-h-screen bg-gray-50 flex items-center justify-center p-4">
                    <Card className="w-full max-w-md">
                        <CardHeader className="text-center">
                            <div className="mx-auto w-12 h-12 rounded-full bg-red-100 flex items-center justify-center mb-4">
                                <AlertCircle className="w-6 h-6 text-red-600" />
                            </div>
                            <CardTitle className="text-red-600">
                                {isAuthError ? 'Lỗi Xác Thực' : 'Có Lỗi Xảy Ra'}
                            </CardTitle>
                            <CardDescription>
                                {isAuthError
                                    ? 'Phiên đăng nhập của bạn đã hết hạn hoặc không hợp lệ.'
                                    : 'Đã xảy ra lỗi không mong muốn. Vui lòng thử lại.'
                                }
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            {process.env.NODE_ENV === 'development' && this.state.error && (
                                <details className="text-sm text-gray-600 bg-gray-100 p-2 rounded">
                                    <summary>Chi tiết lỗi (Chỉ hiển thị trong development)</summary>
                                    <pre className="mt-2 whitespace-pre-wrap">
                                        {this.state.error.message}
                                    </pre>
                                    {this.state.error.stack && (
                                        <pre className="mt-1 text-xs">
                                            {this.state.error.stack}
                                        </pre>
                                    )}
                                </details>
                            )}

                            <div className="flex flex-col gap-2">
                                <Button
                                    onClick={this.resetErrorBoundary}
                                    className="w-full"
                                    variant="outline"
                                >
                                    <RefreshCw className="w-4 h-4 mr-2" />
                                    Thử Lại
                                </Button>

                                {isAuthError && (
                                    <Button
                                        asChild
                                        className="w-full"
                                    >
                                        <Link href="/auth/login">
                                            <LogIn className="w-4 h-4 mr-2" />
                                            Đăng Nhập Lại
                                        </Link>
                                    </Button>
                                )}

                                <Button
                                    asChild
                                    variant="ghost"
                                    className="w-full"
                                >
                                    <Link href="/">
                                        Về Trang Chủ
                                    </Link>
                                </Button>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            );
        }

        return this.props.children;
    }
}

/**
 * Hook version of the auth error boundary for functional components
 */
export function useAuthErrorHandler() {
    const handleAuthError = React.useCallback((error: Error) => {
        console.error('Auth error handled:', error);

        // Check if it's an auth-related error
        const isAuthError = error.message.includes('session') ||
            error.message.includes('auth') ||
            error.message.includes('token') ||
            error.message.includes('401') ||
            error.message.includes('Unauthorized');

        if (isAuthError) {
            // Clear any stored tokens/sessions
            if (typeof window !== 'undefined') {
                localStorage.removeItem('user');
                sessionStorage.clear();
            }

            // Redirect to login with current page as callback
            const currentPath = window?.location?.pathname || '/';
            const loginUrl = `/auth/login?callbackUrl=${encodeURIComponent(currentPath)}`;
            window?.location?.assign(loginUrl);
        }
    }, []);

    return { handleAuthError };
}