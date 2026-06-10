import apiClient from '@/lib/api';
import { getSession } from 'next-auth/react';

export interface UserProfile {
    id: number; // Changed from string to number to match backend UserProfileDto.Id
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber?: string;
    dateOfBirth?: string;
    gender?: string;
    profilePictureUrl?: string;
    address?: {
        street: string;
        city: string;
        district: string;
        ward: string;
        zipCode: string;
    };
    createdAt: string;
    updatedAt: string;
}

export interface UpdateUserProfileRequest {
    firstName?: string;
    lastName?: string;
    phoneNumber?: string;
    dateOfBirth?: string;
    gender?: string;
    address?: {
        street: string;
        city: string;
        district: string;
        ward: string;
        zipCode: string;
    };
}

export interface ChangePasswordRequest {
    currentPassword: string;
    newPassword: string;
    confirmPassword: string;
}

export interface UserStats {
    totalOrders: number;
    totalSpent: number;
    wishlistCount: number;
    addressCount: number;
}

export const userService = {
    // Get current user profile
    async getProfile(): Promise<{ data: UserProfile }> {
        const response = await apiClient.get('/users/profile');
        return response.data;
    },

    // Update user profile
    async updateProfile(data: UpdateUserProfileRequest): Promise<{ data: UserProfile }> {
        const response = await apiClient.put('/users/profile', data);
        return response.data;
    },

    // Change password
    async changePassword(data: ChangePasswordRequest): Promise<{ message: string }> {
        const response = await apiClient.post('/users/change-password', data);
        return response.data;
    },

    // Get user statistics
    async getUserStats(): Promise<{ data: UserStats }> {
        const response = await apiClient.get('/users/stats');
        return response.data;
    },

    // Upload avatar
    async uploadAvatar(file: File): Promise<{ data: { user: UserProfile; avatarUrl: string; message: string } }> {
        const formData = new FormData();
        formData.append('file', file);

        const response = await apiClient.post('/users/avatar', formData, {
            headers: {
                'Content-Type': 'multipart/form-data',
            },
        });
        return response.data;
    },

    // Delete user account
    async deleteAccount(password: string): Promise<{ message: string }> {
        const response = await apiClient.delete('/users/account', {
            data: { password }
        });
        return response.data;
    },

    // Get user addresses
    async getAddresses(): Promise<{ data: UserProfile['address'][] }> {
        // Get userId from session
        const session = await getSession();
        const userId = session?.user?.id;
        if (!userId) throw new Error('User not authenticated');
        const response = await apiClient.get(`/users/${userId}/addresses`);
        return response.data;
    },

    // Add new address
    async addAddress(addressData: any): Promise<{ data: any }> {
        const session = await getSession();
        const userId = session?.user?.id;
        if (!userId) throw new Error('User not authenticated');

        // Map frontend address format to backend format
        const backendAddress = {
            fullName: addressData.fullName,
            phoneNumber: addressData.phoneNumber,
            street: addressData.street || addressData.address,
            city: addressData.city || '', // Huyn/Qun  City
            province: addressData.province || '', // Tnh  Province
            district: addressData.district || '', // X/Phng  District
            postalCode: addressData.postalCode || addressData.PostalCode,
            country: addressData.country || addressData.Country || 'Việt Nam',
            isDefault: addressData.isDefault || false
        };

        const response = await apiClient.post(`/users/${userId}/addresses`, backendAddress);
        return response.data;
    },

    // Update address
    async updateAddress(addressId: string, addressData: any): Promise<{ data: any }> {
        const session = await getSession();
        const userId = session?.user?.id;
        if (!userId) throw new Error('User not authenticated');

        // Map frontend address format to backend format
        const backendAddress = {
            fullName: addressData.fullName,
            phoneNumber: addressData.phoneNumber,
            street: addressData.street || addressData.address,
            city: addressData.city || '', // Huyn/Qun  City
            province: addressData.province || '', // Tnh  Province
            district: addressData.district || '', // X/Phng  District
            postalCode: addressData.postalCode || addressData.PostalCode,
            country: addressData.country || addressData.Country || 'Việt Nam',
            isDefault: addressData.isDefault || false
        };

        const response = await apiClient.put(`/users/${userId}/addresses/${addressId}`, backendAddress);
        return response.data;
    },

    // Delete address
    async deleteAddress(addressId: string): Promise<{ message: string }> {
        const session = await getSession();
        const userId = session?.user?.id;
        if (!userId) throw new Error('User not authenticated');
        const response = await apiClient.delete(`/users/${userId}/addresses/${addressId}`);
        return response.data;
    },

    // Enable two-factor authentication
    async enableTwoFactor(): Promise<{ data: { qrCode: string; secret: string } }> {
        const response = await apiClient.post('/users/two-factor/enable');
        return response.data;
    },

    // Disable two-factor authentication
    async disableTwoFactor(token: string): Promise<{ message: string }> {
        const response = await apiClient.post('/users/two-factor/disable', { token });
        return response.data;
    },

    // Verify two-factor authentication
    async verifyTwoFactor(token: string): Promise<{ message: string }> {
        const response = await apiClient.post('/users/two-factor/verify', { token });
        return response.data;
    }
    ,
    // Resend email confirmation
    async resendEmailConfirmation(email: string): Promise<{ success: boolean; message: string }> {
        const response = await apiClient.post('/auth/resend-confirmation', { email });
        return response.data;
    }
};