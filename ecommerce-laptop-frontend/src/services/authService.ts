import apiClient from '@/lib/api';
import { LoginRequest, LoginResponse, RegisterRequest, TokenResponse } from '@/types/api';

export const authService = {
    async login(credentials: LoginRequest): Promise<LoginResponse> {
        console.log('[authService] Login attempt for:', credentials.email);
        try {
            const response = await apiClient.post('/auth/login', { ...credentials, context: "customer" });
            const authData = response.data.data; // Backend returns { success: true, data: {...} }
            console.log('[authService] Login success, tokens received:', { hasAccessToken: !!authData.accessToken, hasRefreshToken: !!authData.refreshToken, expiresAt: authData.expiresAt });

            // Note: Session management is handled by NextAuth.js automatically
            // For client-side auth state, use useSession() hook from next-auth/react
            console.log('[authService] Login completed successfully');
            return authData;
        } catch (error: any) {
            console.error('[authService] Login failed:', error.response?.status, error.response?.data || error.message);
            throw error;
        }
    },

    async register(userData: RegisterRequest): Promise<LoginResponse> {
        console.log('[authService] Register attempt for:', userData.email);
        try {
            const response = await apiClient.post('/auth/register', { ...userData, context: "customer" });
            const authData = response.data.data; // Backend returns { success: true, data: {...} }
            console.log('[authService] Register success, tokens received:', { hasAccessToken: !!authData.accessToken, hasRefreshToken: !!authData.refreshToken });
            return authData;
        } catch (error: any) {
            console.error('[authService] Register failed:', error.response?.status, error.response?.data || error.message);
            throw error;
        }
    },

    async refreshToken(refreshToken: string): Promise<TokenResponse> {
        console.log('[authService] Refresh token attempt');
        try {
            const response = await apiClient.post('/auth/refresh', { refreshToken, context: "customer" });
            const authData = response.data.data; // Backend returns { success: true, data: {...} }
            console.log('[authService] Refresh success, new tokens:', { hasAccessToken: !!authData.accessToken, hasRefreshToken: !!authData.refreshToken });

            // Note: Session token updates should be handled through NextAuth.js callback functions
            // See: https://next-auth.js.org/configuration/callbacks#jwt-callback
            return authData;
        } catch (error: any) {
            console.error('[authService] Refresh failed:', error.response?.status, error.response?.data || error.message);
            throw error;
        }
    },

    async logout(refreshToken: string): Promise<{ success: boolean }> {
        console.log('[authService] Logout attempt');
        try {
            const response = await apiClient.post('/auth/logout', { refreshToken, context: "customer" });
            console.log('[authService] Logout backend success:', response.data);
            return response.data; // This endpoint returns { success: boolean } directly
        } catch (error: any) {
            console.error('[authService] Logout failed:', error.response?.status, error.response?.data || error.message);
            return { success: false };
        }
    },

    async changePassword(currentPassword: string, newPassword: string): Promise<{ success: boolean }> {
        console.log('[authService] Change password attempt');
        try {
            const response = await apiClient.post('/auth/change-password', {
                currentPassword,
                newPassword,
                context: "customer"
            });
            console.log('[authService] Password change success:', response.data);
            return response.data; // This endpoint returns { success: boolean } directly
        } catch (error: any) {
            console.error('[authService] Password change failed:', error.response?.status, error.response?.data || error.message);
            throw error;
        }
    },

    async forgotPassword(email: string): Promise<{ success: boolean; message: string }> {
        console.log('[authService] Forgot password for:', email);
        try {
            const response = await apiClient.post('/auth/forgot-password', { email, context: "customer" });
            console.log('[authService] Forgot password response:', response.data.message);
            return response.data; // This endpoint returns { success: boolean, message: string } directly
        } catch (error: any) {
            console.error('[authService] Forgot password failed:', error.response?.status, error.response?.data || error.message);
            throw error;
        }
    },

    async resetPassword(email: string, token: string, newPassword: string, confirmPassword: string): Promise<{ success: boolean; message: string }> {
        console.log('[authService] Reset password with token for email:', email);
        try {
            const response = await apiClient.post('/auth/reset-password', {
                email,
                token,
                newPassword,
                confirmPassword,
                context: "customer"
            });
            console.log('[authService] Reset password success:', response.data.message);
            return response.data; // This endpoint returns { success: boolean, message: string } directly
        } catch (error: any) {
            console.error('[authService] Reset password failed:', error.response?.status, error.response?.data || error.message);
            throw error;
        }
    },
};