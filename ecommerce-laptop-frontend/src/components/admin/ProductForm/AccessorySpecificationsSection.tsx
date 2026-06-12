'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Combobox, createOptions } from '@/components/ui/combobox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { ProductFormData } from '@/features/admin/products/types';
import { PRODUCT_SPECS_OPTIONS } from '@/lib/product-specs-options';
import { ChevronDown, ChevronRight } from 'lucide-react';

interface AccessorySpecificationsSectionProps {
    formData: ProductFormData;
    onInputChange: (field: keyof ProductFormData, value: any) => void;
    validationErrors: Record<string, string>;
    isExpanded: boolean;
    onToggle: () => void;
}

export default function AccessorySpecificationsSection({
    formData,
    onInputChange,
    validationErrors,
    isExpanded,
    onToggle
}: AccessorySpecificationsSectionProps) {
    return (
        <Card>
            <Collapsible open={isExpanded} onOpenChange={onToggle}>
                <CollapsibleTrigger asChild>
                    <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                        <div className="flex items-center justify-between">
                            <div>
                                <CardTitle>Thông số phụ kiện</CardTitle>
                                <CardDescription>
                                    Nhập thông tin chi tiết cho phụ kiện
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
                                <Label htmlFor="accessoryType">Loại phụ kiện</Label>
                                <Input
                                    id="accessoryType"
                                    value={formData.accessoryType || ''}
                                    onChange={(e) => onInputChange('accessoryType', e.target.value)}
                                    placeholder="VD: Chuột, Bàn phím, Tai nghe"
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="compatibility">Tương thích</Label>
                                <Input
                                    id="compatibility"
                                    value={formData.compatibility || ''}
                                    onChange={(e) => onInputChange('compatibility', e.target.value)}
                                    placeholder="VD: Windows/MacOS/Linux"
                                />
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-1 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="color">Màu sắc</Label>
                                <Combobox
                                    options={createOptions(PRODUCT_SPECS_OPTIONS.colors)}
                                    value={formData.color || ''}
                                    onValueChange={(value) => onInputChange('color', value)}
                                    placeholder="Chọn màu sắc..."
                                    searchPlaceholder="Tìm kiếm màu sắc..."
                                />
                            </div>
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="specificationDetails">Chi tiết thông số</Label>
                            <Textarea
                                id="specificationDetails"
                                value={formData.specificationDetails || ''}
                                onChange={(e) => onInputChange('specificationDetails', e.target.value)}
                                placeholder="Mô tả chi tiết về thông số kỹ thuật của phụ kiện"
                                rows={3}
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="connectivity">Kết nối</Label>
                            <Input
                                id="connectivity"
                                value={formData.connectivity || ''}
                                onChange={(e) => onInputChange('connectivity', e.target.value)}
                                placeholder="VD: USB-C, Bluetooth 5.0, Wireless"
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="warrantyPeriod">Bảo hành</Label>
                            <Combobox
                                options={createOptions(PRODUCT_SPECS_OPTIONS.warrantyPeriods)}
                                value={formData.warrantyPeriod || ''}
                                onValueChange={(value) => onInputChange('warrantyPeriod', value)}
                                placeholder="Chọn thời gian bảo hành..."
                                searchPlaceholder="Tìm kiếm thời gian bảo hành..."
                            />
                        </div>
                    </CardContent>
                </CollapsibleContent>
            </Collapsible>
        </Card>
    );
}
