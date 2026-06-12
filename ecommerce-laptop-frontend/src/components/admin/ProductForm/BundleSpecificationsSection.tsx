'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { ProductFormData } from '@/features/admin/products/types';
import { ChevronDown, ChevronRight, Plus, X } from 'lucide-react';

interface BundleSpecificationsSectionProps {
    formData: ProductFormData;
    onInputChange: (field: keyof ProductFormData, value: any) => void;
    onNestedInputChange: (field: string, subField: string, value: any) => void;
    validationErrors: Record<string, string>;
    products: any[];
    isExpanded: boolean;
    onToggle: () => void;
}

export default function BundleSpecificationsSection({
    formData,
    onInputChange,
    onNestedInputChange,
    validationErrors,
    products,
    isExpanded,
    onToggle
}: BundleSpecificationsSectionProps) {
    const addBundleItem = () => {
        const newItem = {
            productId: '',
            quantity: '1',
            discountPercentage: '0'
        };
        onInputChange('bundleItems', [...(formData.bundleItems || []), newItem]);
    };

    const removeBundleItem = (index: number) => {
        const updatedItems = formData.bundleItems?.filter((_, i) => i !== index) || [];
        onInputChange('bundleItems', updatedItems);
    };

    const updateBundleItem = (index: number, field: string, value: string) => {
        const updatedItems = formData.bundleItems?.map((item, i) =>
            i === index ? { ...item, [field]: value } : item
        ) || [];
        onInputChange('bundleItems', updatedItems);
    };

    return (
        <Card>
            <Collapsible open={isExpanded} onOpenChange={onToggle}>
                <CollapsibleTrigger asChild>
                    <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                        <div className="flex items-center justify-between">
                            <div>
                                <CardTitle>Thông số combo</CardTitle>
                                <CardDescription>
                                    Nhập thông tin chi tiết cho combo sản phẩm
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
                                <Label htmlFor="bundleType">Loại combo</Label>
                                <Input
                                    id="bundleType"
                                    value={formData.bundleType || ''}
                                    onChange={(e) => onInputChange('bundleType', e.target.value)}
                                    placeholder="VD: Combo Gaming, Combo Văn phòng"
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="discountPercentage">Phần trăm giảm giá</Label>
                                <Input
                                    id="discountPercentage"
                                    type="number"
                                    value={formData.discountPercentage || ''}
                                    onChange={(e) => onInputChange('discountPercentage', e.target.value)}
                                    placeholder="VD: 10"
                                />
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="validFrom">Có hiệu lực từ</Label>
                                <Input
                                    id="validFrom"
                                    type="date"
                                    value={formData.validFrom || ''}
                                    onChange={(e) => onInputChange('validFrom', e.target.value)}
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="validTo">Có hiệu lực đến</Label>
                                <Input
                                    id="validTo"
                                    type="date"
                                    value={formData.validTo || ''}
                                    onChange={(e) => onInputChange('validTo', e.target.value)}
                                />
                            </div>
                        </div>

                        <div className="space-y-4">
                            <div className="flex items-center justify-between">
                                <Label>Sản phẩm trong combo</Label>
                                <Button type="button" onClick={addBundleItem} size="sm">
                                    <Plus className="h-4 w-4 mr-2" />
                                    Thêm sản phẩm
                                </Button>
                            </div>

                            {formData.bundleItems?.map((item, index) => (
                                <div key={index} className="p-4 border rounded-lg space-y-3">
                                    <div className="flex items-center justify-between">
                                        <span className="font-medium">Sản phẩm {index + 1}</span>
                                        <Button
                                            type="button"
                                            variant="ghost"
                                            size="sm"
                                            onClick={() => removeBundleItem(index)}
                                        >
                                            <X className="h-4 w-4" />
                                        </Button>
                                    </div>

                                    <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                                        <div className="space-y-2">
                                            <Label>Sản phẩm</Label>
                                            <select
                                                value={item.productId}
                                                onChange={(e) => updateBundleItem(index, 'productId', e.target.value)}
                                                className="w-full p-2 border rounded-md"
                                            >
                                                <option value="">Chọn sản phẩm</option>
                                                {products?.map((product) => (
                                                    <option key={product.id} value={product.id.toString()}>
                                                        {product.name}
                                                    </option>
                                                ))}
                                            </select>
                                        </div>
                                        <div className="space-y-2">
                                            <Label>Số lượng</Label>
                                            <Input
                                                type="number"
                                                value={item.quantity}
                                                onChange={(e) => updateBundleItem(index, 'quantity', e.target.value)}
                                                placeholder="1"
                                                min="1"
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label>Giảm giá (%)</Label>
                                            <Input
                                                type="number"
                                                value={item.discountPercentage}
                                                onChange={(e) => updateBundleItem(index, 'discountPercentage', e.target.value)}
                                                placeholder="0"
                                                min="0"
                                                max="100"
                                            />
                                        </div>
                                    </div>
                                </div>
                            ))}

                            {(!formData.bundleItems || formData.bundleItems.length === 0) && (
                                <div className="text-center py-8 text-muted-foreground">
                                    <p>Chưa có sản phẩm nào trong combo</p>
                                    <p className="text-sm">Nhấn "Thêm sản phẩm" để bắt đầu</p>
                                </div>
                            )}
                        </div>
                    </CardContent>
                </CollapsibleContent>
            </Collapsible>
        </Card>
    );
}

