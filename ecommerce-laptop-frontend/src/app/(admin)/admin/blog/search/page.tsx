'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
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
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Blog, BlogAuthor, BlogCategory, BlogTag } from '@/types/api';
import {
    BarChart3,
    Calendar,
    Clock,
    Eye,
    Filter,
    Search,
    SlidersHorizontal,
    TrendingUp,
    User,
    X
} from 'lucide-react';
import Link from 'next/link';
import { useCallback, useEffect, useState } from 'react';
import { toast } from 'sonner';

interface SearchFilters {
    query: string;
    categories: string[];
    tags: string[];
    authors: string[];
    status: 'all' | 'published' | 'draft' | 'archived';
    dateFrom: string;
    dateTo: string;
    sortBy: 'relevance' | 'date' | 'title' | 'views' | 'comments';
    sortOrder: 'asc' | 'desc';
}

interface SearchResult extends Blog {
    relevanceScore: number;
    matchedFields: string[];
    excerpt: string;
}

interface SearchAnalytics {
    totalSearches: number;
    popularQueries: Array<{
        query: string;
        count: number;
        trend: 'up' | 'down' | 'stable';
    }>;
    noResultQueries: Array<{
        query: string;
        count: number;
    }>;
    averageResultsPerSearch: number;
    searchConversionRate: number;
}

export default function BlogSearchPage() {
    const [filters, setFilters] = useState<SearchFilters>({
        query: '',
        categories: [],
        tags: [],
        authors: [],
        status: 'all',
        dateFrom: '',
        dateTo: '',
        sortBy: 'relevance',
        sortOrder: 'desc'
    });

    const [results, setResults] = useState<SearchResult[]>([]);
    const [analytics, setAnalytics] = useState<SearchAnalytics | null>(null);
    const [categories, setCategories] = useState<BlogCategory[]>([]);
    const [tags, setTags] = useState<BlogTag[]>([]);
    const [authors, setAuthors] = useState<BlogAuthor[]>([]);
    const [loading, setLoading] = useState(false);
    const [showFilters, setShowFilters] = useState(false);
    const [searchHistory, setSearchHistory] = useState<string[]>([]);
    const [savedSearches, setSavedSearches] = useState<Array<{
        name: string;
        filters: SearchFilters;
    }>>([]);

    // Load initial data
    useEffect(() => {
        loadInitialData();
        loadSearchAnalytics();
    }, []);

    const loadInitialData = async () => {
        try {
            // Mock data loading
            const now = new Date().toISOString();
            setCategories([
                { id: 1, name: 'Technology', slug: 'technology', description: '', postCount: 15, isActive: true, sortOrder: 0, createdAt: now, updatedAt: now },
                { id: 2, name: 'Business', slug: 'business', description: '', postCount: 10, isActive: true, sortOrder: 0, createdAt: now, updatedAt: now },
                { id: 3, name: 'Lifestyle', slug: 'lifestyle', description: '', postCount: 8, isActive: true, sortOrder: 0, createdAt: now, updatedAt: now },
            ]);

            setTags([
                { id: 1, name: 'React', slug: 'react', description: '', color: '#38bdf8', isActive: true, createdAt: now, updatedAt: now, postCount: 12 },
                { id: 2, name: 'JavaScript', slug: 'javascript', description: '', color: '#f97316', isActive: true, createdAt: now, updatedAt: now, postCount: 20 },
                { id: 3, name: 'SEO', slug: 'seo', description: '', color: '#22c55e', isActive: true, createdAt: now, updatedAt: now, postCount: 8 },
            ]);

            setAuthors([
                { id: '1', email: 'john@example.com', firstName: 'John', lastName: 'Doe', displayName: 'John Doe', bio: '', profilePictureUrl: '', socialLinks: {}, postCount: 15, isActive: true, createdAt: new Date().toISOString() },
            ]);
        } catch (error) {
            console.error('Failed to load initial data:', error);
        }
    };

    const loadSearchAnalytics = async () => {
        try {
            // Mock analytics data
            setAnalytics({
                totalSearches: 1247,
                popularQueries: [
                    { query: 'react hooks', count: 156, trend: 'up' },
                    { query: 'javascript tutorial', count: 134, trend: 'stable' },
                    { query: 'seo best practices', count: 98, trend: 'down' },
                    { query: 'next.js guide', count: 87, trend: 'up' },
                    { query: 'css flexbox', count: 76, trend: 'stable' },
                ],
                noResultQueries: [
                    { query: 'advanced typescript', count: 23 },
                    { query: 'vue3 composition api', count: 18 },
                    { query: 'graphql mutations', count: 12 },
                ],
                averageResultsPerSearch: 4.2,
                searchConversionRate: 68.5
            });
        } catch (error) {
            console.error('Failed to load search analytics:', error);
        }
    };

    // Perform search
    const handleSearch = useCallback(async (searchFilters = filters) => {
        if (!searchFilters.query.trim()) {
            setResults([]);
            return;
        }

        try {
            setLoading(true);

            // Add to search history
            if (searchFilters.query && !searchHistory.includes(searchFilters.query)) {
                setSearchHistory(prev => [searchFilters.query, ...prev.slice(0, 9)]);
            }

            // Mock search results
            const now = new Date().toISOString();
            const mockResults: SearchResult[] = [
                {
                    id: 1,
                    title: 'Getting Started with React Hooks',
                    content: 'A comprehensive guide to React Hooks...',
                    excerpt: 'Learn how to use React Hooks effectively in your applications. This guide covers useState, useEffect, and custom hooks.',
                    slug: 'getting-started-react-hooks',
                    status: 'published',
                    featuredImageUrl: '',
                    metaDescription: '',

                    isFeatured: false,
                    allowComments: true,
                    authorId: 1,
                    categoryId: 1,
                    tagIds: [1, 2],
                    publishedAt: now,
                    createdAt: now,
                    updatedAt: now,
                    viewCount: 2500,
                    likeCount: 45,
                    commentCount: 12,
                    relevanceScore: 95,
                    matchedFields: ['title', 'content', 'tags']
                },
                // Add more mock results...
            ];

            setResults(mockResults);
        } catch (error) {
            console.error('Search failed:', error);
            toast.error('Không thể thực hiện tìm kiếm');
        } finally {
            setLoading(false);
        }
    }, [filters, searchHistory]);

    // Update filter
    const updateFilter = <K extends keyof SearchFilters>(key: K, value: SearchFilters[K]) => {
        setFilters(prev => ({
            ...prev,
            [key]: value
        }));
    };

    // Add/remove from array filters
    const toggleArrayFilter = (key: 'categories' | 'tags' | 'authors', value: string) => {
        setFilters(prev => ({
            ...prev,
            [key]: prev[key].includes(value)
                ? prev[key].filter(item => item !== value)
                : [...prev[key], value]
        }));
    };

    // Clear all filters
    const clearFilters = () => {
        setFilters({
            query: '',
            categories: [],
            tags: [],
            authors: [],
            status: 'all',
            dateFrom: '',
            dateTo: '',
            sortBy: 'relevance',
            sortOrder: 'desc'
        });
        setResults([]);
    };

    // Save current search
    const saveCurrentSearch = () => {
        const name = prompt('Tên cho tìm kiếm đã lưu:');
        if (name) {
            setSavedSearches(prev => [...prev, { name, filters }]);
            toast.success('Đã lưu tìm kiếm');
        }
    };

    // Load saved search
    const loadSavedSearch = (savedFilters: SearchFilters) => {
        setFilters(savedFilters);
        handleSearch(savedFilters);
    };

    // Get trend icon
    const getTrendIcon = (trend: 'up' | 'down' | 'stable') => {
        switch (trend) {
            case 'up':
                return <TrendingUp className="w-4 h-4 text-green-600" />;
            case 'down':
                return <TrendingUp className="w-4 h-4 text-red-600 rotate-180" />;
            case 'stable':
                return <BarChart3 className="w-4 h-4 text-gray-600" />;
        }
    };

    // Get status badge
    const getStatusBadge = (status: string) => {
        const variants = {
            published: 'default',
            draft: 'secondary',
            archived: 'outline'
        };
        return <Badge variant={variants[status as keyof typeof variants] as any}>{status}</Badge>;
    };

    return (
        <div className="p-6 max-w-7xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Tìm kiếm Blog</h1>
                    <p className="text-gray-500">Tìm kiếm nâng cao và phân tích xu hướng tìm kiếm</p>
                </div>
                <Button
                    variant="outline"
                    onClick={() => setShowFilters(!showFilters)}
                    className="flex items-center gap-2"
                >
                    <SlidersHorizontal className="w-4 h-4" />
                    Bộ lọc {showFilters ? 'ẩn' : 'hiện'}
                </Button>
            </div>

            <Tabs defaultValue="search" className="space-y-6">
                <TabsList className="grid w-full grid-cols-3">
                    <TabsTrigger value="search">Tìm kiếm</TabsTrigger>
                    <TabsTrigger value="analytics">Phân tích</TabsTrigger>
                    <TabsTrigger value="saved">Đã lưu</TabsTrigger>
                </TabsList>

                {/* Search Tab */}
                <TabsContent value="search" className="space-y-6">
                    {/* Search Bar */}
                    <Card>
                        <CardContent className="pt-6">
                            <div className="flex gap-2">
                                <div className="flex-1 relative">
                                    <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400 w-5 h-5" />
                                    <Input
                                        placeholder="Tìm kiếm bài viết, tiêu đề, nội dung..."
                                        value={filters.query}
                                        onChange={(e) => updateFilter('query', e.target.value)}
                                        onKeyPress={(e) => e.key === 'Enter' && handleSearch()}
                                        className="pl-12 h-12 text-lg"
                                    />
                                </div>
                                <Button
                                    onClick={() => handleSearch()}
                                    disabled={loading}
                                    className="h-12 px-8"
                                >
                                    {loading ? (
                                        <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-white" />
                                    ) : (
                                        <Search className="w-5 h-5" />
                                    )}
                                </Button>
                            </div>

                            {/* Search History */}
                            {searchHistory.length > 0 && (
                                <div className="mt-4">
                                    <Label className="text-sm font-medium mb-2 block">Tìm kiếm gần đây:</Label>
                                    <div className="flex flex-wrap gap-2">
                                        {searchHistory.map((query, index) => (
                                            <Button
                                                key={index}
                                                variant="outline"
                                                size="sm"
                                                onClick={() => {
                                                    updateFilter('query', query);
                                                    handleSearch({ ...filters, query });
                                                }}
                                                className="text-xs"
                                            >
                                                <Clock className="w-3 h-3 mr-1" />
                                                {query}
                                            </Button>
                                        ))}
                                    </div>
                                </div>
                            )}
                        </CardContent>
                    </Card>

                    {/* Advanced Filters */}
                    {showFilters && (
                        <Card>
                            <CardHeader>
                                <CardTitle className="flex items-center gap-2">
                                    <Filter className="w-5 h-5" />
                                    Bộ lọc nâng cao
                                </CardTitle>
                                <div className="flex gap-2">
                                    <Button variant="outline" size="sm" onClick={saveCurrentSearch}>
                                        Lưu tìm kiếm
                                    </Button>
                                    <Button variant="outline" size="sm" onClick={clearFilters}>
                                        <X className="w-4 h-4 mr-1" />
                                        Xóa bộ lọc
                                    </Button>
                                </div>
                            </CardHeader>
                            <CardContent className="space-y-4">
                                {/* Categories Filter */}
                                <div>
                                    <Label className="text-sm font-medium mb-2 block">Danh mục</Label>
                                    <div className="flex flex-wrap gap-2">
                                        {categories.map((category) => (
                                            <div key={category.id} className="flex items-center space-x-2">
                                                <Checkbox
                                                    id={`category-${category.id}`}
                                                    checked={filters.categories.includes(String(category.id))}
                                                    onCheckedChange={() => toggleArrayFilter('categories', String(category.id))}
                                                />
                                                <Label
                                                    htmlFor={`category-${category.id}`}
                                                    className="text-sm cursor-pointer"
                                                >
                                                    {category.name} ({category.postCount})
                                                </Label>
                                            </div>
                                        ))}
                                    </div>
                                </div>

                                {/* Tags Filter */}
                                <div>
                                    <Label className="text-sm font-medium mb-2 block">Thẻ</Label>
                                    <div className="flex flex-wrap gap-2">
                                        {tags.map((tag) => (
                                            <div key={tag.id} className="flex items-center space-x-2">
                                                <Checkbox
                                                    id={`tag-${tag.id}`}
                                                    checked={filters.tags.includes(String(tag.id))}
                                                    onCheckedChange={() => toggleArrayFilter('tags', String(tag.id))}
                                                />
                                                <Label
                                                    htmlFor={`tag-${tag.id}`}
                                                    className="text-sm cursor-pointer"
                                                >
                                                    #{tag.name} ({tag.postCount})
                                                </Label>
                                            </div>
                                        ))}
                                    </div>
                                </div>

                                {/* Date Range and Status */}
                                <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
                                    <div>
                                        <Label htmlFor="dateFrom">Từ ngày</Label>
                                        <Input
                                            id="dateFrom"
                                            type="date"
                                            value={filters.dateFrom}
                                            onChange={(e) => updateFilter('dateFrom', e.target.value)}
                                        />
                                    </div>
                                    <div>
                                        <Label htmlFor="dateTo">Đến ngày</Label>
                                        <Input
                                            id="dateTo"
                                            type="date"
                                            value={filters.dateTo}
                                            onChange={(e) => updateFilter('dateTo', e.target.value)}
                                        />
                                    </div>
                                    <div>
                                        <Label htmlFor="status">Trạng thái</Label>
                                        <Select
                                            value={filters.status}
                                            onValueChange={(value: any) => updateFilter('status', value)}
                                        >
                                            <SelectTrigger>
                                                <SelectValue />
                                            </SelectTrigger>
                                            <SelectContent>
                                                <SelectItem value="all">Tất cả</SelectItem>
                                                <SelectItem value="published">đã xuất bản</SelectItem>
                                                <SelectItem value="draft">Bản nháp</SelectItem>
                                                <SelectItem value="archived">Lưu trữ</SelectItem>
                                            </SelectContent>
                                        </Select>
                                    </div>
                                    <div>
                                        <Label htmlFor="sortBy">Sắp xếp theo</Label>
                                        <Select
                                            value={filters.sortBy}
                                            onValueChange={(value: any) => updateFilter('sortBy', value)}
                                        >
                                            <SelectTrigger>
                                                <SelectValue />
                                            </SelectTrigger>
                                            <SelectContent>
                                                <SelectItem value="relevance">độ liên quan</SelectItem>
                                                <SelectItem value="date">Ngày tạo</SelectItem>
                                                <SelectItem value="title">Tiêu đề</SelectItem>
                                                <SelectItem value="views">Lượt xem</SelectItem>
                                                <SelectItem value="comments">Bình luận</SelectItem>
                                            </SelectContent>
                                        </Select>
                                    </div>
                                </div>
                            </CardContent>
                        </Card>
                    )}

                    {/* Search Results */}
                    <Card>
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Search className="w-5 h-5" />
                                Kết quả tìm kiếm ({results.length})
                            </CardTitle>
                            {filters.query && (
                                <CardDescription>
                                    Kết quả cho "{filters.query}"
                                    {results.length > 0 && ` - tìm thấy trong ${(performance.now() / 1000).toFixed(2)}s`}
                                </CardDescription>
                            )}
                        </CardHeader>
                        <CardContent>
                            {loading ? (
                                <div className="flex items-center justify-center py-12">
                                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900" />
                                </div>
                            ) : results.length > 0 ? (
                                <div className="space-y-4">
                                    {results.map((result) => (
                                        <div key={result.id} className="border rounded-lg p-4 hover:bg-gray-50 transition-colors">
                                            <div className="flex items-start justify-between">
                                                <div className="flex-1">
                                                    <div className="flex items-center gap-3 mb-2">
                                                        <Link
                                                            href={`/admin/blog/posts/${result.id}/view`}
                                                            className="text-lg font-semibold text-blue-600 hover:underline"
                                                        >
                                                            {result.title}
                                                        </Link>
                                                        {getStatusBadge(result.status)}
                                                        <Badge variant="outline" className="text-xs">
                                                            {result.relevanceScore}% match
                                                        </Badge>
                                                    </div>
                                                    <p className="text-gray-600 mb-3">{result.excerpt}</p>
                                                    <div className="flex items-center gap-4 text-sm text-gray-500">
                                                        <span className="flex items-center gap-1">
                                                            <User className="w-4 h-4" />
                                                            {result.authorId}
                                                        </span>
                                                        <span className="flex items-center gap-1">
                                                            <Calendar className="w-4 h-4" />
                                                            {new Date(result.publishedAt || result.createdAt).toLocaleDateString('vi-VN')}
                                                        </span>
                                                        <span className="flex items-center gap-1">
                                                            <Eye className="w-4 h-4" />
                                                            {result.viewCount} lượt xem
                                                        </span>
                                                    </div>
                                                    <div className="flex flex-wrap gap-2 mt-3">
                                                        {result.matchedFields.map((field) => (
                                                            <Badge key={field} variant="secondary" className="text-xs">
                                                                Tìm thấy trong: {field}
                                                            </Badge>
                                                        ))}
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            ) : (
                                <div className="text-center py-12">
                                    <Search className="w-12 h-12 text-gray-300 mx-auto mb-4" />
                                    <p className="text-gray-500 mb-2">
                                        {filters.query
                                            ? 'Không tìm thấy kết quả phù hợp'
                                            : 'Nhập từ khóa để bắt đầu tìm kiếm'
                                        }
                                    </p>
                                    {filters.query && (
                                        <p className="text-sm text-gray-400">
                                            Thử tìm kiếm với từ khóa khác hoặc điều chỉnh bộ lọc
                                        </p>
                                    )}
                                </div>
                            )}
                        </CardContent>
                    </Card>
                </TabsContent>

                {/* Analytics Tab */}
                <TabsContent value="analytics" className="space-y-6">
                    {analytics && (
                        <>
                            {/* Overview Cards */}
                            <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
                                <Card>
                                    <CardContent className="p-6">
                                        <div className="flex items-center gap-3">
                                            <Search className="w-8 h-8 text-blue-600" />
                                            <div>
                                                <p className="text-sm text-gray-600">Tổng tìm kiếm</p>
                                                <p className="text-2xl font-bold">{analytics.totalSearches.toLocaleString()}</p>
                                            </div>
                                        </div>
                                    </CardContent>
                                </Card>

                                <Card>
                                    <CardContent className="p-6">
                                        <div className="flex items-center gap-3">
                                            <BarChart3 className="w-8 h-8 text-green-600" />
                                            <div>
                                                <p className="text-sm text-gray-600">Kết quả TB/tìm kiếm</p>
                                                <p className="text-2xl font-bold">{analytics.averageResultsPerSearch}</p>
                                            </div>
                                        </div>
                                    </CardContent>
                                </Card>

                                <Card>
                                    <CardContent className="p-6">
                                        <div className="flex items-center gap-3">
                                            <TrendingUp className="w-8 h-8 text-purple-600" />
                                            <div>
                                                <p className="text-sm text-gray-600">Tỉ lệ chuyển đổi</p>
                                                <p className="text-2xl font-bold">{analytics.searchConversionRate}%</p>
                                            </div>
                                        </div>
                                    </CardContent>
                                </Card>

                                <Card>
                                    <CardContent className="p-6">
                                        <div className="flex items-center gap-3">
                                            <X className="w-8 h-8 text-red-600" />
                                            <div>
                                                <p className="text-sm text-gray-600">Không có KQ</p>
                                                <p className="text-2xl font-bold">{analytics.noResultQueries.length}</p>
                                            </div>
                                        </div>
                                    </CardContent>
                                </Card>
                            </div>

                            {/* Popular Queries */}
                            <Card>
                                <CardHeader>
                                    <CardTitle>Từ khóa tìm kiếm phổ biến</CardTitle>
                                    <CardDescription>
                                        Các truy vấn tìm kiếm được sử dụng nhiều nhất
                                    </CardDescription>
                                </CardHeader>
                                <CardContent>
                                    <Table>
                                        <TableHeader>
                                            <TableRow>
                                                <TableHead>Từ khóa</TableHead>
                                                <TableHead>Số lần</TableHead>
                                                <TableHead>Xu hướng</TableHead>
                                                <TableHead className="text-right">Hành động</TableHead>
                                            </TableRow>
                                        </TableHeader>
                                        <TableBody>
                                            {analytics.popularQueries.map((query, index) => (
                                                <TableRow key={index}>
                                                    <TableCell className="font-medium">{query.query}</TableCell>
                                                    <TableCell>{query.count}</TableCell>
                                                    <TableCell>
                                                        <div className="flex items-center gap-2">
                                                            {getTrendIcon(query.trend)}
                                                            <span className="capitalize">{query.trend}</span>
                                                        </div>
                                                    </TableCell>
                                                    <TableCell className="text-right">
                                                        <Button
                                                            variant="ghost"
                                                            size="sm"
                                                            onClick={() => {
                                                                updateFilter('query', query.query);
                                                                handleSearch({ ...filters, query: query.query });
                                                            }}
                                                        >
                                                            <Search className="w-4 h-4" />
                                                        </Button>
                                                    </TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </CardContent>
                            </Card>

                            {/* No Result Queries */}
                            <Card>
                                <CardHeader>
                                    <CardTitle>Tìm kiếm không có kết quả</CardTitle>
                                    <CardDescription>
                                        Các từ khóa cần tạo nội dung mới
                                    </CardDescription>
                                </CardHeader>
                                <CardContent>
                                    <Table>
                                        <TableHeader>
                                            <TableRow>
                                                <TableHead>Từ khóa</TableHead>
                                                <TableHead>Số lần</TableHead>
                                                <TableHead className="text-right">Hành động</TableHead>
                                            </TableRow>
                                        </TableHeader>
                                        <TableBody>
                                            {analytics.noResultQueries.map((query, index) => (
                                                <TableRow key={index}>
                                                    <TableCell className="font-medium">{query.query}</TableCell>
                                                    <TableCell>{query.count}</TableCell>
                                                    <TableCell className="text-right">
                                                        <div className="flex gap-2">
                                                            <Button
                                                                variant="outline"
                                                                size="sm"
                                                                onClick={() => {
                                                                    // Create new post with this keyword
                                                                    window.open(`/admin/blog/posts/new?keyword=${encodeURIComponent(query.query)}`, '_blank');
                                                                }}
                                                            >
                                                                 Tạo bài viết
                                                            </Button>
                                                        </div>
                                                    </TableCell>
                                                </TableRow>
                                            ))}
                                        </TableBody>
                                    </Table>
                                </CardContent>
                            </Card>
                        </>
                    )}
                </TabsContent>

                {/* Saved Searches Tab */}
                <TabsContent value="saved" className="space-y-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>Tìm kiếm đã lưu</CardTitle>
                            <CardDescription>
                                Quản lý các bộ lọc tìm kiếm đã lưu
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            {savedSearches.length > 0 ? (
                                <div className="space-y-4">
                                    {savedSearches.map((saved, index) => (
                                        <div key={index} className="border rounded-lg p-4">
                                            <div className="flex items-center justify-between">
                                                <div>
                                                    <h3 className="font-semibold">{saved.name}</h3>
                                                    <div className="flex flex-wrap gap-2 mt-2">
                                                        {saved.filters.query && (
                                                            <Badge variant="outline">
                                                                Từ khóa: {saved.filters.query}
                                                            </Badge>
                                                        )}
                                                        {saved.filters.categories.length > 0 && (
                                                            <Badge variant="outline">
                                                                {saved.filters.categories.length} danh mục
                                                            </Badge>
                                                        )}
                                                        {saved.filters.tags.length > 0 && (
                                                            <Badge variant="outline">
                                                                {saved.filters.tags.length} thẻ
                                                            </Badge>
                                                        )}
                                                        {saved.filters.status !== 'all' && (
                                                            <Badge variant="outline">
                                                                Trạng thái: {saved.filters.status}
                                                            </Badge>
                                                        )}
                                                    </div>
                                                </div>
                                                <div className="flex gap-2">
                                                    <Button
                                                        variant="outline"
                                                        size="sm"
                                                        onClick={() => loadSavedSearch(saved.filters)}
                                                    >
                                                        <Search className="w-4 h-4 mr-1" />
                                                        Tìm kiếm
                                                    </Button>
                                                    <Button
                                                        variant="ghost"
                                                        size="sm"
                                                        onClick={() => {
                                                            setSavedSearches(prev => prev.filter((_, i) => i !== index));
                                                            toast.success('Đã xóa tìm kiếm đã lưu');
                                                        }}
                                                    >
                                                        <X className="w-4 h-4" />
                                                    </Button>
                                                </div>
                                            </div>
                                        </div>
                                    ))}
                                </div>
                            ) : (
                                <div className="text-center py-12">
                                    <Search className="w-12 h-12 text-gray-300 mx-auto mb-4" />
                                    <p className="text-gray-500 mb-2">Chưa có tìm kiếm nào được lưu</p>
                                    <p className="text-sm text-gray-400">
                                        Lưu các bộ lọc tìm kiếm để sử dụng lại sau
                                    </p>
                                </div>
                            )}
                        </CardContent>
                    </Card>
                </TabsContent>
            </Tabs>
        </div>
    );
}
