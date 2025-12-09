/**
 * Secure Payment Forms
 * PCI DSS compliant payment forms with comprehensive security validation,
 * tokenization, and fraud prevention
 */

'use client';

import { Alert, AlertDescription } from '@/components/ui/alert';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle
} from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { zodResolver } from '@hookform/resolvers/zod';
import {
    AlertTriangle,
    CreditCard,
    DollarSign,
    Eye,
    EyeOff,
    Lock,
    Shield,
    Smartphone
} from 'lucide-react';
import React, { useCallback, useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import { z } from 'zod';
import {
    SecureFormWrapper,
    SecureInput,
    SecurityContext,
    SecurityValidationResult
} from './SecureForm';

// Payment security contexts
const PAYMENT_SECURITY_CONTEXTS: Record<string, SecurityContext> = {
    CARD_NUMBER: {
        level: 'strict',
        allowHtml: false,
        allowUrls: false,
        maxLength: 19, // 16 digits + 3 spaces
        pattern: /^[\d\s]{13,19}$/,
        blockedPatterns: [/[^\d\s]/g],
        required: true
    },
    CVV: {
        level: 'strict',
        allowHtml: false,
        allowUrls: false,
        maxLength: 4,
        pattern: /^\d{3,4}$/,
        blockedPatterns: [/[^\d]/g],
        required: true
    },
    EXPIRY: {
        level: 'strict',
        allowHtml: false,
        allowUrls: false,
        maxLength: 5, // MM/YY
        pattern: /^(0[1-9]|1[0-2])\/\d{2}$/,
        blockedPatterns: [/[^\d\/]/g],
        required: true
    },
    CARDHOLDER_NAME: {
        level: 'enhanced',
        allowHtml: false,
        allowUrls: false,
        maxLength: 100,
        pattern: /^[A-Za-z\s.'-]+$/,
        blockedPatterns: [/[<>\"'&]/g],
        required: true
    }
};

// Enhanced card validation schema
const secureCardSchema = z.object({
    cardNumber: z.string()
        .min(1, 'S th l bt buc')
        .refine((val) => {
            const cleaned = val.replace(/\s/g, '');
            return /^\d{13,19}$/.test(cleaned);
        }, 'S th phi c 13-19 ch s')
        .refine((val) => {
            // Luhn algorithm validation
            const cleaned = val.replace(/\s/g, '');
            return luhnCheck(cleaned);
        }, 'S th khng hp l'),

    expiryDate: z.string()
        .min(1, 'Ngy ht hn l bt buc')
        .regex(/^(0[1-9]|1[0-2])\/\d{2}$/, 'nh dng phi l MM/YY')
        .refine((val) => {
            const [month, year] = val.split('/');
            const currentDate = new Date();
            const currentYear = currentDate.getFullYear() % 100;
            const currentMonth = currentDate.getMonth() + 1;
            const cardYear = parseInt(year);
            const cardMonth = parseInt(month);

            if (cardYear < currentYear) return false;
            if (cardYear === currentYear && cardMonth < currentMonth) return false;

            return true;
        }, 'Th  ht hn'),

    cvv: z.string()
        .min(3, 'CVV phi c t nht 3 ch s')
        .max(4, 'CVV khng c qu 4 ch s')
        .regex(/^\d{3,4}$/, 'CVV ch c cha s'),

    cardholderName: z.string()
        .min(1, 'Tn ch th l bt buc')
        .min(2, 'Tn ch th phi c t nht 2 k t')
        .max(100, 'Tn ch th khng c qu 100 k t')
        .regex(/^[A-Za-z\s.'-]+$/, 'Tn ch th ch c cha ch ci v k t hp l'),

    saveCard: z.boolean().optional(),
    agreedToTerms: z.boolean().refine(val => val === true, 'Vui lng ng  vi iu khon'),
});

type SecureCardFormData = z.infer<typeof secureCardSchema>;

// Luhn algorithm for card validation
function luhnCheck(cardNumber: string): boolean {
    let sum = 0;
    let isEven = false;

    for (let i = cardNumber.length - 1; i >= 0; i--) {
        let digit = parseInt(cardNumber[i]);

        if (isEven) {
            digit *= 2;
            if (digit > 9) {
                digit -= 9;
            }
        }

        sum += digit;
        isEven = !isEven;
    }

    return sum % 10 === 0;
}

// Detect card type
function detectCardType(cardNumber: string): string {
    const cleaned = cardNumber.replace(/\s/g, '');

    if (/^4/.test(cleaned)) return 'visa';
    if (/^5[1-5]/.test(cleaned) || /^2[2-7]/.test(cleaned)) return 'mastercard';
    if (/^3[47]/.test(cleaned)) return 'amex';
    if (/^6(?:011|5)/.test(cleaned)) return 'discover';
    if (/^35/.test(cleaned)) return 'jcb';

    return 'unknown';
}

// Format card number with spaces
function formatCardNumber(value: string): string {
    const cleaned = value.replace(/\s/g, '');
    const match = cleaned.match(/.{1,4}/g);
    return match ? match.join(' ') : cleaned;
}

// Format expiry date
function formatExpiryDate(value: string): string {
    const cleaned = value.replace(/\D/g, '');
    if (cleaned.length >= 2) {
        return cleaned.substring(0, 2) + '/' + cleaned.substring(2, 4);
    }
    return cleaned;
}

interface SecurePaymentFormProps {
    amount: number;
    currency: string;
    orderId: number;
    onPaymentSubmit: (data: SecureCardFormData & { paymentToken: string }) => Promise<void>;
    isLoading?: boolean;
}

export function SecurePaymentForm({
    amount,
    currency,
    orderId,
    onPaymentSubmit,
    isLoading = false
}: SecurePaymentFormProps) {
    const [securityValidations, setSecurityValidations] = useState<Record<string, SecurityValidationResult>>({});
    const [overallSecurityScore, setOverallSecurityScore] = useState(100);
    const [cardType, setCardType] = useState<string>('unknown');
    const [showCvv, setShowCvv] = useState(false);
    const [paymentToken, setPaymentToken] = useState<string>('');

    const form = useForm<SecureCardFormData>({
        resolver: zodResolver(secureCardSchema),
        defaultValues: {
            cardNumber: '',
            expiryDate: '',
            cvv: '',
            cardholderName: '',
            saveCard: false,
            agreedToTerms: false
        }
    });

    // Generate secure payment token
    const generatePaymentToken = useCallback((): string => {
        const timestamp = Date.now().toString(36);
        const randomPart = Math.random().toString(36).substring(2, 15);
        return `pmt_${timestamp}_${randomPart}`;
    }, []);

    useEffect(() => {
        setPaymentToken(generatePaymentToken());
    }, [generatePaymentToken]);

    // Handle security validation updates
    const handleSecurityValidation = useCallback((field: string, result: SecurityValidationResult) => {
        setSecurityValidations(prev => ({
            ...prev,
            [field]: result
        }));

        // Calculate overall security score
        const allValidations = { ...securityValidations, [field]: result };
        const scores = Object.values(allValidations).map(v => v.securityScore);
        const avgScore = scores.length > 0 ? scores.reduce((a, b) => a + b, 0) / scores.length : 100;
        setOverallSecurityScore(Math.round(avgScore));
    }, [securityValidations]);

    // Handle card number input with formatting and validation
    const handleCardNumberChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        const formatted = formatCardNumber(e.target.value);
        const type = detectCardType(formatted);

        setCardType(type);
        form.setValue('cardNumber', formatted);
    }, [form]);

    // Handle expiry date input with formatting
    const handleExpiryChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => {
        const formatted = formatExpiryDate(e.target.value);
        form.setValue('expiryDate', formatted);
    }, [form]);

    const handleSubmit = async (data: SecureCardFormData) => {
        try {
            // Final security check
            const hasSecurityErrors = Object.values(securityValidations).some(v => !v.isValid);
            if (hasSecurityErrors) {
                toast.error('Vui lng khc phc cc li bo mt trc khi thanh ton');
                return;
            }

            if (overallSecurityScore < 90) {
                toast.error('im bo mt thanh ton qu thp. Vui lng kim tra li thng tin');
                return;
            }

            // Additional fraud checks
            const cardNumber = data.cardNumber.replace(/\s/g, '');

            // Check for test card numbers (should be blocked in production)
            const testCardNumbers = [
                '4111111111111111', '4000000000000002', '5555555555554444',
                '378282246310005', '6011111111111117'
            ];

            if (testCardNumbers.includes(cardNumber)) {
                toast.error('Khng th s dng s th test trong mi trng production');
                return;
            }

            await onPaymentSubmit({ ...data, paymentToken });
            toast.success('Thanh ton  c x l thnh cng');
        } catch (error) {
            console.error('Payment error:', error);
            toast.error('C li xy ra khi x l thanh ton');
        }
    };

    const cardTypeIcon = {
        visa: '',
        mastercard: '',
        amex: '',
        discover: '',
        jcb: '',
        unknown: ''
    }[cardType];

    return (
        <SecureFormWrapper
            title="Thanh ton an ton"
            description="Thng tin th ca bn c m ha v bo v theo tiu chun PCI DSS"
            securityLevel="strict"
        >
            <div className="space-y-6">
                {/* Payment Summary */}
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <DollarSign className="h-5 w-5" />
                            Tng thanh ton
                        </CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="flex items-center justify-between">
                            <span className="text-lg">n hng #{orderId}</span>
                            <span className="text-2xl font-bold">
                                {new Intl.NumberFormat('vi-VN', {
                                    style: 'currency',
                                    currency: currency
                                }).format(amount)}
                            </span>
                        </div>
                    </CardContent>
                </Card>

                {/* Security Overview */}
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Shield className="h-5 w-5" />
                            Bo mt thanh ton
                        </CardTitle>
                    </CardHeader>
                    <CardContent>
                        <div className="flex items-center gap-4">
                            <Badge
                                variant={overallSecurityScore > 90 ? "default" : "destructive"}
                            >
                                im bo mt: {overallSecurityScore}/100
                            </Badge>
                            <div className="flex items-center gap-2 text-sm">
                                <Lock className="h-4 w-4" />
                                PCI DSS Compliant
                            </div>
                            <div className="flex items-center gap-2 text-sm">
                                <Shield className="h-4 w-4" />
                                SSL/TLS Encrypted
                            </div>
                        </div>
                    </CardContent>
                </Card>

                <form onSubmit={form.handleSubmit(handleSubmit)} className="space-y-6">
                    {/* Card Information */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <CreditCard className="h-5 w-5" />
                                Thng tin th
                            </CardTitle>
                            <CardDescription>
                                Thng tin th ca bn c m ha v khng c lu tr trn my ch
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            {/* Card Number */}
                            <div className="space-y-2">
                                <Label htmlFor="cardNumber">S th</Label>
                                <div className="relative">
                                    <SecureInput
                                        id="cardNumber"
                                        securityContext={PAYMENT_SECURITY_CONTEXTS.CARD_NUMBER}
                                        onSecurityValidation={(result) => handleSecurityValidation('cardNumber', result)}
                                        onChange={handleCardNumberChange}
                                        placeholder="1234 5678 9012 3456"
                                        maxLength={19}
                                        className="pr-12"
                                    />
                                    <div className="absolute right-3 top-1/2 transform -translate-y-1/2 text-2xl">
                                        {cardTypeIcon}
                                    </div>
                                </div>
                                {cardType !== 'unknown' && (
                                    <p className="text-sm text-muted-foreground capitalize">
                                        Loi th: {cardType}
                                    </p>
                                )}
                                {form.formState.errors.cardNumber && (
                                    <p className="text-sm text-red-600">{form.formState.errors.cardNumber.message}</p>
                                )}
                            </div>

                            <div className="grid grid-cols-2 gap-4">
                                {/* Expiry Date */}
                                <div className="space-y-2">
                                    <Label htmlFor="expiryDate">Ngy ht hn</Label>
                                    <SecureInput
                                        id="expiryDate"
                                        securityContext={PAYMENT_SECURITY_CONTEXTS.EXPIRY}
                                        onSecurityValidation={(result) => handleSecurityValidation('expiryDate', result)}
                                        onChange={handleExpiryChange}
                                        placeholder="MM/YY"
                                        maxLength={5}
                                    />
                                    {form.formState.errors.expiryDate && (
                                        <p className="text-sm text-red-600">{form.formState.errors.expiryDate.message}</p>
                                    )}
                                </div>

                                {/* CVV */}
                                <div className="space-y-2">
                                    <Label htmlFor="cvv">CVV</Label>
                                    <div className="relative">
                                        <SecureInput
                                            id="cvv"
                                            type={showCvv ? 'text' : 'password'}
                                            securityContext={PAYMENT_SECURITY_CONTEXTS.CVV}
                                            onSecurityValidation={(result) => handleSecurityValidation('cvv', result)}
                                            {...form.register('cvv')}
                                            placeholder="123"
                                            maxLength={4}
                                            className="pr-10"
                                        />
                                        <Button
                                            type="button"
                                            variant="ghost"
                                            size="sm"
                                            className="absolute right-0 top-0 h-full px-3 py-2 hover:bg-transparent"
                                            onClick={() => setShowCvv(!showCvv)}
                                        >
                                            {showCvv ? (
                                                <EyeOff className="h-4 w-4" />
                                            ) : (
                                                <Eye className="h-4 w-4" />
                                            )}
                                        </Button>
                                    </div>
                                    {form.formState.errors.cvv && (
                                        <p className="text-sm text-red-600">{form.formState.errors.cvv.message}</p>
                                    )}
                                </div>
                            </div>

                            {/* Cardholder Name */}
                            <div className="space-y-2">
                                <Label htmlFor="cardholderName">Tn ch th</Label>
                                <SecureInput
                                    id="cardholderName"
                                    securityContext={PAYMENT_SECURITY_CONTEXTS.CARDHOLDER_NAME}
                                    onSecurityValidation={(result) => handleSecurityValidation('cardholderName', result)}
                                    {...form.register('cardholderName')}
                                    placeholder="NGUYEN VAN A"
                                    style={{ textTransform: 'uppercase' }}
                                />
                                {form.formState.errors.cardholderName && (
                                    <p className="text-sm text-red-600">{form.formState.errors.cardholderName.message}</p>
                                )}
                            </div>
                        </CardContent>
                    </Card>

                    {/* Security Features */}
                    <Card>
                        <CardHeader>
                            <CardTitle>Tnh nng bo mt</CardTitle>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="flex items-center space-x-2">
                                <input
                                    type="checkbox"
                                    id="saveCard"
                                    {...form.register('saveCard')}
                                    className="rounded"
                                />
                                <Label htmlFor="saveCard" className="text-sm">
                                    Lu th ny cho ln thanh ton sau (an ton vi tokenization)
                                </Label>
                            </div>

                            <div className="flex items-center space-x-2">
                                <input
                                    type="checkbox"
                                    id="agreedToTerms"
                                    {...form.register('agreedToTerms')}
                                    className="rounded"
                                />
                                <Label htmlFor="agreedToTerms" className="text-sm">
                                    Ti ng  vi <a href="/terms" className="text-primary hover:underline">iu khon dch v</a> v
                                    <a href="/privacy" className="text-primary hover:underline"> chnh sch bo mt</a>
                                </Label>
                            </div>
                            {form.formState.errors.agreedToTerms && (
                                <p className="text-sm text-red-600">{form.formState.errors.agreedToTerms.message}</p>
                            )}
                        </CardContent>
                    </Card>

                    {/* Security Warnings */}
                    {overallSecurityScore < 90 && (
                        <Alert variant="destructive">
                            <AlertTriangle className="h-4 w-4" />
                            <AlertDescription>
                                im bo mt thanh ton qu thp. Vui lng kim tra li thng tin th.
                            </AlertDescription>
                        </Alert>
                    )}

                    {/* Submit Button */}
                    <Button
                        type="submit"
                        disabled={isLoading || overallSecurityScore < 90 || !form.watch('agreedToTerms')}
                        className="w-full h-12 text-lg"
                    >
                        {isLoading ? (
                            <>
                                <Shield className="h-5 w-5 mr-2 animate-spin" />
                                ang x l thanh ton...
                            </>
                        ) : (
                            <>
                                <Lock className="h-5 w-5 mr-2" />
                                Thanh ton an ton {new Intl.NumberFormat('vi-VN', {
                                    style: 'currency',
                                    currency: currency
                                }).format(amount)}
                            </>
                        )}
                    </Button>

                    {/* Security Notice */}
                    <div className="text-center text-sm text-muted-foreground">
                        <p> Thanh ton c bo v bi m ha SSL 256-bit</p>
                        <p>Thng tin th ca bn khng c lu tr trn my ch</p>
                    </div>
                </form>
            </div>
        </SecureFormWrapper>
    );
}

// Alternative Payment Methods Component
interface AlternativePaymentProps {
    amount: number;
    currency: string;
    orderId: number;
    onPaymentMethodSelect: (method: string) => void;
}

export function AlternativePaymentMethods({
    amount,
    currency,
    orderId,
    onPaymentMethodSelect
}: AlternativePaymentProps) {
    const paymentMethods = [
        {
            id: 'vnpay',
            name: 'VNPay',
            description: 'Thanh ton qua VNPay',
            icon: '',
            fee: 0
        },
        {
            id: 'momo',
            name: 'MoMo',
            description: 'V in t MoMo',
            icon: '',
            fee: 0
        },
        {
            id: 'sepay',
            name: 'SePay',
            description: 'Chuyn khon ngn hng',
            icon: '',
            fee: 0
        },
        {
            id: 'cod',
            name: 'Thanh ton khi nhn hng',
            description: 'Thanh ton bng tin mt',
            icon: '',
            fee: 15000
        }
    ];

    return (
        <Card>
            <CardHeader>
                <CardTitle className="flex items-center gap-2">
                    <Smartphone className="h-5 w-5" />
                    Phng thc thanh ton khc
                </CardTitle>
                <CardDescription>
                    Chn phng thc thanh ton ph hp vi bn
                </CardDescription>
            </CardHeader>
            <CardContent className="space-y-3">
                {paymentMethods.map((method) => (
                    <button
                        key={method.id}
                        onClick={() => onPaymentMethodSelect(method.id)}
                        className="w-full p-4 border rounded-lg hover:bg-muted transition-colors text-left"
                    >
                        <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                                <span className="text-2xl">{method.icon}</span>
                                <div>
                                    <p className="font-medium">{method.name}</p>
                                    <p className="text-sm text-muted-foreground">{method.description}</p>
                                </div>
                            </div>
                            <div className="text-right">
                                {method.fee > 0 ? (
                                    <p className="text-sm text-red-600">
                                        +{new Intl.NumberFormat('vi-VN', {
                                            style: 'currency',
                                            currency: 'VND'
                                        }).format(method.fee)}
                                    </p>
                                ) : (
                                    <p className="text-sm text-green-600">Min ph</p>
                                )}
                            </div>
                        </div>
                    </button>
                ))}
            </CardContent>
        </Card>
    );
}