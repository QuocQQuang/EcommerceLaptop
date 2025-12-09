'use client';

import BlogContentPreview from '@/components/blog/BlogContentPreview';
import HtmlFormattingTest from '@/components/blog/HtmlFormattingTest';
import ModalTest from '@/components/blog/ModalTest';
import ProductImageTest from '@/components/blog/ProductImageTest';
import ProductLinkPickerTest from '@/components/blog/ProductLinkPickerTest';
import QuillHtmlTest from '@/components/blog/QuillHtmlTest';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { ExternalLink, Image as ImageIcon, Package, ShoppingCart } from 'lucide-react';
import Link from 'next/link';

export default function BlogDemoPage() {
    // Demo content with product links
    const demoContent = `
        <h2>Gii thiu v Laptop Gaming mi nht 2025</h2>
        
        <p>Trong th gii cng ngh hin i, laptop gaming  tr thnh mt cng c khng th thiu cho cc game th chuyn nghip v nhng ngi yu thch tri nghim gaming cht lng cao.</p>
        
        <p>Hm nay, chng ti s gii thiu n bn nhng mu laptop gaming tt nht hin ti, vi hiu nng mnh m v thit k p mt.</p>
        
        <h3>1. Laptop Gaming ASUS ROG Strix G15</h3>
        
        <p>y l mt trong nhng laptop gaming c nh gi cao nht hin ti vi cu hnh mnh m v gi c hp l.</p>
        
        [product:1:ASUS ROG Strix G15:25990000:https://images.unsplash.com/photo-1593640408182-31c70c8268f5?w=400]
        
        <h3>2. Laptop Gaming MSI Katana GF66</h3>
        
        <p>MSI Katana GF66 l la chn tuyt vi cho nhng ai mun c hiu nng gaming cao m khng cn chi qu nhiu tin.</p>
        
        [product:2:MSI Katana GF66:18990000:https://images.unsplash.com/photo-1496181133206-80ce9b88a853?w=400]
        
        <h3>3. Laptop Gaming Dell Alienware m15 R7</h3>
        
        <p>Dell Alienware lun l thng hiu c tin tng trong lnh vc gaming, v m15 R7 khng phi ngoi l.</p>
        
        [product:3:Dell Alienware m15 R7:32990000:https://images.unsplash.com/photo-1603302576837-37561b2e2302?w=400]
        
        <h3>Kt lun</h3>
        
        <p>Vic la chn laptop gaming ph hp ph thuc vo nhu cu v ngn sch ca bn. Hy cn nhc k cc yu t nh hiu nng, gi c, v thit k trc khi quyt nh mua.</p>
        
        <p>Chc bn tm c chic laptop gaming ng !</p>
    `;

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

    const productLinks = parseContentForProductLinks(demoContent);

    // Render content with product links
    const renderContentWithProductLinks = (content: string) => {
        let processedContent = content;

        // Replace product links with placeholders
        productLinks.forEach((link, index) => {
            processedContent = processedContent.replace(link.fullMatch, `__PRODUCT_LINK_${index}__`);
        });

        return { processedContent, productLinks };
    };

    const { processedContent, productLinks: links } = renderContentWithProductLinks(demoContent);

    const formatPrice = (price: number) => {
        return new Intl.NumberFormat('vi-VN', {
            style: 'currency',
            currency: 'VND'
        }).format(price);
    };

    const ProductLink = ({ productId, productName, productPrice, productImage }: any) => (
        <Card className="my-4 border-l-4 border-l-blue-500 bg-blue-50">
            <CardContent className="p-4">
                <div className="flex items-center gap-4">
                    {productImage && (
                        <img
                            src={productImage}
                            alt={productName}
                            className="w-16 h-16 object-cover rounded-lg"
                        />
                    )}
                    <div className="flex-1">
                        <h4 className="font-semibold text-gray-900 mb-1">{productName}</h4>
                        {productPrice && (
                            <p className="text-lg font-bold text-blue-600 mb-2">
                                {formatPrice(productPrice)}
                            </p>
                        )}
                        <div className="flex items-center gap-2">
                            <Button asChild size="sm">
                                <Link href={`/products/${productId}`}>
                                    <ExternalLink className="w-4 h-4 mr-2" />
                                    Xem sn phm
                                </Link>
                            </Button>
                            <Button asChild size="sm" variant="outline">
                                <Link href={`/products/${productId}`}>
                                    <ShoppingCart className="w-4 h-4 mr-2" />
                                    Thm vo gi
                                </Link>
                            </Button>
                        </div>
                    </div>
                </div>
            </CardContent>
        </Card>
    );

    return (
        <div className="min-h-screen bg-gray-50">
            <div className="container mx-auto px-4 py-8">
                {/* Header */}
                <div className="text-center mb-8">
                    <h1 className="text-4xl font-bold text-gray-900 mb-4">Demo Blog vi Product Links</h1>
                    <p className="text-xl text-gray-600 max-w-2xl mx-auto">
                        Trang demo hin th cch blog hin th ni dung vi link sn phm v nh minh ha
                    </p>
                </div>

                {/* Demo Blog Content */}
                <Card className="mb-8">
                    <CardContent className="pt-6">
                        {/* Meta Information */}
                        <div className="flex flex-wrap items-center gap-4 text-sm text-gray-600 mb-6">
                            <div className="flex items-center gap-1">
                                <span>Bi Admin</span>
                            </div>
                            <div className="flex items-center gap-1">
                                <span>Ngy 15/12/2025</span>
                            </div>
                            <div className="flex items-center gap-1">
                                <span>5 pht c</span>
                            </div>
                            <div className="flex items-center gap-1">
                                <span>1,234 lt xem</span>
                            </div>
                        </div>

                        {/* Category and Tags */}
                        <div className="flex flex-wrap items-center gap-2 mb-6">
                            <Badge variant="outline">Laptop Gaming</Badge>
                            <Badge variant="secondary" className="text-xs">
                                <Package className="w-3 h-3 mr-1" />
                                Cng ngh
                            </Badge>
                            <Badge variant="secondary" className="text-xs">
                                <Package className="w-3 h-3 mr-1" />
                                Gaming
                            </Badge>
                        </div>

                        {/* Featured Image */}
                        <div className="mb-8">
                            <img
                                src="https://images.unsplash.com/photo-1593640408182-31c70c8268f5?w=800&h=400&fit=crop"
                                alt="Laptop Gaming"
                                className="w-full h-96 object-cover rounded-lg shadow-lg"
                            />
                        </div>

                        {/* Content with Product Links */}
                        <BlogContentPreview content={demoContent} />
                    </CardContent>
                </Card>

                {/* Quill HTML Paste Test */}
                <Card className="mb-8">
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <ImageIcon className="w-5 h-5" />
                            Test Quill HTML Paste
                        </CardTitle>
                        <CardDescription>
                            Kim tra vic paste HTML vo Quill editor v xem kt qu
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <QuillHtmlTest />
                    </CardContent>
                </Card>

                {/* HTML Formatting Test */}
                <Card className="mb-8">
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <ImageIcon className="w-5 h-5" />
                            Test HTML Formatting
                        </CardTitle>
                        <CardDescription>
                            Kim tra nh dng HTML vi cc th h1, h2, h3, p, ul, ol, blockquote, code, img
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <HtmlFormattingTest />
                    </CardContent>
                </Card>

                {/* Product Image Test */}
                <Card className="mb-8">
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Package className="w-5 h-5" />
                            Test Product Images from Database
                        </CardTitle>
                        <CardDescription>
                            Kim tra nh sn phm t database vi cc field name khc nhau
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <ProductImageTest />
                    </CardContent>
                </Card>

                {/* Modal Test */}
                <Card className="mb-8">
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Package className="w-5 h-5" />
                            Test Modal Components
                        </CardTitle>
                        <CardDescription>
                            Test cc modal chn nh v sn phm vi layout ci thin
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <ModalTest />
                    </CardContent>
                </Card>

                {/* Product Link Picker Test */}
                <Card className="mb-8">
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Package className="w-5 h-5" />
                            Test Product Link Picker
                        </CardTitle>
                        <CardDescription>
                            Test component  kim tra chc nng chn sn phm
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <ProductLinkPickerTest />
                    </CardContent>
                </Card>

                {/* Features Demo */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 mb-8">
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <ImageIcon className="w-5 h-5" />
                                nh t Unsplash
                            </CardTitle>
                            <CardDescription>
                                Tch hp nh min ph t Unsplash
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <p className="text-sm text-gray-600">
                                Editor blog c th chn nh trc tip t Unsplash vi attribution t ng.
                            </p>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Package className="w-5 h-5" />
                                Link sn phm
                            </CardTitle>
                            <CardDescription>
                                Chn link sn phm vi card hin th
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <p className="text-sm text-gray-600">
                                T ng to card sn phm vi nh, tn, gi v nt mua hng.
                            </p>
                        </CardContent>
                    </Card>

                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <ExternalLink className="w-5 h-5" />
                                Quill Editor
                            </CardTitle>
                            <CardDescription>
                                Editor rich text mnh m
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <p className="text-sm text-gray-600">
                                S dng React Quill vi y  tnh nng formatting v custom handlers.
                            </p>
                        </CardContent>
                    </Card>
                </div>

                {/* Navigation */}
                <div className="flex justify-center gap-4">
                    <Button asChild>
                        <Link href="/blog">
                            Xem danh sch blog
                        </Link>
                    </Button>
                    <Button asChild variant="outline">
                        <Link href="/admin/blog/posts/new">
                            To blog mi
                        </Link>
                    </Button>
                </div>
            </div>
        </div>
    );
}
