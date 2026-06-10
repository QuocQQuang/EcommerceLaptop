'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { authService } from '@/services/authService';
import { AlertCircle, ArrowLeft, CheckCircle, Mail } from 'lucide-react';
import Link from 'next/link';
import { useState } from 'react';

export default function ForgotPasswordPage() {
    const [email, setEmail] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!email.trim()) {
            setError('Email không được để trống');
            return;
        }

        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            setError('Email không hợp lệ');
            return;
        }

        setLoading(true);
        setError('');

        try {
            const response = await authService.forgotPassword(email);
            setSuccess(true);
        } catch (err: any) {
            if (err.response?.data?.message) {
                setError(err.response.data.message);
            } else {
                setError('Đã có lỗi xảy ra. Vui lòng thử lại.');
            }
        } finally {
            setLoading(false);
        }
    };

    if (success) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
                <div className="max-w-md w-full space-y-8">
                    <Card>
                        <CardContent className="pt-6 text-center">
                            <CheckCircle className="h-16 w-16 text-green-500 mx-auto mb-4" />
                            <h2 className="text-2xl font-bold text-gray-900 mb-2">
                                Email đã được gửi!
                            </h2>
                            <p className="text-gray-600 mb-4">
                                Chúng tôi đã gửi link đặt lại mật khẩu đến email của bạn. Vui lòng kiểm tra hộp thư và làm theo hướng dẫn.
                            </p>
                            <p className="text-sm text-gray-500 mb-6">
                                Không nhận được email? Kiểm tra thư mục spam hoặc thử gửi lại sau 1 phút.
                            </p>
                            <div className="space-y-3">
                                <Button
                                    onClick={() => setSuccess(false)}
                                    variant="outline"
                                    className="w-full"
                                >
                                    Gửi lại email
                                </Button>
                                <Link href="/auth/login">
                                    <Button variant="ghost" className="w-full">
                                        <ArrowLeft className="h-4 w-4 mr-2" />
                                        Quay lại đăng nhập
                                    </Button>
                                </Link>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            </div>
        );
    }

    return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
            <div className="max-w-md w-full space-y-8">
                {/* Header */}
                <div className="text-center">
                    <h2 className="mt-6 text-3xl font-bold text-gray-900">
                        Quên mật khẩu?
                    </h2>
                    <p className="mt-2 text-sm text-gray-600">
                        Nhập email của bạn để nhận link đặt lại mật khẩu
                    </p>
                </div>

                {/* Reset Password Form */}
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center">
                            <Mail className="h-5 w-5 mr-2" />
                            Đặt lại mật khẩu
                        </CardTitle>
                        <CardDescription>
                            Chúng tôi sẽ gửi link đặt lại mật khẩu đến email của bạn
                        </CardDescription>
                    </CardHeader>

                    <form onSubmit={handleSubmit}>
                        <CardContent className="space-y-4">
                            {/* Error Alert */}
                            {error && (
                                <Alert variant="destructive">
                                    <AlertCircle className="h-4 w-4" />
                                    <AlertDescription>{error}</AlertDescription>
                                </Alert>
                            )}

                            {/* Email Field */}
                            <div className="space-y-2">
                                <Label htmlFor="email">Email</Label>
                                <Input
                                    id="email"
                                    name="email"
                                    type="email"
                                    required
                                    value={email}
                                    onChange={(e) => {
                                        setEmail(e.target.value);
                                        if (error) setError('');
                                    }}
                                    placeholder="Nhập địa chỉ email của bạn"
                                    disabled={loading}
                                />
                            </div>
                        </CardContent>

                        <CardFooter className="flex flex-col space-y-3">
                            <Button
                                type="submit"
                                className="w-full"
                                disabled={loading || !email.trim()}
                            >
                                {loading ? (
                                    <>
                                        <LoadingSpinner size="sm" className="mr-2" />
                                        đang gửi email...
                                    </>
                                ) : (
                                    <>
                                        <Mail className="h-4 w-4 mr-2" />
                                        Gửi link đặt lại mật khẩu
                                    </>
                                )}
                            </Button>

                            <Link href="/auth/login">
                                <Button variant="ghost" className="w-full">
                                    <ArrowLeft className="h-4 w-4 mr-2" />
                                    Quay lại đăng nhập
                                </Button>
                            </Link>
                        </CardFooter>
                    </form>
                </Card>

                {/* Additional Help */}
                <div className="text-center">
                    <p className="text-sm text-gray-600">
                        Gặp vấn đề? {' '}
                        <Link href="/contact" className="text-blue-600 hover:text-blue-500">
                            Liên hệ hỗ trợ
                        </Link>
                    </p>
                </div>
            </div>
        </div>
    );
}