'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import { Package, Search } from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';
import { useProductsQuery } from '../../hooks/useProductsQuery';

interface Product {
    id: number;
    name: string;
    price: number;
    imageUrl?: string;
    image?: string; // Alternative field name
    mainImageUrl?: string; // Another alternative
    shortDescription?: string;
    description?: string;
    sku?: string;
}

interface ProductLinkPickerProps {
    onProductSelect: (productId: number, productName: string, productPrice: number, productImage?: string) => void;
    trigger?: React.ReactNode;
}

// Helper function to get product image from various field names
const getProductImage = (product: Product): string | undefined => {
    return product.imageUrl || product.image || product.mainImageUrl;
};

// Helper function to get product description
const getProductDescription = (product: Product): string | undefined => {
    return product.shortDescription || product.description;
};

export default function ProductLinkPicker({ onProductSelect, trigger }: ProductLinkPickerProps) {
    const [open, setOpen] = useState(false);
    const [searchTerm, setSearchTerm] = useState('');
    const [selectedProduct, setSelectedProduct] = useState<Product | null>(null);
    const [filteredProducts, setFilteredProducts] = useState<Product[]>([]);

    const { data: products, isLoading, error } = useProductsQuery({
        page: 1,
        pageSize: 100, // Get more products for better selection
        search: searchTerm || undefined
    });

    // Debug logging
    useEffect(() => {
        console.log('ProductLinkPicker - Products data:', products);
        console.log('ProductLinkPicker - Loading:', isLoading);
        console.log('ProductLinkPicker - Error:', error);

        if (products && typeof products === 'object' && 'items' in products && Array.isArray(products.items)) {
            console.log('ProductLinkPicker - Sample product:', products.items[0]);
            if (products.items[0]) {
                console.log('ProductLinkPicker - Sample product image fields:', {
                    imageUrl: products.items[0].imageUrl,
                    image: products.items[0].image,
                    mainImageUrl: products.items[0].mainImageUrl,
                    resolvedImage: getProductImage(products.items[0])
                });
            }
        }
    }, [products, isLoading, error]);

    // Filter products based on search term
    useEffect(() => {
        if (products && typeof products === 'object' && 'items' in products && Array.isArray(products.items)) {
            const filtered = products.items.filter((product: Product) =>
                product.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                product.sku?.toLowerCase().includes(searchTerm.toLowerCase()) ||
                getProductDescription(product)?.toLowerCase().includes(searchTerm.toLowerCase())
            );
            setFilteredProducts(filtered);
            console.log('Filtered products:', filtered);
        } else {
            setFilteredProducts([]);
        }
    }, [products, searchTerm]);

    const handleProductSelect = (product: Product) => {
        console.log('Product selected:', product);
        setSelectedProduct(product);
    };

    const handleConfirmSelection = () => {
        if (!selectedProduct) {
            toast.error('Vui lng chn mt sn phm');
            return;
        }

        console.log('Confirming product selection:', selectedProduct);

        onProductSelect(
            selectedProduct.id,
            selectedProduct.name,
            selectedProduct.price,
            getProductImage(selectedProduct)
        );

        setOpen(false);
        setSelectedProduct(null);
        toast.success('Link sn phm  c chn vo bi vit');
    };

    const formatPrice = (price: number) => {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(price);
    };

    return (
        <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
                {trigger || (
                    <Button variant="outline" size="sm">
                        <Package className="w-4 h-4 mr-2" />
                        Chn link sn phm
                    </Button>
                )}
            </DialogTrigger>
            <DialogContent className="max-w-6xl max-h-[90vh] w-[95vw] overflow-hidden flex flex-col">
                <DialogHeader>
                    <DialogTitle className="flex items-center gap-2">
                        <Package className="w-5 h-5" />
                        Chn sn phm  chn link
                    </DialogTitle>
                    <DialogDescription>
                        Tm kim v chn sn phm  chn link vo bi vit
                    </DialogDescription>
                </DialogHeader>

                <div className="space-y-4 flex-1 overflow-hidden flex flex-col">
                    {/* Search */}
                    <div className="relative">
                        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
                        <Input
                            placeholder="Tm kim sn phm..."
                            value={searchTerm}
                            onChange={(e) => setSearchTerm(e.target.value)}
                            className="pl-10"
                        />
                    </div>

                    {/* Debug Info */}
                    {process.env.NODE_ENV === 'development' && (
                        <div className="text-xs text-gray-500 bg-gray-100 p-2 rounded">
                            Debug: Loading: {isLoading ? 'Yes' : 'No'},
                            Products: {products && typeof products === 'object' && 'items' in products && Array.isArray(products.items) ? products.items.length : 0},
                            Filtered: {filteredProducts.length},
                            Error: {error ? 'Yes' : 'No'}
                        </div>
                    )}

                    {/* Product Grid */}
                    <div className="flex-1 overflow-y-auto min-h-0 p-1">
                        {isLoading ? (
                            <div className="space-y-4">
                                {Array.from({ length: 5 }).map((_, i) => (
                                    <Card key={i}>
                                        <CardContent className="p-4">
                                            <div className="flex gap-4">
                                                <Skeleton className="w-16 h-16 rounded-lg" />
                                                <div className="flex-1 space-y-2">
                                                    <Skeleton className="h-4 w-3/4" />
                                                    <Skeleton className="h-3 w-1/2" />
                                                    <Skeleton className="h-4 w-1/4" />
                                                </div>
                                            </div>
                                        </CardContent>
                                    </Card>
                                ))}
                            </div>
                        ) : error ? (
                            <div className="text-center py-8 text-red-500">
                                <Package className="w-12 h-12 mx-auto mb-4 opacity-50" />
                                <p>Li khi ti sn phm</p>
                                <p className="text-sm">Vui lng th li sau</p>
                                <Button
                                    variant="outline"
                                    size="sm"
                                    className="mt-2"
                                    onClick={() => window.location.reload()}
                                >
                                    Th li
                                </Button>
                            </div>
                        ) : filteredProducts.length > 0 ? (
                            <div className="space-y-2 max-h-full overflow-y-auto">
                                {filteredProducts.map((product) => (
                                    <Card
                                        key={product.id}
                                        className={`cursor-pointer transition-all duration-200 hover:shadow-md ${selectedProduct?.id === product.id ? 'ring-2 ring-blue-500' : ''
                                            }`}
                                        onClick={() => handleProductSelect(product)}
                                    >
                                        <CardContent className="p-4">
                                            <div className="flex gap-4">
                                                {/* Product Image */}
                                                <div className="w-16 h-16 bg-gray-100 rounded-lg overflow-hidden flex-shrink-0">
                                                    {getProductImage(product) ? (
                                                        <img
                                                            src={getProductImage(product)}
                                                            alt={product.name}
                                                            className="w-full h-full object-cover"
                                                            onError={(e) => {
                                                                console.log('Image load error for product:', product.id, getProductImage(product));
                                                                e.currentTarget.style.display = 'none';
                                                                e.currentTarget.nextElementSibling?.classList.remove('hidden');
                                                            }}
                                                        />
                                                    ) : null}
                                                    <div className={`w-full h-full flex items-center justify-center text-gray-400 ${getProductImage(product) ? 'hidden' : ''}`}>
                                                        <Package className="w-6 h-6" />
                                                    </div>
                                                </div>

                                                {/* Product Info */}
                                                <div className="flex-1 min-w-0">
                                                    <h4 className="font-medium text-gray-900 truncate">
                                                        {product.name}
                                                    </h4>
                                                    {getProductDescription(product) && (
                                                        <p className="text-sm text-gray-600 line-clamp-2 mt-1">
                                                            {getProductDescription(product)}
                                                        </p>
                                                    )}
                                                    <div className="flex items-center gap-4 mt-2">
                                                        <span className="text-lg font-bold text-blue-600">
                                                            {formatPrice(product.price)}
                                                        </span>
                                                        {product.sku && (
                                                            <Badge variant="outline" className="text-xs">
                                                                SKU: {product.sku}
                                                            </Badge>
                                                        )}
                                                    </div>
                                                </div>

                                                {/* Selection Indicator */}
                                                {selectedProduct?.id === product.id && (
                                                    <div className="flex items-center justify-center w-6 h-6 bg-blue-500 text-white rounded-full">
                                                        
                                                    </div>
                                                )}
                                            </div>
                                        </CardContent>
                                    </Card>
                                ))}
                            </div>
                        ) : (
                            <div className="text-center py-8 text-gray-500">
                                <Package className="w-12 h-12 mx-auto mb-4 opacity-50" />
                                <p>Khng tm thy sn phm no</p>
                                <p className="text-sm">Th thay i t kha tm kim</p>
                            </div>
                        )}
                    </div>

                    {/* Selected Product Details */}
                    {selectedProduct && (
                        <Card className="border-blue-200 bg-blue-50">
                            <CardHeader>
                                <CardTitle className="text-sm">Sn phm  chn</CardTitle>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                <div className="flex gap-4">
                                    <div className="w-20 h-20 bg-gray-100 rounded-lg overflow-hidden flex-shrink-0">
                                        {getProductImage(selectedProduct) ? (
                                            <img
                                                src={getProductImage(selectedProduct)}
                                                alt={selectedProduct.name}
                                                className="w-full h-full object-cover"
                                                onError={(e) => {
                                                    console.log('Image load error for selected product:', selectedProduct.id, getProductImage(selectedProduct));
                                                    e.currentTarget.style.display = 'none';
                                                    e.currentTarget.nextElementSibling?.classList.remove('hidden');
                                                }}
                                            />
                                        ) : null}
                                        <div className={`w-full h-full flex items-center justify-center text-gray-400 ${getProductImage(selectedProduct) ? 'hidden' : ''}`}>
                                            <Package className="w-8 h-8" />
                                        </div>
                                    </div>
                                    <div className="flex-1">
                                        <h4 className="font-medium text-gray-900">
                                            {selectedProduct.name}
                                        </h4>
                                        {getProductDescription(selectedProduct) && (
                                            <p className="text-sm text-gray-600 mt-1">
                                                {getProductDescription(selectedProduct)}
                                            </p>
                                        )}
                                        <div className="flex items-center gap-4 mt-2">
                                            <span className="text-lg font-bold text-blue-600">
                                                {formatPrice(selectedProduct.price)}
                                            </span>
                                            {selectedProduct.sku && (
                                                <Badge variant="outline" className="text-xs">
                                                    SKU: {selectedProduct.sku}
                                                </Badge>
                                            )}
                                        </div>
                                    </div>
                                </div>

                                <div className="text-xs text-gray-600 bg-white p-3 rounded border">
                                    <strong>Preview:</strong> Link sn phm s c hin th di dng card vi nh, tn, gi v nt "Xem sn phm"
                                </div>

                                <div className="flex justify-end gap-2">
                                    <Button
                                        variant="outline"
                                        onClick={() => setSelectedProduct(null)}
                                    >
                                        Hy
                                    </Button>
                                    <Button onClick={handleConfirmSelection}>
                                        <Package className="w-4 h-4 mr-2" />
                                        Chn link sn phm
                                    </Button>
                                </div>
                            </CardContent>
                        </Card>
                    )}
                </div>
            </DialogContent>
        </Dialog>
    );
}
