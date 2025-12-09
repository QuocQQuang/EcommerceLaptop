import apiClient from '@/lib/api';

export interface Category {
    id: number;
    name: string;
    description?: string;
    imageUrl?: string;
    parentId?: number;
    isActive: boolean;
    displayOrder?: number;
    sortOrder?: number; // API uses sortOrder
    slug: string;
    productCount?: number;
    children?: Category[];
}

export const categoryService = {
    /**
     * Get all categories as flat list
     */
    async getCategories(): Promise<Category[]> {
        try {
            // Prefer public endpoint if available
            let categories: Category[] = [];
            const { data } = await apiClient.get('/admin/categories');
            categories = data.value || data.data || data || [];

            // API already returns flattened categories, just deduplicate by ID
            const uniqueCategories = new Map<number, Category>();

            categories.forEach((category: Category) => {
                if (!uniqueCategories.has(category.id)) {
                    uniqueCategories.set(category.id, {
                        id: category.id,
                        name: category.name,
                        description: category.description,
                        slug: category.slug,
                        isActive: category.isActive,
                        displayOrder: category.displayOrder || category.sortOrder || 0,
                        parentId: category.parentId,
                        productCount: category.productCount
                    });
                }
            });

            return Array.from(uniqueCategories.values());
        } catch (error) {
            console.error('Failed to fetch categories:', error);
            return [];
        }
    },

    /**
     * Get categories in hierarchical tree structure
     */
    async getCategoryTree(): Promise<Category[]> {
        try {
            // Call known existing admin route first to avoid 404 noise
            try {
                const { data } = await apiClient.get('/admin/categories/tree');
                return data.data || data || [];
            } catch {
                // Fallback: try potential public route if backend exposes it
                const { data } = await apiClient.get('/products/categories/tree');
                return data.data || data || [];
            }
        } catch (error) {
            console.error('Failed to fetch category tree:', error);
            return [];
        }
    },

    /**
     * Get categories with product counts
     */
    async getCategoriesWithProductCount(): Promise<Category[]> {
        try {
            // Use existing admin endpoint first to prevent 404 logs
            try {
                const { data } = await apiClient.get('/admin/categories/with-counts');
                return data.data || data || [];
            } catch {
                // Fallback to public route if available
                const { data } = await apiClient.get('/products/categories/with-counts');
                return data.data || data || [];
            }
        } catch (error) {
            console.error('Failed to fetch categories with product count:', error);
            return [];
        }
    },

    /**
     * Get categories from system settings (alternative)
     */
    async getCategoriesFromSettings(): Promise<Category[]> {
        try {
            const { data } = await apiClient.get('/system-settings/categories');
            return data.data || data || [];
        } catch (error) {
            console.error('Failed to fetch categories from settings:', error);
            return [];
        }
    }
};