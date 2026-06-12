'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { formatCurrencyPrice } from '@/lib/currency';
import { BundleItem, Product } from '@/types/api';
import { Eye, Package, Percent, ShoppingCart } from 'lucide-react';
import Image from 'next/image';
import { useState } from 'react';

interface BundleDisplayProps {
    product: Product;
}

export function BundleDisplay({ product }: BundleDisplayProps) {
    const { selectedCurrency } = useCurrencyContext();
    const [selectedAccessory, setSelectedAccessory] = useState<BundleItem | null>(null);

    if (!product.bundleItems || product.bundleItems.length === 0) {
        return null;
    }

    // Tch laptop chnh v accessories
    const mainLaptop = product.bundleItems.find(item =>
        item.productName.toLowerCase().includes('laptop') ||
        item.productName.toLowerCase().includes('xps') ||
        item.productName.toLowerCase().includes('gaming')
    );

    const accessories = product.bundleItems.filter(item => item !== mainLaptop);

    const formatPrice = (price: number) => {
        return formatCurrencyPrice(price, selectedCurrency);
    };

    const calculateTotalSavings = () => {
        return product.bundleItems?.reduce((total, item) => {
            const originalTotal = item.originalPrice * item.quantity;
            const discountedTotal = item.discountedPrice * item.quantity;
            return total + (originalTotal - discountedTotal);
        }, 0) || 0;
    };

    return (
        <div className="space-y-6">
            {/* Bundle Header */}
            <div className="bg-gradient-to-r from-blue-50 to-purple-50 p-6 rounded-lg border">
                <div className="flex items-center gap-3 mb-4">
                    <Package className="h-6 w-6 text-blue-600" />
                    <h3 className="text-xl font-semibold text-gray-900">Bundle Package</h3>
                    {product.discountPercentage && (
                        <Badge variant="destructive" className="ml-auto">
                            <Percent className="h-3 w-3 mr-1" />
                            {product.discountPercentage}% OFF
                        </Badge>
                    )}
                </div>
                <p className="text-gray-600 mb-4">{product.description}</p>

                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-4">
                        <div className="text-2xl font-bold text-gray-900">
                            {formatPrice(product.price)}
                        </div>
                        {calculateTotalSavings() > 0 && (
                            <div className="text-sm text-green-600">
                                Tiết kiệm: {formatPrice(calculateTotalSavings())}
                            </div>
                        )}
                    </div>
                    <Button size="lg" className="bg-blue-600 hover:bg-blue-700">
                        <ShoppingCart className="h-4 w-4 mr-2" />
                        Thêm Bundle vào giỏ
                    </Button>
                </div>
            </div>

            {/* Main Laptop Display */}
            {mainLaptop && (
                <div className="bg-white border rounded-lg p-6">
                    <h4 className="text-lg font-semibold mb-4 text-gray-900">
                        Sản phẩm chính: {mainLaptop.productName}
                    </h4>
                    <div className="flex items-center gap-4">
                        {mainLaptop.productImageUrl && (
                            <div className="w-20 h-20 relative rounded-lg overflow-hidden bg-gray-100">
                                <Image
                                    src={mainLaptop.productImageUrl}
                                    alt={mainLaptop.productName}
                                    fill
                                    className="object-cover"
                                />
                            </div>
                        )}
                        <div className="flex-1">
                            <div className="flex items-center gap-2 mb-2">
                                <span className="font-medium">{mainLaptop.brand}</span>
                                <span className="text-sm text-gray-500">x{mainLaptop.quantity}</span>
                            </div>
                            <div className="flex items-center gap-2">
                                <span className="text-lg font-semibold text-gray-900">
                                    {formatPrice(mainLaptop.discountedPrice)}
                                </span>
                                {mainLaptop.discountPercentage > 0 && (
                                    <span className="text-sm text-gray-500 line-through">
                                        {formatPrice(mainLaptop.originalPrice)}
                                    </span>
                                )}
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {/* Accessories Section */}
            {accessories.length > 0 && (
                <div className="space-y-4">
                    <h4 className="text-lg font-semibold text-gray-900">
                        Sản phẩm phụ kèm theo ({accessories.length})
                    </h4>

                    {/* Accessory Images Grid */}
                    <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
                        {accessories.map((accessory) => (
                            <Card
                                key={accessory.id}
                                className="cursor-pointer hover:shadow-md transition-shadow"
                            >
                                <CardContent className="p-4">
                                    <div className="space-y-3">
                                        {/* Accessory Image */}
                                        {accessory.productImageUrl && (
                                            <div className="aspect-square relative rounded-lg overflow-hidden bg-gray-100">
                                                <Image
                                                    src={accessory.productImageUrl}
                                                    alt={accessory.productName}
                                                    fill
                                                    className="object-cover"
                                                />
                                            </div>
                                        )}

                                        {/* Accessory Info */}
                                        <div className="space-y-2">
                                            <h5 className="font-medium text-sm text-gray-900 line-clamp-2">
                                                {accessory.productName}
                                            </h5>
                                            <div className="flex items-center gap-1">
                                                <span className="text-xs text-gray-500">x{accessory.quantity}</span>
                                            </div>
                                            <div className="flex items-center gap-2">
                                                <span className="font-semibold text-sm text-gray-900">
                                                    {formatPrice(accessory.discountedPrice)}
                                                </span>
                                                {accessory.discountPercentage > 0 && (
                                                    <span className="text-xs text-gray-500 line-through">
                                                        {formatPrice(accessory.originalPrice)}
                                                    </span>
                                                )}
                                            </div>

                                            {/* View Details Button */}
                                            <Dialog>
                                                <DialogTrigger asChild>
                                                    <Button
                                                        variant="outline"
                                                        size="sm"
                                                        className="w-full"
                                                        onClick={() => setSelectedAccessory(accessory)}
                                                    >
                                                        <Eye className="h-3 w-3 mr-1" />
                                                        Chi tit
                                                    </Button>
                                                </DialogTrigger>
                                                <DialogContent className="max-w-md">
                                                    <DialogHeader>
                                                        <DialogTitle>{accessory.productName}</DialogTitle>
                                                    </DialogHeader>
                                                    <div className="space-y-4">
                                                        {accessory.productImageUrl && (
                                                            <div className="aspect-square relative rounded-lg overflow-hidden bg-gray-100">
                                                                <Image
                                                                    src={accessory.productImageUrl}
                                                                    alt={accessory.productName}
                                                                    fill
                                                                    className="object-cover"
                                                                />
                                                            </div>
                                                        )}
                                                        <div className="space-y-2">
                                                            <div className="flex justify-between">
                                                                <span className="text-sm text-gray-600">Thương hiệu:</span>
                                                                <span className="text-sm font-medium">{accessory.brand}</span>
                                                            </div>
                                                            <div className="flex justify-between">
                                                                <span className="text-sm text-gray-600">SKU:</span>
                                                                <span className="text-sm font-medium">{accessory.productSku}</span>
                                                            </div>
                                                            <div className="flex justify-between">
                                                                <span className="text-sm text-gray-600">S lng:</span>
                                                                <span className="text-sm font-medium">{accessory.quantity}</span>
                                                            </div>
                                                            <div className="flex justify-between">
                                                                <span className="text-sm text-gray-600">Gi gc:</span>
                                                                <span className="text-sm line-through text-gray-500">
                                                                    {formatPrice(accessory.originalPrice)}
                                                                </span>
                                                            </div>
                                                            <div className="flex justify-between">
                                                                <span className="text-sm text-gray-600">Gi sau gim:</span>
                                                                <span className="text-sm font-semibold text-green-600">
                                                                    {formatPrice(accessory.discountedPrice)}
                                                                </span>
                                                            </div>
                                                            {accessory.discountPercentage > 0 && (
                                                                <div className="flex justify-between">
                                                                    <span className="text-sm text-gray-600">Gim gi:</span>
                                                                    <span className="text-sm font-semibold text-red-600">
                                                                        {accessory.discountPercentage}%
                                                                    </span>
                                                                </div>
                                                            )}
                                                            <div className="flex justify-between border-t pt-2">
                                                                <span className="text-sm font-semibold">Tng cng:</span>
                                                                <span className="text-sm font-bold text-blue-600">
                                                                    {formatPrice(accessory.totalPrice)}
                                                                </span>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </DialogContent>
                                            </Dialog>
                                        </div>
                                    </div>
                                </CardContent>
                            </Card>
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
}



