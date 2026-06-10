'use client';

import { Alert, AlertDescription } from '@/components/ui/alert';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { userService } from '@/services/userService';
import { AlertCircle, CheckCircle, Eye, EyeOff, Lock, Shield } from 'lucide-react';
import { useState } from 'react';
import { toast } from 'sonner';

interface ChangePasswordFormProps {
    onSuccess?: () => void;
    onCancel?: () => void;
}

interface PasswordStrength {
    score: number;
    feedback: string[];
    isValid: boolean;
}

export function ChangePasswordForm({ onSuccess, onCancel }: ChangePasswordFormProps) {
    const [formData, setFormData] = useState({
        currentPassword: '',
        newPassword: '',
        confirmPassword: ''
    });
    const [showPasswords, setShowPasswords] = useState({
        current: false,
        new: false,
        confirm: false
    });
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState('');
    const [passwordStrength, setPasswordStrength] = useState<PasswordStrength>({
        score: 0,
        feedback: [],
        isValid: false
    });

    const validatePasswordStrength = (password: string): PasswordStrength => {
        const feedback: string[] = [];
        let score = 0;

        if (password.length < 8) {
            feedback.push('Mật khẩu phải có ít nhất 8 ký tự');
        } else {
            score += 1;
        }

        if (password.length > 128) {
            feedback.push('Mật khẩu không được vượt quá 128 ký tự');
        } else if (password.length >= 8) {
            score += 1;
        }

        // Check for character variety
        const hasLower = /[a-z]/.test(password);
        const hasUpper = /[A-Z]/.test(password);
        const hasDigit = /\d/.test(password);
        const hasSpecial = /[!@#$%^&*()_+\-=\[\]{}|;:,.<>?]/.test(password);

        const categoryCount = [hasLower, hasUpper, hasDigit, hasSpecial].filter(Boolean).length;

        if (categoryCount >= 3) {
            score += 2;
        } else {
            feedback.push('Mật khẩu phải chứa ít nhất 3 trong 4 loại: chữ thường, chữ hoa, số, ký tự đặc biệt');
        }

        // Check for common patterns
        if (password === password[0]?.repeat(password.length)) {
            feedback.push('Mật khẩu không được chứa tất cả ký tự giống nhau');
        } else {
            score += 1;
        }

        // Check for sequential characters
        const isSequential = /(?:012|123|234|345|456|567|678|789|890|987|876|765|654|543|432|321|210)/.test(password);
        if (isSequential) {
            feedback.push('Mật khẩu không được chứa chuỗi ký tự liên tiếp');
        } else {
            score += 1;
        }

        // Check for common passwords
        const commonPasswords = [
            'password', '123456', '123456789', 'qwerty', 'abc123', 'password123',
            'admin', 'letmein', 'welcome', 'monkey', '1234567890', 'dragon',
            'master', 'hello', 'freedom', 'whatever', 'qazwsx', 'trustno1'
        ];

        if (commonPasswords.some(common => password.toLowerCase() === common)) {
            feedback.push('Mật khẩu quá phổ biến, vui lòng chọn mật khẩu mạnh hơn');
        } else {
            score += 1;
        }

        return {
            score,
            feedback,
            isValid: feedback.length === 0 && score >= 4
        };
    };

    const handlePasswordChange = (field: keyof typeof formData, value: string) => {
        setFormData(prev => ({ ...prev, [field]: value }));
        setError('');

        if (field === 'newPassword') {
            setPasswordStrength(validatePasswordStrength(value));
        }
    };

    const togglePasswordVisibility = (field: keyof typeof showPasswords) => {
        setShowPasswords(prev => ({ ...prev, [field]: !prev[field] }));
    };

    const getPasswordStrengthColor = (score: number) => {
        if (score < 2) return 'text-red-500';
        if (score < 4) return 'text-yellow-500';
        return 'text-green-500';
    };

    const getPasswordStrengthText = (score: number) => {
        if (score < 2) return 'Rất yếu';
        if (score < 4) return 'Trung bình';
        return 'Mạnh';
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setError('');

        // Client-side validation
        if (!formData.currentPassword) {
            setError('Vui lòng nhập mật khẩu hiện tại');
            return;
        }

        if (!formData.newPassword) {
            setError('Vui lòng nhập mật khẩu mới');
            return;
        }

        if (!passwordStrength.isValid) {
            setError('Mật khẩu mới không đủ mạnh');
            return;
        }

        if (formData.newPassword !== formData.confirmPassword) {
            setError('Mật khẩu xác nhận không khớp');
            return;
        }

        if (formData.currentPassword === formData.newPassword) {
            setError('Mật khẩu mới phải khác với mật khẩu hiện tại');
            return;
        }

        setIsLoading(true);

        try {
            await userService.changePassword({
                currentPassword: formData.currentPassword,
                newPassword: formData.newPassword,
                confirmPassword: formData.confirmPassword
            });

            toast.success('Mật khẩu đã được thay đổi thành công');

            // Reset form
            setFormData({
                currentPassword: '',
                newPassword: '',
                confirmPassword: ''
            });
            setPasswordStrength({ score: 0, feedback: [], isValid: false });

            onSuccess?.();
        } catch (err: any) {
            const errorMessage = err.response?.data?.message || 'Có lỗi xảy ra khi thay đổi mật khẩu';
            setError(errorMessage);
            toast.error(errorMessage);
        } finally {
            setIsLoading(false);
        }
    };

    return (
        <Card className="w-full max-w-md mx-auto shadow-lg overflow-hidden">
            <CardHeader className="text-center pb-6">
                <div className="mx-auto mb-4 flex h-12 w-12 items-center justify-center rounded-full bg-blue-100">
                    <Shield className="h-6 w-6 text-blue-600" />
                </div>
                <CardTitle className="text-xl font-semibold text-gray-900">Đổi mật khẩu</CardTitle>
                <CardDescription className="text-gray-600">
                    Để bảo mật tài khoản, hãy chọn mật khẩu mạnh và duy nhất
                </CardDescription>
            </CardHeader>
            <CardContent className="px-6 pb-6">
                <form onSubmit={handleSubmit} className="space-y-6 w-full">
                    {error && (
                        <Alert variant="destructive">
                            <AlertCircle className="h-4 w-4" />
                            <AlertDescription>{error}</AlertDescription>
                        </Alert>
                    )}

                    {/* Current Password */}
                    <div className="space-y-2 w-full">
                        <Label htmlFor="currentPassword" className="text-sm font-medium text-gray-700 block">
                            Mật khẩu hiện tại
                        </Label>
                        <div className="relative w-full">
                            <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
                            <Input
                                id="currentPassword"
                                type={showPasswords.current ? 'text' : 'password'}
                                value={formData.currentPassword}
                                onChange={(e) => handlePasswordChange('currentPassword', e.target.value)}
                                className="pl-10 pr-10 h-11 w-full border-gray-300 focus:border-blue-500 focus:ring-blue-500"
                                placeholder="Nhập mật khẩu hiện tại"
                                required
                            />
                            <Button
                                type="button"
                                variant="ghost"
                                size="sm"
                                className="absolute right-0 top-0 h-11 w-11 p-0 hover:bg-gray-100 rounded-r-md"
                                onClick={() => togglePasswordVisibility('current')}
                            >
                                {showPasswords.current ? (
                                    <EyeOff className="h-4 w-4 text-gray-500" />
                                ) : (
                                    <Eye className="h-4 w-4 text-gray-500" />
                                )}
                            </Button>
                        </div>
                    </div>

                    {/* New Password */}
                    <div className="space-y-2 w-full">
                        <Label htmlFor="newPassword" className="text-sm font-medium text-gray-700 block">
                            Mật khẩu mới
                        </Label>
                        <div className="relative w-full">
                            <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
                            <Input
                                id="newPassword"
                                type={showPasswords.new ? 'text' : 'password'}
                                value={formData.newPassword}
                                onChange={(e) => handlePasswordChange('newPassword', e.target.value)}
                                className="pl-10 pr-10 h-11 w-full border-gray-300 focus:border-blue-500 focus:ring-blue-500"
                                placeholder="Nhập mật khẩu mới"
                                required
                            />
                            <Button
                                type="button"
                                variant="ghost"
                                size="sm"
                                className="absolute right-0 top-0 h-11 w-11 p-0 hover:bg-gray-100 rounded-r-md"
                                onClick={() => togglePasswordVisibility('new')}
                            >
                                {showPasswords.new ? (
                                    <EyeOff className="h-4 w-4 text-gray-500" />
                                ) : (
                                    <Eye className="h-4 w-4 text-gray-500" />
                                )}
                            </Button>
                        </div>

                        {/* Password Strength Indicator */}
                        {formData.newPassword && (
                            <div className="mt-3 p-3 bg-gray-50 rounded-md border w-full max-w-full overflow-hidden">
                                <div className="flex items-center justify-between text-sm mb-2">
                                    <span className="text-gray-600 font-medium">Độ mạnh mật khẩu:</span>
                                    <span className={`font-semibold ${getPasswordStrengthColor(passwordStrength.score)}`}>
                                        {getPasswordStrengthText(passwordStrength.score)}
                                    </span>
                                </div>
                                <div className="w-full bg-gray-200 rounded-full h-2 overflow-hidden">
                                    <div
                                        className={`h-2 rounded-full transition-all duration-300 ${passwordStrength.score < 2
                                            ? 'bg-red-500'
                                            : passwordStrength.score < 4
                                                ? 'bg-yellow-500'
                                                : 'bg-green-500'
                                            }`}
                                        style={{
                                            width: `${Math.min(Math.max((passwordStrength.score / 6) * 100, 0), 100)}%`,
                                            maxWidth: '100%'
                                        }}
                                    />
                                </div>
                                {passwordStrength.feedback.length > 0 && (
                                    <div className="text-sm text-red-600 space-y-1 mt-2 max-w-full">
                                        {passwordStrength.feedback.map((feedback, index) => (
                                            <div key={index} className="flex items-start gap-2 max-w-full">
                                                <AlertCircle className="h-3 w-3 flex-shrink-0 mt-0.5" />
                                                <span className="text-xs leading-relaxed break-words flex-1 min-w-0">{feedback}</span>
                                            </div>
                                        ))}
                                    </div>
                                )}
                            </div>
                        )}
                    </div>

                    {/* Confirm Password */}
                    <div className="space-y-2 w-full">
                        <Label htmlFor="confirmPassword" className="text-sm font-medium text-gray-700 block">
                            Xác nhận mật khẩu mới
                        </Label>
                        <div className="relative w-full">
                            <Lock className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
                            <Input
                                id="confirmPassword"
                                type={showPasswords.confirm ? 'text' : 'password'}
                                value={formData.confirmPassword}
                                onChange={(e) => handlePasswordChange('confirmPassword', e.target.value)}
                                className="pl-10 pr-10 h-11 w-full border-gray-300 focus:border-blue-500 focus:ring-blue-500"
                                placeholder="Nhập lại mật khẩu mới"
                                required
                            />
                            <Button
                                type="button"
                                variant="ghost"
                                size="sm"
                                className="absolute right-0 top-0 h-11 w-11 p-0 hover:bg-gray-100 rounded-r-md"
                                onClick={() => togglePasswordVisibility('confirm')}
                            >
                                {showPasswords.confirm ? (
                                    <EyeOff className="h-4 w-4 text-gray-500" />
                                ) : (
                                    <Eye className="h-4 w-4 text-gray-500" />
                                )}
                            </Button>
                        </div>
                        {formData.confirmPassword && formData.newPassword === formData.confirmPassword && (
                            <div className="flex items-center gap-2 text-sm text-green-600 mt-1">
                                <CheckCircle className="h-4 w-4 flex-shrink-0" />
                                <span>Mật khẩu xác nhận khớp</span>
                            </div>
                        )}
                    </div>

                    {/* Security Tips */}
                    <div className="bg-blue-50 border border-blue-200 p-4 rounded-lg">
                        <h4 className="font-medium text-blue-900 mb-3 flex items-center gap-2">
                            <Shield className="h-4 w-4" />
                            Yêu cầu mật khẩu:
                        </h4>
                        <ul className="text-sm text-blue-800 space-y-2">
                            <li className="flex items-start gap-2">
                                <span className="text-blue-600 mt-0.5"></span>
                                <span>Sử dụng ít nhất 8 ký tự</span>
                            </li>
                            <li className="flex items-start gap-2">
                                <span className="text-blue-600 mt-0.5"></span>
                                <span>Kết hợp chữ hoa, chữ thường, số và ký tự đặc biệt</span>
                            </li>
                            <li className="flex items-start gap-2">
                                <span className="text-blue-600 mt-0.5"></span>
                                <span>Tránh thông tin cá nhân dễ đoán</span>
                            </li>
                            <li className="flex items-start gap-2">
                                <span className="text-blue-600 mt-0.5"></span>
                                <span>Không sử dụng mật khẩu đã dùng trước đây</span>
                            </li>
                        </ul>
                    </div>

                    {/* Action Buttons */}
                    <div className="flex gap-3 pt-4">
                        <Button
                            type="button"
                            variant="outline"
                            onClick={onCancel}
                            className="flex-1"
                            disabled={isLoading}
                        >
                            Hủy
                        </Button>
                        <Button
                            type="submit"
                            className="flex-1"
                            disabled={isLoading || !passwordStrength.isValid || formData.newPassword !== formData.confirmPassword}
                        >
                            {isLoading ? 'Đang xử lý...' : 'Đổi mật khẩu'}
                        </Button>
                    </div>
                </form>
            </CardContent>
        </Card>
    );
}
