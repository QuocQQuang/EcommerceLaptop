'use client';

import ImageUpload, { ProductImage } from '@/components/admin/ImageUpload';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
    Card,
    CardContent,
    CardHeader,
    CardTitle
} from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import {
    Tabs,
    TabsContent,
    TabsList,
    TabsTrigger
} from '@/components/ui/tabs';
import {
    PermissionGuard,
    useAdminAuth
} from '@/contexts/AdminAuthContext';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { createVariant, deleteVariant, deleteVariantImage, getAdminProduct, getProductVariants, PERMISSIONS, updateVariant, uploadVariantImages } from '@/lib/admin-api';
import { formatCurrencyPrice } from '@/lib/currency';
import { Product } from '@/types/api';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
    ArrowLeft,
    Edit,
    Package,
    Plus,
    Search,
    Trash2
} from 'lucide-react';
import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

// Variant management API calls
const useProductQuery = (productId: string) => {
    return useQuery({
        queryKey: ['admin', 'product', productId],
        queryFn: () => getAdminProduct(parseInt(productId)),
        enabled: !!productId
    });
};

const useVariantsQuery = (productId: string) => {
    return useQuery({
        queryKey: ['admin', 'variants', productId],
        queryFn: () => getProductVariants(parseInt(productId)),
        enabled: !!productId
    });
};

const useCreateVariantMutation = (productId: string) => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async (variantData: any) => {
            return await createVariant(parseInt(productId), variantData);
        },
        onSuccess: () => {
            toast.success('Tạo biến thể thành công!');
            queryClient.invalidateQueries({ queryKey: ['admin', 'variants', productId] });
        },
        onError: (error: any) => {
            toast.error(`Lỗi khi tạo biến thể: ${error.message}`);
        }
    });
};

const useDeleteVariantMutation = (productId: string) => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async (variantId: number) => {
            return await deleteVariant(parseInt(productId), variantId);
        },
        onSuccess: () => {
            toast.success('Xóa biến thể thành công!');
            queryClient.invalidateQueries({ queryKey: ['admin', 'variants', productId] });
        },
        onError: (error: any) => {
            toast.error(`Lỗi khi xóa biến thể: ${error.message}`);
        }
    });
};

const useUpdateVariantMutation = (productId: string) => {
    const queryClient = useQueryClient();
    return useMutation({
        mutationFn: async ({ variantId, variantData }: { variantId: number; variantData: any }) => {
            return await updateVariant(parseInt(productId), variantId, variantData);
        },
        onSuccess: () => {
            toast.success('Cập nhật biến thể thành công!');
            queryClient.invalidateQueries({ queryKey: ['admin', 'variants', productId] });
        },
        onError: (error: any) => {
            toast.error(`Lỗi khi cập nhật biến thể: ${error.message}`);
        }
    });
};

// Helper functions - moved inside component

const getStockBadge = (stock: number) => {
    if (stock === 0) {
        return <Badge variant="destructive">0</Badge>;
    } else if (stock < 5) {
        return <Badge variant="outline" className="text-orange-600">{stock}</Badge>;
    } else if (stock < 10) {
        return <Badge variant="outline" className="text-yellow-600">{stock}</Badge>;
    } else {
        return <Badge variant="outline" className="text-green-600">{stock}</Badge>;
    }
};

const getStatusBadge = (isActive: boolean) => {
    return isActive ? (
        <Badge variant="default">Hoạt động</Badge>
    ) : (
        <Badge variant="secondary">Không hoạt động</Badge>
    );
};

export default function VariantManagementPage() {
    const { selectedCurrency } = useCurrencyContext();
    const router = useRouter();
    const params = useParams();
    const productId = params.id as string;
    const { hasPermission } = useAdminAuth();
    const canManageImages = hasPermission(PERMISSIONS.PRODUCTS_MANAGE);

    const [searchTerm, setSearchTerm] = useState('');
    const [showCreateDialog, setShowCreateDialog] = useState(false);
    const [showEditDialog, setShowEditDialog] = useState(false);
    const [selectedVariant, setSelectedVariant] = useState<Product | null>(null);

    // Helper functions
    const formatCurrency = (amount: number) => {
        return formatCurrencyPrice(amount, selectedCurrency);
    };
    const [createImages, setCreateImages] = useState<File[]>([]);
    const [editImages, setEditImages] = useState<File[]>([]);
    const [createImageUrls, setCreateImageUrls] = useState<string[]>([]);
    const [editImageUrls, setEditImageUrls] = useState<string[]>([]);
    const [variantImages, setVariantImages] = useState<{ [variantId: number]: ProductImage[] }>({});
    const [createFormData, setCreateFormData] = useState({
        // Basic fields
        variantName: '',
        variantSku: '',
        price: '',
        stock: '',
        description: '',

        // Laptop-specific fields - MỜ RỘNG TẤT CẢ THÔNG SỐ
        series: '',

        // CPU Specifications
        cpuBrand: '',
        cpuModel: '',
        cpuGeneration: '',
        cpuCores: '',
        cpuBaseClockGHz: '',
        cpuBoostClockGHz: '',
        cpuCache: '',

        // RAM Specifications
        ramType: '',
        ramCapacityGB: '',
        ramSlots: '',
        ramSpeed: '',
        ramUpgradeable: false,

        // Storage Specifications
        storageType: '',
        storageCapacityGB: '',
        storageInterface: '',
        nvMeSupport: false,

        // GPU Specifications
        gpuType: '',
        gpuBrand: '',
        gpuModel: '',
        gpuVramGB: '',

        // Display Specifications
        displaySizeInches: '',
        displayResolution: '',
        displayPanelType: '',
        displayRefreshRateHz: '',
        displayTouchscreen: false,

        // Physical Specifications
        batteryCapacityWh: '',
        weightKg: '',
        dimensions: '',
        color: '',
        ports: '',

        // Connectivity
        wiFi6Support: false,
        bluetoothSupport: false,
        bluetoothVersion: '',

        // Business info
        warrantyPeriod: '',
        targetAudience: ''
    });

    const [editFormData, setEditFormData] = useState({
        // Basic fields
        variantName: '',
        variantSku: '',
        price: '',
        stock: '',
        description: '',

        // Laptop-specific fields - MỜ RỘNG TẤT CẢ THÔNG SỐ
        series: '',

        // CPU Specifications
        cpuBrand: '',
        cpuModel: '',
        cpuGeneration: '',
        cpuCores: '',
        cpuBaseClockGHz: '',
        cpuBoostClockGHz: '',
        cpuCache: '',

        // RAM Specifications
        ramType: '',
        ramCapacityGB: '',
        ramSlots: '',
        ramSpeed: '',
        ramUpgradeable: false,

        // Storage Specifications
        storageType: '',
        storageCapacityGB: '',
        storageInterface: '',
        nvMeSupport: false,

        // GPU Specifications
        gpuType: '',
        gpuBrand: '',
        gpuModel: '',
        gpuVramGB: '',

        // Display Specifications
        displaySizeInches: '',
        displayResolution: '',
        displayPanelType: '',
        displayRefreshRateHz: '',
        displayTouchscreen: false,

        // Physical Specifications
        batteryCapacityWh: '',
        weightKg: '',
        dimensions: '',
        color: '',
        ports: '',

        // Connectivity
        wiFi6Support: false,
        bluetoothSupport: false,
        bluetoothVersion: '',

        // Business info
        warrantyPeriod: '',
        targetAudience: ''
    });

    const { data: product, isLoading: productLoading } = useProductQuery(productId);
    const { data: variants, isLoading: variantsLoading } = useVariantsQuery(productId);
    const createVariantMutation = useCreateVariantMutation(productId);
    const deleteVariantMutation = useDeleteVariantMutation(productId);
    const updateVariantMutation = useUpdateVariantMutation(productId);

    // Fill form with base product data when opening create dialog
    useEffect(() => {
        if (product && showCreateDialog) {
            setCreateFormData({
                // Basic fields
                variantName: '',
                variantSku: '',
                price: product.price?.toString() || '',
                stock: '0',
                description: product.description || '',

                // Laptop-specific fields - IN SN T BASE PRODUCT (Map both camelCase and PascalCase)
                series: (product as any).series || (product as any).Series || '',

                // CPU Specifications
                cpuBrand: (product as any).cpuBrand || (product as any).CpuBrand || '',
                cpuModel: (product as any).cpuModel || (product as any).CpuModel || '',
                cpuGeneration: (product as any).cpuGeneration || (product as any).CpuGeneration || '',
                cpuCores: ((product as any).cpuCores || (product as any).CpuCores)?.toString() || '',
                cpuBaseClockGHz: ((product as any).cpuBaseClockGHz || (product as any).CpuBaseClockGHz)?.toString() || '',
                cpuBoostClockGHz: ((product as any).cpuBoostClockGHz || (product as any).CpuBoostClockGHz)?.toString() || '',
                cpuCache: (product as any).cpuCache || (product as any).CpuCache || '',

                // RAM Specifications
                ramType: (product as any).ramType || (product as any).RamType || '',
                ramCapacityGB: ((product as any).ramCapacityGB || (product as any).RamCapacityGB)?.toString() || '',
                ramSlots: ((product as any).ramSlots || (product as any).RamSlots)?.toString() || '',
                ramSpeed: ((product as any).ramSpeed || (product as any).RamSpeed)?.toString() || '',
                ramUpgradeable: (product as any).ramUpgradeable || (product as any).RamUpgradeable || false,

                // Storage Specifications
                storageType: (product as any).storageType || (product as any).StorageType || '',
                storageCapacityGB: ((product as any).storageCapacityGB || (product as any).StorageCapacityGB)?.toString() || '',
                storageInterface: (product as any).storageInterface || (product as any).StorageInterface || '',
                nvMeSupport: (product as any).nvMeSupport || (product as any).NvMeSupport || false,

                // GPU Specifications
                gpuType: (product as any).gpuType || (product as any).GpuType || '',
                gpuBrand: (product as any).gpuBrand || (product as any).GpuBrand || '',
                gpuModel: (product as any).gpuModel || (product as any).GpuModel || '',
                gpuVramGB: ((product as any).gpuVramGB || (product as any).GpuVramGB)?.toString() || '',

                // Display Specifications
                displaySizeInches: ((product as any).displaySizeInches || (product as any).DisplaySizeInches)?.toString() || '',
                displayResolution: (product as any).displayResolution || (product as any).DisplayResolution || '',
                displayPanelType: (product as any).displayPanelType || (product as any).DisplayPanelType || '',
                displayRefreshRateHz: ((product as any).displayRefreshRateHz || (product as any).DisplayRefreshRateHz)?.toString() || '',
                displayTouchscreen: (product as any).displayTouchscreen || (product as any).DisplayTouchscreen || false,

                // Physical Specifications
                batteryCapacityWh: ((product as any).batteryCapacityWh || (product as any).BatteryCapacityWh)?.toString() || '',
                weightKg: ((product as any).weightKg || (product as any).WeightKg)?.toString() || '',
                dimensions: (product as any).dimensions || (product as any).Dimensions || '',
                color: (product as any).color || (product as any).Color || '',
                ports: (product as any).ports || (product as any).Ports || '',

                // Connectivity
                wiFi6Support: (product as any).wiFi6Support || (product as any).WiFi6Support || false,
                bluetoothSupport: (product as any).bluetoothSupport || (product as any).BluetoothSupport || false,
                bluetoothVersion: (product as any).bluetoothVersion || (product as any).BluetoothVersion || '',

                // Business info
                warrantyPeriod: (product as any).warrantyPeriod || (product as any).WarrantyPeriod || '',
                targetAudience: (product as any).targetAudience || (product as any).TargetAudience || ''
            });
        }
    }, [product, showCreateDialog]);

    // Load variant images when variants are loaded
    useEffect(() => {
        if (variants && variants.length > 0) {
            const variantImagesMap: { [variantId: number]: ProductImage[] } = {};

            variants.forEach((variant: any) => {
                if (variant.images && variant.images.length > 0) {
                    const productImages: ProductImage[] = variant.images.map((img: any, index: number) => ({
                        imageId: img.imageId || img.id?.toString(),
                        imageUrl: img.imageUrl,
                        altText: img.altText || `Variant image ${index + 1}`,
                        displayOrder: img.sortOrder || index + 1
                    }));
                    variantImagesMap[variant.id] = productImages;
                }
            });

            setVariantImages(variantImagesMap);
        }
    }, [variants]);

    // Handle image upload for create/edit forms
    const handleImageUpload = (files: FileList | null, isEdit: boolean = false) => {
        if (!files) return;

        const newFiles = Array.from(files);
        const imageFiles = newFiles.filter(file => file.type.startsWith('image/'));

        if (isEdit) {
            setEditImages(prev => [...prev, ...imageFiles]);
            // Create preview URLs
            imageFiles.forEach(file => {
                const url = URL.createObjectURL(file);
                setEditImageUrls(prev => [...prev, url]);
            });
        } else {
            setCreateImages(prev => [...prev, ...imageFiles]);
            // Create preview URLs
            imageFiles.forEach(file => {
                const url = URL.createObjectURL(file);
                setCreateImageUrls(prev => [...prev, url]);
            });
        }
    };

    // Handle image upload for existing variants
    const handleVariantImageUpload = async (variantId: number, files: FileList) => {
        if (!canManageImages) {
            toast.error('Bạn không có quyền quản lý hình ảnh sản phẩm');
            return;
        }

        try {
            console.log(' Uploading images for variant:', variantId, 'Files:', files.length);
            const results = await uploadVariantImages(parseInt(productId), variantId, files);
            console.log(' Upload results:', results);

            const newImages = results.map((r, index) => ({
                imageId: r.imageId,
                imageUrl: r.imageUrl,
                altText: `Variant image ${(variantImages[variantId]?.length || 0) + index + 1}`,
                displayOrder: (variantImages[variantId]?.length || 0) + index + 1
            }));

            console.log(' New images to add:', newImages);

            setVariantImages(prev => {
                const updated = {
                    ...prev,
                    [variantId]: [...(prev[variantId] || []), ...newImages]
                };
                console.log(' Updated variantImages state:', updated);
                return updated;
            });

            toast.success('Images uploaded successfully!');
        } catch (error) {
            console.error('Error uploading variant images:', error);
            toast.error('Failed to upload images');
        }
    };

    // Handle image delete for existing variants
    const handleVariantImageDelete = async (variantId: number, imageId: string) => {
        if (!canManageImages) {
            toast.error('Bạn không có quyền quản lý hình ảnh sản phẩm');
            return;
        }

        try {
            await deleteVariantImage(parseInt(productId), variantId, imageId);

            setVariantImages(prev => ({
                ...prev,
                [variantId]: (prev[variantId] || []).filter(img => img.imageId !== imageId)
            }));

            // Update selectedVariant if it's the same variant
            if (selectedVariant && selectedVariant.id === variantId) {
                setSelectedVariant(prev => {
                    if (prev) {
                        return {
                            ...prev,
                            images: (prev.images || []).filter((img: any) =>
                                img.imageId !== imageId && img.id.toString() !== imageId
                            )
                        };
                    }
                    return prev;
                });
            }

            toast.success('Image deleted successfully!');
        } catch (error) {
            console.error('Error deleting variant image:', error);
            toast.error('Failed to delete image');
        }
    };

    // Handle image removal
    const handleImageRemove = (index: number, isEdit: boolean = false) => {
        if (isEdit) {
            setEditImages(prev => prev.filter((_, i) => i !== index));
            setEditImageUrls(prev => {
                const newUrls = prev.filter((_, i) => i !== index);
                // Revoke the URL to free memory
                URL.revokeObjectURL(prev[index]);
                return newUrls;
            });
        } else {
            setCreateImages(prev => prev.filter((_, i) => i !== index));
            setCreateImageUrls(prev => {
                const newUrls = prev.filter((_, i) => i !== index);
                // Revoke the URL to free memory
                URL.revokeObjectURL(prev[index]);
                return newUrls;
            });
        }
    };

    // Handle create variant - 2-step process like regular products
    const handleCreateVariant = async () => {
        if (!createFormData.variantName || !createFormData.variantSku || !createFormData.price) {
            toast.error('Vui lòng điền đầy đủ các trường bắt buộc');
            return;
        }

        const variantData = {
            // Basic fields
            variantName: createFormData.variantName,
            variantSku: createFormData.variantSku,
            price: parseFloat(createFormData.price),
            stockQuantity: parseInt(createFormData.stock) || 0,
            description: createFormData.description,
            isActive: true,

            // Laptop-specific fields - MỜ RỘNG TẤT CẢ THÔNG SỐ
            series: createFormData.series || undefined,

            // CPU Specifications
            cpuBrand: createFormData.cpuBrand || undefined,
            cpuModel: createFormData.cpuModel || undefined,
            cpuGeneration: createFormData.cpuGeneration || undefined,
            cpuCores: createFormData.cpuCores ? parseInt(createFormData.cpuCores) : undefined,
            cpuBaseClockGHz: createFormData.cpuBaseClockGHz ? parseFloat(createFormData.cpuBaseClockGHz) : undefined,
            cpuBoostClockGHz: createFormData.cpuBoostClockGHz ? parseFloat(createFormData.cpuBoostClockGHz) : undefined,
            cpuCache: createFormData.cpuCache || undefined,

            // RAM Specifications
            ramType: createFormData.ramType || undefined,
            ramCapacityGB: createFormData.ramCapacityGB ? parseInt(createFormData.ramCapacityGB) : undefined,
            ramSlots: createFormData.ramSlots ? parseInt(createFormData.ramSlots) : undefined,
            ramSpeed: createFormData.ramSpeed ? parseInt(createFormData.ramSpeed) : undefined,
            ramUpgradeable: createFormData.ramUpgradeable,

            // Storage Specifications
            storageType: createFormData.storageType || undefined,
            storageCapacityGB: createFormData.storageCapacityGB ? parseInt(createFormData.storageCapacityGB) : undefined,
            storageInterface: createFormData.storageInterface || undefined,
            nvMeSupport: createFormData.nvMeSupport,

            // GPU Specifications
            gpuType: createFormData.gpuType || undefined,
            gpuBrand: createFormData.gpuBrand || undefined,
            gpuModel: createFormData.gpuModel || undefined,
            gpuVramGB: createFormData.gpuVramGB ? parseInt(createFormData.gpuVramGB) : undefined,

            // Display Specifications
            displaySizeInches: createFormData.displaySizeInches ? parseFloat(createFormData.displaySizeInches) : undefined,
            displayResolution: createFormData.displayResolution || undefined,
            displayPanelType: createFormData.displayPanelType || undefined,
            displayRefreshRateHz: createFormData.displayRefreshRateHz ? parseInt(createFormData.displayRefreshRateHz) : undefined,
            displayTouchscreen: createFormData.displayTouchscreen,

            // Physical Specifications
            batteryCapacityWh: createFormData.batteryCapacityWh ? parseInt(createFormData.batteryCapacityWh) : undefined,
            weightKg: createFormData.weightKg ? parseFloat(createFormData.weightKg) : undefined,
            dimensions: createFormData.dimensions || undefined,
            color: createFormData.color || undefined,
            ports: createFormData.ports || undefined,

            // Connectivity
            wiFi6Support: createFormData.wiFi6Support,
            bluetoothSupport: createFormData.bluetoothSupport,
            bluetoothVersion: createFormData.bluetoothVersion || undefined,

            // Business info
            warrantyPeriod: createFormData.warrantyPeriod || undefined,
            targetAudience: createFormData.targetAudience || undefined
        };

        // Step 1: Create variant (without images)
        createVariantMutation.mutate(variantData, {
            onSuccess: async (newVariant) => {
                // Step 2: Upload images if any
                if (canManageImages && createImages.length > 0 && newVariant.id) {
                    try {
                        // Convert File objects to FileList
                        const dt = new DataTransfer();
                        createImages.forEach(file => dt.items.add(file));

                        await uploadVariantImages(parseInt(productId), newVariant.id, dt.files);
                        toast.success('Tạo biến thể và tải lên hình ảnh thành công!');
                    } catch (error) {
                        console.error('Error uploading variant images:', error);
                        toast.error('Tạo biến thể thành công nhưng tải lên hình ảnh thất bại');
                    }
                } else {
                    toast.success('Tạo biến thể thành công!');
                }

                setShowCreateDialog(false);

                // Reset form and images
                setCreateFormData({
                    variantName: '',
                    variantSku: '',
                    price: '',
                    stock: '',
                    description: '',
                    series: '',
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
                    dimensions: '',
                    color: '',
                    ports: '',
                    wiFi6Support: false,
                    bluetoothSupport: false,
                    bluetoothVersion: '',
                    warrantyPeriod: '',
                    targetAudience: ''
                });

                // Clear images
                setCreateImages([]);
                createImageUrls.forEach(url => URL.revokeObjectURL(url));
                setCreateImageUrls([]);
            }
        });
    };

    // Handle edit variant
    const handleEditVariant = (variant: any) => {
        console.log(' Edit Variant Data:', variant);
        console.log(' Base Product Data:', product);
        console.log(' Variant laptop-specific fields (camelCase):', {
            series: variant.series,
            cpuBrand: variant.cpuBrand,
            cpuModel: variant.cpuModel,
            ramType: variant.ramType,
            ramCapacityGB: variant.ramCapacityGB,
            storageType: variant.storageType,
            storageCapacityGB: variant.storageCapacityGB,
            displaySizeInches: variant.displaySizeInches,
            color: variant.color,
            weightKg: variant.weightKg
        });
        console.log(' Variant laptop-specific fields (PascalCase):', {
            Series: variant.Series,
            CpuBrand: variant.CpuBrand,
            CpuModel: variant.CpuModel,
            RamType: variant.RamType,
            RamCapacityGB: variant.RamCapacityGB,
            StorageType: variant.StorageType,
            StorageCapacityGB: variant.StorageCapacityGB,
            DisplaySizeInches: variant.DisplaySizeInches,
            Color: variant.Color,
            WeightKg: variant.WeightKg
        });

        setSelectedVariant(variant);

        // Merge variant data with base product data for laptop specs
        // Variants may not have laptop specs, so use base product as fallback
        const baseProduct = product as any;
        setEditFormData({
            // Basic fields - use variant data
            variantName: variant.variantName || variant.VariantName || '',
            variantSku: variant.variantSku || variant.VariantSku || '',
            price: variant.price?.toString() || '',
            stock: variant.stockQuantity?.toString() || '',
            description: variant.description || '',

            // Laptop-specific fields - try variant first, then base product
            series: variant.series || variant.Series || baseProduct?.series || '',
            cpuBrand: variant.cpuBrand || variant.CpuBrand || baseProduct?.cpuBrand || '',
            cpuModel: variant.cpuModel || variant.CpuModel || baseProduct?.cpuModel || '',
            cpuGeneration: variant.cpuGeneration || variant.CpuGeneration || baseProduct?.cpuGeneration || '',
            cpuCores: (variant.cpuCores || variant.CpuCores || baseProduct?.cpuCores)?.toString() || '',
            cpuBaseClockGHz: (variant.cpuBaseClockGHz || variant.CpuBaseClockGHz || baseProduct?.cpuBaseClockGHz)?.toString() || '',
            cpuBoostClockGHz: (variant.cpuBoostClockGHz || variant.CpuBoostClockGHz || baseProduct?.cpuBoostClockGHz)?.toString() || '',
            cpuCache: variant.cpuCache || variant.CpuCache || baseProduct?.cpuCache || '',
            ramType: variant.ramType || variant.RamType || baseProduct?.ramType || '',
            ramCapacityGB: (variant.ramCapacityGB || variant.RamCapacityGB || baseProduct?.ramCapacityGB)?.toString() || '',
            ramSlots: (variant.ramSlots || variant.RamSlots || baseProduct?.ramSlots)?.toString() || '',
            ramSpeed: (variant.ramSpeed || variant.RamSpeed || baseProduct?.ramSpeed)?.toString() || '',
            ramUpgradeable: variant.ramUpgradeable || variant.RamUpgradeable || baseProduct?.ramUpgradeable || false,
            storageType: variant.storageType || variant.StorageType || baseProduct?.storageType || '',
            storageCapacityGB: (variant.storageCapacityGB || variant.StorageCapacityGB || baseProduct?.storageCapacityGB)?.toString() || '',
            storageInterface: variant.storageInterface || variant.StorageInterface || baseProduct?.storageInterface || '',
            nvMeSupport: variant.nvMeSupport || variant.NvMeSupport || baseProduct?.nvMeSupport || false,
            gpuType: variant.gpuType || variant.GpuType || baseProduct?.gpuType || '',
            gpuBrand: variant.gpuBrand || variant.GpuBrand || baseProduct?.gpuBrand || '',
            gpuModel: variant.gpuModel || variant.GpuModel || baseProduct?.gpuModel || '',
            gpuVramGB: (variant.gpuVramGB || variant.GpuVramGB || baseProduct?.gpuVramGB)?.toString() || '',
            displaySizeInches: (variant.displaySizeInches || variant.DisplaySizeInches || baseProduct?.displaySizeInches)?.toString() || '',
            displayResolution: variant.displayResolution || variant.DisplayResolution || baseProduct?.displayResolution || '',
            displayPanelType: variant.displayPanelType || variant.DisplayPanelType || baseProduct?.displayPanelType || '',
            displayRefreshRateHz: (variant.displayRefreshRateHz || variant.DisplayRefreshRateHz || baseProduct?.displayRefreshRateHz)?.toString() || '',
            displayTouchscreen: variant.displayTouchscreen || variant.DisplayTouchscreen || baseProduct?.displayTouchscreen || false,
            batteryCapacityWh: (variant.batteryCapacityWh || variant.BatteryCapacityWh || baseProduct?.batteryCapacityWh)?.toString() || '',
            weightKg: (variant.weightKg || variant.WeightKg || baseProduct?.weightKg)?.toString() || '',
            dimensions: variant.dimensions || variant.Dimensions || baseProduct?.dimensions || '',
            color: variant.color || variant.Color || baseProduct?.color || '',
            ports: variant.ports || variant.Ports || baseProduct?.ports || '',
            wiFi6Support: variant.wiFi6Support || variant.WiFi6Support || baseProduct?.wiFi6Support || false,
            bluetoothSupport: variant.bluetoothSupport || variant.BluetoothSupport || baseProduct?.bluetoothSupport || false,
            bluetoothVersion: variant.bluetoothVersion || variant.BluetoothVersion || baseProduct?.bluetoothVersion || '',
            warrantyPeriod: variant.warrantyPeriod || variant.WarrantyPeriod || baseProduct?.warrantyPeriod || '',
            targetAudience: variant.targetAudience || variant.TargetAudience || baseProduct?.targetAudience || ''
        });

        // Load existing images for this variant
        console.log(' Loading images for variant:', variant.id);
        console.log(' Variant images from API:', variant.images);

        // Clear current edit images and URLs
        setEditImages([]);

        if (variant.images && variant.images.length > 0) {
            // Use images from API response (ProductImageDto structure)
            console.log(' Using variant.images from API:', variant.images);
            const imageUrls = variant.images.map((img: any) => img.imageUrl);
            setEditImageUrls(imageUrls);

            // Also update variantImages state for consistency
            const productImages: ProductImage[] = variant.images.map((img: any, index: number) => ({
                imageId: img.imageId || img.id.toString(), // Use ImageId if available, fallback to Id
                imageUrl: img.imageUrl,
                altText: img.altText || `Variant image ${index + 1}`,
                displayOrder: img.displayOrder || img.sortOrder || index + 1
            }));

            setVariantImages(prev => ({
                ...prev,
                [variant.id]: productImages
            }));
        } else {
            // Clear edit images if no existing images
            console.log(' No existing images found');
            setEditImageUrls([]);
        }

        setShowEditDialog(true);
    };

    // Handle update variant - 2-step process like regular products
    const handleUpdateVariant = () => {
        if (!editFormData.variantName || !editFormData.variantSku || !editFormData.price) {
            toast.error('Vui lòng điền đầy đủ các trường bắt buộc');
            return;
        }

        if (!selectedVariant) return;

        const variantData = {
            // Basic fields
            variantName: editFormData.variantName,
            variantSku: editFormData.variantSku,
            price: parseFloat(editFormData.price),
            stockQuantity: parseInt(editFormData.stock) || 0,
            description: editFormData.description,
            isActive: true,

            // Laptop-specific fields - MỜ RỘNG TẤT CẢ THÔNG SỐ
            series: editFormData.series || undefined,

            // CPU Specifications
            cpuBrand: editFormData.cpuBrand || undefined,
            cpuModel: editFormData.cpuModel || undefined,
            cpuGeneration: editFormData.cpuGeneration || undefined,
            cpuCores: editFormData.cpuCores ? parseInt(editFormData.cpuCores) : undefined,
            cpuBaseClockGHz: editFormData.cpuBaseClockGHz ? parseFloat(editFormData.cpuBaseClockGHz) : undefined,
            cpuBoostClockGHz: editFormData.cpuBoostClockGHz ? parseFloat(editFormData.cpuBoostClockGHz) : undefined,
            cpuCache: editFormData.cpuCache || undefined,

            // RAM Specifications
            ramType: editFormData.ramType || undefined,
            ramCapacityGB: editFormData.ramCapacityGB ? parseInt(editFormData.ramCapacityGB) : undefined,
            ramSlots: editFormData.ramSlots ? parseInt(editFormData.ramSlots) : undefined,
            ramSpeed: editFormData.ramSpeed ? parseInt(editFormData.ramSpeed) : undefined,
            ramUpgradeable: editFormData.ramUpgradeable,

            // Storage Specifications
            storageType: editFormData.storageType || undefined,
            storageCapacityGB: editFormData.storageCapacityGB ? parseInt(editFormData.storageCapacityGB) : undefined,
            storageInterface: editFormData.storageInterface || undefined,
            nvMeSupport: editFormData.nvMeSupport,

            // GPU Specifications
            gpuType: editFormData.gpuType || undefined,
            gpuBrand: editFormData.gpuBrand || undefined,
            gpuModel: editFormData.gpuModel || undefined,
            gpuVramGB: editFormData.gpuVramGB ? parseInt(editFormData.gpuVramGB) : undefined,

            // Display Specifications
            displaySizeInches: editFormData.displaySizeInches ? parseFloat(editFormData.displaySizeInches) : undefined,
            displayResolution: editFormData.displayResolution || undefined,
            displayPanelType: editFormData.displayPanelType || undefined,
            displayRefreshRateHz: editFormData.displayRefreshRateHz ? parseInt(editFormData.displayRefreshRateHz) : undefined,
            displayTouchscreen: editFormData.displayTouchscreen,

            // Physical Specifications
            batteryCapacityWh: editFormData.batteryCapacityWh ? parseInt(editFormData.batteryCapacityWh) : undefined,
            weightKg: editFormData.weightKg ? parseFloat(editFormData.weightKg) : undefined,
            dimensions: editFormData.dimensions || undefined,
            color: editFormData.color || undefined,
            ports: editFormData.ports || undefined,

            // Connectivity
            wiFi6Support: editFormData.wiFi6Support,
            bluetoothSupport: editFormData.bluetoothSupport,
            bluetoothVersion: editFormData.bluetoothVersion || undefined,

            // Business info
            warrantyPeriod: editFormData.warrantyPeriod || undefined,
            targetAudience: editFormData.targetAudience || undefined
        };

        // Step 1: Update variant (without images)
        updateVariantMutation.mutate({
            variantId: selectedVariant.id,
            variantData: variantData
        }, {
            onSuccess: async () => {
                // Step 2: Upload new images if any
                if (canManageImages && editImages.length > 0) {
                    try {
                        // Convert File objects to FileList
                        const dt = new DataTransfer();
                        editImages.forEach(file => dt.items.add(file));

                        await uploadVariantImages(parseInt(productId), selectedVariant.id, dt.files);
                        toast.success('Cập nhật biến thể và tải lên hình ảnh thành công!');
                    } catch (error) {
                        console.error('Error uploading variant images:', error);
                        toast.error('Cập nhật biến thể thành công nhưng tải lên hình ảnh thất bại');
                    }
                } else {
                    toast.success('Cập nhật biến thể thành công!');
                }

                setShowEditDialog(false);
                setSelectedVariant(null);
                // Clear images
                setEditImages([]);
                editImageUrls.forEach(url => {
                    if (url.startsWith('blob:')) {
                        URL.revokeObjectURL(url);
                    }
                });
                setEditImageUrls([]);
            }
        });
    };

    // Handle delete variant
    const handleDeleteVariant = (variantId: number) => {
        if (window.confirm('Bạn có chắc chắn muốn xóa biến thể này?')) {
            deleteVariantMutation.mutate(variantId);
        }
    };

    const filteredVariants = variants?.filter((variant: any) =>
        variant.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        variant.sku.toLowerCase().includes(searchTerm.toLowerCase()) ||
        variant.variantName?.toLowerCase().includes(searchTerm.toLowerCase())
    ) || [];

    if (productLoading) {
        return (
            <div className="space-y-6">
                <div className="flex items-center gap-4">
                    <Skeleton className="h-8 w-32" />
                    <Skeleton className="h-8 w-64" />
                </div>
                <Card>
                    <CardContent className="pt-6">
                        <div className="space-y-3">
                            {[...Array(5)].map((_, i) => (
                                <Skeleton key={i} className="h-12 w-full" />
                            ))}
                        </div>
                    </CardContent>
                </Card>
            </div>
        );
    }

    if (!product) {
        return (
            <div className="space-y-6">
                <div className="flex items-center gap-4">
                    <Link href="/admin/products">
                        <Button variant="outline" size="sm">
                            <ArrowLeft className="h-4 w-4 mr-2" />
                            Quay lại Sản phẩm
                        </Button>
                    </Link>
                    <div>
                        <h1 className="text-3xl font-bold">Không tìm thấy sản phẩm</h1>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                    <Link href="/admin/products">
                        <Button variant="outline" size="sm">
                            <ArrowLeft className="h-4 w-4 mr-2" />
                            Quay lại Sản phẩm
                        </Button>
                    </Link>
                    <div>
                        <h1 className="text-3xl font-bold">Quản lý Biến thể</h1>
                        <p className="text-muted-foreground">
                            {product.name} - {product.sku}
                        </p>
                    </div>
                </div>

                <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                    <Button type="button" onClick={() => setShowCreateDialog(true)}>
                        <Plus className="h-4 w-4 mr-2" />
                        Thêm Biến thể
                    </Button>
                </PermissionGuard>
            </div>

            {/* Product Info */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <Package className="h-5 w-5" />
                        Thông tin Sản phẩm Gốc
                    </CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                        <div>
                            <Label className="text-sm font-medium text-muted-foreground">Tên</Label>
                            <p className="font-medium">{product.name}</p>
                        </div>
                        <div>
                            <Label className="text-sm font-medium text-muted-foreground">SKU</Label>
                            <p className="font-mono text-sm">{product.sku}</p>
                        </div>
                        <div>
                            <Label className="text-sm font-medium text-muted-foreground">Gi</Label>
                            <p className="font-medium">{formatCurrency(product.price)}</p>
                        </div>
                    </div>
                </CardContent>
            </Card>

            {/* Search and Filters */}
            <Card>
                <CardHeader>
                    <CardTitle>Tìm kiếm Biến thể</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="flex gap-4">
                        <div className="flex-1">
                            <div className="relative">
                                <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                                <Input
                                    placeholder="Tìm kiếm biến thể theo tên, SKU, hoặc tên biến thể..."
                                    value={searchTerm}
                                    onChange={(e) => setSearchTerm(e.target.value)}
                                    className="pl-9"
                                />
                            </div>
                        </div>
                    </div>
                </CardContent>
            </Card>

            {/* Variants Table */}
            <Card>
                <CardHeader>
                    <div className="flex items-center justify-between">
                        <CardTitle>
                            Biến thể ({filteredVariants.length})
                        </CardTitle>
                        <Button type="button" variant="outline" onClick={() => window.location.reload()}>
                            Làm mới
                        </Button>
                    </div>
                </CardHeader>
                <CardContent>
                    {variantsLoading ? (
                        <div className="space-y-3">
                            {[...Array(3)].map((_, i) => (
                                <Skeleton key={i} className="h-12 w-full" />
                            ))}
                        </div>
                    ) : filteredVariants.length === 0 ? (
                        <div className="text-center py-8">
                            <Package className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                            <h3 className="text-lg font-semibold mb-2">Không tìm thấy biến thể</h3>
                            <p className="text-muted-foreground mb-4">
                                {searchTerm ? 'Không có biến thể nào phùhợp với tiêu chí tìm kiếm của bạn.' : 'Sản phẩm này chưa có biến thể nào.'}
                            </p>
                            {!searchTerm && (
                                <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                                    <Button type="button" onClick={() => setShowCreateDialog(true)}>
                                        <Plus className="h-4 w-4 mr-2" />
                                        Tạo Biến thể đầu tiên
                                    </Button>
                                </PermissionGuard>
                            )}
                        </div>
                    ) : (
                        <div className="space-y-4">
                            {/* Base Product Header */}
                            <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                                <div className="flex items-center gap-3">
                                    <div className="w-2 h-2 bg-blue-500 rounded-full"></div>
                                    <div className="flex-1">
                                        <h3 className="font-semibold text-blue-900">{product.name}</h3>
                                        <p className="text-sm text-blue-700">Sản phẩm Gốc - {product.sku}</p>
                                    </div>
                                    <div className="text-right">
                                        <p className="font-medium text-blue-900">{formatCurrency(product.price)}</p>
                                        <p className="text-sm text-blue-700">{getStockBadge(product.stockQuantity || 0)}</p>
                                    </div>
                                </div>
                            </div>

                            {/* Variants List */}
                            {filteredVariants.length > 0 && (
                                <div className="space-y-2">
                                    <h4 className="text-sm font-medium text-muted-foreground mb-3">Biến thể:</h4>
                                    {filteredVariants.map((variant: any) => (
                                        <div key={variant.id} className="bg-gray-50 border border-gray-200 rounded-lg p-4 ml-6">
                                            <div className="flex items-start justify-between">
                                                <div className="flex items-start gap-3">
                                                    <div className="w-1.5 h-1.5 bg-gray-400 rounded-full mt-2"></div>
                                                    <div className="flex-1">
                                                        <div className="font-medium">{variant.name}</div>
                                                        {variant.variantName && (
                                                            <div className="text-sm text-muted-foreground">
                                                                {variant.variantName}
                                                            </div>
                                                        )}
                                                        <div className="flex items-center gap-4 mt-1">
                                                            <code className="text-xs bg-gray-200 px-2 py-1 rounded">
                                                                {variant.sku}
                                                            </code>
                                                            {variant.variantSku && (
                                                                <code className="text-xs bg-gray-200 px-2 py-1 rounded">
                                                                    {variant.variantSku}
                                                                </code>
                                                            )}
                                                        </div>

                                                    </div>
                                                </div>
                                                <div className="flex items-center gap-4">
                                                    <div className="text-right">
                                                        <p className="font-medium">{formatCurrency(variant.price)}</p>
                                                        <div className="flex items-center gap-2">
                                                            {getStockBadge(variant.stockQuantity || 0)}
                                                            {getStatusBadge(variant.isActive)}
                                                        </div>
                                                    </div>
                                                    <div className="flex items-center gap-1">
                                                        <Button
                                                            variant="ghost"
                                                            size="sm"
                                                            onClick={() => handleEditVariant(variant)}
                                                        >
                                                            <Edit className="h-4 w-4" />
                                                        </Button>
                                                        <PermissionGuard permission={PERMISSIONS.PRODUCTS_DELETE}>
                                                            <Button
                                                                type="button"
                                                                variant="ghost"
                                                                size="sm"
                                                                onClick={() => handleDeleteVariant(variant.id)}
                                                                className="text-red-600 hover:text-red-700"
                                                            >
                                                                <Trash2 className="h-4 w-4" />
                                                            </Button>
                                                        </PermissionGuard>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            )}
                        </div>
                    )}
                </CardContent>
            </Card>

            {/* Create Variant Dialog */}
            <Dialog open={showCreateDialog} onOpenChange={setShowCreateDialog}>
                <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
                    <DialogHeader>
                        <DialogTitle>Tạo Biến thể Mới</DialogTitle>
                        <DialogDescription>
                            Tạo biến thể mới cho {product.name}
                        </DialogDescription>
                    </DialogHeader>
                    <Tabs defaultValue="basic" className="w-full">
                        <TabsList className="grid w-full grid-cols-7">
                            <TabsTrigger value="basic">Thông tin Cơ bản</TabsTrigger>
                            <TabsTrigger value="cpu">CPU</TabsTrigger>
                            <TabsTrigger value="ram">RAM</TabsTrigger>
                            <TabsTrigger value="storage">Lưu trữ</TabsTrigger>
                            <TabsTrigger value="display">Màn hình</TabsTrigger>
                            <TabsTrigger value="other">Khác</TabsTrigger>
                            <TabsTrigger value="images">Hình ảnh</TabsTrigger>
                        </TabsList>

                        {/* Basic Information Tab */}
                        <TabsContent value="basic" className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="variantName">Tên Biến thể *</Label>
                                    <Input
                                        id="variantName"
                                        placeholder="VD: 16GB RAM, 512GB SSD"
                                        value={createFormData.variantName}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, variantName: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="variantSku">SKU Biến thể *</Label>
                                    <Input
                                        id="variantSku"
                                        placeholder="VD: VAR-1-001"
                                        value={createFormData.variantSku}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, variantSku: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="price">Gi *</Label>
                                    <Input
                                        id="price"
                                        type="number"
                                        placeholder="0"
                                        value={createFormData.price}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, price: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="stock">Số lượng Tồn kho</Label>
                                    <Input
                                        id="stock"
                                        type="number"
                                        placeholder="0"
                                        value={createFormData.stock}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, stock: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div>
                                <Label htmlFor="description">Mô tả</Label>
                                <Input
                                    id="description"
                                    placeholder="Mô tả biến thể..."
                                    value={createFormData.description}
                                    onChange={(e) => setCreateFormData(prev => ({ ...prev, description: e.target.value }))}
                                />
                            </div>
                        </TabsContent>

                        {/* CPU Tab */}
                        <TabsContent value="cpu" className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="cpuBrand">Thương hiệu CPU</Label>
                                    <Input
                                        id="cpuBrand"
                                        placeholder="VD: Intel, AMD"
                                        value={createFormData.cpuBrand}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, cpuBrand: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="cpuModel">Model CPU</Label>
                                    <Input
                                        id="cpuModel"
                                        placeholder="VD: Core i7-12700H"
                                        value={createFormData.cpuModel}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, cpuModel: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="cpuGeneration">Thế hệ</Label>
                                    <Input
                                        id="cpuGeneration"
                                        placeholder="VD: 12th Gen"
                                        value={createFormData.cpuGeneration}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, cpuGeneration: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="cpuCores">Số nhân</Label>
                                    <Input
                                        id="cpuCores"
                                        type="number"
                                        placeholder="VD: 8"
                                        value={createFormData.cpuCores}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, cpuCores: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="cpuBaseClockGHz">Tần số Cơ bản (GHz)</Label>
                                    <Input
                                        id="cpuBaseClockGHz"
                                        type="number"
                                        step="0.1"
                                        placeholder="VD: 2.3"
                                        value={createFormData.cpuBaseClockGHz}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, cpuBaseClockGHz: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="cpuBoostClockGHz">Tần số Tăng tốc (GHz)</Label>
                                    <Input
                                        id="cpuBoostClockGHz"
                                        type="number"
                                        step="0.1"
                                        placeholder="VD: 4.7"
                                        value={createFormData.cpuBoostClockGHz}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, cpuBoostClockGHz: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div>
                                <Label htmlFor="cpuCache">Bộ nhớ đệm</Label>
                                <Input
                                    id="cpuCache"
                                    placeholder="VD: 24MB L3"
                                    value={createFormData.cpuCache}
                                    onChange={(e) => setCreateFormData(prev => ({ ...prev, cpuCache: e.target.value }))}
                                />
                            </div>
                        </TabsContent>

                        {/* RAM Tab */}
                        <TabsContent value="ram" className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="ramType">Loại RAM</Label>
                                    <Input
                                        id="ramType"
                                        placeholder="VD: DDR4, DDR5"
                                        value={createFormData.ramType}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, ramType: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="ramCapacityGB">Dung lượng (GB)</Label>
                                    <Input
                                        id="ramCapacityGB"
                                        type="number"
                                        placeholder="VD: 16"
                                        value={createFormData.ramCapacityGB}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, ramCapacityGB: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="ramSlots">Khe cắm RAM</Label>
                                    <Input
                                        id="ramSlots"
                                        type="number"
                                        placeholder="VD: 2"
                                        value={createFormData.ramSlots}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, ramSlots: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="ramSpeed">Tốc độ (MHz)</Label>
                                    <Input
                                        id="ramSpeed"
                                        type="number"
                                        placeholder="VD: 3200"
                                        value={createFormData.ramSpeed}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, ramSpeed: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="flex items-center space-x-2">
                                <Checkbox
                                    id="ramUpgradeable"
                                    checked={createFormData.ramUpgradeable}
                                    onCheckedChange={(checked) => setCreateFormData(prev => ({ ...prev, ramUpgradeable: !!checked }))}
                                />
                                <Label htmlFor="ramUpgradeable">RAM có thể nâng cấp</Label>
                            </div>
                        </TabsContent>

                        {/* Storage Tab */}
                        <TabsContent value="storage" className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="storageType">Loại lưu trữ</Label>
                                    <Input
                                        id="storageType"
                                        placeholder="VD: SSD, HDD"
                                        value={createFormData.storageType}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, storageType: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="storageCapacityGB">Dung lượng (GB)</Label>
                                    <Input
                                        id="storageCapacityGB"
                                        type="number"
                                        placeholder="VD: 512"
                                        value={createFormData.storageCapacityGB}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, storageCapacityGB: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="storageInterface">Giao diện</Label>
                                    <Input
                                        id="storageInterface"
                                        placeholder="VD: NVMe, SATA"
                                        value={createFormData.storageInterface}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, storageInterface: e.target.value }))}
                                    />
                                </div>
                                <div className="flex items-center space-x-2">
                                    <Checkbox
                                        id="nvMeSupport"
                                        checked={createFormData.nvMeSupport}
                                        onCheckedChange={(checked) => setCreateFormData(prev => ({ ...prev, nvMeSupport: !!checked }))}
                                    />
                                    <Label htmlFor="nvMeSupport">Hỗ trợ NVMe</Label>
                                </div>
                            </div>
                        </TabsContent>

                        {/* Display Tab */}
                        <TabsContent value="display" className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="displaySizeInches">Kích thước (inch)</Label>
                                    <Input
                                        id="displaySizeInches"
                                        type="number"
                                        step="0.1"
                                        placeholder="VD: 15.6"
                                        value={createFormData.displaySizeInches}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, displaySizeInches: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="displayResolution"> độ phân giải</Label>
                                    <Input
                                        id="displayResolution"
                                        placeholder="VD: 1920x1080"
                                        value={createFormData.displayResolution}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, displayResolution: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="displayPanelType">Loại màn hình</Label>
                                    <Input
                                        id="displayPanelType"
                                        placeholder="VD: IPS, OLED"
                                        value={createFormData.displayPanelType}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, displayPanelType: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="displayRefreshRateHz">Tần số quét (Hz)</Label>
                                    <Input
                                        id="displayRefreshRateHz"
                                        type="number"
                                        placeholder="VD: 144"
                                        value={createFormData.displayRefreshRateHz}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, displayRefreshRateHz: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="flex items-center space-x-2">
                                <Checkbox
                                    id="displayTouchscreen"
                                    checked={createFormData.displayTouchscreen}
                                    onCheckedChange={(checked) => setCreateFormData(prev => ({ ...prev, displayTouchscreen: !!checked }))}
                                />
                                <Label htmlFor="displayTouchscreen">Cảm ứng</Label>
                            </div>
                        </TabsContent>

                        {/* Other Tab */}
                        <TabsContent value="other" className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="gpuModel">Model GPU</Label>
                                    <Input
                                        id="gpuModel"
                                        placeholder="VD: RTX 4060"
                                        value={createFormData.gpuModel}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, gpuModel: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="color">Màu sắc</Label>
                                    <Input
                                        id="color"
                                        placeholder="VD: Silver, Space Gray"
                                        value={createFormData.color}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, color: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="weightKg">Trọng lượng (kg)</Label>
                                    <Input
                                        id="weightKg"
                                        type="number"
                                        step="0.1"
                                        placeholder="VD: 1.8"
                                        value={createFormData.weightKg}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, weightKg: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="batteryCapacityWh">Pin (Wh)</Label>
                                    <Input
                                        id="batteryCapacityWh"
                                        type="number"
                                        placeholder="VD: 83"
                                        value={createFormData.batteryCapacityWh}
                                        onChange={(e) => setCreateFormData(prev => ({ ...prev, batteryCapacityWh: e.target.value }))}
                                    />
                                </div>
                            </div>
                        </TabsContent>

                        {/* Images Tab */}
                        <TabsContent value="images" className="space-y-4">
                            <ImageUpload
                                productId={undefined} // No productId yet for create
                                images={createImages.map((file, index) => ({
                                    id: `temp-${index}`,
                                    imageUrl: createImageUrls[index] || '',
                                    altText: `Variant image ${index + 1}`,
                                    displayOrder: index + 1
                                }))}
                                onImagesChange={(images) => {
                                    // Simple callback - just update state based on what ImageUpload provides
                                    // This prevents the conflict between onImagesChange and onImageUpload
                                    const newFiles: File[] = [];
                                    const newUrls: string[] = [];

                                    images.forEach((img, index) => {
                                        if (img.imageUrl.startsWith('blob:')) {
                                            // Find corresponding file in createImages array
                                            const fileIndex = createImages.findIndex((_, i) => createImageUrls[i] === img.imageUrl);
                                            if (fileIndex >= 0) {
                                                newFiles.push(createImages[fileIndex]);
                                                newUrls.push(img.imageUrl);
                                            }
                                        }
                                    });

                                    // Only update if there are changes
                                    if (newFiles.length !== createImages.length || newUrls.length !== createImageUrls.length) {
                                        setCreateImages(newFiles);
                                        setCreateImageUrls(newUrls);
                                    }
                                }}
                                onImageUpload={async (files) => handleImageUpload(files, false)}
                                onImageDelete={async (imageId) => {
                                    const index = parseInt(imageId.replace('temp-', ''));
                                    handleImageRemove(index, false);
                                }}
                                maxImages={5}
                                disabled={!canManageImages}
                                className="w-full"
                            />
                        </TabsContent>
                    </Tabs>
                    <DialogFooter>
                        <Button type="button" variant="outline" onClick={() => setShowCreateDialog(false)}>
                            Hủy
                        </Button>
                        <Button
                            onClick={handleCreateVariant}
                            disabled={createVariantMutation.isPending}
                        >
                            {createVariantMutation.isPending ? 'đang tạo...' : 'Tạo Biến thể'}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* Edit Variant Dialog */}
            <Dialog open={showEditDialog} onOpenChange={setShowEditDialog}>
                <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
                    <DialogHeader>
                        <DialogTitle>Chỉnh sửa Biến thể</DialogTitle>
                        <DialogDescription>
                            Chỉnh sửa biến thể cho {product?.name}
                        </DialogDescription>
                    </DialogHeader>
                    <Tabs defaultValue="basic" className="w-full">
                        <TabsList className="grid w-full grid-cols-7">
                            <TabsTrigger value="basic">Thông tin Cơ bản</TabsTrigger>
                            <TabsTrigger value="cpu">CPU</TabsTrigger>
                            <TabsTrigger value="ram">RAM</TabsTrigger>
                            <TabsTrigger value="storage">Lưu trữ</TabsTrigger>
                            <TabsTrigger value="display">Màn hình</TabsTrigger>
                            <TabsTrigger value="other">Khác</TabsTrigger>
                            <TabsTrigger value="images">Hình ảnh</TabsTrigger>
                        </TabsList>

                        {/* Basic Information Tab */}
                        <TabsContent value="basic" className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="edit-variantName">Variant Name *</Label>
                                    <Input
                                        id="edit-variantName"
                                        placeholder="VD: 16GB RAM, 512GB SSD"
                                        value={editFormData.variantName}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, variantName: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="edit-variantSku">Variant SKU *</Label>
                                    <Input
                                        id="edit-variantSku"
                                        placeholder="VD: VAR-1-001"
                                        value={editFormData.variantSku}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, variantSku: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="edit-price">Price *</Label>
                                    <Input
                                        id="edit-price"
                                        type="number"
                                        placeholder="0"
                                        value={editFormData.price}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, price: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="edit-stock">Stock Quantity</Label>
                                    <Input
                                        id="edit-stock"
                                        type="number"
                                        placeholder="0"
                                        value={editFormData.stock}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, stock: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div>
                                <Label htmlFor="edit-description">Description</Label>
                                <Input
                                    id="edit-description"
                                    placeholder="Variant description..."
                                    value={editFormData.description}
                                    onChange={(e) => setEditFormData(prev => ({ ...prev, description: e.target.value }))}
                                />
                            </div>
                        </TabsContent>

                        {/* CPU Tab */}
                        <TabsContent value="cpu" className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="edit-cpuBrand">CPU Brand</Label>
                                    <Input
                                        id="edit-cpuBrand"
                                        placeholder="VD: Intel, AMD"
                                        value={editFormData.cpuBrand}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, cpuBrand: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="edit-cpuModel">CPU Model</Label>
                                    <Input
                                        id="edit-cpuModel"
                                        placeholder="VD: Core i7-12700H"
                                        value={editFormData.cpuModel}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, cpuModel: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="edit-cpuGeneration">Generation</Label>
                                    <Input
                                        id="edit-cpuGeneration"
                                        placeholder="VD: 12th Gen"
                                        value={editFormData.cpuGeneration}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, cpuGeneration: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="edit-cpuCores">Cores</Label>
                                    <Input
                                        id="edit-cpuCores"
                                        type="number"
                                        placeholder="VD: 8"
                                        value={editFormData.cpuCores}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, cpuCores: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="edit-cpuBaseClockGHz">Base Clock (GHz)</Label>
                                    <Input
                                        id="edit-cpuBaseClockGHz"
                                        type="number"
                                        step="0.1"
                                        placeholder="VD: 2.3"
                                        value={editFormData.cpuBaseClockGHz}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, cpuBaseClockGHz: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="edit-cpuBoostClockGHz">Boost Clock (GHz)</Label>
                                    <Input
                                        id="edit-cpuBoostClockGHz"
                                        type="number"
                                        step="0.1"
                                        placeholder="VD: 4.7"
                                        value={editFormData.cpuBoostClockGHz}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, cpuBoostClockGHz: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div>
                                <Label htmlFor="edit-cpuCache">Cache</Label>
                                <Input
                                    id="edit-cpuCache"
                                    placeholder="VD: 24MB L3"
                                    value={editFormData.cpuCache}
                                    onChange={(e) => setEditFormData(prev => ({ ...prev, cpuCache: e.target.value }))}
                                />
                            </div>
                        </TabsContent>

                        {/* RAM Tab */}
                        <TabsContent value="ram" className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="edit-ramType">RAM Type</Label>
                                    <Input
                                        id="edit-ramType"
                                        placeholder="VD: DDR4, DDR5"
                                        value={editFormData.ramType}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, ramType: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="edit-ramCapacityGB">Capacity (GB)</Label>
                                    <Input
                                        id="edit-ramCapacityGB"
                                        type="number"
                                        placeholder="VD: 16"
                                        value={editFormData.ramCapacityGB}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, ramCapacityGB: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="edit-ramSlots">RAM Slots</Label>
                                    <Input
                                        id="edit-ramSlots"
                                        type="number"
                                        placeholder="VD: 2"
                                        value={editFormData.ramSlots}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, ramSlots: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="edit-ramSpeed">Speed (MHz)</Label>
                                    <Input
                                        id="edit-ramSpeed"
                                        type="number"
                                        placeholder="VD: 3200"
                                        value={editFormData.ramSpeed}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, ramSpeed: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="flex items-center space-x-2">
                                <Checkbox
                                    id="edit-ramUpgradeable"
                                    checked={editFormData.ramUpgradeable}
                                    onCheckedChange={(checked) => setEditFormData(prev => ({ ...prev, ramUpgradeable: !!checked }))}
                                />
                                <Label htmlFor="edit-ramUpgradeable">RAM Upgradeable</Label>
                            </div>
                        </TabsContent>

                        {/* Storage Tab */}
                        <TabsContent value="storage" className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="edit-storageType">Storage Type</Label>
                                    <Input
                                        id="edit-storageType"
                                        placeholder="VD: SSD, HDD"
                                        value={editFormData.storageType}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, storageType: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="edit-storageCapacityGB">Capacity (GB)</Label>
                                    <Input
                                        id="edit-storageCapacityGB"
                                        type="number"
                                        placeholder="VD: 512"
                                        value={editFormData.storageCapacityGB}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, storageCapacityGB: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="edit-storageInterface">Interface</Label>
                                    <Input
                                        id="edit-storageInterface"
                                        placeholder="VD: NVMe, SATA"
                                        value={editFormData.storageInterface}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, storageInterface: e.target.value }))}
                                    />
                                </div>
                                <div className="flex items-center space-x-2">
                                    <Checkbox
                                        id="edit-nvMeSupport"
                                        checked={editFormData.nvMeSupport}
                                        onCheckedChange={(checked) => setEditFormData(prev => ({ ...prev, nvMeSupport: !!checked }))}
                                    />
                                    <Label htmlFor="edit-nvMeSupport">NVMe Support</Label>
                                </div>
                            </div>
                        </TabsContent>

                        {/* Display Tab */}
                        <TabsContent value="display" className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="edit-displaySizeInches">Size (inches)</Label>
                                    <Input
                                        id="edit-displaySizeInches"
                                        type="number"
                                        step="0.1"
                                        placeholder="VD: 15.6"
                                        value={editFormData.displaySizeInches}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, displaySizeInches: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="edit-displayResolution">Resolution</Label>
                                    <Input
                                        id="edit-displayResolution"
                                        placeholder="VD: 1920x1080"
                                        value={editFormData.displayResolution}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, displayResolution: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="edit-displayPanelType">Panel Type</Label>
                                    <Input
                                        id="edit-displayPanelType"
                                        placeholder="VD: IPS, OLED"
                                        value={editFormData.displayPanelType}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, displayPanelType: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="edit-displayRefreshRateHz">Refresh Rate (Hz)</Label>
                                    <Input
                                        id="edit-displayRefreshRateHz"
                                        type="number"
                                        placeholder="VD: 144"
                                        value={editFormData.displayRefreshRateHz}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, displayRefreshRateHz: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="flex items-center space-x-2">
                                <Checkbox
                                    id="edit-displayTouchscreen"
                                    checked={editFormData.displayTouchscreen}
                                    onCheckedChange={(checked) => setEditFormData(prev => ({ ...prev, displayTouchscreen: !!checked }))}
                                />
                                <Label htmlFor="edit-displayTouchscreen">Touchscreen</Label>
                            </div>
                        </TabsContent>

                        {/* Other Tab */}
                        <TabsContent value="other" className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="edit-gpuModel">GPU Model</Label>
                                    <Input
                                        id="edit-gpuModel"
                                        placeholder="VD: RTX 4060"
                                        value={editFormData.gpuModel}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, gpuModel: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="edit-color">Color</Label>
                                    <Input
                                        id="edit-color"
                                        placeholder="VD: Silver, Space Gray"
                                        value={editFormData.color}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, color: e.target.value }))}
                                    />
                                </div>
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <Label htmlFor="edit-weightKg">Weight (kg)</Label>
                                    <Input
                                        id="edit-weightKg"
                                        type="number"
                                        step="0.1"
                                        placeholder="VD: 1.8"
                                        value={editFormData.weightKg}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, weightKg: e.target.value }))}
                                    />
                                </div>
                                <div>
                                    <Label htmlFor="edit-batteryCapacityWh">Battery (Wh)</Label>
                                    <Input
                                        id="edit-batteryCapacityWh"
                                        type="number"
                                        placeholder="VD: 83"
                                        value={editFormData.batteryCapacityWh}
                                        onChange={(e) => setEditFormData(prev => ({ ...prev, batteryCapacityWh: e.target.value }))}
                                    />
                                </div>
                            </div>
                        </TabsContent>

                        {/* Images Tab */}
                        <TabsContent value="images" className="space-y-4">
                            <ImageUpload
                                productId={selectedVariant?.id} // Use selected variant ID for edit
                                images={editImageUrls.map((url, index) => {
                                    // Check if this is an existing image (not a blob URL)
                                    const isExistingImage = !url.startsWith('blob:');
                                    const existingImage = selectedVariant?.images?.find((img: any) => img.imageUrl === url);

                                    return {
                                        id: isExistingImage && existingImage
                                            ? existingImage.id.toString()
                                            : `temp-edit-${index}`,
                                        imageId: isExistingImage && existingImage
                                            ? ((existingImage as any).imageId || existingImage.id.toString())
                                            : `temp-edit-${index}`,
                                        imageUrl: url,
                                        altText: existingImage?.altText || `Variant image ${index + 1}`,
                                        displayOrder: existingImage?.sortOrder || index + 1
                                    };
                                })}
                                onImagesChange={(images) => {
                                    // Convert back to files and URLs
                                    const newFiles: File[] = [];
                                    const newUrls: string[] = [];

                                    images.forEach((img, index) => {
                                        if (img.imageUrl.startsWith('blob:')) {
                                            // This is a preview URL, keep the file
                                            const fileIndex = editImages.findIndex((_, i) => editImageUrls[i] === img.imageUrl);
                                            if (fileIndex >= 0) {
                                                newFiles.push(editImages[fileIndex]);
                                                newUrls.push(img.imageUrl);
                                            }
                                        } else {
                                            // This is an existing image URL
                                            newUrls.push(img.imageUrl);
                                        }
                                    });

                                    setEditImages(newFiles);
                                    setEditImageUrls(newUrls);
                                }}
                                onImageUpload={async (files) => handleImageUpload(files, true)}
                                onImageDelete={async (imageId) => {
                                    console.log(' Delete image called with imageId:', imageId);
                                    console.log(' Selected variant:', selectedVariant);
                                    console.log(' Selected variant images:', selectedVariant?.images);

                                    // Check if this is an existing image or a new one
                                    if (imageId.startsWith('temp-edit-')) {
                                        // New image - remove from arrays
                                        const index = parseInt(imageId.replace('temp-edit-', ''));
                                        console.log(' Removing new image at index:', index);
                                        handleImageRemove(index, true);
                                    } else {
                                        // Existing image - delete from server
                                        if (selectedVariant) {
                                            console.log(' Deleting existing image from server');

                                            // Find the image in variantImages state first
                                            const variantImagesList = variantImages[selectedVariant.id] || [];
                                            const imageToRemove = variantImagesList.find((img: any) =>
                                                img.imageId === imageId || img.id === imageId
                                            );

                                            console.log(' Image to remove found in variantImages:', imageToRemove);

                                            if (imageToRemove) {
                                                // Delete from server
                                                await handleVariantImageDelete(selectedVariant.id, imageId);

                                                // Remove from editImageUrls
                                                setEditImageUrls(prev => {
                                                    const newUrls = prev.filter(url => url !== imageToRemove.imageUrl);
                                                    console.log(' Updated editImageUrls:', newUrls);
                                                    return newUrls;
                                                });

                                                // Update variantImages state
                                                setVariantImages(prev => ({
                                                    ...prev,
                                                    [selectedVariant.id]: (prev[selectedVariant.id] || []).filter(img =>
                                                        (img as any).imageId !== imageId && (img as any).id !== imageId
                                                    )
                                                }));
                                            } else {
                                                console.log(' Image not found in variantImages, trying selectedVariant.images');

                                                // Fallback: try to find in selectedVariant.images
                                                const fallbackImage = selectedVariant.images?.find((img: any) =>
                                                    img.imageId === imageId || img.id.toString() === imageId
                                                );

                                                if (fallbackImage) {
                                                    console.log(' Found in selectedVariant.images, deleting from server');
                                                    await handleVariantImageDelete(selectedVariant.id, imageId);

                                                    // Remove from editImageUrls
                                                    setEditImageUrls(prev => {
                                                        const newUrls = prev.filter(url => url !== fallbackImage.imageUrl);
                                                        console.log(' Updated editImageUrls (fallback):', newUrls);
                                                        return newUrls;
                                                    });
                                                } else {
                                                    console.log(' Image not found anywhere, cannot delete');
                                                    toast.error('Image not found');
                                                }
                                            }
                                        }
                                    }
                                }}
                                maxImages={5}
                                disabled={!canManageImages}
                                className="w-full"
                            />
                        </TabsContent>
                    </Tabs>
                    <DialogFooter>
                        <Button type="button" variant="outline" onClick={() => {
                            setShowEditDialog(false);
                            setSelectedVariant(null);
                            // Clear images
                            setEditImages([]);
                            editImageUrls.forEach(url => {
                                if (url.startsWith('blob:')) {
                                    URL.revokeObjectURL(url);
                                }
                            });
                            setEditImageUrls([]);
                        }}>
                            Hủy
                        </Button>
                        <Button
                            onClick={handleUpdateVariant}
                            disabled={updateVariantMutation.isPending}
                        >
                            {updateVariantMutation.isPending ? 'đang cập nhật...' : 'Cập nhật Biến thể'}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    );
}
