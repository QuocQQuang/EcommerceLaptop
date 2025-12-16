'use client';

import { CurrencyUpdateProvider } from '@/components/providers/CurrencyUpdateProvider';
import { CustomSessionProvider } from '@/components/providers/CustomSessionProvider';
import { CurrencyProvider } from '@/contexts/CurrencyContext';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { usePathname } from 'next/navigation';
import { ReactNode, useState } from 'react';
import { ChatbotProvider } from '@/components/Chatbot/ChatbotProvider';

interface ConditionalProvidersProps {
    children: ReactNode;
}

/**
 * ConditionalProviders component that decides whether to wrap children
 * with NextAuth SessionProvider based on the current route.
 * Admin routes use their own AdminAuthProvider instead of NextAuth.
 */
export function ConditionalProviders({ children }: ConditionalProvidersProps) {
    const pathname = usePathname();
    const [queryClient] = useState(() => new QueryClient({
        defaultOptions: {
            queries: {
                staleTime: 5 * 60 * 1000, // 5 minutes
                gcTime: 10 * 60 * 1000, // 10 minutes
                retry: 3,
                retryDelay: 1000,
            },
        },
    }));

    // Check if current route is an admin route
    const isAdminRoute = pathname?.startsWith('/admin') || pathname?.startsWith('/admin-login');

    // If it's an admin route, don't wrap with NextAuth SessionProvider
    if (isAdminRoute) {
        return (
            <QueryClientProvider client={queryClient}>
                <CurrencyProvider>
                    <CurrencyUpdateProvider>
                        {children}
                    </CurrencyUpdateProvider>
                </CurrencyProvider>
            </QueryClientProvider>
        );
    }

    // For non-admin routes, use NextAuth SessionProvider
    return (
        <QueryClientProvider client={queryClient}>
            <CustomSessionProvider>
                <CurrencyProvider>
                    <CurrencyUpdateProvider>
                        {children}
                        <ChatbotProvider />
                    </CurrencyUpdateProvider>
                </CurrencyProvider>
            </CustomSessionProvider>
        </QueryClientProvider>
    );
}