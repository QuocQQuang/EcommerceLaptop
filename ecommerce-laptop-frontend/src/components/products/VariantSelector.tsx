'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Label } from '@/components/ui/label';
import { RadioGroup, RadioGroupItem } from '@/components/ui/radio-group';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Product } from '@/types/api';
import {
    CheckCircle,
    Cpu,
    HardDrive,
    Info,
    Palette,
    XCircle,
    Zap
} from 'lucide-react';
import { useEffect, useState } from 'react';

interface VariantSelectorProps {
    product: Product;
    variants: Product[];
    selectedVariantId?: number;
    onVariantSelect: (variant: Product) => void;
    onAddToCart: (variant: Product, quantity: number) => void;
    className?: string;
}

interface VariantGroup {
    key: string;
    label: string;
    options: { value: string; label: string; variants: Product[] }[];
}

export default function VariantSelector({
    product,
    variants,
    selectedVariantId,
    onVariantSelect,
    onAddToCart,
    className = ''
}: VariantSelectorProps) {
    const [selectedVariant, setSelectedVariant] = useState<Product | null>(null);
    const [quantity, setQuantity] = useState(1);
    const [variantGroups, setVariantGroups] = useState<VariantGroup[]>([]);
    const [selectedOptions, setSelectedOptions] = useState<Record<string, string>>({});

    // Group variants by differentiators
    useEffect(() => {
        if (variants.length === 0) return;

        const groups: VariantGroup[] = [];

        // RAM Capacity group
        const ramVariants = variants.filter(v => v.ramCapacityGB !== product.ramCapacityGB);
        if (ramVariants.length > 0) {
            const ramOptions = Array.from(new Set(ramVariants.map(v => v.ramCapacityGB)))
                .filter(Boolean)
                .sort((a, b) => (a || 0) - (b || 0))
                .map(ram => ({
                    value: ram?.toString() || '',
                    label: `${ram}GB RAM`,
                    variants: variants.filter(v => v.ramCapacityGB === ram)
                }));

            groups.push({
                key: 'ram',
                label: 'RAM',
                options: ramOptions
            });
        }

        // Storage Capacity group
        const storageVariants = variants.filter(v => v.storageCapacityGB !== product.storageCapacityGB);
        if (storageVariants.length > 0) {
            const storageOptions = Array.from(new Set(storageVariants.map(v => v.storageCapacityGB)))
                .filter(Boolean)
                .sort((a, b) => (a || 0) - (b || 0))
                .map(storage => ({
                    value: storage?.toString() || '',
                    label: `${storage}GB SSD`,
                    variants: variants.filter(v => v.storageCapacityGB === storage)
                }));

            groups.push({
                key: 'storage',
                label: 'Storage',
                options: storageOptions
            });
        }

        // Color group
        const colorVariants = variants.filter(v => v.color !== product.color);
        if (colorVariants.length > 0) {
            const colorOptions = Array.from(new Set(colorVariants.map(v => v.color)))
                .filter(Boolean)
                .map(color => ({
                    value: color || '',
                    label: color || '',
                    variants: variants.filter(v => v.color === color)
                }));

            groups.push({
                key: 'color',
                label: 'Color',
                options: colorOptions
            });
        }

        // GPU Model group
        const gpuVariants = variants.filter(v => v.gpuModel !== product.gpuModel);
        if (gpuVariants.length > 0) {
            const gpuOptions = Array.from(new Set(gpuVariants.map(v => v.gpuModel)))
                .filter(Boolean)
                .map(gpu => ({
                    value: gpu || '',
                    label: gpu || '',
                    variants: variants.filter(v => v.gpuModel === gpu)
                }));

            groups.push({
                key: 'gpu',
                label: 'GPU',
                options: gpuOptions
            });
        }

        setVariantGroups(groups);
    }, [variants, product]);

    // Find matching variant based on selected options
    useEffect(() => {
        if (Object.keys(selectedOptions).length === 0) {
            setSelectedVariant(null);
            return;
        }

        const matchingVariant = variants.find(variant => {
            return Object.entries(selectedOptions).every(([key, value]) => {
                switch (key) {
                    case 'ram':
                        return variant.ramCapacityGB?.toString() === value;
                    case 'storage':
                        return variant.storageCapacityGB?.toString() === value;
                    case 'color':
                        return variant.color === value;
                    case 'gpu':
                        return variant.gpuModel === value;
                    default:
                        return true;
                }
            });
        });

        setSelectedVariant(matchingVariant || null);
    }, [selectedOptions, variants]);

    // Set initial selected variant
    useEffect(() => {
        if (selectedVariantId && variants.length > 0) {
            const variant = variants.find(v => v.id === selectedVariantId);
            if (variant) {
                setSelectedVariant(variant);
                onVariantSelect(variant);
            }
        }
    }, [selectedVariantId, variants, onVariantSelect]);

    const handleOptionChange = (groupKey: string, value: string) => {
        setSelectedOptions(prev => ({
            ...prev,
            [groupKey]: value
        }));
    };

    const handleVariantSelect = (variant: Product) => {
        setSelectedVariant(variant);
        onVariantSelect(variant);
    };

    const handleAddToCart = () => {
        if (selectedVariant) {
            onAddToCart(selectedVariant, quantity);
        }
    };

    const getVariantIcon = (key: string) => {
        switch (key) {
            case 'ram':
                return <Zap className="h-4 w-4" />;
            case 'storage':
                return <HardDrive className="h-4 w-4" />;
            case 'color':
                return <Palette className="h-4 w-4" />;
            case 'gpu':
                return <Cpu className="h-4 w-4" />;
            default:
                return <Info className="h-4 w-4" />;
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
        <Card className={`w-full ${className}`}>
            <CardHeader>
                <CardTitle className="flex items-center gap-2">
                    <Info className="h-5 w-5" />
                    Choose Configuration
                </CardTitle>
                <p className="text-sm text-muted-foreground">
                    Select your preferred specifications
                </p>
            </CardHeader>
            <CardContent className="space-y-6">
                {/* Variant Groups */}
                {variantGroups.map((group) => (
                    <div key={group.key} className="space-y-3">
                        <Label className="flex items-center gap-2 text-sm font-medium">
                            {getVariantIcon(group.key)}
                            {group.label}
                        </Label>
                        <RadioGroup
                            value={selectedOptions[group.key] || ''}
                            onValueChange={(value) => handleOptionChange(group.key, value)}
                            className="grid grid-cols-2 gap-3"
                        >
                            {group.options.map((option) => (
                                <div key={option.value} className="flex items-center space-x-2">
                                    <RadioGroupItem
                                        value={option.value}
                                        id={`${group.key}-${option.value}`}
                                        className="peer"
                                    />
                                    <Label
                                        htmlFor={`${group.key}-${option.value}`}
                                        className="flex-1 cursor-pointer rounded-md border border-input bg-background px-3 py-2 text-sm font-medium ring-offset-background hover:bg-accent hover:text-accent-foreground peer-data-[state=checked]:border-primary peer-data-[state=checked]:bg-primary peer-data-[state=checked]:text-primary-foreground"
                                    >
                                        {option.label}
                                    </Label>
                                </div>
                            ))}
                        </RadioGroup>
                    </div>
                ))}

                {/* Selected Variant Display */}
                {selectedVariant && (
                    <>
                        <Separator />
                        <div className="space-y-4">
                            <div className="flex items-center justify-between">
                                <h4 className="font-medium">Selected Configuration</h4>
                                <Badge variant="secondary" className="flex items-center gap-1">
                                    <CheckCircle className="h-3 w-3" />
                                    Available
                                </Badge>
                            </div>

                            <div className="space-y-2">
                                <div className="flex justify-between text-sm">
                                    <span className="text-muted-foreground">Configuration:</span>
                                    <span className="font-medium">{selectedVariant.variantName}</span>
                                </div>
                                <div className="flex justify-between text-sm">
                                    <span className="text-muted-foreground">SKU:</span>
                                    <span className="font-mono text-xs">{selectedVariant.variantSku}</span>
                                </div>
                                <div className="flex justify-between text-sm">
                                    <span className="text-muted-foreground">Stock:</span>
                                    <span className="font-medium">
                                        {selectedVariant.stockQuantity > 0 ? (
                                            <span className="text-green-600">{selectedVariant.stockQuantity} available</span>
                                        ) : (
                                            <span className="text-red-600 flex items-center gap-1">
                                                <XCircle className="h-3 w-3" />
                                                Out of stock
                                            </span>
                                        )}
                                    </span>
                                </div>
                                <div className="flex justify-between text-lg font-semibold">
                                    <span>Price:</span>
                                    <div className="flex items-center gap-2">
                                        <span>${selectedVariant.price.toLocaleString()}</span>
                                        {getPriceDifference() !== 0 && (
                                            <Badge variant={getPriceDifference() > 0 ? "destructive" : "default"}>
                                                {getPriceDifference() > 0 ? '+' : ''}${getPriceDifference().toLocaleString()}
                                            </Badge>
                                        )}
                                    </div>
                                </div>
                            </div>
                        </div>
                    </>
                )}

                {/* Quantity and Add to Cart */}
                {selectedVariant && (
                    <>
                        <Separator />
                        <div className="space-y-4">
                            <div className="flex items-center justify-between">
                                <Label htmlFor="quantity">Quantity</Label>
                                <Select
                                    value={quantity.toString()}
                                    onValueChange={(value) => setQuantity(parseInt(value))}
                                >
                                    <SelectTrigger className="w-20">
                                        <SelectValue />
                                    </SelectTrigger>
                                    <SelectContent>
                                        {Array.from({ length: Math.min(10, selectedVariant.stockQuantity) }, (_, i) => i + 1).map(num => (
                                            <SelectItem key={num} value={num.toString()}>
                                                {num}
                                            </SelectItem>
                                        ))}
                                    </SelectContent>
                                </Select>
                            </div>

                            <Button
                                onClick={handleAddToCart}
                                disabled={selectedVariant.stockQuantity === 0}
                                className="w-full"
                                size="lg"
                            >
                                {selectedVariant.stockQuantity === 0 ? 'Out of Stock' : 'Add to Cart'}
                            </Button>
                        </div>
                    </>
                )}

                {/* Quick Variant Selection */}
                {variants.length <= 6 && (
                    <>
                        <Separator />
                        <div className="space-y-3">
                            <Label className="text-sm font-medium">Quick Select</Label>
                            <div className="grid grid-cols-1 gap-2">
                                {variants.map((variant) => (
                                    <Button
                                        key={variant.id}
                                        variant={selectedVariant?.id === variant.id ? "default" : "outline"}
                                        size="sm"
                                        onClick={() => handleVariantSelect(variant)}
                                        className="justify-start h-auto p-3"
                                    >
                                        <div className="flex flex-col items-start w-full">
                                            <div className="flex justify-between w-full">
                                                <span className="font-medium">{variant.variantName}</span>
                                                <span className="text-sm">${variant.price.toLocaleString()}</span>
                                            </div>
                                            <div className="flex gap-2 text-xs text-muted-foreground mt-1">
                                                {variant.ramCapacityGB && (
                                                    <span>{variant.ramCapacityGB}GB RAM</span>
                                                )}
                                                {variant.storageCapacityGB && (
                                                    <span>{variant.storageCapacityGB}GB SSD</span>
                                                )}
                                                {variant.color && (
                                                    <span>{variant.color}</span>
                                                )}
                                            </div>
                                        </div>
                                    </Button>
                                ))}
                            </div>
                        </div>
                    </>
                )}
            </CardContent>
        </Card>
    );
}
