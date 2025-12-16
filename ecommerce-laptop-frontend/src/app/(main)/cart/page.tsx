'use client';

import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { formatCurrencyPrice } from '@/lib/currency';
import { productService } from '@/services/productService';
import { useCart } from '@/hooks/useCart';
import { useCartStore } from '@/store/cartStore'; // Still needed for selector if useCart doesn't expose everything?
// Actually useCart exposes everything.
// Let's verify useCart returns.
import { Product } from '@/types/api';
import { ArrowLeft, Minus, Plus, ShoppingBag, ShoppingCart, Trash2 } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';
import { useEffect, useState } from 'react';

export default function CartPage() {
    const { selectedCurrency } = useCurrencyContext();
    const {
        items,
        total,
        itemCount,
        updateCartQuantity, // Hook action (syncs)
        removeFromCart,     // Hook action (syncs)
        clearCartItems,     // Hook action (syncs)
        addToCart           // Hook action (syncs)
    } = useCart();

    const [mounted, setMounted] = useState(false);
    const [loading, setLoading] = useState(false);
    const [recommended, setRecommended] = useState<Product[]>([]);

    useEffect(() => {
        setMounted(true);
    }, []);

    // Fetch recommended in-stock products (non-mock)
    useEffect(() => {
        const fetchRecommendations = async () => {
            try {
                const res = await productService.getProducts({ pageSize: 6, sortBy: 'popularity' });
                const inStock = res.items.filter(p => (p.stockQuantity ?? p.inventory?.availableQuantity ?? 0) > 0);
                setRecommended(inStock.slice(0, 3));
            } catch (e) {
                // Silent fail; keep recommendations empty if error
                console.error('Failed to load recommendations:', e);
            }
        };
        fetchRecommendations();
    }, []);

    if (!mounted) {
        return (
            <div className="min-h-screen flex items-center justify-center">
                <LoadingSpinner size="lg" />
            </div>
        );
    }

    // Shipping fee is always 0 per requirement
    const finalShippingFee = 0;
    const finalTotal = total + finalShippingFee;

    const addToCartFromReco = (product: Product) => {
        try {
            if (!product) return;
            addToCart(product, 1);
        } catch (e) {
            console.error('Failed to add recommended product to cart:', e);
        }
    };

    return (
        <div className="min-h-screen bg-gray-50 py-8 pb-48">
            <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
                {/* Header */}
                <div className="mb-8">
                    <div className="flex items-center gap-4 mb-4">
                        <Link href="/products">
                            <Button variant="ghost" size="sm">
                                <ArrowLeft className="h-4 w-4 mr-2" />
                                Tip tc mua sm
                            </Button>
                        </Link>
                    </div>
                    <h1 className="text-3xl font-bold text-gray-900 flex items-center">
                        <ShoppingCart className="h-8 w-8 mr-3" />
                        Gi hng ({itemCount} sn phm)
                    </h1>
                </div>

                {items.length === 0 ? (
                    /* Empty Cart */
                    <div className="text-center py-16">
                        <ShoppingBag className="h-24 w-24 text-gray-300 mx-auto mb-6" />
                        <h2 className="text-2xl font-bold text-gray-900 mb-4">
                            Gi hng ca bn ang trng
                        </h2>
                        <p className="text-gray-500 mb-8 max-w-md mx-auto">
                            Hy khm ph cc sn phm tuyt vi ca chng ti v thm chng vo gi hng  bt u mua sm.
                        </p>
                        <Link href="/products">
                            <Button size="lg">
                                Khm ph sn phm
                            </Button>
                        </Link>
                    </div>
                ) : (
                    <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
                        {/* Cart Items */}
                        <div className="lg:col-span-2">
                            <Card>
                                <CardHeader className="flex flex-row items-center justify-between">
                                    <CardTitle>Sn phm trong gi hng</CardTitle>
                                    <Button
                                        variant="ghost"
                                        size="sm"
                                        className="text-red-600 hover:text-red-700 hover:bg-red-50"
                                        onClick={() => {
                                            if (confirm('Bn c chc mun xa tt c sn phm trong gi hng?')) {
                                                clearCartItems();
                                            }
                                        }}
                                    >
                                        <Trash2 className="h-4 w-4 mr-2" />
                                        Xa tt c
                                    </Button>
                                </CardHeader>
                                <CardContent>
                                    <div className="space-y-6">
                                        {items.map((item) => (
                                            <div key={item.id} className="space-y-4">
                                                {/* Main cart item */}
                                                <div className={`flex items-center space-x-4 p-4 border rounded-lg ${item.isBundle ? 'border-blue-200 bg-blue-50' : 'border-gray-200'
                                                    }`}>
                                                    {/* Bundle badge */}
                                                    {item.isBundle && (
                                                        <div className="absolute -top-2 -left-2 bg-blue-500 text-white px-2 py-1 rounded-md text-xs font-semibold">
                                                            COMBO
                                                        </div>
                                                    )}

                                                    {/* Product Image */}
                                                    <div className="relative w-20 h-20 flex-shrink-0">
                                                        <Image
                                                            src={item.product.imageUrl || '/placeholder-product.jpg'}
                                                            alt={item.product.name}
                                                            fill
                                                            className="object-cover rounded-md"
                                                        />
                                                    </div>

                                                    {/* Product Info */}
                                                    <div className="flex-1 min-w-0">
                                                        <Link
                                                            href={`/products/${item.product.id}`}
                                                            className="text-lg font-medium text-gray-900 hover:text-blue-600 transition-colors"
                                                        >
                                                            {item.product.name}
                                                        </Link>
                                                        <p className="text-sm text-gray-500 mt-1">
                                                            SKU: {item.product.sku}
                                                        </p>
                                                        <div className="flex items-center mt-2">
                                                            <span className="text-lg font-bold text-blue-600">
                                                                {formatCurrencyPrice(item.unitPrice, selectedCurrency)}
                                                            </span>
                                                            {item.product.price !== item.unitPrice && (
                                                                <span className="text-sm text-gray-500 line-through ml-2">
                                                                    {formatCurrencyPrice(item.product.price, selectedCurrency)}
                                                                </span>
                                                            )}
                                                        </div>
                                                    </div>

                                                    {/* Quantity Controls */}
                                                    <div className="flex items-center space-x-3">
                                                        <div className="flex items-center border border-gray-300 rounded-md">
                                                            <Button
                                                                variant="ghost"
                                                                size="sm"
                                                                className="h-10 w-10 p-0 hover:bg-gray-100"
                                                                onClick={() => updateCartQuantity(item.id, item.quantity - 1)}
                                                                disabled={item.quantity <= 1 || loading}
                                                            >
                                                                <Minus className="h-4 w-4" />
                                                            </Button>
                                                            <span className="px-4 py-2 text-center font-medium min-w-[60px]">
                                                                {item.quantity}
                                                            </span>
                                                            <Button
                                                                variant="ghost"
                                                                size="sm"
                                                                className="h-10 w-10 p-0 hover:bg-gray-100"
                                                                onClick={() => updateCartQuantity(item.id, item.quantity + 1)}
                                                                disabled={loading}
                                                            >
                                                                <Plus className="h-4 w-4" />
                                                            </Button>
                                                        </div>
                                                    </div>

                                                    {/* Price and Remove */}
                                                    <div className="flex flex-col items-end space-y-2">
                                                        <p className="text-lg font-bold text-gray-900">
                                                            {formatCurrencyPrice(item.totalPrice, selectedCurrency)}
                                                        </p>
                                                        <Button
                                                            variant="ghost"
                                                            size="sm"
                                                            className="text-red-600 hover:text-red-700 hover:bg-red-50"
                                                            onClick={() => {
                                                                console.log('FrontEnd Debug: Delete Button Clicked for Item', item.id);
                                                                removeFromCart(item.id);
                                                            }}
                                                        >
                                                            <Trash2 className="h-4 w-4" />
                                                        </Button>
                                                    </div>
                                                </div>

                                                {/* Bundle items breakdown */}
                                                {item.isBundle && item.bundleItems && item.bundleItems.length > 0 && (
                                                    <div className="ml-8 pl-4 border-l-2 border-blue-200 space-y-2">
                                                        <p className="text-sm font-medium text-gray-700 mb-3">Bao gm:</p>
                                                        {item.bundleItems.map((bundleItem) => (
                                                            <div key={bundleItem.id} className="flex items-center justify-between p-2 bg-white rounded border border-gray-100">
                                                                <div className="flex items-center space-x-3">
                                                                    <div className="relative w-12 h-12 flex-shrink-0">
                                                                        <Image
                                                                            src={bundleItem.product.imageUrl || '/placeholder-product.jpg'}
                                                                            alt={bundleItem.product.name}
                                                                            fill
                                                                            className="object-cover rounded"
                                                                        />
                                                                    </div>
                                                                    <div>
                                                                        <p className="text-sm font-medium text-gray-900">
                                                                            {bundleItem.product.name}
                                                                        </p>
                                                                        <p className="text-xs text-gray-500">
                                                                            S lng: {bundleItem.quantity}
                                                                        </p>
                                                                    </div>
                                                                </div>
                                                                <div className="text-right">
                                                                    <p className="text-sm font-medium text-gray-900">
                                                                        {formatCurrencyPrice(bundleItem.totalPrice, selectedCurrency)}
                                                                    </p>
                                                                    {bundleItem.itemDiscount && bundleItem.itemDiscount > 0 && (
                                                                        <p className="text-xs text-green-600">
                                                                            Tit kim: {formatCurrencyPrice(bundleItem.itemDiscount, selectedCurrency)}
                                                                        </p>
                                                                    )}
                                                                </div>
                                                            </div>
                                                        ))}
                                                    </div>
                                                )}
                                            </div>
                                        ))}
                                    </div>
                                </CardContent>
                            </Card>
                        </div>

                        {/* Order Summary */}
                        <div className="lg:col-span-1">
                            <div className="sticky top-4 z-10">
                                <Card className="z-10">
                                    <CardHeader>
                                        <CardTitle>Tm tt n hng</CardTitle>
                                    </CardHeader>
                                    <CardContent className="space-y-4">
                                        {/* Subtotal */}
                                        <div className="flex justify-between">
                                            <span>Tm tnh ({itemCount} sn phm):</span>
                                            <span className="font-medium">{formatCurrencyPrice(total, selectedCurrency)}</span>
                                        </div>

                                        {/* Shipping */}
                                        <div className="flex justify-between">
                                            <span>Ph vn chuyn:</span>
                                            <span className="font-medium">
                                                {finalShippingFee === 0 ? (
                                                    <span className="text-green-600">Min ph</span>
                                                ) : (
                                                    formatCurrencyPrice(finalShippingFee, selectedCurrency)
                                                )}
                                            </span>
                                        </div>

                                        {/* Free shipping notice removed since shipping is always free */}

                                        <div className="border-t pt-4">
                                            <div className="flex justify-between text-lg font-bold">
                                                <span>Tng cng:</span>
                                                <span className="text-blue-600">{formatCurrencyPrice(finalTotal, selectedCurrency)}</span>
                                            </div>
                                        </div>

                                        {/* Checkout Button */}
                                        <Button
                                            size="lg"
                                            className="w-full"
                                            asChild
                                        >
                                            <Link href="/checkout">
                                                Tin hnh thanh ton
                                            </Link>
                                        </Button>

                                        {/* Security Notice */}
                                        <div className="text-center text-sm text-gray-500 pt-4">
                                            <p>H tr cc phng thc thanh ton ph bin</p>
                                        </div>
                                    </CardContent>
                                </Card>

                                {/* Recommended Products (real, in-stock) */}
                                <Card className="mt-6">
                                    <CardHeader>
                                        <CardTitle className="text-lg">C th bn quan tm</CardTitle>
                                    </CardHeader>
                                    <CardContent>
                                        <div className="space-y-3">
                                            {recommended.map((p) => (
                                                <div key={p.id} className="flex items-center space-x-3 p-3 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors">
                                                    <div className="relative w-12 h-12 bg-gray-100 rounded-md overflow-hidden flex-shrink-0">
                                                        {/* Using next/image would require layout in a small container; keep simple img */}
                                                        <img src={p.imageUrl || '/placeholder-product.jpg'} alt={p.name} className="w-full h-full object-cover" />
                                                    </div>
                                                    <div className="flex-1 min-w-0">
                                                        <Link href={`/products/${p.slug}`} className="text-sm font-medium line-clamp-1 hover:text-blue-600">
                                                            {p.name}
                                                        </Link>
                                                        <p className="text-sm text-blue-600 font-medium">
                                                            {formatCurrencyPrice(p.discountPrice ?? p.price, selectedCurrency)}
                                                        </p>
                                                    </div>
                                                    <Button size="sm" variant="outline" onClick={() => addToCartFromReco(p)}>
                                                        Thm
                                                    </Button>
                                                </div>
                                            ))}
                                            {recommended.length === 0 && (
                                                <p className="text-sm text-gray-500">Khng c gi  ph hp</p>
                                            )}
                                        </div>
                                    </CardContent>
                                </Card>
                            </div>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}