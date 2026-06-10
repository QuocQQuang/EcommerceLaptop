'use client';

import { Image } from '@/components/atoms/Image';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useCart } from '@/hooks/useCart';
import { cn } from '@/lib/utils';
import { productService } from '@/services/productService';
import { useWishlistStore } from '@/store/wishlistStore';
import { Product } from '@/types/api';
import {
    CheckCircle,
    Cpu,
    HardDrive,
    Heart,
    Minus,
    Palette,
    Plus,
    Share2,
    ShoppingCart,
    Star,
    Zap
} from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { toast } from 'sonner';
import VariantSelectorSimple from './VariantSelectorSimple';

interface ProductDetailWithVariantsProps {
    product: Product;
    className?: string;
}

export default function ProductDetailWithVariants({
    product,
    className = ''
}: ProductDetailWithVariantsProps) {
    const { addToCart } = useCart();
    const { toggleItem, isInWishlist } = useWishlistStore();
    const [variants, setVariants] = useState<Product[]>([]);
    const [selectedVariant, setSelectedVariant] = useState<Product | null>(null);
    const [loading, setLoading] = useState(false);
    const [quantity, setQuantity] = useState(1);

    // Zoom lens states
    const [selectedImageIndex, setSelectedImageIndex] = useState(0);
    const imageRef = useRef<HTMLDivElement>(null);
    const [showLens, setShowLens] = useState(false);
    const [position, setPosition] = useState({ x: 0, y: 0 });
    const [containerSize, setContainerSize] = useState({ width: 0, height: 0 });
    const ZOOM_FACTOR = 2;
    const LENS_SIZE = 250;

    // Load variants if this is a base product
    useEffect(() => {
        if (product.isBaseProduct && product.variants && product.variants.length > 0) {
            setVariants(product.variants);
        } else if (product.isBaseProduct) {
            loadVariants();
        }
    }, [product]);

    const loadVariants = async () => {
        try {
            setLoading(true);
            const variantData = await productService.getVariants(product.id);
            setVariants(variantData);
        } catch (error) {
            console.error('Error loading variants:', error);
            toast.error('Failed to load product variants');
        } finally {
            setLoading(false);
        }
    };

    const handleVariantSelect = (variant: Product) => {
        setSelectedVariant(variant);
    };

    const handleAddToCart = (variant: Product, qty: number) => {
        addToCart(variant, qty, variant.id);
        toast.success('Đã thêm vào giỏ', {
            description: `${variant.name} đã được thêm vào giỏ hàng`
        });
    };

    const handleAddBaseProductToCart = () => {
        addToCart(product, quantity);
        toast.success('Đã thêm vào giỏ', {
            description: `${product.name} đã được thêm vào giỏ hàng`
        });
    };

    // Zoom lens handlers
    const handleMouseEnter = () => {
        setShowLens(true);
    };

    const handleMouseLeave = () => {
        setShowLens(false);
    };

    const handleMouseMove = (e: React.MouseEvent<HTMLDivElement>) => {
        if (!imageRef.current) return;

        const rect = imageRef.current.getBoundingClientRect();
        const x = e.clientX - rect.left;
        const y = e.clientY - rect.top;

        setContainerSize({ width: rect.width, height: rect.height });

        const halfLens = LENS_SIZE / 2;
        const clampedX = Math.max(halfLens, Math.min(x, rect.width - halfLens));
        const clampedY = Math.max(halfLens, Math.min(y, rect.height - halfLens));

        setPosition({ x: clampedX, y: clampedY });
    };

    const getDisplayProduct = () => {
        return selectedVariant || product;
    };

    const displayProduct = getDisplayProduct();
    const hasVariants = variants.length > 0 || product.isBaseProduct;
    const isWishlisted = isInWishlist(displayProduct.id);

    const handleToggleWishlist = () => {
        toggleItem(displayProduct);
        toast.success(isWishlisted ? 'Đã bỏ khỏi yêu thích' : 'Đã thêm vào yêu thích', {
            description: displayProduct.name
        });
    };

    const handleShare = async () => {
        try {
            const shareData = {
                title: displayProduct.name,
                text: displayProduct.shortDescription || displayProduct.description || displayProduct.name,
                url: typeof window !== 'undefined' ? window.location.href : ''
            };

            if (navigator && (navigator as any).share) {
                await (navigator as any).share(shareData);
            } else {
                await navigator.clipboard.writeText(shareData.url);
                toast.success('Đã sao chép liên kết để chia sẻ');
            }
        } catch (err) {
            toast.error('Không thể chia sẻ lúc này');
            console.error('Share failed:', err);
        }
    };

    // Get all images from product.images array, fallback to single imageUrl if no images array
    const images = displayProduct.images && displayProduct.images.length > 0
        ? displayProduct.images.map(img => img.imageUrl)
        : (displayProduct.imageUrl ? [displayProduct.imageUrl] : ['/images/placeholder-product.jpg']);

    return (
        <div className={`grid grid-cols-1 lg:grid-cols-2 gap-8 ${className}`}>
            {/* Product Images */}
            <div className="space-y-4">
                <div
                    ref={imageRef}
                    className="aspect-[4/3] relative overflow-hidden rounded-lg bg-gray-100 cursor-zoom-in max-h-[500px] w-full"
                    onMouseEnter={handleMouseEnter}
                    onMouseLeave={handleMouseLeave}
                    onMouseMove={handleMouseMove}
                >
                    <Image
                        key={selectedImageIndex} // Force re-render when index changes
                        src={images[selectedImageIndex] || displayProduct.imageUrl}
                        alt={displayProduct.name}
                        fill
                        className="object-contain"
                        priority={selectedImageIndex === 0}
                    />

                    {/* Badges */}
                    <div className="absolute top-4 left-4 flex flex-col gap-2">
                        {displayProduct.isFeatured && (
                            <Badge variant="destructive">HOT</Badge>
                        )}
                        {displayProduct.discountPrice && displayProduct.discountPrice < displayProduct.price && (
                            <Badge variant="secondary">
                                -{Math.round(((displayProduct.price - displayProduct.discountPrice) / displayProduct.price) * 100)}%
                            </Badge>
                        )}
                        {displayProduct.stockQuantity === 0 && (
                            <Badge variant="outline">Hết hàng</Badge>
                        )}
                    </div>
                    {showLens && containerSize.width > 0 && (
                        <div
                            className="absolute rounded-full border-2 border-white/80 shadow-lg pointer-events-none bg-no-repeat"
                            style={{
                                width: LENS_SIZE,
                                height: LENS_SIZE,
                                left: `${position.x - LENS_SIZE / 2}px`,
                                top: `${position.y - LENS_SIZE / 2}px`,
                                backgroundImage: `url(${images[selectedImageIndex] || displayProduct.imageUrl})`,
                                backgroundSize: `${containerSize.width * ZOOM_FACTOR}px ${containerSize.height * ZOOM_FACTOR}px`,
                                backgroundPosition: `${-(position.x * ZOOM_FACTOR - LENS_SIZE / 2)}px ${-(position.y * ZOOM_FACTOR - LENS_SIZE / 2)}px`,
                            }}
                        />
                    )}
                </div>

                {/* Thumbnail Images */}
                {images.length > 1 && (
                    <div className="flex space-x-2 overflow-x-auto">
                        {images.map((image, index) => {
                            const productImage = displayProduct.images?.[index];
                            const altText = productImage?.altText || `${displayProduct.name} ${index + 1}`;
                            return (
                                <button
                                    key={productImage?.id || index}
                                    onClick={(e) => {
                                        e.preventDefault();
                                        e.stopPropagation();
                                        setSelectedImageIndex(index);
                                    }}
                                    className={cn(
                                        'relative w-20 h-20 rounded-lg overflow-hidden border-2 flex-shrink-0 cursor-pointer hover:border-blue-300 transition-colors',
                                        selectedImageIndex === index ? 'border-blue-500' : 'border-gray-200'
                                    )}
                                    type="button"
                                >
                                    <Image src={image} alt={altText} fill className="object-cover pointer-events-none" />
                                </button>
                            );
                        })}
                    </div>
                )}
            </div>

            {/* Product Info */}
            <div className="space-y-6">
                {/* Header */}
                <div>
                    <div className="flex items-center gap-2 mb-2">
                        <Badge variant="outline">{displayProduct.brand}</Badge>
                        {displayProduct.isFeatured && (
                            <Badge variant="destructive">Featured</Badge>
                        )}
                        {displayProduct.isVariant && (
                            <Badge variant="secondary">Variant</Badge>
                        )}
                    </div>

                    <h1 className="text-3xl font-bold text-gray-900">
                        {displayProduct.name}
                    </h1>

                    {displayProduct.isVariant && displayProduct.variantName && (
                        <p className="text-lg text-gray-600 mt-2">
                            {displayProduct.variantName}
                        </p>
                    )}
                </div>

                {/* Price */}
                <div className="space-y-2">
                    <div className="flex items-center gap-3">
                        <span className="text-3xl font-bold text-gray-900">
                            ${displayProduct.price.toLocaleString()}
                        </span>
                        {displayProduct.discountPrice && (
                            <span className="text-xl text-gray-500 line-through">
                                ${displayProduct.discountPrice.toLocaleString()}
                            </span>
                        )}
                    </div>

                    {/* Price comparison for variants */}
                    {selectedVariant && selectedVariant.price !== product.price && (
                        <div className="text-sm text-gray-600">
                            {selectedVariant.price > product.price ? (
                                <span className="text-red-600">
                                    +${(selectedVariant.price - product.price).toLocaleString()} nhiều hơn giá gốc
                                </span>
                            ) : (
                                <span className="text-green-600">
                                    ${(product.price - selectedVariant.price).toLocaleString()} tiết kiệm hơn giá gốc
                                </span>
                            )}
                        </div>
                    )}
                </div>

                {/* Rating */}
                {displayProduct.reviewSummary && (
                    <div className="flex items-center gap-2">
                        <div className="flex items-center">
                            {[...Array(5)].map((_, i) => (
                                <Star
                                    key={i}
                                    className={`h-5 w-5 ${i < Math.floor(displayProduct.reviewSummary!.averageRating)
                                        ? 'text-yellow-400 fill-current'
                                        : 'text-gray-300'
                                        }`}
                                />
                            ))}
                        </div>
                        <span className="text-sm text-gray-600">
                            {displayProduct.reviewSummary.averageRating.toFixed(1)}
                            ({displayProduct.reviewSummary.totalReviews} reviews)
                        </span>
                    </div>
                )}

                {/* Stock Status */}
                <div className="flex items-center gap-2">
                    {displayProduct.stockQuantity > 0 ? (
                        <>
                            <CheckCircle className="h-5 w-5 text-green-500" />
                            <span className="text-green-600 font-medium">
                                Còn hàng ({displayProduct.stockQuantity} sản phẩm)
                            </span>
                        </>
                    ) : (
                        <>
                            <CheckCircle className="h-5 w-5 text-red-500" />
                            <span className="text-red-600 font-medium">Hết hàng</span>
                        </>
                    )}
                </div>

                {/* Variant Selector */}
                {hasVariants && (
                    <VariantSelectorSimple
                        product={product}
                        variants={variants}
                        selectedVariantId={selectedVariant?.id}
                        onVariantSelect={handleVariantSelect}
                    />
                )}

                {/* Add to Cart - Always show for all products */}
                <div className="space-y-4">
                    <div className="flex items-center justify-between">
                        <label className="text-sm font-medium">
                            Số lượng:
                        </label>
                        <div className="flex items-center gap-3">
                            <Button
                                variant="outline"
                                size="icon"
                                onClick={() => setQuantity(Math.max(1, quantity - 1))}
                                disabled={quantity <= 1}
                                className="h-8 w-8"
                            >
                                <Minus className="h-4 w-4" />
                            </Button>
                            <div className="min-w-[3rem] text-center text-lg font-semibold">
                                {quantity}
                            </div>
                            <Button
                                variant="outline"
                                size="icon"
                                onClick={() => setQuantity(Math.min(displayProduct.stockQuantity, quantity + 1))}
                                disabled={quantity >= displayProduct.stockQuantity}
                                className="h-8 w-8"
                            >
                                <Plus className="h-4 w-4" />
                            </Button>
                        </div>
                    </div>

                    <Button
                        onClick={() => {
                            if (selectedVariant) {
                                handleAddToCart(selectedVariant, quantity);
                            } else {
                                handleAddBaseProductToCart();
                            }
                        }}
                        disabled={displayProduct.stockQuantity === 0}
                        className="w-full"
                        size="lg"
                    >
                        <ShoppingCart className="h-5 w-5 mr-2" />
                        Thêm vào giỏ
                    </Button>
                </div>

                {/* Action Buttons */}
                <div className="flex gap-3">
                    <Button variant="outline" className="flex-1" onClick={handleToggleWishlist}>
                        <Heart className={cn('h-4 w-4 mr-2', isWishlisted ? 'fill-red-500 text-red-500' : '')} />
                        Yêu thích
                    </Button>
                    <Button variant="outline" className="flex-1" onClick={handleShare}>
                        <Share2 className="h-4 w-4 mr-2" />
                        Chia sẻ
                    </Button>
                </div>

                {/* Product Details Tabs */}
                <Tabs defaultValue="description" className="w-full">
                    <TabsList className="grid w-full grid-cols-3">
                        <TabsTrigger value="description">Mô tả</TabsTrigger>
                        <TabsTrigger value="specifications">Thông số kỹ thuật</TabsTrigger>
                        <TabsTrigger value="reviews">Đánh giá</TabsTrigger>
                    </TabsList>

                    <TabsContent value="description" className="mt-4">
                        <Card>
                            <CardContent className="p-4">
                                <p className="text-gray-700 whitespace-pre-line">
                                    {displayProduct.description}
                                </p>
                            </CardContent>
                        </Card>
                    </TabsContent>

                    <TabsContent value="specifications" className="mt-4">
                        <Card>
                            <CardContent className="p-4">
                                <div className="space-y-4">
                                    {/* Laptop Specifications */}
                                    {displayProduct.type === 'Laptop' && (
                                        <>
                                            <div className="grid grid-cols-2 gap-4">
                                                <div className="flex items-center gap-2">
                                                    <Cpu className="h-4 w-4 text-gray-500" />
                                                    <span className="text-sm font-medium">CPU:</span>
                                                    <span className="text-sm">{displayProduct.cpuBrand} {displayProduct.cpuModel}</span>
                                                </div>
                                                <div className="flex items-center gap-2">
                                                    <Zap className="h-4 w-4 text-gray-500" />
                                                    <span className="text-sm font-medium">RAM:</span>
                                                    <span className="text-sm">{displayProduct.ramCapacityGB}GB {displayProduct.ramType}</span>
                                                </div>
                                                <div className="flex items-center gap-2">
                                                    <HardDrive className="h-4 w-4 text-gray-500" />
                                                    <span className="text-sm font-medium">Ổ cứng:</span>
                                                    <span className="text-sm">{displayProduct.storageCapacityGB}GB {displayProduct.storageType}</span>
                                                </div>
                                                <div className="flex items-center gap-2">
                                                    <Palette className="h-4 w-4 text-gray-500" />
                                                    <span className="text-sm font-medium">Màu sắc:</span>
                                                    <span className="text-sm">{displayProduct.color}</span>
                                                </div>
                                            </div>

                                            <Separator />

                                            <div className="grid grid-cols-2 gap-4">
                                                <div>
                                                    <span className="text-sm font-medium">Màn hình:</span>
                                                    <p className="text-sm text-gray-600">
                                                        {displayProduct.displaySizeInches}" {displayProduct.displayResolution}
                                                    </p>
                                                </div>
                                                <div>
                                                    <span className="text-sm font-medium">GPU:</span>
                                                    <p className="text-sm text-gray-600">
                                                        {displayProduct.gpuBrand} {displayProduct.gpuModel}
                                                    </p>
                                                </div>
                                                <div>
                                                    <span className="text-sm font-medium">Trọng lượng:</span>
                                                    <p className="text-sm text-gray-600">
                                                        {displayProduct.weightKg}kg
                                                    </p>
                                                </div>
                                                <div>
                                                    <span className="text-sm font-medium">Bảo hành:</span>
                                                    <p className="text-sm text-gray-600">
                                                        {displayProduct.warrantyPeriod}
                                                    </p>
                                                </div>
                                            </div>
                                        </>
                                    )}

                                    {/* Specifications from backend */}
                                    {displayProduct.specifications && displayProduct.specifications.length > 0 && (
                                        <div className="space-y-2">
                                            <h4 className="font-medium">Thông số bổ sung</h4>
                                            {displayProduct.specifications.map((spec, index) => (
                                                <div key={index} className="flex justify-between py-1">
                                                    <span className="text-sm font-medium">{spec.name}:</span>
                                                    <span className="text-sm text-gray-600">{spec.value}</span>
                                                </div>
                                            ))}
                                        </div>
                                    )}
                                </div>
                            </CardContent>
                        </Card>
                    </TabsContent>

                    <TabsContent value="reviews" className="mt-4">
                        <Card>
                            <CardContent className="p-4">
                                <p className="text-gray-500 text-center py-8">
                                    Đánh giá sẽ được hiển thị ở đây
                                </p>
                            </CardContent>
                        </Card>
                    </TabsContent>
                </Tabs>
            </div>
        </div>
    );
}
