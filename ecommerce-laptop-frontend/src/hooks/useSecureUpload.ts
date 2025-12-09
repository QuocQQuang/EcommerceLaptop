/**
 * Secure File Upload Hooks
 * React hooks for secure file uploading with comprehensive validation,
 * progress tracking, and error handling
 */

import { useCallback, useRef, useState } from 'react';
import {
    FileSecurityConfig,
    FileValidationResult,
    SECURITY_CONFIGS,
    sanitizeFilename,
    validateFileSecurityAsync
} from '../lib/upload-security';

// Upload status types
export type UploadStatus = 'idle' | 'validating' | 'uploading' | 'success' | 'error';

// Upload result interface
export interface UploadResult {
    url: string;
    id?: string;
    fileName: string;
    size: number;
    deleteUrl?: string;
}

// Upload progress interface
export interface UploadProgress {
    loaded: number;
    total: number;
    percentage: number;
}

// Secure upload hook state
export interface SecureUploadState {
    status: UploadStatus;
    files: File[];
    validationResults: FileValidationResult[];
    uploadProgress: UploadProgress;
    uploadResults: UploadResult[];
    errors: string[];
    warnings: string[];
}

// Upload options
export interface UploadOptions {
    maxFiles?: number;
    securityConfig?: FileSecurityConfig;
    onProgress?: (progress: UploadProgress) => void;
    onValidation?: (results: FileValidationResult[]) => void;
    onSuccess?: (results: UploadResult[]) => void;
    onError?: (errors: string[]) => void;
}

/**
 * Secure file upload hook with comprehensive validation
 */
export function useSecureUpload(uploadEndpoint: string, options: UploadOptions = {}) {
    const {
        maxFiles = 5,
        securityConfig = SECURITY_CONFIGS.IMAGE,
        onProgress,
        onValidation,
        onSuccess,
        onError
    } = options;

    const [state, setState] = useState<SecureUploadState>({
        status: 'idle',
        files: [],
        validationResults: [],
        uploadProgress: { loaded: 0, total: 0, percentage: 0 },
        uploadResults: [],
        errors: [],
        warnings: []
    });

    const abortControllerRef = useRef<AbortController | null>(null);

    // Reset state
    const reset = useCallback(() => {
        if (abortControllerRef.current) {
            abortControllerRef.current.abort();
        }
        setState({
            status: 'idle',
            files: [],
            validationResults: [],
            uploadProgress: { loaded: 0, total: 0, percentage: 0 },
            uploadResults: [],
            errors: [],
            warnings: []
        });
    }, []);

    // Validate files security
    const validateFiles = useCallback(async (files: FileList | File[]): Promise<FileValidationResult[]> => {
        setState(prev => ({ ...prev, status: 'validating', errors: [], warnings: [] }));

        const fileArray = Array.from(files);
        const validationResults: FileValidationResult[] = [];

        // Check file count limit
        if (fileArray.length > maxFiles) {
            const error = `Ch c upload ti a ${maxFiles} file`;
            setState(prev => ({ ...prev, status: 'error', errors: [error] }));
            onError?.([error]);
            return [];
        }

        // Validate each file
        for (const file of fileArray) {
            try {
                const result = await validateFileSecurityAsync(file, securityConfig);
                validationResults.push(result);
            } catch (error) {
                console.error('File validation error:', error);
                validationResults.push({
                    isValid: false,
                    errors: ['Li khi kim tra file'],
                    warnings: [],
                    securityScore: 0
                });
            }
        }

        // Collect all errors and warnings
        const allErrors = validationResults.flatMap(r => r.errors);
        const allWarnings = validationResults.flatMap(r => r.warnings);

        const hasValidFiles = validationResults.some(r => r.isValid);

        setState(prev => ({
            ...prev,
            status: hasValidFiles ? 'idle' : 'error',
            files: hasValidFiles ? fileArray.filter((_, i) => validationResults[i].isValid) : [],
            validationResults,
            errors: allErrors,
            warnings: allWarnings
        }));

        onValidation?.(validationResults);

        if (allErrors.length > 0) {
            onError?.(allErrors);
        }

        return validationResults;
    }, [maxFiles, securityConfig, onValidation, onError]);

    // Upload files
    const uploadFiles = useCallback(async (files?: FileList | File[]): Promise<UploadResult[]> => {
        let filesToUpload = state.files;

        // If new files provided, validate them first
        if (files) {
            const validationResults = await validateFiles(files);
            const validFiles = Array.from(files).filter((_, i) => validationResults[i]?.isValid);
            if (validFiles.length === 0) {
                return [];
            }
            filesToUpload = validFiles;
        }

        if (filesToUpload.length === 0) {
            const error = 'Khng c file hp l  upload';
            setState(prev => ({ ...prev, status: 'error', errors: [error] }));
            onError?.([error]);
            return [];
        }

        setState(prev => ({ ...prev, status: 'uploading', errors: [], warnings: [] }));

        // Create abort controller for cancellation
        abortControllerRef.current = new AbortController();

        try {
            const uploadResults: UploadResult[] = [];
            const totalSize = filesToUpload.reduce((sum, file) => sum + file.size, 0);
            let loadedSize = 0;

            // Upload files sequentially for better progress tracking
            for (let i = 0; i < filesToUpload.length; i++) {
                const file = filesToUpload[i];
                const sanitizedName = sanitizeFilename(file.name);

                // Create form data
                const formData = new FormData();
                formData.append('file', file, sanitizedName);
                formData.append('originalName', file.name);
                formData.append('securityScore', state.validationResults[i]?.securityScore?.toString() || '0');

                // Upload with progress tracking
                const result = await uploadFileWithProgress(
                    uploadEndpoint,
                    formData,
                    abortControllerRef.current.signal,
                    (loaded, total) => {
                        const currentProgress = {
                            loaded: loadedSize + loaded,
                            total: totalSize,
                            percentage: Math.round(((loadedSize + loaded) / totalSize) * 100)
                        };

                        setState(prev => ({ ...prev, uploadProgress: currentProgress }));
                        onProgress?.(currentProgress);
                    }
                );

                uploadResults.push({
                    ...result,
                    fileName: sanitizedName,
                    size: file.size
                });

                loadedSize += file.size;
            }

            setState(prev => ({
                ...prev,
                status: 'success',
                uploadResults,
                uploadProgress: { loaded: totalSize, total: totalSize, percentage: 100 }
            }));

            onSuccess?.(uploadResults);
            return uploadResults;

        } catch (error: any) {
            if (error.name === 'AbortError') {
                setState(prev => ({ ...prev, status: 'idle' }));
                return [];
            }

            const errorMessage = error?.message || 'Upload tht bi';
            setState(prev => ({ ...prev, status: 'error', errors: [errorMessage] }));
            onError?.([errorMessage]);
            return [];
        }
    }, [state.files, state.validationResults, uploadEndpoint, validateFiles, onProgress, onSuccess, onError]);

    // Cancel upload
    const cancelUpload = useCallback(() => {
        if (abortControllerRef.current) {
            abortControllerRef.current.abort();
        }
        setState(prev => ({ ...prev, status: 'idle' }));
    }, []);

    return {
        ...state,
        validateFiles,
        uploadFiles,
        cancelUpload,
        reset
    };
}

/**
 * Upload file with progress tracking
 */
async function uploadFileWithProgress(
    url: string,
    formData: FormData,
    signal: AbortSignal,
    onProgress: (loaded: number, total: number) => void
): Promise<UploadResult> {
    return new Promise((resolve, reject) => {
        const xhr = new XMLHttpRequest();

        xhr.upload.addEventListener('progress', (event) => {
            if (event.lengthComputable) {
                onProgress(event.loaded, event.total);
            }
        });

        xhr.addEventListener('load', () => {
            if (xhr.status >= 200 && xhr.status < 300) {
                try {
                    const response = JSON.parse(xhr.responseText);

                    // Handle different response formats
                    if (response.data) {
                        resolve(response.data);
                    } else if (response.url) {
                        resolve(response);
                    } else {
                        reject(new Error('Invalid response format'));
                    }
                } catch (error) {
                    reject(new Error('Invalid JSON response'));
                }
            } else {
                reject(new Error(`Upload failed: ${xhr.status} ${xhr.statusText}`));
            }
        });

        xhr.addEventListener('error', () => {
            reject(new Error('Network error'));
        });

        xhr.addEventListener('abort', () => {
            reject(new Error('Upload cancelled'));
        });

        // Handle abort signal
        signal.addEventListener('abort', () => {
            xhr.abort();
        });

        xhr.open('POST', url);

        // Add auth header if available
        const token = localStorage.getItem('token') || sessionStorage.getItem('token');
        if (token) {
            xhr.setRequestHeader('Authorization', `Bearer ${token}`);
        }

        xhr.send(formData);
    });
}

/**
 * Hook for drag and drop file handling with security
 */
export function useSecureDragDrop(onFiles: (files: FileList) => void, securityConfig: FileSecurityConfig) {
    const [isDragActive, setIsDragActive] = useState(false);
    const [dragErrors, setDragErrors] = useState<string[]>([]);

    const handleDragEnter = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        setIsDragActive(true);
        setDragErrors([]);
    }, []);

    const handleDragLeave = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        setIsDragActive(false);
    }, []);

    const handleDragOver = useCallback((e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
    }, []);

    const handleDrop = useCallback(async (e: React.DragEvent) => {
        e.preventDefault();
        e.stopPropagation();
        setIsDragActive(false);

        const files = e.dataTransfer.files;
        if (!files || files.length === 0) return;

        // Quick validation before processing
        const errors: string[] = [];
        const validFiles: File[] = [];

        for (let i = 0; i < files.length; i++) {
            const file = files[i];

            // Basic checks
            if (file.size > securityConfig.maxFileSize) {
                errors.push(`${file.name}: File qu ln`);
                continue;
            }

            if (!securityConfig.allowedMimeTypes.includes(file.type)) {
                errors.push(`${file.name}: Loi file khng c php`);
                continue;
            }

            validFiles.push(file);
        }

        if (errors.length > 0) {
            setDragErrors(errors);
            return;
        }

        if (validFiles.length > 0) {
            const fileList = new DataTransfer();
            validFiles.forEach(file => fileList.items.add(file));
            onFiles(fileList.files);
        }
    }, [onFiles, securityConfig]);

    const dragProps = {
        onDragEnter: handleDragEnter,
        onDragLeave: handleDragLeave,
        onDragOver: handleDragOver,
        onDrop: handleDrop
    };

    return {
        isDragActive,
        dragErrors,
        dragProps,
        clearDragErrors: () => setDragErrors([])
    };
}

/**
 * Hook for image preview with security checks
 */
export function useSecureImagePreview() {
    const [previews, setPreviews] = useState<Array<{ file: File; url: string; isValid: boolean }>>([]);

    const generatePreviews = useCallback(async (files: File[], securityConfig: FileSecurityConfig = SECURITY_CONFIGS.IMAGE) => {
        const newPreviews = await Promise.all(
            Array.from(files).map(async (file) => {
                const validation = await validateFileSecurityAsync(file, securityConfig);
                return {
                    file,
                    url: URL.createObjectURL(file),
                    isValid: validation.isValid
                };
            })
        );

        setPreviews(newPreviews);
    }, []);

    const clearPreviews = useCallback(() => {
        // Clean up object URLs to prevent memory leaks
        previews.forEach(preview => URL.revokeObjectURL(preview.url));
        setPreviews([]);
    }, [previews]);

    const removePreview = useCallback((index: number) => {
        const preview = previews[index];
        if (preview) {
            URL.revokeObjectURL(preview.url);
            setPreviews(prev => prev.filter((_, i) => i !== index));
        }
    }, [previews]);

    return {
        previews,
        generatePreviews,
        clearPreviews,
        removePreview
    };
}