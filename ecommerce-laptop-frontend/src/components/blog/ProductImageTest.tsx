'use client';

import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Package } from 'lucide-react';
import { useProductsQuery } from '../../hooks/useProductsQuery';

// Helper function to get product image from various field names
const getProductImage = (product: any): string | undefined => {
    return product.imageUrl || product.image || product.mainImageUrl;
};

export default function ProductImageTest() {
    const { data: products, isLoading, error } = useProductsQuery({
        page: 1,
        pageSize: 10
    });

    if (isLoading) {
        return <div>Loading products...</div>;
    }

    if (error) {
        return <div className="text-red-500">Error loading products: {error.message}</div>;
    }

    if (!products || !products.items || products.items.length === 0) {
        return <div>No products found</div>;
    }

    return (
        <div className="space-y-4">
            <h3 className="text-lg font-semibold">Product Images from Database</h3>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                {products.items.map((product: any) => (
                    <Card key={product.id}>
                        <CardHeader>
                            <CardTitle className="text-sm">{product.name}</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <div className="space-y-2">
                                <div className="w-full h-32 bg-gray-100 rounded-lg overflow-hidden">
                                    {getProductImage(product) ? (
                                        <img
                                            src={getProductImage(product)}
                                            alt={product.name}
                                            className="w-full h-full object-cover"
                                            onError={(e) => {
                                                console.log('Image load error:', {
                                                    productId: product.id,
                                                    productName: product.name,
                                                    imageUrl: getProductImage(product),
                                                    allFields: {
                                                        imageUrl: product.imageUrl,
                                                        image: product.image,
                                                        mainImageUrl: product.mainImageUrl
                                                    }
                                                });
                                                e.currentTarget.style.display = 'none';
                                                e.currentTarget.nextElementSibling?.classList.remove('hidden');
                                            }}
                                        />
                                    ) : null}
                                    <div className={`w-full h-full flex items-center justify-center text-gray-400 ${getProductImage(product) ? 'hidden' : ''}`}>
                                        <Package className="w-8 h-8" />
                                    </div>
                                </div>

                                <div className="text-xs text-gray-500">
                                    <p><strong>ID:</strong> {product.id}</p>
                                    <p><strong>Price:</strong> {product.price?.toLocaleString('vi-VN')} VND</p>
                                    <p><strong>Image URL:</strong> {getProductImage(product) || 'None'}</p>
                                    <p><strong>All fields:</strong></p>
                                    <ul className="ml-2">
                                        <li>imageUrl: {product.imageUrl || 'None'}</li>
                                        <li>image: {product.image || 'None'}</li>
                                        <li>mainImageUrl: {product.mainImageUrl || 'None'}</li>
                                    </ul>
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                ))}
            </div>
        </div>
    );
}
