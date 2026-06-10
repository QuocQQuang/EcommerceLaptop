'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { formatCurrencyPrice } from '@/lib/currency';
import { WishlistItem, wishlistService } from '@/services/wishlistService';
import { useCartStore } from '@/store/cartStore';
import { useWishlistStore } from '@/store/wishlistStore';
import {
    Grid3x3,
    Heart,
    List,
    Package,
    Share,
    ShoppingCart,
    Trash2
} from 'lucide-react';
import { useSession } from 'next-auth/react';
import Link from 'next/link';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

// Wishlist item interface
interface WishlistItemDisplay extends WishlistItem {
    originalPrice?: number;
    rating?: number;
    reviewCount?: number;
}

type ViewMode = 'grid' | 'list';

export default function WishlistPage() {
    const { selectedCurrency } = useCurrencyContext();
    const { data: session } = useSession();
    const [wishlistItems, setWishlistItems] = useState<WishlistItemDisplay[]>([]);
    const [viewMode, setViewMode] = useState<ViewMode>('grid');
    const [isLoading, setIsLoading] = useState(true);

    // Store hooks
    const { items: wishlistStoreItems, removeItem, clearWishlist } = useWishlistStore();
    const { addItem: addToCart } = useCartStore();

    // Load wishlist from API
    const loadWishlist = async () => {
        if (!session?.user) return;

        setIsLoading(true);
        try {
            const response = await wishlistService.getWishlist();
            const items: WishlistItemDisplay[] = response.items.map(item => ({
                ...item,
                // Add additional properties that might be missing from API
                originalPrice: item.product.price * 1.1, // Mock original price
                rating: 4.5, // Mock rating
                reviewCount: 50 // Mock review count
            }));
            setWishlistItems(items);
        } catch (error) {
            console.error('Error loading wishlist:', error);
            toast.error('Không thể tải danh sách yêu thích');
            setWishlistItems([]);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        loadWishlist();
    }, [session]);

    const handleRemoveFromWishlist = async (productId: number) => {
        try {
            await wishlistService.removeFromWishlist(productId);
            setWishlistItems(prev => prev.filter(item => item.productId !== productId));
            toast.success('Đã xóa khỏi wishlist');
        } catch (error) {
            console.error('Error removing from wishlist:', error);
            toast.error('Có lỗi khi xóa khỏi wishlist');
        }
    };

    const handleAddToCart = (item: WishlistItemDisplay) => {
        if (!item.product.isActive) {
            toast.error('Sản phẩm hiện tại hết hàng');
            return;
        }

        // Add to cart logic here - Create a minimal Product object
        const productForCart = {
            ...item.product,
            description: '',
            shortDescription: '',
            slug: item.product.sku,
            model: '',
            type: 'Laptop' as const,
            discountPrice: undefined,
            isFeatured: false,
            stockQuantity: 999,
            imageUrl: item.product.images.find(img => img.isPrimary)?.imageUrl ||
                (item.product.images.length > 0 ? item.product.images[0].imageUrl : ''),
            images: item.product.images.map((img, index) => ({
                ...img,
                sortOrder: index
            })),
            specifications: [],
            categories: [],
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
            isVariant: false,
            isBaseProduct: true,
            variants: []
        };

        addToCart(productForCart, 1);
        toast.success('Đã thêm vào giỏ hàng');
    };

    const handleClearWishlist = async () => {
        try {
            await wishlistService.clearWishlist();
            setWishlistItems([]);
            toast.success('Đã xóa toàn bộ wishlist');
        } catch (error) {
            console.error('Error clearing wishlist:', error);
            toast.error('Có lỗi khi xóa wishlist');
        }
    };

    const handleShareWishlist = async () => {
        try {
            await navigator.share({
                title: 'Wishlist của tôi',
                text: 'Xem các sản phẩm yêu thích của tôi',
                url: window.location.href,
            });
        } catch (error) {
            // Fallback to copy to clipboard
            navigator.clipboard.writeText(window.location.href);
            toast.success('Đã copy link vào clipboard');
        }
    };

    if (isLoading) {
        return (
            <Card>
                <CardHeader>
                    <div className="h-6 bg-gray-200 rounded w-32 animate-pulse"></div>
                    <div className="h-4 bg-gray-200 rounded w-48 animate-pulse"></div>
                </CardHeader>
                <CardContent>
                    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                        {[...Array(6)].map((_, i) => (
                            <div key={i} className="h-64 bg-gray-200 rounded-lg animate-pulse"></div>
                        ))}
                    </div>
                </CardContent>
            </Card>
        );
    }

    return (
        <Card>
            <CardHeader>
                <div className="flex justify-between items-start">
                    <div>
                        <CardTitle className="flex items-center space-x-2">
                            <Heart className="h-6 w-6 text-red-500" />
                            <span>Wishlist của tôi</span>
                        </CardTitle>
                        <CardDescription>
                            {wishlistItems.length} sản phẩm yêu thích
                        </CardDescription>
                    </div>
                    <div className="flex items-center space-x-2">
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={handleShareWishlist}
                        >
                            <Share className="h-4 w-4 mr-2" />
                            Chia sẻ
                        </Button>
                        <div className="flex border rounded-lg">
                            <Button
                                variant={viewMode === 'grid' ? 'default' : 'ghost'}
                                size="sm"
                                onClick={() => setViewMode('grid')}
                                className="rounded-r-none"
                            >
                                <Grid3x3 className="h-4 w-4" />
                            </Button>
                            <Button
                                variant={viewMode === 'list' ? 'default' : 'ghost'}
                                size="sm"
                                onClick={() => setViewMode('list')}
                                className="rounded-l-none"
                            >
                                <List className="h-4 w-4" />
                            </Button>
                        </div>
                    </div>
                </div>
            </CardHeader>
            <CardContent>
                {wishlistItems.length === 0 ? (
                    <div className="text-center py-12">
                        <Heart className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                        <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">
                            Wishlist trống
                        </h3>
                        <p className="text-gray-500 dark:text-gray-400 mb-4">
                            Thêm sản phẩm yêu thích để dễ dàng theo dõi và mua sau này
                        </p>
                        <Link href="/products">
                            <Button>
                                Khám phá sản phẩm
                            </Button>
                        </Link>
                    </div>
                ) : (
                    <>
                        {/* Actions Bar */}
                        <div className="flex justify-between items-center mb-6">
                            <p className="text-sm text-gray-500">
                                Hiển thị {wishlistItems.length} sản phẩm
                            </p>
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={handleClearWishlist}
                                className="text-red-600 hover:text-red-700"
                            >
                                <Trash2 className="h-4 w-4 mr-2" />
                                Xóa tất cả
                            </Button>
                        </div>

                        {/* Products Grid/List */}
                        {viewMode === 'grid' ? (
                            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                                {wishlistItems.map((item) => (
                                    <div key={item.id} className="border rounded-lg overflow-hidden hover:shadow-lg transition-shadow">
                                        <div className="relative">
                                            <div className="aspect-square bg-gray-100 flex items-center justify-center">
                                                {item.product.images.length > 0 ? (
                                                    <img
                                                        src={item.product.images.find(img => img.isPrimary)?.imageUrl || item.product.images[0]?.imageUrl}
                                                        alt={item.product.name}
                                                        className="w-full h-full object-cover"
                                                    />
                                                ) : (
                                                    <Package className="h-16 w-16 text-gray-400" />
                                                )}
                                            </div>
                                            {!item.product.isActive && (
                                                <div className="absolute inset-0 bg-black bg-opacity-50 flex items-center justify-center">
                                                    <Badge variant="destructive">Hết hàng</Badge>
                                                </div>
                                            )}
                                            <Button
                                                variant="ghost"
                                                size="sm"
                                                className="absolute top-2 right-2 bg-white shadow-sm"
                                                onClick={() => handleRemoveFromWishlist(item.productId)}
                                            >
                                                <Trash2 className="h-4 w-4 text-red-500" />
                                            </Button>
                                        </div>
                                        <div className="p-4">
                                            <h3 className="font-medium mb-2 line-clamp-2">
                                                <Link href={`/products/${item.productId}`} className="hover:text-blue-600">
                                                    {item.product.name}
                                                </Link>
                                            </h3>
                                            <p className="text-sm text-gray-500 mb-2">{item.product.brand}</p>
                                            <div className="flex items-center space-x-2 mb-3">
                                                <span className="text-lg font-bold text-red-600">
                                                    {formatCurrencyPrice(item.product.price, selectedCurrency)}
                                                </span>
                                                {item.originalPrice && item.originalPrice > item.product.price && (
                                                    <span className="text-sm text-gray-500 line-through">
                                                        {formatCurrencyPrice(item.originalPrice, selectedCurrency)}
                                                    </span>
                                                )}
                                            </div>
                                            <div className="flex items-center justify-between">
                                                <div className="flex items-center space-x-1">
                                                    <div className="flex text-yellow-400">
                                                        {''.repeat(Math.floor(item.rating || 0))}
                                                    </div>
                                                    <span className="text-sm text-gray-500">
                                                        ({item.reviewCount || 0})
                                                    </span>
                                                </div>
                                                <Button
                                                    size="sm"
                                                    onClick={() => handleAddToCart(item)}
                                                    disabled={!item.product.isActive}
                                                >
                                                    <ShoppingCart className="h-4 w-4 mr-2" />
                                                    {item.product.isActive ? 'Thêm vào giỏ' : 'Hết hàng'}
                                                </Button>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        ) : (
                            <div className="space-y-4">
                                {wishlistItems.map((item) => (
                                    <div key={item.id} className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                                        <div className="flex items-center space-x-4">
                                            <div className="w-20 h-20 bg-gray-100 rounded-md flex items-center justify-center flex-shrink-0">
                                                {item.product.images.length > 0 ? (
                                                    <img
                                                        src={item.product.images.find(img => img.isPrimary)?.imageUrl || item.product.images[0]?.imageUrl}
                                                        alt={item.product.name}
                                                        className="w-full h-full object-cover rounded-md"
                                                    />
                                                ) : (
                                                    <Package className="h-8 w-8 text-gray-400" />
                                                )}
                                            </div>
                                            <div className="flex-1 min-w-0">
                                                <h3 className="font-medium mb-1">
                                                    <Link href={`/products/${item.productId}`} className="hover:text-blue-600">
                                                        {item.product.name}
                                                    </Link>
                                                </h3>
                                                <p className="text-sm text-gray-500 mb-2">{item.product.brand}</p>
                                                <div className="flex items-center space-x-2 mb-2">
                                                    <span className="text-lg font-bold text-red-600">
                                                        {formatCurrencyPrice(item.product.price, selectedCurrency)}
                                                    </span>
                                                    {item.originalPrice && item.originalPrice > item.product.price && (
                                                        <span className="text-sm text-gray-500 line-through">
                                                            {formatCurrencyPrice(item.originalPrice, selectedCurrency)}
                                                        </span>
                                                    )}
                                                    {!item.product.isActive && (
                                                        <Badge variant="destructive">Hết hàng</Badge>
                                                    )}
                                                </div>
                                                <div className="flex items-center space-x-1">
                                                    <div className="flex text-yellow-400">
                                                        {''.repeat(Math.floor(item.rating || 0))}
                                                    </div>
                                                    <span className="text-sm text-gray-500">
                                                        ({item.reviewCount || 0} đánh giá)
                                                    </span>
                                                </div>
                                            </div>
                                            <div className="flex flex-col space-y-2">
                                                <Button
                                                    size="sm"
                                                    onClick={() => handleAddToCart(item)}
                                                    disabled={!item.product.isActive}
                                                >
                                                    <ShoppingCart className="h-4 w-4 mr-2" />
                                                    {item.product.isActive ? 'Thêm vào giỏ' : 'Hết hàng'}
                                                </Button>
                                                <Button
                                                    variant="outline"
                                                    size="sm"
                                                    onClick={() => handleRemoveFromWishlist(item.productId)}
                                                >
                                                    <Trash2 className="h-4 w-4 mr-2" />
                                                    Xóa
                                                </Button>
                                            </div>
                                        </div>
                                    </div>
                                ))}
                            </div>
                        )}
                    </>
                )}
            </CardContent>
        </Card>
    );
}