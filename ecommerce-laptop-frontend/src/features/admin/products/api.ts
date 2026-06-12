import type { Product } from '@/types/api';
import { ApiResponse, api, unwrapApiArray, unwrapApiData } from '@/lib/admin/http';
import { getBrands } from '../catalog/api';
import type { AdminProductsParams, AdminProductsResponse, ImageUploadResult } from './types';

const normalizeProduct = (product: Product): Product => {
  const parentProductId = product.parentProductId ?? null;
  const variants = Array.isArray(product.variants)
    ? product.variants.map(normalizeProduct)
    : [];

  return {
    ...product,
    parentProductId: parentProductId ?? undefined,
    isVariant: product.isVariant ?? parentProductId !== null,
    isBaseProduct: product.isBaseProduct ?? parentProductId === null,
    variants
  };
};

export const getAdminProducts = async (params: AdminProductsParams = {}): Promise<AdminProductsResponse> => {
  const backendParams: any = { ...params };
  if (backendParams.limit && !backendParams.pageSize) {
    backendParams.pageSize = backendParams.limit;
  }
  delete backendParams.limit;
  if (backendParams.productType === 'all') {
    delete backendParams.productType;
  }
  if (typeof backendParams.isActive === 'boolean' && !backendParams.status) {
    backendParams.status = backendParams.isActive ? 'active' : 'inactive';
  }
  delete backendParams.isActive;

  const response = await api.get<any>('/products/admin', { params: backendParams });
  const raw = response.data;
  const products = raw.data ?? raw.items ?? raw.products ?? [];

  return {
    products: products.map(normalizeProduct),
    totalCount: raw.totalCount || 0,
    currentPage: raw.page || 1,
    totalPages: raw.totalPages || 0,
    pageSize: raw.pageSize || 20
  };
};

export const getAdminProduct = async (productId: number): Promise<Product> => {
  const response = await api.get<ApiResponse<Product>>(`/products/admin/${productId}`);
  return normalizeProduct(unwrapApiData<Product>(response.data));
};

export const getProductVariants = async (productId: number): Promise<Product[]> => {
  const response = await api.get<ApiResponse<Product[]>>(`/products/${productId}/variants`);
  return unwrapApiArray<Product>(response.data).map(normalizeProduct);
};

export const createProduct = async (productData: any): Promise<Product> => {
  const response = await api.post<ApiResponse<Product>>('/products', productData);
  return unwrapApiData<Product>(response.data);
};

export const updateProduct = async (productId: number, productData: any): Promise<Product> => {
  if (productData?.BrandId && !productData.Brand) {
    try {
      const brands = await getBrands();
      const brand = brands.find(b => b.id === Number(productData.BrandId));
      if (brand?.name) productData.Brand = brand.name;
    } catch {
      // Brand name is backward-compatible metadata; let the update continue.
    }
  }

  const response = await api.put<ApiResponse<Product>>(`/products/${productId}`, productData);
  return unwrapApiData<Product>(response.data);
};

export const deleteProduct = async (productId: number): Promise<void> => {
  await api.delete(`/products/${productId}`);
};

export const uploadProductImages = async (productId: number, files: FileList): Promise<ImageUploadResult[]> => {
  const formData = new FormData();
  for (let i = 0; i < files.length; i++) {
    formData.append('files', files[i]);
  }

  const response = await api.post(`/products/${productId}/images`, formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });

  const data = unwrapApiData<any>(response.data);
  const uploaded = Array.isArray(data)
    ? data
    : (data?.uploadedImages ?? data?.items ?? []);

  return uploaded.map((u: any, idx: number) => ({
    productId: data?.productId ?? productId,
    imageUrl: u.imageUrl ?? u.url,
    imageId: u.imageId ?? u.id,
    displayOrder: u.displayOrder ?? idx + 1,
    message: u.message ?? ''
  }));
};

export const deleteProductImage = async (productId: number, imageId: string): Promise<void> => {
  await api.delete(`/products/${productId}/images/${imageId}`);
};

export const createVariant = async (productId: number, variantData: any): Promise<any> => {
  const response = await api.post(`/products/${productId}/variants`, variantData);
  return unwrapApiData(response.data);
};

export const updateVariant = async (productId: number, variantId: number, variantData: any): Promise<any> => {
  const response = await api.put(`/products/${productId}/variants/${variantId}`, variantData);
  return unwrapApiData(response.data);
};

export const deleteVariant = async (productId: number, variantId: number): Promise<any> => {
  const response = await api.delete(`/products/${productId}/variants/${variantId}`);
  return unwrapApiData(response.data);
};

export const uploadVariantImages = async (productId: number, variantId: number, files: FileList): Promise<ImageUploadResult[]> => {
  const formData = new FormData();
  for (let i = 0; i < files.length; i++) {
    formData.append('files', files[i]);
  }

  const response = await api.post(
    `/products/${productId}/variants/${variantId}/images`,
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } }
  );

  const data = unwrapApiData<any>(response.data);
  const uploaded = Array.isArray(data?.uploadedImages)
    ? data.uploadedImages
    : (data?.uploadedImages ?? data?.items ?? []);

  return uploaded.map((u: any, idx: number) => ({
    productId: data?.variantId ?? variantId,
    imageUrl: u.imageUrl ?? u.url,
    imageId: u.imageId ?? u.id,
    displayOrder: u.displayOrder ?? idx + 1,
    message: u.message ?? ''
  }));
};

export const deleteVariantImage = async (productId: number, variantId: number, imageId: string): Promise<void> => {
  await api.delete(`/products/${productId}/variants/${variantId}/images/${imageId}`);
};

export interface UpdateInventoryRequest {
  quantityInStock?: number;
  reservedQuantity?: number;
  reorderLevel?: number;
  maxStockLevel?: number;
  warehouseLocation?: string;
}

export interface CreateInventoryRequest extends UpdateInventoryRequest {
  productId: number;
}

export const createInventory = async (payload: CreateInventoryRequest) => {
  const response = await api.post('/inventory', payload);
  return unwrapApiData(response.data);
};

export const updateInventoryById = async (inventoryId: number, payload: UpdateInventoryRequest) => {
  const response = await api.put(`/inventory/${inventoryId}`, payload);
  return unwrapApiData(response.data);
};

export const adjustStock = async (productId: number, quantityDelta: number) => {
  const response = await api.post('/inventory/adjust-stock', {
    productId,
    quantity: quantityDelta,
    reference: 'ADMIN_PRODUCT_EDIT',
    notes: 'Admin adjusted stock from product edit page'
  });
  return unwrapApiData(response.data);
};
