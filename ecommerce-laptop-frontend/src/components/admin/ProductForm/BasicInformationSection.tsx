'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Combobox, createOptions } from '@/components/ui/combobox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { ProductFormData } from '@/features/admin/products/types';
import { PRODUCT_SPECS_OPTIONS } from '@/lib/product-specs-options';
import { ChevronDown, ChevronRight, Plus } from 'lucide-react';
import { useState } from 'react';
import BrandCreateModal from '../BrandCreateModal';
import CategoryCreateModal from '../CategoryCreateModal';

interface BasicInformationSectionProps {
    formData: ProductFormData;
    onInputChange: (field: keyof ProductFormData, value: any) => void;
    validationErrors: Record<string, string>;
    categories: any[];
    brands: any[];
    isExpanded: boolean;
    onToggle: () => void;
    onCategoryCreated?: (category: any) => void;
    onBrandCreated?: (brand: any) => void;
}

export default function BasicInformationSection({
    formData,
    onInputChange,
    validationErrors,
    categories,
    brands,
    isExpanded,
    onToggle,
    onCategoryCreated,
    onBrandCreated
}: BasicInformationSectionProps) {
    const [showCategoryModal, setShowCategoryModal] = useState(false);
    const [showBrandModal, setShowBrandModal] = useState(false);

    // Get parent categories for the modal
    const parentCategories = categories?.filter(cat => !cat.parentId) || [];
    const hasSelectedCategory = Boolean(
        formData.categoryId && categories?.some(category => category.id?.toString() === formData.categoryId)
    );
    const hasSelectedBrand = Boolean(
        formData.brandId && brands?.some(brand => brand.id?.toString() === formData.brandId)
    );

    return (
        <Card>
            <Collapsible open={isExpanded} onOpenChange={onToggle}>
                <CollapsibleTrigger asChild>
                    <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                        <div className="flex items-center justify-between">
                            <div>
                                <CardTitle>Base</CardTitle>
                                <CardDescription>
                                    Nhập thông tin cơ bản của sản phẩm
                                </CardDescription>
                            </div>
                            {isExpanded ? (
                                <ChevronDown className="h-5 w-5 text-muted-foreground" />
                            ) : (
                                <ChevronRight className="h-5 w-5 text-muted-foreground" />
                            )}
                        </div>
                    </CardHeader>
                </CollapsibleTrigger>
                <CollapsibleContent>
                    <CardContent className="space-y-4">
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="name">Tên sản phẩm *</Label>
                                <Input
                                    id="name"
                                    value={formData.name}
                                    onChange={(e) => onInputChange('name', e.target.value)}
                                    placeholder="Nhập tên sản phẩm"
                                    className={validationErrors.name ? 'border-red-500' : ''}
                                />
                                {validationErrors.name && (
                                    <p className="text-sm text-red-500">{validationErrors.name}</p>
                                )}
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="sku">Mã SKU *</Label>
                                <Input
                                    id="sku"
                                    value={formData.sku}
                                    onChange={(e) => onInputChange('sku', e.target.value)}
                                    placeholder="VD: MBP-M3-16-512"
                                    className={validationErrors.sku ? 'border-red-500' : ''}
                                />
                                {validationErrors.sku && (
                                    <p className="text-sm text-red-500">{validationErrors.sku}</p>
                                )}
                            </div>
                        </div>


                        <div className="space-y-2">
                            <Label htmlFor="description">Mô tả chi tiết</Label>
                            <Textarea
                                id="description"
                                value={formData.description}
                                onChange={(e) => onInputChange('description', e.target.value)}
                                placeholder="Mô tả chi tiết về sản phẩm"
                                rows={4}
                            />
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="price">Giá bán *</Label>
                                <Input
                                    id="price"
                                    type="number"
                                    value={formData.price}
                                    onChange={(e) => onInputChange('price', e.target.value)}
                                    placeholder="0"
                                    className={validationErrors.price ? 'border-red-500' : ''}
                                />
                                {validationErrors.price && (
                                    <p className="text-sm text-red-500">{validationErrors.price}</p>
                                )}
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="stock">Số lượng *</Label>
                                <Input
                                    id="stock"
                                    type="number"
                                    value={formData.stock}
                                    onChange={(e) => onInputChange('stock', e.target.value)}
                                    placeholder="0"
                                    className={validationErrors.stock ? 'border-red-500' : ''}
                                />
                                {validationErrors.stock && (
                                    <p className="text-sm text-red-500">{validationErrors.stock}</p>
                                )}
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="weight">Trọng lượng</Label>
                                <Combobox
                                    options={createOptions(PRODUCT_SPECS_OPTIONS.weightRanges)}
                                    value={formData.weight}
                                    onValueChange={(value) => onInputChange('weight', value)}
                                    placeholder="Chọn trọng lượng..."
                                    searchPlaceholder="Tìm kiếm trọng lượng..."
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="dimensions">Kích thước</Label>
                                <Input
                                    id="dimensions"
                                    value={formData.dimensions}
                                    onChange={(e) => onInputChange('dimensions', e.target.value)}
                                    placeholder="VD: 35.6 x 24.8 x 1.8 cm"
                                />
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="productType">Loại sản phẩm *</Label>
                                <Select
                                    value={formData.productType}
                                    onValueChange={(value: 'Laptop' | 'Accessory' | 'Bundle') => onInputChange('productType', value)}
                                >
                                    <SelectTrigger className={validationErrors.productType ? 'border-red-500' : ''}>
                                        <SelectValue placeholder="Chọn loại sản phẩm" />
                                    </SelectTrigger>
                                    <SelectContent>
                                        <SelectItem value="Laptop">Laptop</SelectItem>
                                        <SelectItem value="Accessory">Phụ kiện</SelectItem>
                                        <SelectItem value="Bundle">Combo</SelectItem>
                                    </SelectContent>
                                </Select>
                                {validationErrors.productType && (
                                    <p className="text-sm text-red-500">{validationErrors.productType}</p>
                                )}
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="categoryId">Danh mục *</Label>
                                <div className="flex gap-2">
                                    <Select
                                        value={formData.categoryId}
                                        onValueChange={(value) => onInputChange('categoryId', value)}
                                    >
                                        <SelectTrigger className={validationErrors.categoryId ? 'border-red-500' : ''}>
                                            <SelectValue placeholder="Chọn danh mục" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {formData.categoryId && !hasSelectedCategory && (
                                                <SelectItem value={formData.categoryId}>
                                                    Danh mục #{formData.categoryId}
                                                </SelectItem>
                                            )}
                                            {categories?.map((category) => (
                                                <SelectItem key={category.id} value={category.id.toString()}>
                                                    {category.name}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                    <Button
                                        type="button"
                                        variant="outline"
                                        size="sm"
                                        onClick={() => setShowCategoryModal(true)}
                                        className="px-3"
                                    >
                                        <Plus className="h-4 w-4" />
                                    </Button>
                                </div>
                                {validationErrors.categoryId && (
                                    <p className="text-sm text-red-500">{validationErrors.categoryId}</p>
                                )}
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="brandId">Thương hiệu *</Label>
                                <div className="flex gap-2">
                                    <Select
                                        value={formData.brandId}
                                        onValueChange={(value) => onInputChange('brandId', value)}
                                    >
                                        <SelectTrigger className={validationErrors.brandId ? 'border-red-500' : ''}>
                                            <SelectValue placeholder="Chọn thương hiệu" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            {formData.brandId && !hasSelectedBrand && (
                                                <SelectItem value={formData.brandId}>
                                                    Thương hiệu #{formData.brandId}
                                                </SelectItem>
                                            )}
                                            {brands?.map((brand) => (
                                                <SelectItem key={brand.id} value={brand.id.toString()}>
                                                    {brand.name}
                                                </SelectItem>
                                            ))}
                                        </SelectContent>
                                    </Select>
                                    <Button
                                        type="button"
                                        variant="outline"
                                        size="sm"
                                        onClick={() => setShowBrandModal(true)}
                                        className="px-3"
                                    >
                                        <Plus className="h-4 w-4" />
                                    </Button>
                                </div>
                                {validationErrors.brandId && (
                                    <p className="text-sm text-red-500">{validationErrors.brandId}</p>
                                )}
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-1 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="status">Trạng thái sản phẩm</Label>
                                <Select
                                    value={formData.status}
                                    onValueChange={(value: 'active' | 'inactive') => onInputChange('status', value)}
                                >
                                    <SelectTrigger>
                                        <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent>
                                        <SelectItem value="active">Đang bán</SelectItem>
                                        <SelectItem value="inactive">Tạm ngừng</SelectItem>
                                    </SelectContent>
                                </Select>
                            </div>
                        </div>
                    </CardContent>
                </CollapsibleContent>
            </Collapsible>

            {/* Category Create Modal */}
            <CategoryCreateModal
                open={showCategoryModal}
                onOpenChange={setShowCategoryModal}
                onCategoryCreated={(category) => {
                    onCategoryCreated?.(category);
                    // Auto-select the newly created category
                    onInputChange('categoryId', category.id.toString());
                }}
                parentCategories={parentCategories}
            />

            {/* Brand Create Modal */}
            <BrandCreateModal
                open={showBrandModal}
                onOpenChange={setShowBrandModal}
                onBrandCreated={(brand) => {
                    onBrandCreated?.(brand);
                    // Auto-select the newly created brand
                    onInputChange('brandId', brand.id.toString());
                }}
            />
        </Card>
    );
}
