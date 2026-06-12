'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Button } from '@/components/ui/button';
import { CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useFormValidation } from '@/hooks/useFormValidation';
import { isSequential, registerSchema, type RegisterFormData } from '@/lib/schemas';
import { cn } from '@/lib/utils';
import { authService } from '@/services/authService';
import { motion } from 'framer-motion';
import { AlertCircle, Check, CheckCircle, Eye, EyeOff } from 'lucide-react';
import { ChangeEvent, FocusEvent, useState } from 'react';
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

interface RegisterFormProps {
    onSuccess?: () => void;
    callbackUrl?: string;
}

export function RegisterForm({ onSuccess, callbackUrl = '/' }: RegisterFormProps) {
    const [formData, setFormData] = useState<RegisterFormData>({
        firstName: '',
        lastName: '',
        email: '',
        password: '',
        confirmPassword: '',
        phoneNumber: '',
        agreeToTerms: false
    });

    const [showPassword, setShowPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);
    const [loading, setLoading] = useState(false);
    const [success, setSuccess] = useState(false);

    // Use our secure validation hooks
    const validation = useFormValidation(registerSchema, {
        debounceMs: 300,
        validateOnChange: true,
        validateOnBlur: true
    });

    // Enhanced password validation rules
    const passwordRules = [
        { rule: 't nht 8 k t', valid: formData.password.length >= 8 },
        { rule: 'Cha ch ci in hoa', valid: /[A-Z]/.test(formData.password) },
        { rule: 'Cha ch ci thng', valid: /[a-z]/.test(formData.password) },
        { rule: 'Cha s', valid: /[0-9]/.test(formData.password) },
        { rule: 'Cha k t c bit (@$!%*?&)', valid: /[@$!%*?&]/.test(formData.password) },
        { rule: 'Khng cha chui lin tip (e.g., 123)', valid: !isSequential(formData.password) }
    ];

    const handleInputChange = (e: ChangeEvent<HTMLInputElement>) => {
        const { name, value, type, checked } = e.target;
        const newValue = type === 'checkbox' ? checked : value;

        setFormData(prev => ({
            ...prev,
            [name]: newValue
        }));

        // Validate field with new validation system
        validation.validateField(name, newValue);
    };

    const handleInputBlur = (e: FocusEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        validation.markFieldTouched(name);
        validation.validateField(name, value);
    };

    const handleCheckboxChange = (checked: boolean) => {
        setFormData(prev => ({ ...prev, agreeToTerms: checked }));
        validation.validateField('agreeToTerms', checked);
    };



    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        // Validate all fields first
        if (!validation.validateAllFields(formData)) {
            // Mark all fields as touched to show errors
            Object.keys(formData).forEach(field => {
                validation.markFieldTouched(field);
            });
            return;
        }

        setLoading(true);

        try {
            // Parse and sanitize data with schema for security
            const validatedData = registerSchema.parse(formData);

            // Prepare registration data
            const registrationData = {
                firstName: validatedData.firstName,
                lastName: validatedData.lastName,
                email: validatedData.email,
                password: validatedData.password,
                confirmPassword: validatedData.confirmPassword,
                acceptTerms: validatedData.agreeToTerms,
                ...(validatedData.phoneNumber && { phoneNumber: validatedData.phoneNumber })
            };

            await authService.register(registrationData);

            // Show success and prompt user to check email for confirmation
            setSuccess(true);
            toast.success('Đăng ký thành công! Vui lòng kiểm tra email để xác nhận tài khoản.', { duration: 4000 });
            setTimeout(() => {
                if (onSuccess) onSuccess();
            }, 2000);

        } catch (err: any) {
            let errorMessage = 'Đã có lỗi xảy ra. Vui lòng thử lại.';
            if (err.response?.data?.error) {
                errorMessage = err.response.data.error;
            } else if (err.response?.data?.message) {
                errorMessage = err.response.data.message;
            } else if (err.response?.status === 400 || err.response?.status === 409) {
                errorMessage = 'Tài khoản đã tồn tại. Vui lòng đăng nhập.';
            } else if (err.response?.status >= 500) {
                errorMessage = 'Máy chủ gặp sự cố, vui lòng thử lại sau.';
            }
            toast.error(
                <div className="flex items-center gap-2">
                    <AlertCircle className="h-4 w-4 text-red-500" />
                    {errorMessage}
                </div>,
                { duration: 4000 }
            );
        } finally {
            setLoading(false);
        }
    };

    if (success) {
        return (
            <CardContent className="pt-6 text-center">
                <motion.div
                    initial={{ scale: 0 }}
                    animate={{ scale: 1 }}
                    transition={{ duration: 0.5 }}
                >
                    <CheckCircle className="h-16 w-16 text-[#00f5d4] mx-auto mb-4" />
                </motion.div>
                <h2 className="text-2xl font-bold text-[#f5f5f5] mb-2">
                    Đăng ký thành công!
                </h2>
                <p className="text-[#9ca3af] mb-4">
                    Tài khoản của bạn đã được tạo thành công. Vui lòng kiểm tra email để xác nhận tài khoản trước khi đăng nhập.
                </p>
                <LoadingSpinner size="sm" />
            </CardContent>
        );
    }

    return (
        <>
            <CardHeader className="text-center pb-4">
                <CardTitle className="text-2xl font-bold text-[#f5f5f5]">Đăng ký</CardTitle>
                <CardDescription className="text-[#9ca3af]">
                    Điền thông tin dưới đây để tạo tài khoản mới
                </CardDescription>
            </CardHeader>

            <form onSubmit={handleSubmit}>
                <CardContent className="space-y-6 pt-4">

                    {/* Name Fields */}
                    <div className="grid grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <Label htmlFor="firstName" className="text-sm font-medium text-[#f5f5f5]">H *</Label>
                            <motion.div whileFocus={inputFocusVariants}>
                                <Input
                                    id="firstName"
                                    name="firstName"
                                    type="text"
                                    required
                                    value={formData.firstName}
                                    onChange={handleInputChange}
                                    onBlur={handleInputBlur}
                                    placeholder="Nguyn"
                                    className={cn(
                                        "h-12 bg-[#1a1a1a] text-[#f5f5f5] placeholder-[#9ca3af] focus:ring-[#00f5d4]/20 transition-all duration-200",
                                        validation.errors.firstName && validation.touched.firstName
                                            ? "border-red-500 focus:border-red-500"
                                            : "border-[#333] focus:border-[#00f5d4]"
                                    )}
                                    aria-invalid={validation.errors.firstName && validation.touched.firstName ? 'true' : 'false'}
                                    aria-describedby={validation.errors.firstName ? 'firstName-error' : undefined}
                                />
                                {validation.errors.firstName && validation.touched.firstName && (
                                    <motion.p
                                        id="firstName-error"
                                        initial={{ opacity: 0, y: -10 }}
                                        animate={{ opacity: 1, y: 0 }}
                                        className="text-xs text-red-500 mt-1 flex items-center"
                                    >
                                        <AlertCircle className="h-3 w-3 mr-1" />
                                        {validation.errors.firstName}
                                    </motion.p>
                                )}
                            </motion.div>
                        </div>
                        <div className="space-y-2">
                            <Label htmlFor="lastName" className="text-sm font-medium text-[#f5f5f5]">Tn *</Label>
                            <motion.div whileFocus={inputFocusVariants}>
                                <Input
                                    id="lastName"
                                    name="lastName"
                                    type="text"
                                    required
                                    value={formData.lastName}
                                    onChange={handleInputChange}
                                    onBlur={handleInputBlur}
                                    placeholder="Vn A"
                                    className={cn(
                                        "h-12 bg-[#1a1a1a] text-[#f5f5f5] placeholder-[#9ca3af] focus:ring-[#00f5d4]/20 transition-all duration-200",
                                        validation.errors.lastName && validation.touched.lastName
                                            ? "border-red-500 focus:border-red-500"
                                            : "border-[#333] focus:border-[#00f5d4]"
                                    )}
                                    aria-invalid={validation.errors.lastName && validation.touched.lastName ? 'true' : 'false'}
                                    aria-describedby={validation.errors.lastName ? 'lastName-error' : undefined}
                                />
                                {validation.errors.lastName && validation.touched.lastName && (
                                    <motion.p
                                        id="lastName-error"
                                        initial={{ opacity: 0, y: -10 }}
                                        animate={{ opacity: 1, y: 0 }}
                                        className="text-xs text-red-500 mt-1 flex items-center"
                                    >
                                        <AlertCircle className="h-3 w-3 mr-1" />
                                        {validation.errors.lastName}
                                    </motion.p>
                                )}
                            </motion.div>
                        </div>
                    </div>

                    {/* Email Field */}
                    <div className="space-y-2">
                        <Label htmlFor="email" className="text-sm font-medium text-[#f5f5f5]">Email *</Label>
                        <motion.div whileFocus={inputFocusVariants}>
                            <Input
                                id="email"
                                name="email"
                                type="email"
                                required
                                value={formData.email}
                                onChange={handleInputChange}
                                onBlur={handleInputBlur}
                                placeholder="example@email.com"
                                className={cn(
                                    "h-12 bg-[#1a1a1a] text-[#f5f5f5] placeholder-[#9ca3af] focus:ring-[#00f5d4]/20 transition-all duration-200",
                                    validation.errors.email && validation.touched.email
                                        ? "border-red-500 focus:border-red-500"
                                        : "border-[#333] focus:border-[#00f5d4]"
                                )}
                                aria-invalid={validation.errors.email && validation.touched.email ? 'true' : 'false'}
                                aria-describedby={validation.errors.email ? 'email-error' : undefined}
                            />
                            {validation.errors.email && validation.touched.email && (
                                <motion.p
                                    id="email-error"
                                    initial={{ opacity: 0, y: -10 }}
                                    animate={{ opacity: 1, y: 0 }}
                                    className="text-xs text-red-500 mt-1 flex items-center"
                                >
                                    <AlertCircle className="h-3 w-3 mr-1" />
                                    {validation.errors.email}
                                </motion.p>
                            )}
                        </motion.div>
                    </div>

                    {/* Phone Field */}
                    <div className="space-y-2">
                        <Label htmlFor="phoneNumber" className="text-sm font-medium text-[#f5f5f5]">S in thoi</Label>
                        <motion.div whileFocus={inputFocusVariants}>
                            <Input
                                id="phoneNumber"
                                name="phoneNumber"
                                type="tel"
                                value={formData.phoneNumber}
                                onChange={handleInputChange}
                                onBlur={handleInputBlur}
                                placeholder="0901234567"
                                className={cn(
                                    "h-12 bg-[#1a1a1a] text-[#f5f5f5] placeholder-[#9ca3af] focus:ring-[#00f5d4]/20 transition-all duration-200",
                                    validation.errors.phoneNumber && validation.touched.phoneNumber
                                        ? "border-red-500 focus:border-red-500"
                                        : "border-[#333] focus:border-[#00f5d4]"
                                )}
                                aria-invalid={validation.errors.phoneNumber && validation.touched.phoneNumber ? 'true' : 'false'}
                                aria-describedby={validation.errors.phoneNumber ? 'phoneNumber-error' : undefined}
                            />
                            {validation.errors.phoneNumber && validation.touched.phoneNumber && (
                                <motion.p
                                    id="phoneNumber-error"
                                    initial={{ opacity: 0, y: -10 }}
                                    animate={{ opacity: 1, y: 0 }}
                                    className="text-xs text-red-500 mt-1 flex items-center"
                                >
                                    <AlertCircle className="h-3 w-3 mr-1" />
                                    {validation.errors.phoneNumber}
                                </motion.p>
                            )}
                        </motion.div>
                    </div>

                    {/* Password Field */}
                    <div className="space-y-2">
                        <Label htmlFor="password" className="text-sm font-medium text-[#f5f5f5]">Mật khẩu *</Label>
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
                                onBlur={() => validation.handleFieldBlur('password', formData.password)}
                                placeholder="Nhập mật khẩu"
                                className={cn(
                                    "h-12 bg-[#1a1a1a] border-[#333] text-[#f5f5f5] placeholder-[#9ca3af] focus:border-[#00f5d4] focus:ring-[#00f5d4]/20 transition-all duration-200 pr-10",
                                    validation.touched.password && validation.errors.password && "border-red-500 focus:border-red-500"
                                )}
                            />
                            <button
                                type="button"
                                className="absolute inset-y-0 right-0 pr-3 flex items-center text-[#9ca3af] hover:text-[#f5f5f5]"
                                onClick={() => setShowPassword(!showPassword)}
                            >
                                {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                            </button>
                        </motion.div>

                        {/* Password Rules */}
                        {formData.password && (
                            <div className="mt-2 space-y-1">
                                <p className="text-xs text-[#9ca3af]">Mật khẩu phải có:</p>
                                {passwordRules.map((rule, index) => (
                                    <div key={index} className="flex items-center text-xs">
                                        <Check
                                            className={cn(
                                                'h-3 w-3 mr-2',
                                                rule.valid ? 'text-[#00f5d4]' : 'text-[#4b5563]'
                                            )}
                                        />
                                        <span className={rule.valid ? 'text-[#00f5d4]' : 'text-[#9ca3af]'}>
                                            {rule.rule}
                                        </span>
                                    </div>
                                ))}
                            </div>
                        )}
                        {validation.touched.password && validation.errors.password && (
                            <p className="text-xs text-red-500 mt-1 flex items-center">
                                <AlertCircle className="h-3 w-3 mr-1" />
                                {validation.errors.password}
                            </p>
                        )}
                    </div>

                    {/* Confirm Password Field */}
                    <div className="space-y-2">
                        <Label htmlFor="confirmPassword" className="text-sm font-medium text-[#f5f5f5]">Xác nhận mật khẩu *</Label>
                        <motion.div
                            className="relative"
                            whileFocus={inputFocusVariants}
                        >
                            <Input
                                id="confirmPassword"
                                name="confirmPassword"
                                type={showConfirmPassword ? 'text' : 'password'}
                                required
                                value={formData.confirmPassword}
                                onChange={handleInputChange}
                                onBlur={() => validation.handleFieldBlur('confirmPassword', formData.confirmPassword)}
                                placeholder="Nhập lại mật khẩu"
                                className={cn(
                                    "h-12 bg-[#1a1a1a] border-[#333] text-[#f5f5f5] placeholder-[#9ca3af] focus:border-[#00f5d4] focus:ring-[#00f5d4]/20 transition-all duration-200 pr-10",
                                    validation.touched.confirmPassword && validation.errors.confirmPassword && "border-red-500 focus:border-red-500"
                                )}
                            />
                            <button
                                type="button"
                                className="absolute inset-y-0 right-0 pr-3 flex items-center text-[#9ca3af] hover:text-[#f5f5f5]"
                                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                            >
                                {showConfirmPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                            </button>
                        </motion.div>
                        {validation.touched.confirmPassword && validation.errors.confirmPassword && (
                            <p className="text-xs text-red-500 mt-1 flex items-center">
                                <AlertCircle className="h-3 w-3 mr-1" />
                                {validation.errors.confirmPassword}
                            </p>
                        )}
                    </div>

                    {/* Terms Agreement */}
                    <div className="space-y-2">
                        <div className="flex items-start space-x-2">
                            <Checkbox
                                id="agreeToTerms"
                                checked={formData.agreeToTerms}
                                onCheckedChange={(checked: boolean) => {
                                    setFormData(prev => ({ ...prev, agreeToTerms: checked }));
                                    validation.handleFieldBlur('agreeToTerms', checked);
                                }}
                                className={cn(
                                    "border-[#333] data-[state=checked]:bg-[#00f5d4]",
                                    validation.touched.agreeToTerms && validation.errors.agreeToTerms && "border-red-500"
                                )}
                            />
                            <label htmlFor="agreeToTerms" className="text-sm text-[#9ca3af] cursor-pointer leading-relaxed">
                                Tôi đồng ý với{' '}
                                <a href="/terms" className="text-[#00f5d4] hover:text-[#6366f1]">
                                    Điều khoản sử dụng
                                </a>{' '}
                                v{' '}
                                <a href="/privacy" className="text-[#00f5d4] hover:text-[#6366f1]">
                                    Chính sách bảo mật
                                </a>
                            </label>
                        </div>
                        {validation.touched.agreeToTerms && validation.errors.agreeToTerms && (
                            <p className="text-xs text-red-500 flex items-center">
                                <AlertCircle className="h-3 w-3 mr-1" />
                                {validation.errors.agreeToTerms}
                            </p>
                        )}
                    </div>
                </CardContent>

                <CardFooter className="flex flex-col space-y-4 pt-4">
                    <motion.div variants={buttonHoverVariants} whileHover="hover">
                        <Button
                            type="submit"
                            className="w-full h-12 bg-gradient-to-r from-[#00f5d4] to-[#6366f1] text-[#0a0a0a] font-semibold shadow-lg hover:shadow-[#00f5d4]/25 transition-all duration-200"
                            disabled={loading || !formData.agreeToTerms}
                        >
                            {loading ? (
                                <>
                                    <LoadingSpinner size="sm" className="mr-2" />
                                    đang tạo tài khoản...
                                </>
                            ) : (
                                'Tạo tài khoản'
                            )}
                        </Button>
                    </motion.div>
                </CardFooter>
            </form>
        </>
    );
}