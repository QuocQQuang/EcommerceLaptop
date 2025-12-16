'use client';

import { EnrichedProduct } from '@/types/chat';
import { useCart } from '@/hooks/useCart';
import { ShoppingCart } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';
import { toast } from 'sonner';

interface ProductCarouselProps {
    products: EnrichedProduct[];
}

export function ProductCarousel({ products }: ProductCarouselProps) {
    const { addToCart } = useCart();

    const handleAddToCart = async (product: EnrichedProduct) => {
        // Convert EnrichedProduct to Product format
        const productForCart = {
            id: product.id,
            name: product.name,
            price: product.originalPrice || product.price,
            discountPrice: product.originalPrice ? product.price : undefined,
            imageUrl: product.thumbnailUrl,
            sku: `PROD-${product.id}`,
            slug: product.slug,
            description: '',
            brand: 'Unknown',
            model: '',
            type: 'Laptop' as const,
            isFeatured: false,
            isActive: product.inStock,
            stockQuantity: product.inStock ? 999 : 0,
            images: [{ id: 1, imageUrl: product.thumbnailUrl, isPrimary: true, altText: product.name, sortOrder: 0 }],
            categories: [],
            specifications: [],
            createdAt: new Date().toISOString(),
            updatedAt: new Date().toISOString(),
            isVariant: false,
            isBaseProduct: true,
            variants: []
        };

        await addToCart(productForCart, 1);
    };

    if (!products || products.length === 0) return null;

    return (
        <div className="flex gap-3 overflow-x-auto p-2 snap-x hide-scrollbar">
            {products.map((product) => (
                <div
                    key={product.id}
                    className="min-w-[220px] snap-center bg-white border border-neutral-200 rounded-xl p-3 shadow-sm hover:shadow-md transition-all flex-shrink-0"
                >
                    {/* Product Image */}
                    <Link href={`/products/${product.slug}`} className="block">
                        <div className="aspect-[16/10] bg-neutral-50 rounded-lg mb-3 flex items-center justify-center overflow-hidden relative">
                            <Image
                                src={product.thumbnailUrl || '/images/placeholder-product.jpg'}
                                alt={product.name}
                                fill
                                className="object-contain"
                                sizes="220px"
                            />
                        </div>
                    </Link>

                    {/* Product Title */}
                    <Link href={`/products/${product.slug}`}>
                        <h4 className="font-semibold text-sm leading-tight line-clamp-2 hover:text-neutral-600 transition-colors mb-2">
                            {product.name}
                        </h4>
                    </Link>

                    {/* AI Reasoning (if available) */}
                    {product.aiReasoning && (
                        <p className="text-[10px] text-neutral-500 mb-2 line-clamp-2">
                            {product.aiReasoning}
                        </p>
                    )}

                    {/* Stock Status */}
                    <div className="mb-2">
                        <span className={`text-[10px] font-mono px-1.5 py-0.5 rounded ${product.inStock
                            ? 'bg-green-100 text-green-700'
                            : 'bg-red-100 text-red-700'
                            }`}>
                            {product.inStock ? 'In Stock' : 'Out of Stock'}
                        </span>
                    </div>

                    {/* Price and Action */}
                    <div className="flex justify-between items-center mt-3 border-t border-neutral-100 pt-2">
                        <div className="flex flex-col">
                            <span className="font-bold text-sm font-mono">${product.price.toLocaleString()}</span>
                            {product.originalPrice && product.originalPrice > product.price && (
                                <span className="text-[10px] text-neutral-400 line-through font-mono">
                                    ${product.originalPrice.toLocaleString()}
                                </span>
                            )}
                        </div>
                        <button
                            onClick={() => handleAddToCart(product)}
                            disabled={!product.inStock}
                            className="bg-neutral-900 text-white p-1.5 rounded-lg hover:bg-neutral-800 disabled:bg-neutral-300 disabled:cursor-not-allowed transition-colors"
                            aria-label={`Add ${product.name} to cart`}
                        >
                            <ShoppingCart size={14} />
                        </button>
                    </div>
                </div>
            ))}
        </div>
    );
}
