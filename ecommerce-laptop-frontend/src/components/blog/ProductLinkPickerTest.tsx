'use client';

import { Button } from '@/components/ui/button';
import ProductLinkPicker from './ProductLinkPicker';

export default function ProductLinkPickerTest() {
    const handleProductSelect = (productId: number, productName: string, productPrice: number, productImage?: string) => {
        console.log('Product selected:', { productId, productName, productPrice, productImage });
        alert(`Sn phm  chn: ${productName} - ${productPrice} VND`);
    };

    return (
        <div className="p-4">
            <h2 className="text-xl font-bold mb-4">Test Product Link Picker</h2>
            <ProductLinkPicker
                onProductSelect={handleProductSelect}
                trigger={
                    <Button>
                        Test Product Picker
                    </Button>
                }
            />
        </div>
    );
}
