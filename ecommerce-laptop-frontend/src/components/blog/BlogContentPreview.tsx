'use client';

import { Check, ExternalLink, ShoppingCart, Loader2 } from 'lucide-react';
import Link from 'next/link';
import { useState } from 'react';
import { useCart } from '@/hooks/useCart';

interface ProductLinkProps {
    productId: number;
    productName: string;
    productPrice?: number;
    productImage?: string;
}

function ProductLink({ productId, productName, productPrice, productImage }: ProductLinkProps) {
    const { addToCart } = useCart();
    const [added, setAdded] = useState(false);

    const handleAddToCart = async (e: React.MouseEvent) => {
        e.preventDefault();
        e.stopPropagation();
        setAdded(true);
        await addToCart({
            id: productId,
            name: productName,
            price: productPrice ?? 0,
            stockQuantity: 1,
            imageUrl: productImage ?? '',
            slug: productId.toString(),
            description: '',
            brand: '',
            model: '',
            type: 'Accessory',
            isActive: true,
            isFeatured: false,
            images: [],
            specifications: [],
            categories: [],
            createdAt: '',
            updatedAt: '',
            sku: '',
        } as any, 1);
        setTimeout(() => setAdded(false), 2000);
    };

    return (
        <div className="my-8 overflow-hidden rounded-lg border border-gray-100 bg-white shadow-sm transition-all duration-300 hover:shadow-xl">
            <div className="flex flex-col sm:flex-row">
                {productImage && (
                    <div className="relative w-full sm:w-56 h-56 sm:h-auto bg-gradient-to-br from-blue-50 via-white to-purple-50 flex-shrink-0 overflow-hidden">
                        <div className="absolute inset-0 bg-gradient-to-t from-black/5 to-transparent z-10" />
                        <img
                            src={productImage}
                            alt={productName}
                            className="w-full h-full object-contain p-6 group-hover:scale-105 transition-transform duration-500"
                        />
                    </div>
                )}
                <div className="flex-1 p-6 sm:p-8 flex flex-col justify-center">
                    {productPrice && (
                        <div className="inline-flex items-center gap-1.5 px-3 py-1 bg-blue-50 text-blue-700 text-xs font-semibold rounded-full mb-3 w-fit">
                            <ShoppingCart className="w-3.5 h-3.5" />
                            Sản phẩm đề xuất
                        </div>
                    )}
                    <h4 className="font-bold text-gray-900 text-lg sm:text-xl mb-2 line-clamp-2 leading-snug">
                        {productName}
                    </h4>
                    {productPrice && (
                        <p className="text-2xl sm:text-3xl font-extrabold text-blue-600 mb-5 tracking-tight">
                            {new Intl.NumberFormat('vi-VN', {
                                style: 'currency',
                                currency: 'VND'
                            }).format(productPrice)}
                        </p>
                    )}
                    <div className="flex items-center gap-3">
                        <Link
                            href={`/products/${productId}`}
                            className="inline-flex w-fit items-center justify-center gap-2 rounded-md border border-gray-200 px-5 py-2.5 text-sm font-semibold text-gray-700 transition-all duration-200 hover:border-gray-300 hover:bg-gray-50"
                        >
                            <ExternalLink className="w-4 h-4" />
                            Xem chi tiết
                        </Link>
                        <button
                            onClick={handleAddToCart}
                            disabled={added}
                            className="inline-flex w-fit items-center justify-center gap-2 rounded-md bg-blue-600 px-5 py-2.5 text-sm font-semibold text-white shadow-md transition-all duration-200 hover:bg-blue-700 hover:shadow-lg active:bg-blue-800 disabled:cursor-default disabled:bg-green-600"
                        >
                            {added ? (
                                <><Check className="w-4 h-4" /> Đã thêm</>
                            ) : (
                                <><ShoppingCart className="w-4 h-4" /> Thêm vào giỏ</>
                            )}
                        </button>
                    </div>
                </div>
            </div>
        </div>
    );
}

interface BlogContentPreviewProps {
    content: string;
}

export default function BlogContentPreview({ content }: BlogContentPreviewProps) {
    // Parse content for product links
    const parseContentForProductLinks = (content: string) => {
        const productLinkRegex = /\[product:(\d+):([^:]+):([^:]*):([^\]]*)\]/g;
        const matches = [];
        let match;

        while ((match = productLinkRegex.exec(content)) !== null) {
            matches.push({
                productId: parseInt(match[1]),
                productName: match[2],
                productPrice: match[3] ? parseFloat(match[3]) : undefined,
                productImage: match[4] || undefined,
                fullMatch: match[0]
            });
        }

        return matches;
    };

    // Render content with product links
    const renderContentWithProductLinks = (content: string) => {
        const productLinks = parseContentForProductLinks(content);
        let processedContent = content;

        // Replace product links with placeholders
        productLinks.forEach((link, index) => {
            processedContent = processedContent.replace(link.fullMatch, `__PRODUCT_LINK_${index}__`);
        });

        return { processedContent, productLinks };
    };

    const { processedContent, productLinks } = renderContentWithProductLinks(content);

    return (
        <div className="blog-content mx-auto max-w-3xl">
            {processedContent.split('__PRODUCT_LINK_').map((part, index) => {
                if (index === 0) {
                    return (
                        <div
                            key={index}
                            dangerouslySetInnerHTML={{ __html: part }}
                        />
                    );
                }

                const linkIndex = parseInt(part.split('__')[0]);
                const remainingContent = part.split('__').slice(1).join('__');

                return (
                    <div key={index}>
                        {productLinks[linkIndex] && (
                            <ProductLink
                                productId={productLinks[linkIndex].productId}
                                productName={productLinks[linkIndex].productName}
                                productPrice={productLinks[linkIndex].productPrice}
                                productImage={productLinks[linkIndex].productImage}
                            />
                        )}
                        <div
                            dangerouslySetInnerHTML={{ __html: remainingContent }}
                        />
                    </div>
                );
            })}
        </div>
    );
}
