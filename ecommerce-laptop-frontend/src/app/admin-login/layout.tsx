'use client';

import { AdminAuthProvider } from '@/contexts/AdminAuthContext';
import { ReactNode } from 'react';

interface AdminLoginLayoutProps {
    children: ReactNode;
}

export default function AdminLoginLayout({ children }: AdminLoginLayoutProps) {
    return (
        <AdminAuthProvider>
            {children}
        </AdminAuthProvider>
    );
}