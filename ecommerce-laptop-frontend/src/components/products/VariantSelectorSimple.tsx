'use client';

import { Badge } from '@/components/ui/badge';
import { Label } from '@/components/ui/label';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
// Removed Select import - using +/- buttons instead
import { Separator } from '@/components/ui/separator';
import { Product } from '@/types/api';
import {
    CheckCircle,
    XCircle
} from 'lucide-react';
import { useEffect, useState } from 'react';

interface VariantSelectorSimpleProps {
    product: Product;
    variants: Product[];
    selectedVariantId?: number;
    onVariantSelect: (variant: Product) => void;
    className?: string;
}

export default function VariantSelectorSimple({
    product,
    variants,
    selectedVariantId,
    onVariantSelect,
    className = ''
}: VariantSelectorSimpleProps) {
    const [selectedVariant, setSelectedVariant] = useState<Product | null>(null);

    // Set initial selected variant
    useEffect(() => {
        if (selectedVariantId) {
            const variant = variants.find(v => v.id === selectedVariantId);
            if (variant) {
                setSelectedVariant(variant);
            }
        }
    }, [selectedVariantId, variants]);

    const handleVariantChange = (variantId: string) => {
        const variant = variants.find(v => v.id.toString() === variantId);
        if (variant) {
            setSelectedVariant(variant);
            onVariantSelect(variant);
        }
    };

    const getPriceDifference = () => {
        if (!selectedVariant || !product) return 0;
        return selectedVariant.price - product.price;
    };

    if (variants.length === 0) {
        return null;
    }

    return (
        <div className={`space-y-4 ${className}`}>
            {/* Variant Selection */}
            <div className="space-y-3">
                <Label className="text-base font-semibold">Chn cu hnh:</Label>
                <RadioGroup
                    value={selectedVariantId?.toString() || ''}
                    onValueChange={handleVariantChange}
                    className="space-y-2"
                >
                    {variants.map((variant) => (
                        <div key={variant.id} className="relative">
                            <RadioGroupItem
                                value={variant.id.toString()}
                                id={`variant-${variant.id}`}
                                className="peer sr-only"
                            />
                            <Label
                                htmlFor={`variant-${variant.id}`}
                                className="flex cursor-pointer items-center justify-between rounded-lg border-2 border-gray-200 p-4 transition-all hover:border-gray-300 hover:bg-gray-50 peer-checked:border-blue-500 peer-checked:bg-blue-50 peer-checked:shadow-sm"
                            >
                                <div className="flex-1">
                                    <div className="flex items-center gap-2">
                                        <div className="font-medium text-gray-900">
                                            {variant.variantName || variant.name}
                                        </div>
                                        {variant.stockQuantity > 0 ? (
                                            <Badge variant="secondary" className="text-xs">
                                                <CheckCircle className="mr-1 h-3 w-3" />
                                                Cn hng
                                            </Badge>
                                        ) : (
                                            <Badge variant="destructive" className="text-xs">
                                                <XCircle className="mr-1 h-3 w-3" />
                                                Ht hng
                                            </Badge>
                                        )}
                                    </div>
                                    <div className="mt-1 text-sm text-gray-600">
                                        {variant.ramCapacityGB && `${variant.ramCapacityGB}GB RAM`}
                                        {variant.ramCapacityGB && variant.storageCapacityGB && '  '}
                                        {variant.storageCapacityGB && `${variant.storageCapacityGB}GB SSD`}
                                        {variant.color && `  ${variant.color}`}
                                    </div>
                                </div>
                                <div className="text-right">
                                    <div className="text-lg font-bold text-gray-900">
                                        ${variant.price.toLocaleString()}
                                    </div>
                                    {variant.price !== product.price && (
                                        <div className={`text-sm ${variant.price > product.price ? 'text-red-600' : 'text-green-600'}`}>
                                            {variant.price > product.price ? '+' : ''}${(variant.price - product.price).toLocaleString()}
                                        </div>
                                    )}
                                </div>
                            </Label>
                        </div>
                    ))}
                </RadioGroup>
            </div>

            {/* Selected Variant Info */}
            {selectedVariant && (
                <div className="space-y-4">
                    <Separator />
                    <div className="rounded-lg border border-blue-200 bg-blue-50 p-4">
                        <div className="flex items-center justify-between">
                            <div>
                                <h4 className="font-medium text-blue-900">
                                    {selectedVariant.variantName || selectedVariant.name}
                                </h4>
                                <p className="text-sm text-blue-700">
                                    {selectedVariant.variantSku}
                                </p>
                            </div>
                            <div className="text-right">
                                <div className="text-lg font-bold text-blue-900">
                                    ${selectedVariant.price.toLocaleString()}
                                </div>
                                {selectedVariant.price !== product.price && (
                                    <div className={`text-sm ${selectedVariant.price > product.price ? 'text-red-600' : 'text-green-600'}`}>
                                        {selectedVariant.price > product.price ? '+' : ''}${(selectedVariant.price - product.price).toLocaleString()}
                                    </div>
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}
