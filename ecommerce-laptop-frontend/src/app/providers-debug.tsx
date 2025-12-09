import { SessionProvider } from 'next-auth/react';
import { ReactNode } from 'react';

interface ProvidersProps {
    children: ReactNode;
}

export function Providers({ children }: ProvidersProps) {
    return (
        <SessionProvider
            refetchInterval={0} // Completely disable automatic refetching for debugging
            refetchOnWindowFocus={false} // Don't refetch when window gains focus
            refetchWhenOffline={false} // Don't refetch when offline
        >
            {children}
        </SessionProvider>
    );
}