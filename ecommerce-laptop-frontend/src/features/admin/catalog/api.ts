import { api, unwrapApiArray, unwrapApiData } from '@/lib/admin/http';
import type { Brand, BrandFormData, Category, CategoryFormData } from './types';

export const getCategories = async (): Promise<Category[]> => {
  const response = await api.get<Category[]>('/admin/categories');
  return unwrapApiArray<Category>(response.data);
};

export const getBrands = async (): Promise<Brand[]> => {
  const response = await api.get<Brand[]>('/admin/brands');
  return unwrapApiArray<Brand>(response.data);
};

export const getCategoriesWithCounts = async (): Promise<Category[]> => {
  const response = await api.get<Category[]>('/admin/categories/with-counts');
  return unwrapApiArray<Category>(response.data);
};

export const getBrandsWithCounts = async (): Promise<Brand[]> => {
  const response = await api.get<Brand[]>('/admin/brands/with-counts');
  return unwrapApiArray<Brand>(response.data);
};

export const createCategory = async (categoryData: CategoryFormData): Promise<Category> => {
  const response = await api.post<Category>('/admin/categories', {
    name: categoryData.name,
    description: categoryData.description,
    parentId: categoryData.parentId ? parseInt(categoryData.parentId) : null,
    isActive: categoryData.isActive
  });
  return unwrapApiData<Category>(response.data);
};

export const updateCategory = async (id: number, categoryData: CategoryFormData): Promise<Category> => {
  const response = await api.put<Category>(`/admin/categories/${id}`, {
    name: categoryData.name,
    description: categoryData.description,
    parentId: categoryData.parentId ? parseInt(categoryData.parentId) : null,
    isActive: categoryData.isActive
  });
  return unwrapApiData<Category>(response.data);
};

export const deleteCategory = async (id: number): Promise<void> => {
  await api.delete(`/admin/categories/${id}`);
};

export const reassignAndDeleteCategory = async (categoryIdToDelete: number, newCategoryId: number): Promise<void> => {
  await api.post(`/admin/categories/${categoryIdToDelete}/reassign-and-delete`, { newCategoryId });
};

export const forceDeleteCategory = async (id: number): Promise<void> => {
  await api.delete(`/admin/categories/${id}/force`);
};

export const createBrand = async (brandData: BrandFormData): Promise<Brand> => {
  const response = await api.post<Brand>('/admin/brands', {
    name: brandData.name,
    slug: brandData.slug,
    description: brandData.description,
    logoUrl: brandData.logoUrl,
    websiteUrl: brandData.website,
    isActive: brandData.isActive
  });
  return unwrapApiData<Brand>(response.data);
};

export const updateBrand = async (id: number, brandData: BrandFormData): Promise<Brand> => {
  const response = await api.put<Brand>(`/admin/brands/${id}`, {
    name: brandData.name,
    slug: brandData.slug,
    description: brandData.description,
    logoUrl: brandData.logoUrl,
    websiteUrl: brandData.website,
    isActive: brandData.isActive
  });
  return unwrapApiData<Brand>(response.data);
};

export const deleteBrand = async (id: number): Promise<void> => {
  await api.delete(`/admin/brands/${id}`);
};

export const reassignAndDeleteBrand = async (brandIdToDelete: number, newBrandId: number): Promise<void> => {
  await api.post(`/admin/brands/${brandIdToDelete}/reassign-and-delete`, { newBrandId });
};

export const forceDeleteBrand = async (id: number): Promise<void> => {
  await api.delete(`/admin/brands/${id}/force`);
};
