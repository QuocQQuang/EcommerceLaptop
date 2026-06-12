'use client';

import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { ProductFormData } from '@/lib/admin-api';
import { ChevronDown, ChevronRight } from 'lucide-react';

interface InventoryManagementSectionProps {
    formData: ProductFormData;
    onNestedInputChange: (field: string, subField: string, value: any) => void;
    validationErrors: Record<string, string>;
    isExpanded: boolean;
    onToggle: () => void;
}

export default function InventoryManagementSection({
    formData,
    onNestedInputChange,
    validationErrors,
    isExpanded,
    onToggle
}: InventoryManagementSectionProps) {
    return (
        <Card>
            <Collapsible open={isExpanded} onOpenChange={onToggle}>
                <CollapsibleTrigger asChild>
                    <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                        <div className="flex items-center justify-between">
                            <div>
                                <CardTitle>Quản lý kho hàng</CardTitle>
                                <CardDescription>
                                    Quản lý số lượng tồn kho, mức cảnh báo và vị trí kho
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
                                <Label htmlFor="quantityInStock">Số lượng tồn kho</Label>
                                <Input
                                    id="quantityInStock"
                                    type="number"
                                    value={formData.inventory?.quantityInStock || ''}
                                    onChange={(e) => onNestedInputChange('inventory', 'quantityInStock', e.target.value)}
                                    placeholder="0"
                                    className={validationErrors.quantityInStock ? 'border-red-500' : ''}
                                />
                                {validationErrors.quantityInStock && (
                                    <p className="text-sm text-red-500">{validationErrors.quantityInStock}</p>
                                )}
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="reservedQuantity">Số lượng đã đặt</Label>
                                <Input
                                    id="reservedQuantity"
                                    type="number"
                                    value={formData.inventory?.reservedQuantity || ''}
                                    onChange={(e) => onNestedInputChange('inventory', 'reservedQuantity', e.target.value)}
                                    placeholder="0"
                                />
                            </div>
                        </div>

                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="reorderLevel">Mức đặt hàng lại</Label>
                                <Input
                                    id="reorderLevel"
                                    type="number"
                                    value={formData.inventory?.reorderLevel || ''}
                                    onChange={(e) => onNestedInputChange('inventory', 'reorderLevel', e.target.value)}
                                    placeholder="5"
                                />
                                <p className="text-xs text-muted-foreground">
                                    Khi tồn kho xuống dưới mức này, hệ thống sẽ cảnh báo
                                </p>
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="maxStockLevel">Mức tồn kho tối đa</Label>
                                <Input
                                    id="maxStockLevel"
                                    type="number"
                                    value={formData.inventory?.maxStockLevel || ''}
                                    onChange={(e) => onNestedInputChange('inventory', 'maxStockLevel', e.target.value)}
                                    placeholder="100"
                                />
                                <p className="text-xs text-muted-foreground">
                                    Mức tồn kho tối đa cho phép trong kho
                                </p>
                            </div>
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="warehouseLocation">Vị trí kho</Label>
                            <Input
                                id="warehouseLocation"
                                value={formData.inventory?.warehouseLocation || ''}
                                onChange={(e) => onNestedInputChange('inventory', 'warehouseLocation', e.target.value)}
                                placeholder="VD: Kho A - Kệ 1 - Tầng 2"
                            />
                            <p className="text-xs text-muted-foreground">
                                Vị trí cụ thể của sản phẩm trong kho
                            </p>
                        </div>

                        {/* Inventory Summary */}
                        <div className="p-4 bg-muted rounded-lg">
                            <h4 className="font-medium mb-2">Tóm tắt tồn kho</h4>
                            <div className="grid grid-cols-2 gap-4 text-sm">
                                <div>
                                    <span className="text-muted-foreground">Tổng tồn kho:</span>
                                    <span className="ml-2 font-medium">
                                        {formData.inventory?.quantityInStock || 0}
                                    </span>
                                </div>
                                <div>
                                    <span className="text-muted-foreground">Đã đặt:</span>
                                    <span className="ml-2 font-medium">
                                        {formData.inventory?.reservedQuantity || 0}
                                    </span>
                                </div>
                                <div>
                                    <span className="text-muted-foreground">Có thể bán:</span>
                                    <span className="ml-2 font-medium text-green-600">
                                        {(parseInt(formData.inventory?.quantityInStock || '0') - parseInt(formData.inventory?.reservedQuantity || '0'))}
                                    </span>
                                </div>
                                <div>
                                    <span className="text-muted-foreground">Trạng thái:</span>
                                    <span className={`ml-2 font-medium ${parseInt(formData.inventory?.quantityInStock || '0') <= parseInt(formData.inventory?.reorderLevel || '0')
                                            ? 'text-red-600'
                                            : 'text-green-600'
                                        }`}>
                                        {parseInt(formData.inventory?.quantityInStock || '0') <= parseInt(formData.inventory?.reorderLevel || '0')
                                            ? 'Cần đặt hàng'
                                            : 'Đủ hàng'
                                        }
                                    </span>
                                </div>
                            </div>
                        </div>
                    </CardContent>
                </CollapsibleContent>
            </Collapsible>
        </Card>
    );
}
