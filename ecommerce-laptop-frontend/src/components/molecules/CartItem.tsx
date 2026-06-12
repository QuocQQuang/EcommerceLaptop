'use client';

import { Image } from '@/components/atoms/Image';
import { Price } from '@/components/atoms/Price';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useCart } from '@/hooks/useCart';
import { cn } from '@/lib/utils';
import { useCartStore } from '@/store/cartStore';
import { Minus, Plus, X } from 'lucide-react';
import Link from 'next/link';

import type { CartItem as CartItemType } from '@/types/api';

interface CartItemWithVariant extends CartItemType {
    variant?: string;
}

interface CartItemProps {
    item: CartItemWithVariant;
    variant?: 'default' | 'compact';
    className?: string;
}

export function CartItem({ item, variant = 'default', className }: CartItemProps) {
    const { updateCartQuantity, removeFromCart } = useCart();

    const handleQuantityChange = (newQuantity: number) => {
        if (newQuantity <= 0) {
            removeFromCart(item.id);
        } else {
            updateCartQuantity(item.id, newQuantity);
        }
    };

    if (variant === 'compact') {
        return (
            <div className={cn('flex items-center gap-3 py-2', className)}>
                <div className="relative w-12 h-12 flex-shrink-0">
                    <Image
                        src={item.product.imageUrl}
                        alt={item.product.name}
                        fill
                        className="rounded object-cover"
                    />
                </div>
                <div className="flex-1 min-w-0">
                    <h4 className="font-medium text-sm truncate">{item.product.name}</h4>
                    <div className="flex items-center justify-between mt-1">
                        <span className="text-xs text-gray-500">x{item.quantity}</span>
                        <Price price={item.totalPrice} size="sm" />
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className={cn('flex gap-4 p-4 border-b border-gray-200 last:border-b-0', className)}>
            {/* Product Image */}
            <div className="relative w-20 h-20 flex-shrink-0">
                <Link href={`/products/${item.product.slug}`}>
                    <Image
                        src={item.product.imageUrl}
                        alt={item.product.name}
                        fill
                        className="rounded object-cover hover:opacity-80 transition-opacity"
                    />
                </Link>
            </div>

            {/* Product Info */}
            <div className="flex-1 min-w-0">
                <div className="flex justify-between items-start">
                    <div className="flex-1 min-w-0 pr-4">
                        <Link href={`/products/${item.product.slug}`}>
                            <h4 className="font-medium text-sm hover:text-blue-600 transition-colors line-clamp-2">
                                {item.product.name}
                            </h4>
                        </Link>
                        <p className="text-xs text-gray-500 mt-1">{item.product.brand}</p>

                        {/* Product variants/options could go here */}
                        {item.variant && (
                            <div className="mt-1">
                                <Badge variant="outline" className="text-xs">
                                    {item.variant}
                                </Badge>
                            </div>
                        )}
                    </div>

                    {/* Remove Button */}
                    <Button
                        variant="ghost"
                        size="sm"
                        className="p-1 h-6 w-6 text-gray-400 hover:text-red-500"
                        onClick={() => removeFromCart(item.id)}
                    >
                        <X className="w-4 h-4" />
                    </Button>
                </div>

                {/* Quantity and Price */}
                <div className="flex items-center justify-between mt-3">
                    {/* Quantity Controls */}
                    <div className="flex items-center border border-gray-300 rounded">
                        <Button
                            variant="ghost"
                            size="sm"
                            className="h-8 w-8 p-0"
                            onClick={() => handleQuantityChange(item.quantity - 1)}
                        >
                            <Minus className="w-3 h-3" />
                        </Button>
                        <span className="px-3 py-1 text-sm font-medium min-w-[40px] text-center">
                            {item.quantity}
                        </span>
                        <Button
                            variant="ghost"
                            size="sm"
                            className="h-8 w-8 p-0"
                            onClick={() => handleQuantityChange(item.quantity + 1)}
                        >
                            <Plus className="w-3 h-3" />
                        </Button>
                    </div>

                    {/* Price */}
                    <div className="text-right">
                        <Price price={item.totalPrice} size="md" className="font-bold" />
                        {item.quantity > 1 && (
                            <div className="text-xs text-gray-500">
                                {new Intl.NumberFormat(undefined, { style: 'currency', currency: process.env.NEXT_PUBLIC_CURRENCY || 'USD' }).format(item.unitPrice)} x {item.quantity}
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
}

// Cart Summary Component
interface CartSummaryProps {
    className?: string;
}

export function CartSummary({ className }: CartSummaryProps) {
    const { items, total, itemCount } = useCartStore();

    const subtotal = items.reduce((sum, item) => sum + item.totalPrice, 0);
    const shipping = 0;
    const totalAmount = subtotal + shipping;

    return (
        <div className={cn('bg-gray-50 p-4 rounded-lg', className)}>
            <h3 className="font-semibold text-lg mb-4">Tóm tắt đơn hàng</h3>

            <div className="space-y-3">
                <div className="flex justify-between text-sm">
                    <span>Tạm tính ({itemCount} sản phẩm)</span>
                    <span>{new Intl.NumberFormat(undefined, { style: 'currency', currency: process.env.NEXT_PUBLIC_CURRENCY || 'USD' }).format(subtotal)}</span>
                </div>

                <div className="flex justify-between text-sm">
                    <span>Phí vận chuyển</span>
                    <span>Miễn phí</span>
                </div>

                {/* Free shipping note removed for USD default */}

                <hr className="border-gray-300" />

                <div className="flex justify-between font-semibold text-lg">
                    <span>Tổng cộng</span>
                    <span className="text-red-600">{new Intl.NumberFormat(undefined, { style: 'currency', currency: process.env.NEXT_PUBLIC_CURRENCY || 'USD' }).format(totalAmount)}</span>
                </div>
            </div>
        </div>
    );
}