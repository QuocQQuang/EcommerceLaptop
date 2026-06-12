'use client';

import { ProductImage } from '@/components/admin/ImageUpload';
import EnhancedProductForm from '@/components/admin/ProductForm/EnhancedProductForm';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardHeader
} from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import {
  PermissionGuard,
  useAdminAuth
} from '@/contexts/AdminAuthContext';
import {
  PERMISSIONS,
  ProductFormData,
  deleteProductImage,
  getAdminProduct,
  getBrands,
  getCategories,
  mapFormToUpdatePayload,
  mapProductToForm,
  updateProduct,
  updateVariant,
  uploadProductImages
} from '@/lib/admin-api';
import apiClient from '@/lib/api';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  AlertCircle,
  ArrowLeft
} from 'lucide-react';
import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { useEffect, useMemo, useState } from 'react';
import { toast } from 'sonner';

// Using interfaces from admin-api.ts

// API calls
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

const useProductQuery = (productId: string) => {
  return useQuery({
    queryKey: ['admin', 'product', productId],
    queryFn: () => getAdminProduct(parseInt(productId)),
    enabled: !!productId
  });
};

const useUpdateProductMutation = (productId: string) => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (productData: ProductFormData) => {
      const backendData = mapFormToUpdatePayload(productData);
      return await updateProduct(parseInt(productId), backendData);
    },
    onSuccess: () => {
      toast.success('Sản phẩm đã được cập nhật thành công!');
      // Invalidate related queries
      queryClient.invalidateQueries({ queryKey: ['admin', 'products'] });
      queryClient.invalidateQueries({ queryKey: ['admin', 'product', productId] });
    },
    onError: (error: any) => {
      toast.error(`Lỗi cập nhật sản phẩm: ${error.message || 'Có lỗi xảy ra'}`);
    }
  });
};

export default function EditProductPage() {
  const router = useRouter();
  const params = useParams();
  const productId = params.id as string;
  const { user } = useAdminAuth();
  const queryClient = useQueryClient();

  const { data: categories, isLoading: categoriesLoading, error: categoriesError } = useCategoriesQuery();
  const { data: brands, isLoading: brandsLoading, error: brandsError } = useBrandsQuery();
  const productQuery = useProductQuery(productId);
  const { data: product, isLoading: productLoading, error } = productQuery;

  console.log(' Data loading status:', {
    productLoading,
    categoriesLoading,
    brandsLoading,
    hasProduct: !!product,
    hasCategories: !!categories,
    hasBrands: !!brands,
    productId,
    error: error?.message,
    categoriesError: categoriesError?.message,
    brandsError: brandsError?.message
  });
  const updateProductMutation = useUpdateProductMutation(productId);

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
    status: 'active' as const,
    isFeatured: false,
    tags: [],
    specifications: {},
    images: [],
    productType: 'Laptop' as const,

    // === INVENTORY MANAGEMENT ===
    inventory: {
      quantityInStock: '',
      reservedQuantity: '0',
      reorderLevel: '5',
      maxStockLevel: '100',
      warehouseLocation: ''
    },

    // === LAPTOP SPECIFICATIONS - COMPREHENSIVE ===
    // Basic Info
    series: '',
    model: '',

    // CPU Specifications - DETAILED
    cpuBrand: '',
    cpuModel: '',
    cpuGeneration: '',
    cpuCores: '',
    cpuBaseClockGHz: '',
    cpuBoostClockGHz: '',
    cpuCache: '',

    // RAM Specifications - DETAILED
    ramType: '',
    ramCapacityGB: '',
    ramSlots: '',
    ramSpeed: '',
    ramUpgradeable: false,

    // Storage Specifications - DETAILED
    storageType: '',
    storageCapacityGB: '',
    storageInterface: '',
    nvMeSupport: false,

    // GPU Specifications - DETAILED
    gpuType: '',
    gpuBrand: '',
    gpuModel: '',
    gpuVramGB: '',

    // Display Specifications - DETAILED
    displaySizeInches: '',
    displayResolution: '',
    displayPanelType: '',
    displayRefreshRateHz: '',
    displayTouchscreen: false,

    // Physical Specifications - DETAILED
    batteryCapacityWh: '',
    weightKg: '',
    color: '',
    ports: '',

    // Connectivity - DETAILED
    wiFi6Support: false,
    bluetoothSupport: false,
    bluetoothVersion: '',

    // Business Information - DETAILED
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

    // === LEGACY FIELDS (for backward compatibility) ===
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
  const [productImages, setProductImages] = useState<ProductImage[]>([]);

  //  FIXED: Directly update form data when product data is loaded
  useEffect(() => {
    console.log(' useEffect triggered:', {
      hasProduct: !!product,
      hasBrands: !!brands,
      hasCategories: !!categories,
      productId: product?.id,
      brandsCount: brands?.length,
      categoriesCount: categories?.length
    });

    if (product && brands && categories) {
      console.log(' Mapping product to form data:', {
        product: product,
        brands: brands,
        categories: categories,
        productType: product.productType || product.type
      });

      const mappedData = mapProductToForm(product, brands);
      console.log(' Mapped form data:', mappedData);
      setFormData(mappedData);

      if (product.images) {
        const images: ProductImage[] = product.images.map((img: any, index: number) => ({
          imageId: img.imageId || img.id, // Use ImageId if available, fallback to Id
          imageUrl: img.imageUrl,
          altText: img.altText || `Product image ${index + 1}`,
          displayOrder: img.displayOrder || img.sortOrder || index + 1
        }));
        setProductImages(images);
      }
    } else {
      console.log(' Missing data:', {
        product: !!product,
        brands: !!brands,
        categories: !!categories,
        productData: product,
        brandsData: brands,
        categoriesData: categories
      });
    }
  }, [product, brands, categories]);

  const [currentTag, setCurrentTag] = useState('');
  const [currentSpecKey, setCurrentSpecKey] = useState('');
  const [currentSpecValue, setCurrentSpecValue] = useState('');
  const [variants, setVariants] = useState<any[]>([]);
  const [activeTab, setActiveTab] = useState('basic');

  // Load variants when product is loaded
  useEffect(() => {
    if (product && product.isBaseProduct) {
      // Load variants for base products
      apiClient.get(`/products/${productId}/variants`)
        .then(response => {
          if (response.data.success) {
            setVariants(response.data.data || []);
          }
        })
        .catch(err => console.error('Failed to load variants:', err));
    }
  }, [product, productId]);

  // Debug form data after state updates
  useEffect(() => {
    console.log(' Current form data state:', {
      name: formData.name,
      price: formData.price,
      stock: formData.stock,
      cpu: formData.cpu,
      ram: formData.ram,
      display: formData.display,
      categoryId: formData.categoryId,
      brandId: formData.brandId
    });
  }, [formData]);

  const handleInputChange = (field: keyof ProductFormData, value: any) => {
    setFormData(prev => ({
      ...prev,
      [field]: value
    }));
  };

  const addTag = () => {
    if (currentTag && !(formData.tags || []).includes(currentTag)) {
      setFormData(prev => ({
        ...prev,
        tags: [...(prev.tags || []), currentTag]
      }));
      setCurrentTag('');
    }
  };

  const removeTag = (tagToRemove: string) => {
    setFormData(prev => ({
      ...prev,
      tags: (prev.tags || []).filter((tag: string) => tag !== tagToRemove)
    }));
  };

  const addSpecification = () => {
    if (currentSpecKey && currentSpecValue) {
      setFormData(prev => ({
        ...prev,
        specifications: {
          ...prev.specifications,
          [currentSpecKey]: currentSpecValue
        }
      }));
      setCurrentSpecKey('');
      setCurrentSpecValue('');
    }
  };

  const removeSpecification = (keyToRemove: string) => {
    setFormData(prev => ({
      ...prev,
      specifications: Object.fromEntries(
        Object.entries(prev.specifications || {}).filter(([key]) => key !== keyToRemove)
      ) as any
    }));
  };

  const handleImageUpload = async (files: FileList) => {
    try {
      await uploadProductImages(parseInt(productId), files);
      toast.success('Upload ảnh thành công!');
      // Refresh product data to get updated images
      await productQuery.refetch();
    } catch (error) {
      console.error('Error uploading images:', error);
      toast.error('Upload ảnh thất bại');
      throw error;
    }
  };

  const handleImageDelete = async (imageId: string) => {
    try {
      await deleteProductImage(parseInt(productId), imageId);
      toast.success('Xóa ảnh thành công!');

      // Update local state
      setProductImages(prev => prev.filter(img => img.imageId !== imageId));
    } catch (error) {
      console.error('Error deleting image:', error);
      toast.error('Xóa ảnh thất bại');
      throw error;
    }
  };

  const handleSubmit = async (data: ProductFormData) => {
    console.log(' handleSubmit called with data:', data);

    if (!isFormValid) {
      toast.error('Vui lòng điền đầy đủ thông tin bắt buộc và kiểm tra định dạng số');
      return;
    }

    try {
      // If this product is a variant, use variant update API to update inventory correctly
      if (product?.isVariant && product?.parentProductId) {
        const stockQty = data.inventory?.quantityInStock
          ? parseInt(data.inventory.quantityInStock)
          : (data.stock ? parseInt(data.stock) : undefined);

        const variantPayload: any = {};
        if (!isNaN(Number(stockQty))) {
          variantPayload.StockQuantity = stockQty;
        }
        // Optional: keep other simple fields in sync
        if (data.price) variantPayload.Price = parseFloat(data.price);
        if (data.status) variantPayload.IsActive = data.status === 'active';
        if (data.description) variantPayload.Description = data.description;

        await updateVariant(parseInt(product.parentProductId as any), parseInt(product.id as any), variantPayload);
        toast.success('Cập nhật biến thể thành công!');
        await productQuery.refetch();
      } else {
        // Base product update handled entirely by backend (also updates inventory if StockQuantity provided)
        await updateProductMutation.mutateAsync(data);
      }
      router.push('/admin/products');
    } catch (error) {
      console.error('Error updating product:', error);
    }
  };

  const isFormValid = useMemo(() => {
    return formData.name &&
      formData.sku &&
      formData.productType &&
      formData.brandId &&
      formData.price &&
      !isNaN(parseFloat(formData.price)) &&
      parseFloat(formData.price) >= 0 &&
      !isNaN(parseInt(formData.stock)) &&
      parseInt(formData.stock) >= 0;
  }, [formData.name, formData.sku, formData.productType, formData.brandId, formData.price, formData.stock]);

  if (productLoading || categoriesLoading || brandsLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Button variant="outline" size="sm" disabled>
            <ArrowLeft className="h-4 w-4 mr-2" />
            Quay lại
          </Button>
          <div>
            <h1 className="text-3xl font-bold">đang tải dữ liệu...</h1>
            <div className="text-sm text-muted-foreground">
              Product ID: {productId} |
              Product Loading: {productLoading ? 'YES' : 'NO'} |
              Categories Loading: {categoriesLoading ? 'YES' : 'NO'} |
              Brands Loading: {brandsLoading ? 'YES' : 'NO'}
            </div>
          </div>
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-6">
            <Card>
              <CardHeader>
                <Skeleton className="h-6 w-32" />
                <Skeleton className="h-4 w-48" />
              </CardHeader>
              <CardContent className="space-y-4">
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-10 w-full" />
                <Skeleton className="h-20 w-full" />
              </CardContent>
            </Card>
          </div>
          <div className="lg:col-span-1">
            <Card>
              <CardHeader>
                <Skeleton className="h-6 w-24" />
              </CardHeader>
              <CardContent>
                <Skeleton className="h-10 w-full" />
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    );
  }

  if (error || categoriesError || brandsError) {
    const isNotFound = (error as any)?.status === 404;

    if (isNotFound) {
      toast.error('Sản phẩm không tồn tại');
      router.push('/admin/products');
      return null;
    }

    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Link href="/admin/products">
            <Button variant="outline" size="sm">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Quay lại
            </Button>
          </Link>
          <div>
            <h1 className="text-3xl font-bold">Lỗi</h1>
          </div>
        </div>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2 text-red-600">
              <AlertCircle className="h-5 w-5" />
              <span>
                Không thể tải dữ liệu:
                {error && `Product: ${(error as any)?.message || 'Có lỗi xảy ra'}`}
                {categoriesError && ` | Categories: ${(categoriesError as any)?.message || 'Có lỗi xảy ra'}`}
                {brandsError && ` | Brands: ${(brandsError as any)?.message || 'Có lỗi xảy ra'}`}
              </span>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
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
            <h1 className="text-3xl font-bold">Chỉnh sửa sản phẩm</h1>
            <p className="text-muted-foreground">
              Cập nhật thông tin sản phẩm #{productId}
            </p>
          </div>
        </div>

        <EnhancedProductForm
          formData={formData}
          onFormDataChange={setFormData}
          onSubmit={handleSubmit}
          isLoading={updateProductMutation.isPending}
          categories={categories || []}
          brands={brands || []}
          onCategoryCreated={handleCategoryCreated}
          onBrandCreated={handleBrandCreated}
          productId={parseInt(productId)}
          images={productImages}
          onImagesChange={setProductImages}
          onImageUpload={handleImageUpload}
          onImageDelete={handleImageDelete}
        />
      </div>
    </PermissionGuard>
  );
}