'use client';

import { AlertDialog, AlertDialogAction, AlertDialogCancel, AlertDialogContent, AlertDialogDescription, AlertDialogFooter, AlertDialogHeader, AlertDialogTitle } from '@/components/ui/alert-dialog';
import { Badge } from '@/components/ui/badge';
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
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow,
} from '@/components/ui/table';
import { cn } from '@/lib/utils';
import { blogService } from '@/services/blogService';
import { BlogTag, CreateBlogTagRequest, UpdateBlogTagRequest } from '@/types/api';
import {
    Edit2,
    Hash,
    Loader2,
    Plus,
    Search,
    Tag,
    Trash2
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';

interface TagModalState {
    isOpen: boolean;
    mode: 'create' | 'edit';
    tag: Partial<BlogTag>;
    errors: Record<string, string>;
}

interface TagsPageState {
    tags: BlogTag[];
    loading: boolean;
    searchTerm: string;
    modalState: TagModalState;
    deletingTag: BlogTag | null;
    saving: boolean;
}

export default function TagsPage() {
    const [state, setState] = useState<TagsPageState>({
        tags: [],
        loading: true,
        searchTerm: '',
        modalState: {
            isOpen: false,
            mode: 'create',
            tag: { name: '' },
            errors: {},
        },
        deletingTag: null,
        saving: false,
    });

    // Load tags
    const loadTags = useCallback(async () => {
        try {
            setState(prev => ({ ...prev, loading: true }));
            const tags = await blogService.getAllTags();
            setState(prev => ({ ...prev, tags, loading: false }));
        } catch (error) {
            console.error('Failed to load tags:', error);
            setState(prev => ({ ...prev, loading: false }));
        }
    }, []);

    useEffect(() => {
        loadTags();
    }, [loadTags]);

    // Filter tags by search term
    const filteredTags = state.tags.filter(tag =>
        tag.name.toLowerCase().includes(state.searchTerm.toLowerCase())
    );

    // Update modal tag
    const updateModalTag = (field: keyof BlogTag, value: string) => {
        setState(prev => ({
            ...prev,
            modalState: {
                ...prev.modalState,
                tag: {
                    ...prev.modalState.tag,
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
    const openModal = (mode: 'create' | 'edit', tag?: BlogTag) => {
        setState(prev => ({
            ...prev,
            modalState: {
                isOpen: true,
                mode,
                tag: tag ? { ...tag } : { name: '' },
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
                tag: { name: '' },
                errors: {},
            }
        }));
    };

    // Validate tag
    const validateTag = (): boolean => {
        const errors: Record<string, string> = {};

        if (!state.modalState.tag.name?.trim()) {
            errors.name = 'Tên tag là bắt buộc';
        }

        // Check if name is unique (except for current tag in edit mode)
        const existingTag = state.tags.find(tag =>
            tag.name.toLowerCase() === state.modalState.tag.name?.toLowerCase() &&
            tag.id !== state.modalState.tag.id
        );
        if (existingTag) {
            errors.name = 'Tên tag đã tồn tại';
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

    // Save tag
    const saveTag = async () => {
        if (!validateTag()) return;

        try {
            setState(prev => ({ ...prev, saving: true }));

            if (state.modalState.mode === 'create') {
                const createData: CreateBlogTagRequest = {
                    name: state.modalState.tag.name!.trim(),
                };
                await blogService.createTag(createData);
            } else {
                const updateData: UpdateBlogTagRequest = {
                    id: state.modalState.tag.id!,
                    name: state.modalState.tag.name!.trim(),
                };
                await blogService.updateTag(state.modalState.tag.id!, updateData);
            }

            await loadTags();
            closeModal();
        } catch (error) {
            console.error('Failed to save tag:', error);
        } finally {
            setState(prev => ({ ...prev, saving: false }));
        }
    };

    // Delete tag
    const deleteTag = async (tag: BlogTag) => {
        try {
            await blogService.deleteTag(tag.id);
            await loadTags();
            setState(prev => ({ ...prev, deletingTag: null }));
        } catch (error) {
            console.error('Failed to delete tag:', error);
            setState(prev => ({ ...prev, deletingTag: null }));
        }
    };

    return (
        <div className="p-6 max-w-6xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Quản lý Tags</h1>
                    <p className="text-gray-500">Tạo và quản lý tags cho bài viết blog</p>
                </div>
                <Button onClick={() => openModal('create')}>
                    <Plus className="w-4 h-4 mr-2" />
                    Thêm tag
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
                            placeholder="Tìm kiếm tags..."
                            className="pl-10"
                        />
                    </div>
                </CardContent>
            </Card>

            {/* Tags Table */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <Hash className="w-5 h-5" />
                        Danh sách tags ({filteredTags.length})
                    </CardTitle>
                </CardHeader>
                <CardContent>
                    {state.loading ? (
                        <div className="flex items-center justify-center h-64">
                            <Loader2 className="w-8 h-8 animate-spin" />
                        </div>
                    ) : filteredTags.length === 0 ? (
                        <div className="text-center py-12">
                            <Tag className="w-12 h-12 text-gray-400 mx-auto mb-4" />
                            <h3 className="text-lg font-medium text-gray-900 mb-2">
                                {state.searchTerm ? 'Không tìm thấy tag' : 'Chưa có tag nào'}
                            </h3>
                            <p className="text-gray-500 mb-4">
                                {state.searchTerm
                                    ? 'Thử tìm kiếm với từ khóa khác'
                                    : 'Tạo tag đầu tiên để bắt đầu gắn nhãn bài viết'
                                }
                            </p>
                            {!state.searchTerm && (
                                <Button onClick={() => openModal('create')}>
                                    <Plus className="w-4 h-4 mr-2" />
                                    Tạo tag đầu tiên
                                </Button>
                            )}
                        </div>
                    ) : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Tên tag</TableHead>
                                    <TableHead>Ngày tạo</TableHead>
                                    <TableHead className="w-24">Thao tác</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {filteredTags.map(tag => (
                                    <TableRow key={tag.id}>
                                        <TableCell>
                                            <div className="flex items-center gap-2">
                                                <Badge variant="secondary" className="font-medium">
                                                    #{tag.name}
                                                </Badge>
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            <span className="text-gray-600 text-sm">
                                                {new Date(tag.createdAt).toLocaleDateString('vi-VN')}
                                            </span>
                                        </TableCell>
                                        <TableCell>
                                            <div className="flex items-center gap-2">
                                                <Button
                                                    variant="ghost"
                                                    size="sm"
                                                    onClick={() => openModal('edit', tag)}
                                                >
                                                    <Edit2 className="w-4 h-4" />
                                                </Button>
                                                <Button
                                                    variant="ghost"
                                                    size="sm"
                                                    onClick={() => setState(prev => ({ ...prev, deletingTag: tag }))}
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

            {/* Tag Modal */}
            <Dialog open={state.modalState.isOpen} onOpenChange={(open) => !open && closeModal()}>
                <DialogContent className="sm:max-w-md">
                    <DialogHeader>
                        <DialogTitle>
                            {state.modalState.mode === 'create' ? 'Tạo tag mới' : 'Chỉnh sửa tag'}
                        </DialogTitle>
                        <DialogDescription>
                            {state.modalState.mode === 'create'
                                ? 'Tạo tag mới để gắn nhãn bài viết blog'
                                : 'Cập nhật thông tin tag'
                            }
                        </DialogDescription>
                    </DialogHeader>

                    <div className="space-y-4">
                        <div>
                            <Label htmlFor="tagName">Tên tag *</Label>
                            <Input
                                id="tagName"
                                value={state.modalState.tag.name || ''}
                                onChange={(e) => updateModalTag('name', e.target.value)}
                                placeholder="Nhập tên tag..."
                                className={cn(state.modalState.errors.name && "border-red-500")}
                            />
                            {state.modalState.errors.name && (
                                <p className="text-sm text-red-600 mt-1">{state.modalState.errors.name}</p>
                            )}
                            <p className="text-xs text-gray-500 mt-1">
                                Tag sẽ hiển thị như: #{state.modalState.tag.name || 'ten-tag'}
                            </p>
                        </div>
                    </div>

                    <DialogFooter>
                        <Button variant="outline" onClick={closeModal}>
                            Hủy
                        </Button>
                        <Button onClick={saveTag} disabled={state.saving}>
                            {state.saving && <Loader2 className="w-4 h-4 mr-2 animate-spin" />}
                            {state.modalState.mode === 'create' ? 'Tạo tag' : 'Cập nhật'}
                        </Button>
                    </DialogFooter>
                </DialogContent>
            </Dialog>

            {/* Delete Confirmation */}
            <AlertDialog
                open={!!state.deletingTag}
                onOpenChange={(open) => !open && setState(prev => ({ ...prev, deletingTag: null }))}
            >
                <AlertDialogContent>
                    <AlertDialogHeader>
                        <AlertDialogTitle>Xóa tag</AlertDialogTitle>
                        <AlertDialogDescription>
                            Bạn có chắc chắn muốn xóa tag "#{state.deletingTag?.name}"?
                        </AlertDialogDescription>
                    </AlertDialogHeader>
                    <AlertDialogFooter>
                        <AlertDialogCancel>Hủy</AlertDialogCancel>
                        <AlertDialogAction
                            onClick={() => state.deletingTag && deleteTag(state.deletingTag)}
                            className="bg-red-600 hover:bg-red-700"
                        >
                            Xóa tag
                        </AlertDialogAction>
                    </AlertDialogFooter>
                </AlertDialogContent>
            </AlertDialog>
        </div>
    );
}
