import apiClient from '@/lib/api';
import { useQuery } from '@tanstack/react-query';

interface Product {
    id: number;
    name: string;
    price: number;
    imageUrl?: string;
    image?: string; // Alternative field name
    mainImageUrl?: string; // Another alternative
    shortDescription?: string;
    description?: string;
    sku?: string;
    stock?: number;
    isActive?: boolean;
    createdAt?: string;
    updatedAt?: string;
}

interface ProductsResponse {
    items: Product[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
    hasNextPage: boolean;
    hasPreviousPage: boolean;
}

interface UseProductsQueryParams {
    page?: number;
    pageSize?: number;
    search?: string;
    categoryId?: number;
    brandId?: number;
    minPrice?: number;
    maxPrice?: number;
    isActive?: boolean;
    sortBy?: string;
    sortOrder?: 'asc' | 'desc';
}

export function useProductsQuery(params: UseProductsQueryParams = {}) {
    return useQuery({
        queryKey: ['products', params],
        queryFn: async (): Promise<ProductsResponse> => {
            try {
                const queryParams = {
                    pageNumber: params.page || 1,
                    pageSize: params.pageSize || 12,
                    ...params
                };

                // Remove undefined values
                Object.keys(queryParams).forEach(key => {
                    if (queryParams[key as keyof typeof queryParams] === undefined) {
                        delete queryParams[key as keyof typeof queryParams];
                    }
                });

                console.log('Fetching products with params:', queryParams);

                const response = await apiClient.get('/products', { params: queryParams });
                const data = response.data;

                console.log('Products API response:', data);

                // Handle different response formats
                if (data.success && data.data) {
                    return {
                        items: data.data.items || data.data || [],
                        totalCount: data.data.totalCount || data.data.total || 0,
                        page: data.data.page || data.data.pageNumber || 1,
                        pageSize: data.data.pageSize || 12,
                        totalPages: data.data.totalPages || Math.ceil((data.data.totalCount || 0) / (data.data.pageSize || 12)),
                        hasNextPage: data.data.hasNextPage || false,
                        hasPreviousPage: data.data.hasPreviousPage || false
                    };
                }

                // Fallback for direct array response
                if (Array.isArray(data)) {
                    return {
                        items: data,
                        totalCount: data.length,
                        page: 1,
                        pageSize: data.length,
                        totalPages: 1,
                        hasNextPage: false,
                        hasPreviousPage: false
                    };
                }

                // Fallback for direct object response
                return {
                    items: data.items || data.data || [],
                    totalCount: data.totalCount || data.total || 0,
                    page: data.page || data.pageNumber || 1,
                    pageSize: data.pageSize || 12,
                    totalPages: data.totalPages || 1,
                    hasNextPage: data.hasNextPage || false,
                    hasPreviousPage: data.hasPreviousPage || false
                };
            } catch (error) {
                console.error('Error fetching products:', error);
                throw error;
            }
        },
        staleTime: 5 * 60 * 1000, // 5 minutes
        gcTime: 10 * 60 * 1000, // 10 minutes (renamed from cacheTime in newer versions)
        retry: 3,
        retryDelay: 1000,
    });
}
