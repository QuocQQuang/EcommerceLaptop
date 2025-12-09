'use client';

import { Image } from '@/components/atoms/Image';
import { LoadingSpinner } from '@/components/atoms/LoadingSpinner';
import { Price } from '@/components/atoms/Price';
import { Rating } from '@/components/atoms/Rating';
import { BundleDisplay } from '@/components/bundle/BundleDisplay';
import { ProductCard } from '@/components/molecules/ProductCard';
import { ReviewList } from '@/components/organisms/ReviewList';
import ProductDetailWithVariants from '@/components/products/ProductDetailWithVariants';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { useCart } from '@/hooks/useCart';
import { cn } from '@/lib/utils';
import { productService } from '@/services/productService';
import { reviewService } from '@/services/reviewService';
import { useWishlistStore } from '@/store/wishlistStore';
import { Product, ProductSpecification } from '@/types/api';
import {
    Heart,
    Minus,
    Plus,
    RotateCcw,
    Share2,
    Shield,
    ShoppingCart,
    Truck
} from 'lucide-react';
import { notFound, useParams } from 'next/navigation';
import { useEffect, useRef, useState } from 'react';

export default function ProductPage() {
    const [product, setProduct] = useState<Product | null>(null);
    const [relatedProducts, setRelatedProducts] = useState<Product[]>([]);
    const [loading, setLoading] = useState(true);
    const [quantity, setQuantity] = useState(1);
    const [selectedImageIndex, setSelectedImageIndex] = useState(0);
    const imageRef = useRef<HTMLDivElement>(null);
    const [showLens, setShowLens] = useState(false);
    const [position, setPosition] = useState({ x: 0, y: 0 });
    const [containerSize, setContainerSize] = useState({ width: 0, height: 0 });
    const ZOOM_FACTOR = 2;
    const LENS_SIZE = 250;
    const [activeTab, setActiveTab] = useState(0);
    const [reviewCount, setReviewCount] = useState<number>(0);

    const { addToCart } = useCart();
    const { toggleItem, isInWishlist } = useWishlistStore();

    const params = useParams();
    useEffect(() => {
        const fetchProduct = async () => {
            try {
                const slug = params?.slug as string;
                if (!slug) {
                    notFound();
                }
                const productData = await productService.getProductBySlug(slug);
                if (!productData) {
                    notFound();
                }

                setProduct(productData);

                // Fetch related products
                const relatedResponse = await productService.getProducts({
                    page: 1,
                    pageSize: 4,
                    brand: productData.brand,
                });
                setRelatedProducts(relatedResponse.items.filter(p => p.id !== productData.id));

                // Fetch review count
                try {
                    const reviewSummary = await reviewService.getReviewSummary(productData.id);
                    setReviewCount(reviewSummary.totalReviews);
                } catch (error) {
                    console.error('Failed to fetch review count:', error);
                    setReviewCount(0);
                }
            } catch (error) {
                console.error('Failed to fetch product:', error);
                notFound();
            } finally {
                setLoading(false);
            }
        };

        fetchProduct();
    }, [params?.slug]);

    const handleAddToCart = () => {
        if (product) {
            // If product is a bundle, ensure bundleItems are attached and sent
            addToCart(product, quantity);
        }
    };

    const handleToggleWishlist = () => {
        if (product) {
            toggleItem(product);
        }
    };

    const increaseQuantity = () => {
        if (product && quantity < product.stockQuantity) {
            setQuantity(quantity + 1);
        }
    };

    const decreaseQuantity = () => {
        if (quantity > 1) {
            setQuantity(quantity - 1);
        }
    };

    const handleTabClick = (index: number) => {
        setActiveTab(index);
    };

    // Group specifications by category
    const groupedSpecs = (product?.specifications || []).reduce((acc: Record<string, ProductSpecification[]>, spec: ProductSpecification) => {
        const category = spec.category || 'General'; // Fallback category if not provided
        if (!acc[category]) {
            acc[category] = [];
        }
        acc[category].push(spec);
        return acc;
    }, {});

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

    if (loading) {
        return (
            <div className="container mx-auto px-4 py-8">
                <div className="flex justify-center items-center min-h-96">
                    <LoadingSpinner size="lg" />
                </div>
            </div>
        );
    }

    if (!product) {
        return notFound();
    }

    const isWishlisted = isInWishlist(product.id);
    const hasDiscount = product.discountPrice && product.discountPrice < product.price;
    const discountPercentage = hasDiscount
        ? Math.round(((product.price - product.discountPrice!) / product.price) * 100)
        : 0;

    // Get all images from product.images array, fallback to single imageUrl if no images array
    const images = product.images && product.images.length > 0
        ? product.images.map(img => img.imageUrl)
        : (product.imageUrl ? [product.imageUrl] : ['/images/placeholder-product.jpg']);

    // Debug logging
    console.log('Product images debug:', {
        productImages: product.images,
        imagesArray: images,
        imageUrl: product.imageUrl,
        hasImages: product.images && product.images.length > 0,
        finalImagesCount: images.length
    });

    const tabContents = [
        // Tab 0: Description
        <div key="description" className="prose max-w-none">
            <p>{product.description || 'ang cp nht thng tin chi tit...'}</p>
        </div>,
        // Tab 1: Specifications
        <div key="specs" className="space-y-6">
            {Object.entries(groupedSpecs).map(([category, specs]) => (
                <div key={category}>
                    <h3 className="text-lg font-semibold text-gray-900 border-b pb-2 mb-4">{category}</h3>
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                        {specs.map((spec) => (
                            <div key={spec.id} className="flex justify-between py-2 border-b border-gray-100 last:border-b-0">
                                <span className="text-gray-600">{spec.name}</span>
                                <span className="font-medium">{spec.value}</span>
                            </div>
                        ))}
                    </div>
                </div>
            ))}
            {Object.keys(groupedSpecs).length === 0 && (
                <p className="text-gray-500">Thng s k thut s c cp nht sm.</p>
            )}
        </div>,
        // Tab 2: Reviews
        <div key="reviews">
            <ReviewList productId={product.id} />
        </div>
    ];

    return (
        <div className="container mx-auto px-4 py-8">
            {/* Breadcrumb */}
            <nav className="flex items-center space-x-2 text-sm text-gray-500 mb-8">
                <span>Trang ch</span>
                <span>/</span>
                <span>Sn phm</span>
                <span>/</span>
                <span className="text-gray-900">{product.name}</span>
            </nav>

            {/* Use ProductDetailWithVariants for products with variant support */}
            {product.isBaseProduct || product.variants?.length > 0 ? (
                <ProductDetailWithVariants product={product} />
            ) : (
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-12">
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
                                src={images[selectedImageIndex] || product.imageUrl}
                                alt={product.name}
                                fill
                                className="object-contain"
                                priority={selectedImageIndex === 0}
                            />

                            {/* Badges */}
                            <div className="absolute top-4 left-4 flex flex-col gap-2">
                                {product.isFeatured && (
                                    <Badge variant="destructive">HOT</Badge>
                                )}
                                {hasDiscount && (
                                    <Badge variant="secondary">-{discountPercentage}%</Badge>
                                )}
                                {product.stockQuantity === 0 && (
                                    <Badge variant="outline">Ht hng</Badge>
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
                                        backgroundImage: `url(${images[selectedImageIndex] || product.imageUrl})`,
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
                                    const productImage = product.images?.[index];
                                    const altText = productImage?.altText || `${product.name} ${index + 1}`;
                                    return (
                                        <button
                                            key={productImage?.id || index}
                                            onClick={(e) => {
                                                e.preventDefault();
                                                e.stopPropagation();
                                                console.log('Thumbnail clicked:', index);
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
                        <div>
                            <h1 className="text-3xl font-bold text-gray-900 mb-2">{product.name}</h1>
                            <p className="text-lg text-gray-600">{product.brand}</p>
                        </div>

                        {/* Rating */}
                        <div className="flex items-center gap-4">
                            <Rating rating={4.5} size="lg" showCount reviewCount={128} />
                            <span className="text-sm text-gray-500">SKU: {product.sku}</span>
                        </div>

                        {/* Price */}
                        <div className="space-y-2">
                            <Price
                                price={product.price}
                                discountPrice={product.discountPrice}
                                size="lg"
                            />
                            {hasDiscount && (
                                <p className="text-sm text-green-600">
                                    Tit kim: {new Intl.NumberFormat(undefined, { style: 'currency', currency: process.env.NEXT_PUBLIC_CURRENCY || 'USD' }).format(product.price - (product.discountPrice || 0))}
                                </p>
                            )}
                        </div>

                        {/* Stock Status */}
                        <div className="flex items-center gap-2">
                            {product.stockQuantity > 0 ? (
                                <>
                                    <div className="w-3 h-3 bg-green-500 rounded-full"></div>
                                    <span className="text-green-600">Cn hng ({product.stockQuantity} sn phm)</span>
                                </>
                            ) : (
                                <>
                                    <div className="w-3 h-3 bg-red-500 rounded-full"></div>
                                    <span className="text-red-600">Ht hng</span>
                                </>
                            )}
                        </div>

                        {/* Description */}
                        {product.shortDescription && (
                            <div>
                                <h3 className="font-semibold mb-2">M t ngn</h3>
                                <p className="text-gray-600">{product.shortDescription}</p>
                            </div>
                        )}

                        {/* Quantity and Add to Cart */}
                        <div className="space-y-4">
                            <div className="flex items-center gap-4">
                                <span className="font-medium">S lng:</span>
                                <div className="flex items-center border rounded-lg">
                                    <Button
                                        variant="ghost"
                                        size="sm"
                                        onClick={decreaseQuantity}
                                        disabled={quantity <= 1}
                                    >
                                        <Minus className="w-4 h-4" />
                                    </Button>
                                    <span className="px-4 py-2 min-w-12 text-center">{quantity}</span>
                                    <Button
                                        variant="ghost"
                                        size="sm"
                                        onClick={increaseQuantity}
                                        disabled={quantity >= product.stockQuantity}
                                    >
                                        <Plus className="w-4 h-4" />
                                    </Button>
                                </div>
                            </div>

                            <div className="flex gap-3">
                                <Button
                                    size="lg"
                                    onClick={handleAddToCart}
                                    disabled={product.stockQuantity === 0}
                                    className="flex-1"
                                >
                                    <ShoppingCart className="w-5 h-5 mr-2" />
                                    Thm vo gi
                                </Button>

                                <Button
                                    variant="outline"
                                    size="lg"
                                    onClick={handleToggleWishlist}
                                >
                                    <Heart
                                        className={cn(
                                            'w-5 h-5',
                                            isWishlisted ? 'fill-red-500 text-red-500' : ''
                                        )}
                                    />
                                </Button>

                                <Button variant="outline" size="lg">
                                    <Share2 className="w-5 h-5" />
                                </Button>
                            </div>
                        </div>

                        {/* Bundle Display */}
                        {product.type === 'Bundle' && (
                            <BundleDisplay product={product} />
                        )}

                        {/* Features */}
                        <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 p-4 bg-gray-50 rounded-lg">
                            <div className="flex items-center gap-2">
                                <Truck className="w-5 h-5 text-blue-600" />
                                <span className="text-sm">Giao hng min ph</span>
                            </div>
                            <div className="flex items-center gap-2">
                                <Shield className="w-5 h-5 text-green-600" />
                                <span className="text-sm">Bo hnh chnh hng</span>
                            </div>
                            <div className="flex items-center gap-2">
                                <RotateCcw className="w-5 h-5 text-orange-600" />
                                <span className="text-sm">i tr 7 ngy</span>
                            </div>
                        </div>
                    </div>
                </div>
            )}

            {/* Product Details Tabs */}
            <div className="mt-16">
                <div className="border-b">
                    <div className="flex space-x-8">
                        <button
                            onClick={() => handleTabClick(0)}
                            className={cn(
                                "py-4 px-2 cursor-pointer",
                                activeTab === 0 ? "border-b-2 border-blue-500 text-blue-600 font-medium" : "text-gray-500 hover:text-gray-700"
                            )}
                        >
                            M t chi tit
                        </button>
                        <button
                            onClick={() => handleTabClick(1)}
                            className={cn(
                                "py-4 px-2 cursor-pointer",
                                activeTab === 1 ? "border-b-2 border-blue-500 text-blue-600 font-medium" : "text-gray-500 hover:text-gray-700"
                            )}
                        >
                            Thng s k thut
                        </button>
                        <button
                            onClick={() => handleTabClick(2)}
                            className={cn(
                                "py-4 px-2 cursor-pointer",
                                activeTab === 2 ? "border-b-2 border-blue-500 text-blue-600 font-medium" : "text-gray-500 hover:text-gray-700"
                            )}
                        >
                            nh gi ({reviewCount})
                        </button>
                    </div>
                </div>

                <div className="py-8">
                    {tabContents[activeTab]}
                </div>
            </div>

            {/* Related Products */}
            {relatedProducts.length > 0 && (
                <div className="mt-16">
                    <h2 className="text-2xl font-bold mb-8">Sn phm lin quan</h2>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                        {relatedProducts.map((relatedProduct) => (
                            <ProductCard key={relatedProduct.id} product={relatedProduct} />
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
}