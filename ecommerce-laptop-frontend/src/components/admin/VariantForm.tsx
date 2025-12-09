'use client';

import { Button } from '@/components/ui/button';
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle
} from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue
} from '@/components/ui/select';
import { Textarea } from '@/components/ui/textarea';
import { Product } from '@/types/api';
import { Loader2, Save, X } from 'lucide-react';
import { useState } from 'react';

export interface VariantFormData {
    variantName: string;
    variantSku: string;
    price: string;
    stockQuantity: string;
    description: string;
    isActive: boolean;
    // Laptop-specific variant properties
    ramCapacityGB?: string;
    storageCapacityGB?: string;
    color?: string;
    gpuModel?: string;
}

interface VariantFormProps {
    product: Product;
    variant?: Product;
    onSubmit: (data: VariantFormData) => void;
    onCancel: () => void;
    isLoading?: boolean;
}

export function VariantForm({ product, variant, onSubmit, onCancel, isLoading = false }: VariantFormProps) {
    const [formData, setFormData] = useState<VariantFormData>({
        variantName: variant?.variantName || '',
        variantSku: variant?.variantSku || '',
        price: variant?.price?.toString() || '',
        stockQuantity: variant?.stockQuantity?.toString() || '0',
        description: variant?.description || '',
        isActive: variant?.isActive ?? true,
        ramCapacityGB: variant?.ramCapacityGB?.toString() || '',
        storageCapacityGB: variant?.storageCapacityGB?.toString() || '',
        color: variant?.color || '',
        gpuModel: variant?.gpuModel || ''
    });

    const handleInputChange = (field: keyof VariantFormData, value: string | boolean) => {
        setFormData(prev => ({
            ...prev,
            [field]: value
        }));
    };

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        onSubmit(formData);
    };

    const isFormValid =
        formData.variantName.trim() &&
        formData.variantSku.trim() &&
        formData.price &&
        !isNaN(parseFloat(formData.price)) &&
        parseFloat(formData.price) >= 0 &&
        formData.stockQuantity &&
        !isNaN(parseInt(formData.stockQuantity)) &&
        parseInt(formData.stockQuantity) >= 0;

    return (
        <form onSubmit={handleSubmit} className="space-y-6">
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {/* Basic Information */}
                <Card>
                    <CardHeader>
                        <CardTitle>Basic Information</CardTitle>
                        <CardDescription>
                            Basic variant information and pricing
                        </CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="space-y-2">
                            <Label htmlFor="variantName">Variant Name *</Label>
                            <Input
                                id="variantName"
                                value={formData.variantName}
                                onChange={(e) => handleInputChange('variantName', e.target.value)}
                                placeholder="e.g., 16GB RAM, 512GB SSD"
                                required
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="variantSku">Variant SKU *</Label>
                            <Input
                                id="variantSku"
                                value={formData.variantSku}
                                onChange={(e) => handleInputChange('variantSku', e.target.value)}
                                placeholder="e.g., VAR-1-001"
                                required
                            />
                        </div>

                        <div className="grid grid-cols-2 gap-4">
                            <div className="space-y-2">
                                <Label htmlFor="price">Price *</Label>
                                <Input
                                    id="price"
                                    type="number"
                                    min="0"
                                    step="0.01"
                                    value={formData.price}
                                    onChange={(e) => handleInputChange('price', e.target.value)}
                                    placeholder="0"
                                    required
                                />
                            </div>
                            <div className="space-y-2">
                                <Label htmlFor="stockQuantity">Stock Quantity *</Label>
                                <Input
                                    id="stockQuantity"
                                    type="number"
                                    min="0"
                                    value={formData.stockQuantity}
                                    onChange={(e) => handleInputChange('stockQuantity', e.target.value)}
                                    placeholder="0"
                                    required
                                />
                            </div>
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="description">Description</Label>
                            <Textarea
                                id="description"
                                value={formData.description}
                                onChange={(e) => handleInputChange('description', e.target.value)}
                                placeholder="Variant description..."
                                rows={3}
                            />
                        </div>

                        <div className="flex items-center space-x-2">
                            <input
                                type="checkbox"
                                id="isActive"
                                checked={formData.isActive}
                                onChange={(e) => handleInputChange('isActive', e.target.checked)}
                                className="rounded border-gray-300"
                            />
                            <Label htmlFor="isActive" className="text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70">
                                Active variant
                            </Label>
                        </div>
                    </CardContent>
                </Card>

                {/* Laptop-specific Specifications */}
                {product.productType === 'Laptop' && (
                    <Card>
                        <CardHeader>
                            <CardTitle>Laptop Specifications</CardTitle>
                            <CardDescription>
                                Laptop-specific variant properties
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="grid grid-cols-2 gap-4">
                                <div className="space-y-2">
                                    <Label htmlFor="ramCapacityGB">RAM Capacity (GB)</Label>
                                    <Select
                                        value={formData.ramCapacityGB}
                                        onValueChange={(value) => handleInputChange('ramCapacityGB', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue placeholder="Select RAM" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="8">8 GB</SelectItem>
                                            <SelectItem value="16">16 GB</SelectItem>
                                            <SelectItem value="32">32 GB</SelectItem>
                                            <SelectItem value="64">64 GB</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>

                                <div className="space-y-2">
                                    <Label htmlFor="storageCapacityGB">Storage Capacity (GB)</Label>
                                    <Select
                                        value={formData.storageCapacityGB}
                                        onValueChange={(value) => handleInputChange('storageCapacityGB', value)}
                                    >
                                        <SelectTrigger>
                                            <SelectValue placeholder="Select Storage" />
                                        </SelectTrigger>
                                        <SelectContent>
                                            <SelectItem value="256">256 GB</SelectItem>
                                            <SelectItem value="512">512 GB</SelectItem>
                                            <SelectItem value="1024">1 TB</SelectItem>
                                            <SelectItem value="2048">2 TB</SelectItem>
                                            <SelectItem value="4096">4 TB</SelectItem>
                                        </SelectContent>
                                    </Select>
                                </div>
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="color">Color</Label>
                                <Input
                                    id="color"
                                    value={formData.color}
                                    onChange={(e) => handleInputChange('color', e.target.value)}
                                    placeholder="e.g., Space Gray, Silver, Black"
                                />
                            </div>

                            <div className="space-y-2">
                                <Label htmlFor="gpuModel">GPU Model</Label>
                                <Input
                                    id="gpuModel"
                                    value={formData.gpuModel}
                                    onChange={(e) => handleInputChange('gpuModel', e.target.value)}
                                    placeholder="e.g., NVIDIA RTX 4060, Intel Iris Xe"
                                />
                            </div>
                        </CardContent>
                    </Card>
                )}
            </div>

            {/* Actions */}
            <div className="flex justify-end gap-4">
                <Button
                    type="button"
                    variant="outline"
                    onClick={onCancel}
                    disabled={isLoading}
                >
                    <X className="h-4 w-4 mr-2" />
                    Cancel
                </Button>
                <Button
                    type="submit"
                    disabled={!isFormValid || isLoading}
                >
                    {isLoading ? (
                        <>
                            <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                            {variant ? 'Updating...' : 'Creating...'}
                        </>
                    ) : (
                        <>
                            <Save className="h-4 w-4 mr-2" />
                            {variant ? 'Update Variant' : 'Create Variant'}
                        </>
                    )}
                </Button>
            </div>
        </form>
    );
}
