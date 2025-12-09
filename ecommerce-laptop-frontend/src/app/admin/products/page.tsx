'use client';

import { ExportButtons } from '@/components/admin/ExportButtons';
import { SortableTableHeader, SortConfig } from '@/components/admin/SortableTableHeader';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle
} from '@/components/ui/card';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from '@/components/ui/table';
import {
  PermissionGuard,
  useAdminAuth
} from '@/contexts/AdminAuthContext';
import { deleteProduct, getAdminProducts, PERMISSIONS } from '@/lib/admin-api';
import { Product as ApiProduct } from '@/types/api';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  AlertCircle,
  ArrowLeft,
  BarChart3,
  Edit,
  Eye,
  Filter,
  MoreHorizontal,
  Package,
  Plus,
  Search,
  Trash2
} from 'lucide-react';
import Link from 'next/link';
import { useState } from 'react';
import { toast } from 'sonner';

// Types for products
interface Product {
  id: number;
  name: string;
  sku: string;
  category: string;
  brand: string;
  price: number;
  stock: number;
  status: 'active' | 'inactive' | 'out_of_stock';
  createdAt: string;
  updatedAt: string;
  // Variant support
  isBaseProduct?: boolean;
  isVariant?: boolean;
  parentProductId?: number;
  variantCount?: number;
  variantName?: string;
  variantSku?: string;
}

interface ProductsResponse {
  products: Product[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
  pageSize: number;
}

// Helper function to map API Product to local Product interface
const mapApiProductToProduct = (apiProduct: ApiProduct): Product => {
  // Extract stock quantity from inventory object (availableQuantity) or fallback to stockQuantity
  const stockQuantity = apiProduct.inventory?.availableQuantity || apiProduct.stockQuantity || 0;

  return {
    id: apiProduct.id,
    name: apiProduct.name,
    sku: apiProduct.sku,
    category: apiProduct.categories?.[0]?.name || apiProduct.type,
    brand: apiProduct.brand,
    price: apiProduct.price,
    stock: stockQuantity,
    status: apiProduct.isActive ? 'active' : 'inactive',
    createdAt: apiProduct.createdAt,
    updatedAt: apiProduct.updatedAt,
    // Variant support
    isBaseProduct: apiProduct.isBaseProduct || false,
    isVariant: apiProduct.isVariant || false,
    parentProductId: apiProduct.parentProductId,
    variantCount: apiProduct.variants?.length || 0,
    variantName: apiProduct.variantName,
    variantSku: apiProduct.variantSku
  };
};

// Real API call using admin-api
const useProductsQuery = (params: {
  page?: number;
  limit?: number;
  search?: string;
  category?: string;
  status?: string;
  productType?: 'all' | 'base' | 'variant';
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}) => {
  return useQuery({
    queryKey: ['admin', 'products', params],
    queryFn: async (): Promise<ProductsResponse> => {
      const apiParams = {
        page: params.page || 1,
        limit: params.limit || 20,
        search: params.search,
        // Map status to isActive
        isActive: params.status === 'active' ? true : params.status === 'inactive' ? false : undefined,
        productType: params.productType,
        sortBy: params.sortBy,
        sortOrder: params.sortOrder
      };

      const response = await getAdminProducts(apiParams);
      return {
        products: response.products.map(mapApiProductToProduct),
        totalCount: response.totalCount,
        currentPage: response.currentPage,
        totalPages: response.totalPages,
        pageSize: response.pageSize
      };
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    refetchInterval: 60 * 1000, // 1 minute
  });
};

// Currency formatting utilities - now using the new currency system
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { formatCurrencyPrice } from '@/lib/currency';

const formatCurrency = (amount: number, selectedCurrency: string) => {
  return formatCurrencyPrice(amount, selectedCurrency as any);
};

const formatDate = (dateString: string) => {
  return new Intl.DateTimeFormat('vi-VN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(dateString));
};

const getStatusBadge = (status: string, stock: number) => {
  if (stock === 0) {
    return <Badge variant="destructive">Ht hng</Badge>;
  }

  switch (status) {
    case 'active':
      return <Badge variant="default">ang bn</Badge>;
    case 'inactive':
      return <Badge variant="secondary">Tm ngng</Badge>;
    case 'out_of_stock':
      return <Badge variant="destructive">Ht hng</Badge>;
    default:
      return <Badge variant="outline">Khng xc nh</Badge>;
  }
};

const getStockBadge = (stock: number) => {
  if (stock === 0) {
    return <Badge variant="destructive">0</Badge>;
  } else if (stock < 5) {
    return <Badge variant="outline" className="text-orange-600">{stock}</Badge>;
  } else if (stock < 10) {
    return <Badge variant="outline" className="text-yellow-600">{stock}</Badge>;
  } else {
    return <Badge variant="outline" className="text-green-600">{stock}</Badge>;
  }
};

export default function ProductsPage() {
  const { selectedCurrency } = useCurrencyContext();
  const { user } = useAdminAuth();
  const queryClient = useQueryClient();
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedCategory, setSelectedCategory] = useState('');
  const [selectedStatus, setSelectedStatus] = useState('');
  const [selectedProductType, setSelectedProductType] = useState<'all' | 'base' | 'variant'>('all');
  const [sortConfig, setSortConfig] = useState<SortConfig | null>(null);

  const {
    data: productsData,
    isLoading,
    error,
    refetch
  } = useProductsQuery({
    search: searchTerm,
    category: selectedCategory,
    status: selectedStatus,
    productType: selectedProductType,
    sortBy: sortConfig?.field,
    sortOrder: sortConfig?.direction || undefined
  });

  const deleteProductMutation = useMutation({
    mutationFn: (productId: number) => deleteProduct(productId),
    onSuccess: () => {
      toast.success('Sn phm  c xa thnh cng!');
      queryClient.invalidateQueries({ queryKey: ['admin', 'products'] });
    },
    onError: (error: any) => {
      toast.error(`Li xa sn phm: ${error.message || 'C li xy ra'}`);
    }
  });

  const handleDeleteProduct = async (productId: number) => {
    if (window.confirm('Bn c chc chn mun xa sn phm ny? Hnh ng ny khng th hon tc.')) {
      deleteProductMutation.mutate(productId);
    }
  };

  const handleSort = (field: string) => {
    setSortConfig(prevConfig => {
      if (prevConfig?.field === field) {
        // Toggle direction if same field
        const newDirection = prevConfig.direction === 'asc' ? 'desc' : prevConfig.direction === 'desc' ? null : 'asc';
        return newDirection ? { field, direction: newDirection } : null;
      }
      // New field, start with ascending
      return { field, direction: 'asc' };
    });
  };

  if (error) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <AlertCircle className="h-12 w-12 text-red-500 mx-auto mb-4" />
          <h3 className="text-lg font-semibold">Li ti d liu</h3>
          <p className="text-muted-foreground mb-4">
            Khng th ti danh sch sn phm. Vui lng th li.
          </p>
          <Button onClick={() => refetch()}>Th li</Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Qun l sn phm</h1>
          <p className="text-muted-foreground">
            Qun l danh mc sn phm laptop v ph kin
          </p>
        </div>

        <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
          <div className="flex gap-2">
            <ExportButtons type="products" options={{ search: searchTerm, isActive: selectedStatus === 'active' ? true : selectedStatus === 'inactive' ? false : undefined }} className="flex items-center" />
            <Link href="/admin/products/categories">
              <Button variant="outline">
                <Filter className="h-4 w-4 mr-2" />
                Danh mc
              </Button>
            </Link>
            <Link href="/admin/products/brands">
              <Button variant="outline">
                <Package className="h-4 w-4 mr-2" />
                Thng hiu
              </Button>
            </Link>
            <Link href="/admin/products/bundles">
              <Button variant="outline">
                <Package className="h-4 w-4 mr-2" />
                Bundles
              </Button>
            </Link>
            <Link href="/admin/products/analytics">
              <Button variant="outline">
                <BarChart3 className="h-4 w-4 mr-2" />
                Analytics
              </Button>
            </Link>
            <Link href="/admin/products/add">
              <Button>
                <Plus className="h-4 w-4 mr-2" />
                Thm sn phm
              </Button>
            </Link>
          </div>
        </PermissionGuard>
      </div>

      {/* Filters */}
      <Card>
        <CardHeader>
          <CardTitle>Tm kim v lc</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex gap-4 flex-wrap">
            <div className="flex-1 min-w-[200px]">
              <div className="relative">
                <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                <Input
                  placeholder="Tm theo tn sn phm, SKU..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-9"
                />
              </div>
            </div>

            <select
              value={selectedCategory}
              onChange={(e) => setSelectedCategory(e.target.value)}
              className="px-3 py-2 border rounded-md"
            >
              <option value="">Tt c danh mc</option>
              <option value="Laptop">Laptop</option>
              <option value="Gaming Laptop">Gaming Laptop</option>
              <option value="Ultrabook">Ultrabook</option>
              <option value="Accessories">Ph kin</option>
            </select>

            <select
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(e.target.value)}
              className="px-3 py-2 border rounded-md"
            >
              <option value="">Tt c trng thi</option>
              <option value="active">ang bn</option>
              <option value="inactive">Tm ngng</option>
              <option value="out_of_stock">Ht hng</option>
            </select>

            <select
              value={selectedProductType}
              onChange={(e) => setSelectedProductType(e.target.value as 'all' | 'base' | 'variant')}
              className="px-3 py-2 border rounded-md"
            >
              <option value="all">Tt c loi</option>
              <option value="base">Sn phm gc</option>
              <option value="variant">Variants</option>
            </select>
          </div>
        </CardContent>
      </Card>

      {/* Products Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>
              Danh sch sn phm ({productsData?.totalCount || 0})
            </CardTitle>
            <Button variant="outline" onClick={() => refetch()}>
              Lm mi
            </Button>
          </div>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3">
              {[...Array(5)].map((_, i) => (
                <div key={i} className="flex items-center space-x-4">
                  <Skeleton className="h-12 w-12" />
                  <div className="space-y-2 flex-1">
                    <Skeleton className="h-4 w-[200px]" />
                    <Skeleton className="h-4 w-[100px]" />
                  </div>
                  <Skeleton className="h-8 w-[80px]" />
                  <Skeleton className="h-8 w-[100px]" />
                </div>
              ))}
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>
                    <SortableTableHeader
                      field="name"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Sn phm
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="sku"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      SKU
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>Loi</TableHead>
                  <TableHead>Variants</TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="category"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Danh mc
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="brand"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Thng hiu
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="price"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Gi bn
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="stock"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Tn kho
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="status"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Trng thi
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead>
                    <SortableTableHeader
                      field="updatedAt"
                      sortConfig={sortConfig}
                      onSort={handleSort}
                    >
                      Cp nht
                    </SortableTableHeader>
                  </TableHead>
                  <TableHead className="text-right">Thao tc</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {productsData?.products?.map((product) => (
                  <TableRow key={product.id}>
                    <TableCell>
                      <div className="font-medium">{product.name}</div>
                      {product.isVariant && product.variantName && (
                        <div className="text-sm text-muted-foreground">
                          {product.variantName}
                        </div>
                      )}
                    </TableCell>
                    <TableCell>
                      <code className="text-sm bg-muted px-2 py-1 rounded">
                        {product.sku}
                      </code>
                      {product.isVariant && product.variantSku && (
                        <div className="text-xs text-muted-foreground mt-1">
                          {product.variantSku}
                        </div>
                      )}
                    </TableCell>
                    <TableCell>
                      {product.isBaseProduct ? (
                        <Badge variant="default" className="bg-blue-100 text-blue-800">
                          Base
                        </Badge>
                      ) : product.isVariant ? (
                        <Badge variant="secondary" className="bg-green-100 text-green-800">
                          Variant
                        </Badge>
                      ) : (
                        <Badge variant="outline">Standard</Badge>
                      )}
                    </TableCell>
                    <TableCell>
                      {product.isBaseProduct && product.variantCount ? (
                        <Badge variant="outline" className="bg-purple-100 text-purple-800">
                          {product.variantCount} variants
                        </Badge>
                      ) : product.isVariant ? (
                        <span className="text-sm text-muted-foreground">-</span>
                      ) : (
                        <span className="text-sm text-muted-foreground">0</span>
                      )}
                    </TableCell>
                    <TableCell>{product.category}</TableCell>
                    <TableCell>{product.brand}</TableCell>
                    <TableCell>{formatCurrency(product.price, selectedCurrency)}</TableCell>
                    <TableCell>{getStockBadge(product.stock)}</TableCell>
                    <TableCell>{getStatusBadge(product.status, product.stock)}</TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {formatDate(product.updatedAt)}
                    </TableCell>
                    <TableCell className="text-right">
                      <DropdownMenu>
                        <DropdownMenuTrigger asChild>
                          <Button variant="ghost" className="h-8 w-8 p-0">
                            <MoreHorizontal className="h-4 w-4" />
                          </Button>
                        </DropdownMenuTrigger>
                        <DropdownMenuContent align="end">
                          <DropdownMenuItem asChild>
                            <Link href={`/admin/products/${product.id}/view`}>
                              <Eye className="h-4 w-4 mr-2" />
                              Xem chi tit
                            </Link>
                          </DropdownMenuItem>
                          <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                            <DropdownMenuItem asChild>
                              <Link href={`/admin/products/${product.id}`}>
                                <Edit className="h-4 w-4 mr-2" />
                                Chnh sa
                              </Link>
                            </DropdownMenuItem>
                          </PermissionGuard>
                          {product.isBaseProduct && (
                            <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                              <DropdownMenuItem asChild>
                                <Link href={`/admin/products/${product.id}/variants`}>
                                  <Package className="h-4 w-4 mr-2" />
                                  Qun l Variants
                                </Link>
                              </DropdownMenuItem>
                            </PermissionGuard>
                          )}
                          {product.isVariant && product.parentProductId && (
                            <DropdownMenuItem asChild>
                              <Link href={`/admin/products/${product.parentProductId}`}>
                                <ArrowLeft className="h-4 w-4 mr-2" />
                                Xem sn phm gc
                              </Link>
                            </DropdownMenuItem>
                          )}
                          <PermissionGuard permission={PERMISSIONS.PRODUCTS_DELETE}>
                            <DropdownMenuItem
                              onClick={() => handleDeleteProduct(product.id)}
                              className="text-red-600"
                            >
                              <Trash2 className="h-4 w-4 mr-2" />
                              Xa
                            </DropdownMenuItem>
                          </PermissionGuard>
                        </DropdownMenuContent>
                      </DropdownMenu>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>
    </div>
  );
}