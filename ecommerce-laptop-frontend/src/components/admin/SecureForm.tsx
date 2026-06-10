/**
 * Secure Admin Form Components
 * Enhanced form components with comprehensive security validation,
 * sanitization, and protection against common attacks
 */

import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { sanitizeText, sanitizeUrl } from '@/lib/sanitize';
import { cn } from '@/lib/utils';
import { AlertTriangle, Eye, EyeOff, Shield } from 'lucide-react';
import React, { forwardRef, useCallback, useState } from 'react';

// Security validation levels
export type SecurityLevel = 'basic' | 'enhanced' | 'strict';

// Security context for form validation
export interface SecurityContext {
    level: SecurityLevel;
    allowHtml: boolean;
    allowUrls: boolean;
    maxLength: number;
    pattern?: RegExp;
    blockedPatterns?: RegExp[];
    required?: boolean;
}

// Default security contexts
export const SECURITY_CONTEXTS: Record<string, SecurityContext> = {
    NAME: {
        level: 'enhanced',
        allowHtml: false,
        allowUrls: false,
        maxLength: 100,
        pattern: /^[\p{L}\p{M}\s.'-]+$/u,
        blockedPatterns: [/script/i, /<[^>]*>/g, /javascript:/i],
        required: true
    },
    EMAIL: {
        level: 'enhanced',
        allowHtml: false,
        allowUrls: false,
        maxLength: 254,
        pattern: /^[^\s@]+@[^\s@]+\.[^\s@]+$/,
        blockedPatterns: [/script/i, /<[^>]*>/g],
        required: true
    },
    PHONE: {
        level: 'basic',
        allowHtml: false,
        allowUrls: false,
        maxLength: 20,
        pattern: /^[\d\s()+.-]+$/,
        blockedPatterns: [/script/i, /<[^>]*>/g]
    },
    DESCRIPTION: {
        level: 'enhanced',
        allowHtml: true,
        allowUrls: true,
        maxLength: 2000,
        blockedPatterns: [/javascript:/i, /vbscript:/i, /onload=/i, /onerror=/i]
    },
    URL: {
        level: 'enhanced',
        allowHtml: false,
        allowUrls: true,
        maxLength: 500,
        pattern: /^https?:\/\/.+/,
        blockedPatterns: [/javascript:/i, /vbscript:/i, /data:/i]
    },
    PRICE: {
        level: 'basic',
        allowHtml: false,
        allowUrls: false,
        maxLength: 20,
        pattern: /^\d+(\.\d{1,2})?$/,
        required: true
    },
    QUANTITY: {
        level: 'basic',
        allowHtml: false,
        allowUrls: false,
        maxLength: 10,
        pattern: /^\d+$/,
        required: true
    }
};

// Security validation result
export interface SecurityValidationResult {
    isValid: boolean;
    securityScore: number;
    errors: string[];
    warnings: string[];
    sanitizedValue?: string;
}

/**
 * Validates input security based on context
 */
export function validateInputSecurity(
    value: string,
    context: SecurityContext
): SecurityValidationResult {
    const errors: string[] = [];
    const warnings: string[] = [];
    let securityScore = 100;
    let sanitizedValue = value;

    // Basic validation
    if (context.required && (!value || value.trim().length === 0)) {
        errors.push('Trường này là bắt buộc');
        return { isValid: false, securityScore: 0, errors, warnings };
    }

    if (value && value.length > context.maxLength) {
        errors.push(`Không được vượt quá ${context.maxLength} ký tự`);
        securityScore -= 20;
    }

    // Pattern validation
    if (value && context.pattern && !context.pattern.test(value)) {
        errors.push('Định dạng không hợp lệ');
        securityScore -= 30;
    }

    // Check blocked patterns
    if (value && context.blockedPatterns) {
        for (const blockedPattern of context.blockedPatterns) {
            if (blockedPattern.test(value)) {
                errors.push('Nội dung chứa ký tự không được phép');
                securityScore -= 40;
                break;
            }
        }
    }

    // Sanitization based on context
    if (value) {
        if (!context.allowHtml) {
            const textSanitized = sanitizeText(value);
            if (textSanitized !== value) {
                warnings.push('Nội dung đã được làm sạch để đảm bảo an toàn');
                sanitizedValue = textSanitized;
                securityScore -= 10;
            }
        }

        if (context.allowUrls) {
            // Check for valid URLs
            const urlRegex = /https?:\/\/[^\s]+/g;
            const urls = value.match(urlRegex);
            if (urls) {
                for (const url of urls) {
                    const sanitizedUrl = sanitizeUrl(url);
                    if (!sanitizedUrl) {
                        errors.push('URL không an toàn đã được phát hiện');
                        securityScore -= 25;
                    }
                }
            }
        }
    }

    return {
        isValid: errors.length === 0,
        securityScore: Math.max(0, securityScore),
        errors,
        warnings,
        sanitizedValue
    };
}

// Secure Input Component
interface SecureInputProps extends React.InputHTMLAttributes<HTMLInputElement> {
    securityContext?: SecurityContext;
    onSecurityValidation?: (result: SecurityValidationResult) => void;
    showSecurityBadge?: boolean;
}

export const SecureInput = forwardRef<HTMLInputElement, SecureInputProps>(
    ({
        securityContext = SECURITY_CONTEXTS.NAME,
        onSecurityValidation,
        showSecurityBadge = true,
        className,
        onChange,
        ...props
    }, ref) => {
        const [validationResult, setValidationResult] = useState<SecurityValidationResult | null>(null);
        const [showPassword, setShowPassword] = useState(false);

        const handleChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
            const value = e.target.value;
            const result = validateInputSecurity(value, securityContext);

            setValidationResult(result);
            onSecurityValidation?.(result);

            // Update the input with sanitized value if needed
            if (result.sanitizedValue && result.sanitizedValue !== value) {
                e.target.value = result.sanitizedValue;
            }

            onChange?.(e);
        }, [securityContext, onSecurityValidation, onChange]);

        const isPasswordField = props.type === 'password';

        return (
            <div className="space-y-2">
                <div className="relative">
                    <Input
                        ref={ref}
                        {...props}
                        type={isPasswordField && showPassword ? 'text' : props.type}
                        className={cn(
                            className,
                            validationResult?.errors.length && 'border-red-500',
                            validationResult?.warnings.length && 'border-yellow-500',
                            validationResult?.isValid && validationResult.securityScore > 80 && 'border-green-500'
                        )}
                        onChange={handleChange}
                        maxLength={securityContext.maxLength}
                    />

                    {isPasswordField && (
                        <Button
                            type="button"
                            variant="ghost"
                            size="sm"
                            className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                            onClick={() => setShowPassword(!showPassword)}
                        >
                            {showPassword ? (
                                <EyeOff className="h-4 w-4" />
                            ) : (
                                <Eye className="h-4 w-4" />
                            )}
                        </Button>
                    )}
                </div>

                {/* Security Badge */}
                {showSecurityBadge && validationResult && (
                    <div className="flex items-center gap-2">
                        <Shield className="h-4 w-4 text-green-600" />
                        <Badge
                            variant={validationResult.securityScore > 80 ? "default" : validationResult.securityScore > 60 ? "secondary" : "destructive"}
                            className="text-xs"
                        >
                            Bảo mật: {validationResult.securityScore}/100
                        </Badge>
                    </div>
                )}

                {/* Validation Messages */}
                {validationResult?.errors.map((error, index) => (
                    <Alert key={`error-${index}`} variant="destructive" className="py-2">
                        <AlertTriangle className="h-4 w-4" />
                        <AlertDescription className="text-sm">{error}</AlertDescription>
                    </Alert>
                ))}

                {validationResult?.warnings.map((warning, index) => (
                    <Alert key={`warning-${index}`} className="py-2">
                        <AlertTriangle className="h-4 w-4" />
                        <AlertDescription className="text-sm">{warning}</AlertDescription>
                    </Alert>
                ))}
            </div>
        );
    }
);

SecureInput.displayName = 'SecureInput';

// Secure Textarea Component
interface SecureTextareaProps extends React.TextareaHTMLAttributes<HTMLTextAreaElement> {
    securityContext?: SecurityContext;
    onSecurityValidation?: (result: SecurityValidationResult) => void;
    showSecurityBadge?: boolean;
}

export const SecureTextarea = forwardRef<HTMLTextAreaElement, SecureTextareaProps>(
    ({
        securityContext = SECURITY_CONTEXTS.DESCRIPTION,
        onSecurityValidation,
        showSecurityBadge = true,
        className,
        onChange,
        ...props
    }, ref) => {
        const [validationResult, setValidationResult] = useState<SecurityValidationResult | null>(null);

        const handleChange = useCallback((e: React.ChangeEvent<HTMLTextAreaElement>) => {
            const value = e.target.value;
            const result = validateInputSecurity(value, securityContext);

            setValidationResult(result);
            onSecurityValidation?.(result);

            if (result.sanitizedValue && result.sanitizedValue !== value) {
                e.target.value = result.sanitizedValue;
            }

            onChange?.(e);
        }, [securityContext, onSecurityValidation, onChange]);

        return (
            <div className="space-y-2">
                <Textarea
                    ref={ref}
                    {...props}
                    className={cn(
                        className,
                        validationResult?.errors.length && 'border-red-500',
                        validationResult?.warnings.length && 'border-yellow-500',
                        validationResult?.isValid && validationResult.securityScore > 80 && 'border-green-500'
                    )}
                    onChange={handleChange}
                    maxLength={securityContext.maxLength}
                />

                {/* Character count and security info */}
                <div className="flex items-center justify-between text-sm text-muted-foreground">
                    <span>
                        {props.value?.toString()?.length || 0}/{securityContext.maxLength}
                    </span>

                    {showSecurityBadge && validationResult && (
                        <div className="flex items-center gap-2">
                            <Shield className="h-4 w-4 text-green-600" />
                            <Badge
                                variant={validationResult.securityScore > 80 ? "default" : validationResult.securityScore > 60 ? "secondary" : "destructive"}
                                className="text-xs"
                            >
                                Bảo mật: {validationResult.securityScore}/100
                            </Badge>
                        </div>
                    )}
                </div>

                {/* Validation Messages */}
                {validationResult?.errors.map((error, index) => (
                    <Alert key={`error-${index}`} variant="destructive" className="py-2">
                        <AlertTriangle className="h-4 w-4" />
                        <AlertDescription className="text-sm">{error}</AlertDescription>
                    </Alert>
                ))}

                {validationResult?.warnings.map((warning, index) => (
                    <Alert key={`warning-${index}`} className="py-2">
                        <AlertTriangle className="h-4 w-4" />
                        <AlertDescription className="text-sm">{warning}</AlertDescription>
                    </Alert>
                ))}
            </div>
        );
    }
);

SecureTextarea.displayName = 'SecureTextarea';

// Secure Form Wrapper
interface SecureFormWrapperProps {
    children: React.ReactNode;
    title: string;
    description?: string;
    securityLevel: SecurityLevel;
    className?: string;
}

export function SecureFormWrapper({
    children,
    title,
    description,
    securityLevel,
    className
}: SecureFormWrapperProps) {
    const securityColor = {
        basic: 'text-yellow-600',
        enhanced: 'text-blue-600',
        strict: 'text-green-600'
    }[securityLevel];

    const securityBadgeVariant = {
        basic: 'secondary' as const,
        enhanced: 'default' as const,
        strict: 'default' as const
    }[securityLevel];

    return (
        <div className={cn("space-y-6", className)}>
            <div className="flex items-center justify-between">
                <div className="space-y-1">
                    <h2 className="text-2xl font-semibold tracking-tight">{title}</h2>
                    {description && (
                        <p className="text-sm text-muted-foreground">{description}</p>
                    )}
                </div>
                <div className="flex items-center gap-2">
                    <Shield className={cn("h-5 w-5", securityColor)} />
                    <Badge variant={securityBadgeVariant}>
                        Bảo mật {securityLevel === 'basic' ? 'Cơ bản' : securityLevel === 'enhanced' ? 'Nâng cao' : 'Nghiêm ngặt'}
                    </Badge>
                </div>
            </div>

            <Alert>
                <Shield className="h-4 w-4" />
                <AlertDescription>
                    Form này được bảo vệ bởi form {securityLevel === 'basic' ? 'cơ bản' : securityLevel === 'enhanced' ? 'nâng cao' : 'nghiêm ngặt'}.
                    Tất cả dữ liệu nhập vào sẽ được kiểm tra và làm sạch tự động.
                </AlertDescription>
            </Alert>

            {children}
        </div>
    );
}
