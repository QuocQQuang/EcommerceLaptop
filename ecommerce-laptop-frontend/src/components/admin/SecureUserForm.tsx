/**
 * Secure User Form Component
 * Enhanced user creation/editing form with comprehensive security validation
 */

'use client';

import { Alert, AlertDescription } from '@/components/ui/alert';
import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
    Card,
    CardContent,
    CardHeader,
    CardTitle
} from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { Switch } from '@/components/ui/switch';
import { cn } from '@/lib/utils';
import { zodResolver } from '@hookform/resolvers/zod';
import {
    AlertTriangle,
    CheckCircle,
    Lock,
    Mail,
    Save,
    Shield,
    User,
    UserPlus
} from 'lucide-react';
import { useCallback, useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';
import {
    SecureFormWrapper,
    SecureInput,
    SecureTextarea,
    SECURITY_CONTEXTS,
    SecurityValidationResult
} from './SecureForm';

// Enhanced Vietnamese phone validation
const vietnamesePhoneRegex = /^(0|\+84)(3[2-9]|5[689]|7[06-9]|8[1-689]|9[0-46-9])[0-9]{7}$/;

// Enhanced user schema with security validation
const secureUserSchema = z.object({
    firstName: z.string()
        .min(1, 'Họ là bắt buộc')
        .min(2, 'Họ phải có ít nhất 2 ký tự')
        .max(50, 'Họ không được quá 50 ký tự')
        .regex(/^[\p{L}\p{M}\s.'-]+$/u, 'Họ chỉ được chứa chữ cái, dấu cách và các ký tự đặc biệt hợp lệ')
        .refine((val) => !/[<>\"'&]/.test(val), 'Họ chứa ký tự không an toàn'),

    lastName: z.string()
        .min(1, 'Tên là bắt buộc')
        .min(2, 'Tên phải có ít nhất 2 ký tự')
        .max(50, 'Tên không được quá 50 ký tự')
        .regex(/^[\p{L}\p{M}\s.'-]+$/u, 'Tên chỉ được chứa chữ cái, dấu cách và các ký tự đặc biệt hợp lệ')
        .refine((val) => !/[<>\"'&]/.test(val), 'Tên chứa ký tự không an toàn'),

    email: z.string()
        .min(1, 'Email là bắt buộc')
        .email('Email không hợp lệ')
        .max(254, 'Email không được quá 254 ký tự')
        .toLowerCase()
        .refine((val) => {
            // Check for common malicious patterns in emails
            const maliciousPatterns = [
                /javascript:/i,
                /data:/i,
                /vbscript:/i,
                /<script/i,
                /onload=/i,
                /onerror=/i
            ];
            return !maliciousPatterns.some(pattern => pattern.test(val));
        }, 'Email chứa nội dung không an toàn'),

    phone: z.string()
        .optional()
        .refine((val) => !val || vietnamesePhoneRegex.test(val), {
            message: 'Số điện thoại Việt Nam không hợp lệ (ví dụ: 0912345678, +84912345678)'
        }),

    password: z.string()
        .min(8, 'Mật khẩu phải có ít nhất 8 ký tự')
        .max(128, 'Mật khẩu không được quá 128 ký tự')
        .regex(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/,
            'Mật khẩu phải có ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt')
        .refine((val) => {
            // Check against common weak passwords
            const commonPasswords = [
                'password', '123456', 'qwerty', 'admin', 'letmein',
                'welcome', 'monkey', '1234567890', 'password123'
            ];
            return !commonPasswords.includes(val.toLowerCase());
        }, 'Mật khẩu quá phổ biến, vui lòng chọn mật khẩu khác'),

    confirmPassword: z.string()
        .min(1, 'Xác nhận mật khẩu là bắt buộc'),

    roleId: z.number()
        .min(1, 'Vui lòng chọn vai trò'),

    isActive: z.boolean(),

    bio: z.string()
        .optional()
        .refine((val) => !val || val.length <= 500, 'Tiểu sử không được quá 500 ký tự'),

    sendWelcomeEmail: z.boolean(),

    // Additional security fields
    requirePasswordChange: z.boolean(),
    twoFactorEnabled: z.boolean(),

}).refine((data) => data.password === data.confirmPassword, {
    message: 'Mật khẩu xác nhận không khớp',
    path: ['confirmPassword'],
});

type SecureUserFormData = z.infer<typeof secureUserSchema>;

interface UserRole {
    id: number;
    name: string;
    displayName: string;
    description: string;
    permissions: string[];
    isSystemRole: boolean;
}

interface SecureUserFormProps {
    onSubmit: (data: SecureUserFormData) => Promise<void>;
    initialData?: Partial<SecureUserFormData>;
    roles: UserRole[];
    isLoading?: boolean;
    isEdit?: boolean;
}

export function SecureUserForm({
    onSubmit,
    initialData,
    roles,
    isLoading = false,
    isEdit = false
}: SecureUserFormProps) {
    const [securityValidations, setSecurityValidations] = useState<Record<string, SecurityValidationResult>>({});
    const [overallSecurityScore, setOverallSecurityScore] = useState(100);
    const [passwordStrength, setPasswordStrength] = useState(0);

    const form = useForm<SecureUserFormData>({
        resolver: zodResolver(secureUserSchema),
        defaultValues: {
            firstName: '',
            lastName: '',
            email: '',
            phone: '',
            password: '',
            confirmPassword: '',
            roleId: 0,
            isActive: true,
            bio: '',
            sendWelcomeEmail: true,
            requirePasswordChange: false,
            twoFactorEnabled: false,
            ...initialData
        }
    });

    // Calculate password strength
    const calculatePasswordStrength = useCallback((password: string): number => {
        let strength = 0;

        if (password.length >= 8) strength += 20;
        if (password.length >= 12) strength += 10;
        if (/[a-z]/.test(password)) strength += 15;
        if (/[A-Z]/.test(password)) strength += 15;
        if (/\d/.test(password)) strength += 15;
        if (/[@$!%*?&]/.test(password)) strength += 15;
        if (password.length >= 16) strength += 10;

        return Math.min(strength, 100);
    }, []);

    // Handle security validation updates
    const handleSecurityValidation = useCallback((field: string, result: SecurityValidationResult) => {
        setSecurityValidations(prev => ({
            ...prev,
            [field]: result
        }));

        // Special handling for password field
        if (field === 'password' && result.sanitizedValue) {
            const strength = calculatePasswordStrength(result.sanitizedValue);
            setPasswordStrength(strength);
        }

        // Calculate overall security score
        const allValidations = { ...securityValidations, [field]: result };
        const scores = Object.values(allValidations).map(v => v.securityScore);
        const avgScore = scores.length > 0 ? scores.reduce((a, b) => a + b, 0) / scores.length : 100;
        setOverallSecurityScore(Math.round(avgScore));
    }, [securityValidations, calculatePasswordStrength]);

    const handleSubmit = async (data: SecureUserFormData) => {
        try {
            // Final security check
            const hasSecurityErrors = Object.values(securityValidations).some(v => !v.isValid);
            if (hasSecurityErrors) {
                toast.error('Vui lòng khắc phục các lỗi bảo mật trước khi lưu');
                return;
            }

            if (overallSecurityScore < 80) {
                toast.error('Điểm bảo mật tổng thể quá thấp. Vui lòng kiểm tra lại các trường nhập liệu');
                return;
            }

            if (!isEdit && passwordStrength < 60) {
                toast.error('Mật khẩu không đủ mạnh. Vui lòng chọn mật khẩu phức tạp hơn');
                return;
            }

            await onSubmit(data);
            toast.success(`Người dùng đã được ${isEdit ? 'cập nhật' : 'tạo'} thành công`);
        } catch (error) {
            console.error('Submit error:', error);
            toast.error(`Có lỗi xảy ra khi ${isEdit ? 'cập nhật' : 'tạo'} người dùng`);
        }
    };

    const selectedRole = roles.find(role => role.id === form.watch('roleId'));

    return (
        <SecureFormWrapper
            title={isEdit ? "Chỉnh sửa người dùng" : "Thêm người dùng mới"}
            description={`${isEdit ? 'Cập nhật thông tin' : 'Tạo tài khoản'} người dùng với form nghiêm ngặt`}
            securityLevel="strict"
        >
            <div className="space-y-6">
                {/* Security Overview */}
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Shield className="h-5 w-5" />
                            Tổng quan bảo mật
                        </CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                            <div className="flex items-center gap-2">
                                <Badge
                                    variant={overallSecurityScore > 80 ? "default" : overallSecurityScore > 60 ? "secondary" : "destructive"}
                                >
                                    Tổng điểm: {overallSecurityScore}/100
                                </Badge>
                            </div>

                            {!isEdit && (
                                <div className="flex items-center gap-2">
                                    <Badge
                                        variant={passwordStrength > 80 ? "default" : passwordStrength > 60 ? "secondary" : "destructive"}
                                    >
                                        Mật khẩu: {passwordStrength}/100
                                    </Badge>
                                </div>
                            )}

                            <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                {overallSecurityScore > 80 ? (
                                    <>
                                        <CheckCircle className="h-4 w-4 text-green-600" />
                                        Mức độ bảo mật cao
                                    </>
                                ) : (
                                    <>
                                        <AlertTriangle className="h-4 w-4 text-yellow-600" />
                                        Cần cải thiện bảo mật
                                    </>
                                )}
                            </div>
                        </div>
                    </CardContent>
                </Card>

                <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-6">
                    {/* Personal Information */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <User className="h-5 w-5" />
                                Thông tin cá nhân
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="flex items-center gap-4 mb-4">
                                <Avatar className="h-16 w-16">
                                    <AvatarImage src="" />
                                    <AvatarFallback>
                                        {form.watch('firstName')?.charAt(0)}{form.watch('lastName')?.charAt(0)}
                                    </AvatarFallback>
                                </Avatar>
                                <div className="text-sm text-muted-foreground">
                                    Avatar sẽ được tạo tự động từ tên người dùng
                                </div>
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="firstName">Họ và tên đệm *</Label>
                                    <SecureInput
                                        id="firstName"
                                        securityContext={SECURITY_CONTEXTS.NAME}
                                        onSecurityValidation={(result) => handleSecurityValidation('firstName', result)}
                                        {...form.register('firstName')}
                                    />
                                    {form.formState.errors.firstName && (
                                        <p className="text-sm text-red-600">{form.formState.errors.firstName.message}</p>
                                    )}
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="lastName">Tên *</Label>
                                    <SecureInput
                                        id="lastName"
                                        securityContext={SECURITY_CONTEXTS.NAME}
                                        onSecurityValidation={(result) => handleSecurityValidation('lastName', result)}
                                        {...form.register('lastName')}
                                    />
                                    {form.formState.errors.lastName && (
                                        <p className="text-sm text-red-600">{form.formState.errors.lastName.message}</p>
                                    )}
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    {/* Contact Information */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Mail className="h-5 w-5" />
                                Thông tin liên hệ
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="email">Email *</Label>
                                    <SecureInput
                                        id="email"
                                        type="email"
                                        securityContext={SECURITY_CONTEXTS.EMAIL}
                                        onSecurityValidation={(result) => handleSecurityValidation('email', result)}
                                        {...form.register('email')}
                                    />
                                    {form.formState.errors.email && (
                                        <p className="text-sm text-red-600">{form.formState.errors.email.message}</p>
                                    )}
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="phone">Số điện thoại</Label>
                                    <SecureInput
                                        id="phone"
                                        securityContext={SECURITY_CONTEXTS.PHONE}
                                        onSecurityValidation={(result) => handleSecurityValidation('phone', result)}
                                        placeholder="0912345678 hoặc +84912345678"
                                        {...form.register('phone')}
                                    />
                                    {form.formState.errors.phone && (
                                        <p className="text-sm text-red-600">{form.formState.errors.phone.message}</p>
                                    )}
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    {/* Security Settings */}
                    {!isEdit && (
                        <Card>
                            <CardHeader>
                                <CardTitle className="flex items-center gap-2">
                                    <Lock className="h-5 w-5" />
                                    Cài đặt bảo mật
                                </CardTitle>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <div className="space-y-2">
                                        <Label htmlFor="password">Mật khẩu *</Label>
                                        <SecureInput
                                            id="password"
                                            type="password"
                                            securityContext={{
                                                level: 'strict',
                                                allowHtml: false,
                                                allowUrls: false,
                                                maxLength: 128,
                                                required: true
                                            }}
                                            onSecurityValidation={(result) => handleSecurityValidation('password', result)}
                                            {...form.register('password')}
                                        />
                                        {form.formState.errors.password && (
                                            <p className="text-sm text-red-600">{form.formState.errors.password.message}</p>
                                        )}

                                        {/* Password Strength Indicator */}
                                        <div className="space-y-2">
                                            <div className="flex justify-between text-xs">
                                                <span>Độ mạnh mật khẩu</span>
                                                <span>{passwordStrength}/100</span>
                                            </div>
                                            <div className="w-full bg-gray-200 rounded-full h-2">
                                                <div
                                                    className={cn(
                                                        "h-2 rounded-full transition-all",
                                                        passwordStrength < 40 && "bg-red-500",
                                                        passwordStrength >= 40 && passwordStrength < 70 && "bg-yellow-500",
                                                        passwordStrength >= 70 && "bg-green-500"
                                                    )}
                                                    style={{ width: `${passwordStrength}%` }}
                                                />
                                            </div>
                                        </div>
                                    </div>

                                    <div className="space-y-2">
                                        <Label htmlFor="confirmPassword">Xác nhận mật khẩu *</Label>
                                        <SecureInput
                                            id="confirmPassword"
                                            type="password"
                                            securityContext={{
                                                level: 'basic',
                                                allowHtml: false,
                                                allowUrls: false,
                                                maxLength: 128,
                                                required: true
                                            }}
                                            {...form.register('confirmPassword')}
                                        />
                                        {form.formState.errors.confirmPassword && (
                                            <p className="text-sm text-red-600">{form.formState.errors.confirmPassword.message}</p>
                                        )}
                                    </div>
                                </div>

                                <div className="space-y-4">
                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>Yêu cầu đổi mật khẩu khi đăng nhập đầu tiên</Label>
                                            <p className="text-sm text-muted-foreground">
                                                Bắt buộc người dùng đổi mật khẩu ngay lần đăng nhập đầu tiên
                                            </p>
                                        </div>
                                        <Switch
                                            checked={form.watch('requirePasswordChange')}
                                            onCheckedChange={(checked) => form.setValue('requirePasswordChange', checked)}
                                        />
                                    </div>

                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>Bật xác thực hai lớp (2FA)</Label>
                                            <p className="text-sm text-muted-foreground">
                                                Tăng cường bảo mật với xác thực hai lớp
                                            </p>
                                        </div>
                                        <Switch
                                            checked={form.watch('twoFactorEnabled')}
                                            onCheckedChange={(checked) => form.setValue('twoFactorEnabled', checked)}
                                        />
                                    </div>
                                </div>
                            </CardContent>
                        </Card>
                    )}

                    {/* Role and Permissions */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Shield className="h-5 w-5" />
                                Vai trò và quyền hạn
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-2">
                                <Label>Vai trò *</Label>
                                <Select
                                    value={form.watch('roleId')?.toString()}
                                    onValueChange={(value) => form.setValue('roleId', parseInt(value))}
                                >
                                    <SelectTrigger>
                                        <SelectValue placeholder="Chọn vai trò" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {roles.map((role) => (
                                            <SelectItem key={role.id} value={role.id.toString()}>
                                                <div className="flex items-center gap-2">
                                                    {role.displayName}
                                                    {role.isSystemRole && (
                                                        <Badge variant="secondary" className="text-xs">
                                                            Hệ thống
                                                        </Badge>
                                                    )}
                                                </div>
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                                {form.formState.errors.roleId && (
                                    <p className="text-sm text-red-600">{form.formState.errors.roleId.message}</p>
                                )}
                            </div>

                            {selectedRole && (
                                <div className="p-4 bg-muted rounded-lg">
                                    <h4 className="font-medium mb-2">{selectedRole.displayName}</h4>
                                    <p className="text-sm text-muted-foreground mb-3">{selectedRole.description}</p>
                                    <div className="flex flex-wrap gap-1">
                                        {selectedRole.permissions.slice(0, 5).map((permission) => (
                                            <Badge key={permission} variant="outline" className="text-xs">
                                                {permission}
                                            </Badge>
                                        ))}
                                        {selectedRole.permissions.length > 5 && (
                                            <Badge variant="outline" className="text-xs">
                                                +{selectedRole.permissions.length - 5} khác
                                            </Badge>
                                        )}
                                    </div>
                                </div>
                            )}
                        </CardContent>
                    </Card>

                    {/* Additional Information */}
                    <Card>
                        <CardHeader>
                            <CardTitle>Thông tin bổ sung</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-2">
                                <Label htmlFor="bio">Tiểu sử</Label>
                                <SecureTextarea
                                    id="bio"
                                    securityContext={{
                                        ...SECURITY_CONTEXTS.DESCRIPTION,
                                        maxLength: 500
                                    }}
                                    onSecurityValidation={(result) => handleSecurityValidation('bio', result)}
                                    rows={3}
                                    placeholder="Thông tin ngắn về người dùng..."
                                    {...form.register('bio')}
                                />
                            </div>

                            <div className="space-y-4">
                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Tài khoản hoạt động</Label>
                                        <p className="text-sm text-muted-foreground">
                                            Cho phép người dùng đăng nhập và sử dụng hệ thống
                                        </p>
                                    </div>
                                    <Switch
                                        checked={form.watch('isActive')}
                                        onCheckedChange={(checked) => form.setValue('isActive', checked)}
                                    />
                                </div>

                                {!isEdit && (
                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>Gửi email chào mừng</Label>
                                            <p className="text-sm text-muted-foreground">
                                                Gửi email hướng dẫn và thông tin đăng nhập cho người dùng mới
                                            </p>
                                        </div>
                                        <Switch
                                            checked={form.watch('sendWelcomeEmail')}
                                            onCheckedChange={(checked) => form.setValue('sendWelcomeEmail', checked)}
                                        />
                                    </div>
                                )}
                            </div>
                        </CardContent>
                    </Card>

                    {/* Submit Button */}
                    <div className="flex items-center gap-4">
                        <Button
                            type="submit"
                            disabled={isLoading || overallSecurityScore < 80 || (!isEdit && passwordStrength < 60)}
                            className="min-w-[120px]"
                        >
                            {isLoading ? (
                                <>
                                    <UserPlus className="h-4 w-4 mr-2 animate-spin" />
                                    {isEdit ? 'Đang cập nhật...' : 'Đang tạo...'}
                                </>
                            ) : (
                                <>
                                    <Save className="h-4 w-4 mr-2" />
                                    {isEdit ? 'Cập nhật người dùng' : 'Tạo người dùng'}
                                </>
                            )}
                        </Button>

                        {(overallSecurityScore < 80 || (!isEdit && passwordStrength < 60)) && (
                            <Alert>
                                <AlertTriangle className="h-4 w-4" />
                                <AlertDescription>
                                    {overallSecurityScore < 80
                                        ? 'Điểm bảo mật tổng thể quá thấp.'
                                        : 'Mật khẩu chưa đủ mạnh.'
                                    } Vui lòng kiểm tra lại.
                                </AlertDescription>
                            </Alert>
                        )}
                    </div>
                </form>
            </div>
        </SecureFormWrapper>
    );
}
