import apiClient from '@/lib/api';

export interface Brand {
    id: number;
    name: string;
    description?: string;
    logoUrl?: string;
    isActive: boolean;
    productCount?: number;
}

export const brandService = {
    /**
     * Get all active brands
     */
    async getBrands(): Promise<Brand[]> {
        try {
            // Fix: call public endpoint directly (no admin fallback needed for storefront)
            const { data } = await apiClient.get('/products/brands');
            return data.data || data || [];
        } catch (error) {
            console.error('Failed to fetch brands:', error);
            return [];
        }
    },

    /**
     * Get brands with product counts (admin endpoint)
     */
    async getBrandsWithCounts(): Promise<Brand[]> {
        try {
            // Fix: use public brands endpoint to avoid admin 403 on storefront
            // (admin/brands/with-counts requires auth and caused failed requests for non-admin users)
            const { data } = await apiClient.get('/products/brands');
            return data.data || data || [];
        } catch (error) {
            console.error('Failed to fetch brands with counts:', error);
            return [];
        }
    },

    /**
     * Get brands from products endpoint (alternative)
     */
    async getBrandsFromProducts(): Promise<Brand[]> {
        try {
            const { data } = await apiClient.get('/products/brands');
            return data.data || data || [];
        } catch (error) {
            console.error('Failed to fetch brands from products:', error);
            return [];
        }
    }
};