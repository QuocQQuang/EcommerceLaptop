'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
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
import { useAdminAuth } from '@/contexts/AdminAuthContext';
import { blogService } from '@/services/blogService';
import { BlogAnalytics } from '@/types/api';
import {
    BarChart3,
    Calendar,
    Download,
    Eye,
    MessageCircle,
    RefreshCw,
    TrendingDown,
    TrendingUp,
    Users
} from 'lucide-react';
import { useCallback, useEffect, useState } from 'react';

interface AnalyticsPageState {
    analytics: BlogAnalytics | null;
    loading: boolean;
    refreshing: boolean;
    period: 'week' | 'month' | 'quarter' | 'year';
}

interface StatsCardProps {
    title: string;
    value: number | string;
    change?: number;
    icon: React.ReactNode;
    color: string;
}

function StatsCard({ title, value, change, icon, color }: StatsCardProps) {
    const isPositive = change && change > 0;
    const isNegative = change && change < 0;

    return (
        <Card>
            <CardContent className="pt-6">
                <div className="flex items-center justify-between">
                    <div>
                        <p className="text-sm font-medium text-gray-600">{title}</p>
                        <p className="text-2xl font-bold text-gray-900">{value}</p>
                        {change !== undefined && (
                            <div className="flex items-center gap-1 mt-1">
                                {isPositive ? (
                                    <TrendingUp className="w-4 h-4 text-green-600" />
                                ) : isNegative ? (
                                    <TrendingDown className="w-4 h-4 text-red-600" />
                                ) : null}
                                <span className={`text-sm ${isPositive ? 'text-green-600' :
                                    isNegative ? 'text-red-600' : 'text-gray-600'
                                    }`}>
                                    {change > 0 ? '+' : ''}{change}%
                                </span>
                                <span className="text-sm text-gray-500">t k trc</span>
                            </div>
                        )}
                    </div>
                    <div className={`p-3 rounded-full ${color}`}>
                        {icon}
                    </div>
                </div>
            </CardContent>
        </Card>
    );
}

export default function AnalyticsPage() {
    const { isAuthenticated, isLoading: authLoading } = useAdminAuth();
    const [state, setState] = useState<AnalyticsPageState>({
        analytics: null,
        loading: true,
        refreshing: false,
        period: 'month',
    });

    // Load analytics data
    const loadAnalytics = useCallback(async () => {
        if (!isAuthenticated || authLoading) return;

        try {
            setState(prev => ({ ...prev, loading: true }));

            // Get date range based on period
            const endDate = new Date();
            const startDate = new Date();

            switch (state.period) {
                case 'week':
                    startDate.setDate(endDate.getDate() - 7);
                    break;
                case 'month':
                    startDate.setMonth(endDate.getMonth() - 1);
                    break;
                case 'quarter':
                    startDate.setMonth(endDate.getMonth() - 3);
                    break;
                case 'year':
                    startDate.setFullYear(endDate.getFullYear() - 1);
                    break;
            }

            const analytics = await blogService.getBlogAnalytics();

            setState(prev => ({ ...prev, analytics, loading: false }));
        } catch (error) {
            console.error('Failed to load analytics:', error);
            setState(prev => ({ ...prev, loading: false }));
        }
    }, [state.period, isAuthenticated, authLoading]);

    useEffect(() => {
        loadAnalytics();
    }, [loadAnalytics]);

    // Refresh analytics
    const refreshAnalytics = async () => {
        setState(prev => ({ ...prev, refreshing: true }));
        try {
            await loadAnalytics();
        } finally {
            setState(prev => ({ ...prev, refreshing: false }));
        }
    };

    // Export analytics report
    const exportReport = async () => {
        try {
            // Mock export functionality
            const csvContent = `Date,Posts,Views,Comments\n${new Date().toLocaleDateString()},${analytics?.totalBlogs || 0},${analytics?.totalViews || 0},${analytics?.totalComments || 0}`;
            const blob = new Blob([csvContent], { type: 'text/csv' });

            // Create download link
            const url = URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = url;
            link.download = `blog-analytics-${new Date().toISOString().split('T')[0]}.csv`;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(url);
        } catch (error) {
            console.error('Failed to export report:', error);
        }
    };

    // Get period display name
    const getPeriodDisplay = (period: string) => {
        switch (period) {
            case 'week': return '7 ngy qua';
            case 'month': return '30 ngy qua';
            case 'quarter': return '3 thng qua';
            case 'year': return '1 nm qua';
            default: return period;
        }
    };

    // Format number with commas
    const formatNumber = (num: number) => {
        return new Intl.NumberFormat('vi-VN').format(num);
    };

    if (authLoading || state.loading) {
        return (
            <div className="p-6">
                <div className="flex items-center justify-center h-64">
                    <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900" />
                </div>
            </div>
        );
    }

    const analytics = state.analytics;

    return (
        <div className="p-6 max-w-6xl mx-auto space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-gray-900">Thng k Blog</h1>
                    <p className="text-gray-500">Phn tch hiu sut v tng tc blog</p>
                </div>
                <div className="flex items-center gap-2">
                    <Select
                        value={state.period}
                        onValueChange={(value: 'week' | 'month' | 'quarter' | 'year') =>
                            setState(prev => ({ ...prev, period: value }))
                        }
                    >
                        <SelectTrigger className="w-40">
                            <SelectValue />
                        </SelectTrigger>
                        <SelectContent>
                            <SelectItem value="week">7 ngy qua</SelectItem>
                            <SelectItem value="month">30 ngy qua</SelectItem>
                            <SelectItem value="quarter">3 thng qua</SelectItem>
                            <SelectItem value="year">1 nm qua</SelectItem>
                        </SelectContent>
                    </Select>

                    <Button variant="outline" onClick={refreshAnalytics} disabled={state.refreshing}>
                        <RefreshCw className={`w-4 h-4 mr-2 ${state.refreshing ? 'animate-spin' : ''}`} />
                        Lm mi
                    </Button>

                    <Button variant="outline" onClick={exportReport}>
                        <Download className="w-4 h-4 mr-2" />
                        Xut bo co
                    </Button>
                </div>
            </div>

            {/* Overview Stats */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
                <StatsCard
                    title="Tng lt xem"
                    value={formatNumber(analytics?.totalViews || 0)}
                    icon={<Eye className="w-6 h-6 text-white" />}
                    color="bg-blue-500"
                />
                <StatsCard
                    title="Tng bi vit"
                    value={formatNumber(analytics?.totalBlogs || 0)}
                    icon={<BarChart3 className="w-6 h-6 text-white" />}
                    color="bg-green-500"
                />
                <StatsCard
                    title="Bnh lun"
                    value={formatNumber(analytics?.totalComments || 0)}
                    icon={<MessageCircle className="w-6 h-6 text-white" />}
                    color="bg-purple-500"
                />
                <StatsCard
                    title=" xut bn"
                    value={formatNumber(analytics?.publishedBlogs || 0)}
                    icon={<Users className="w-6 h-6 text-white" />}
                    color="bg-orange-500"
                />
            </div>

            {/* Charts Section */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <Card>
                    <CardHeader>
                        <CardTitle>Lt xem theo thi gian</CardTitle>
                        <CardDescription>
                            Biu  lt xem trong {getPeriodDisplay(state.period)}
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        {/* In a real app, you would use a charting library like Chart.js or Recharts */}
                        <div className="h-64 bg-gray-50 rounded-lg flex items-center justify-center">
                            <div className="text-center">
                                <BarChart3 className="w-12 h-12 text-gray-400 mx-auto mb-2" />
                                <p className="text-gray-500">Biu  lt xem</p>
                                <p className="text-sm text-gray-400">Tch hp th vin biu </p>
                            </div>
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>Ngun truy cp</CardTitle>
                        <CardDescription>
                            T u ngi c n vi blog
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-4">
                            {analytics?.monthlyStats?.slice(0, 5).map((stat, index) => (
                                <div key={index} className="flex items-center justify-between">
                                    <div>
                                        <div className="font-medium">{stat.month}</div>
                                        <div className="text-sm text-gray-500">{stat.views} lt xem</div>
                                    </div>
                                    <div className="text-right">
                                        <div className="font-medium">{stat.posts} bi</div>
                                        <div className="text-sm text-gray-500">{stat.comments} bnh lun</div>
                                    </div>
                                </div>
                            )) || (
                                    <div className="text-center text-gray-500 py-8">
                                        Khng c d liu thng k thng
                                    </div>
                                )}
                        </div>
                    </CardContent>
                </Card>
            </div>

            {/* Popular Posts */}
            <Card>
                <CardHeader>
                    <CardTitle>Bi vit ph bin</CardTitle>
                    <CardDescription>
                        Top bi vit c lt xem cao nht trong {getPeriodDisplay(state.period)}
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    <Table>
                        <TableHeader>
                            <TableRow>
                                <TableHead>Tiu  bi vit</TableHead>
                                <TableHead>Lt xem</TableHead>
                                <TableHead>Bnh lun</TableHead>
                                <TableHead>Ngy xut bn</TableHead>
                                <TableHead>Trng thi</TableHead>
                            </TableRow>
                        </TableHeader>
                        <TableBody>
                            {analytics?.topBlogs?.map((post) => (
                                <TableRow key={post.id}>
                                    <TableCell>
                                        <div className="font-medium max-w-md truncate">
                                            {post.title}
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <div className="flex items-center gap-1">
                                            <Eye className="w-4 h-4 text-gray-400" />
                                            {formatNumber(post.viewCount)}
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <div className="flex items-center gap-1">
                                            <MessageCircle className="w-4 h-4 text-gray-400" />
                                            N/A
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <div className="flex items-center gap-1">
                                            <Calendar className="w-4 h-4 text-gray-400" />
                                            {new Date(post.publishedAt).toLocaleDateString('vi-VN')}
                                        </div>
                                    </TableCell>
                                    <TableCell>
                                        <Badge variant="default">
                                             xut bn
                                        </Badge>
                                    </TableCell>
                                </TableRow>
                            )) || (
                                    <TableRow>
                                        <TableCell colSpan={5} className="text-center text-gray-500 py-8">
                                            Khng c d liu bi vit ph bin
                                        </TableCell>
                                    </TableRow>
                                )}
                        </TableBody>
                    </Table>
                </CardContent>
            </Card>

            {/* Recent Activity */}
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
                <Card>
                    <CardHeader>
                        <CardTitle>Bnh lun gn y</CardTitle>
                        <CardDescription>
                            Hot ng bnh lun mi nht
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-4">
                            {analytics?.recentActivity?.map((activity, index) => (
                                <div key={index} className="border-b border-gray-100 pb-3 last:border-0">
                                    <div className="flex items-start gap-3">
                                        <div className="w-8 h-8 bg-gray-200 rounded-full flex items-center justify-center">
                                            <Users className="w-4 h-4 text-gray-500" />
                                        </div>
                                        <div className="flex-1">
                                            <div className="flex items-center gap-2 mb-1">
                                                <span className="font-medium text-sm">{activity.author || 'System'}</span>
                                                <span className="text-xs text-gray-500">
                                                    {new Date(activity.timestamp).toLocaleDateString('vi-VN')}
                                                </span>
                                            </div>
                                            <p className="text-sm text-gray-700">
                                                {activity.type === 'blog_created' && 'To bi vit:'}
                                                {activity.type === 'blog_published' && 'Xut bn bi vit:'}
                                                {activity.type === 'comment_added' && 'Bnh lun mi:'}
                                                <span className="font-medium ml-1">{activity.title}</span>
                                            </p>
                                        </div>
                                    </div>
                                </div>
                            )) || (
                                    <div className="text-center text-gray-500 py-8">
                                        Khng c hot ng gn y
                                    </div>
                                )}
                        </div>
                    </CardContent>
                </Card>

                <Card>
                    <CardHeader>
                        <CardTitle>Top t kha tm kim</CardTitle>
                        <CardDescription>
                            T kha c tm kim nhiu nht
                        </CardDescription>
                    </CardHeader>
                    <CardContent>
                        <div className="space-y-3">
                            <div className="text-center text-gray-500 py-8">
                                Tnh nng thng k t kha tm kim s c pht trin
                            </div>
                        </div>
                    </CardContent>
                </Card>
            </div>
        </div>
    );
}