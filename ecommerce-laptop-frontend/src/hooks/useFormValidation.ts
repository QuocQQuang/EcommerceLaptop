/**
 * Form Validation Hook
 * 
 * Custom hook for consistent form validation using Zod schemas with debounced validation
 * and comprehensive error handling.
 * 
 * @see FRONTEND_FORM_HANDLING_AND_SECURITY.md
 */

import { useCallback, useRef, useState } from 'react';
import { z } from 'zod';

interface ValidationError {
  [key: string]: string;
}

interface UseFormValidationOptions {
  debounceMs?: number;
  validateOnChange?: boolean;
  validateOnBlur?: boolean;
}

export function useFormValidation<T extends z.ZodTypeAny>(
  schema: T,
  options: UseFormValidationOptions = {}
) {
  const {
    debounceMs = 300,
    validateOnChange = true,
    validateOnBlur = true
  } = options;

  const [errors, setErrors] = useState<ValidationError>({});
  const [touched, setTouched] = useState<Record<string, boolean>>({});
  const [isValidating, setIsValidating] = useState(false);
  const debounceTimeouts = useRef<Record<string, NodeJS.Timeout>>({});

  const clearFieldError = useCallback((fieldName: string) => {
    setErrors(prev => {
      const newErrors = { ...prev };
      delete newErrors[fieldName];
      return newErrors;
    });
  }, []);

  const setFieldError = useCallback((fieldName: string, message: string) => {
    setErrors(prev => ({
      ...prev,
      [fieldName]: message
    }));
  }, []);

  const validateField = useCallback((fieldName: string, value: any) => {
    // Clear existing timeout
    if (debounceTimeouts.current[fieldName]) {
      clearTimeout(debounceTimeouts.current[fieldName]);
    }

    debounceTimeouts.current[fieldName] = setTimeout(() => {
      setIsValidating(true);

      try {
        // Resolve underlying object shape even when schema is wrapped by effects/refine
        const resolveObjectShape = (s: z.ZodTypeAny): Record<string, z.ZodTypeAny> | null => {
          if (s instanceof z.ZodObject) {
            return s.shape;
          }
          // ZodEffects wraps inner schema under _def.schema
          const inner: unknown = (s as any)?._def?.schema;
          if (inner && inner instanceof z.ZodType) {
            return resolveObjectShape(inner as z.ZodTypeAny);
          }
          return null;
        };

        const shape = resolveObjectShape(schema);
        if (shape) {
          const fieldSchema = shape[fieldName];
          if (fieldSchema) {
            fieldSchema.parse(value);
            clearFieldError(fieldName);
          }
        } else {
          // Fallback: validate against the whole schema
          schema.parse(value);
          clearFieldError(fieldName);
        }
      } catch (error) {
        if (error instanceof z.ZodError) {
          const fieldError = error.errors.find(err =>
            err.path.length === 0 || err.path[0] === fieldName
          );
          if (fieldError) {
            setFieldError(fieldName, fieldError.message);
          }
        }
      } finally {
        setIsValidating(false);
      }
    }, debounceMs);
  }, [schema, debounceMs, clearFieldError, setFieldError]);

  const validateAllFields = useCallback((data: any): boolean => {
    try {
      schema.parse(data);
      setErrors({});
      return true;
    } catch (error) {
      if (error instanceof z.ZodError) {
        const newErrors: ValidationError = {};
        error.errors.forEach(err => {
          const fieldName = err.path.join('.');
          newErrors[fieldName] = err.message;
        });
        setErrors(newErrors);
      }
      return false;
    }
  }, [schema]);

  const handleFieldChange = useCallback((fieldName: string, value: any) => {
    if (validateOnChange && touched[fieldName]) {
      validateField(fieldName, value);
    }
  }, [validateField, validateOnChange, touched]);

  const handleFieldBlur = useCallback((fieldName: string, value: any) => {
    setTouched(prev => ({ ...prev, [fieldName]: true }));

    if (validateOnBlur) {
      validateField(fieldName, value);
    }
  }, [validateField, validateOnBlur]);

  const reset = useCallback(() => {
    setErrors({});
    setTouched({});
    setIsValidating(false);

    // Clear all pending timeouts
    Object.values(debounceTimeouts.current).forEach(timeout => {
      clearTimeout(timeout);
    });
    debounceTimeouts.current = {};
  }, []);

  const getFieldProps = useCallback((fieldName: string) => ({
    error: errors[fieldName],
    touched: touched[fieldName],
    hasError: Boolean(errors[fieldName] && touched[fieldName]),
  }), [errors, touched]);

  // Cleanup timeouts on unmount
  const cleanup = useCallback(() => {
    Object.values(debounceTimeouts.current).forEach(timeout => {
      clearTimeout(timeout);
    });
    debounceTimeouts.current = {};
  }, []);

  const markFieldTouched = useCallback((fieldName: string) => {
    setTouched(prev => ({ ...prev, [fieldName]: true }));
  }, []);

  return {
    errors,
    touched,
    isValidating,
    validateField,
    validateAllFields,
    handleFieldChange,
    handleFieldBlur,
    clearFieldError,
    setFieldError,
    markFieldTouched,
    reset,
    getFieldProps,
    cleanup,
    hasErrors: Object.keys(errors).length > 0,
    isFieldValid: (fieldName: string) => !errors[fieldName] && touched[fieldName],
  };
}

/**
 * Safe form submission hook
 * Handles form submission with validation, loading states, and error handling
 */
export function useSafeFormSubmit<T extends z.ZodTypeAny>(
  schema: T,
  onSubmit: (data: z.infer<T>) => Promise<void> | void
) {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const validation = useFormValidation(schema);

  const handleSubmit = useCallback(async (data: any, event?: React.FormEvent) => {
    if (event) {
      event.preventDefault();
    }

    setSubmitError(null);

    // Validate all fields first
    if (!validation.validateAllFields(data)) {
      return;
    }

    setIsSubmitting(true);

    try {
      // Parse and sanitize data with schema
      const cleanData = schema.parse(data);
      await onSubmit(cleanData);
    } catch (error) {
      if (error instanceof z.ZodError) {
        // Handle validation errors - set errors for each field
        error.errors.forEach(err => {
          const fieldName = err.path.join('.');
          validation.setFieldError(fieldName, err.message);
        });
      } else {
        // Handle submission errors
        setSubmitError(error instanceof Error ? error.message : ' xy ra li khng xc nh');
      }
    } finally {
      setIsSubmitting(false);
    }
  }, [schema, onSubmit, validation]);

  return {
    ...validation,
    isSubmitting,
    submitError,
    handleSubmit,
    clearSubmitError: () => setSubmitError(null)
  };
}

// Type definitions for better TypeScript support
export type FormValidationHook<T extends z.ZodTypeAny> = ReturnType<typeof useFormValidation<T>>;
export type SafeFormSubmitHook<T extends z.ZodTypeAny> = ReturnType<typeof useSafeFormSubmit<T>>;