'use client';

import { Card, CardContent } from '@/components/ui/card';
import { ExternalLink, ShoppingCart } from 'lucide-react';
import Link from 'next/link';

interface ProductLinkProps {
    productId: number;
    productName: string;
    productPrice?: number;
    productImage?: string;
}

function ProductLink({ productId, productName, productPrice, productImage }: ProductLinkProps) {
    return (
        <Card className="my-6 border border-gray-200 bg-white shadow-sm hover:shadow-md transition-shadow duration-200">
            <CardContent className="p-6">
                <div className="flex items-center gap-6">
                    {productImage && (
                        <div className="flex-shrink-0">
                            <img
                                src={productImage}
                                alt={productName}
                                className="w-20 h-20 object-cover rounded-lg shadow-sm"
                            />
                        </div>
                    )}
                    <div className="flex-1 min-w-0">
                        <h4 className="font-semibold text-gray-900 mb-2 text-lg leading-tight">{productName}</h4>
                        {productPrice && (
                            <p className="text-xl font-bold text-gray-900 mb-3">
                                {new Intl.NumberFormat('vi-VN', {
                                    style: 'currency',
                                    currency: 'VND'
                                }).format(productPrice)}
                            </p>
                        )}
                        <div className="flex items-center gap-3">
                            <Link
                                href={`/products/${productId}`}
                                className="inline-flex items-center px-4 py-2 bg-gray-900 text-white text-sm font-medium rounded-lg hover:bg-gray-800 transition-colors duration-200"
                            >
                                <ExternalLink className="w-4 h-4 mr-2" />
                                Xem sn phm
                            </Link>
                            <Link
                                href={`/products/${productId}`}
                                className="inline-flex items-center px-4 py-2 border border-gray-300 text-gray-700 text-sm font-medium rounded-lg hover:bg-gray-50 transition-colors duration-200"
                            >
                                <ShoppingCart className="w-4 h-4 mr-2" />
                                Thm vo gi
                            </Link>
                        </div>
                    </div>
                </div>
            </CardContent>
        </Card>
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
        <div className="blog-content max-w-none">
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
