'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuSeparator,
    DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { Input } from '@/components/ui/input';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { blogService } from '@/services/blogService';
import { Blog, BlogSearchParams, BlogStatus, BlogStatusType } from '@/types/api';
import {
    Archive,
    Calendar,
    CheckCircle,
    Edit,
    Eye,
    Filter,
    Heart,
    MessageCircle,
    MoreHorizontal,
    Plus,
    Search,
    Trash2,
    User,
    X
} from 'lucide-react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { useEffect, useState, type ComponentType } from 'react';

const statusLabels: Record<BlogStatusType, string> = {
    [BlogStatus.Published]: ' xut bn',
    [BlogStatus.Draft]: 'Bn nhp',
};

interface BlogsListState {
    blogs: Blog[];
    totalCount: number;
    currentPage: number;
    totalPages: number;
    loading: boolean;
    selectedBlogs: Set<number>;
    bulkActionLoading: boolean;
}

export default function BlogsListPage() {
    const router = useRouter();
    const searchParams = useSearchParams();

    const [state, setState] = useState<BlogsListState>({
        blogs: [],
        totalCount: 0,
        currentPage: 1,
        totalPages: 0,
        loading: true,
        selectedBlogs: new Set<number>(),
        bulkActionLoading: false,
    });

    const [filters, setFilters] = useState({
        search: searchParams.get('search') || '',
        status: (searchParams.get('status')?.toLowerCase() as BlogStatusType) || undefined,
        author: searchParams.get('author') || '',
        sortBy: (searchParams.get('sortBy') as any) || 'updatedAt',
        sortOrder: (searchParams.get('sortOrder') as 'asc' | 'desc') || 'desc',
    });

    // Fetch blogs based on current filters and page
    const fetchBlogs = async (page = 1) => {
        try {
            setState(prev => ({ ...prev, loading: true }));

            const params: BlogSearchParams = {
                page,
                pageSize: 10,
                search: filters.search || undefined,
                status: filters.status,
                authorId: filters.author || undefined,
                sortBy: filters.sortBy,
                sortOrder: filters.sortOrder,
            };

            const response = await blogService.getBlogs(params);

            setState(prev => ({
                ...prev,
                blogs: response.items,
                totalCount: response.totalCount,
                currentPage: page,
                totalPages: response.totalPages,
                loading: false,
            }));
        } catch (error) {
            console.error('Failed to fetch blogs:', error);
            setState(prev => ({ ...prev, loading: false }));
        }
    };

    // Update URL when filters change
    const updateURL = () => {
        const params = new URLSearchParams();
        if (filters.search) params.set('search', filters.search);
        if (filters.status) params.set('status', filters.status);
        if (filters.author) params.set('author', filters.author);
        if (filters.sortBy !== 'updatedAt') params.set('sortBy', filters.sortBy);
        if (filters.sortOrder !== 'desc') params.set('sortOrder', filters.sortOrder);

        const queryString = params.toString();
        const newUrl = queryString ? `?${queryString}` : '';
        router.push(`/admin/blog/posts${newUrl}`);
    };

    useEffect(() => {
        fetchBlogs(1);
    }, [filters]);

    useEffect(() => {
        updateURL();
    }, [filters]);

    // Handle filter changes
    const handleFilterChange = (key: string, value: any) => {
        setFilters(prev => ({
            ...prev,
            [key]: value,
        }));
    };

    // Handle selection
    const handleSelectBlog = (blogId: number) => {
        setState(prev => ({
            ...prev,
            selectedBlogs: new Set(
                prev.selectedBlogs.has(blogId)
                    ? Array.from(prev.selectedBlogs).filter(id => id !== blogId)
                    : [...Array.from(prev.selectedBlogs), blogId]
            ),
        }));
    };

    const handleSelectAll = () => {
        setState(prev => ({
            ...prev,
            selectedBlogs: prev.selectedBlogs.size === prev.blogs.length
                ? new Set<number>()
                : new Set(prev.blogs.map(blog => blog.id)),
        }));
    };

    // Bulk actions
    const handleBulkDelete = async () => {
        if (state.selectedBlogs.size === 0 || !confirm(`Bn c chc mun xa ${state.selectedBlogs.size} bi vit?`)) {
            return;
        }

        try {
            setState(prev => ({ ...prev, bulkActionLoading: true }));
            const blogIds = Array.from(state.selectedBlogs);
            await Promise.all(blogIds.map(async (blogId) => blogService.deleteBlog(blogId)));
            setState(prev => ({ ...prev, selectedBlogs: new Set<number>() }));
            fetchBlogs(state.currentPage);
        } catch (error) {
            console.error('Failed to delete blogs:', error);
        } finally {
            setState(prev => ({ ...prev, bulkActionLoading: false }));
        }
    };

    // Individual actions
    const handleDeleteBlog = async (id: number, title: string) => {
        if (!confirm(`Bn c chc mun xa bi vit "${title}"?`)) {
            return;
        }

        try {
            await blogService.deleteBlog(id);
            fetchBlogs(state.currentPage);
        } catch (error) {
            console.error('Failed to delete blog:', error);
        }
    };

    const handlePublishBlog = async (id: number) => {
        try {
            await blogService.publishBlog(id);
            fetchBlogs(state.currentPage);
        } catch (error) {
            console.error('Failed to publish blog:', error);
        }
    };

    const handleUnpublishBlog = async (id: number) => {
        try {
            await blogService.unpublishBlog(id);
            fetchBlogs(state.currentPage);
        } catch (error) {
            console.error('Failed to unpublish blog:', error);
        }
    };


    return (
        <div className="p-6 space-y-6">
            {/* Header */}
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900">Quản lý bài viết</h1>
                    <p className="text-gray-600 mt-1">
                        Qun l tt c bi vit blog ca bn
                    </p>
                </div>
                <Button asChild>
                    <Link href="/admin/blog/posts/new">
                        <Plus className="w-4 h-4 mr-2" />
                        To bi vit
                    </Link>
                </Button>
            </div>

            {/* Filters */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <Filter className="w-5 h-5" />
                        B lc v tm kim
                    </CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                        {/* Search */}
                        <div className="relative">
                            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-gray-400" />
                            <Input
                                placeholder="Tm kim bi vit..."
                                value={filters.search}
                                onChange={(e) => handleFilterChange('search', e.target.value)}
                                className="pl-10"
                            />
                        </div>

                        {/* Status Filter */}
                        <Select
                            value={filters.status || 'all'}
                            onValueChange={(value) => handleFilterChange('status', value === 'all' ? undefined : value)}
                        >
                            <SelectTrigger>
                                <SelectValue placeholder="Trng thi" />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">Tt c trng thi</SelectItem>
                                <SelectItem value="published"> xut bn</SelectItem>
                                <SelectItem value="draft">Bn nhp</SelectItem>
                            </SelectContent>
                        </Select>

                        {/* Sort By */}
                        <Select
                            value={filters.sortBy}
                            onValueChange={(value) => handleFilterChange('sortBy', value)}
                        >
                            <SelectTrigger>
                                <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="updatedAt">Mi cp nht</SelectItem>
                                <SelectItem value="createdAt">Mi to</SelectItem>
                                <SelectItem value="publishedAt">Mi xut bn</SelectItem>
                                <SelectItem value="title">Tiu </SelectItem>
                                <SelectItem value="viewCount">Lt xem</SelectItem>
                            </SelectContent>
                        </Select>

                        {/* Sort Order */}
                        <Select
                            value={filters.sortOrder}
                            onValueChange={(value) => handleFilterChange('sortOrder', value)}
                        >
                            <SelectTrigger>
                                <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="desc">Gim dn</SelectItem>
                                <SelectItem value="asc">Tng dn</SelectItem>
                            </SelectContent>
                        </Select>
                    </div>

                    {/* Active Filters */}
                    {(filters.search || filters.status || filters.author) && (
                        <div className="flex flex-wrap gap-2 mt-4">
                            {filters.search && (
                                <Badge variant="secondary" className="flex items-center gap-1">
                                    Tm kim: {filters.search}
                                    <X
                                        className="w-3 h-3 cursor-pointer"
                                        onClick={() => handleFilterChange('search', '')}
                                    />
                                </Badge>
                            )}
                            {filters.status && (
                                <Badge variant="secondary" className="flex items-center gap-1">
                                    Trng thi: {statusLabels[filters.status] ?? filters.status}
                                    <X
                                        className="w-3 h-3 cursor-pointer"
                                        onClick={() => handleFilterChange('status', undefined)}
                                    />
                                </Badge>
                            )}
                        </div>
                    )}
                </CardContent>
            </Card>

            {/* Bulk Actions */}
            {state.selectedBlogs.size > 0 && (
                <Card className="bg-blue-50 border-blue-200">
                    <CardContent className="p-4">
                        <div className="flex items-center justify-between">
                            <div className="flex items-center gap-3">
                                <span className="text-sm font-medium">
                                     chn {state.selectedBlogs.size} bi vit
                                </span>
                            </div>
                            <div className="flex items-center gap-2">
                                <Button
                                    variant="destructive"
                                    size="sm"
                                    onClick={handleBulkDelete}
                                    disabled={state.bulkActionLoading}
                                >
                                    <Trash2 className="w-4 h-4 mr-2" />
                                    Xa
                                </Button>
                                <Button
                                    variant="outline"
                                    size="sm"
                                    onClick={() => setState(prev => ({ ...prev, selectedBlogs: new Set<number>() }))}
                                >
                                    Hy
                                </Button>
                            </div>
                        </div>
                    </CardContent>
                </Card>
            )}

            {/* Blog List */}
            <Card>
                <CardHeader>
                    <div className="flex items-center justify-between">
                        <div>
                            <CardTitle>Danh sách bài viết</CardTitle>
                            <CardDescription>
                                {state.loading ? 'ang ti...' : `${state.totalCount} bi vit`}
                            </CardDescription>
                        </div>

                        {state.blogs.length > 0 && (
                            <div className="flex items-center gap-2">
                                <Checkbox
                                    checked={state.selectedBlogs.size === state.blogs.length}
                                    onCheckedChange={handleSelectAll}
                                />
                                <span className="text-sm text-gray-600">Chn tt c</span>
                            </div>
                        )}
                    </div>
                </CardHeader>
                <CardContent className="p-0">
                    {state.loading ? (
                        <BlogsListSkeleton />
                    ) : state.blogs.length === 0 ? (
                        <div className="text-center py-12">
                            <div className="text-gray-500">
                                <h3 className="text-lg font-medium mb-2">Cha c bi vit no</h3>
                                <p className="mb-4">To bi vit u tin  bt u</p>
                                <Button asChild>
                                    <Link href="/admin/blog/posts/new">
                                        <Plus className="w-4 h-4 mr-2" />
                                        To bi vit
                                    </Link>
                                </Button>
                            </div>
                        </div>
                    ) : (
                        <div className="space-y-0">
                            {state.blogs.map((blog, index) => (
                                <BlogRowItem
                                    key={blog.id}
                                    blog={blog}
                                    isSelected={state.selectedBlogs.has(blog.id)}
                                    onSelect={handleSelectBlog}
                                    onDelete={handleDeleteBlog}
                                    onPublish={handlePublishBlog}
                                    onUnpublish={handleUnpublishBlog}
                                    isLast={index === state.blogs.length - 1}
                                />
                            ))}
                        </div>
                    )}
                </CardContent>
            </Card>

            {/* Pagination */}
            {!state.loading && state.totalPages > 1 && (
                <div className="flex items-center justify-between">
                    <p className="text-sm text-gray-700">
                        Hin th {((state.currentPage - 1) * 10) + 1} n {Math.min(state.currentPage * 10, state.totalCount)} trong {state.totalCount} kt qu
                    </p>
                    <div className="flex items-center gap-2">
                        <Button
                            variant="outline"
                            size="sm"
                            onClick={() => fetchBlogs(state.currentPage - 1)}
                            disabled={state.currentPage <= 1}
                        >
                            Trc
                        </Button>

                        {/* Page Numbers */}
                        {Array.from({ length: Math.min(5, state.totalPages) }, (_, i) => {
                            let page = i + 1;
                            if (state.totalPages > 5) {
                                const start = Math.max(1, state.currentPage - 2);
                                const end = Math.min(state.totalPages, start + 4);
                                page = start + i;
                                if (page > end) return null;
                            }

                            return (
                                <Button
                                    key={page}
                                    variant={page === state.currentPage ? "default" : "outline"}
                                    size="sm"
                                    onClick={() => fetchBlogs(page)}
                                >
                                    {page}
                                </Button>
                            );
                        })}

                        <Button
                            variant="outline"
                            size="sm"
                            onClick={() => fetchBlogs(state.currentPage + 1)}
                            disabled={state.currentPage >= state.totalPages}
                        >
                            Sau
                        </Button>
                    </div>
                </div>
            )}
        </div>
    );
}

function BlogRowItem({
    blog,
    isSelected,
    onSelect,
    onDelete,
    onPublish,
    onUnpublish,
    isLast
}: {
    blog: Blog;
    isSelected: boolean;
    onSelect: (id: number) => void;
    onDelete: (id: number, title: string) => void | Promise<void>;
    onPublish: (id: number) => void | Promise<void>;
    onUnpublish: (id: number) => void | Promise<void>;
    isLast: boolean;
}) {
    const getStatusBadge = (status: BlogStatusType) => {
        const variants: Record<BlogStatusType, { variant: 'default' | 'secondary' | 'outline' | 'destructive'; label: string; icon: ComponentType<{ className?: string }> }> = {
            [BlogStatus.Published]: { variant: 'default', label: 'Xut bn', icon: CheckCircle },
            [BlogStatus.Draft]: { variant: 'secondary', label: 'Nhp', icon: Edit },
        };

        const config = variants[status] ?? variants[BlogStatus.Draft];
        const IconComponent = config.icon;

        return (
            <Badge variant={config.variant} className="flex items-center gap-1">
                <IconComponent className="w-3 h-3" />
                {config.label}
            </Badge>
        );
    };

    return (
        <div className={`p-6 ${!isLast ? 'border-b border-gray-200' : ''} hover:bg-gray-50 transition-colors`}>
            <div className="flex items-start gap-4">
                {/* Selection checkbox */}
                <Checkbox
                    checked={isSelected}
                    onCheckedChange={() => onSelect(blog.id)}
                    className="mt-1"
                />

                {/* Featured Image */}
                <div className="w-16 h-16 rounded-lg overflow-hidden bg-gray-100 flex-shrink-0">
                    {blog.featuredImageUrl ? (
                        <img
                            src={blog.featuredImageUrl}
                            alt={blog.featuredImageAlt || blog.title}
                            className="w-full h-full object-cover"
                        />
                    ) : (
                        <div className="w-full h-full flex items-center justify-center">
                            <Edit className="w-6 h-6 text-gray-400" />
                        </div>
                    )}
                </div>

                {/* Content */}
                <div className="flex-1 min-w-0">
                    <div className="flex items-start justify-between">
                        <div className="flex-1 min-w-0">
                            <Link
                                href={`/admin/blog/posts/${blog.id}`}
                                className="text-lg font-semibold text-gray-900 hover:text-blue-600 truncate block"
                            >
                                {blog.title}
                            </Link>
                            {blog.excerpt && (
                                <p className="text-gray-600 text-sm mt-1 line-clamp-2">
                                    {blog.excerpt}
                                </p>
                            )}
                        </div>

                        <div className="flex items-center gap-2 ml-4">
                            {getStatusBadge(blog.status)}

                            <DropdownMenu>
                                <DropdownMenuTrigger asChild>
                                    <Button variant="ghost" size="sm">
                                        <MoreHorizontal className="w-4 h-4" />
                                    </Button>
                                </DropdownMenuTrigger>
                                <DropdownMenuContent align="end">
                                    <DropdownMenuItem asChild>
                                        <Link href={`/admin/blog/posts/${blog.id}`}>
                                            <Edit className="w-4 h-4 mr-2" />
                                            Sa
                                        </Link>
                                    </DropdownMenuItem>

                                    {blog.status === BlogStatus.Draft && (
                                        <DropdownMenuItem onClick={() => onPublish(blog.id)}>
                                            <CheckCircle className="w-4 h-4 mr-2" />
                                            Xut bn
                                        </DropdownMenuItem>
                                    )}

                                    {blog.status === BlogStatus.Published && (
                                        <DropdownMenuItem onClick={() => onUnpublish(blog.id)}>
                                            <Archive className="w-4 h-4 mr-2" />
                                            n bi vit
                                        </DropdownMenuItem>
                                    )}

                                    <DropdownMenuSeparator />
                                    <DropdownMenuItem
                                        onClick={() => onDelete(blog.id, blog.title)}
                                        className="text-red-600 hover:text-red-700 hover:bg-red-50"
                                    >
                                        <Trash2 className="w-4 h-4 mr-2" />
                                        Xa
                                    </DropdownMenuItem>
                                </DropdownMenuContent>
                            </DropdownMenu>
                        </div>
                    </div>

                    {/* Metadata */}
                    <div className="flex items-center gap-4 mt-3 text-sm text-gray-500">
                        <div className="flex items-center gap-1">
                            <User className="w-4 h-4" />
                            {blog.author?.displayName || `${blog.author?.firstName} ${blog.author?.lastName}`}
                        </div>

                        <div className="flex items-center gap-1">
                            <Calendar className="w-4 h-4" />
                            {blog.publishedAt
                                ? new Date(blog.publishedAt).toLocaleDateString('vi-VN')
                                : new Date(blog.updatedAt).toLocaleDateString('vi-VN')
                            }
                        </div>

                        {blog.category && (
                            <Badge variant="outline" className="text-xs">
                                {blog.category.name}
                            </Badge>
                        )}

                        <div className="flex items-center gap-3 ml-auto">
                            <div className="flex items-center gap-1">
                                <Eye className="w-4 h-4" />
                                {blog.viewCount || 0}
                            </div>
                            <div className="flex items-center gap-1">
                                <MessageCircle className="w-4 h-4" />
                                {blog.commentCount || 0}
                            </div>
                            <div className="flex items-center gap-1">
                                <Heart className="w-4 h-4" />
                                {blog.likeCount || 0}
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}

function BlogsListSkeleton() {
    return (
        <div className="space-y-0">
            {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="p-6 border-b border-gray-200">
                    <div className="flex items-start gap-4">
                        <Skeleton className="w-4 h-4 mt-1" />
                        <Skeleton className="w-16 h-16 rounded-lg" />
                        <div className="flex-1 space-y-2">
                            <Skeleton className="h-5 w-3/4" />
                            <Skeleton className="h-4 w-full" />
                            <Skeleton className="h-4 w-2/3" />
                            <div className="flex items-center gap-4 mt-3">
                                <Skeleton className="h-4 w-20" />
                                <Skeleton className="h-4 w-24" />
                                <Skeleton className="h-6 w-16" />
                            </div>
                        </div>
                        <Skeleton className="w-8 h-8" />
                    </div>
                </div>
            ))}
        </div>
    );
}