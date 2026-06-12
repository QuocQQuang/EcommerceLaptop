'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { useAdminAuth } from '@/contexts/AdminAuthContext';
import { blogService } from '@/services/blogService';
import { BlogAnalytics } from '@/types/api';
import {
    BarChart3,
    Calendar,
    Eye,
    Edit,
    FileText,
    Heart,
    MessageCircle,
    Plus,
    Share2,
    TrendingUp
} from 'lucide-react';
import Link from 'next/link';
import { useEffect, useState } from 'react';

export default function BlogDashboardPage() {
    const { isAuthenticated, isLoading: authLoading } = useAdminAuth();
    const [analytics, setAnalytics] = useState<BlogAnalytics | null>(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        if (!isAuthenticated || authLoading) return;

        const fetchAnalytics = async () => {
            try {
                setLoading(true);
                const data = await blogService.getBlogAnalytics();
                setAnalytics(data);
            } catch (err: any) {
                setError(err.message || 'Failed to fetch analytics');
            } finally {
                setLoading(false);
            }
        };

        fetchAnalytics();
    }, [isAuthenticated, authLoading]);

    if (authLoading || loading) {
        return <BlogDashboardSkeleton />;
    }

    if (error) {
        return (
            <div className="p-6">
                <div className="flex items-center justify-center h-64">
                    <div className="text-center">
                        <h3 className="text-lg font-semibold text-gray-900 mb-2">Error loading dashboard</h3>
                        <p className="text-gray-600 mb-4">{error}</p>
                        <Button onClick={() => window.location.reload()}>
                            Try Again
                        </Button>
                    </div>
                </div>
            </div>
        );
    }

    return (
        <div className="p-6 space-y-6">
            {/* Header */}
            <div className="flex justify-between items-center">
                <div>
                    <h1 className="text-3xl font-bold text-gray-900">Blog Dashboard</h1>
                    <p className="text-gray-600 mt-1">
                        Quản lý và theo dõi hiệu suất blog của bạn
                    </p>
                </div>
                <div className="flex gap-3">
                    <Button variant="outline" asChild>
                        <Link href="/admin/blog/posts">
                            <FileText className="w-4 h-4 mr-2" />
                            Quản lý bài viết
                        </Link>
                    </Button>
                    <Button asChild>
                        <Link href="/admin/blog/posts/new">
                            <Plus className="w-4 h-4 mr-2" />
                            Tạo bài viết
                        </Link>
                    </Button>
                </div>
            </div>

            {/* Stats Overview */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                <StatsCard
                    title="Tổng bài viết"
                    value={analytics?.totalBlogs || 0}
                    icon={FileText}
                    description={`${analytics?.publishedBlogs || 0} đã xuất bản`}
                    color="blue"
                />
                <StatsCard
                    title="Lượt xem"
                    value={analytics?.totalViews || 0}
                    icon={Eye}
                    description="Trong tháng này"
                    color="green"
                />
                <StatsCard
                    title="Bình luận"
                    value={analytics?.totalComments || 0}
                    icon={MessageCircle}
                    description="Tương tác từ độc giả"
                    color="purple"
                />
                <StatsCard
                    title="Lượt thích"
                    value={analytics?.totalLikes || 0}
                    icon={Heart}
                    description="Phản hồi tích cực"
                    color="red"
                />
            </div>

            {/* Quick Stats */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <Calendar className="w-5 h-5" />
                            Trạng thái bài viết
                        </CardTitle>
                        <CardDescription>
                            Phân bổ bài viết theo trạng thái
                        </CardDescription>
                    </CardHeader>
                    <CardContent className="space-y-4">
                        <div className="flex justify-between items-center">
                            <span className="text-sm font-medium">Đã xuất bản</span>
                            <Badge variant="default">{analytics?.publishedBlogs || 0}</Badge>
                        </div>
                        <div className="flex justify-between items-center">
                            <span className="text-sm font-medium">Bản nháp</span>
                            <Badge variant="secondary">{analytics?.draftBlogs || 0}</Badge>
                        </div>
                        <div className="flex justify-between items-center">
                            <span className="text-sm font-medium">Đã lên lịch</span>
                            <Badge variant="outline">{analytics?.scheduledBlogs || 0}</Badge>
                        </div>
                        <div className="flex justify-between items-center">
                            <span className="text-sm font-medium">Đã lưu trữ</span>
                            <Badge variant="destructive">{analytics?.archivedBlogs || 0}</Badge>
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle className="flex items-center gap-2">
                            <TrendingUp className="w-5 h-5" />
                            Bài viết phổ biến
                        </CardTitle>
                        <CardDescription>
                            Top bài viết có lượt xem cao nhất
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        {analytics?.topBlogs && analytics.topBlogs.length > 0 ? (
                            <div className="space-y-3">
                                {analytics.topBlogs.slice(0, 5).map((blog, index) => (
                                    <div key={blog.id} className="flex items-center gap-3 p-2 rounded-lg hover:bg-gray-50">
                                        <div className="w-6 h-6 rounded-full bg-blue-100 flex items-center justify-center text-xs font-medium text-blue-700">
                                            {index + 1}
                                        </div>
                                        <div className="flex-1 min-w-0">
                                            <Link
                                                href={`/admin/blog/posts/${blog.id}/view`}
                                                className="text-sm font-medium text-gray-900 hover:text-blue-600 truncate block"
                                            >
                                                {blog.title}
                                            </Link>
                                            <div className="flex items-center gap-2 text-xs text-gray-500">
                                                <Eye className="w-3 h-3" />
                                                {blog.viewCount.toLocaleString()} lượt xem
                                            </div>
                                        </div>
                                        <Button asChild variant="outline" size="sm">
                                            <Link href={`/admin/blog/posts/${blog.id}/edit`}>
                                                <Edit className="w-4 h-4 mr-2" />
                                                Sửa
                                            </Link>
                                        </Button>
                                    </div>
                                ))}
                            </div>
                        ) : (
                            <div className="text-center py-4 text-gray-500">
                                <FileText className="w-8 h-8 mx-auto mb-2 opacity-50" />
                                <p>Chưa có dữ liệu</p>
                            </div>
                        )}
                    </CardContent>
                </Card>
            </div>

            {/* Recent Activity */}
            <Card>
                <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                        <BarChart3 className="w-5 h-5" />
                        Hoạt động gần đây
                    </CardTitle>
                    <CardDescription>
                        Các hoạt động blog mới nhất
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    {analytics?.recentActivity && analytics.recentActivity.length > 0 ? (
                        <div className="space-y-3">
                            {analytics.recentActivity.slice(0, 10).map((activity, index) => (
                                <div key={index} className="flex items-center gap-3 p-3 border border-gray-200 rounded-lg">
                                    <div className="w-8 h-8 rounded-full bg-blue-100 flex items-center justify-center">
                                        {activity.type === 'blog_created' && <Plus className="w-4 h-4 text-blue-600" />}
                                        {activity.type === 'blog_published' && <FileText className="w-4 h-4 text-green-600" />}
                                        {activity.type === 'comment_added' && <MessageCircle className="w-4 h-4 text-purple-600" />}
                                    </div>
                                    <div className="flex-1">
                                        <p className="text-sm font-medium text-gray-900">
                                            {activity.title}
                                        </p>
                                        <div className="flex items-center gap-2 text-xs text-gray-500">
                                            <span>{new Date(activity.timestamp).toLocaleDateString('vi-VN')}</span>
                                            {activity.author && (
                                                <>
                                                    <span></span>
                                                    <span>{activity.author}</span>
                                                </>
                                            )}
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    ) : (
                        <div className="text-center py-8 text-gray-500">
                            <BarChart3 className="w-12 h-12 mx-auto mb-2 opacity-50" />
                            <p>Chưa có hoạt động nào</p>
                        </div>
                    )}
                </CardContent>
            </Card>

            {/* Quick Actions */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                <QuickActionCard
                    title="Quản lý bài viết"
                    description="Sửa, ẩn hoặc xóa bài viết"
                    href="/admin/blog/posts"
                    icon={FileText}
                />
                <QuickActionCard
                    title="Tạo bài viết"
                    description="Viết bài viết mới"
                    href="/admin/blog/posts/new"
                    icon={Plus}
                />
                <QuickActionCard
                    title="Quản lý danh mục"
                    description="Tổ chức danh mục blog"
                    href="/admin/blog/categories"
                    icon={FileText}
                />
                <QuickActionCard
                    title="Quản lý tags"
                    description="Thêm và sửa tags"
                    href="/admin/blog/tags"
                    icon={Share2}
                />
                <QuickActionCard
                    title="Xem báo cáo"
                    description="Phân tích chi tiết"
                    href="/admin/blog/analytics"
                    icon={BarChart3}
                />
            </div>
        </div>
    );
}

function StatsCard({
    title,
    value,
    icon: Icon,
    description,
    color
}: {
    title: string;
    value: number;
    icon: any;
    description: string;
    color: string;
}) {
    const colorClasses = {
        blue: 'text-blue-600 bg-blue-100',
        green: 'text-green-600 bg-green-100',
        purple: 'text-purple-600 bg-purple-100',
        red: 'text-red-600 bg-red-100',
    };

    return (
        <Card>
            <CardContent className="p-6">
                <div className="flex items-center justify-between">
                    <div>
                        <p className="text-sm font-medium text-gray-600">{title}</p>
                        <p className="text-2xl font-bold text-gray-900 mt-1">
                            {value.toLocaleString()}
                        </p>
                        <p className="text-xs text-gray-500 mt-1">{description}</p>
                    </div>
                    <div className={`p-3 rounded-lg ${colorClasses[color as keyof typeof colorClasses]}`}>
                        <Icon className="w-6 h-6" />
                    </div>
                </div>
            </CardContent>
        </Card>
    );
}

function QuickActionCard({
    title,
    description,
    href,
    icon: Icon
}: {
    title: string;
    description: string;
    href: string;
    icon: any;
}) {
    return (
        <Link href={href}>
            <Card className="hover:shadow-md transition-shadow cursor-pointer">
                <CardContent className="p-4">
                    <div className="flex items-center gap-3">
                        <div className="p-2 rounded-lg bg-blue-100">
                            <Icon className="w-5 h-5 text-blue-600" />
                        </div>
                        <div>
                            <h3 className="font-medium text-gray-900">{title}</h3>
                            <p className="text-sm text-gray-600">{description}</p>
                        </div>
                    </div>
                </CardContent>
            </Card>
        </Link>
    );
}

function BlogDashboardSkeleton() {
    return (
        <div className="p-6 space-y-6">
            {/* Header Skeleton */}
            <div className="flex justify-between items-center">
                <div>
                    <Skeleton className="h-8 w-48 mb-2" />
                    <Skeleton className="h-4 w-64" />
                </div>
                <Skeleton className="h-10 w-32" />
            </div>

            {/* Stats Skeleton */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                {Array.from({ length: 4 }).map((_, i) => (
                    <Card key={i}>
                        <CardContent className="p-6">
                            <div className="flex items-center justify-between">
                                <div className="space-y-2">
                                    <Skeleton className="h-4 w-20" />
                                    <Skeleton className="h-6 w-16" />
                                    <Skeleton className="h-3 w-24" />
                                </div>
                                <Skeleton className="h-12 w-12 rounded-lg" />
                            </div>
                        </CardContent>
                    </Card>
                ))}
            </div>

            {/* Cards Skeleton */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                {Array.from({ length: 2 }).map((_, i) => (
                    <Card key={i}>
                        <CardHeader>
                            <Skeleton className="h-5 w-32" />
                            <Skeleton className="h-4 w-48" />
                        </CardHeader>
                        <CardContent className="space-y-3">
                            {Array.from({ length: 4 }).map((_, j) => (
                                <div key={j} className="flex justify-between items-center">
                                    <Skeleton className="h-4 w-24" />
                                    <Skeleton className="h-5 w-8" />
                                </div>
                            ))}
                        </CardContent>
                    </Card>
                ))}
            </div>
        </div>
    );
}
