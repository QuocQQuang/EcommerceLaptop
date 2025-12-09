'use client';

import { Badge } from '@/components/ui/badge';
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
import { Product } from '@/types/api';
import { Loader2, Package, Plus, Trash2, X } from 'lucide-react';
import { useState } from 'react';

export interface BundleItem {
    productId: number;
    product: Product;
    quantity: number;
    isRequired: boolean;
    variantId?: number;
    variantName?: string;
}

export interface BundleConfiguration {
    name: string;
    description: string;
    basePrice: number;
    discountPercentage: number;
    finalPrice: number;
    items: BundleItem[];
}

interface BundleBuilderProps {
    products: Product[];
    bundle?: BundleConfiguration;
    onSubmit: (bundle: BundleConfiguration) => void;
    onCancel: () => void;
    isLoading?: boolean;
}

export function BundleBuilder({ products, bundle, onSubmit, onCancel, isLoading = false }: BundleBuilderProps) {
    const [bundleConfig, setBundleConfig] = useState<BundleConfiguration>(
        bundle || {
            name: '',
            description: '',
            basePrice: 0,
            discountPercentage: 0,
            finalPrice: 0,
            items: []
        }
    );

    const [selectedProductId, setSelectedProductId] = useState<string>('');
    const [selectedQuantity, setSelectedQuantity] = useState<number>(1);
    const [selectedVariantId, setSelectedVariantId] = useState<string>('');

    const handleInputChange = (field: keyof BundleConfiguration, value: any) => {
        setBundleConfig(prev => {
            const updated = { ...prev, [field]: value };

            // Recalculate final price when base price or discount changes
            if (field === 'basePrice' || field === 'discountPercentage') {
                updated.finalPrice = updated.basePrice * (1 - updated.discountPercentage / 100);
            }

            return updated;
        });
    };

    const addProductToBundle = () => {
        if (!selectedProductId) return;

        const product = products.find(p => p.id === parseInt(selectedProductId));
        if (!product) return;

        // Check if product already exists in bundle
        const existingItem = bundleConfig.items.find(item =>
            item.productId === product.id &&
            item.variantId === (selectedVariantId ? parseInt(selectedVariantId) : undefined)
        );

        if (existingItem) {
            // Update quantity
            setBundleConfig(prev => ({
                ...prev,
                items: prev.items.map(item =>
                    item.productId === product.id && item.variantId === (selectedVariantId ? parseInt(selectedVariantId) : undefined)
                        ? { ...item, quantity: item.quantity + selectedQuantity }
                        : item
                )
            }));
        } else {
            // Add new item
            const newItem: BundleItem = {
                productId: product.id,
                product,
                quantity: selectedQuantity,
                isRequired: true,
                variantId: selectedVariantId ? parseInt(selectedVariantId) : undefined,
                variantName: selectedVariantId ?
                    product.variants?.find(v => v.id === parseInt(selectedVariantId))?.variantName :
                    undefined
            };

            setBundleConfig(prev => ({
                ...prev,
                items: [...prev.items, newItem]
            }));
        }

        // Reset selection
        setSelectedProductId('');
        setSelectedQuantity(1);
        setSelectedVariantId('');
    };

    const removeItemFromBundle = (productId: number, variantId?: number) => {
        setBundleConfig(prev => ({
            ...prev,
            items: prev.items.filter(item =>
                !(item.productId === productId && item.variantId === variantId)
            )
        }));
    };

    const updateItemQuantity = (productId: number, variantId: number | undefined, quantity: number) => {
        setBundleConfig(prev => ({
            ...prev,
            items: prev.items.map(item =>
                item.productId === productId && item.variantId === variantId
                    ? { ...item, quantity: Math.max(1, quantity) }
                    : item
            )
        }));
    };

    const toggleItemRequired = (productId: number, variantId: number | undefined) => {
        setBundleConfig(prev => ({
            ...prev,
            items: prev.items.map(item =>
                item.productId === productId && item.variantId === variantId
                    ? { ...item, isRequired: !item.isRequired }
                    : item
            )
        }));
    };

    const calculateBundlePrice = () => {
        const totalPrice = bundleConfig.items.reduce((sum, item) => {
            const itemPrice = item.variantId
                ? item.product.variants?.find(v => v.id === item.variantId)?.price || item.product.price
                : item.product.price;
            return sum + (itemPrice * item.quantity);
        }, 0);

        setBundleConfig(prev => ({
            ...prev,
            basePrice: totalPrice,
            finalPrice: totalPrice * (1 - prev.discountPercentage / 100)
        }));
    };

    const isFormValid =
        bundleConfig.name.trim() &&
        bundleConfig.items.length > 0 &&
        bundleConfig.basePrice > 0;

    return (
        <div className="space-y-6">
            {/* Bundle Configuration */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <Package className="h-5 w-5" />
                        Bundle Configuration
                    </CardTitle>
                    <CardDescription>
                        Configure your product bundle
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        <div className="space-y-2">
                            <Label htmlFor="bundleName">Bundle Name *</Label>
                            <Input
                                id="bundleName"
                                value={bundleConfig.name}
                                onChange={(e) => handleInputChange('name', e.target.value)}
                                placeholder="e.g., Gaming Laptop Bundle"
                                required
                            />
                        </div>
                        <div className="space-y-2">
                            <Label htmlFor="discountPercentage">Discount Percentage</Label>
                            <Input
                                id="discountPercentage"
                                type="number"
                                min="0"
                                max="100"
                                step="1"
                                value={bundleConfig.discountPercentage}
                                onChange={(e) => handleInputChange('discountPercentage', parseFloat(e.target.value) || 0)}
                                placeholder="0"
                            />
                        </div>
                    </div>

                    <div className="space-y-2">
                        <Label htmlFor="bundleDescription">Description</Label>
                        <Input
                            id="bundleDescription"
                            value={bundleConfig.description}
                            onChange={(e) => handleInputChange('description', e.target.value)}
                            placeholder="Bundle description..."
                        />
                    </div>

                    <div className="grid grid-cols-3 gap-4 p-4 bg-muted rounded-lg">
                        <div>
                            <Label className="text-sm font-medium text-muted-foreground">Base Price</Label>
                            <p className="text-lg font-semibold">
                                {new Intl.NumberFormat('vi-VN', {
                                    style: 'currency',
                                    currency: 'VND'
                                }).format(bundleConfig.basePrice)}
                            </p>
                        </div>
                        <div>
                            <Label className="text-sm font-medium text-muted-foreground">Discount</Label>
                            <p className="text-lg font-semibold text-green-600">
                                -{bundleConfig.discountPercentage}%
                            </p>
                        </div>
                        <div>
                            <Label className="text-sm font-medium text-muted-foreground">Final Price</Label>
                            <p className="text-lg font-semibold text-blue-600">
                                {new Intl.NumberFormat('vi-VN', {
                                    style: 'currency',
                                    currency: 'VND'
                                }).format(bundleConfig.finalPrice)}
                            </p>
                        </div>
                    </div>
                </CardContent>
            </Card>

            {/* Add Products */}
            <Card>
                <CardHeader>
                    <CardTitle>Add Products to Bundle</CardTitle>
                    <CardDescription>
                        Select products and variants to include in this bundle
                    </CardDescription>
                </CardHeader>
                <CardContent className="space-y-4">
                    <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                        <div className="space-y-2">
                            <Label htmlFor="productSelect">Product</Label>
                            <Select value={selectedProductId} onValueChange={setSelectedProductId}>
                                <SelectTrigger>
                                    <SelectValue placeholder="Select product" />
                                </SelectTrigger>
                                <SelectContent>
                                    {products.map((product) => (
                                        <SelectItem key={product.id} value={product.id.toString()}>
                                            {product.name}
                                        </SelectItem>
                                    ))}
                                </SelectContent>
                            </Select>
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="variantSelect">Variant (Optional)</Label>
                            <Select value={selectedVariantId} onValueChange={setSelectedVariantId}>
                                <SelectTrigger>
                                    <SelectValue placeholder="Select variant" />
                                </SelectTrigger>
                                <SelectContent>
                                    <SelectItem value="">No variant</SelectItem>
                                    {selectedProductId && products
                                        .find(p => p.id === parseInt(selectedProductId))
                                        ?.variants?.map((variant) => (
                                            <SelectItem key={variant.id} value={variant.id.toString()}>
                                                {variant.variantName}
                                            </SelectItem>
                                        ))}
                                </SelectContent>
                            </Select>
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="quantity">Quantity</Label>
                            <Input
                                id="quantity"
                                type="number"
                                min="1"
                                value={selectedQuantity}
                                onChange={(e) => setSelectedQuantity(parseInt(e.target.value) || 1)}
                            />
                        </div>

                        <div className="space-y-2">
                            <Label>&nbsp;</Label>
                            <Button onClick={addProductToBundle} className="w-full">
                                <Plus className="h-4 w-4 mr-2" />
                                Add to Bundle
                            </Button>
                        </div>
                    </div>
                </CardContent>
            </Card>

            {/* Bundle Items */}
            <Card>
                <CardHeader>
                    <div className="flex items-center justify-between">
                        <div>
                            <CardTitle>Bundle Items ({bundleConfig.items.length})</CardTitle>
                            <CardDescription>
                                Products included in this bundle
                            </CardDescription>
                        </div>
                        <Button onClick={calculateBundlePrice} variant="outline">
                            Recalculate Price
                        </Button>
                    </div>
                </CardHeader>
                <CardContent>
                    {bundleConfig.items.length === 0 ? (
                        <div className="text-center py-8">
                            <Package className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                            <h3 className="text-lg font-semibold mb-2">No items in bundle</h3>
                            <p className="text-muted-foreground">
                                Add products to create your bundle
                            </p>
                        </div>
                    ) : (
                        <div className="space-y-4">
                            {bundleConfig.items.map((item, index) => (
                                <div key={`${item.productId}-${item.variantId || 'base'}`} className="flex items-center justify-between p-4 border rounded-lg">
                                    <div className="flex-1">
                                        <div className="flex items-center gap-2">
                                            <h4 className="font-medium">{item.product.name}</h4>
                                            {item.variantId && (
                                                <Badge variant="secondary">{item.variantName}</Badge>
                                            )}
                                            {item.isRequired && (
                                                <Badge variant="destructive">Required</Badge>
                                            )}
                                        </div>
                                        <p className="text-sm text-muted-foreground">
                                            {item.product.sku}  Qty: {item.quantity}
                                        </p>
                                    </div>

                                    <div className="flex items-center gap-4">
                                        <div className="flex items-center gap-2">
                                            <Button
                                                variant="outline"
                                                size="sm"
                                                onClick={() => updateItemQuantity(item.productId, item.variantId, item.quantity - 1)}
                                                disabled={item.quantity <= 1}
                                            >
                                                -
                                            </Button>
                                            <span className="w-8 text-center">{item.quantity}</span>
                                            <Button
                                                variant="outline"
                                                size="sm"
                                                onClick={() => updateItemQuantity(item.productId, item.variantId, item.quantity + 1)}
                                            >
                                                +
                                            </Button>
                                        </div>

                                        <Button
                                            variant="outline"
                                            size="sm"
                                            onClick={() => toggleItemRequired(item.productId, item.variantId)}
                                        >
                                            {item.isRequired ? 'Required' : 'Optional'}
                                        </Button>

                                        <Button
                                            variant="ghost"
                                            size="sm"
                                            onClick={() => removeItemFromBundle(item.productId, item.variantId)}
                                            className="text-red-600 hover:text-red-700"
                                        >
                                            <Trash2 className="h-4 w-4" />
                                        </Button>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>

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
                    onClick={() => onSubmit(bundleConfig)}
                    disabled={!isFormValid || isLoading}
                >
                    {isLoading ? (
                        <>
                            <Loader2 className="h-4 w-4 mr-2 animate-spin" />
                            {bundle ? 'Updating...' : 'Creating...'}
                        </>
                    ) : (
                        <>
                            <Package className="h-4 w-4 mr-2" />
                            {bundle ? 'Update Bundle' : 'Create Bundle'}
                        </>
                    )}
                </Button>
            </div>
        </div>
    );
}
