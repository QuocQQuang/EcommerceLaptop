'use client';

import { useEffect } from 'react';
import { signOut, getSession } from 'next-auth/react';
import { useRouter } from 'next/navigation';
import { authService } from '@/services/authService';

export default function LogoutPage() {
    const router = useRouter();

    useEffect(() => {
        const performLogout = async () => {
            try {
                console.log("Starting logout process...");

                // Call backend logout first
                try {
                    const session = await getSession();
                    if (session?.refreshToken) {
                        await authService.logout(session.refreshToken);
                        console.log("Backend logout successful");
                    }
                } catch (error) {
                    console.error('Backend logout failed:', error);
                    // Continue with client logout even if backend fails
                }

                // Clear all storage
                if (typeof window !== 'undefined') {
                    localStorage.clear();
                    sessionStorage.clear();
                    
                    // Clear cookies
                    document.cookie.split(";").forEach((c) => {
                        const eqPos = c.indexOf("=");
                        const name = eqPos > -1 ? c.substr(0, eqPos) : c;
                        document.cookie = name + "=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/";
                        document.cookie = name + "=;expires=Thu, 01 Jan 1970 00:00:00 GMT;path=/;domain=localhost";
                    });
                    
                    console.log("Local storage and cookies cleared");
                }

                // Sign out and redirect
                await signOut({ 
                    callbackUrl: '/',
                    redirect: true 
                });
            } catch (error) {
                console.error('Logout error:', error);
                // Fallback redirect
                router.push('/');
            }
        };

        performLogout();
    }, [router]);

    return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50">
            <div className="text-center">
                <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
                <p className="text-gray-600">đang đăng xuất...</p>
            </div>
        </div>
    );
}