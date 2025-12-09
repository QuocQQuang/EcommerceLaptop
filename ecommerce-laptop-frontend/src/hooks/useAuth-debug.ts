'use client';

import { useUserStore } from '@/store/userStore';
import { useSession } from 'next-auth/react';
import { useEffect } from 'react';

export function useAuth() {
    const { data: session, status } = useSession(); // Use default without polling config for now
    const { user, setUser, clearUser, setLoading } = useUserStore();

    useEffect(() => {
        setLoading(status === 'loading');

        if (status === 'authenticated' && session?.user) {
            setUser({
                id: session.user.id,
                email: session.user.email,
                firstName: session.user.firstName,
                lastName: session.user.lastName,
                phoneNumber: '',
                profilePictureUrl: session.user.profilePictureUrl,
                roles: session.user.roles,
                isActive: true,
                createdAt: '',
            });
        } else if (status === 'unauthenticated') {
            clearUser();
        }
    }, [session, status, setUser, clearUser, setLoading]);

    return {
        user,
        session,
        isLoading: status === 'loading',
        isAuthenticated: status === 'authenticated',
        isUnauthenticated: status === 'unauthenticated',
    };
}