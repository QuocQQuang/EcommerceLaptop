'use client';

import { ProductImage } from '@/components/admin/ImageUpload';
import EnhancedProductForm from '@/components/admin/ProductForm/EnhancedProductForm';
import { Button } from '@/components/ui/button';
import {
  useAdminAuth
} from '@/contexts/AdminAuthContext';
import { PERMISSIONS, createProduct, getBrands, getCategories, mapFormToCreatePayload, ProductFormData, uploadProductImages, type Brand } from '@/lib/admin-api';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { toast } from 'sonner';


// Using Category and Brand interfaces from admin-api.ts

// Real API calls
const useCategoriesQuery = () => {
  return useQuery({
    queryKey: ['admin', 'categories'],
    queryFn: getCategories
  });
};

const useBrandsQuery = () => {
  return useQuery({
    queryKey: ['admin', 'brands'],
    queryFn: getBrands
  });
};

const useCreateProductMutation = (productImages: ProductImage[], brands: Brand[], canManageImages: boolean) => {
  return useMutation({
    mutationFn: async (productData: ProductFormData) => {
      const backendData = mapFormToCreatePayload(productData, brands);
      const newProduct = await createProduct(backendData);

      // Upload images if any
      if (canManageImages && productImages.length > 0 && newProduct.id) {
        const imageFiles = await Promise.all(
          productImages.map(async (img) => {
            const response = await fetch(img.imageUrl);
            const blob = await response.blob();
            return new File([blob], img.altText || 'product-image.jpg', { type: blob.type });
          })
        );

        const dt = new DataTransfer();
        imageFiles.forEach(file => dt.items.add(file));

        await uploadProductImages(newProduct.id, dt.files);
      }

      return newProduct;
    },
    onSuccess: (newProduct) => {
      toast.success('Sản phẩm đã được tạo thành công!');
      return newProduct;
    },
    onError: (error: any) => {
      toast.error(`Lỗi tạo sản phẩm: ${error.message || 'Có lỗi xảy ra'}`);
    }
  });
};

export default function AddProductPage() {
  const router = useRouter();
  const { hasPermission } = useAdminAuth();
  const queryClient = useQueryClient();

  const { data: categories } = useCategoriesQuery();
  const { data: brands } = useBrandsQuery();

  const [productImages, setProductImages] = useState<ProductImage[]>([]);
  const canManageImages = hasPermission(PERMISSIONS.PRODUCTS_MANAGE);
  const createProductMutation = useCreateProductMutation(productImages, brands || [], canManageImages);

  // Callback functions for refreshing data
  const handleCategoryCreated = (category: any) => {
    queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
  };

  const handleBrandCreated = (brand: any) => {
    queryClient.invalidateQueries({ queryKey: ['admin', 'brands'] });
  };

  const [formData, setFormData] = useState<ProductFormData>({
    // === BASIC INFORMATION ===
    name: '',
    sku: '',
    description: '',
    shortDescription: '',
    categoryId: '',
    brandId: '',
    price: '',
    comparePrice: '',
    costPrice: '',
    stock: '',

    weight: '',
    dimensions: '',
    status: 'active',
    isFeatured: false,
    tags: [],
    specifications: {},
    images: [],
    productType: 'Laptop',

    // === INVENTORY MANAGEMENT ===
    inventory: {
      quantityInStock: '',
      reservedQuantity: '0',
      reorderLevel: '5',
      maxStockLevel: '100',
      warehouseLocation: ''
    },

    // === LAPTOP SPECIFICATIONS ===
    series: '',
    model: '',
    cpuBrand: '',
    cpuModel: '',
    cpuGeneration: '',
    cpuCores: '',
    cpuBaseClockGHz: '',
    cpuBoostClockGHz: '',
    cpuCache: '',
    ramType: '',
    ramCapacityGB: '',
    ramSlots: '',
    ramSpeed: '',
    ramUpgradeable: false,
    storageType: '',
    storageCapacityGB: '',
    storageInterface: '',
    nvMeSupport: false,
    gpuType: '',
    gpuBrand: '',
    gpuModel: '',
    gpuVramGB: '',
    displaySizeInches: '',
    displayResolution: '',
    displayPanelType: '',
    displayRefreshRateHz: '',
    displayTouchscreen: false,
    batteryCapacityWh: '',
    weightKg: '',
    color: '',
    ports: '',
    wiFi6Support: false,
    bluetoothSupport: false,
    bluetoothVersion: '',
    warrantyPeriod: '',
    targetAudience: '',

    // === ACCESSORY SPECIFICATIONS ===
    accessoryType: '',
    compatibility: '',
    specificationDetails: '',
    connectivity: '',
    material: '',

    // === BUNDLE SPECIFICATIONS ===
    bundleType: '',
    discountPercentage: '',
    validFrom: '',
    validTo: '',
    bundleItems: [],

    // === LEGACY FIELDS ===
    cpu: '',
    ram: '',
    storage: '',
    gpu: '',
    display: '',
    battery: '',
    weight_kg: '',
    operatingSystem: '',
    warranty: ''
  });

  const handleSubmit = async (data: ProductFormData) => {
    try {
      await createProductMutation.mutateAsync(data);
      router.push('/admin/products');
    } catch (error) {
      console.error('Error creating product:', error);
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <Link href="/admin/products">
          <Button variant="outline" size="sm">
            <ArrowLeft className="h-4 w-4 mr-2" />
            Quay lại
          </Button>
        </Link>
        <div>
          <h1 className="text-3xl font-bold">Thêm sản phẩm mới</h1>
          <p className="text-muted-foreground">
            Tạo sản phẩm mới trong hệ thống
          </p>
        </div>
      </div>

      <EnhancedProductForm
        formData={formData}
        onFormDataChange={setFormData}
        onSubmit={handleSubmit}
        isLoading={createProductMutation.isPending}
        categories={categories || []}
        brands={brands || []}
        products={[]} // Empty for create form
        onCategoryCreated={handleCategoryCreated}
        onBrandCreated={handleBrandCreated}
        images={productImages}
        onImagesChange={setProductImages}
        imageManagementDisabled={!canManageImages}
      />
    </div>
  );
}
