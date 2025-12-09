'use client';

import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { useUIStore } from '@/store/uiStore';
import { Loader2 } from 'lucide-react';
import React from 'react';

interface LoadingButtonProps extends React.ComponentProps<typeof Button> {
  loading?: boolean;
  loadingText?: string;
  loadingKey?: string;
  onClick?: (e: React.MouseEvent<HTMLButtonElement>) => void | Promise<void>;
}

/**
 * Enhanced Button component with automatic loading state management
 * Integrates with Zustand UI store for global loading states
 */
export function LoadingButton({
  children,
  loading = false,
  loadingText = "ang ti...",
  loadingKey,
  onClick,
  disabled,
  className,
  ...props
}: LoadingButtonProps) {
  const { loadingStates, setLoading } = useUIStore();
  const [localLoading, setLocalLoading] = React.useState(false);

  // Determine if button should show loading state
  const isLoading = loading ||
    localLoading ||
    (loadingKey && loadingStates[loadingKey]);

  const handleClick = async (e: React.MouseEvent<HTMLButtonElement>) => {
    if (isLoading || disabled) {
      e.preventDefault();
      return;
    }

    // Set loading states
    if (loadingKey) {
      setLoading(loadingKey, true);
    } else {
      setLocalLoading(true);
    }

    try {
      await onClick?.(e);
    } catch (error) {
      console.error('LoadingButton onClick error:', error);
    } finally {
      // Clear loading states
      if (loadingKey) {
        setLoading(loadingKey, false);
      } else {
        setLocalLoading(false);
      }
    }
  };

  return (
    <Button
      {...props}
      onClick={handleClick}
      disabled={isLoading || disabled}
      className={cn(
        'relative',
        isLoading && 'cursor-not-allowed',
        className
      )}
    >
      {isLoading && (
        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
      )}
      {isLoading ? loadingText : children}
    </Button>
  );
}

/**
 * LoadingButton variant for form submissions
 */
export function SubmitButton({
  children = "Gi",
  loadingText = "ang gi...",
  ...props
}: Omit<LoadingButtonProps, 'type'>) {
  return (
    <LoadingButton
      type="submit"
      loadingText={loadingText}
      {...props}
    >
      {children}
    </LoadingButton>
  );
}

/**
 * LoadingButton variant for async actions with confirmation
 */
interface ConfirmLoadingButtonProps extends LoadingButtonProps {
  confirmMessage?: string;
  confirmTitle?: string;
}

export function ConfirmLoadingButton({
  confirmMessage = "Bn c chc chn mun thc hin hnh ng ny?",
  confirmTitle = "Xc nhn",
  onClick,
  children,
  ...props
}: ConfirmLoadingButtonProps) {
  const handleClick = async (e: React.MouseEvent<HTMLButtonElement>) => {
    const confirmed = window.confirm(`${confirmTitle}\n\n${confirmMessage}`);

    if (confirmed) {
      await onClick?.(e);
    }
  };

  return (
    <LoadingButton
      onClick={handleClick}
      {...props}
    >
      {children}
    </LoadingButton>
  );
}

/**
 * Hook for managing button loading states
 */
export function useButtonLoading(key: string) {
  const { loadingStates, setLoading } = useUIStore();

  const isLoading = loadingStates[key] || false;

  const withLoading = React.useCallback(async (fn: () => Promise<void>) => {
    setLoading(key, true);
    try {
      await fn();
    } finally {
      setLoading(key, false);
    }
  }, [key, setLoading]);

  const startLoading = () => setLoading(key, true);
  const stopLoading = () => setLoading(key, false);

  return { isLoading, withLoading, startLoading, stopLoading };
}