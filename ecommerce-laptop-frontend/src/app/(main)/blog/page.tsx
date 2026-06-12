'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Skeleton } from '@/components/ui/skeleton';
import { blogService } from '@/services/blogService';
import { Blog, BlogCategory, BlogSearchParams } from '@/types/api';
import {
    Calendar,
    Clock,
    Eye,
    ImageIcon,
    MessageCircle,
    Search,
    Tag,
    User,
    X
} from 'lucide-react';
import Link from 'next/link';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

interface BlogListingState {
    blogs: Blog[];
    categories: BlogCategory[];
    loading: boolean;
    categoriesLoading: boolean;
    searchTerm: string;
    selectedCategory: number | null;
    currentPage: number;
    totalPages: number;
    totalCount: number;
}

export default function BlogListingPage() {
    const [state, setState] = useState<BlogListingState>({
        blogs: [],
        categories: [],
        loading: true,
        categoriesLoading: true,
        searchTerm: '',
        selectedCategory: null,
        currentPage: 1,
        totalPages: 1,
        totalCount: 0
    });

    // Load blogs
    const loadBlogs = async (params: BlogSearchParams = {}) => {
        try {
            setState(prev => ({ ...prev, loading: true }));
            const response = await blogService.getBlogs({
                page: state.currentPage,
                pageSize: 12,
                search: state.searchTerm || undefined,
                categoryId: state.selectedCategory || undefined,
                status: 'published',
                ...params
            });

            setState(prev => ({
                ...prev,
                blogs: response.items,
                totalPages: response.totalPages,
                totalCount: response.totalCount,
                loading: false
            }));
        } catch (error) {
            console.error('Failed to load blogs:', error);
            setState(prev => ({ ...prev, loading: false }));
            toast.error('Không thể tải danh sách blog');
        }
    };

    // Load categories
    const loadCategories = async () => {
        try {
            setState(prev => ({ ...prev, categoriesLoading: true }));
            const categories = await blogService.getAllCategories();
            setState(prev => ({ ...prev, categories, categoriesLoading: false }));
        } catch (error) {
            console.error('Failed to load categories:', error);
            setState(prev => ({ ...prev, categoriesLoading: false }));
        }
    };

    useEffect(() => {
        loadBlogs();
        loadCategories();
    }, []);

    useEffect(() => {
        loadBlogs();
    }, [state.currentPage, state.searchTerm, state.selectedCategory]);

    const handleSearch = (value: string) => {
        setState(prev => ({ ...prev, searchTerm: value, currentPage: 1 }));
    };

    const handleCategoryFilter = (categoryId: number | null) => {
        setState(prev => ({ ...prev, selectedCategory: categoryId, currentPage: 1 }));
    };

    const formatReadingTime = (minutes: number) => {
        return minutes < 1 ? 'Dưới 1 phút đọc' : `${minutes} phút đọc`;
    };

    const truncateText = (text: string, maxLength: number) => {
        if (text.length <= maxLength) return text;
        return text.substring(0, maxLength) + '...';
    };

    if (state.loading && state.blogs.length === 0) {
        return <BlogListingSkeleton />;
    }

    const visiblePages = getVisiblePages(state.currentPage, state.totalPages);

    return (
        <div className="min-h-screen bg-gray-50">
            <div className="container mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
                {/* Header */}
                <div className="mx-auto mb-10 max-w-3xl text-center">
                    <h1 className="mb-4 text-4xl font-bold tracking-tight text-gray-900 sm:text-5xl">Blog</h1>
                    <p className="text-lg leading-8 text-gray-600 sm:text-xl">
                        Khám phá những bài viết mới nhất về công nghệ, laptop và các sản phẩm điện tử
                    </p>
                </div>

                {/* Search and Filters */}
                <Card className="mb-8">
                    <CardContent className="pt-6">
                        <div className="flex flex-col md:flex-row gap-4">
                            {/* Search */}
                            <div className="flex-1">
                                <div className="relative">
                                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-4 h-4" />
                                    <Input
                                        placeholder="Tìm kiếm bài viết..."
                                        value={state.searchTerm}
                                        onChange={(e) => handleSearch(e.target.value)}
                                        className="pl-10"
                                    />
                                </div>
                            </div>

                            {/* Category Filter */}
                            <div className="md:w-64">
                                <select
                                    value={state.selectedCategory || ''}
                                    onChange={(e) => handleCategoryFilter(e.target.value ? parseInt(e.target.value) : null)}
                                    className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
                                >
                                    <option value="">Tất cả danh mục</option>
                                    {state.categories.map(category => (
                                        <option key={category.id} value={category.id}>
                                            {category.name}
                                        </option>
                                    ))}
                                </select>
                            </div>
                        </div>

                        {/* Active Filters */}
                        {(state.searchTerm || state.selectedCategory) && (
                            <div className="flex flex-wrap items-center gap-2 mt-4">
                                <span className="text-sm text-gray-600">Bộ lọc:</span>
                                {state.searchTerm && (
                                    <Badge variant="secondary" className="flex items-center gap-1">
                                        Tìm kiếm: {state.searchTerm}
                                        <button
                                            onClick={() => handleSearch('')}
                                            className="ml-1 rounded-full text-gray-500 hover:text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
                                            aria-label="Xóa bộ lọc tìm kiếm"
                                        >
                                            <X className="h-3 w-3" />
                                        </button>
                                    </Badge>
                                )}
                                {state.selectedCategory && (
                                    <Badge variant="secondary" className="flex items-center gap-1">
                                        Danh mục: {state.categories.find(c => c.id === state.selectedCategory)?.name}
                                        <button
                                            onClick={() => handleCategoryFilter(null)}
                                            className="ml-1 rounded-full text-gray-500 hover:text-gray-700 focus:outline-none focus:ring-2 focus:ring-blue-500"
                                            aria-label="Xóa bộ lọc danh mục"
                                        >
                                            <X className="h-3 w-3" />
                                        </button>
                                    </Badge>
                                )}
                            </div>
                        )}
                    </CardContent>
                </Card>

                {/* Results Count */}
                <div className="mb-6">
                    <p className="text-gray-600">
                        Hiển thị {state.blogs.length} trong tổng số {state.totalCount} bài viết
                    </p>
                </div>

                {/* Blog Grid */}
                {state.loading ? (
                    <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
                    {Array.from({ length: 6 }).map((_, i) => (
                        <Card key={i}>
                                <CardContent className="p-0">
                                    <Skeleton className="h-48 w-full rounded-t-lg" />
                                    <div className="p-6">
                                        <Skeleton className="h-4 w-3/4 mb-2" />
                                        <Skeleton className="h-3 w-1/2 mb-4" />
                                        <Skeleton className="h-16 w-full" />
                                    </div>
                            </CardContent>
                        </Card>
                    ))}
                    </div>
                ) : state.blogs.length > 0 ? (
                    <div className="grid grid-cols-1 gap-6 md:grid-cols-2 lg:grid-cols-3">
                        {state.blogs.map((blog) => (
                            <BlogCard key={blog.id} blog={blog} />
                        ))}
                    </div>
                ) : (
                    <div className="text-center py-12">
                        <div className="text-gray-400 mb-4">
                            <Search className="w-16 h-16 mx-auto" />
                        </div>
                        <h3 className="text-lg font-semibold text-gray-900 mb-2">Không tìm thấy bài viết</h3>
                        <p className="text-gray-600 mb-4">
                            {state.searchTerm || state.selectedCategory
                                ? 'Thử thay đổi từ khóa tìm kiếm hoặc bộ lọc'
                                : 'Chưa có bài viết nào được xuất bản'
                            }
                        </p>
                        {(state.searchTerm || state.selectedCategory) && (
                            <Button
                                onClick={() => {
                                    setState(prev => ({
                                        ...prev,
                                        searchTerm: '',
                                        selectedCategory: null,
                                        currentPage: 1
                                    }));
                                }}
                                variant="outline"
                            >
                                Xóa bộ lọc
                            </Button>
                        )}
                    </div>
                )}

                {/* Pagination */}
                {state.totalPages > 1 && (
                    <div className="flex justify-center mt-8">
                        <div className="flex items-center gap-2">
                            <Button
                                variant="outline"
                                size="sm"
                                onClick={() => setState(prev => ({ ...prev, currentPage: Math.max(1, prev.currentPage - 1) }))}
                                disabled={state.currentPage === 1}
                            >
                                Trước
                            </Button>

                            {visiblePages.map((page) => (
                                <Button
                                    key={page}
                                    variant={state.currentPage === page ? "default" : "outline"}
                                    size="sm"
                                    onClick={() => setState(prev => ({ ...prev, currentPage: page }))}
                                >
                                    {page}
                                </Button>
                            ))}

                            <Button
                                variant="outline"
                                size="sm"
                                onClick={() => setState(prev => ({ ...prev, currentPage: Math.min(state.totalPages, prev.currentPage + 1) }))}
                                disabled={state.currentPage === state.totalPages}
                            >
                                Sau
                            </Button>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
}

function getVisiblePages(currentPage: number, totalPages: number) {
    if (totalPages <= 5) {
        return Array.from({ length: totalPages }, (_, i) => i + 1);
    }

    const start = Math.min(Math.max(currentPage - 2, 1), totalPages - 4);
    return Array.from({ length: 5 }, (_, i) => start + i);
}

function BlogCard({ blog }: { blog: Blog }) {
    const formatReadingTime = (minutes: number) => {
        return minutes < 1 ? 'Dưới 1 phút đọc' : `${minutes} phút đọc`;
    };

    const truncateText = (text: string, maxLength: number) => {
        if (text.length <= maxLength) return text;
        return text.substring(0, maxLength) + '...';
    };

    return (
        <Card className="group h-full overflow-hidden transition-shadow duration-300 hover:shadow-lg">
            <Link href={`/blog/${blog.slug}`} className="flex h-full flex-col">
                <CardContent className="flex h-full flex-col p-0">
                    {/* Featured Image */}
                    {blog.featuredImageUrl ? (
                        <div className="relative overflow-hidden rounded-t-lg">
                            <img
                                src={blog.featuredImageUrl}
                                alt={blog.title}
                                className="w-full h-48 object-cover group-hover:scale-105 transition-transform duration-300"
                            />
                            {blog.isFeatured && (
                                <Badge className="absolute top-2 left-2 bg-blue-600">
                                    Nổi bật
                                </Badge>
                            )}
                        </div>
                    ) : (
                        <div className="w-full h-48 bg-gray-200 flex items-center justify-center rounded-t-lg">
                            <div className="text-gray-400 text-center">
                                <ImageIcon className="mx-auto mb-2 h-8 w-8" />
                                <div className="text-sm">Không có ảnh</div>
                            </div>
                        </div>
                    )}

                    <div className="flex flex-1 flex-col p-6">
                        {/* Category */}
                        <div className="mb-3 min-h-6">
                            {blog.category && (
                                <Badge variant="outline">
                                    {blog.category.name}
                                </Badge>
                            )}
                        </div>

                        {/* Title */}
                        <h3 className="text-xl font-semibold text-gray-900 mb-3 line-clamp-2 group-hover:text-blue-600 transition-colors">
                            {blog.title}
                        </h3>

                        {/* Excerpt */}
                        {blog.excerpt && (
                            <p className="text-gray-600 mb-4 line-clamp-3">
                                {truncateText(blog.excerpt, 120)}
                            </p>
                        )}

                        {/* Meta Information */}
                        <div className="mb-4 flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-gray-500">
                            <div className="flex items-center gap-1">
                                <User className="w-4 h-4" />
                                {blog.author?.displayName || 'Tác giả'}
                            </div>
                            <div className="flex items-center gap-1">
                                <Calendar className="w-4 h-4" />
                                {new Date(blog.createdAt).toLocaleDateString('vi-VN')}
                            </div>
                            <div className="flex items-center gap-1">
                                <Clock className="w-4 h-4" />
                                {formatReadingTime((blog as any).readingTime || 0)}
                            </div>
                        </div>

                        {/* Stats */}
                        <div className="mt-auto flex items-center gap-4 text-sm text-gray-500">
                            <div className="flex items-center gap-1">
                                <Eye className="w-4 h-4" />
                                {blog.viewCount}
                            </div>
                            <div className="flex items-center gap-1">
                                <MessageCircle className="w-4 h-4" />
                                {blog.commentCount}
                            </div>
                        </div>

                        {/* Tags */}
                        {blog.tags && blog.tags.length > 0 && (
                            <div className="flex flex-wrap gap-1 mt-3">
                                {blog.tags.slice(0, 3).map(tag => (
                                    <Badge key={tag.id} variant="secondary" className="text-xs">
                                        <Tag className="w-3 h-3 mr-1" />
                                        {tag.name}
                                    </Badge>
                                ))}
                                {blog.tags.length > 3 && (
                                    <Badge variant="secondary" className="text-xs">
                                        +{blog.tags.length - 3}
                                    </Badge>
                                )}
                            </div>
                        )}
                    </div>
                </CardContent>
                            </Link>
        </Card>
    );
}

function BlogListingSkeleton() {
    return (
        <div className="min-h-screen bg-gray-50">
            <div className="container mx-auto px-4 py-8">
                {/* Header Skeleton */}
                <div className="text-center mb-12">
                    <Skeleton className="h-10 w-32 mx-auto mb-4" />
                    <Skeleton className="h-6 w-96 mx-auto" />
                </div>

                {/* Search Skeleton */}
                <Card className="mb-8">
                    <CardContent className="pt-6">
                        <div className="flex flex-col md:flex-row gap-4">
                            <Skeleton className="h-10 flex-1" />
                            <Skeleton className="h-10 md:w-64" />
                        </div>
                    </CardContent>
                </Card>

                {/* Results Skeleton */}
                <div className="mb-6">
                    <Skeleton className="h-5 w-48" />
                </div>

                {/* Grid Skeleton */}
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
                    {Array.from({ length: 6 }).map((_, i) => (
                        <Card key={i}>
                            <CardContent className="p-0">
                                <Skeleton className="h-48 w-full rounded-t-lg" />
                                <div className="p-6">
                                    <Skeleton className="h-4 w-20 mb-3" />
                                    <Skeleton className="h-6 w-3/4 mb-3" />
                                    <Skeleton className="h-4 w-full mb-2" />
                                    <Skeleton className="h-4 w-2/3 mb-4" />
                                    <div className="flex gap-4 mb-3">
                                        <Skeleton className="h-4 w-16" />
                                        <Skeleton className="h-4 w-20" />
                                        <Skeleton className="h-4 w-16" />
                                </div>
                                    <div className="flex gap-4">
                                        <Skeleton className="h-4 w-12" />
                                        <Skeleton className="h-4 w-12" />
                                </div>
                            </div>
                        </CardContent>
                    </Card>
                ))}
                </div>
            </div>
        </div>
    );
}
