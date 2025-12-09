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
            setError('Email khng c  trng');
            return;
        }

        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
            setError('Email khng hp l');
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
                setError(' c li xy ra. Vui lng th li.');
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
                                Email  c gi!
                            </h2>
                            <p className="text-gray-600 mb-4">
                                Chng ti  gi link t li mt khu n email ca bn. Vui lng kim tra hp th v lm theo hng dn.
                            </p>
                            <p className="text-sm text-gray-500 mb-6">
                                Khng nhn c email? Kim tra th mc spam hoc th gi li sau 1 pht.
                            </p>
                            <div className="space-y-3">
                                <Button
                                    onClick={() => setSuccess(false)}
                                    variant="outline"
                                    className="w-full"
                                >
                                    Gi li email
                                </Button>
                                <Link href="/auth/login">
                                    <Button variant="ghost" className="w-full">
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
                        Qun mt khu?
                    </h2>
                    <p className="mt-2 text-sm text-gray-600">
                        Nhp email ca bn  nhn link t li mt khu
                    </p>
                </div>

                {/* Reset Password Form */}
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center">
                            <Mail className="h-5 w-5 mr-2" />
                            t li mt khu
                        </CardTitle>
                        <CardDescription>
                            Chng ti s gi link t li mt khu n email ca bn
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
                                    placeholder="Nhp a ch email ca bn"
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
                                        ang gi email...
                                    </>
                                ) : (
                                    <>
                                        <Mail className="h-4 w-4 mr-2" />
                                        Gi link t li mt khu
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