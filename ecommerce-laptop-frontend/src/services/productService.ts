
import apiClient from '@/lib/api';
import { getApiUrl } from '@/lib/utils';
import { PaginatedResponse, Product, ProductSearchParams } from '@/types/api';

// Normalize image paths coming from backend. If backend returns a relative path
// (e.g. `/uploads/xyz.png`) we need to prefix it with the API base URL so the
// Next.js image optimizer can fetch it. If it's already an absolute URL, leave it.
function normalizeImagePath(path: string | undefined | null, fallback = '/images/placeholder-product.jpg') {
    if (!path) return fallback;

    let normalized = path;

    // If already absolute, return as-is but enhance Unsplash URLs
    if (/^https?:\/\//i.test(path)) {
        // Detect Unsplash URLs and append optimization params
        if (path.includes('unsplash.com')) {
            // Avoid duplicating params if already present
            const url = new URL(path);
            if (!url.searchParams.has('w')) {
                url.searchParams.append('w', '800');
                url.searchParams.append('fit', 'crop');
                url.searchParams.append('auto', 'format');
                url.searchParams.append('q', '80');
                normalized = url.toString();
            }
        }
        return normalized;
    }

    // Ensure path starts with a leading slash and prefix with API base URL
    const p = path.startsWith('/') ? path : `/${path}`;
    // Assumes backend serves uploaded files under the same API host (e.g. /uploads/...)
    return `${getApiUrl('')}${p}`;
}

// Transform backend ProductDto to frontend Product type
const transformProduct = (backendProduct: any): Product => {
    const images = backendProduct.Images ?? backendProduct.images ?? [];
    const primaryImage = images.find((img: any) => img.IsPrimary || img.isPrimary) || images[0];
    const getImageUrl = (img: any) => img?.ImageUrl ?? img?.imageUrl ?? img?.url;

    // Transform images to match new ProductImage interface
    const transformedImages = images?.length > 0
        ? images.map((img: any) => ({
            id: img.Id ?? img.id ?? 0,
            imageUrl: normalizeImagePath(img.ImageUrl ?? img.imageUrl ?? img.url ?? ''),
            altText: img.AltText ?? img.altText ?? '',
            sortOrder: img.SortOrder ?? img.sortOrder ?? 0,
            isPrimary: img.IsPrimary ?? img.isPrimary ?? false
        }))
        : [{
            id: 0,
            imageUrl: '/images/placeholder-product.jpg',
            altText: 'Product placeholder image',
            sortOrder: 0,
            isPrimary: true
        }];

    const name = backendProduct.name || backendProduct.Name || '';

    // Auto-generate slug for SEO-friendly URLs when backend misses data
    const generatedSlug = name.toLowerCase()
        .replace(/\s+/g, '-')
        .replace(/[^a-z0-9-]/g, '')
        .replace(/-+/g, '-')
        .replace(/^-|-$/g, '');

    // Strategy:
    // 1. Prefer Backend Slug (Canonical)
    // 2. Fallback: GeneratedSlug + "-" + ID (e.g., "macbook-pro-123")
    //    - Appending ID ensures uniqueness and allows us to resolve the product 
    //      even if the slug text changes or isn't in the DB.
    // 3. Last Resort: "product-" + ID
    let slug = backendProduct.slug || backendProduct.Slug;
    if (!slug) {
        if (generatedSlug) {
            slug = `${generatedSlug}-${backendProduct.id}`;
        } else {
            slug = `product-${backendProduct.id}`;
        }
    }

    const parentProductId = backendProduct.parentProductId ?? backendProduct.ParentProductId ?? null;
    const variants = backendProduct.variants ?? backendProduct.Variants ?? [];
    const productType = backendProduct.productType ?? backendProduct.ProductType;

    return {
        ...backendProduct,
        slug: slug,
        imageUrl: normalizeImagePath(getImageUrl(primaryImage)),
        images: transformedImages,
        specifications: backendProduct.specifications || [], // Map from backend if available
        // Prefer availableQuantity; fallback to quantityInStock
        stockQuantity: backendProduct.inventory?.availableQuantity
            ?? backendProduct.inventory?.AvailableQuantity
            ?? backendProduct.inventory?.quantityInStock
            ?? backendProduct.inventory?.QuantityInStock
            ?? 0,

        // Variant support
        parentProductId: parentProductId ?? undefined,
        variantName: backendProduct.variantName ?? backendProduct.VariantName,
        variantSku: backendProduct.variantSku ?? backendProduct.VariantSku,
        isVariant: backendProduct.isVariant ?? backendProduct.IsVariant ?? parentProductId !== null,
        isBaseProduct: backendProduct.isBaseProduct ?? backendProduct.IsBaseProduct ?? parentProductId === null,
        variants: variants.map((v: any) => transformProduct(v)),

        // Handle TPT-specific fields
        ...(productType === 'Laptop' && {
            type: 'Laptop' as const,
            cpuBrand: backendProduct.cpuBrand ?? backendProduct.CpuBrand,
            cpuModel: backendProduct.cpuModel ?? backendProduct.CpuModel,
            cpuCores: backendProduct.cpuCores ?? backendProduct.CpuCores,
            ramCapacityGB: backendProduct.ramCapacityGB ?? backendProduct.RamCapacityGB,
            ramType: backendProduct.ramType ?? backendProduct.RamType,
            storageCapacityGB: backendProduct.storageCapacityGB ?? backendProduct.StorageCapacityGB,
            storageType: backendProduct.storageType ?? backendProduct.StorageType,
            displaySizeInches: backendProduct.displaySizeInches ?? backendProduct.DisplaySizeInches,
            displayResolution: backendProduct.displayResolution ?? backendProduct.DisplayResolution,
            battery: backendProduct.batteryCapacityWh ?? backendProduct.BatteryCapacityWh,
            weight: backendProduct.weightKg ?? backendProduct.WeightKg,
            warranty: backendProduct.warrantyPeriod ?? backendProduct.WarrantyPeriod,
            // Provide a sensible specifications fallback if backend doesn't send structured specs
            specifications: backendProduct.specifications || [
                { id: 1, name: 'CPU', value: `${backendProduct.CpuBrand ?? ''} ${backendProduct.CpuModel ?? ''}`, category: 'Hardware' },
                { id: 2, name: 'RAM', value: `${backendProduct.RamCapacityGB ?? ''}GB ${backendProduct.RamType ?? ''}`, category: 'Hardware' },
                { id: 3, name: 'Storage', value: `${backendProduct.StorageCapacityGB ?? ''}GB ${backendProduct.StorageType ?? ''}`, category: 'Hardware' },
                { id: 4, name: 'Display', value: `${backendProduct.DisplaySizeInches ?? ''}" ${backendProduct.DisplayResolution ?? ''}`, category: 'Display' },
            ],
        }),

        // Bundle-specific mapping
        ...((productType === 'Bundle' || backendProduct.BundleItems || backendProduct.bundleItems) && {
            type: 'Bundle' as const,
            bundleItems: (backendProduct.BundleItems ?? backendProduct.bundleItems)?.map((item: any) => ({
                id: item.Id ?? item.id ?? 0,
                productId: item.ProductId ?? item.productId,
                productName: item.ProductName ?? item.productName ?? item.Name ?? '',
                productSku: item.ProductSku ?? item.productSku ?? '',
                productImageUrl: item.ProductImageUrl ?? item.productImageUrl,
                brand: item.Brand ?? item.brand ?? '',
                originalPrice: item.OriginalPrice ?? item.originalPrice ?? 0,
                quantity: item.Quantity ?? item.quantity ?? 1,
                discountPercentage: item.DiscountPercentage ?? item.discountPercentage ?? 0,
                discountedPrice: item.DiscountedPrice ?? item.discountedPrice ?? 0,
                totalPrice: item.TotalPrice ?? item.totalPrice ?? 0,
                isAvailable: item.IsAvailable ?? item.isAvailable ?? true,
                stockQuantity: item.StockQuantity ?? item.stockQuantity ?? 0,
            })) || [],
            discountPercentage: backendProduct.DiscountPercentage ?? backendProduct.discountPercentage ?? 0,
            validFrom: backendProduct.ValidFrom ?? backendProduct.validFrom,
            validTo: backendProduct.ValidTo ?? backendProduct.validTo,
        }),
    };
};

export const productService = {
    async getProducts(params: ProductSearchParams = {}): Promise<PaginatedResponse<Product>> {
        const { data } = await apiClient.get('/products', { params });
        // Backend returns { success: true, data: [...], totalCount, page, ... }
        return {
            items: (data.data || []).map((p: any) => transformProduct(p)),
            totalCount: data.totalCount || 0,
            page: data.page || 1,
            pageSize: data.pageSize || 10,
            totalPages: data.totalPages || 0,
            hasNextPage: data.hasNextPage || false,
            hasPreviousPage: data.hasPreviousPage || false
        };
    },

    async getProductById(id: number): Promise<Product> {
        const { data } = await apiClient.get(`/products/${id}`);
        // Normalize wrapper responses (some backend endpoints return { success, data })
        let backendProduct = data?.data ?? data;

        // Handle stringified inner response (parse if string)
        if (typeof backendProduct === 'string') {
            try {
                const parsed = JSON.parse(backendProduct);
                backendProduct = parsed.data || parsed;
            } catch (e) {
                console.error('Failed to parse stringified product data:', e);
                throw new Error('Invalid product data format');
            }
        }

        // Ensure backendProduct is an object
        if (typeof backendProduct !== 'object' || backendProduct === null) {
            throw new Error('Invalid product data structure');
        }

        return transformProduct(backendProduct);
    },

    async getProductBySlug(slug: string): Promise<Product> {
        // Fallback 1: Check for "product-{id}" format
        if (slug.startsWith('product-') && /^\d+$/.test(slug.replace('product-', ''))) {
            const id = parseInt(slug.replace('product-', ''), 10);
            if (!isNaN(id)) {
                return this.getProductById(id);
            }
        }

        // Fallback 2: Check for "{slug}-{id}" format (Suffix ID strategy)
        // Regex looks for a hyphen followed by numbers at the end of the string
        const suffixIdMatch = slug.match(/-(\d+)$/);
        if (suffixIdMatch) {
            const id = parseInt(suffixIdMatch[1], 10);
            if (!isNaN(id)) {
                // Try fetching by ID directly since we extracted a valid ID
                try {
                    return await this.getProductById(id);
                } catch (error) {
                    console.warn(`Failed to resolve slug "${slug}" via suffix ID ${id}, falling back to API slug lookup.`);
                    // Fallthrough to normal slug lookup if ID lookup fails (rare but safe)
                }
            }
        }

        try {
            // Use the new dedicated slug endpoint
            const { data } = await apiClient.get(`/products/slug/${slug}`);

            // Normalize wrapper responses (some backend endpoints return { success, data })
            let backendProduct = data?.data ?? data;

            // Handle stringified inner response (parse if string)
            if (typeof backendProduct === 'string') {
                try {
                    const parsed = JSON.parse(backendProduct);
                    backendProduct = parsed.data || parsed;
                } catch (e) {
                    console.error('Failed to parse stringified product data:', e);
                    throw new Error('Invalid product data format');
                }
            }

            // Ensure backendProduct is an object
            if (typeof backendProduct !== 'object' || backendProduct === null) {
                throw new Error('Invalid product data structure');
            }

            return transformProduct(backendProduct);
        } catch (error) {
            console.error('Error in getProductBySlug:', error);
            throw error;
        }
    },

    // Variant methods
    async getVariants(productId: number): Promise<Product[]> {
        const { data } = await apiClient.get(`/products/${productId}/variants`);
        return (data.data || []).map((p: any) => transformProduct(p));
    },

    async getBaseProduct(variantId: number): Promise<Product> {
        const { data } = await apiClient.get(`/products/${variantId}/base-product`);
        return transformProduct(data.data);
    },

    async getProductsWithVariants(params: ProductSearchParams = {}): Promise<PaginatedResponse<Product>> {
        const { data } = await apiClient.get('/products/with-variants', { params });
        return {
            items: (data.data || []).map((p: any) => transformProduct(p)),
            totalCount: data.totalCount || 0,
            page: data.page || 1,
            pageSize: data.pageSize || 10,
            totalPages: data.totalPages || 0,
            hasNextPage: data.hasNextPage || false,
            hasPreviousPage: data.hasPreviousPage || false
        };
    },

    async createVariant(productId: number, variantData: any): Promise<Product> {
        const { data } = await apiClient.post(`/products/${productId}/variants`, variantData);
        return transformProduct(data.data);
    },

    async updateVariant(productId: number, variantId: number, variantData: any): Promise<Product> {
        const { data } = await apiClient.put(`/products/${productId}/variants/${variantId}`, variantData);
        return transformProduct(data.data);
    },

    async deleteVariant(productId: number, variantId: number): Promise<void> {
        await apiClient.delete(`/products/${productId}/variants/${variantId}`);
    }
};
