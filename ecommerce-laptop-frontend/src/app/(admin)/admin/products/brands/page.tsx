'use client';

import AdvancedDeleteDialog, { DeleteOption } from '@/components/admin/AdvancedDeleteDialog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle
} from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from '@/components/ui/table';
import { Textarea } from '@/components/ui/textarea';
import {
  PermissionGuard,
  useAdminAuth
} from '@/contexts/AdminAuthContext';
import {
  createBrand,
  deleteBrand,
  forceDeleteBrand,
  getBrandsWithCounts,
  reassignAndDeleteBrand,
  updateBrand
} from '@/features/admin/catalog/api';
import type { Brand, BrandFormData } from '@/features/admin/catalog/types';
import { PERMISSIONS } from '@/lib/admin-api';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  AlertCircle,
  ArrowLeft,
  Building2,
  Edit,
  MoreHorizontal,
  Plus,
  Search,
  Trash2
} from 'lucide-react';
import Link from 'next/link';
import React, { useState } from 'react';
import { toast } from 'sonner';

// Real API calls using admin-api.ts
const useBrandsQuery = (search?: string) => {
  return useQuery({
    queryKey: ['admin', 'brands', search],
    queryFn: async (): Promise<Brand[]> => {
      const brands = await getBrandsWithCounts();

      if (search) {
        return brands.filter(brand =>
          brand.name.toLowerCase().includes(search.toLowerCase()) ||
          brand.country?.toLowerCase().includes(search.toLowerCase())
        );
      }

      return brands;
    },
    staleTime: 5 * 60 * 1000,
  });
};

const useCreateBrandMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (brandData: BrandFormData) => {
      return await createBrand(brandData);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'brands'] });
    }
  });
};

const useUpdateBrandMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, data }: { id: number; data: BrandFormData }) => {
      return await updateBrand(id, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'brands'] });
    }
  });
};

const useDeleteBrandMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: number) => {
      return await deleteBrand(id);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'brands'] });
    },
    onError: (error: any) => {
      console.error('Error deleting brand:', error);
    }
  });
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

export default function BrandsPage() {
  const { user } = useAdminAuth();
  const queryClient = useQueryClient();
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [editingBrand, setEditingBrand] = useState<Brand | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [brandToDelete, setBrandToDelete] = useState<Brand | null>(null);

  const [formData, setFormData] = useState<BrandFormData>({
    name: '',
    slug: '',
    description: '',
    website: '',
    country: '',
    isActive: true
  });

  const {
    data: brands,
    isLoading,
    error,
    refetch
  } = useBrandsQuery(searchTerm);

  const createBrandMutation = useCreateBrandMutation();
  const updateBrandMutation = useUpdateBrandMutation();
  const deleteBrandMutation = useDeleteBrandMutation();

  // Advanced delete mutations
  const reassignBrandMutation = useMutation({
    mutationFn: async ({ brandIdToDelete, newBrandId }: { brandIdToDelete: number; newBrandId: number }) => {
      return await reassignAndDeleteBrand(brandIdToDelete, newBrandId);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'brands'] });
    }
  });

  const forceBrandMutation = useMutation({
    mutationFn: async (id: number) => {
      return await forceDeleteBrand(id);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'brands'] });
    }
  });

  const resetForm = () => {
    setFormData({
      name: '',
      slug: '',
      description: '',
      website: '',
      country: '',
      isActive: true
    });
    setEditingBrand(null);
  };

  const handleCreate = () => {
    resetForm();
    setShowCreateDialog(true);
  };

  const handleEdit = (brand: Brand) => {
    setFormData({
      name: brand.name,
      slug: brand.slug,
      description: brand.description || '',
      website: brand.website || '',
      country: brand.country || '',
      isActive: brand.isActive
    });
    setEditingBrand(brand);
    setShowCreateDialog(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      if (editingBrand) {
        await updateBrandMutation.mutateAsync({
          id: editingBrand.id,
          data: formData
        });
      } else {
        await createBrandMutation.mutateAsync(formData);
      }

      setShowCreateDialog(false);
      resetForm();
    } catch (error) {
      console.error('Error saving brand:', error);
    }
  };

  const handleDeleteClick = (brand: Brand) => {
    setBrandToDelete(brand);
    setShowDeleteDialog(true);
  };

  const handleSimpleDelete = async () => {
    if (!brandToDelete) return;

    try {
      await deleteBrandMutation.mutateAsync(brandToDelete.id);
      toast.success(` xóa thương hiệu "${brandToDelete.name}" thành công!`);
    } catch (error: any) {
      const errorMessage = error?.response?.data?.error || error?.message || 'Có lỗi xảy ra khi xóa thương hiệu';
      toast.error(`Không thể xóa thương hiệu "${brandToDelete.name}": ${errorMessage}`);
      throw error;
    }
  };

  const handleReassignAndDelete = async (newBrandId: string) => {
    if (!brandToDelete) return;

    try {
      await reassignBrandMutation.mutateAsync({
        brandIdToDelete: brandToDelete.id,
        newBrandId: parseInt(newBrandId)
      });
      toast.success(` chuyển sản phẩm và xóa thương hiệu "${brandToDelete.name}" thành công!`);
    } catch (error: any) {
      const errorMessage = error?.response?.data?.error || error?.message || 'Có lỗi xảy ra khi chuyển sản phẩm và xóa thương hiệu';
      toast.error(`Không thể chuyển sản phẩm và xóa thương hiệu "${brandToDelete.name}": ${errorMessage}`);
      throw error;
    }
  };

  const handleForceDelete = async () => {
    if (!brandToDelete) return;

    try {
      await forceBrandMutation.mutateAsync(brandToDelete.id);
      toast.success(` xóa bắt buộc thương hiệu "${brandToDelete.name}" và tắt trạng thái các sản phẩm liên quan!`);
    } catch (error: any) {
      const errorMessage = error?.response?.data?.error || error?.message || 'Có lỗi xảy ra khi xóa bắt buộc thương hiệu';
      toast.error(`Không thể xóa bắt buộc thương hiệu "${brandToDelete.name}": ${errorMessage}`);
      throw error;
    }
  };

  // Get available brands for reassignment (excluding the one being deleted)
  const getAvailableBrandsForReassignment = (): DeleteOption[] => {
    if (!brands || !brandToDelete) return [];

    return brands
      .filter(brand => brand.id !== brandToDelete.id)
      .map(brand => ({
        id: brand.id.toString(),
        name: brand.name,
        productCount: brand.productCount
      }));
  };

  if (error) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <AlertCircle className="h-12 w-12 text-red-500 mx-auto mb-4" />
          <h3 className="text-lg font-semibold">Lỗi tải dữ liệu</h3>
          <p className="text-muted-foreground mb-4">
            Không thể tải danh sách thương hiệu. Vui lòng thử lại.
          </p>
          <Button onClick={() => refetch()}>Thử lại</Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4">
          <Link href="/admin/products">
            <Button variant="outline" size="sm">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Quay lại Sản phẩm
            </Button>
          </Link>
          <div>
            <h1 className="text-3xl font-bold">Quản lý thương hiệu</h1>
            <p className="text-muted-foreground">
              Quản lý thương hiệu sản phẩm laptop
            </p>
          </div>
        </div>

        <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
          <Button onClick={handleCreate}>
            <Plus className="h-4 w-4 mr-2" />
            Thêm thương hiệu
          </Button>
        </PermissionGuard>
      </div>

      {/* Search */}
      <Card>
        <CardHeader>
          <CardTitle>Tìm kiếm thương hiệu</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="relative max-w-md">
            <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
            <Input
              placeholder="Tìm theo tên thương hiệu, quốc gia..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-9"
            />
          </div>
        </CardContent>
      </Card>

      {/* Brands Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>
              Danh sách thương hiệu ({brands?.length || 0})
            </CardTitle>
            <Button variant="outline" onClick={() => refetch()}>
              Làm mới
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
                    <Skeleton className="h-4 w-[150px]" />
                  </div>
                  <Skeleton className="h-8 w-[60px]" />
                  <Skeleton className="h-8 w-[100px]" />
                </div>
              ))}
            </div>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Thương hiệu</TableHead>
                  <TableHead>Slug</TableHead>
                  <TableHead>Quốc gia</TableHead>
                  <TableHead>Website</TableHead>
                  <TableHead>Số sản phẩm</TableHead>
                  <TableHead>Trạng thái</TableHead>
                  <TableHead>Cập nhật</TableHead>
                  <TableHead className="text-right">Thao tác</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {brands?.map((brand) => (
                  <TableRow key={brand.id}>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <Building2 className="h-4 w-4 text-muted-foreground" />
                        <div>
                          <div className="font-medium">{brand.name}</div>
                          {brand.description && (
                            <div className="text-sm text-muted-foreground">
                              {brand.description}
                            </div>
                          )}
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <code className="text-sm bg-muted px-2 py-1 rounded">
                        {brand.slug}
                      </code>
                    </TableCell>
                    <TableCell>
                      {brand.country ? (
                        <Badge variant="outline">{brand.country}</Badge>
                      ) : (
                        <span className="text-muted-foreground">-</span>
                      )}
                    </TableCell>
                    <TableCell>
                      {brand.website ? (
                        <a
                          href={brand.website}
                          target="_blank"
                          rel="noopener noreferrer"
                          className="text-blue-600 hover:underline text-sm"
                        >
                          {brand.website.replace('https://', '')}
                        </a>
                      ) : (
                        <span className="text-muted-foreground">-</span>
                      )}
                    </TableCell>
                    <TableCell>
                      <Badge variant="secondary">{brand.productCount}</Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={brand.isActive ? "default" : "secondary"}>
                        {brand.isActive ? 'Hoạt động' : 'Tạm ngưng'}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {formatDate(brand.updatedAt)}
                    </TableCell>
                    <TableCell className="text-right">
                      <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                        <DropdownMenu>
                          <DropdownMenuTrigger asChild>
                            <Button variant="ghost" className="h-8 w-8 p-0">
                              <MoreHorizontal className="h-4 w-4" />
                            </Button>
                          </DropdownMenuTrigger>
                          <DropdownMenuContent align="end">
                            <DropdownMenuItem onClick={() => handleEdit(brand)}>
                              <Edit className="h-4 w-4 mr-2" />
                              Chỉnh sửa
                            </DropdownMenuItem>
                            <PermissionGuard permission={PERMISSIONS.PRODUCTS_DELETE}>
                              <DropdownMenuItem
                                onClick={() => handleDeleteClick(brand)}
                                className="text-red-600"
                              >
                                <Trash2 className="h-4 w-4 mr-2" />
                                Xóa
                              </DropdownMenuItem>
                            </PermissionGuard>
                          </DropdownMenuContent>
                        </DropdownMenu>
                      </PermissionGuard>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      {/* Create/Edit Dialog */}
      <Dialog open={showCreateDialog} onOpenChange={setShowCreateDialog}>
        <DialogContent className="sm:max-w-[500px]">
          <DialogHeader>
            <DialogTitle>
              {editingBrand ? 'Chỉnh sửa thương hiệu' : 'Thêm thương hiệu mới'}
            </DialogTitle>
            <DialogDescription>
              {editingBrand
                ? 'Cập nhật thông tin thương hiệu sản phẩm'
                : 'Tạo thương hiệu mới cho sản phẩm'
              }
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSubmit}>
            <div className="grid gap-4 py-4">
              <div className="grid grid-cols-2 gap-4">
                <div className="space-y-2">
                  <Label htmlFor="name">Tên thương hiệu *</Label>
                  <Input
                    id="name"
                    value={formData.name}
                    onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                    placeholder="Nhập tên thương hiệu"
                    required
                  />
                </div>

                <div className="space-y-2">
                  <Label htmlFor="country">Quốc gia</Label>
                  <Input
                    id="country"
                    value={formData.country}
                    onChange={(e) => setFormData(prev => ({ ...prev, country: e.target.value }))}
                    placeholder="VD: Mỹ, Đài Loan"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <Label htmlFor="website">Website</Label>
                <Input
                  id="website"
                  type="url"
                  value={formData.website}
                  onChange={(e) => setFormData(prev => ({ ...prev, website: e.target.value }))}
                  placeholder="https://example.com"
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">Mô tả</Label>
                <Textarea
                  id="description"
                  value={formData.description}
                  onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
                  placeholder="Mô tả về thương hiệu"
                  rows={3}
                />
              </div>

              <div className="flex items-center space-x-2">
                <input
                  type="checkbox"
                  id="isActive"
                  checked={formData.isActive}
                  onChange={(e) => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
                />
                <Label htmlFor="isActive">Kích hoạt thương hiệu</Label>
              </div>
            </div>

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setShowCreateDialog(false)}
              >
                Hủy bỏ
              </Button>
              <Button
                type="submit"
                disabled={!formData.name || createBrandMutation.isPending || updateBrandMutation.isPending}
              >
                {createBrandMutation.isPending || updateBrandMutation.isPending ? (
                  editingBrand ? 'đang cập nhật...' : 'đang tạo...'
                ) : (
                  editingBrand ? 'Cập nhật' : 'Tạo thương hiệu'
                )}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Advanced Delete Dialog */}
      <AdvancedDeleteDialog
        open={showDeleteDialog}
        onOpenChange={setShowDeleteDialog}
        itemName={brandToDelete?.name || ''}
        itemType="brand"
        productCount={brandToDelete?.productCount || 0}
        availableOptions={getAvailableBrandsForReassignment()}
        onSimpleDelete={handleSimpleDelete}
        onReassignAndDelete={handleReassignAndDelete}
        onForceDelete={handleForceDelete}
        isLoading={deleteBrandMutation.isPending || reassignBrandMutation.isPending || forceBrandMutation.isPending}
      />
    </div>
  );
}









