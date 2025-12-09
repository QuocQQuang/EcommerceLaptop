'use client';

import { Card, CardContent } from '@/components/ui/card';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { formatCurrencyPrice } from '@/lib/currency';
import { Package } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';

interface OrderItem {
    id: number;
    productId: number;
    productName: string;
    quantity: number;
    unitPrice: number;
    totalPrice: number;
    productImage?: string;
}

interface OrderItemCardProps {
    item: OrderItem;
    className?: string;
}

export function OrderItemCard({ item, className }: OrderItemCardProps) {
    const { selectedCurrency } = useCurrencyContext();

    return (
        <Card className={`${className}`}>
            <CardContent className="p-4">
                <div className="flex items-center space-x-4">
                    {/* Product Image */}
                    <div className="flex-shrink-0">
                        <div className="w-16 h-16 bg-gray-100 rounded-lg flex items-center justify-center overflow-hidden">
                            {item.productImage ? (
                                <Image
                                    src={item.productImage}
                                    alt={item.productName}
                                    width={64}
                                    height={64}
                                    className="w-full h-full object-cover"
                                />
                            ) : (
                                <Package className="h-8 w-8 text-gray-400" />
                            )}
                        </div>
                    </div>

                    {/* Product Details */}
                    <div className="flex-1 min-w-0">
                        <Link
                            href={`/products/${item.productId}`}
                            className="hover:text-blue-600 transition-colors"
                        >
                            <h3 className="font-medium text-gray-900 truncate">
                                {item.productName}
                            </h3>
                        </Link>
                        <p className="text-sm text-gray-600 mt-1">
                            S lng: {item.quantity}
                        </p>
                        <p className="text-sm text-gray-500">
                            n gi: {formatCurrencyPrice(item.unitPrice, selectedCurrency)}
                        </p>
                    </div>

                    {/* Price */}
                    <div className="flex-shrink-0 text-right">
                        <p className="font-medium text-gray-900">
                            {formatCurrencyPrice(item.totalPrice, selectedCurrency)}
                        </p>
                        <p className="text-sm text-gray-500">
                            {item.quantity}  {formatCurrencyPrice(item.unitPrice, selectedCurrency)}
                        </p>
                    </div>
                </div>
            </CardContent>
        </Card>
    );
}
