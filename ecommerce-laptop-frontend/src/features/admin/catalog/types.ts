export interface Category {
  id: number;
  name: string;
  slug: string;
  description?: string;
  parentId?: number;
  parentName?: string;
  productCount: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface Brand {
  id: number;
  name: string;
  slug: string;
  description?: string;
  logoUrl?: string;
  website?: string;
  country?: string;
  productCount: number;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CategoryFormData {
  name: string;
  description?: string;
  parentId?: string;
  isActive: boolean;
}

export interface BrandFormData {
  name: string;
  slug: string;
  description?: string;
  logoUrl?: string;
  website?: string;
  country?: string;
  isActive: boolean;
}
