'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
    Card,
    CardContent,
    CardDescription,
    CardHeader,
    CardTitle
} from '@/components/ui/card';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow
} from '@/components/ui/table';
import {
    useAdminAuth
} from '@/contexts/AdminAuthContext';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { formatCurrencyPrice } from '@/lib/currency';
import { useQuery } from '@tanstack/react-query';
import {
    ArrowLeft,
    BarChart3,
    Package,
    TrendingDown,
    TrendingUp
} from 'lucide-react';
import Link from 'next/link';
import { useState } from 'react';

// Analytics interfaces
interface VariantAnalytics {
    productId: number;
    productName: string;
    totalVariants: number;
    activeVariants: number;
    totalSales: number;
    bestSellingVariant: {
        id: number;
        name: string;
        sales: number;
    };
    worstSellingVariant: {
        id: number;
        name: string;
        sales: number;
    };
    averagePrice: number;
    priceRange: {
        min: number;
        max: number;
    };
}

interface BundleAnalytics {
    bundleId: number;
    bundleName: string;
    totalSales: number;
    revenue: number;
    averageOrderValue: number;
    conversionRate: number;
    mostPopularItems: Array<{
        productId: number;
        productName: string;
        timesIncluded: number;
    }>;
}

// Mock data for demonstration
const mockVariantAnalytics: VariantAnalytics[] = [
    {
        productId: 1,
        productName: 'Dell XPS 13 Plus2',
        totalVariants: 3,
        activeVariants: 2,
        totalSales: 150,
        bestSellingVariant: {
            id: 110,
            name: '16GB RAM, 512GB SSD',
            sales: 80
        },
        worstSellingVariant: {
            id: 111,
            name: '32GB RAM, 1TB SSD',
            sales: 20
        },
        averagePrice: 1500000,
        priceRange: {
            min: 1299999,
            max: 1799999
        }
    }
];

const mockBundleAnalytics: BundleAnalytics[] = [
    {
        bundleId: 1,
        bundleName: 'Gaming Laptop Bundle',
        totalSales: 25,
        revenue: 562500000,
        averageOrderValue: 22500000,
        conversionRate: 12.5,
        mostPopularItems: [
            {
                productId: 1,
                productName: 'Dell XPS 13 Plus2',
                timesIncluded: 25
            },
            {
                productId: 2,
                productName: 'Gaming Mouse',
                timesIncluded: 20
            }
        ]
    }
];

const useVariantAnalyticsQuery = () => {
    return useQuery({
        queryKey: ['admin', 'variant-analytics'],
        queryFn: async () => {
            // Simulate API call
            await new Promise(resolve => setTimeout(resolve, 1000));
            return mockVariantAnalytics;
        }
    });
};

const useBundleAnalyticsQuery = () => {
    return useQuery({
        queryKey: ['admin', 'bundle-analytics'],
        queryFn: async () => {
            // Simulate API call
            await new Promise(resolve => setTimeout(resolve, 1000));
            return mockBundleAnalytics;
        }
    });
};

// Helper functions - moved inside component

const formatNumber = (num: number) => {
    return new Intl.NumberFormat('vi-VN').format(num);
};

const getTrendIcon = (current: number, previous: number) => {
    if (current > previous) {
        return <TrendingUp className="h-4 w-4 text-green-600" />;
    } else if (current < previous) {
        return <TrendingDown className="h-4 w-4 text-red-600" />;
    }
    return null;
};

export default function ProductAnalyticsPage() {
    const { selectedCurrency } = useCurrencyContext();
    const { user } = useAdminAuth();
    const [activeTab, setActiveTab] = useState<'variants' | 'bundles'>('variants');

    // Helper functions
    const formatCurrency = (amount: number) => {
        return formatCurrencyPrice(amount, selectedCurrency);
    };

    const { data: variantAnalytics, isLoading: variantsLoading } = useVariantAnalyticsQuery();
    const { data: bundleAnalytics, isLoading: bundlesLoading } = useBundleAnalyticsQuery();

    if (variantsLoading || bundlesLoading) {
        return (
            <div className="space-y-6">
                <div className="flex items-center gap-4">
                    <Link href="/admin/products">
                        <Button variant="outline" size="sm">
                            <ArrowLeft className="h-4 w-4 mr-2" />
                            Quay lại Sản phẩm
                        </Button>
                    </Link>
                    <div>
                        <h1 className="text-3xl font-bold">Product Analytics</h1>
                        <p className="text-muted-foreground">Loading analytics data...</p>
                    </div>
                </div>
                <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
                    {[...Array(3)].map((_, i) => (
                        <Card key={i}>
                            <CardContent className="pt-6">
                                <div className="animate-pulse">
                                    <div className="h-4 bg-gray-200 rounded w-3/4 mb-2"></div>
                                    <div className="h-8 bg-gray-200 rounded w-1/2"></div>
                                </div>
                            </CardContent>
                        </Card>
                    ))}
                </div>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div className="flex items-center gap-4">
                    <Link href="/admin/products">
                        <Button variant="outline" size="sm">
                            <ArrowLeft className="h-4 w-4 mr-2" />
                            Quay lại Sản phẩm
                        </Button>
                    </Link>
                    <div>
                        <h1 className="text-3xl font-bold">Product Analytics</h1>
                        <p className="text-muted-foreground">
                            Variant and bundle performance insights
                        </p>
                    </div>
                </div>
            </div>

            {/* Tab Navigation */}
            <div className="flex space-x-1 bg-muted p-1 rounded-lg w-fit">
                <Button
                    variant={activeTab === 'variants' ? 'default' : 'ghost'}
                    size="sm"
                    onClick={() => setActiveTab('variants')}
                >
                    <Package className="h-4 w-4 mr-2" />
                    Variant Analytics
                </Button>
                <Button
                    variant={activeTab === 'bundles' ? 'default' : 'ghost'}
                    size="sm"
                    onClick={() => setActiveTab('bundles')}
                >
                    <BarChart3 className="h-4 w-4 mr-2" />
                    Bundle Analytics
                </Button>
            </div>

            {/* Variant Analytics */}
            {activeTab === 'variants' && (
                <div className="space-y-6">
                    {/* Summary Cards */}
                    <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
                        <Card>
                            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                                <CardTitle className="text-sm font-medium">Total Products</CardTitle>
                                <Package className="h-4 w-4 text-muted-foreground" />
                            </CardHeader>
                            <CardContent>
                                <div className="text-2xl font-bold">{variantAnalytics?.length || 0}</div>
                                <p className="text-xs text-muted-foreground">
                                    Products with variants
                                </p>
                            </CardContent>
                        </Card>

                        <Card>
                            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                                <CardTitle className="text-sm font-medium">Total Variants</CardTitle>
                                <BarChart3 className="h-4 w-4 text-muted-foreground" />
                            </CardHeader>
                            <CardContent>
                                <div className="text-2xl font-bold">
                                    {variantAnalytics?.reduce((sum, item) => sum + item.totalVariants, 0) || 0}
                                </div>
                                <p className="text-xs text-muted-foreground">
                                    Across all products
                                </p>
                            </CardContent>
                        </Card>

                        <Card>
                            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                                <CardTitle className="text-sm font-medium">Active Variants</CardTitle>
                                <TrendingUp className="h-4 w-4 text-muted-foreground" />
                            </CardHeader>
                            <CardContent>
                                <div className="text-2xl font-bold">
                                    {variantAnalytics?.reduce((sum, item) => sum + item.activeVariants, 0) || 0}
                                </div>
                                <p className="text-xs text-muted-foreground">
                                    Currently available
                                </p>
                            </CardContent>
                        </Card>

                        <Card>
                            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                                <CardTitle className="text-sm font-medium">Total Sales</CardTitle>
                                <TrendingUp className="h-4 w-4 text-muted-foreground" />
                            </CardHeader>
                            <CardContent>
                                <div className="text-2xl font-bold">
                                    {formatNumber(variantAnalytics?.reduce((sum, item) => sum + item.totalSales, 0) || 0)}
                                </div>
                                <p className="text-xs text-muted-foreground">
                                    Units sold
                                </p>
                            </CardContent>
                        </Card>
                    </div>

                    {/* Variant Details Table */}
                    <Card>
                        <CardHeader>
                            <CardTitle>Variant Performance</CardTitle>
                            <CardDescription>
                                Detailed analytics for each product with variants
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <Table>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead>Product</TableHead>
                                        <TableHead>Variants</TableHead>
                                        <TableHead>Best Seller</TableHead>
                                        <TableHead>Worst Seller</TableHead>
                                        <TableHead>Price Range</TableHead>
                                        <TableHead>Total Sales</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {variantAnalytics?.map((analytics) => (
                                        <TableRow key={analytics.productId}>
                                            <TableCell>
                                                <div className="font-medium">{analytics.productName}</div>
                                                <div className="text-sm text-muted-foreground">
                                                    ID: {analytics.productId}
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="flex items-center gap-2">
                                                    <Badge variant="outline">
                                                        {analytics.activeVariants}/{analytics.totalVariants}
                                                    </Badge>
                                                    <span className="text-sm text-muted-foreground">
                                                        active
                                                    </span>
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="font-medium">{analytics.bestSellingVariant.name}</div>
                                                <div className="text-sm text-muted-foreground">
                                                    {analytics.bestSellingVariant.sales} sales
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="font-medium">{analytics.worstSellingVariant.name}</div>
                                                <div className="text-sm text-muted-foreground">
                                                    {analytics.worstSellingVariant.sales} sales
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="text-sm">
                                                    {formatCurrency(analytics.priceRange.min)} - {formatCurrency(analytics.priceRange.max)}
                                                </div>
                                                <div className="text-xs text-muted-foreground">
                                                    Avg: {formatCurrency(analytics.averagePrice)}
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="font-medium">{formatNumber(analytics.totalSales)}</div>
                                                <div className="text-sm text-muted-foreground">
                                                    units sold
                                                </div>
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </CardContent>
                    </Card>
                </div>
            )}

            {/* Bundle Analytics */}
            {activeTab === 'bundles' && (
                <div className="space-y-6">
                    {/* Summary Cards */}
                    <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
                        <Card>
                            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                                <CardTitle className="text-sm font-medium">Total Bundles</CardTitle>
                                <Package className="h-4 w-4 text-muted-foreground" />
                            </CardHeader>
                            <CardContent>
                                <div className="text-2xl font-bold">{bundleAnalytics?.length || 0}</div>
                                <p className="text-xs text-muted-foreground">
                                    Active bundles
                                </p>
                            </CardContent>
                        </Card>

                        <Card>
                            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                                <CardTitle className="text-sm font-medium">Total Revenue</CardTitle>
                                <TrendingUp className="h-4 w-4 text-muted-foreground" />
                            </CardHeader>
                            <CardContent>
                                <div className="text-2xl font-bold">
                                    {formatCurrency(bundleAnalytics?.reduce((sum, item) => sum + item.revenue, 0) || 0)}
                                </div>
                                <p className="text-xs text-muted-foreground">
                                    From bundles
                                </p>
                            </CardContent>
                        </Card>

                        <Card>
                            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                                <CardTitle className="text-sm font-medium">Avg Order Value</CardTitle>
                                <BarChart3 className="h-4 w-4 text-muted-foreground" />
                            </CardHeader>
                            <CardContent>
                                <div className="text-2xl font-bold">
                                    {formatCurrency(
                                        (bundleAnalytics?.reduce((sum, item) => sum + item.averageOrderValue, 0) ?? 0) / (bundleAnalytics?.length || 1) || 0
                                    )}
                                </div>
                                <p className="text-xs text-muted-foreground">
                                    Per bundle
                                </p>
                            </CardContent>
                        </Card>

                        <Card>
                            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                                <CardTitle className="text-sm font-medium">Avg Conversion</CardTitle>
                                <TrendingUp className="h-4 w-4 text-muted-foreground" />
                            </CardHeader>
                            <CardContent>
                                <div className="text-2xl font-bold">
                                    {((bundleAnalytics?.reduce((sum, item) => sum + item.conversionRate, 0) ?? 0) / (bundleAnalytics?.length || 1) || 0).toFixed(1)}%
                                </div>
                                <p className="text-xs text-muted-foreground">
                                    Conversion rate
                                </p>
                            </CardContent>
                        </Card>
                    </div>

                    {/* Bundle Details Table */}
                    <Card>
                        <CardHeader>
                            <CardTitle>Bundle Performance</CardTitle>
                            <CardDescription>
                                Detailed analytics for each bundle
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <Table>
                                <TableHeader>
                                    <TableRow>
                                        <TableHead>Bundle</TableHead>
                                        <TableHead>Sales</TableHead>
                                        <TableHead>Revenue</TableHead>
                                        <TableHead>Avg Order Value</TableHead>
                                        <TableHead>Conversion Rate</TableHead>
                                        <TableHead>Popular Items</TableHead>
                                    </TableRow>
                                </TableHeader>
                                <TableBody>
                                    {bundleAnalytics?.map((analytics) => (
                                        <TableRow key={analytics.bundleId}>
                                            <TableCell>
                                                <div className="font-medium">{analytics.bundleName}</div>
                                                <div className="text-sm text-muted-foreground">
                                                    ID: {analytics.bundleId}
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="font-medium">{formatNumber(analytics.totalSales)}</div>
                                                <div className="text-sm text-muted-foreground">
                                                    units sold
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="font-medium">{formatCurrency(analytics.revenue)}</div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="font-medium">{formatCurrency(analytics.averageOrderValue)}</div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="flex items-center gap-2">
                                                    <span className="font-medium">{analytics.conversionRate}%</span>
                                                    {getTrendIcon(analytics.conversionRate, 10)}
                                                </div>
                                            </TableCell>
                                            <TableCell>
                                                <div className="space-y-1">
                                                    {analytics.mostPopularItems.slice(0, 2).map((item, index) => (
                                                        <div key={index} className="text-sm">
                                                            {item.productName} ({item.timesIncluded}x)
                                                        </div>
                                                    ))}
                                                </div>
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </CardContent>
                    </Card>
                </div>
            )}
        </div>
    );
}

