'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
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
import { BlogAuthor } from '@/types/api';
import {
    Edit,
    Eye,
    Filter,
    Plus,
    RefreshCw,
    Search,
    Trash2,
    User,
    Users
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';
import { toast } from 'sonner';

interface AuthorsPageState {
    authors: BlogAuthor[];
    loading: boolean;
    searchTerm: string;
    statusFilter: 'all' | 'active' | 'inactive';
    modalState: {
        isOpen: boolean;
        mode: 'create' | 'edit' | 'view';
        author: Partial<BlogAuthor> | null;
    };
    saving: boolean;
}

interface AuthorModalProps {
    isOpen: boolean;
    mode: 'create' | 'edit' | 'view';
    author: Partial<BlogAuthor> | null;
    onClose: () => void;
    onSave: (author: Partial<BlogAuthor>) => Promise<void>;
    saving: boolean;
}

function AuthorModal({ isOpen, mode, author, onClose, onSave, saving }: AuthorModalProps) {
    const [formData, setFormData] = useState<Partial<BlogAuthor>>({
        firstName: '',
        lastName: '',
        email: '',
        displayName: '',
        bio: '',
        profilePictureUrl: '',
        socialLinks: {
            twitter: '',
            linkedin: '',
            github: '',
            website: ''
        },
        isActive: true
    });

    useEffect(() => {
        if (author) {
            setFormData({
                ...author,
                socialLinks: author.socialLinks || {
                    twitter: '',
                    linkedin: '',
                    github: '',
                    website: ''
                }
            });
        } else {
            setFormData({
                firstName: '',
                lastName: '',
                email: '',
                displayName: '',
                bio: '',
                profilePictureUrl: '',
                socialLinks: {
                    twitter: '',
                    linkedin: '',
                    github: '',
                    website: ''
                },
                isActive: true
            });
        }
    }, [author, isOpen]);

    const handleSave = async () => {
        if (!formData.firstName?.trim() || !formData.lastName?.trim() || !formData.email?.trim()) {
            toast.error('Vui lòng điền đầy đủ thông tin bắt buộc');
            return;
        }

        await onSave(formData);
    };

    const isViewMode = mode === 'view';

    return (
        <Dialog open={isOpen} onOpenChange={onClose}>
            <DialogContent className="max-w-2xl max-h-[80vh] overflow-y-auto">
                <DialogHeader>
                    <DialogTitle>
                        {mode === 'create' && 'Thêm tác giả mới'}
                        {mode === 'edit' && 'Chỉnh sửa tác giả'}
                        {mode === 'view' && 'Chi tiết tác giả'}
                    </DialogTitle>
                    <DialogDescription>
                        {mode === 'create' && 'Tạo tài khoản tác giả mới cho hệ thống blog'}
                        {mode === 'edit' && 'Cập nhật thông tin tác giả'}
                        {mode === 'view' && 'Xem chi tiết thông tin tác giả'}
                    </DialogDescription>
                </DialogHeader>

                <div className="space-y-6">
                    {/* Basic Information */}
                    <div className="space-y-4">
                        <h3 className="text-lg font-semibold">Thông tin cơ bản</h3>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <Label htmlFor="firstName">Tên *</Label>
                                <Input
                                    id="firstName"
                                    value={formData.firstName || ''}
                                    onChange={(e) => setFormData(prev => ({ ...prev, firstName: e.target.value }))}
                                    placeholder="Nhập tên"
                                    disabled={isViewMode}
                                />
                            </div>
                            <div>
                                <Label htmlFor="lastName">Họ *</Label>
                                <Input
                                    id="lastName"
                                    value={formData.lastName || ''}
                                    onChange={(e) => setFormData(prev => ({ ...prev, lastName: e.target.value }))}
                                    placeholder="Nhập họ"
                                    disabled={isViewMode}
                                />
                            </div>
                        </div>

                        <div>
                            <Label htmlFor="email">Email *</Label>
                            <Input
                                id="email"
                                type="email"
                                value={formData.email || ''}
                                onChange={(e) => setFormData(prev => ({ ...prev, email: e.target.value }))}
                                    placeholder="Nhập email"
                                disabled={isViewMode || mode === 'edit'}
                            />
                        </div>

                        <div>
                                <Label htmlFor="displayName">Tên hiển thị</Label>
                                <Input
                                    id="displayName"
                                    value={formData.displayName || ''}
                                    onChange={(e) => setFormData(prev => ({ ...prev, displayName: e.target.value }))}
                                    placeholder="Tên hiển thị công khai"
                                disabled={isViewMode}
                            />
                        </div>

                        <div>
                            <Label htmlFor="bio">Giới thiệu</Label>
                            <Textarea
                                id="bio"
                                value={formData.bio || ''}
                                onChange={(e) => setFormData(prev => ({ ...prev, bio: e.target.value }))}
                                    placeholder="Viết giới thiệu về tác giả..."
                                className="min-h-20"
                                disabled={isViewMode}
                            />
                        </div>

                        <div>
                            <Label htmlFor="profilePicture">Avatar URL</Label>
                            <Input
                                id="profilePicture"
                                value={formData.profilePictureUrl || ''}
                                onChange={(e) => setFormData(prev => ({ ...prev, profilePictureUrl: e.target.value }))}
                                placeholder="https://example.com/avatar.jpg"
                                disabled={isViewMode}
                            />
                        </div>
                    </div>

                    {/* Social Links */}
                    <div className="space-y-4">
                        <h3 className="text-lg font-semibold">Liên kết mạng xã hội</h3>
                        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                            <div>
                                <Label htmlFor="twitter">Twitter</Label>
                                <Input
                                    id="twitter"
                                    value={formData.socialLinks?.twitter || ''}
                                    onChange={(e) => setFormData(prev => ({
                                        ...prev,
                                        socialLinks: {
                                            ...prev.socialLinks,
                                            twitter: e.target.value
                                        }
                                    }))}
                                    placeholder="@username"
                                    disabled={isViewMode}
                                />
                            </div>
                            <div>
                                <Label htmlFor="linkedin">LinkedIn</Label>
                                <Input
                                    id="linkedin"
                                    value={formData.socialLinks?.linkedin || ''}
                                    onChange={(e) => setFormData(prev => ({
                                        ...prev,
                                        socialLinks: {
                                            ...prev.socialLinks,
                                            linkedin: e.target.value
                                        }
                                    }))}
                                    placeholder="linkedin.com/in/username"
                                    disabled={isViewMode}
                                />
                            </div>
                            <div>
                                <Label htmlFor="github">GitHub</Label>
                                <Input
                                    id="github"
                                    value={formData.socialLinks?.github || ''}
                                    onChange={(e) => setFormData(prev => ({
                                        ...prev,
                                        socialLinks: {
                                            ...prev.socialLinks,
                                            github: e.target.value
                                        }
                                    }))}
                                    placeholder="github.com/username"
                                    disabled={isViewMode}
                                />
                            </div>
                            <div>
                                <Label htmlFor="website">Website</Label>
                                <Input
                                    id="website"
                                    value={formData.socialLinks?.website || ''}
                                    onChange={(e) => setFormData(prev => ({
                                        ...prev,
                                        socialLinks: {
                                            ...prev.socialLinks,
                                            website: e.target.value
                                        }
                                    }))}
                                    placeholder="https://example.com"
                                    disabled={isViewMode}
                                />
                            </div>
                        </div>
                    </div>

                    {/* Stats (View mode only) */}
                    {isViewMode && author && (
                        <div className="space-y-4">
                            <h3 className="text-lg font-semibold">Thống kê</h3>
                            <div className="grid grid-cols-2 md:grid-cols-3 gap-4">
                                <div className="text-center p-4 bg-gray-50 rounded-lg">
                                    <div className="text-2xl font-bold text-blue-600">{author.postCount}</div>
                                    <div className="text-sm text-gray-600">Bài viết</div>
                                </div>
                                <div className="text-center p-4 bg-gray-50 rounded-lg">
                                    <div className="text-2xl font-bold text-green-600">
                                        {author.isActive ? 'Hoạt động' : 'Tạm khóa'}
                                    </div>
                                    <div className="text-sm text-gray-600">Trạng thái</div>
                                </div>
                                <div className="text-center p-4 bg-gray-50 rounded-lg">
                                    <div className="text-2xl font-bold text-gray-600">
                                        {new Date(author.createdAt || '').toLocaleDateString('vi-VN')}
                                    </div>
                                    <div className="text-sm text-gray-600">Ngày tham gia</div>
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Actions */}
                    {!isViewMode && (
                        <div className="flex items-center gap-2 pt-4">
                            <Button
                                onClick={handleSave}
                                disabled={saving}
                                className="flex-1"
                            >
                                {saving ? (
                                    <>
                                        <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white mr-2" />
                                        Đang lưu...
                                    </>
                                ) : (
                                    mode === 'create' ? 'Tạo tác giả' : 'Cập nhật'
                                )}
                            </Button>
                                <Button variant="outline" onClick={onClose} disabled={saving}>
                                    Hủy
                            </Button>
                        </div>
                    )}
                </div>
            </DialogContent>
        </Dialog>
    );
}

export default function BlogAuthorsPage() {
    const [state, setState] = useState<AuthorsPageState>({
        authors: [],
        loading: true,
        searchTerm: '',
        statusFilter: 'all',
        modalState: {
            isOpen: false,
            mode: 'create',
            author: null
        },
        saving: false,
    });

    // Load authors
    const loadAuthors = useCallback(async () => {
        try {
            setState(prev => ({ ...prev, loading: true }));
            // Mock API call - replace with actual service call
            const mockAuthors: BlogAuthor[] = [
                {
                    id: '1',
                    email: 'john.doe@example.com',
                    firstName: 'John',
                    lastName: 'Doe',
                    displayName: 'John Doe',
                    bio: 'Senior technical writer with 5+ years experience',
                    profilePictureUrl: '',
                    socialLinks: {
                        twitter: '@johndoe',
                        linkedin: 'linkedin.com/in/johndoe',
                        github: 'github.com/johndoe',
                        website: 'https://johndoe.com'
                    },
                    postCount: 15,
                    isActive: true,
                    createdAt: '2025-01-15T00:00:00Z'
                },
                // Add more mock data as needed
            ];

            setState(prev => ({ ...prev, authors: mockAuthors, loading: false }));
        } catch (error) {
            console.error('Failed to load authors:', error);
            setState(prev => ({ ...prev, loading: false }));
            toast.error('Không thể tải danh sách tác giả');
        }
    }, []);

    useEffect(() => {
        loadAuthors();
    }, [loadAuthors]);

    // Filter authors
    const normalizedSearch = state.searchTerm.trim().toLowerCase();

    const filteredAuthors = state.authors.filter(author => {
        const matchesSearch = !normalizedSearch ||
            author.firstName?.toLowerCase().includes(normalizedSearch) ||
            author.lastName?.toLowerCase().includes(normalizedSearch) ||
            author.email.toLowerCase().includes(normalizedSearch) ||
            author.displayName?.toLowerCase().includes(normalizedSearch);

        const matchesStatus = state.statusFilter === 'all' ||
            (state.statusFilter === 'active' && author.isActive) ||
            (state.statusFilter === 'inactive' && !author.isActive);

        return matchesSearch && matchesStatus;
    });

    // Handle modal actions
    const openModal = (mode: 'create' | 'edit' | 'view', author?: BlogAuthor) => {
        setState(prev => ({
            ...prev,
            modalState: {
                isOpen: true,
                mode,
                author: author || null
            }
        }));
    };

    const closeModal = () => {
        setState(prev => ({
            ...prev,
            modalState: {
                isOpen: false,
                mode: 'create',
                author: null
            },
            saving: false
        }));
    };

    const handleSaveAuthor = async (authorData: Partial<BlogAuthor>) => {
        try {
            setState(prev => ({ ...prev, saving: true }));

            if (state.modalState.mode === 'create') {
                // Mock API call - replace with actual service call
                console.log('Creating author:', authorData);
                toast.success('Tác giả đã được tạo thành công');
            } else {
                // Mock API call - replace with actual service call
                console.log('Updating author:', authorData);
                toast.success('Thông tin tác giả đã được cập nhật');
            }

            closeModal();
            await loadAuthors();
        } catch (error) {
            console.error('Failed to save author:', error);
            setState(prev => ({ ...prev, saving: false }));
            toast.error('Không thể lưu thông tin tác giả');
        }
    };

    const handleDeleteAuthor = async (authorId: string) => {
        if (!confirm('Bạn có chắc chắn muốn xóa tác giả này?')) return;

        try {
            // Mock API call - replace with actual service call
            console.log('Deleting author:', authorId);
            toast.success('Tác giả đã được xóa');
            await loadAuthors();
        } catch (error) {
            console.error('Failed to delete author:', error);
            toast.error('Không thể xóa tác giả');
        }
    };

    const getStatusBadgeVariant = (isActive: boolean) => {
        return isActive ? 'default' : 'secondary';
    };

    const getStatusText = (isActive: boolean) => {
        return isActive ? 'Hoạt động' : 'Tạm khóa';
    };

    return (
        <div className="p-6 max-w-7xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Quản lý Tác giả</h1>
                    <p className="text-gray-500">Quản lý tài khoản tác giả và quyền hạn</p>
                </div>
                <Button onClick={() => openModal('create')}>
                    <Plus className="w-4 h-4 mr-2" />
                    Thêm tác giả
                </Button>
            </div>

            {/* Filters */}
            <Card>
                <CardContent className="pt-6">
                    <div className="flex flex-col md:flex-row gap-4">
                        <div className="flex-1">
                            <div className="relative">
                                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
                                <Input
                                    placeholder="Tìm kiếm theo tên, email..."
                                    value={state.searchTerm}
                                    onChange={(e) => setState(prev => ({ ...prev, searchTerm: e.target.value }))}
                                    className="pl-10"
                                />
                            </div>
                        </div>
                        <Select
                            value={state.statusFilter}
                            onValueChange={(value: 'all' | 'active' | 'inactive') =>
                                setState(prev => ({ ...prev, statusFilter: value }))}
                        >
                            <SelectTrigger className="w-40">
                                <Filter className="w-4 h-4 mr-2" />
                                <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">Tất cả</SelectItem>
                                <SelectItem value="active">Hoạt động</SelectItem>
                                <SelectItem value="inactive">Tạm khóa</SelectItem>
                            </SelectContent>
                        </Select>
                        <Button variant="outline" onClick={loadAuthors} disabled={state.loading}>
                            <RefreshCw className={cn("w-4 h-4 mr-2", state.loading && "animate-spin")} />
                            Làm mới
                        </Button>
                    </div>
                </CardContent>
            </Card>

            {/* Authors List */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <Users className="w-5 h-5" />
                        Danh sách Tác giả ({filteredAuthors.length})
                    </CardTitle>
                    <CardDescription>
                        Quản lý thông tin và quyền hạn của các tác giả
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    {state.loading ? (
                        <div className="flex items-center justify-center py-8">
                            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900" />
                        </div>
                    ) : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Tác giả</TableHead>
                                    <TableHead>Email</TableHead>
                                    <TableHead>Bài viết</TableHead>
                                    <TableHead>Trạng thái</TableHead>
                                    <TableHead>Ngày tham gia</TableHead>
                                    <TableHead className="text-right">Thao tác</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {filteredAuthors.length > 0 ? (
                                    filteredAuthors.map((author) => (
                                        <TableRow key={author.id}>
                                            <TableCell>
                                                <div className="flex items-center gap-3">
                                                    <div className="w-10 h-10 bg-gray-200 rounded-full flex items-center justify-center">
                                                        {author.profilePictureUrl ? (
                                                            <img
                                                                src={author.profilePictureUrl}
                                                                alt={`${author.firstName} ${author.lastName}`}
                                                                className="w-10 h-10 rounded-full object-cover"
                                                            />
                                                        ) : (
                                                            <User className="w-5 h-5 text-gray-500" />
                                                        )}
                                                    </div>
                                                    <div>
                                                        <div className="font-semibold">
                                                            {author.displayName || `${author.firstName} ${author.lastName}`}
                                                        </div>
                                                        {author.bio && (
                                                            <div className="text-sm text-gray-500 truncate max-w-xs">
                                                                {author.bio}
                                                            </div>
                                                        )}
                                                    </div>
                                                </div>
                                            </TableCell>
                                            <TableCell>{author.email}</TableCell>
                                            <TableCell>
                                                <Badge variant="outline">
                                                    {author.postCount} bài viết
                                                </Badge>
                                            </TableCell>
                                            <TableCell>
                                                <Badge variant={getStatusBadgeVariant(Boolean(author.isActive))}>
                                                    {getStatusText(Boolean(author.isActive))}
                                                </Badge>
                                            </TableCell>
                                            <TableCell>
                                                {author.createdAt
                                                    ? new Date(author.createdAt).toLocaleDateString('vi-VN')
                                                    : 'Chưa xác định'}
                                            </TableCell>
                                            <TableCell className="text-right">
                                                <div className="flex items-center justify-end gap-2">
                                                    <Button
                                                        variant="ghost"
                                                        size="sm"
                                                        onClick={() => openModal('view', author)}
                                                    >
                                                        <Eye className="w-4 h-4" />
                                                    </Button>
                                                    <Button
                                                        variant="ghost"
                                                        size="sm"
                                                        onClick={() => openModal('edit', author)}
                                                    >
                                                        <Edit className="w-4 h-4" />
                                                    </Button>
                                                    <Button
                                                        variant="ghost"
                                                        size="sm"
                                                        onClick={() => handleDeleteAuthor(String(author.id))}
                                                        className="text-red-600 hover:text-red-700"
                                                    >
                                                        <Trash2 className="w-4 h-4" />
                                                    </Button>
                                                </div>
                                            </TableCell>
                                        </TableRow>
                                    ))
                                ) : (
                                    <TableRow>
                                        <TableCell colSpan={6} className="text-center py-8">
                                            <Users className="w-12 h-12 text-gray-300 mx-auto mb-4" />
                                            <p className="text-gray-500 mb-2">Không có tác giả nào</p>
                                            <p className="text-sm text-gray-400">
                                                {state.searchTerm || state.statusFilter !== 'all'
                                                    ? 'Không tìm thấy tác giả phù hợp với bộ lọc'
                                                    : 'Hãy thêm tác giả đầu tiên cho hệ thống'
                                                }
                                            </p>
                                        </TableCell>
                                    </TableRow>
                                )}
                            </TableBody>
                        </Table>
                    )}
                </CardContent>
            </Card>

            {/* Author Modal */}
            <AuthorModal
                isOpen={state.modalState.isOpen}
                mode={state.modalState.mode}
                author={state.modalState.author}
                onClose={closeModal}
                onSave={handleSaveAuthor}
                saving={state.saving}
            />
        </div>
    );
}