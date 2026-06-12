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
import { BrandFormData, createBrand } from '@/lib/admin-api';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { toast } from 'sonner';

interface BrandCreateModalProps {
    open: boolean;
    onOpenChange: (open: boolean) => void;
    onBrandCreated?: (brand: any) => void;
}

export default function BrandCreateModal({
    open,
    onOpenChange,
    onBrandCreated
}: BrandCreateModalProps) {
    const queryClient = useQueryClient();
    const [formData, setFormData] = useState<BrandFormData>({
        name: '',
        slug: '',
        country: '',
        website: '',
        description: '',
        isActive: true
    });

    const createBrandMutation = useMutation({
        mutationFn: async (brandData: BrandFormData) => {
            return await createBrand(brandData);
        },
        onSuccess: (newBrand) => {
            queryClient.invalidateQueries({ queryKey: ['admin', 'brands'] });
            toast.success('To thng hiu thnh cng!');
            onBrandCreated?.(newBrand);
            onOpenChange(false);
            resetForm();
        },
        onError: (error: any) => {
            toast.error(`Li khi to thng hiu: ${error.message || 'C li xy ra'}`);
        }
    });

    const resetForm = () => {
        setFormData({
            name: '',
            slug: '',
            country: '',
            website: '',
            description: '',
            isActive: true
        });
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        if (!formData.name.trim()) {
            toast.error('Vui lòng nhập tên thương hiệu');
            return;
        }

        // Auto-generate slug if not provided
        if (!formData.slug.trim()) {
            formData.slug = formData.name.toLowerCase().replace(/\s+/g, '-');
        }

        await createBrandMutation.mutateAsync(formData);
    };

    const handleOpenChange = (newOpen: boolean) => {
        if (!newOpen) {
            resetForm();
        }
        onOpenChange(newOpen);
    };

    return (
        <Dialog open={open} onOpenChange={handleOpenChange}>
            <DialogContent className="sm:max-w-[500px]">
                <DialogHeader>
                    <DialogTitle>Thêm thương hiệu mới</DialogTitle>
                    <DialogDescription>
                        Tạo thương hiệu mới cho sản phẩm
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
                            onClick={() => handleOpenChange(false)}
                        >
                            Hủy bỏ
                        </Button>
                        <Button
                            type="submit"
                            disabled={!formData.name || createBrandMutation.isPending}
                        >
                            {createBrandMutation.isPending ? 'Đang tạo...' : 'Tạo thương hiệu'}
                        </Button>
                    </DialogFooter>
                </form>
            </DialogContent>
        </Dialog>
    );
}
