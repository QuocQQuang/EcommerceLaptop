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
            // Prefer public endpoint if available
            try {
                const { data } = await apiClient.get('/products/brands');
                return data.data || data || [];
            } catch {
                // Fallback to admin endpoint (requires proper auth); not ideal for storefront
                const { data } = await apiClient.get('/admin/brands');
                return data.value || data.data || data || [];
            }
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
            const { data } = await apiClient.get('/admin/brands/with-counts');
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