'use client'

import { Image } from '@/components/atoms/Image'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Progress } from '@/components/ui/progress'
import { uploadProductImages } from '@/features/admin/products/api'
import { cn } from '@/lib/utils'
import { AlertTriangle, Shield, Trash2, Upload } from 'lucide-react'
import React, { useRef } from 'react'
import { toast } from 'sonner'

export interface ProductImage {
  id?: string;
  imageId?: string;
  imageUrl: string;
  altText?: string;
  displayOrder: number;
  isUploading?: boolean;
  uploadProgress?: number;
}

interface ImageUploadProps {
  productId?: number;
  images: ProductImage[];
  onImagesChange: (images: ProductImage[]) => void;
  onImageUpload?: (files: FileList) => Promise<void>;
  onImageDelete?: (imageId: string) => Promise<void>;
  maxImages?: number;
  disabled?: boolean;
  className?: string;
}

export const ImageUpload: React.FC<ImageUploadProps> = ({
  productId,
  images,
  onImagesChange,
  onImageUpload,
  onImageDelete,
  maxImages = 10,
  disabled = false,
  className
}) => {
  const fileInputRef = useRef<HTMLInputElement>(null);

  const doDirectUpload = async (files: FileList) => {
    if (disabled) return;

    console.log('doDirectUpload called with files:', {
      count: files.length,
      names: Array.from(files).map(f => f.name),
      productId
    });

    if (onImageUpload) {
      try {
        await onImageUpload(files);
      } catch (err: any) {
        console.error('Upload failed:', err?.message || err);
      }
      return;
    }

    if (!productId) {
      handlePreviewFiles(files);
      return;
    }
    try {
      const results = await uploadProductImages(productId, files);
      console.log('Upload results:', results);

      const newImages = results.map((r, index) => ({
        imageId: r.imageId,
        imageUrl: r.imageUrl,
        altText: `Product image ${images.length + index + 1}`,
        displayOrder: images.length + index + 1
      }));
      onImagesChange([...images, ...newImages]);
    } catch (err: any) {
      console.error('Upload failed:', err?.message || err);
      alert('Upload thất bại');
    }
  };

  const isDragActive = false;
  const dragErrors: string[] = [];
  const dragProps = {
    onDragEnter: (e: React.DragEvent) => { e.preventDefault(); e.stopPropagation(); },
    onDragLeave: (e: React.DragEvent) => { e.preventDefault(); e.stopPropagation(); },
    onDragOver: (e: React.DragEvent) => { e.preventDefault(); e.stopPropagation(); },
    onDrop: (e: React.DragEvent) => {
      e.preventDefault();
      e.stopPropagation();
      if (disabled) return;
      const files = e.dataTransfer.files;
      if (!files || files.length === 0) return;
      doDirectUpload(files);
    }
  };
  const clearDragErrors = () => { };

  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (disabled) return;

    const files = e.target.files;
    console.log('File select event:', { fileCount: files?.length, files: files ? Array.from(files).map(f => f.name) : 'no files' });

    if (files && files.length > 0) {
      clearDragErrors();
      doDirectUpload(files);

      // Reset input to allow selecting same files again
      if (e.target) {
        e.target.value = '';
      }
    }
  };

  const handlePreviewFiles = (files: FileList) => {
    const newPreviews: ProductImage[] = [];
    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      const previewUrl = URL.createObjectURL(file);
      newPreviews.push({
        imageUrl: previewUrl,
        displayOrder: images.length + i + 1,
        altText: file.name,
        isUploading: false
      });
    }
    onImagesChange([...images, ...newPreviews]);
  };

  const handleRemoveImage = async (index: number) => {
    const image = images[index];

    try {
      if (image.imageId && onImageDelete) {
        // Delete from API
        await onImageDelete(image.imageId);
      } else {
        // Remove from preview
        const newImages = images.filter((_, i) => i !== index);
        onImagesChange(newImages);
      }
    } catch (error) {
      console.error('Delete failed:', error);
      toast.error('Xóa ảnh thất bại. Vui lòng thử lại.');
    }
  };

  const openFileDialog = () => {
    if (!disabled && fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  return (
    <div className={cn("space-y-4", className)}>
      <div className="flex items-center gap-2">
        <Label>Hình ảnh sản phẩm ({images.length}/{maxImages})</Label>
        <Shield className="h-4 w-4 text-green-600" />
        <Badge variant="outline" className="text-xs">
        </Badge>
      </div>

      {/* Security Warnings */}
      {dragErrors.length > 0 && (
        <Alert variant="destructive">
          <AlertTriangle className="h-4 w-4" />
          <AlertDescription>
            <ul className="list-disc list-inside">
              {dragErrors.map((error, index) => (
                <li key={index}>{error}</li>
              ))}
            </ul>
          </AlertDescription>
        </Alert>
      )}

      {/* No validation errors/warnings in direct mode */}

      {/* Upload Area */}
      <Card
        className={cn(
          "border-2 border-dashed cursor-pointer transition-colors",
          isDragActive && "border-primary bg-primary/5",
          disabled && "opacity-50 cursor-not-allowed",
          false && ""
        )}
        {...dragProps}
        onClick={openFileDialog}
      >
        <CardContent className="flex flex-col items-center justify-center py-8">
          <Upload className="h-10 w-10 text-muted-foreground mb-4" />

          <p className="text-sm text-muted-foreground text-center">
            <>
              Kéo thả ảnh vào đây hoặc{" "}
              <span className="text-primary hover:underline">chọn file</span>
              <br />
              <span className="text-xs">JPG, PNG, WebP</span>
            </>
          </p>
        </CardContent>
      </Card>

      <Input
        ref={fileInputRef}
        type="file"
        multiple
        accept="image/jpeg,image/jpg,image/png,image/webp"
        onChange={handleFileSelect}
        className="hidden"
        disabled={disabled}
      />
      {/* No security status in direct mode */}

      {/* Image Preview Grid */}
      {images.length > 0 && (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
          {images.map((image, index) => (
            <Card key={index} className="relative group">
              <CardContent className="p-2">
                <div className="aspect-square relative overflow-hidden rounded">
                  <Image
                    src={image.imageUrl}
                    alt={image.altText || `Product image ${index + 1}`}
                    fill
                    className="object-cover"
                  />
                  {image.isUploading && (
                    <div className="absolute inset-0 bg-black/50 flex items-center justify-center">
                      <Progress value={image.uploadProgress || 50} className="w-16" />
                    </div>
                  )}
                  {/* Security badge for uploaded images */}
                  <div className="absolute top-1 left-1">
                    <Badge variant="secondary" className="text-xs">
                      <Shield className="h-3 w-3 mr-1" />
                      Secure
                    </Badge>
                  </div>
                </div>
                <div className="flex items-center justify-between mt-2">
                  <span className="text-xs text-muted-foreground">
                    Ảnh {index + 1}
                  </span>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => handleRemoveImage(index)}
                    disabled={disabled || image.isUploading}
                    className="h-6 w-6 p-0 text-destructive hover:text-destructive"
                  >
                    <Trash2 className="h-3 w-3" />
                  </Button>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
};

export default ImageUpload;
