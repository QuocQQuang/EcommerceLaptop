/**
 * File Upload Security Module
 * Comprehensive security utilities for file uploads with MIME validation,
 * content scanning, and malware detection
 */

import { z } from 'zod';

// MIME type to extension mapping for validation
const MIME_TYPE_MAP: Record<string, string[]> = {
    'image/jpeg': ['.jpg', '.jpeg'],
    'image/jpg': ['.jpg', '.jpeg'],
    'image/png': ['.png'],
    'image/webp': ['.webp'],
    'image/gif': ['.gif'],
    'image/svg+xml': ['.svg'],
    'application/pdf': ['.pdf'],
    'text/plain': ['.txt'],
    'application/msword': ['.doc'],
    'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx']
};

// Known malicious file signatures
const MALICIOUS_SIGNATURES: Array<{ name: string; signature: number[]; offset: number }> = [
    // PE executable header
    { name: 'PE_EXECUTABLE', signature: [0x4D, 0x5A], offset: 0 },
    // ELF executable header
    { name: 'ELF_EXECUTABLE', signature: [0x7F, 0x45, 0x4C, 0x46], offset: 0 },
    // ZIP with suspicious content
    { name: 'ZIP_EXECUTABLE', signature: [0x50, 0x4B, 0x03, 0x04], offset: 0 },
    // Script signatures in images
    { name: 'PHP_IN_IMAGE', signature: [0x3C, 0x3F, 0x70, 0x68, 0x70], offset: -1 }, // <?php
    { name: 'SCRIPT_TAG', signature: [0x3C, 0x73, 0x63, 0x72, 0x69, 0x70, 0x74], offset: -1 } // <script
];

// File validation configuration
export interface FileSecurityConfig {
    maxFileSize: number;
    allowedMimeTypes: string[];
    allowedExtensions: string[];
    scanForMalware: boolean;
    sanitizeFilenames: boolean;
    checkMagicBytes: boolean;
    // When true, perform only extension (format) check and skip other validations
    minimalChecks?: boolean;
}

// Default security configurations by category
export const SECURITY_CONFIGS: Record<string, FileSecurityConfig> = {
    IMAGE: {
        maxFileSize: 10 * 1024 * 1024, // 10MB
        allowedMimeTypes: ['image/jpeg', 'image/jpg', 'image/png', 'image/webp', 'image/gif'],
        allowedExtensions: ['.jpg', '.jpeg', '.png', '.webp', '.gif'],
        scanForMalware: false,
        sanitizeFilenames: true,
        checkMagicBytes: false,
        minimalChecks: true
    },
    AVATAR: {
        maxFileSize: 5 * 1024 * 1024, // 5MB
        allowedMimeTypes: ['image/jpeg', 'image/jpg', 'image/png', 'image/webp'],
        allowedExtensions: ['.jpg', '.jpeg', '.png', '.webp'],
        scanForMalware: false,
        sanitizeFilenames: true,
        checkMagicBytes: false,
        minimalChecks: true
    },
    DOCUMENT: {
        maxFileSize: 25 * 1024 * 1024, // 25MB
        allowedMimeTypes: ['application/pdf', 'text/plain', 'application/msword', 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'],
        allowedExtensions: ['.pdf', '.txt', '.doc', '.docx'],
        scanForMalware: true,
        sanitizeFilenames: true,
        checkMagicBytes: true
    }
};

// File validation result
export interface FileValidationResult {
    isValid: boolean;
    errors: string[];
    warnings: string[];
    sanitizedName?: string;
    detectedMimeType?: string;
    securityScore: number; // 0-100, higher is safer
}

/**
 * Validates file security comprehensively
 */
export async function validateFileSecurityAsync(
    file: File,
    config: FileSecurityConfig
): Promise<FileValidationResult> {
    const errors: string[] = [];
    const warnings: string[] = [];
    let securityScore = 100;
    let sanitizedName = file.name;

    try {
        // Minimal mode: only sanitize name and check extension
        if (config.minimalChecks) {
            if (!file || file.size === 0) {
                errors.push('File khng hp l hoc rng');
                return { isValid: false, errors, warnings, securityScore: 0 };
            }

            if (config.sanitizeFilenames) {
                sanitizedName = sanitizeFilename(file.name);
                if (sanitizedName !== file.name) {
                    warnings.push('Tn file  c lm sch  m bo an ton');
                    securityScore -= 2;
                }
            }

            const fileExtension = getFileExtension(sanitizedName).toLowerCase();
            if (!config.allowedExtensions.includes(fileExtension)) {
                errors.push(`nh dng file khng c php. Cho php: ${config.allowedExtensions.join(', ')}`);
                securityScore -= 50;
            }

            return {
                isValid: errors.length === 0,
                errors,
                warnings,
                sanitizedName,
                detectedMimeType: undefined,
                securityScore: Math.max(0, securityScore)
            };
        }

        // 1. Basic file validation
        if (!file || file.size === 0) {
            errors.push('File khng hp l hoc rng');
            return { isValid: false, errors, warnings, securityScore: 0 };
        }

        // 2. File size validation
        if (file.size > config.maxFileSize) {
            errors.push(`File qu ln. Ti a: ${formatFileSize(config.maxFileSize)}`);
            securityScore -= 20;
        }

        // 3. Filename sanitization
        if (config.sanitizeFilenames) {
            sanitizedName = sanitizeFilename(file.name);
            if (sanitizedName !== file.name) {
                warnings.push('Tn file  c lm sch  m bo an ton');
                securityScore -= 5;
            }
        }

        // 4. Extension validation
        const fileExtension = getFileExtension(sanitizedName).toLowerCase();
        if (!config.allowedExtensions.includes(fileExtension)) {
            warnings.push(`nh dng file khng nm trong danh sch  xut. Cho php: ${config.allowedExtensions.join(', ')}`);
            securityScore -= 10;
        }

        // 5. MIME type validation
        if (!config.allowedMimeTypes.includes(file.type)) {
            warnings.push(`Loi file khng nm trong danh sch  xut. Cho php: ${config.allowedMimeTypes.join(', ')}`);
            securityScore -= 10;
        }

        // 6. MIME type vs extension consistency
        const expectedExtensions = MIME_TYPE_MAP[file.type] || [];
        if (expectedExtensions.length > 0 && !expectedExtensions.includes(fileExtension)) {
            warnings.push('nh dng file c th khng khp vi loi ni dung');
            securityScore -= 5;
        }

        // 7. Magic bytes validation (if enabled)
        let detectedMimeType: string | undefined;
        if (config.checkMagicBytes) {
            detectedMimeType = await detectMimeTypeFromContent(file);
            if (detectedMimeType && detectedMimeType !== file.type) {
                warnings.push('Loi file thc t khc vi loi file c khai bo');
                securityScore -= 15;
            }
        }

        // 8. Malware scanning (if enabled)
        if (config.scanForMalware) {
            const malwareResult = await scanForMalware(file);
            if (malwareResult.detected) {
                errors.push(`Pht hin ni dung ng ng: ${malwareResult.threats.join(', ')}`);
                securityScore -= 50;
            } else if (malwareResult.suspicious) {
                warnings.push('File c mt s c im ng ng');
                securityScore -= 10;
            }
        }

        // 9. Additional image-specific validation (relaxed: treat failures as warnings)
        if (file.type.startsWith('image/')) {
            try {
                const imageValidation = await validateImageSecurity(file);
                if (!imageValidation.isValid) {
                    // Relaxed: do not block upload on image validation failures
                    warnings.push(...imageValidation.errors);
                    securityScore -= 5;
                }
                warnings.push(...imageValidation.warnings);
            } catch {
                // Any unexpected validation error becomes a warning in relaxed mode
                warnings.push('Khng th xc minh nh, vn cho php upload');
                securityScore -= 5;
            }
        }

        return {
            isValid: errors.length === 0,
            errors,
            warnings,
            sanitizedName,
            detectedMimeType,
            securityScore: Math.max(0, securityScore)
        };

    } catch (error) {
        console.error('File validation error:', error);
        return {
            isValid: false,
            errors: ['Li khi kim tra file'],
            warnings,
            securityScore: 0
        };
    }
}

/**
 * Sanitizes filename to prevent path traversal and injection attacks
 */
export function sanitizeFilename(filename: string): string {
    // Remove or replace dangerous characters
    return filename
        .replace(/[<>:"/\\|?*\x00-\x1f]/g, '_') // Windows forbidden chars
        .replace(/^\.+/, '') // Leading dots
        .replace(/\.+$/, '') // Trailing dots
        .replace(/\s+/g, '_') // Spaces to underscores
        .replace(/_{2,}/g, '_') // Multiple underscores
        .substring(0, 255) // Limit length
        .trim();
}

/**
 * Detects MIME type from file content using magic bytes
 */
async function detectMimeTypeFromContent(file: File): Promise<string | undefined> {
    const buffer = await file.slice(0, 64).arrayBuffer();
    const bytes = new Uint8Array(buffer);

    // Common file type signatures
    const signatures: Array<{ mime: string; signature: number[]; offset: number }> = [
        { mime: 'image/jpeg', signature: [0xFF, 0xD8, 0xFF], offset: 0 },
        { mime: 'image/png', signature: [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], offset: 0 },
        { mime: 'image/gif', signature: [0x47, 0x49, 0x46, 0x38], offset: 0 },
        { mime: 'image/webp', signature: [0x52, 0x49, 0x46, 0x46], offset: 0 },
        { mime: 'application/pdf', signature: [0x25, 0x50, 0x44, 0x46], offset: 0 }
    ];

    for (const sig of signatures) {
        if (matchesSignature(bytes, sig.signature, sig.offset)) {
            return sig.mime;
        }
    }

    return undefined;
}

/**
 * Scans file for malicious content
 */
async function scanForMalware(file: File): Promise<{ detected: boolean; suspicious: boolean; threats: string[] }> {
    const threats: string[] = [];
    let suspicious = false;

    try {
        // Read first 1KB for signature scanning
        const buffer = await file.slice(0, 1024).arrayBuffer();
        const bytes = new Uint8Array(buffer);

        // Check for known malicious signatures
        for (const sig of MALICIOUS_SIGNATURES) {
            if (sig.offset === -1) {
                // Search anywhere in buffer
                if (searchBytes(bytes, sig.signature)) {
                    threats.push(sig.name);
                }
            } else {
                // Check at specific offset
                if (matchesSignature(bytes, sig.signature, sig.offset)) {
                    threats.push(sig.name);
                }
            }
        }

        // Additional heuristic checks
        const content = new TextDecoder('utf-8', { fatal: false }).decode(bytes);

        // Check for suspicious script content
        const suspiciousPatterns = [
            /javascript:/i,
            /vbscript:/i,
            /onload=/i,
            /onerror=/i,
            /eval\(/i,
            /document\.write/i,
            /window\.location/i
        ];

        for (const pattern of suspiciousPatterns) {
            if (pattern.test(content)) {
                suspicious = true;
                break;
            }
        }

        return {
            detected: threats.length > 0,
            suspicious,
            threats
        };

    } catch (error) {
        console.error('Malware scan error:', error);
        return { detected: false, suspicious: true, threats: ['SCAN_ERROR'] };
    }
}

/**
 * Validates image-specific security concerns
 */
async function validateImageSecurity(file: File): Promise<{ isValid: boolean; errors: string[]; warnings: string[] }> {
    const errors: string[] = [];
    const warnings: string[] = [];

    try {
        // Check if image can be loaded (basic corruption check)
        const img = new Image();
        const objectUrl = URL.createObjectURL(file);

        const loadPromise = new Promise<void>((resolve, reject) => {
            img.onload = () => {
                URL.revokeObjectURL(objectUrl);

                // Check for suspicious dimensions
                if (img.width * img.height > 50_000_000) { // 50MP limit
                    warnings.push('nh c  phn gii rt cao');
                }

                if (img.width < 10 || img.height < 10) {
                    warnings.push('nh c kch thc nh bt thng');
                }

                resolve();
            };

            img.onerror = () => {
                URL.revokeObjectURL(objectUrl);
                reject(new Error('Khng th ti nh'));
            };

            // Timeout after 5 seconds
            setTimeout(() => {
                URL.revokeObjectURL(objectUrl);
                reject(new Error('Timeout khi ti nh'));
            }, 5000);
        });

        img.src = objectUrl;
        await loadPromise;

        return { isValid: errors.length === 0, errors, warnings };

    } catch (error) {
        errors.push('nh khng hp l hoc b li');
        return { isValid: false, errors, warnings };
    }
}

/**
 * Helper function to match byte signature
 */
function matchesSignature(bytes: Uint8Array, signature: number[], offset: number): boolean {
    if (offset + signature.length > bytes.length) return false;

    for (let i = 0; i < signature.length; i++) {
        if (bytes[offset + i] !== signature[i]) return false;
    }

    return true;
}

/**
 * Helper function to search for bytes anywhere in buffer
 */
function searchBytes(bytes: Uint8Array, signature: number[]): boolean {
    for (let i = 0; i <= bytes.length - signature.length; i++) {
        if (matchesSignature(bytes, signature, i)) return true;
    }
    return false;
}

/**
 * Gets file extension from filename
 */
function getFileExtension(filename: string): string {
    const lastDot = filename.lastIndexOf('.');
    return lastDot === -1 ? '' : filename.substring(lastDot);
}

/**
 * Formats file size for display
 */
function formatFileSize(bytes: number): string {
    const units = ['B', 'KB', 'MB', 'GB'];
    let size = bytes;
    let unitIndex = 0;

    while (size >= 1024 && unitIndex < units.length - 1) {
        size /= 1024;
        unitIndex++;
    }

    return `${size.toFixed(1)} ${units[unitIndex]}`;
}

// Enhanced Zod schemas with security validation
export const secureImageUploadSchema = z.object({
    file: z
        .instanceof(File)
        .refine(async (file) => {
            const result = await validateFileSecurityAsync(file, SECURITY_CONFIGS.IMAGE);
            return result.isValid;
        }, {
            message: 'File khng p ng yu cu bo mt'
        })
});

export const secureAvatarUploadSchema = z.object({
    file: z
        .instanceof(File)
        .refine(async (file) => {
            const result = await validateFileSecurityAsync(file, SECURITY_CONFIGS.AVATAR);
            return result.isValid;
        }, {
            message: 'Avatar khng p ng yu cu bo mt'
        })
});

export type SecureImageUploadFormData = z.infer<typeof secureImageUploadSchema>;
export type SecureAvatarUploadFormData = z.infer<typeof secureAvatarUploadSchema>;