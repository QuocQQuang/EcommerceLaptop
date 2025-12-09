/**
 * Validation Schemas
 * 
 * Centralized Zod schemas for consistent form validation across the application.
 * These schemas include both client-side validation and sanitization.
 * 
 * @see FRONTEND_FORM_HANDLING_AND_SECURITY.md
 */

import { z } from 'zod';
import { sanitizeHtml, sanitizeText } from './sanitize';

// Common validation patterns
const vietnameseNameRegex = /^[a-zA-Z-\s]+$/;
const vietnamesePhoneRegex = /^(\+84|0)[0-9]{9,10}$/;
const strongPasswordRegex = /^(?=(?!.*(.)\1{2}))(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/;

// Base user validation schema
export const baseUserSchema = z.object({
    firstName: z
        .string()
        .min(3, 'H phi c t nht 3 k t')
        .max(50, 'H khng c qu 50 k t')
        .regex(vietnameseNameRegex, 'H ch c cha ch ci v khong trng')
        .transform((val) => sanitizeText(val)),

    lastName: z
        .string()
        .min(3, 'Tn phi c t nht 3 k t')
        .max(50, 'Tn khng c qu 50 k t')
        .regex(vietnameseNameRegex, 'Tn ch c cha ch ci v khong trng')
        .transform((val) => sanitizeText(val)),

    email: z
        .string()
        .min(1, 'Email khng c  trng')
        .email('Email khng hp l')
        .max(254, 'Email khng c qu 254 k t')
        .transform((val) => sanitizeText(val.toLowerCase())),

    phoneNumber: z
        .string()
        .optional()
        .refine((val) => !val || vietnamesePhoneRegex.test(val), {
            message: 'S in thoi khng hp l (VD: 0901234567 hoc +84901234567)'
        })
        .transform((val) => val ? sanitizeText(val) : undefined),
});

// Authentication schemas
export const loginSchema = z.object({
    email: z
        .string()
        .min(1, 'Email khng c  trng')
        .email('Email khng hp l')
        .transform((val) => sanitizeText(val.toLowerCase())),

    password: z
        .string()
        .min(1, 'Mt khu khng c  trng')
        .min(6, 'Mt khu phi c t nht 6 k t'),

    rememberMe: z.boolean().optional().default(false),
});

export const registerSchema = baseUserSchema.extend({
    password: z
        .string()
        .min(8, 'Mt khu phi c t nht 8 k t')
        .regex(strongPasswordRegex, 'Mt khu phi c t nht 1 ch hoa, 1 ch thng, 1 s v 1 k t c bit')
        .refine((val) => !isSequential(val), { message: 'Mt khu khng c cha chui k t lin tip' }),

    confirmPassword: z.string(),

    agreeToTerms: z
        .boolean()
        .refine((val) => val === true, {
            message: 'Bn phi ng  vi iu khon s dng'
        }),
}).refine((data) => data.password === data.confirmPassword, {
    message: 'Mt khu xc nhn khng khp',
    path: ['confirmPassword'],
});

// Profile update schemas
export const profileSchema = baseUserSchema.partial().extend({
    bio: z
        .string()
        .max(500, 'Tiu s khng c qu 500 k t')
        .optional()
        .transform((val) => val ? sanitizeText(val) : undefined),
});

export const passwordChangeSchema = z.object({
    currentPassword: z
        .string()
        .min(1, 'Mt khu hin ti l bt buc'),

    newPassword: z
        .string()
        .min(8, 'Mt khu mi phi c t nht 8 k t')
        .regex(strongPasswordRegex, 'Mt khu phi c t nht 1 ch hoa, 1 ch thng, 1 s v 1 k t c bit'),

    confirmPassword: z.string(),
}).refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Mt khu xc nhn khng khp',
    path: ['confirmPassword'],
});

// Admin user creation schema
export const adminUserSchema = baseUserSchema.extend({
    password: z
        .string()
        .min(8, 'Mt khu phi c t nht 8 k t')
        .regex(strongPasswordRegex, 'Mt khu phi c t nht 1 ch hoa, 1 ch thng, 1 s v 1 k t c bit'),

    confirmPassword: z.string(),

    roleId: z
        .number()
        .min(1, 'Vui lng chn vai tr'),

    isActive: z.boolean().default(true),

    bio: z
        .string()
        .max(500, 'Tiu s khng c qu 500 k t')
        .optional()
        .transform((val) => val ? sanitizeText(val) : undefined),

    sendWelcomeEmail: z.boolean().default(true),
}).refine((data) => data.password === data.confirmPassword, {
    message: 'Mt khu xc nhn khng khp',
    path: ['confirmPassword'],
});

// Product schemas
export const productSchema = z.object({
    name: z
        .string()
        .min(1, 'Tn sn phm khng c  trng')
        .max(200, 'Tn sn phm khng c qu 200 k t')
        .transform((val) => sanitizeText(val)),

    description: z
        .string()
        .min(1, 'M t sn phm khng c  trng')
        .max(5000, 'M t sn phm khng c qu 5000 k t')
        .transform((val) => sanitizeHtml(val)),

    price: z
        .number()
        .positive('Gi phi ln hn 0')
        .max(999999999, 'Gi khng c qu 999,999,999')
        .multipleOf(0.01, 'Gi ch c c ti a 2 ch s thp phn'),

    stock: z
        .number()
        .int('S lng phi l s nguyn')
        .min(0, 'S lng khng c m')
        .max(99999, 'S lng khng c qu 99,999'),

    categoryId: z
        .number()
        .min(1, 'Vui lng chn danh mc'),

    brand: z
        .string()
        .min(1, 'Thng hiu khng c  trng')
        .max(100, 'Thng hiu khng c qu 100 k t')
        .transform((val) => sanitizeText(val)),

    model: z
        .string()
        .min(1, 'Model khng c  trng')
        .max(100, 'Model khng c qu 100 k t')
        .transform((val) => sanitizeText(val)),

    sku: z
        .string()
        .min(1, 'SKU khng c  trng')
        .max(50, 'SKU khng c qu 50 k t')
        .regex(/^[A-Z0-9_-]+$/, 'SKU ch c cha ch hoa, s, du gch ngang v gch di')
        .transform((val) => sanitizeText(val.toUpperCase())),

    isActive: z.boolean().default(true),

    specifications: z
        .record(z.string())
        .optional()
        .transform((val) => {
            if (!val) return val;
            const sanitized: Record<string, string> = {};
            for (const [key, value] of Object.entries(val)) {
                sanitized[sanitizeText(key)] = sanitizeText(value);
            }
            return sanitized;
        }),
});

// Review schema
export const reviewSchema = z.object({
    rating: z
        .number()
        .int('nh gi phi l s nguyn')
        .min(1, 'nh gi ti thiu l 1 sao')
        .max(5, 'nh gi ti a l 5 sao'),

    title: z
        .string()
        .min(1, 'Tiu  khng c  trng')
        .max(100, 'Tiu  khng c qu 100 k t')
        .transform((val) => sanitizeText(val)),

    comment: z
        .string()
        .min(1, 'Bnh lun khng c  trng')
        .max(1000, 'Bnh lun khng c qu 1000 k t')
        .transform((val) => sanitizeText(val)),

    productId: z
        .number()
        .min(1, 'ID sn phm khng hp l'),
});

// Contact form schema
export const contactSchema = z.object({
    name: z
        .string()
        .min(1, 'Tn khng c  trng')
        .max(100, 'Tn khng c qu 100 k t')
        .transform((val) => sanitizeText(val)),

    email: z
        .string()
        .min(1, 'Email khng c  trng')
        .email('Email khng hp l')
        .transform((val) => sanitizeText(val.toLowerCase())),

    phone: z
        .string()
        .optional()
        .refine((val) => !val || vietnamesePhoneRegex.test(val), {
            message: 'S in thoi khng hp l'
        })
        .transform((val) => val ? sanitizeText(val) : undefined),

    subject: z
        .string()
        .min(1, 'Ch  khng c  trng')
        .max(200, 'Ch  khng c qu 200 k t')
        .transform((val) => sanitizeText(val)),

    message: z
        .string()
        .min(1, 'Tin nhn khng c  trng')
        .max(2000, 'Tin nhn khng c qu 2000 k t')
        .transform((val) => sanitizeText(val)),
});

// File upload validation
export const imageUploadSchema = z.object({
    file: z
        .instanceof(File)
        .refine((file) => file.size <= 10 * 1024 * 1024, {
            message: 'File khng c ln hn 10MB'
        })
        .refine((file) => {
            const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
            return allowedTypes.includes(file.type);
        }, {
            message: 'Ch chp nhn file nh nh dng JPEG, PNG, WebP'
        })
        .refine((file) => {
            // Additional filename validation
            const allowedExtensions = /\.(jpg|jpeg|png|webp)$/i;
            return allowedExtensions.test(file.name);
        }, {
            message: 'Tn file phi c ui .jpg, .jpeg, .png hoc .webp'
        }),
});

// Type exports for TypeScript
export type LoginFormData = z.infer<typeof loginSchema>;
export type RegisterFormData = z.infer<typeof registerSchema>;
export type ProfileFormData = z.infer<typeof profileSchema>;
export type PasswordChangeFormData = z.infer<typeof passwordChangeSchema>;
export type AdminUserFormData = z.infer<typeof adminUserSchema>;
export type ProductFormData = z.infer<typeof productSchema>;
export type ReviewFormData = z.infer<typeof reviewSchema>;
export type ContactFormData = z.infer<typeof contactSchema>;
export type ImageUploadFormData = z.infer<typeof imageUploadSchema>;

export const isSequential = (password: string): boolean => {
    for (let i = 0; i < password.length - 2; i++) {
        if (/\d/.test(password[i]) && /\d/.test(password[i + 1]) && /\d/.test(password[i + 2])) {
            const num1 = parseInt(password[i]);
            const num2 = parseInt(password[i + 1]);
            const num3 = parseInt(password[i + 2]);
            if (num2 === num1 + 1 && num3 === num1 + 2) return true;
            if (num2 === num1 - 1 && num3 === num1 - 2) return true;
        }
    }
    return false;
};