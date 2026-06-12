'use client';

import { ProductImage } from '@/components/admin/ImageUpload';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Progress } from '@/components/ui/progress';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { ProductFormData } from '@/lib/admin-api';
import { AlertCircle, CheckCircle, Save } from 'lucide-react';
import { useEffect, useMemo, useState } from 'react';
import AccessorySpecificationsSection from './AccessorySpecificationsSection';
import BasicInformationSection from './BasicInformationSection';
import BundleSpecificationsSection from './BundleSpecificationsSection';
import InventoryManagementSection from './InventoryManagementSection';
import LaptopSpecificationsSection from './LaptopSpecificationsSection';
import MediaManagementSection from './MediaManagementSection';

interface EnhancedProductFormProps {
    formData: ProductFormData;
    onFormDataChange: (data: ProductFormData) => void;
    onSubmit: (data: ProductFormData) => void;
    isLoading: boolean;
    categories: any[];
    brands: any[];
    products?: any[];
    onCategoryCreated?: (category: any) => void;
    onBrandCreated?: (brand: any) => void;
    // Media wiring
    productId?: number;
    images?: ProductImage[];
    onImagesChange?: (images: ProductImage[]) => void;
    onImageUpload?: (files: FileList) => Promise<void>;
    onImageDelete?: (imageId: string) => Promise<void>;
    imageManagementDisabled?: boolean;
}

export default function EnhancedProductForm({
    formData,
    onFormDataChange,
    onSubmit,
    isLoading,
    categories,
    brands,
    products = [],
    onCategoryCreated,
    onBrandCreated,
    productId,
    images,
    onImagesChange,
    onImageUpload,
    onImageDelete,
    imageManagementDisabled = false
}: EnhancedProductFormProps) {
    const [activeTab, setActiveTab] = useState('basic');
    const [expandedSections, setExpandedSections] = useState<Record<string, boolean>>({
        'basic-info': true,
        'laptop-specs': true,
        'accessory-specs': true,
        'bundle-specs': true,
        'inventory': true,
        'media': true
    });
    const [hasUnsavedChanges, setHasUnsavedChanges] = useState(false);
    const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

    // Calculate form completion progress
    const completionProgress = useMemo(() => {
        const requiredFields = [
            'name', 'sku', 'productType', 'brandId', 'price'
        ];

        const filledFields = requiredFields.filter(field => {
            const value = formData[field as keyof ProductFormData];
            return value && value.toString().trim() !== '';
        });

        return Math.round((filledFields.length / requiredFields.length) * 100);
    }, [formData]);

    // Check for validation errors
    const hasErrors = useMemo(() => {
        return Object.keys(validationErrors).length > 0;
    }, [validationErrors]);

    // Handle input changes
    const handleInputChange = (field: keyof ProductFormData, value: any) => {
        onFormDataChange({
            ...formData,
            [field]: value
        });
        setHasUnsavedChanges(true);
    };

    // Handle nested input changes (for inventory, bundle items, etc.)
    const handleNestedInputChange = (field: string, subField: string, value: any) => {
        onFormDataChange({
            ...formData,
            [field]: {
                ...(formData[field as keyof ProductFormData] as any),
                [subField]: value
            }
        });
        setHasUnsavedChanges(true);
    };

    // Toggle section expansion
    const toggleSection = (section: string) => {
        setExpandedSections(prev => ({
            ...prev,
            [section]: !prev[section]
        }));
    };

    const isSectionExpanded = (section: string) => {
        return expandedSections[section] ?? true;
    };

    // Auto-save functionality
    useEffect(() => {
        if (hasUnsavedChanges) {
            const timer = setTimeout(() => {
                // Auto-save logic here
                console.log('Auto-saving...');
                setHasUnsavedChanges(false);
            }, 3000);

            return () => clearTimeout(timer);
        }
    }, [hasUnsavedChanges, formData]);

    // Form submission
    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        onSubmit(formData);
    };

    return (
        <div className="space-y-6">
            {/* Progress Indicator */}
            <Card>
                <CardContent className="pt-6">
                    <div className="space-y-2">
                        <div className="flex items-center justify-between">
                            <span className="text-sm font-medium">Tiến độ hoàn thành form</span>
                            <span className="text-sm text-muted-foreground">{completionProgress}%</span>
                        </div>
                        <Progress value={completionProgress} className="h-2" />
                        {hasUnsavedChanges && (
                            <div className="flex items-center gap-2 text-amber-600 text-sm">
                                <AlertCircle className="h-4 w-4" />
                                <span>Có thay đổi chưa được lưu</span>
                            </div>
                        )}
                    </div>
                </CardContent>
            </Card>

            <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
                <TabsList className="grid w-full grid-cols-4">
                    <TabsTrigger value="basic">Thông tin cơ bản</TabsTrigger>
                    <TabsTrigger value="specs">Thông số kỹ thuật</TabsTrigger>
                    <TabsTrigger value="inventory">Kho hàng</TabsTrigger>
                    <TabsTrigger value="media">Hình ảnh & Media</TabsTrigger>
                </TabsList>

                {/* Basic Information Tab */}
                <TabsContent value="basic" className="space-y-6">
                    <BasicInformationSection
                        formData={formData}
                        onInputChange={handleInputChange}
                        validationErrors={validationErrors}
                        categories={categories}
                        brands={brands}
                        isExpanded={isSectionExpanded('basic-info')}
                        onToggle={() => toggleSection('basic-info')}
                        onCategoryCreated={onCategoryCreated}
                        onBrandCreated={onBrandCreated}
                    />
                </TabsContent>

                {/* Specifications Tab */}
                <TabsContent value="specs" className="space-y-6">
                    {formData.productType === 'Laptop' && (
                        <LaptopSpecificationsSection
                            formData={formData}
                            onInputChange={handleInputChange}
                            validationErrors={validationErrors}
                            expandedSections={expandedSections}
                            onToggleSection={toggleSection}
                        />
                    )}
                    {formData.productType === 'Accessory' && (
                        <AccessorySpecificationsSection
                            formData={formData}
                            onInputChange={handleInputChange}
                            validationErrors={validationErrors}
                            isExpanded={isSectionExpanded('accessory-specs')}
                            onToggle={() => toggleSection('accessory-specs')}
                        />
                    )}
                    {formData.productType === 'Bundle' && (
                        <BundleSpecificationsSection
                            formData={formData}
                            onInputChange={handleInputChange}
                            onNestedInputChange={handleNestedInputChange}
                            validationErrors={validationErrors}
                            products={products}
                            isExpanded={isSectionExpanded('bundle-specs')}
                            onToggle={() => toggleSection('bundle-specs')}
                        />
                    )}
                </TabsContent>

                {/* Inventory Tab */}
                <TabsContent value="inventory" className="space-y-6">
                    <InventoryManagementSection
                        formData={formData}
                        onNestedInputChange={handleNestedInputChange}
                        validationErrors={validationErrors}
                        isExpanded={isSectionExpanded('inventory')}
                        onToggle={() => toggleSection('inventory')}
                    />
                </TabsContent>

                {/* Media Tab */}
                <TabsContent value="media" className="space-y-6">
                    <MediaManagementSection
                        formData={formData}
                        onInputChange={handleInputChange}
                        isExpanded={isSectionExpanded('media')}
                        onToggle={() => toggleSection('media')}
                        productId={productId}
                        images={images}
                        onImagesChange={onImagesChange}
                        onImageUpload={onImageUpload}
                        onImageDelete={onImageDelete}
                        disabled={imageManagementDisabled}
                    />
                </TabsContent>

            </Tabs>

            {/* Action Buttons */}
            <Card>
                <CardContent className="pt-6">
                    <div className="flex items-center justify-between">
                        <div className="flex items-center gap-2">
                            {hasErrors ? (
                                <Badge variant="destructive" className="flex items-center gap-1">
                                    <AlertCircle className="h-3 w-3" />
                                    Có lỗi validation
                                </Badge>
                            ) : (
                                <Badge variant="default" className="flex items-center gap-1">
                                    <CheckCircle className="h-3 w-3" />
                                    Form hợp lệ
                                </Badge>
                            )}
                        </div>
                        <div className="flex items-center gap-2">
                            <Button
                                variant="outline"
                                onClick={() => {
                                    // Save draft logic
                                    console.log('Saving draft...');
                                    setHasUnsavedChanges(false);
                                }}
                                disabled={isLoading}
                            >
                                <Save className="h-4 w-4 mr-2" />
                                Lưu nháp
                            </Button>
                            <Button
                                onClick={handleSubmit}
                                disabled={isLoading || hasErrors}
                                className="min-w-[120px]"
                            >
                                {isLoading ? (
                                    <>
                                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2" />
                                        Đang lưu...
                                    </>
                                ) : (
                                    <>
                                        <Save className="h-4 w-4 mr-2" />
                                        Lưu sản phẩm
                                    </>
                                )}
                            </Button>
                        </div>
                    </div>
                </CardContent>
            </Card>
        </div>
    );
}
