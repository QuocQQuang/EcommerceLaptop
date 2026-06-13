import apiClient from '@/lib/api';

export interface Brand {
    id: number;
    name: string;
    description?: string;
    logoUrl?: string;
    isActive: boolean;
    productCount?: number;
}

const normalizeBrands = (data: any): Brand[] => {
    const brands = data?.data || data || [];

    return brands.map((brand: any, index: number) => {
        if (typeof brand === 'string') {
            return {
                id: index + 1,
                name: brand,
                isActive: true,
            };
        }

        return {
            id: brand.id ?? brand.Id ?? index + 1,
            name: brand.name ?? brand.Name ?? '',
            description: brand.description ?? brand.Description,
            logoUrl: brand.logoUrl ?? brand.LogoUrl,
            isActive: brand.isActive ?? brand.IsActive ?? true,
            productCount: brand.productCount ?? brand.ProductCount,
        };
    }).filter((brand: Brand) => brand.name);
};

export const brandService = {
    /**
     * Get all active brands
     */
    async getBrands(): Promise<Brand[]> {
        try {
            const { data } = await apiClient.get('/products/brands');
            return normalizeBrands(data);
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
            return normalizeBrands(data);
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
            return normalizeBrands(data);
        } catch (error) {
            console.error('Failed to fetch brands from products:', error);
            return [];
        }
    }
};
