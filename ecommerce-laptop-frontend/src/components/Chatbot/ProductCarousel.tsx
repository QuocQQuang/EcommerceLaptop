import React from 'react';
import { EnrichedProduct } from '../../types/chat';

interface ProductCarouselProps {
    products: EnrichedProduct[];
}

const ProductCarousel: React.FC<ProductCarouselProps> = ({ products }) => {
    if (!products || products.length === 0) return null;

    return (
        <div className="mt-2 w-full overflow-x-auto pb-4 snap-x flex space-x-4 scrollbar-thin scrollbar-thumb-gray-300">
            {products.map((product) => (
                <div
                    key={product.id}
                    className="flex-none w-48 bg-white border border-gray-200 rounded-lg shadow-sm snap-center hover:shadow-md transition-shadow"
                >
                    <div className="relative h-32 bg-gray-100 rounded-t-lg overflow-hidden">
                        <img
                            src={product.thumbnailUrl || '/placeholder-laptop.jpg'}
                            alt={product.name}
                            className="object-cover w-full h-full"
                        />
                        {product.inStock && (
                            <span className="absolute top-1 right-1 bg-green-100 text-green-800 text-xs px-2 py-0.5 rounded-full">
                                In Stock
                            </span>
                        )}
                    </div>
                    <div className="p-2">
                        <h4 className="text-sm font-semibold truncate" title={product.name}>{product.name}</h4>
                        <div className="flex justify-between items-center mt-1">
                            <span className="text-blue-600 font-bold text-sm">${product.price.toLocaleString()}</span>
                            {product.originalPrice && (
                                <span className="text-gray-400 text-xs line-through">${product.originalPrice.toLocaleString()}</span>
                            )}
                        </div>
                        <a
                            href={`/products/${product.slug || product.id}`}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="block mt-2 w-full text-center bg-blue-600 hover:bg-blue-700 text-white text-xs py-1.5 rounded transition-colors"
                        >
                            View Details
                        </a>
                    </div>
                </div>
            ))}
        </div>
    );
};

export default ProductCarousel;
