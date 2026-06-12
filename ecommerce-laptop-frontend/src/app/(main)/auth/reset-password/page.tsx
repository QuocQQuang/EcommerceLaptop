'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Alert, AlertDescription } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { authService } from '@/services/authService';
import { AlertCircle, ArrowLeft, CheckCircle, Eye, EyeOff, Lock } from 'lucide-react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { Suspense, useEffect, useState } from 'react';
import { toast } from 'sonner';

function ResetPasswordContent() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const token = searchParams.get('token');
    const email = searchParams.get('email');

    const [formData, setFormData] = useState({
        newPassword: '',
        confirmPassword: ''
    });
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState('');
    const [success, setSuccess] = useState(false);
    const [showPassword, setShowPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);

    // Validate token and email on mount
    useEffect(() => {
        if (!token || !email) {
            setError('Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn');
        }
    }, [token, email]);

    const validatePassword = (password: string) => {
        const minLength = 8;
        const hasUpperCase = /[A-Z]/.test(password);
        const hasLowerCase = /[a-z]/.test(password);
        const hasNumbers = /\d/.test(password);
        const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(password);

        if (password.length < minLength) {
            return `Mật khẩu phải có ít nhất ${minLength} ký tự`;
        }
        if (!hasUpperCase) {
            return 'Mật khẩu phải có ít nhất 1 chữ hoa';
        }
        if (!hasLowerCase) {
            return 'Mật khẩu phải có ít nhất 1 chữ thường';
        }
        if (!hasNumbers) {
            return 'Mật khẩu phải có ít nhất 1 số';
        }
        if (!hasSpecialChar) {
            return 'Mật khẩu phải có ít nhất 1 ký tự đặc biệt';
        }
        return null;
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!token || !email) {
            setError('Link đặt lại mật khẩu không hợp lệ');
            return;
        }

        // Validate passwords
        if (!formData.newPassword.trim()) {
            setError('Mật khẩu mới không được để trống');
            return;
        }

        if (!formData.confirmPassword.trim()) {
            setError('Xác nhận mật khẩu không được để trống');
            return;
        }

        if (formData.newPassword !== formData.confirmPassword) {
            setError('Mật khẩu xác nhận không khớp');
            return;
        }

        const passwordError = validatePassword(formData.newPassword);
        if (passwordError) {
            setError(passwordError);
            return;
        }

        setLoading(true);
        setError('');

        try {
            const response = await authService.resetPassword(email, token, formData.newPassword, formData.confirmPassword);

            if (response.success) {
                setSuccess(true);
                toast.success('Đặt lại mật khẩu thành công!');
            } else {
                setError(response.message || 'Đã có lỗi xảy ra. Vui lòng thử lại.');
            }
        } catch (err: any) {
            console.error('Reset password error:', err);
            if (err.response?.data?.error) {
                setError(err.response.data.error);
            } else if (err.response?.data?.message) {
                setError(err.response.data.message);
            } else {
                setError('Đã có lỗi xảy ra. Vui lòng thử lại.');
            }
        } finally {
            setLoading(false);
        }
    };

    const handleInputChange = (field: string, value: string) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        if (error) setError('');
    };

    if (success) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
                <div className="max-w-md w-full space-y-8">
                    <Card>
                        <CardContent className="pt-6 text-center">
                            <CheckCircle className="h-16 w-16 text-green-500 mx-auto mb-4" />
                            <h2 className="text-2xl font-bold text-gray-900 mb-2">
                                Đặt lại mật khẩu thành công!
                            </h2>
                            <p className="text-gray-600 mb-4">
                                Mật khẩu của bạn đã được cập nhật thành công. Bây giờ bạn có thể đăng nhập với mật khẩu mới.
                            </p>
                            <div className="space-y-3">
                                <Link href="/auth/login">
                                    <Button className="w-full">
                                        <Lock className="h-4 w-4 mr-2" />
                                        Đăng nhập ngay
                                    </Button>
                                </Link>
                                <Link href="/">
                                    <Button variant="outline" className="w-full">
                                        Về trang chủ
                                    </Button>
                                </Link>
                            </div>
                        </CardContent>
                    </Card>
                </div>
            </div>
        );
    }

    if (!token || !email) {
        return (
            <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
                <div className="max-w-md w-full space-y-8">
                    <Card>
                        <CardContent className="pt-6 text-center">
                            <AlertCircle className="h-16 w-16 text-red-500 mx-auto mb-4" />
                            <h2 className="text-2xl font-bold text-gray-900 mb-2">
                                Link không hợp lệ
                            </h2>
                            <p className="text-gray-600 mb-4">
                                Link đặt lại mật khẩu không hợp lệ hoặc đã hết hạn. Vui lòng yêu cầu link mới.
                            </p>
                            <div className="space-y-3">
                                <Link href="/auth/forgot-password">
                                    <Button className="w-full">
                                        Yêu cầu link mới
                                    </Button>
                                </Link>
                                <Link href="/auth/login">
                                    <Button variant="outline" className="w-full">
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
                        Đặt lại mật khẩu
                    </h2>
                    <p className="mt-2 text-sm text-gray-600">
                        Nhập mật khẩu mới cho tài khoản <span className="font-medium">{email}</span>
                    </p>
                </div>

                {/* Reset Password Form */}
                <Card>
                    <CardHeader>
                            <CardTitle className="flex items-center">
                                <Lock className="h-5 w-5 mr-2" />
                                Mật khẩu mới
                            </CardTitle>
                            <CardDescription>
                                Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường, số và ký tự đặc biệt
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

                            {/* New Password Field */}
                            <div className="space-y-2">
                                <Label htmlFor="newPassword">Mật khẩu mới</Label>
                                <div className="relative">
                                    <Input
                                        id="newPassword"
                                        name="newPassword"
                                        type={showPassword ? "text" : "password"}
                                        required
                                        value={formData.newPassword}
                                        onChange={(e) => handleInputChange('newPassword', e.target.value)}
                                        placeholder="Nhập mật khẩu mới"
                                        disabled={loading}
                                        className="pr-10"
                                    />
                                    <button
                                        type="button"
                                        className="absolute inset-y-0 right-0 pr-3 flex items-center"
                                        onClick={() => setShowPassword(!showPassword)}
                                        disabled={loading}
                                    >
                                        {showPassword ? (
                                            <EyeOff className="h-4 w-4 text-gray-400" />
                                        ) : (
                                            <Eye className="h-4 w-4 text-gray-400" />
                                        )}
                                    </button>
                                </div>
                            </div>

                            {/* Confirm Password Field */}
                            <div className="space-y-2">
                                <Label htmlFor="confirmPassword">Xác nhận mật khẩu</Label>
                                <div className="relative">
                                    <Input
                                        id="confirmPassword"
                                        name="confirmPassword"
                                        type={showConfirmPassword ? "text" : "password"}
                                        required
                                        value={formData.confirmPassword}
                                        onChange={(e) => handleInputChange('confirmPassword', e.target.value)}
                                        placeholder="Nhập lại mật khẩu mới"
                                        disabled={loading}
                                        className="pr-10"
                                    />
                                    <button
                                        type="button"
                                        className="absolute inset-y-0 right-0 pr-3 flex items-center"
                                        onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                                        disabled={loading}
                                    >
                                        {showConfirmPassword ? (
                                            <EyeOff className="h-4 w-4 text-gray-400" />
                                        ) : (
                                            <Eye className="h-4 w-4 text-gray-400" />
                                        )}
                                    </button>
                                </div>
                            </div>
                        </CardContent>

                        <CardFooter className="flex flex-col space-y-3">
                            <Button
                                type="submit"
                                className="w-full"
                                disabled={loading || !formData.newPassword.trim() || !formData.confirmPassword.trim()}
                            >
                                {loading ? (
                                    <>
                                        <LoadingSpinner size="sm" className="mr-2" />
                                        Đang cập nhật...
                                    </>
                                ) : (
                                    <>
                                        <Lock className="h-4 w-4 mr-2" />
                                        Đặt lại mật khẩu
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
                        Gp vn ? {' '}
                        <Link href="/contact" className="text-blue-600 hover:text-blue-500">
                            Liên hệ hỗ trợ
                        </Link>
                    </p>
                </div>
            </div>
        </div>
    );
}

export default function ResetPasswordPage() {
    return (
        <Suspense fallback={
            <div className="min-h-screen flex items-center justify-center">
                <LoadingSpinner size="lg" />
            </div>
        }>
            <ResetPasswordContent />
        </Suspense>
    );
}
