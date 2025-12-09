'use client';

import { Button } from '@/components/ui/button';
import ProductLinkPicker from './ProductLinkPicker';
import UnsplashImagePicker from './UnsplashImagePicker';

export default function ModalTest() {
    const handleImageSelect = (imageUrl: string, altText: string, attribution: string) => {
        console.log('Image selected:', { imageUrl, altText, attribution });
        alert(`nh  chn: ${altText}`);
    };

    const handleProductSelect = (productId: number, productName: string, productPrice: number, productImage?: string) => {
        console.log('Product selected:', { productId, productName, productPrice, productImage });
        alert(`Sn phm  chn: ${productName} - ${productPrice} VND`);
    };

    return (
        <div className="space-y-4 p-4">
            <h2 className="text-xl font-bold">Test Modal Components</h2>

            <div className="flex gap-4">
                <UnsplashImagePicker
                    onImageSelect={handleImageSelect}
                    trigger={
                        <Button variant="outline">
                            Test Image Picker
                        </Button>
                    }
                />

                <ProductLinkPicker
                    onProductSelect={handleProductSelect}
                    trigger={
                        <Button variant="outline">
                            Test Product Picker
                        </Button>
                    }
                />
            </div>

            <div className="text-sm text-gray-600">
                <p>Click cc nt trn  test modal:</p>
                <ul className="list-disc list-inside mt-2 space-y-1">
                    <li>Modal s hin th ton mn hnh (90vh)</li>
                    <li>Ni dung s scroll c nu qu di</li>
                    <li>Grid layout responsive cho mobile</li>
                    <li>Search v filter hot ng bnh thng</li>
                </ul>
            </div>
        </div>
    );
}
