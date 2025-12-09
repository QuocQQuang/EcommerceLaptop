'use client';

import { Image } from '@/components/atoms/Image';
import { cn } from '@/lib/utils';
import { ProductImage } from '@/types/api';
import { useState } from 'react';

interface ImageGalleryTestProps {
    images: ProductImage[];
    productName: string;
}

export function ImageGalleryTest({ images, productName }: ImageGalleryTestProps) {
    const [selectedImageIndex, setSelectedImageIndex] = useState(0);

    // Get all image URLs
    const imageUrls = images.map(img => img.imageUrl);

    console.log('ImageGalleryTest debug:', {
        images,
        imageUrls,
        selectedImageIndex,
        hasImages: images.length > 0
    });

    if (images.length === 0) {
        return (
            <div className="text-center py-8">
                <p className="text-gray-500">No images available</p>
            </div>
        );
    }

    return (
        <div className="space-y-4">
            {/* Main Image */}
            <div className="aspect-square relative overflow-hidden rounded-lg bg-gray-100">
                <img
                    key={selectedImageIndex} // Force re-render when index changes
                    src={imageUrls[selectedImageIndex]}
                    alt={images[selectedImageIndex]?.altText || `${productName} ${selectedImageIndex + 1}`}
                    className="w-full h-full object-cover"
                    style={{ width: '100%', height: '100%' }}
                />
            </div>

            {/* Thumbnail Images */}
            {images.length > 1 && (
                <div className="flex space-x-2 overflow-x-auto">
                    {images.map((image, index) => (
                        <button
                            key={image.id || index}
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
                            <Image
                                src={image.imageUrl}
                                alt={image.altText || `${productName} ${index + 1}`}
                                fill
                                className="object-cover pointer-events-none"
                            />
                        </button>
                    ))}
                </div>
            )}

            {/* Debug Info */}
            <div className="text-sm text-gray-600 space-y-1 p-4 bg-gray-100 rounded">
                <p><strong>Total Images:</strong> {images.length}</p>
                <p><strong>Selected Index:</strong> {selectedImageIndex}</p>
                <p><strong>Current Image URL:</strong> {imageUrls[selectedImageIndex]}</p>
                <p><strong>Image Alt Text:</strong> {images[selectedImageIndex]?.altText || 'N/A'}</p>
                <div className="mt-2">
                    <strong>All Image URLs:</strong>
                    <ul className="ml-4 text-xs">
                        {imageUrls.map((url, index) => (
                            <li key={index} className={selectedImageIndex === index ? 'font-bold text-blue-600' : ''}>
                                {index}: {url.substring(0, 50)}...
                            </li>
                        ))}
                    </ul>
                </div>
            </div>
        </div>
    );
}
