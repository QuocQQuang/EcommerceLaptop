import React from 'react';
import { EnrichedProduct } from '../../types/chat';
import { ExternalLink } from 'lucide-react';

interface ProductCarouselProps {
    products: EnrichedProduct[];
}

const ProductCarousel: React.FC<ProductCarouselProps> = ({ products }) => {
    if (!products || products.length === 0) return null;

    return (
        <div className="mt-2 w-full">
            {/* Horizontal Scroll Container */}
            <div className="flex gap-3 overflow-x-auto snap-x snap-mandatory scrollbar-hide pb-2">
                {products.map((product) => (
                    <div
                        key={product.id}
                        className="flex-shrink-0 w-[200px] bg-white border border-slate-100 rounded-xl shadow-sm hover:shadow-md hover:-translate-y-1 transition-all duration-200 snap-start flex flex-col"
                    >
                        {/* Product Image */}
                        <div className="relative h-32 bg-slate-50 rounded-t-xl overflow-hidden">
                            <img
                                src={product.thumbnailUrl || '/placeholder-laptop.jpg'}
                                alt={product.name}
                                className="object-cover w-full h-full"
                            />
                            {product.inStock && (
                                <span className="absolute top-2 right-2 bg-teal-100 text-teal-700 text-[10px] px-2 py-0.5 rounded-full font-medium">
                                    In Stock
                                </span>
                            )}
                        </div>

                        {/* Product Info */}
                        <div className="p-3 flex flex-col flex-1">
                            <h4 className="text-xs font-semibold text-slate-800 line-clamp-2 h-8 mb-2" title={product.name}>
                                {product.name}
                            </h4>

                            {/* Price - Monospace */}
                            <div className="flex items-baseline gap-2 mb-3">
                                <span className="text-indigo-600 font-bold font-mono-tech text-sm">
                                    ${product.price.toLocaleString()}
                                </span>
                                {product.originalPrice && product.originalPrice > product.price && (
                                    <span className="text-slate-400 line-through text-xs font-mono-tech">
                                        ${product.originalPrice.toLocaleString()}
                                    </span>
                                )}
                            </div>

                            {/* CTA Button */}
                            <a
                                href={`/products/${product.slug || product.id}`}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="mt-auto flex items-center justify-center gap-1.5 w-full bg-indigo-600 hover:bg-indigo-700 text-white text-xs py-2 rounded-lg active:scale-95 transition-all font-medium"
                            >
                                <span>View Details</span>
                                <ExternalLink size={12} />
                            </a>
                        </div>
                    </div>
                ))}
            </div>

            {/* Scroll Hint */}
            {products.length > 1 && (
                <div className="text-center mt-2">
                    <span className="text-[10px] text-slate-400 uppercase tracking-wide">
                         Scroll for more 
                    </span>
                </div>
            )}
        </div>
    );
};

export default ProductCarousel;
