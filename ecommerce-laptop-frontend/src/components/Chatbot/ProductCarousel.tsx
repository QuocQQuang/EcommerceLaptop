import React, { useState } from 'react';
import { EnrichedProduct } from '../../types/chat';
import { ChevronLeft, ChevronRight } from 'lucide-react';

interface ProductCarouselProps {
    products: EnrichedProduct[];
}

const ProductCarousel: React.FC<ProductCarouselProps> = ({ products }) => {
    const [currentIndex, setCurrentIndex] = useState(0);
    const itemsPerPage = 2;

    if (!products || products.length === 0) return null;

    const visibleProducts = products.slice(currentIndex, currentIndex + itemsPerPage);
    const hasNext = currentIndex + itemsPerPage < products.length;
    const hasPrev = currentIndex > 0;

    const handleNext = () => {
        if (hasNext) setCurrentIndex(prev => prev + itemsPerPage);
    };

    const handlePrev = () => {
        if (hasPrev) setCurrentIndex(prev => prev - itemsPerPage);
    };

    return (
        <div className="mt-4 w-full max-w-full flex flex-col items-center">
            <div className="flex w-full space-x-2">
                {visibleProducts.map((product) => (
                    <div
                        key={product.id}
                        className="flex-1 w-1/2 bg-white border border-gray-200 rounded-lg shadow-sm hover:shadow-md transition-shadow flex flex-col"
                    >
                        <div className="relative h-24 bg-gray-100 rounded-t-lg overflow-hidden">
                            <img
                                src={product.thumbnailUrl || '/placeholder-laptop.jpg'}
                                alt={product.name}
                                className="object-cover w-full h-full"
                            />
                            {product.inStock && (
                                <span className="absolute top-1 right-1 bg-green-100 text-green-800 text-[10px] px-1.5 py-0.5 rounded-full">
                                    In Stock
                                </span>
                            )}
                        </div>
                        <div className="p-2 flex flex-col flex-1">
                            <h4 className="text-xs font-semibold line-clamp-2 h-8" title={product.name}>{product.name}</h4>
                            <div className="flex justify-between items-center mt-1">
                                <span className="text-blue-600 font-bold text-xs">${product.price.toLocaleString()}</span>
                            </div>
                            <a
                                href={`/products/${product.slug || product.id}`}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="mt-auto block w-full text-center bg-blue-600 hover:bg-blue-700 text-white text-[10px] py-1 rounded transition-colors"
                            >
                                View
                            </a>
                        </div>
                    </div>
                ))}
            </div>

            {/* Pagination Controls */}
            {(products.length > itemsPerPage) && (
                <div className="flex items-center space-x-4 mt-2">
                    <button
                        onClick={handlePrev}
                        disabled={!hasPrev}
                        className={`p-1 rounded-full ${hasPrev ? 'bg-gray-200 hover:bg-gray-300 text-gray-700' : 'bg-gray-100 text-gray-300 cursor-not-allowed'}`}
                    >
                        <ChevronLeft size={16} />
                    </button>
                    <span className="text-xs text-gray-400">
                        {Math.ceil((currentIndex + 1) / itemsPerPage)} / {Math.ceil(products.length / itemsPerPage)}
                    </span>
                    <button
                        onClick={handleNext}
                        disabled={!hasNext}
                        className={`p-1 rounded-full ${hasNext ? 'bg-gray-200 hover:bg-gray-300 text-gray-700' : 'bg-gray-100 text-gray-300 cursor-not-allowed'}`}
                    >
                        <ChevronRight size={16} />
                    </button>
                </div>
            )}
        </div>
    );
};

export default ProductCarousel;
