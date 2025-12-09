'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { ProductFormData } from '@/lib/admin-api';
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
                                <CardTitle>Thng s combo</CardTitle>
                                <CardDescription>
                                    Nhp thng tin chi tit cho combo sn phm
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
                                <Label htmlFor="bundleType">Loi combo</Label>
                                <Input
                                    id="bundleType"
                                    value={formData.bundleType || ''}
                                    onChange={(e) => onInputChange('bundleType', e.target.value)}
                                    placeholder="VD: Combo Gaming, Combo Vn phng"
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="discountPercentage">Phn trm gim gi</Label>
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
                                <Label htmlFor="validFrom">C hiu lc t</Label>
                                <Input
                                    id="validFrom"
                                    type="date"
                                    value={formData.validFrom || ''}
                                    onChange={(e) => onInputChange('validFrom', e.target.value)}
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="validTo">C hiu lc n</Label>
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
                                <Label>Sn phm trong combo</Label>
                                <Button type="button" onClick={addBundleItem} size="sm">
                                    <Plus className="h-4 w-4 mr-2" />
                                    Thm sn phm
                                </Button>
                            </div>

                            {formData.bundleItems?.map((item, index) => (
                                <div key={index} className="p-4 border rounded-lg space-y-3">
                                    <div className="flex items-center justify-between">
                                        <span className="font-medium">Sn phm {index + 1}</span>
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
                                            <Label>Sn phm</Label>
                                            <select
                                                value={item.productId}
                                                onChange={(e) => updateBundleItem(index, 'productId', e.target.value)}
                                                className="w-full p-2 border rounded-md"
                                            >
                                                <option value="">Chn sn phm</option>
                                                {products?.map((product) => (
                                                    <option key={product.id} value={product.id.toString()}>
                                                        {product.name}
                                                    </option>
                                                ))}
                                            </select>
                                        </div>
                                        <div className="space-y-2">
                                            <Label>S lng</Label>
                                            <Input
                                                type="number"
                                                value={item.quantity}
                                                onChange={(e) => updateBundleItem(index, 'quantity', e.target.value)}
                                                placeholder="1"
                                                min="1"
                                            />
                                        </div>
                                        <div className="space-y-2">
                                            <Label>Gim gi (%)</Label>
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
                                    <p>Cha c sn phm no trong combo</p>
                                    <p className="text-sm">Nhn "Thm sn phm"  bt u</p>
                                </div>
                            )}
                        </div>
                    </CardContent>
                </CollapsibleContent>
            </Collapsible>
        </Card>
    );
}

