'use client';

import ImageUpload, { ProductImage } from '@/components/admin/ImageUpload';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Collapsible, CollapsibleContent, CollapsibleTrigger } from '@/components/ui/collapsible';
import { Label } from '@/components/ui/label';
import { ProductFormData } from '@/lib/admin-api';
import { ChevronDown, ChevronRight } from 'lucide-react';

interface MediaManagementSectionProps {
    formData: ProductFormData;
    onInputChange: (field: keyof ProductFormData, value: any) => void;
    isExpanded: boolean;
    onToggle: () => void;
    // Optional props to enable direct upload/delete
    productId?: number;
    images?: ProductImage[];
    onImagesChange?: (images: ProductImage[]) => void;
    onImageUpload?: (files: FileList) => Promise<void>;
    onImageDelete?: (imageId: string) => Promise<void>;
    disabled?: boolean;
}

export default function MediaManagementSection({
    formData,
    onInputChange,
    isExpanded,
    onToggle,
    productId,
    images,
    onImagesChange,
    onImageUpload,
    onImageDelete,
    disabled = false
}: MediaManagementSectionProps) {
    // Convert formData.images to ProductImage format when explicit images not provided
    const fallbackImages: ProductImage[] = formData.images.map((url, index) => ({
        id: `image-${index}`,
        imageUrl: url,
        altText: `Product image ${index + 1}`,
        isMain: index === 0,
        displayOrder: index
    }));

    const effectiveImages = images ?? fallbackImages;

    const handleImagesChange = (imgs: ProductImage[]) => {
        // Keep form data in sync with URLs regardless of which images array is used
        const imageUrls = imgs.map(img => img.imageUrl);
        onInputChange('images', imageUrls);
        // Also notify parent if provided
        onImagesChange?.(imgs);
    };

    return (
        <Card>
            <Collapsible open={isExpanded} onOpenChange={onToggle}>
                <CollapsibleTrigger asChild>
                    <CardHeader className="cursor-pointer hover:bg-muted/50 transition-colors">
                        <div className="flex items-center justify-between">
                            <div>
                                <CardTitle>Hình ảnh & Media</CardTitle>
                                <CardDescription>
                                    Quản lý hình ảnh và media cho sản phẩm
                                </CardDescription>
                            </div>
                            {isExpanded ? (
                                <ChevronDown className="h-5 w-5 text-muted-foreground" />
                            ) : (
                                <ChevronRight className="h-5 w-5 text-muted-foreground" />
                            )}
                        </div>
                    </CardHeader>
                </CollapsibleTrigger>
                <CollapsibleContent>
                    <CardContent className="space-y-4">
                        <div className="space-y-2">
                            <Label>Hình ảnh sản phẩm</Label>
                            <p className="text-sm text-muted-foreground">
                                Upload hình ảnh cho sản phẩm (tối đa 10 ảnh)
                            </p>
                            <ImageUpload
                                productId={productId}
                                images={effectiveImages}
                                onImagesChange={handleImagesChange}
                                onImageUpload={onImageUpload}
                                onImageDelete={onImageDelete}
                                maxImages={10}
                                disabled={disabled}
                            />
                        </div>
                    </CardContent>
                </CollapsibleContent>
            </Collapsible>
        </Card>
    );
}
