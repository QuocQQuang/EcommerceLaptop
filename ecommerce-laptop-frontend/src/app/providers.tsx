'use client';

import { ConditionalProviders } from '@/components/providers/ConditionalProviders';
import { ReactNode } from 'react';

interface ProvidersProps {
    children: ReactNode;
}

export function Providers({ children }: ProvidersProps) {
    return (
        <ConditionalProviders>
            {children}
        </ConditionalProviders>
    );
}