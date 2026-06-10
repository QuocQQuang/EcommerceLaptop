'use client';

import { useUserStore } from '@/store/userStore';
import { signOut, useSession } from 'next-auth/react';
import { useEffect } from 'react';
import { toast } from 'sonner';

export function useAuth() {
    // useSession will now inherit the polling behavior from CustomSessionProvider
    const { data: session, status } = useSession();
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

        // Handle session errors globally
        // Only show the error toast if the user is supposed to be authenticated
        // but the session has an error. This prevents the toast from showing during/after logout.
        if (status === 'authenticated' && session?.error === "RefreshAccessTokenError") {
            console.log("useAuth detected RefreshAccessTokenError while authenticated, signing out.");
            toast.error("Phiên đăng nhập đã hết hạn", {
                description: "Vui lòng đăng nhập lại để tiếp tục.",
                action: {
                    label: "Đăng nhập",
                    onClick: () => window.location.href = '/auth/login',
                },
            });
            // Use signOut to clear the session cookie and redirect
            signOut({ redirect: false }); // redirect: false to prevent NextAuth's default redirect
        }

    }, [session, status, setUser, clearUser, setLoading]);

    // Determine user role for easier access
    const userRole = user?.roles?.[0] || user?.role;
    const isAuthenticated = status === 'authenticated';
    const isAdmin = userRole === 'Admin' || userRole === 'SuperAdmin';
    const isCustomer = userRole === 'Customer' || (!userRole && isAuthenticated);

    return {
        user,
        session,
        userRole,
        isAdmin,
        isCustomer,
        isLoading: status === 'loading',
        isAuthenticated,
        isUnauthenticated: status === 'unauthenticated',
    };
}
