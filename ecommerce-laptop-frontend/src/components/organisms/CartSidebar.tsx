'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Sheet, SheetContent, SheetHeader, SheetTitle, SheetTrigger } from '@/components/ui/sheet';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { formatCurrencyPrice } from '@/lib/currency';
import { useCartStore } from '@/store/cartStore';
import { useUIStore } from '@/store/uiStore';
import { Minus, Plus, ShoppingBag, Trash2 } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';
import { useEffect, useState } from 'react';

export function CartSidebar() {
    const { selectedCurrency } = useCurrencyContext();
    const {
        items,
        updateQuantity,
        removeItem,
        total,
        itemCount,
        clearCart
    } = useCartStore();

    const { isCartSidebarOpen, setCartSidebarOpen } = useUIStore();

    const [mounted, setMounted] = useState(false);
    const [loading, setLoading] = useState(false);

    useEffect(() => {
        setMounted(true);
    }, []);

    if (!mounted) {
        return null;
    }

    const totalItems = itemCount;
    const totalPrice = total;

    return (
        <Sheet open={isCartSidebarOpen} onOpenChange={setCartSidebarOpen}>
            <SheetTrigger asChild>
                <Button variant="outline" size="sm" className="relative">
                    <ShoppingBag className="h-4 w-4" />
                    {totalItems > 0 && (
                        <Badge
                            variant="destructive"
                            className="absolute -top-2 -right-2 h-5 w-5 p-0 flex items-center justify-center text-xs"
                        >
                            {totalItems}
                        </Badge>
                    )}
                </Button>
            </SheetTrigger>
            <SheetContent className="w-full sm:max-w-lg flex flex-col">
                <SheetHeader className="space-y-2.5 pr-6">
                    <SheetTitle className="flex items-center text-left">
                        <ShoppingBag className="h-5 w-5 mr-2" />
                        Giỏ hàng ({totalItems} sản phẩm)
                    </SheetTitle>
                </SheetHeader>

                <div className="flex flex-col flex-1 gap-4 pr-6">
                    {loading ? (
                        <div className="flex-1 flex items-center justify-center">
                            <LoadingSpinner size="lg" />
                        </div>
                    ) : items?.length === 0 ? (
                        <div className="flex-1 flex flex-col items-center justify-center text-center py-8">
                            <div className="w-24 h-24 bg-gray-100 rounded-full flex items-center justify-center mb-4">
                                <ShoppingBag className="h-12 w-12 text-gray-400" />
                            </div>
                            <h3 className="text-lg font-semibold text-gray-900 mb-2">
                                Giỏ hàng trống
                            </h3>
                            <p className="text-gray-500 mb-6 max-w-sm">
                                Thêm sản phẩm vào giỏ hàng để bắt đầu mua sắm
                            </p>
                            <Button onClick={() => setCartSidebarOpen(false)} asChild>
                                <Link href="/products" className="px-6">
                                    Khám phá sản phẩm
                                </Link>
                            </Button>
                        </div>
                    ) : (
                        <>
                            {/* Cart Items */}
                            <div className="flex-1 overflow-y-auto space-y-4">
                                {items?.map((item) => (
                                    <div key={item.id} className="flex items-start space-x-4 p-4 border border-gray-200 rounded-lg bg-white hover:shadow-sm transition-shadow">
                                        {/* Product Image */}
                                        <div className="relative w-16 h-16 flex-shrink-0 bg-gray-100 rounded-md overflow-hidden">
                                            <Image
                                                src={item.product.imageUrl || '/placeholder-product.jpg'}
                                                alt={item.product.name}
                                                fill
                                                className="object-cover"
                                            />
                                        </div>

                                        {/* Product Info */}
                                        <div className="flex-1 min-w-0">
                                            <h4 className="text-sm font-medium text-gray-900 line-clamp-2 mb-1">
                                                {item.product.name}
                                            </h4>
                                            <p className="text-sm text-gray-500 mb-2">
                                                {formatCurrencyPrice(item.unitPrice, selectedCurrency)}
                                            </p>

                                            {/* Quantity Controls */}
                                            <div className="flex items-center justify-between">
                                                <div className="flex items-center border border-gray-300 rounded-md">
                                                    <Button
                                                        variant="ghost"
                                                        size="sm"
                                                        className="h-8 w-8 p-0 hover:bg-gray-100"
                                                        onClick={() => updateQuantity(item.id, item.quantity - 1)}
                                                        disabled={item.quantity <= 1}
                                                    >
                                                        <Minus className="h-3 w-3" />
                                                    </Button>
                                                    <span className="px-3 py-1 text-sm font-medium min-w-[40px] text-center">
                                                        {item.quantity}
                                                    </span>
                                                    <Button
                                                        variant="ghost"
                                                        size="sm"
                                                        className="h-8 w-8 p-0 hover:bg-gray-100"
                                                        onClick={() => updateQuantity(item.id, item.quantity + 1)}
                                                    >
                                                        <Plus className="h-3 w-3" />
                                                    </Button>
                                                </div>

                                                <Button
                                                    variant="ghost"
                                                    size="sm"
                                                    className="h-8 w-8 p-0 text-red-600 hover:text-red-700 hover:bg-red-50"
                                                    onClick={() => removeItem(item.id)}
                                                >
                                                    <Trash2 className="h-4 w-4" />
                                                </Button>
                                            </div>
                                        </div>

                                        {/* Price */}
                                        <div className="text-right">
                                            <p className="text-sm font-semibold text-gray-900">
                                                {formatCurrencyPrice(item.totalPrice, selectedCurrency)}
                                            </p>
                                        </div>
                                    </div>
                                ))}
                            </div>

                            {/* Cart Footer */}
                            <div className="border-t border-gray-200 pt-4 space-y-4 mt-auto">
                                {/* Free shipping notice */}
                                <div className="bg-green-50 border border-green-200 rounded-lg p-3">
                                    <p className="text-sm text-green-800 text-center">
                                         Miễn phí vận chuyển cho đơn hàng trên 500.000₫
                                    </p>
                                </div>

                                {/* Total */}
                                <div className="flex justify-between items-center py-2">
                                    <span className="text-lg font-semibold text-gray-900">
                                        Tổng cộng:
                                    </span>
                                    <span className="text-xl font-bold text-blue-600">
                                        {formatCurrencyPrice(totalPrice, selectedCurrency)}
                                    </span>
                                </div>

                                {/* Action Buttons */}
                                <div className="space-y-3">
                                    <Button
                                        className="w-full h-12 text-base font-semibold"
                                        size="lg"
                                        onClick={() => setCartSidebarOpen(false)}
                                        asChild
                                    >
                                        <Link href="/checkout">
                                            Thanh toán ngay
                                        </Link>
                                    </Button>
                                    <Button
                                        variant="outline"
                                        className="w-full h-10"
                                        onClick={() => setCartSidebarOpen(false)}
                                        asChild
                                    >
                                        <Link href="/cart">
                                            Xem giỏ hàng chi tiết
                                        </Link>
                                    </Button>
                                </div>

                                {/* Clear Cart */}
                                {totalItems > 0 && (
                                    <div className="pt-2">
                                        <Button
                                            variant="ghost"
                                            size="sm"
                                            className="w-full text-red-600 hover:text-red-700 hover:bg-red-50"
                                            onClick={() => {
                                                if (confirm('Bạn có chắc muốn xóa tất cả sản phẩm trong giỏ hàng?')) {
                                                    clearCart();
                                                }
                                            }}
                                        >
                                            <Trash2 className="h-4 w-4 mr-2" />
                                            Xóa tất cả
                                        </Button>
                                    </div>
                                )}
                            </div>
                        </>
                    )}
                </div>
            </SheetContent>
        </Sheet>
    );
}