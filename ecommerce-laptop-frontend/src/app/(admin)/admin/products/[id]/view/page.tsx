'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle
} from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import {
  PermissionGuard
} from '@/contexts/AdminAuthContext';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import {
  getAdminProduct,
  PERMISSIONS
} from '@/lib/admin-api';
import { formatCurrencyPrice } from '@/lib/currency';
import { useQuery } from '@tanstack/react-query';
import {
  AlertCircle,
  ArrowLeft,
  Building,
  Calendar,
  DollarSign,
  Edit,
  Hash,
  Package,
  Tag
} from 'lucide-react';
import Link from 'next/link';
import { useParams } from 'next/navigation';

const useProductQuery = (productId: string) => {
  return useQuery({
    queryKey: ['admin', 'product', productId],
    queryFn: () => getAdminProduct(parseInt(productId)),
    enabled: !!productId
  });
};

// Helper functions - moved inside component

const formatDate = (dateString: string) => {
  return new Intl.DateTimeFormat('vi-VN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(dateString));
};

const getStockBadge = (stock: number) => {
  if (stock === 0) {
    return <Badge variant="destructive">Hết hàng</Badge>;
  } else if (stock < 5) {
    return <Badge variant="outline" className="text-orange-600">Sắp hếÍt ({stock})</Badge>;
  } else if (stock < 10) {
    return <Badge variant="outline" className="text-yellow-600">Ít ({stock})</Badge>;
  } else {
    return <Badge variant="outline" className="text-green-600">Còn hàng ({stock})</Badge>;
  }
};

export default function ProductViewPage() {
  const { selectedCurrency } = useCurrencyContext();
  const params = useParams();
  const productId = params.id as string;

  const {
    data: product,
    isLoading,
    error
  } = useProductQuery(productId);

  // Helper functions
  const formatCurrency = (amount: number) => {
    return formatCurrencyPrice(amount, selectedCurrency);
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Skeleton className="h-8 w-[100px]" />
          <Skeleton className="h-8 w-[200px]" />
        </div>
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          <div className="lg:col-span-2 space-y-6">
            <Card>
              <CardHeader>
                <Skeleton className="h-6 w-32" />
                <Skeleton className="h-4 w-48" />
              </CardHeader>
              <CardContent className="space-y-4">
                <Skeleton className="h-4 w-full" />
                <Skeleton className="h-4 w-3/4" />
                <Skeleton className="h-4 w-1/2" />
              </CardContent>
            </Card>
          </div>
          <div className="space-y-6">
            <Card>
              <CardHeader>
                <Skeleton className="h-6 w-24" />
              </CardHeader>
              <CardContent>
                <Skeleton className="h-20 w-full" />
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="space-y-6">
        <div className="flex items-center gap-4">
          <Link href="/admin/products">
            <Button variant="outline" size="sm">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Quay lại
            </Button>
          </Link>
          <div>
            <h1 className="text-3xl font-bold">Li</h1>
          </div>
        </div>
        <Card>
          <CardContent className="pt-6">
            <div className="flex items-center gap-2 text-red-600">
              <AlertCircle className="h-5 w-5" />
              <span>Khng th ti Thường tin sản phẩm: {(error as any)?.message || 'Có lỗi xảy ra'}</span>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <PermissionGuard permission={PERMISSIONS.PRODUCTS_READ}>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4">
            <Link href="/admin/products">
              <Button variant="outline" size="sm">
                <ArrowLeft className="h-4 w-4 mr-2" />
                Quay lại
              </Button>
            </Link>
            <div>
              <h1 className="text-3xl font-bold">{product?.name}</h1>
              <p className="text-muted-foreground">
                Chi tit sản phẩm #{productId}
              </p>
            </div>
          </div>

          <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
            <Link href={`/admin/products/${productId}`}>
              <Button>
                <Edit className="h-4 w-4 mr-2" />
                Chỉnh sửa
              </Button>
            </Link>
          </PermissionGuard>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Main Content */}
          <div className="lg:col-span-2 space-y-6">
            {/* Basic Information */}
            <Card>
              <CardHeader>
                <CardTitle>Thông tin cơ bản</CardTitle>
                <CardDescription>
                  Thông tin cơ bản ca sản phẩm
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="grid grid-cols-2 gap-4">
                  <div className="space-y-2">
                    <div className="flex items-center gap-2">
                      <Hash className="h-4 w-4 text-muted-foreground" />
                      <span className="font-medium">SKU:</span>
                    </div>
                    <code className="bg-muted px-2 py-1 rounded text-sm">
                      {product?.sku}
                    </code>
                  </div>
                  <div className="space-y-2">
                    <div className="flex items-center gap-2">
                      <Building className="h-4 w-4 text-muted-foreground" />
                      <span className="font-medium">Thương hiệu:</span>
                    </div>
                    <p>{product?.brand}</p>
                  </div>
                  <div className="space-y-2">
                    <div className="flex items-center gap-2">
                      <Tag className="h-4 w-4 text-muted-foreground" />
                      <span className="font-medium">Loi sản phẩm:</span>
                    </div>
                    <p>{product?.type}</p>
                  </div>
                  <div className="space-y-2">
                    <div className="flex items-center gap-2">
                      <DollarSign className="h-4 w-4 text-muted-foreground" />
                      <span className="font-medium">Giá bán:</span>
                    </div>
                    <p className="text-lg font-semibold text-green-600">
                      {formatCurrency(product?.price || 0)}
                    </p>
                  </div>
                </div>

                <Separator />

                <div className="space-y-2">
                  <span className="font-medium">M t:</span>
                  <p className="text-muted-foreground">
                    {product?.description || 'Không có mô tả'}
                  </p>
                </div>

                {product?.shortDescription && (
                  <>
                    <Separator />
                    <div className="space-y-2">
                      <span className="font-medium">Mô tả ngắn:</span>
                      <p className="text-muted-foreground">
                        {product.shortDescription}
                      </p>
                    </div>
                  </>
                )}
              </CardContent>
            </Card>

            {/* Product Images */}
            <Card>
              <CardHeader>
                <CardTitle>Hnh nh sản phẩm</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                  {product?.images?.map((image, index) => (
                    <div key={index} className="aspect-square bg-muted rounded-lg overflow-hidden">
                      <img
                        src={image.imageUrl}
                        alt={`${product.name} - ${index + 1}`}
                        className="w-full h-full object-cover"
                      />
                    </div>
                  )) || (
                      <div className="aspect-square bg-muted rounded-lg flex items-center justify-center">
                        <Package className="h-12 w-12 text-muted-foreground" />
                      </div>
                    )}
                </div>
              </CardContent>
            </Card>

            {/* Specifications */}
            {product?.specifications && product.specifications.length > 0 && (
              <Card>
                <CardHeader>
                  <CardTitle>Thường s k thut</CardTitle>
                </CardHeader>
                <CardContent>
                  <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                    {product.specifications.map((spec, index) => (
                      <div key={index} className="flex justify-between py-2 border-b">
                        <span className="font-medium">{spec.name}:</span>
                        <span className="text-muted-foreground">{spec.value}</span>
                      </div>
                    ))}
                  </div>
                </CardContent>
              </Card>
            )}
          </div>

          {/* Sidebar */}
          <div className="space-y-6">
            {/* Status */}
            <Card>
              <CardHeader>
                <CardTitle>Trạng thái</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <span className="font-medium">Hot ng:</span>
                  {product?.isActive ? (
                    <Badge variant="default">đang bán</Badge>
                  ) : (
                    <Badge variant="secondary">Tạm ngưng</Badge>
                  )}
                </div>

                <Separator />

                <div className="space-y-2">
                  <span className="font-medium">Tồn kho:</span>
                  {getStockBadge(product?.stockQuantity || 0)}
                </div>

                <Separator />

                <div className="space-y-2">
                  <span className="font-medium">Nổi bật:</span>
                  {product?.isFeatured ? (
                    <Badge variant="default">Nổi bật</Badge>
                  ) : (
                    <Badge variant="outline">Thường</Badge>
                  )}
                </div>
              </CardContent>
            </Card>

            {/* Timestamps */}
            <Card>
              <CardHeader>
                <CardTitle>Thời gian</CardTitle>
              </CardHeader>
              <CardContent className="space-y-4">
                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <Calendar className="h-4 w-4 text-muted-foreground" />
                    <span className="font-medium">Tạo lúc:</span>
                  </div>
                  <p className="text-sm text-muted-foreground">
                    {formatDate(product?.createdAt || '')}
                  </p>
                </div>

                <Separator />

                <div className="space-y-2">
                  <div className="flex items-center gap-2">
                    <Calendar className="h-4 w-4 text-muted-foreground" />
                    <span className="font-medium">Cập nhật lúc:</span>
                  </div>
                  <p className="text-sm text-muted-foreground">
                    {formatDate(product?.updatedAt || '')}
                  </p>
                </div>
              </CardContent>
            </Card>
          </div>
        </div>
      </div>
    </PermissionGuard>
  );
}
