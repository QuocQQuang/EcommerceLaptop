'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Button } from '@/components/ui/button';
import { CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useFormValidation } from '@/hooks/useFormValidation';
import { loginSchema, type LoginFormData } from '@/lib/schemas';
import { motion } from 'framer-motion';
import { AlertCircle, Eye, EyeOff, Mail } from 'lucide-react';
import { getSession, signIn } from 'next-auth/react';
import Link from 'next/link';
import { useState } from 'react';
import { toast } from 'sonner';

const buttonHoverVariants = {
    hover: {
        scale: 1.05,
        boxShadow: '0 0 20px rgba(0, 245, 212, 0.3)'
    }
};

const inputFocusVariants = {
    focus: {
        borderColor: '#00f5d4',
        boxShadow: '0 0 0 3px rgba(0, 245, 212, 0.1)'
    }
};

interface LoginFormProps {
    onSuccess?: () => void;
    callbackUrl?: string;
}

export function LoginForm({ onSuccess, callbackUrl = '/' }: LoginFormProps) {
    const [formData, setFormData] = useState<LoginFormData>({
        email: '',
        password: '',
        rememberMe: false
    });
    const [showPassword, setShowPassword] = useState(false);
    const [loading, setLoading] = useState(false);
    const [attempts, setAttempts] = useState(0);

    const validation = useFormValidation(loginSchema, {
        debounceMs: 300,
        validateOnChange: true,
        validateOnBlur: true
    });

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        const newFormData = {
            ...formData,
            [name]: value
        };
        setFormData(newFormData);
        validation.handleFieldChange(name, value);
    };

    const handleInputBlur = (e: React.FocusEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        validation.handleFieldBlur(name, value);
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        // Validate form before submission
        if (!validation.validateAllFields(formData)) {
                    toast.error('Vui lòng kiểm tra lại thông tin đã nhập');
            return;
        }

        setLoading(true);

        try {
            // Parse and sanitize data with schema
            const cleanData = loginSchema.parse(formData);

            const result = await signIn('credentials', {
                email: cleanData.email,
                password: cleanData.password,
                redirect: false,
                callbackUrl
            });

            if (result?.error) {
                const newAttempts = attempts + 1;
                setAttempts(newAttempts);
                if (result.error.includes('đã khóa') || result.error.toLowerCase().includes('blocked')) {
                    toast.error('IP của bạn đã bị khóa tạm thời do nhập sai quá nhiều. Thử lại sau 15 phút.');
                } else {
                    toast.error('Thông tin đăng nhập không hợp lệ');
                }
                if (newAttempts >= 3) {
                    toast.info('Bạn quên mật khẩu? Khôi phục tại đây.', {
                        duration: 8000,
                        action: {
                            label: 'Khôi phục',
                            onClick: () => window.location.href = '/auth/forgot-password'
                        }
                    });
                }
            } else {
                await getSession();
                if (onSuccess) onSuccess();
            }
        } catch (err) {
            if (err instanceof Error) {
                toast.error(err.message);
            } else {
                toast.error('Máy chủ gặp sự cố, vui lòng thử lại sau.');
            }
        } finally {
            setLoading(false);
        }
    };

    return (
        <>
            <CardHeader className="text-center pb-4">
                <CardTitle className="text-2xl font-bold text-[#f5f5f5]">Đăng nhập</CardTitle>
                <CardDescription className="text-[#9ca3af]">
                    Nhập email và mật khẩu để truy cập tài khoản của bạn
                </CardDescription>
            </CardHeader>

            <form onSubmit={handleSubmit}>
                <CardContent className="space-y-6 pt-4">

                    {/* Email Input */}
                    <div className="space-y-2">
                        <Label htmlFor="email" className="text-sm font-medium text-[#f5f5f5]">Email</Label>
                        <motion.div
                            className="relative"
                            whileFocus={inputFocusVariants}
                        >
                            <Input
                                id="email"
                                name="email"
                                type="email"
                                required
                                value={formData.email}
                                onChange={handleInputChange}
                                onBlur={handleInputBlur}
                                placeholder="example@email.com"
                                className={`h-12 bg-[#1a1a1a] text-[#f5f5f5] placeholder-[#9ca3af] focus:ring-[#00f5d4]/20 transition-all duration-200 pr-12 ${validation.errors.email && validation.touched.email
                                    ? 'border-red-500 focus:border-red-500'
                                    : 'border-[#333] focus:border-[#00f5d4]'
                                    }`}
                                aria-invalid={validation.errors.email && validation.touched.email ? 'true' : 'false'}
                                aria-describedby={validation.errors.email ? 'email-error' : undefined}
                            />
                            <motion.div
                                className="absolute inset-y-0 right-0 pr-3 flex items-center"
                                initial={false}
                                whileHover={{ opacity: 0.7 }}
                            >
                                {validation.errors.email && validation.touched.email ? (
                                    <AlertCircle className="h-4 w-4 text-red-500" />
                                ) : (
                                    <Mail className="h-4 w-4 text-[#9ca3af]" />
                                )}
                            </motion.div>
                        </motion.div>
                        {validation.errors.email && validation.touched.email && (
                            <motion.p
                                id="email-error"
                                initial={{ opacity: 0, y: -10 }}
                                animate={{ opacity: 1, y: 0 }}
                                className="text-sm text-red-500 flex items-center gap-1"
                            >
                                <AlertCircle className="h-3 w-3" />
                                {validation.errors.email}
                            </motion.p>
                        )}
                    </div>

                    {/* Password Input */}
                    <div className="space-y-2">
                        <div className="flex items-center justify-between">
                            <Label htmlFor="password" className="text-sm font-medium text-[#f5f5f5]">Mật khẩu</Label>
                            <Link
                                href="/auth/forgot-password"
                                className="text-sm text-[#00f5d4] hover:text-[#6366f1] transition-colors duration-200"
                            >
                                Quên mật khẩu?
                            </Link>
                        </div>
                        <motion.div
                            className="relative"
                            whileFocus={inputFocusVariants}
                        >
                            <Input
                                id="password"
                                name="password"
                                type={showPassword ? 'text' : 'password'}
                                required
                                value={formData.password}
                                onChange={handleInputChange}
                                onBlur={handleInputBlur}
                                placeholder="Nhập mật khẩu của bạn"
                                className={`h-12 bg-[#1a1a1a] text-[#f5f5f5] placeholder-[#9ca3af] focus:ring-[#00f5d4]/20 transition-all duration-200 pr-12 ${validation.errors.password && validation.touched.password
                                    ? 'border-red-500 focus:border-red-500'
                                    : 'border-[#333] focus:border-[#00f5d4]'
                                    }`}
                                aria-invalid={validation.errors.password && validation.touched.password ? 'true' : 'false'}
                                aria-describedby={validation.errors.password ? 'password-error' : undefined}
                            />
                            <button
                                type="button"
                                className="absolute inset-y-0 right-0 pr-3 flex items-center text-[#9ca3af] hover:text-[#f5f5f5] transition-colors"
                                onClick={() => setShowPassword(!showPassword)}
                            >
                                {showPassword ? (
                                    <EyeOff className="h-4 w-4" />
                                ) : (
                                    <Eye className="h-4 w-4" />
                                )}
                            </button>
                        </motion.div>
                        {validation.errors.password && validation.touched.password && (
                            <motion.p
                                id="password-error"
                                initial={{ opacity: 0, y: -10 }}
                                animate={{ opacity: 1, y: 0 }}
                                className="text-sm text-red-500 flex items-center gap-1"
                            >
                                <AlertCircle className="h-3 w-3" />
                                {validation.errors.password}
                            </motion.p>
                        )}
                    </div>

                    {/* Remember Me */}
                    <div className="flex items-center space-x-2">
                        <Checkbox
                            id="rememberMe"
                            checked={formData.rememberMe}
                            onCheckedChange={(checked: boolean) => setFormData(prev => ({ ...prev, rememberMe: checked }))}
                            className="border-[#333] data-[state=checked]:bg-[#00f5d4]"
                        />
                        <Label htmlFor="rememberMe" className="text-sm text-[#9ca3af] cursor-pointer">
                            Ghi nhớ đăng nhập
                        </Label>
                    </div>
                </CardContent>

                <CardFooter className="flex flex-col space-y-4 pt-6 pb-3">
                    <motion.div variants={buttonHoverVariants} whileHover="hover">
                        <Button
                            type="submit"
                            className="w-full h-12 bg-gradient-to-r from-[#00f5d4] to-[#6366f1] text-[#0a0a0a] font-semibold shadow-lg hover:shadow-[#00f5d4]/25 transition-all duration-200"
                            disabled={loading}
                        >
                            {loading ? (
                                <>
                                    <LoadingSpinner size="sm" className="mr-2" />
                                    đang đăng nhập...
                                </>
                            ) : (
                                'Đăng nhập'
                            )}
                        </Button>
                    </motion.div>
                </CardFooter>
            </form>
        </>
    );
}