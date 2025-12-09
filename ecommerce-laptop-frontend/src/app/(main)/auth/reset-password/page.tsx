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
            setError('Link t li mt khu khng hp l hoc  ht hn');
        }
    }, [token, email]);

    const validatePassword = (password: string) => {
        const minLength = 8;
        const hasUpperCase = /[A-Z]/.test(password);
        const hasLowerCase = /[a-z]/.test(password);
        const hasNumbers = /\d/.test(password);
        const hasSpecialChar = /[!@#$%^&*(),.?":{}|<>]/.test(password);

        if (password.length < minLength) {
            return `Mt khu phi c t nht ${minLength} k t`;
        }
        if (!hasUpperCase) {
            return 'Mt khu phi c t nht 1 ch hoa';
        }
        if (!hasLowerCase) {
            return 'Mt khu phi c t nht 1 ch thng';
        }
        if (!hasNumbers) {
            return 'Mt khu phi c t nht 1 s';
        }
        if (!hasSpecialChar) {
            return 'Mt khu phi c t nht 1 k t c bit';
        }
        return null;
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!token || !email) {
            setError('Link t li mt khu khng hp l');
            return;
        }

        // Validate passwords
        if (!formData.newPassword.trim()) {
            setError('Mt khu mi khng c  trng');
            return;
        }

        if (!formData.confirmPassword.trim()) {
            setError('Xc nhn mt khu khng c  trng');
            return;
        }

        if (formData.newPassword !== formData.confirmPassword) {
            setError('Mt khu xc nhn khng khp');
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
                toast.success('t li mt khu thnh cng!');
            } else {
                setError(response.message || ' c li xy ra. Vui lng th li.');
            }
        } catch (err: any) {
            console.error('Reset password error:', err);
            if (err.response?.data?.error) {
                setError(err.response.data.error);
            } else if (err.response?.data?.message) {
                setError(err.response.data.message);
            } else {
                setError(' c li xy ra. Vui lng th li.');
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
                                t li mt khu thnh cng!
                            </h2>
                            <p className="text-gray-600 mb-4">
                                Mt khu ca bn  c cp nht thnh cng. By gi bn c th ng nhp vi mt khu mi.
                            </p>
                            <div className="space-y-3">
                                <Link href="/auth/login">
                                    <Button className="w-full">
                                        <Lock className="h-4 w-4 mr-2" />
                                        ng nhp ngay
                                    </Button>
                                </Link>
                                <Link href="/">
                                    <Button variant="outline" className="w-full">
                                        V trang ch
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
                                Link khng hp l
                            </h2>
                            <p className="text-gray-600 mb-4">
                                Link t li mt khu khng hp l hoc  ht hn. Vui lng yu cu link mi.
                            </p>
                            <div className="space-y-3">
                                <Link href="/auth/forgot-password">
                                    <Button className="w-full">
                                        Yu cu link mi
                                    </Button>
                                </Link>
                                <Link href="/auth/login">
                                    <Button variant="outline" className="w-full">
                                        <ArrowLeft className="h-4 w-4 mr-2" />
                                        Quay li ng nhp
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
                        t li mt khu
                    </h2>
                    <p className="mt-2 text-sm text-gray-600">
                        Nhp mt khu mi cho ti khon <span className="font-medium">{email}</span>
                    </p>
                </div>

                {/* Reset Password Form */}
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center">
                            <Lock className="h-5 w-5 mr-2" />
                            Mt khu mi
                        </CardTitle>
                        <CardDescription>
                            Mt khu phi c t nht 8 k t, bao gm ch hoa, ch thng, s v k t c bit
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
                                <Label htmlFor="newPassword">Mt khu mi</Label>
                                <div className="relative">
                                    <Input
                                        id="newPassword"
                                        name="newPassword"
                                        type={showPassword ? "text" : "password"}
                                        required
                                        value={formData.newPassword}
                                        onChange={(e) => handleInputChange('newPassword', e.target.value)}
                                        placeholder="Nhp mt khu mi"
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
                                <Label htmlFor="confirmPassword">Xc nhn mt khu</Label>
                                <div className="relative">
                                    <Input
                                        id="confirmPassword"
                                        name="confirmPassword"
                                        type={showConfirmPassword ? "text" : "password"}
                                        required
                                        value={formData.confirmPassword}
                                        onChange={(e) => handleInputChange('confirmPassword', e.target.value)}
                                        placeholder="Nhp li mt khu mi"
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
                                        ang cp nht...
                                    </>
                                ) : (
                                    <>
                                        <Lock className="h-4 w-4 mr-2" />
                                        t li mt khu
                                    </>
                                )}
                            </Button>

                            <Link href="/auth/login">
                                <Button variant="ghost" className="w-full">
                                    <ArrowLeft className="h-4 w-4 mr-2" />
                                    Quay li ng nhp
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
                            Lin h h tr
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
