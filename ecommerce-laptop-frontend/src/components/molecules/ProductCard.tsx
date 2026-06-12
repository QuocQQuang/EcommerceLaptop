'use client';

import { Image } from '@/components/atoms/Image';
import { Price } from '@/components/atoms/Price';
import { Rating } from '@/components/atoms/Rating';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardFooter } from '@/components/ui/card';
import { useCart } from '@/hooks/useCart';
import { cn } from '@/lib/utils';
import { useCompareStore } from '@/store/compareStore';
import { useUIStore } from '@/store/uiStore';
import { useWishlistStore } from '@/store/wishlistStore';
import { Product } from '@/types/api';
import { Check, Eye, Heart, Scale, ShoppingCart } from 'lucide-react';
import Link from 'next/link';

interface ProductCardProps {
    product: Product;
    className?: string;
    variant?: 'default' | 'compact';
    layout?: 'grid' | 'list';
}

export function ProductCard({ product, className, variant = 'default', layout = 'grid' }: ProductCardProps) {
    const { addToCart } = useCart();
    const { toggleItem, isInWishlist } = useWishlistStore();
    const { setQuickViewModal } = useUIStore();

    const isWishlisted = isInWishlist(product.id);
    const hasDiscount = product.discountPrice && product.discountPrice < product.price;
    const discountPercentage = hasDiscount
        ? Math.round(((product.price - product.discountPrice!) / product.price) * 100)
        : 0;

    const handleAddToCart = (e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();
        addToCart(product, 1);
    };

    const handleToggleWishlist = (e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();
        toggleItem(product);
    };

    const handleQuickView = (e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();
        setQuickViewModal(true, product);
    };
    const { toggleItem: toggleCompare, isInCompare } = useCompareStore();

    const handleToggleCompare = (e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();
        toggleCompare(product);
    };

    if (variant === 'compact') {
        return (
            <Link href={`/products/${product.slug}`}>
                <Card className={cn('group cursor-pointer hover:shadow-md transition-shadow', className)}>
                    <CardContent className="p-3">
                        <div className="flex gap-3">
                            <div className="relative w-16 h-16 flex-shrink-0">
                                <Image
                                    src={product.imageUrl || '/placeholder.svg'}
                                    alt={product.name}
                                    fill
                                    sizes="(max-width: 768px) 50vw, (max-width: 1200px) 33vw, 25vw"
                                    className="rounded object-cover"
                                />
                            </div>
                            <div className="flex-1 min-w-0">
                                <h3 className="font-medium text-sm truncate">{product.name}</h3>
                                <p className="text-xs text-gray-500 truncate">{product.brand}</p>
                                <Rating rating={product.reviewSummary?.averageRating ?? 0} size="xs" reviewCount={product.reviewSummary?.totalReviews ?? 0} />
                                <Price
                                    price={product.price}
                                    discountPrice={product.discountPrice}
                                    size="sm"
                                    className="mt-1"
                                />
                            </div>
                        </div>
                    </CardContent>
                </Card>
            </Link>
        );
    }

    // List layout
    if (layout === 'list') {
        return (
            <Link href={`/products/${product.slug}`}>
                <Card className={cn('group cursor-pointer hover:shadow-lg transition-all duration-300', className)}>
                    <CardContent className="p-4">
                        <div className="flex gap-4">
                            {/* Image Section */}
                            <div className="relative w-32 h-32 flex-shrink-0 overflow-hidden rounded-lg">
                                <Image
                                    src={product.imageUrl || '/placeholder.svg'}
                                    alt={product.name}
                                    fill
                                    sizes="(max-width: 768px) 100vw, 25vw"
                                    className="group-hover:scale-105 transition-transform duration-300"
                                />

                                {/* Badges */}
                                <div className="absolute top-2 left-2 flex flex-col gap-1">
                                    {product.isFeatured && (
                                        <Badge variant="destructive" className="text-xs">
                                            HOT
                                        </Badge>
                                    )}
                                    {hasDiscount && (
                                        <Badge variant="secondary" className="text-xs">
                                            -{discountPercentage}%
                                        </Badge>
                                    )}
                                    {product.stockQuantity === 0 && (
                                        <Badge variant="outline" className="text-xs">
                                            Hết hàng
                                        </Badge>
                                    )}
                                </div>
                            </div>

                            {/* Product Info */}
                            <div className="flex-1 flex flex-col justify-between">
                                <div className="space-y-2">
                                    <div>
                                        <h3 className="font-medium text-lg line-clamp-2 group-hover:text-blue-600 transition-colors">
                                            {product.name}
                                        </h3>
                                        <p className="text-sm text-gray-500">{product.brand}</p>
                                    </div>

                                    <p className="text-sm text-gray-600 line-clamp-2">{product.shortDescription}</p>

                                    <Rating rating={product.reviewSummary?.averageRating ?? 0} size="sm" showCount reviewCount={product.reviewSummary?.totalReviews ?? 0} />

                                    {/* Stock Status */}
                                    <div className="text-sm">
                                        {product.stockQuantity > 0 ? (
                                            <span className="text-green-600">Còn hàng</span>
                                        ) : (
                                            <span className="text-red-600">Hết hàng</span>
                                        )}
                                    </div>
                                </div>

                                <div className="flex items-center justify-between mt-4">
                                    <Price
                                        price={product.price}
                                        discountPrice={product.discountPrice}
                                        size="lg"
                                    />

                                    <div className="flex items-center gap-2">
                                        <Button
                                            size="sm"
                                            variant="outline"
                                            onClick={handleToggleWishlist}
                                        >
                                            <Heart
                                                className={cn(
                                                    'w-4 h-4',
                                                    isWishlisted ? 'fill-red-500 text-red-500' : 'text-gray-600'
                                                )}
                                            />
                                        </Button>
                                        <Button
                                            size="sm"
                                            variant="outline"
                                            onClick={handleQuickView}
                                        >
                                            <Eye className="w-4 h-4 text-gray-600" />
                                        </Button>
                                        <Button
                                            onClick={handleAddToCart}
                                            disabled={product.stockQuantity === 0}
                                            size="sm"
                                        >
                                            <ShoppingCart className="w-4 h-4 mr-2" />
                                            Thêm vào giỏ
                                        </Button>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </CardContent>
                </Card>
            </Link>
        );
    }

    return (
        <Link href={`/products/${product.slug}`}>
            <Card className={cn('group cursor-pointer hover:shadow-lg transition-all duration-300', className)}>
                <CardContent className="p-4">
                    {/* Image Section */}
                    <div className="relative aspect-square mb-3 overflow-hidden rounded-lg">
                        <Image
                            src={product.imageUrl || '/placeholder.svg'}
                            alt={product.name}
                            fill
                            sizes="(max-width: 768px) 100vw, 25vw"
                            className="group-hover:scale-105 transition-transform duration-300"
                        />

                        {/* Badges */}
                        <div className="absolute top-2 left-2 flex flex-col gap-1">
                            {product.isFeatured && (
                                <Badge variant="destructive" className="text-xs">
                                    HOT
                                </Badge>
                            )}
                            {hasDiscount && (
                                <Badge variant="secondary" className="text-xs">
                                    -{discountPercentage}%
                                </Badge>
                            )}
                            {product.stockQuantity === 0 && (
                                <Badge variant="outline" className="text-xs">
                                    Hết hàng
                                </Badge>
                            )}
                        </div>

                        {/* Action Buttons */}
                        <div className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 group-focus-within:opacity-100 transition-opacity duration-300 flex flex-col gap-1">
                            <Button
                                size="sm"
                                variant="outline"
                                className="h-8 w-8 p-0 bg-white hover:bg-gray-50"
                                onClick={handleToggleWishlist}
                            >
                                <Heart
                                    className={cn(
                                        'w-4 h-4',
                                        isWishlisted ? 'fill-red-500 text-red-500' : 'text-gray-600'
                                    )}
                                />
                            </Button>
                            <Button
                                size="sm"
                                variant={isInCompare(product.id) ? 'default' : 'outline'}
                                className={cn('h-8 w-8 p-0 bg-white hover:bg-gray-50', isInCompare(product.id) ? 'bg-blue-600 text-white' : '')}
                                onClick={handleToggleCompare}
                            >
                                {isInCompare(product.id) ? (
                                    <Check className="w-4 h-4" />
                                ) : (
                                    <Scale className="w-4 h-4 text-gray-600" />
                                )}
                            </Button>
                            <Button
                                size="sm"
                                variant="outline"
                                className="h-8 w-8 p-0 bg-white hover:bg-gray-50"
                                onClick={handleQuickView}
                            >
                                <Eye className="w-4 h-4 text-gray-600" />
                            </Button>
                        </div>
                    </div>

                    {/* Product Info */}
                    <div className="space-y-2">
                        <div>
                            <h3 className="font-medium text-sm line-clamp-2 group-hover:text-blue-600 transition-colors">
                                {product.name}
                            </h3>
                            <p className="text-xs text-gray-500">{product.brand}</p>

                            {/* Variant Information */}
                            {product.isBaseProduct && product.variants && product.variants.length > 0 && (
                                <div className="mt-1">
                                    <Badge variant="outline" className="text-xs">
                                        {product.variants.length} variants
                                    </Badge>
                                </div>
                            )}

                            {product.isVariant && product.variantName && (
                                <div className="mt-1">
                                    <Badge variant="secondary" className="text-xs">
                                        {product.variantName}
                                    </Badge>
                                </div>
                            )}
                        </div>

                        <Rating rating={product.reviewSummary?.averageRating ?? 0} size="sm" showCount reviewCount={product.reviewSummary?.totalReviews ?? 0} />

                        <Price
                            price={product.price}
                            discountPrice={product.discountPrice}
                            size="md"
                        />

                        {/* Price Range for Base Products with Variants */}
                        {product.isBaseProduct && product.variants && product.variants.length > 0 && (
                            <div className="text-xs text-muted-foreground">
                                From ${Math.min(product.price, ...product.variants.map(v => v.price)).toLocaleString()}
                                {Math.max(product.price, ...product.variants.map(v => v.price)) !== Math.min(product.price, ...product.variants.map(v => v.price)) && (
                                    <span> - ${Math.max(product.price, ...product.variants.map(v => v.price)).toLocaleString()}</span>
                                )}
                            </div>
                        )}

                        {/* Stock Status */}
                        <div className="text-xs">
                            {product.stockQuantity > 0 ? (
                                <span className="text-green-600">Còn hàng</span>
                            ) : (
                                <span className="text-red-600">Hết hàng</span>
                            )}
                        </div>
                    </div>
                </CardContent>

                <CardFooter className="p-4 pt-0">
                    <Button
                        className="w-full"
                        onClick={handleAddToCart}
                        disabled={product.stockQuantity === 0}
                        size="sm"
                    >
                        <ShoppingCart className="w-4 h-4 mr-2" />
                        Thêm vào giỏ
                    </Button>
                </CardFooter>
            </Card>
        </Link>
    );
}