'use client';

import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '@/components/ui/alert-dialog';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
import { Textarea } from '@/components/ui/textarea';
import { cn } from '@/lib/utils';
import { blogService } from '@/services/blogService';
import { BlogCategory, CreateCategoryRequest, UpdateCategoryRequest } from '@/types/api';
import {
    Edit2,
    FolderOpen,
    Loader2,
    Plus,
    Search,
    Trash2
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';

interface CategoryModalState {
    isOpen: boolean;
    mode: 'create' | 'edit';
    category: Partial<BlogCategory>;
    errors: Record<string, string>;
}

interface CategoriesPageState {
    categories: BlogCategory[];
    loading: boolean;
    searchTerm: string;
    modalState: CategoryModalState;
    deletingCategory: BlogCategory | null;
    saving: boolean;
}

export default function CategoriesPage() {
    const [state, setState] = useState<CategoriesPageState>({
        categories: [],
        loading: true,
        searchTerm: '',
        modalState: {
            isOpen: false,
            mode: 'create',
            category: { name: '', description: '', slug: '', metaTitle: '', metaDescription: '', isActive: true, sortOrder: 0 },
            errors: {},
        },
        deletingCategory: null,
        saving: false,
    });

    // Load categories
    const loadCategories = useCallback(async () => {
        try {
            setState(prev => ({ ...prev, loading: true }));
            const categories = await blogService.getAllCategories();
            setState(prev => ({ ...prev, categories, loading: false }));
        } catch (error) {
            console.error('Failed to load categories:', error);
            setState(prev => ({ ...prev, loading: false }));
        }
    }, []);

    useEffect(() => {
        loadCategories();
    }, [loadCategories]);

    // Auto-generate slug from name
    useEffect(() => {
        if (state.modalState.category.name && !state.modalState.category.slug) {
            const slug = blogService.generateSlug(state.modalState.category.name);
            updateModalCategory('slug', slug);
        }
    }, [state.modalState.category.name]);

    // Filter categories by search term
    const filteredCategories = state.categories.filter(category =>
        category.name.toLowerCase().includes(state.searchTerm.toLowerCase()) ||
        (category.description || '').toLowerCase().includes(state.searchTerm.toLowerCase())
    );

    // Update modal category
    const updateModalCategory = (field: keyof BlogCategory, value: any) => {
        setState(prev => ({
            ...prev,
            modalState: {
                ...prev.modalState,
                category: {
                    ...prev.modalState.category,
                    [field]: value,
                },
                errors: {
                    ...prev.modalState.errors,
                    [field]: '',
                }
            }
        }));
    };

    // Open modal
    const openModal = (mode: 'create' | 'edit', category?: BlogCategory) => {
        setState(prev => ({
            ...prev,
            modalState: {
                isOpen: true,
                mode,
                category: category ? { ...category } : { name: '', description: '', slug: '', metaTitle: '', metaDescription: '', isActive: true, sortOrder: 0 },
                errors: {},
            }
        }));
    };

    // Close modal
    const closeModal = () => {
        setState(prev => ({
            ...prev,
            modalState: {
                ...prev.modalState,
                isOpen: false,
                category: { name: '', description: '', slug: '', metaTitle: '', metaDescription: '', isActive: true, sortOrder: 0 },
                errors: {},
            }
        }));
    };

    // Validate category
    const validateCategory = (): boolean => {
        const errors: Record<string, string> = {};

        if (!state.modalState.category.name?.trim()) {
            errors.name = 'Tn danh mc l bt buc';
        }

        if (!state.modalState.category.slug?.trim()) {
            errors.slug = 'Slug l bt buc';
        }

        // Check if slug is unique (except for current category in edit mode)
        const existingCategory = state.categories.find(cat =>
            cat.slug === state.modalState.category.slug &&
            cat.id !== state.modalState.category.id
        );
        if (existingCategory) {
            errors.slug = 'Slug  tn ti';
        }

        setState(prev => ({
            ...prev,
            modalState: {
                ...prev.modalState,
                errors,
            }
        }));

        return Object.keys(errors).length === 0;
    };

    // Save category
    const saveCategory = async () => {
        if (!validateCategory()) return;

        try {
            setState(prev => ({ ...prev, saving: true }));

            if (state.modalState.mode === 'create') {
                const createData: CreateCategoryRequest = {
                    name: state.modalState.category.name!,
                    slug: state.modalState.category.slug!,
                    description: state.modalState.category.description,
                    metaTitle: state.modalState.category.metaTitle,
                    metaDescription: state.modalState.category.metaDescription,
                    isActive: state.modalState.category.isActive !== false,
                    sortOrder: state.modalState.category.sortOrder || 0
                };
                await blogService.createCategory(createData);
            } else {
                const updateData: UpdateCategoryRequest = {
                    id: state.modalState.category.id!,
                    name: state.modalState.category.name!,
                    slug: state.modalState.category.slug!,
                    description: state.modalState.category.description,
                    metaTitle: state.modalState.category.metaTitle,
                    metaDescription: state.modalState.category.metaDescription,
                    isActive: state.modalState.category.isActive,
                    sortOrder: state.modalState.category.sortOrder
                };
                await blogService.updateCategory(state.modalState.category.id!, updateData);
            }

            await loadCategories();
            closeModal();
        } catch (error) {
            console.error('Failed to save category:', error);
        } finally {
            setState(prev => ({ ...prev, saving: false }));
        }
    };

    // Delete category
    const deleteCategory = async (category: BlogCategory) => {
        try {
            await blogService.deleteCategory(category.id);
            await loadCategories();
            setState(prev => ({ ...prev, deletingCategory: null }));
        } catch (error) {
            console.error('Failed to delete category:', error);
            setState(prev => ({ ...prev, deletingCategory: null }));
        }
    };

    return (
        <div className="p-6 max-w-6xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Qun l danh mc</h1>
                    <p className="text-gray-500">To v qun l danh mc bi vit blog</p>
                </div>
                <Button onClick={() => openModal('create')}>
                    <Plus className="w-4 h-4 mr-2" />
                    Thm danh mc
                </Button>
            </div>

            {/* Search */}
            <Card>
                <CardContent className="pt-6">
                    <div className="relative">
                        <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
                        <Input
                            value={state.searchTerm}
                            onChange={(e) => setState(prev => ({ ...prev, searchTerm: e.target.value }))}
                            placeholder="Tm kim danh mc..."
                            className="pl-10"
                        />
                    </div>
                </CardContent>
            </Card>

            {/* Categories Table */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <FolderOpen className="w-5 h-5" />
                        Danh sch danh mc ({filteredCategories.length})
                    </CardTitle>
                </CardHeader>
                <CardContent>
                    {state.loading ? (
                        <div className="flex items-center justify-center h-64">
                            <Loader2 className="w-8 h-8 animate-spin" />
                        </div>
                    ) : filteredCategories.length === 0 ? (
                        <div className="text-center py-12">
                            <FolderOpen className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                            <h3 className="text-lg font-medium text-gray-900 mb-2">
                                {state.searchTerm ? 'Khng tm thy danh mc' : 'Cha c danh mc no'}
                            </h3>
                            <p className="text-gray-500 mb-4">
                                {state.searchTerm
                                    ? 'Th tm kim vi t kha khc'
                                    : 'To danh mc u tin  bt u phn loi bi vit'
                                }
                            </p>
                            {!state.searchTerm && (
                                <Button onClick={() => openModal('create')}>
                                    <Plus className="w-4 h-4 mr-2" />
                                    To danh mc u tin
                                </Button>
                            )}
                        </div>
                    ) : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Tn danh mc</TableHead>
                                    <TableHead>Slug</TableHead>
                                    <TableHead>M t</TableHead>
                                    <TableHead className="w-24">Thao tc</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {filteredCategories.map(category => (
                                    <TableRow key={category.id}>
                                        <TableCell>
                                            <div className="font-medium">{category.name}</div>
                                        </TableCell>
                                        <TableCell>
                                            <code className="bg-gray-100 px-2 py-1 rounded text-sm">
                                                {category.slug}
                                            </code>
                                        </TableCell>
                                        <TableCell>
                                            <div className="max-w-xs truncate text-gray-600">
                                                {category.description || 'Khng c m t'}
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            <div className="flex items-center gap-2">
                                                <Button
                                                    variant="ghost"
                                                    size="sm"
                                                    onClick={() => openModal('edit', category)}
                                                >
                                                    <Edit2 className="w-4 h-4" />
                                                </Button>
                                                <Button
                                                    variant="ghost"
                                                    size="sm"
                                                    onClick={() => setState(prev => ({ ...prev, deletingCategory: category }))}
                                                >
                                                    <Trash2 className="w-4 h-4" />
                                                </Button>
                                            </div>
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    )}
                </CardContent>
            </Card>

            {/* Category Modal */}
            <Dialog open={state.modalState.isOpen} onOpenChange={(open) => !open && closeModal()}>
                <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                        <DialogTitle>
                            {state.modalState.mode === 'create' ? 'To danh mc mi' : 'Chnh sa danh mc'}
                        </DialogTitle>
                        <DialogDescription>
                            {state.modalState.mode === 'create'
                                ? 'To danh mc mi  phn loi bi vit blog'
                                : 'Cp nht thng tin danh mc'
                            }
                        </DialogDescription>
                    </DialogHeader>

                    <div className="space-y-4">
                        <div>
                            <Label htmlFor="categoryName">Tn danh mc *</Label>
                            <Input
                                id="categoryName"
                                value={state.modalState.category.name || ''}
                                onChange={(e) => updateModalCategory('name', e.target.value)}
                                placeholder="Nhp tn danh mc..."
                                className={cn(state.modalState.errors.name && "border-red-500")}
                            />
                            {state.modalState.errors.name && (
                                <p className="text-sm text-red-600 mt-1">{state.modalState.errors.name}</p>
                            )}
                        </div>

                        <div>
                            <Label htmlFor="categorySlug">Slug *</Label>
                            <Input
                                id="categorySlug"
                                value={state.modalState.category.slug || ''}
                                onChange={(e) => updateModalCategory('slug', e.target.value)}
                                placeholder="slug-danh-muc"
                                className={cn(state.modalState.errors.slug && "border-red-500")}
                            />
                            {state.modalState.errors.slug && (
                                <p className="text-sm text-red-600 mt-1">{state.modalState.errors.slug}</p>
                            )}
                            <p className="text-xs text-gray-500 mt-1">
                                URL: /blog/category/{state.modalState.category.slug || 'slug-danh-muc'}
                            </p>
                        </div>

                        <div>
                            <Label htmlFor="categoryDescription">M t</Label>
                            <Textarea
                                id="categoryDescription"
                                value={state.modalState.category.description || ''}
                                onChange={(e) => updateModalCategory('description', e.target.value)}
                                placeholder="M t ngn v danh mc..."
                                rows={3}
                            />
                        </div>

                        <div>
                            <Label htmlFor="metaTitle">Meta Title</Label>
                            <Input
                                id="metaTitle"
                                value={state.modalState.category.metaTitle || ''}
                                onChange={(e) => updateModalCategory('metaTitle', e.target.value)}
                                placeholder="Tiu  meta..."
                            />
                        </div>

                        <div>
                            <Label htmlFor="metaDescription">Meta Description</Label>
                            <Textarea
                                id="metaDescription"
                                value={state.modalState.category.metaDescription || ''}
                                onChange={(e) => updateModalCategory('metaDescription', e.target.value)}
                                placeholder="M t meta..."
                                rows={3}
                            />
                        </div>

                        <div className="flex items-center justify-between">
                            <Label htmlFor="isActive">Hot ng</Label>
                            <Switch
                                id="isActive"
                                checked={state.modalState.category.isActive !== false}
                                onCheckedChange={(checked) => updateModalCategory('isActive', checked)}
                            />
                        </div>

                        <div>
                            <Label htmlFor="sortOrder">Th t sp xp</Label>
                            <Input
                                id="sortOrder"
                                type="number"
                                value={state.modalState.category.sortOrder || ''}
                                onChange={(e) => updateModalCategory('sortOrder', parseInt(e.target.value) || 0)}
                                placeholder="0"
                            />
                        </div>
                    </div>

                    <DialogFooter>
                        <Button variant="outline" onClick={closeModal}>
                            Hy
                        </Button>
                        <Button onClick={saveCategory} disabled={state.saving}>
                            {state.saving && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                            {state.modalState.mode === 'create' ? 'To danh mc' : 'Cp nht'}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* Delete Confirmation */}
            <AlertDialog
                open={!!state.deletingCategory}
                onOpenChange={(open) => !open && setState(prev => ({ ...prev, deletingCategory: null }))}
            >
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Xa danh mc</AlertDialogTitle>
                        <AlertDialogDescription>
                            Bn c chc chn mun xa danh mc "{state.deletingCategory?.name}"?
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel>Hy</AlertDialogCancel>
                        <AlertDialogAction
                            onClick={() => state.deletingCategory && deleteCategory(state.deletingCategory)}
                            className="bg-red-600 hover:bg-red-700"
                        >
                            Xa danh mc
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </div>
    );
}