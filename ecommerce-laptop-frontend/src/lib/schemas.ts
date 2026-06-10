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
        .min(3, 'Họ phải có ít nhất 3 ký tự')
        .max(50, 'Họ không được quá 50 ký tự')
        .regex(vietnameseNameRegex, 'Họ chỉ chứa chữ cái và khoảng trắng')
        .transform((val) => sanitizeText(val)),

    lastName: z
        .string()
        .min(3, 'Tên phải có ít nhất 3 ký tự')
        .max(50, 'Tên không được quá 50 ký tự')
        .regex(vietnameseNameRegex, 'Tên chỉ chứa chữ cái và khoảng trắng')
        .transform((val) => sanitizeText(val)),

    email: z
        .string()
        .min(1, 'Email không được để trống')
        .email('Email không hợp lệ')
        .max(254, 'Email không được quá 254 ký tự')
        .transform((val) => sanitizeText(val.toLowerCase())),

    phoneNumber: z
        .string()
        .optional()
        .refine((val) => !val || vietnamesePhoneRegex.test(val), {
            message: 'Số điện thoại không hợp lệ (VD: 0901234567 hoặc +84901234567)'
        })
        .transform((val) => val ? sanitizeText(val) : undefined),
});

// Authentication schemas
export const loginSchema = z.object({
    email: z
        .string()
        .min(1, 'Email không được để trống')
        .email('Email không hợp lệ')
        .transform((val) => sanitizeText(val.toLowerCase())),

    password: z
        .string()
        .min(1, 'Mật khẩu không được để trống')
        .min(6, 'Mật khẩu phải có ít nhất 6 ký tự'),

    rememberMe: z.boolean().optional().default(false),
});

export const registerSchema = baseUserSchema.extend({
    password: z
        .string()
        .min(8, 'Mật khẩu phải có ít nhất 8 ký tự')
        .regex(strongPasswordRegex, 'Mật khẩu phải có ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt')
        .refine((val) => !isSequential(val), { message: 'Mật khẩu không được chứa chuỗi ký tự liên tiếp' }),

    confirmPassword: z.string(),

    agreeToTerms: z
        .boolean()
        .refine((val) => val === true, {
            message: 'Bạn phải đồng ý với điều khoản sử dụng'
        }),
}).refine((data) => data.password === data.confirmPassword, {
    message: 'Mật khẩu xác nhận không khớp',
    path: ['confirmPassword'],
});

// Profile update schemas
export const profileSchema = baseUserSchema.partial().extend({
    bio: z
        .string()
        .max(500, 'Tiểu sử không được quá 500 ký tự')
        .optional()
        .transform((val) => val ? sanitizeText(val) : undefined),
});

export const passwordChangeSchema = z.object({
    currentPassword: z
        .string()
        .min(1, 'Mật khẩu hiện tại là bắt buộc'),

    newPassword: z
        .string()
        .min(8, 'Mật khẩu mới phải có ít nhất 8 ký tự')
        .regex(strongPasswordRegex, 'Mật khẩu phải có ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt'),

    confirmPassword: z.string(),
}).refine((data) => data.newPassword === data.confirmPassword, {
    message: 'Mật khẩu xác nhận không khớp',
    path: ['confirmPassword'],
});

// Admin user creation schema
export const adminUserSchema = baseUserSchema.extend({
    password: z
        .string()
        .min(8, 'Mật khẩu phải có ít nhất 8 ký tự')
        .regex(strongPasswordRegex, 'Mật khẩu phải có ít nhất 1 chữ hoa, 1 chữ thường, 1 số và 1 ký tự đặc biệt'),

    confirmPassword: z.string(),

    roleId: z
        .number()
        .min(1, 'Vui lòng chọn vai trò'),

    isActive: z.boolean().default(true),

    bio: z
        .string()
        .max(500, 'Tiểu sử không được quá 500 ký tự')
        .optional()
        .transform((val) => val ? sanitizeText(val) : undefined),

    sendWelcomeEmail: z.boolean().default(true),
}).refine((data) => data.password === data.confirmPassword, {
    message: 'Mật khẩu xác nhận không khớp',
    path: ['confirmPassword'],
});

// Product schemas
export const productSchema = z.object({
    name: z
        .string()
        .min(1, 'Tên sản phẩm không được để trống')
        .max(200, 'Tên sản phẩm không được quá 200 ký tự')
        .transform((val) => sanitizeText(val)),

    description: z
        .string()
        .min(1, 'Mô tả sản phẩm không được để trống')
        .max(5000, 'Mô tả sản phẩm không được quá 5000 ký tự')
        .transform((val) => sanitizeHtml(val)),

    price: z
        .number()
        .positive('Giá phải lớn hơn 0')
        .max(999999999, 'Giá không được quá 999,999,999')
        .multipleOf(0.01, 'Giá chỉ có thể có tối đa 2 chữ số thập phân'),

    stock: z
        .number()
        .int('Số lượng phải là số nguyên')
        .min(0, 'Số lượng không được âm')
        .max(99999, 'Số lượng không được quá 99,999'),

    categoryId: z
        .number()
        .min(1, 'Vui lòng chọn danh mục'),

    brand: z
        .string()
        .min(1, 'Thương hiệu không được để trống')
        .max(100, 'Thương hiệu không được quá 100 ký tự')
        .transform((val) => sanitizeText(val)),

    model: z
        .string()
        .min(1, 'Model không được để trống')
        .max(100, 'Model không được quá 100 ký tự')
        .transform((val) => sanitizeText(val)),

    sku: z
        .string()
        .min(1, 'SKU không được để trống')
        .max(50, 'SKU không được quá 50 ký tự')
        .regex(/^[A-Z0-9_-]+$/, 'SKU chỉ có thể chứa chữ hoa, số, dấu gạch ngang và gạch dưới')
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
        .int('Đánh giá phải là số nguyên')
        .min(1, 'Đánh giá tối thiểu là 1 sao')
        .max(5, 'Đánh giá tối đa là 5 sao'),

    title: z
        .string()
        .min(1, 'Tiêu đề không được để trống')
        .max(100, 'Tiêu đề không được quá 100 ký tự')
        .transform((val) => sanitizeText(val)),

    comment: z
        .string()
        .min(1, 'Bình luận không được để trống')
        .max(1000, 'Bình luận không được quá 1000 ký tự')
        .transform((val) => sanitizeText(val)),

    productId: z
        .number()
        .min(1, 'ID sản phẩm không hợp lệ'),
});

// Contact form schema
export const contactSchema = z.object({
    name: z
        .string()
        .min(1, 'Tên không được để trống')
        .max(100, 'Tên không được quá 100 ký tự')
        .transform((val) => sanitizeText(val)),

    email: z
        .string()
        .min(1, 'Email không được để trống')
        .email('Email không hợp lệ')
        .transform((val) => sanitizeText(val.toLowerCase())),

    phone: z
        .string()
        .optional()
        .refine((val) => !val || vietnamesePhoneRegex.test(val), {
            message: 'Số điện thoại không hợp lệ'
        })
        .transform((val) => val ? sanitizeText(val) : undefined),

    subject: z
        .string()
        .min(1, 'Chủ đề không được để trống')
        .max(200, 'Chủ đề không được quá 200 ký tự')
        .transform((val) => sanitizeText(val)),

    message: z
        .string()
        .min(1, 'Tin nhắn không được để trống')
        .max(2000, 'Tin nhắn không được quá 2000 ký tự')
        .transform((val) => sanitizeText(val)),
});

// File upload validation
export const imageUploadSchema = z.object({
    file: z
        .instanceof(File)
        .refine((file) => file.size <= 10 * 1024 * 1024, {
            message: 'File không được lớn hơn 10MB'
        })
        .refine((file) => {
            const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'];
            return allowedTypes.includes(file.type);
        }, {
            message: 'Chỉ chấp nhận file ảnh định dạng JPEG, PNG, WebP'
        })
        .refine((file) => {
            // Additional filename validation
            const allowedExtensions = /\.(jpg|jpeg|png|webp)$/i;
            return allowedExtensions.test(file.name);
        }, {
            message: 'Tên file phải có đuôi .jpg, .jpeg, .png hoặc .webp'
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
