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
        .min(1, 'H l bt buc')
        .min(2, 'H phi c t nht 2 k t')
        .max(50, 'H khng c qu 50 k t')
        .regex(/^[\p{L}\p{M}\s.'-]+$/u, 'H ch c cha ch ci, du cch v cc k t c bit hp l')
        .refine((val) => !/[<>\"'&]/.test(val), 'H cha k t khng an ton'),

    lastName: z.string()
        .min(1, 'Tn l bt buc')
        .min(2, 'Tn phi c t nht 2 k t')
        .max(50, 'Tn khng c qu 50 k t')
        .regex(/^[\p{L}\p{M}\s.'-]+$/u, 'Tn ch c cha ch ci, du cch v cc k t c bit hp l')
        .refine((val) => !/[<>\"'&]/.test(val), 'Tn cha k t khng an ton'),

    email: z.string()
        .min(1, 'Email l bt buc')
        .email('Email khng hp l')
        .max(254, 'Email khng c qu 254 k t')
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
        }, 'Email cha ni dung khng an ton'),

    phone: z.string()
        .optional()
        .refine((val) => !val || vietnamesePhoneRegex.test(val), {
            message: 'S in thoi Vit Nam khng hp l (v d: 0912345678, +84912345678)'
        }),

    password: z.string()
        .min(8, 'Mt khu phi c t nht 8 k t')
        .max(128, 'Mt khu khng c qu 128 k t')
        .regex(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]/,
            'Mt khu phi c t nht 1 ch hoa, 1 ch thng, 1 s v 1 k t c bit')
        .refine((val) => {
            // Check against common weak passwords
            const commonPasswords = [
                'password', '123456', 'qwerty', 'admin', 'letmein',
                'welcome', 'monkey', '1234567890', 'password123'
            ];
            return !commonPasswords.includes(val.toLowerCase());
        }, 'Mt khu qu ph bin, vui lng chn mt khu khc'),

    confirmPassword: z.string()
        .min(1, 'Xc nhn mt khu l bt buc'),

    roleId: z.number()
        .min(1, 'Vui lng chn vai tr'),

    isActive: z.boolean(),

    bio: z.string()
        .optional()
        .refine((val) => !val || val.length <= 500, 'Tiu s khng c qu 500 k t'),

    sendWelcomeEmail: z.boolean(),

    // Additional security fields
    requirePasswordChange: z.boolean(),
    twoFactorEnabled: z.boolean(),

}).refine((data) => data.password === data.confirmPassword, {
    message: 'Mt khu xc nhn khng khp',
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
                toast.error('Vui lng khc phc cc li bo mt trc khi lu');
                return;
            }

            if (overallSecurityScore < 80) {
                toast.error('im bo mt tng th qu thp. Vui lng kim tra li cc trng nhp liu');
                return;
            }

            if (!isEdit && passwordStrength < 60) {
                toast.error('Mt khu khng  mnh. Vui lng chn mt khu phc tp hn');
                return;
            }

            await onSubmit(data);
            toast.success(`Ngi dng  c ${isEdit ? 'cp nht' : 'to'} thnh cng`);
        } catch (error) {
            console.error('Submit error:', error);
            toast.error(`C li xy ra khi ${isEdit ? 'cp nht' : 'to'} ngi dng`);
        }
    };

    const selectedRole = roles.find(role => role.id === form.watch('roleId'));

    return (
        <SecureFormWrapper
            title={isEdit ? "Chnh sa ngi dng" : "Thm ngi dng mi"}
            description={`${isEdit ? 'Cp nht thng tin' : 'To ti khon'} ngi dng vi form nghim ngt`}
            securityLevel="strict"
        >
            <div className="space-y-6">
                {/* Security Overview */}
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Shield className="h-5 w-5" />
                            Tng quan bo mt
                        </CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                            <div className="flex items-center gap-2">
                                <Badge
                                    variant={overallSecurityScore > 80 ? "default" : overallSecurityScore > 60 ? "secondary" : "destructive"}
                                >
                                    Tng im: {overallSecurityScore}/100
                                </Badge>
                            </div>

                            {!isEdit && (
                                <div className="flex items-center gap-2">
                                    <Badge
                                        variant={passwordStrength > 80 ? "default" : passwordStrength > 60 ? "secondary" : "destructive"}
                                    >
                                        Mt khu: {passwordStrength}/100
                                    </Badge>
                                </div>
                            )}

                            <div className="flex items-center gap-2 text-sm text-muted-foreground">
                                {overallSecurityScore > 80 ? (
                                    <>
                                        <CheckCircle className="h-4 w-4 text-green-600" />
                                        Mc  bo mt cao
                                    </>
                                ) : (
                                    <>
                                        <AlertTriangle className="h-4 w-4 text-yellow-600" />
                                        Cn ci thin bo mt
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
                                Thng tin c nhn
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
                                    Avatar s c to t ng t tn ngi dng
                                </div>
                            </div>

                            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="firstName">H v tn m *</Label>
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
                                    <Label htmlFor="lastName">Tn *</Label>
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
                                Thng tin lin h
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
                                    <Label htmlFor="phone">S in thoi</Label>
                                    <SecureInput
                                        id="phone"
                                        securityContext={SECURITY_CONTEXTS.PHONE}
                                        onSecurityValidation={(result) => handleSecurityValidation('phone', result)}
                                        placeholder="0912345678 hoc +84912345678"
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
                                    Ci t bo mt
                                </CardTitle>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                                    <div className="space-y-2">
                                        <Label htmlFor="password">Mt khu *</Label>
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
                                                <span> mnh mt khu</span>
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
                                        <Label htmlFor="confirmPassword">Xc nhn mt khu *</Label>
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
                                            <Label>Yu cu i mt khu khi ng nhp u tin</Label>
                                            <p className="text-sm text-muted-foreground">
                                                Bt buc ngi dng i mt khu ngay ln ng nhp u tin
                                            </p>
                                        </div>
                                        <Switch
                                            checked={form.watch('requirePasswordChange')}
                                            onCheckedChange={(checked) => form.setValue('requirePasswordChange', checked)}
                                        />
                                    </div>

                                    <div className="flex items-center justify-between">
                                        <div className="space-y-0.5">
                                            <Label>Bt xc thc hai lp (2FA)</Label>
                                            <p className="text-sm text-muted-foreground">
                                                Tng cng bo mt vi xc thc hai lp
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
                                Vai tr v quyn hn
                            </CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-2">
                                <Label>Vai tr *</Label>
                                <Select
                                    value={form.watch('roleId')?.toString()}
                                    onValueChange={(value) => form.setValue('roleId', parseInt(value))}
                                >
                                    <SelectTrigger>
                                        <SelectValue placeholder="Chn vai tr" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {roles.map((role) => (
                                            <SelectItem key={role.id} value={role.id.toString()}>
                                                <div className="flex items-center gap-2">
                                                    {role.displayName}
                                                    {role.isSystemRole && (
                                                        <Badge variant="secondary" className="text-xs">
                                                            H thng
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
                                                +{selectedRole.permissions.length - 5} khc
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
                            <CardTitle>Thng tin b sung</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="space-y-2">
                                <Label htmlFor="bio">Tiu s</Label>
                                <SecureTextarea
                                    id="bio"
                                    securityContext={{
                                        ...SECURITY_CONTEXTS.DESCRIPTION,
                                        maxLength: 500
                                    }}
                                    onSecurityValidation={(result) => handleSecurityValidation('bio', result)}
                                    rows={3}
                                    placeholder="Thng tin ngn v ngi dng..."
                                    {...form.register('bio')}
                                />
                            </div>

                            <div className="space-y-4">
                                <div className="flex items-center justify-between">
                                    <div className="space-y-0.5">
                                        <Label>Ti khon hot ng</Label>
                                        <p className="text-sm text-muted-foreground">
                                            Cho php ngi dng ng nhp v s dng h thng
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
                                            <Label>Gi email cho mng</Label>
                                            <p className="text-sm text-muted-foreground">
                                                Gi email hng dn v thng tin ng nhp cho ngi dng mi
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
                                    {isEdit ? 'ang cp nht...' : 'ang to...'}
                                </>
                            ) : (
                                <>
                                    <Save className="h-4 w-4 mr-2" />
                                    {isEdit ? 'Cp nht ngi dng' : 'To ngi dng'}
                                </>
                            )}
                        </Button>

                        {(overallSecurityScore < 80 || (!isEdit && passwordStrength < 60)) && (
                            <Alert>
                                <AlertTriangle className="h-4 w-4" />
                                <AlertDescription>
                                    {overallSecurityScore < 80
                                        ? 'im bo mt tng th qu thp.'
                                        : 'Mt khu cha  mnh.'
                                    } Vui lng kim tra li.
                                </AlertDescription>
                            </Alert>
                        )}
                    </div>
                </form>
            </div>
        </SecureFormWrapper>
    );
}