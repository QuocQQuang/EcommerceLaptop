'use client';

import { ProductImage } from '@/components/admin/ImageUpload';
import EnhancedProductForm from '@/components/admin/ProductForm/EnhancedProductForm';
import { Button } from '@/components/ui/button';
import {
  useAdminAuth
} from '@/contexts/AdminAuthContext';
import { createProduct, getBrands, getCategories, ProductFormData, uploadProductImages } from '@/lib/admin-api';
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

const useCreateProductMutation = (productImages: ProductImage[]) => {
  return useMutation({
    mutationFn: async (productData: ProductFormData) => {
      // Map to backend API format - send as JsonElement
      const backendData: any = {
        ProductType: productData.productType,
        Name: productData.name,
        SKU: productData.sku,
        Description: productData.description,
        Price: parseFloat(productData.price),
        Stock: parseInt(productData.stock) || 0,
        Brand: productData.brandId, // Will need to get actual brand name
        Model: productData.shortDescription || '',
        IsActive: productData.status === 'active'
      };

      // Add specific fields based on product type
      if (productData.productType === 'Laptop') {
        backendData.CPU = productData.cpu || '';
        backendData.RAM = productData.ram || '';
        backendData.Storage = productData.storage || '';
        backendData.GPU = productData.gpu || '';
        backendData.Display = productData.display || '';
        backendData.Battery = productData.battery || '';
        backendData.Weight_Kg = productData.weight_kg || '';
        backendData.OperatingSystem = productData.operatingSystem || '';
        backendData.Ports = productData.ports || '';
      } else if (productData.productType === 'Accessory') {
        backendData.Type = productData.accessoryType || '';
        backendData.Compatibility = productData.compatibility || '';
        backendData.Color = productData.color || '';
        backendData.Material = productData.material || '';
        backendData.Warranty = productData.warranty || '';
      }

      const newProduct = await createProduct(backendData);

      // Upload images if any
      if (productImages.length > 0 && newProduct.id) {
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
      toast.success('Sn phm  c to thnh cng!');
      return newProduct;
    },
    onError: (error: any) => {
      toast.error(`Li to sn phm: ${error.message || 'C li xy ra'}`);
    }
  });
};

export default function AddProductPage() {
  const router = useRouter();
  const { user } = useAdminAuth();
  const queryClient = useQueryClient();

  const { data: categories } = useCategoriesQuery();
  const { data: brands } = useBrandsQuery();

  const [productImages, setProductImages] = useState<ProductImage[]>([]);
  const createProductMutation = useCreateProductMutation(productImages);

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
    lowStockThreshold: '5',
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
            Quay li
          </Button>
        </Link>
        <div>
          <h1 className="text-3xl font-bold">Thm sn phm mi</h1>
          <p className="text-muted-foreground">
            To sn phm mi trong h thng
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
      />
    </div>
  );
}