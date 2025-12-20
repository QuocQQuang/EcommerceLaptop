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
  DialogTitle
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
  Category,
  CategoryFormData,
  PERMISSIONS,
  createCategory,
  deleteCategory,
  forceDeleteCategory,
  getCategoriesWithCounts,
  reassignAndDeleteCategory,
  updateCategory
} from '@/lib/admin-api';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  AlertCircle,
  ArrowLeft,
  Edit,
  Folder,
  MoreHorizontal,
  Plus,
  Trash2
} from 'lucide-react';
import Link from 'next/link';
import React, { useState } from 'react';
import { toast } from 'sonner';

// Real API calls using admin-api.ts
const useCategoriesQuery = () => {
  return useQuery({
    queryKey: ['admin', 'categories'],
    queryFn: async (): Promise<Category[]> => {
      return await getCategoriesWithCounts();
    },
    staleTime: 5 * 60 * 1000,
  });
};

const useCreateCategoryMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (categoryData: CategoryFormData) => {
      return await createCategory(categoryData);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
    }
  });
};

const useUpdateCategoryMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, data }: { id: number; data: CategoryFormData }) => {
      return await updateCategory(id, data);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
    }
  });
};

const useDeleteCategoryMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (id: number) => {
      return await deleteCategory(id);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
    },
    onError: (error: any) => {
      console.error('Error deleting category:', error);
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

export default function CategoriesPage() {
  const { user } = useAdminAuth();
  const queryClient = useQueryClient();
  const [showCreateDialog, setShowCreateDialog] = useState(false);
  const [editingCategory, setEditingCategory] = useState<Category | null>(null);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);
  const [categoryToDelete, setCategoryToDelete] = useState<Category | null>(null);

  const [formData, setFormData] = useState<CategoryFormData>({
    name: '',
    description: '',
    parentId: '',
    isActive: true
  });

  const {
    data: categories,
    isLoading,
    error,
    refetch
  } = useCategoriesQuery();

  const createCategoryMutation = useCreateCategoryMutation();
  const updateCategoryMutation = useUpdateCategoryMutation();
  const deleteCategoryMutation = useDeleteCategoryMutation();

  // Advanced delete mutations
  const reassignCategoryMutation = useMutation({
    mutationFn: async ({ categoryIdToDelete, newCategoryId }: { categoryIdToDelete: number; newCategoryId: number }) => {
      return await reassignAndDeleteCategory(categoryIdToDelete, newCategoryId);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
    }
  });

  const forceCategoryMutation = useMutation({
    mutationFn: async (id: number) => {
      return await forceDeleteCategory(id);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
    }
  });

  const resetForm = () => {
    setFormData({
      name: '',
      description: '',
      parentId: '',
      isActive: true
    });
    setEditingCategory(null);
  };

  const handleCreate = () => {
    resetForm();
    setShowCreateDialog(true);
  };

  const handleEdit = (category: Category) => {
    setFormData({
      name: category.name,
      description: category.description || '',
      parentId: category.parentId?.toString() || '',
      isActive: category.isActive
    });
    setEditingCategory(category);
    setShowCreateDialog(true);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    try {
      if (editingCategory) {
        await updateCategoryMutation.mutateAsync({
          id: editingCategory.id,
          data: formData
        });
      } else {
        await createCategoryMutation.mutateAsync(formData);
      }

      setShowCreateDialog(false);
      resetForm();
    } catch (error) {
      console.error('Error saving category:', error);
    }
  };

  const handleDeleteClick = (category: Category) => {
    setCategoryToDelete(category);
    setShowDeleteDialog(true);
  };

  const handleSimpleDelete = async () => {
    if (!categoryToDelete) return;

    try {
      await deleteCategoryMutation.mutateAsync(categoryToDelete.id);
      toast.success(` xa danh mc "${categoryToDelete.name}" thnh cng!`);
    } catch (error: any) {
      const errorMessage = error?.response?.data?.error || error?.message || 'C li xy ra khi xa danh mc';
      toast.error(`Khng th xa danh mc "${categoryToDelete.name}": ${errorMessage}`);
      throw error;
    }
  };

  const handleReassignAndDelete = async (newCategoryId: string) => {
    if (!categoryToDelete) return;

    try {
      await reassignCategoryMutation.mutateAsync({
        categoryIdToDelete: categoryToDelete.id,
        newCategoryId: parseInt(newCategoryId)
      });
      toast.success(` chuyn sn phm v xa danh mc "${categoryToDelete.name}" thnh cng!`);
    } catch (error: any) {
      const errorMessage = error?.response?.data?.error || error?.message || 'C li xy ra khi chuyn sn phm v xa danh mc';
      toast.error(`Khng th chuyn sn phm v xa danh mc "${categoryToDelete.name}": ${errorMessage}`);
      throw error;
    }
  };

  const handleForceDelete = async () => {
    if (!categoryToDelete) return;

    try {
      await forceCategoryMutation.mutateAsync(categoryToDelete.id);
      toast.success(` xa p buc danh mc "${categoryToDelete.name}" v x l cc mc lin quan!`);
    } catch (error: any) {
      const errorMessage = error?.response?.data?.error || error?.message || 'C li xy ra khi xa p buc danh mc';
      toast.error(`Khng th xa p buc danh mc "${categoryToDelete.name}": ${errorMessage}`);
      throw error;
    }
  };

  // Get available categories for reassignment (excluding the one being deleted and its children)
  const getAvailableCategoriesForReassignment = (): DeleteOption[] => {
    if (!categories || !categoryToDelete) return [];

    return categories
      .filter(category =>
        category.id !== categoryToDelete.id &&
        category.parentId !== categoryToDelete.id // Don't show children of the category being deleted
      )
      .map(category => ({
        id: category.id.toString(),
        name: category.parentName ? `${category.parentName} > ${category.name}` : category.name,
        productCount: category.productCount
      }));
  };

  const parentCategories = categories?.filter(cat => !cat.parentId) || [];

  if (error) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <AlertCircle className="h-12 w-12 text-red-500 mx-auto mb-4" />
          <h3 className="text-lg font-semibold">Li ti d liu</h3>
          <p className="text-muted-foreground mb-4">
            Khng th ti danh sch danh mc. Vui lng th li.
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
        <div className="flex items-center gap-4">
          <Link href="/admin/products">
            <Button variant="outline" size="sm">
              <ArrowLeft className="h-4 w-4 mr-2" />
              Quay li Sn phm
            </Button>
          </Link>
          <div>
            <h1 className="text-3xl font-bold">Qun l danh mc</h1>
            <p className="text-muted-foreground">
              Qun l danh mc sn phm laptop
            </p>
          </div>
        </div>

        <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
          <Button onClick={handleCreate}>
            <Plus className="h-4 w-4 mr-2" />
            Thm danh mc
          </Button>
        </PermissionGuard>
      </div>

      {/* Categories Table */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <CardTitle>
              Danh sch danh mc ({categories?.length || 0})
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
                  <TableHead>Tn danh mc</TableHead>
                  <TableHead>Slug</TableHead>
                  <TableHead>Danh mc cha</TableHead>
                  <TableHead>S sn phm</TableHead>
                  <TableHead>Trng thi</TableHead>
                  <TableHead>Cp nht</TableHead>
                  <TableHead className="text-right">Thao tc</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {categories?.map((category) => (
                  <TableRow key={category.id}>
                    <TableCell>
                      <div className="flex items-center gap-2">
                        <Folder className="h-4 w-4 text-muted-foreground" />
                        <div>
                          <div className="font-medium">{category.name}</div>
                          {category.description && (
                            <div className="text-sm text-muted-foreground">
                              {category.description}
                            </div>
                          )}
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      <code className="text-sm bg-muted px-2 py-1 rounded">
                        {category.slug}
                      </code>
                    </TableCell>
                    <TableCell>
                      {category.parentName ? (
                        <Badge variant="outline">{category.parentName}</Badge>
                      ) : (
                        <span className="text-muted-foreground">-</span>
                      )}
                    </TableCell>
                    <TableCell>
                      <Badge variant="secondary">{category.productCount}</Badge>
                    </TableCell>
                    <TableCell>
                      <Badge variant={category.isActive ? "default" : "secondary"}>
                        {category.isActive ? 'Hot ng' : 'Tm ngng'}
                      </Badge>
                    </TableCell>
                    <TableCell className="text-sm text-muted-foreground">
                      {formatDate(category.updatedAt)}
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
                            <DropdownMenuItem onClick={() => handleEdit(category)}>
                              <Edit className="h-4 w-4 mr-2" />
                              Chnh sa
                            </DropdownMenuItem>
                            <PermissionGuard permission={PERMISSIONS.PRODUCTS_DELETE}>
                              <DropdownMenuItem
                                onClick={() => handleDeleteClick(category)}
                                className="text-red-600"
                              >
                                <Trash2 className="h-4 w-4 mr-2" />
                                Xa
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
        <DialogContent className="sm:max-w-[425px]">
          <DialogHeader>
            <DialogTitle>
              {editingCategory ? 'Chnh sa danh mc' : 'Thm danh mc mi'}
            </DialogTitle>
            <DialogDescription>
              {editingCategory
                ? 'Cp nht thng tin danh mc sn phm'
                : 'To danh mc mi cho sn phm'
              }
            </DialogDescription>
          </DialogHeader>

          <form onSubmit={handleSubmit}>
            <div className="grid gap-4 py-4">
              <div className="space-y-2">
                <Label htmlFor="name">Tn danh mc *</Label>
                <Input
                  id="name"
                  value={formData.name}
                  onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                  placeholder="Nhp tn danh mc"
                  required
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="description">M t</Label>
                <Textarea
                  id="description"
                  value={formData.description}
                  onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
                  placeholder="M t danh mc"
                  rows={3}
                />
              </div>

              <div className="space-y-2">
                <Label htmlFor="parentId">Danh mc cha</Label>
                <select
                  id="parentId"
                  value={formData.parentId}
                  onChange={(e) => setFormData(prev => ({ ...prev, parentId: e.target.value }))}
                  className="w-full px-3 py-2 border rounded-md"
                >
                  <option value="">Khng c (danh mc gc)</option>
                  {parentCategories.map((category) => (
                    <option key={category.id} value={category.id.toString()}>
                      {category.name}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex items-center space-x-2">
                <input
                  type="checkbox"
                  id="isActive"
                  checked={formData.isActive}
                  onChange={(e) => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
                />
                <Label htmlFor="isActive">Kch hot danh mc</Label>
              </div>
            </div>

            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setShowCreateDialog(false)}
              >
                Hy b
              </Button>
              <Button
                type="submit"
                disabled={!formData.name || createCategoryMutation.isPending || updateCategoryMutation.isPending}
              >
                {createCategoryMutation.isPending || updateCategoryMutation.isPending ? (
                  editingCategory ? 'ang cp nht...' : 'ang to...'
                ) : (
                  editingCategory ? 'Cp nht' : 'To danh mc'
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
        itemName={categoryToDelete?.name || ''}
        itemType="category"
        productCount={categoryToDelete?.productCount || 0}
        childCount={categories?.filter(c => c.parentId === categoryToDelete?.id).length || 0}
        availableOptions={getAvailableCategoriesForReassignment()}
        onSimpleDelete={handleSimpleDelete}
        onReassignAndDelete={handleReassignAndDelete}
        onForceDelete={handleForceDelete}
        isLoading={deleteCategoryMutation.isPending || reassignCategoryMutation.isPending || forceCategoryMutation.isPending}
      />
    </div>
  );
}