'use client';

import { EnrichedProduct } from '@/types/chat';
import { useCart } from '@/hooks/useCart';
import { ShoppingCart, ChevronLeft, ChevronRight, MessageCircle } from 'lucide-react';
import Image from 'next/image';
import Link from 'next/link';
import { toast } from 'sonner';
import { useState } from 'react';

interface ProductCarouselProps {
    products: EnrichedProduct[];
    onConsult?: (productName: string) => void;//
}

export function ProductCarousel({ products, onConsult }: ProductCarouselProps) {
    const { addToCart } = useCart();
    const [currentPage, setCurrentPage] = useState(0);
    const itemsPerPage = 2;
    const totalPages = Math.ceil(products.length / itemsPerPage);

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

    const handlePrevious = () => {
        setCurrentPage((prev) => Math.max(0, prev - 1));
    };

    const handleNext = () => {
        setCurrentPage((prev) => Math.min(totalPages - 1, prev + 1));
    };

    if (!products || products.length === 0) return null;

    const startIndex = currentPage * itemsPerPage;
    const visibleProducts = products.slice(startIndex, startIndex + itemsPerPage);

    return (
        <div className="relative">
            {/* Navigation Buttons */}
            {totalPages > 1 && (
                <>
                    <button
                        onClick={handlePrevious}
                        disabled={currentPage === 0}
                        className="absolute left-0 top-1/2 -translate-y-1/2 z-10 bg-white/90 hover:bg-white border border-neutral-200 rounded-full p-1.5 shadow-md disabled:opacity-30 disabled:cursor-not-allowed transition-all"
                        aria-label="Previous products"
                    >
                        <ChevronLeft className="w-4 h-4 text-neutral-700" />
                    </button>
                    <button
                        onClick={handleNext}
                        disabled={currentPage === totalPages - 1}
                        className="absolute right-0 top-1/2 -translate-y-1/2 z-10 bg-white/90 hover:bg-white border border-neutral-200 rounded-full p-1.5 shadow-md disabled:opacity-30 disabled:cursor-not-allowed transition-all"
                        aria-label="Next products"
                    >
                        <ChevronRight className="w-4 h-4 text-neutral-700" />
                    </button>
                </>
            )}

            {/* Products Grid */}
            <div className="grid grid-cols-2 gap-3 px-8 py-2">
                {visibleProducts.map((product) => (
                    <div
                        key={product.id}
                        className="bg-white border border-neutral-200 rounded-xl p-3 shadow-sm hover:shadow-md transition-all flex flex-col"
                    >
                        {/* Product Image */}
                        <Link href={`/products/${product.slug}`} className="block">
                            <div className="aspect-[16/10] bg-neutral-50 rounded-lg mb-3 flex items-center justify-center overflow-hidden relative">
                                <Image
                                    src={product.thumbnailUrl || '/images/placeholder-product.jpg'}
                                    alt={product.name}
                                    fill
                                    className="object-contain"
                                    sizes="(max-width: 768px) 50vw, 200px"
                                />
                            </div>
                        </Link>

                        {/* Product Title */}
                        <Link href={`/products/${product.slug}`}>
                            <h4 className="font-semibold text-sm leading-tight line-clamp-2 min-h-[2.5rem] hover:text-neutral-600 transition-colors mb-2">
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
                                {product.inStock ? 'Còn hàng' : 'Hết hàng'}
                            </span>
                        </div>

                        {/* Spacer to push price to bottom */}
                        <div className="flex-grow"></div>

                        {/* Price and Action */}
                        <div className="mt-3 border-t border-neutral-100 pt-2">
                            <div className="flex justify-between items-center mb-2">
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
                                    aria-label={`Thêm ${product.name} vào giỏ hàng`}
                                >
                                    <ShoppingCart size={14} />
                                </button>
                            </div>
                            {onConsult && (
                                <button
                                    onClick={() => onConsult(product.name)}
                                    className="w-full flex items-center justify-center gap-1.5 py-1.5 text-xs font-medium text-neutral-700 bg-neutral-50 hover:bg-neutral-100 border border-neutral-200 rounded-lg transition-colors"
                                    aria-label={`Tư vấn về ${product.name}`}
                                >
                                    <MessageCircle size={12} />
                                    <span>Tư vấn</span>
                                </button>
                            )}
                        </div>
                    </div>
                ))}
            </div>

            {/* Page Indicator */}
            {totalPages > 1 && (
                <div className="flex justify-center gap-1.5 mt-2">
                    {Array.from({ length: totalPages }).map((_, index) => (
                        <button
                            key={index}
                            onClick={() => setCurrentPage(index)}
                            className={`w-1.5 h-1.5 rounded-full transition-all ${index === currentPage
                                ? 'bg-neutral-900 w-4'
                                : 'bg-neutral-300 hover:bg-neutral-400'
                                }`}
                            aria-label={`Go to page ${index + 1}`}
                        />
                    ))}
                </div>
            )}
        </div>
    );
}
