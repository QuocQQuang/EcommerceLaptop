'use client';

import { Alert, AlertDescription } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useAdminAuth } from '@/contexts/AdminAuthContext';
import { AlertCircle, Eye, EyeOff, Shield } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';

export default function AdminLoginPage() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState('');
  const [attempts, setAttempts] = useState(0);

  const { login } = useAdminAuth();
  const router = useRouter();

  const MAX_ATTEMPTS = 5;
  const isBlocked = attempts >= MAX_ATTEMPTS;

  const validateForm = () => {
    if (!email || !password) {
      setError('Email và mật khẩu không được để trống');
      return false;
    }

    if (!email.includes('@')) {
      setError('Email không hợp lệ');
      return false;
    }

    if (password.length < 6) {
      setError('Mật khẩu phải có ít nhất 6 ký tự');
      return false;
    }

    return true;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');

    if (isBlocked) {
      setError('Tài khoản đã bị khóa do quá nhiều lần đăng nhập sai. Vui lòng thử lại sau.');
      return;
    }

    if (!validateForm()) {
      return;
    }

    setIsLoading(true);

    try {
      await login({ email: email.trim(), password });
      // Login successful, redirect to admin dashboard
      router.push('/admin/dashboard');
    } catch (error: any) {
      console.error('Login error:', error);
      setAttempts(prev => prev + 1);

      // Handle different error types
      if (error.response?.status === 401) {
        setError('Email hoặc mật khẩu không chính xác');
      } else if (error.response?.status === 429) {
        setError('Quá nhiều lần thử đăng nhập. Vui lòng thử lại sau.');
      } else if (error.response?.status === 403) {
        setError('Tài khoản không có quyền truy cập admin');
      } else {
        setError('Có lỗi xảy ra. Vui lòng thử lại sau.');
      }
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 dark:from-gray-900 dark:to-gray-800 flex items-center justify-center p-4">
      <div className="w-full max-w-md">
        {/* Header */}
        <div className="text-center mb-8">
          <div className="inline-flex items-center justify-center w-16 h-16 bg-blue-600 rounded-full mb-4">
            <Shield className="w-8 h-8 text-white" />
          </div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
            Đăng nhập Admin
          </h1>
          <p className="text-gray-600 dark:text-gray-400 mt-2">
            Truy cập hệ thống quản trị
          </p>
        </div>

        {/* Login Form */}
        <Card className="shadow-xl border-0">
          <CardHeader className="space-y-1 pb-6">
            <CardDescription className="text-center">
            </CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-4">
              {/* Error Alert */}
              {error && (
                <Alert variant="destructive">
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription>{error}</AlertDescription>
                </Alert>
              )}

              {/* Attempt Warning */}
              {attempts > 0 && attempts < MAX_ATTEMPTS && (
                <Alert>
                  <AlertCircle className="h-4 w-4" />
                  <AlertDescription>
                    Còn {MAX_ATTEMPTS - attempts} lần thử trước khi bị khóa
                  </AlertDescription>
                </Alert>
              )}

              {/* Email Field */}
              <div className="space-y-2">
                <Label htmlFor="email">Email admin</Label>
                <Input
                  id="email"
                  type="email"
                  placeholder="admin@company.com"
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  disabled={isLoading || isBlocked}
                  className="h-11"
                  autoComplete="username"
                  required
                  suppressHydrationWarning
                />
              </div>

              {/* Password Field */}
              <div className="space-y-2">
                <Label htmlFor="password">Mật khẩu</Label>
                <div className="relative">
                  <Input
                    id="password"
                    type={showPassword ? 'text' : 'password'}
                    placeholder="Nhập mật khẩu admin"
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    disabled={isLoading || isBlocked}
                    className="h-11 pr-10"
                    autoComplete="current-password"
                    required
                    suppressHydrationWarning
                  />
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                    onClick={() => setShowPassword(!showPassword)}
                    disabled={isLoading || isBlocked}
                  >
                    {showPassword ? (
                      <EyeOff className="h-4 w-4 text-gray-400" />
                    ) : (
                      <Eye className="h-4 w-4 text-gray-400" />
                    )}
                  </Button>
                </div>
              </div>

              {/* Submit Button */}
              <Button
                type="submit"
                className="w-full h-11"
                disabled={isLoading || isBlocked || !email || !password}
              >
                {isLoading ? (
                  <div className="flex items-center gap-2">
                    <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin" />
                    đang xác thực...
                  </div>
                ) : (
                  'Đăng nhập'
                )}
              </Button>
            </form>


            {/* Back to Home */}
            <div className="mt-4 text-center">
              <Link
                href="/"
                className="text-sm text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-gray-100"
              >
                 Quay về trang chủ
              </Link>
            </div>
          </CardContent>
        </Card>

        {/* Test Credentials (Development Only) */}
        {process.env.NODE_ENV === 'development' && (
          <Card className="mt-4 border-dashed">
            <CardContent className="pt-4">
              <p className="text-xs text-gray-500 text-center mb-2">Test Credentials (Dev only):</p>
              <div className="text-xs text-gray-600 space-y-1">
                <p><strong>Email:</strong> admin@company.com</p>
                <p><strong>Password:</strong> Admin123!</p>
              </div>
            </CardContent>
          </Card>
        )}
      </div>
    </div>
  );
}