'use client';

import { cn } from '@/lib/utils';
import { useUIStore } from '@/store/uiStore';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import React from 'react';

interface AppLinkProps {
  href: string;
  children: React.ReactNode;
  className?: string;
  pageType?: 'static' | 'dashboard' | 'default';
  showLoading?: boolean;
  loadingKey?: string;
  replace?: boolean;
  scroll?: boolean;
  prefetch?: boolean;
  onClick?: (e: React.MouseEvent<HTMLAnchorElement>) => void;
}

/**
 * Enhanced Link component with loading states and type-specific styling
 * Replaces all internal navigation with Next.js Link + loading management
 */
export function AppLink({
  href,
  children,
  className,
  pageType = 'default',
  showLoading = true,
  loadingKey,
  replace = false,
  scroll = true,
  prefetch = true,
  onClick,
  ...props
}: AppLinkProps) {
  const { setLoading } = useUIStore();
  const router = useRouter();

  const handleClick = (e: React.MouseEvent<HTMLAnchorElement>) => {
    // Call custom onClick if provided
    if (onClick) {
      onClick(e);
    }

    // Set loading state if enabled
    if (showLoading && loadingKey) {
      setLoading(loadingKey, true);

      // Clear loading after navigation (fallback)
      setTimeout(() => {
        setLoading(loadingKey, false);
      }, 2000);
    }

    // Set page-specific loading states
    if (pageType === 'static') {
      setLoading('static-page-loading', true);
    }
  };

  return (
    <Link
      href={href}
      replace={replace}
      scroll={scroll}
      prefetch={prefetch}
      className={cn(
        'transition-colors duration-200',
        // Static page specific styling
        pageType === 'static' && [
          'focus:ring-2 focus:ring-blue-500 focus:ring-offset-2',
          'hover:text-blue-600',
          'underline-offset-4 hover:underline'
        ],
        // Dashboard specific styling  
        pageType === 'dashboard' && [
          'font-medium',
          'hover:bg-gray-50 hover:text-gray-900',
          'focus:bg-gray-100 focus:text-gray-900',
          'px-2 py-1 rounded-md',
          'flex items-center'
        ],
        // Default styling
        pageType === 'default' && [
          'hover:text-blue-600',
          'focus:text-blue-700'
        ],
        className
      )}
      onClick={handleClick}
      {...props}
    >
      {children}
    </Link>
  );
}

/**
 * Hook for managing loading states with AppLink
 */
export function useAppLinkLoading(key: string) {
  const { loadingStates, setLoading } = useUIStore();

  const isLoading = loadingStates[key] || false;

  const clearLoading = () => {
    setLoading(key, false);
  };

  return { isLoading, clearLoading };
}

/**
 * Higher-order component for pages that need to clear loading states
 */
export function withLinkLoadingClear<T extends object>(
  Component: React.ComponentType<T>,
  loadingKeys: string[] = []
) {
  return function WrappedComponent(props: T) {
    const { setLoading } = useUIStore();

    React.useEffect(() => {
      // Clear specified loading states when component mounts
      loadingKeys.forEach(key => {
        setLoading(key, false);
      });

      // Clear static page loading
      setLoading('static-page-loading', false);
    }, [setLoading]);

    return <Component {...props} />;
  };
}