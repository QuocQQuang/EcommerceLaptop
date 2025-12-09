'use client';

import { SessionProvider } from 'next-auth/react';
import { ReactNode } from 'react';

interface CustomSessionProviderProps {
    children: ReactNode;
    refetchInterval?: number;
}

/**
 * A custom session provider to wrap NextAuth's SessionProvider.
 * This allows for more granular control over session polling behavior.
 * By default, it disables polling by setting refetchInterval to 0.
 */
export function CustomSessionProvider({
    children,
    refetchInterval = 0, // Default to 0 to disable polling
}: CustomSessionProviderProps) {
    return (
        <SessionProvider
            refetchInterval={refetchInterval}
            refetchOnWindowFocus={false} // Recommended to keep this false to avoid unnecessary refetches
        >
            {children}
        </SessionProvider>
    );
}
