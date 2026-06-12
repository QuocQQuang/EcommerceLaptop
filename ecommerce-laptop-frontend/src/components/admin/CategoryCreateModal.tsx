'use client';

import { Button } from '@/components/ui/button';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { createCategory } from '@/features/admin/catalog/api';
import type { CategoryFormData } from '@/features/admin/catalog/types';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { toast } from 'sonner';

interface CategoryCreateModalProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    onCategoryCreated?: (category: any) => void;
    parentCategories?: any[];
}

export default function CategoryCreateModal({
    open,
    onOpenChange,
    onCategoryCreated,
    parentCategories = []
}: CategoryCreateModalProps) {
    const queryClient = useQueryClient();
    const [formData, setFormData] = useState<CategoryFormData>({
        name: '',
        description: '',
        parentId: '',
        isActive: true
    });

    const createCategoryMutation = useMutation({
        mutationFn: async (categoryData: CategoryFormData) => {
            return await createCategory(categoryData);
        },
        onSuccess: (newCategory) => {
            queryClient.invalidateQueries({ queryKey: ['admin', 'categories'] });
            toast.success('Tạo danh mục thành công!');
            onCategoryCreated?.(newCategory);
            onOpenChange(false);
            resetForm();
        },
        onError: (error: any) => {
            toast.error(`Lỗi khi tạo danh mục: ${error.message || 'Có lỗi xảy ra'}`);
        }
    });

    const resetForm = () => {
        setFormData({
            name: '',
            description: '',
            parentId: '',
            isActive: true
        });
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!formData.name.trim()) {
            toast.error('Vui lòng nhập tên danh mục');
            return;
        }

        await createCategoryMutation.mutateAsync(formData);
    };

    const handleOpenChange = (newOpen: boolean) => {
        if (!newOpen) {
            resetForm();
        }
        onOpenChange(newOpen);
    };

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogContent className="sm:max-w-[425px]">
                <DialogHeader>
                    <DialogTitle>Thêm danh mục mới</DialogTitle>
                    <DialogDescription>
                        Tạo danh mục mới cho sản phẩm
                    </DialogDescription>
                </DialogHeader>

                <form onSubmit={handleSubmit}>
                    <div className="grid gap-4 py-4">
                        <div className="space-y-2">
                            <Label htmlFor="name">Tên danh mục *</Label>
                            <Input
                                id="name"
                                value={formData.name}
                                onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
                                placeholder="Nhập tên danh mục"
                                required
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="description">Mô tả</Label>
                            <Textarea
                                id="description"
                                value={formData.description}
                                onChange={(e) => setFormData(prev => ({ ...prev, description: e.target.value }))}
                                placeholder="Mô tả danh mục"
                                rows={3}
                            />
                        </div>

                        <div className="space-y-2">
                            <Label htmlFor="parentId">Danh mục cha</Label>
                            <select
                                id="parentId"
                                value={formData.parentId}
                                onChange={(e) => setFormData(prev => ({ ...prev, parentId: e.target.value }))}
                                className="w-full px-3 py-2 border rounded-md"
                            >
                                <option value="">Không có (danh mục gốc)</option>
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
                            <Label htmlFor="isActive">Kích hoạt danh mục</Label>
                        </div>
                    </div>

                    <DialogFooter>
                        <Button
                            type="button"
                            variant="outline"
                            onClick={() => handleOpenChange(false)}
                        >
                            Hủy bỏ
                        </Button>
                        <Button
                            type="submit"
                            disabled={!formData.name || createCategoryMutation.isPending}
                        >
                            {createCategoryMutation.isPending ? 'Đang tạo...' : 'Tạo danh mục'}
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    );
}
