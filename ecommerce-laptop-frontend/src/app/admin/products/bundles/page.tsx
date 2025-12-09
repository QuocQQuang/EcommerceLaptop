'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
    Card,
    CardContent,
    CardHeader,
    CardTitle
} from '@/components/ui/card';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import {
    Table,
    TableBody,
    TableCell,
    TableHead,
    TableHeader,
    TableRow
} from '@/components/ui/table';
import {
    PermissionGuard,
    useAdminAuth
} from '@/contexts/AdminAuthContext';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { PERMISSIONS } from '@/lib/admin-api';
import apiClient from '@/lib/api';
import { formatCurrencyPrice } from '@/lib/currency';
import { Product } from '@/types/api';
import { useQuery } from '@tanstack/react-query';
import {
    ArrowLeft,
    Edit,
    Package,
    Plus,
    Search,
    Trash2
} from 'lucide-react';
import Link from 'next/link';
import { useState } from 'react';
import { toast } from 'sonner';

// Bundle interface
interface Bundle {
    id: number;
    name: string;
    description: string;
    basePrice: number;
    discountPercentage: number;
    finalPrice: number;
    isActive: boolean;
    items: BundleItem[];
    createdAt: string;
    updatedAt: string;
}

interface BundleItem {
    id: number;
    productId: number;
    product: Product;
    quantity: number;
    isRequired: boolean;
    variantId?: number;
    variantName?: string;
}

// Mock data for demonstration
const mockBundles: Bundle[] = [
    {
        id: 1,
        name: 'Gaming Laptop Bundle',
        description: 'Complete gaming setup with laptop, mouse, and keyboard',
        basePrice: 25000000,
        discountPercentage: 10,
        finalPrice: 22500000,
        isActive: true,
        items: [
            {
                id: 1,
                productId: 1,
                product: {
                    id: 1,
                    name: 'Dell XPS 13 Plus2',
                    sku: 'DELL-XPS13-001',
                    price: 20000000,
                    isActive: true,
                    productType: 'Laptop',
                    variants: []
                } as Product,
                quantity: 1,
                isRequired: true
            },
            {
                id: 2,
                productId: 2,
                product: {
                    id: 2,
                    name: 'Gaming Mouse',
                    sku: 'MOUSE-001',
                    price: 5000000,
                    isActive: true,
                    productType: 'Accessory',
                    variants: []
                } as Product,
                quantity: 1,
                isRequired: false
            }
        ],
        createdAt: '2025-01-01T00:00:00Z',
        updatedAt: '2025-01-01T00:00:00Z'
    }
];

const useBundlesQuery = () => {
    return useQuery({
        queryKey: ['admin', 'bundles'],
        queryFn: async () => {
            // Simulate API call
            await new Promise(resolve => setTimeout(resolve, 1000));
            return mockBundles;
        }
    });
};

const useProductsQuery = () => {
    return useQuery({
        queryKey: ['admin', 'products'],
        queryFn: async () => {
            const response = await apiClient.get('/products/admin');
            return response.data.products || [];
        }
    });
};

// Helper functions - moved inside component

const getStatusBadge = (isActive: boolean) => {
    return isActive ? (
        <Badge variant="default">Active</Badge>
    ) : (
        <Badge variant="secondary">Inactive</Badge>
    );
};

export default function BundlesPage() {
    const { selectedCurrency } = useCurrencyContext();
    const { user } = useAdminAuth();
    const [searchTerm, setSearchTerm] = useState('');
    const [showCreateDialog, setShowCreateDialog] = useState(false);
    const [selectedBundle, setSelectedBundle] = useState<Bundle | null>(null);

    // Helper functions
    const formatCurrency = (amount: number) => {
        return formatCurrencyPrice(amount, selectedCurrency);
    };

    const { data: bundles, isLoading: bundlesLoading } = useBundlesQuery();
    const { data: products, isLoading: productsLoading } = useProductsQuery();

    const filteredBundles = bundles?.filter(bundle =>
        bundle.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
        bundle.description.toLowerCase().includes(searchTerm.toLowerCase())
    ) || [];

    const handleDeleteBundle = async (bundleId: number) => {
        if (window.confirm('Are you sure you want to delete this bundle?')) {
            // TODO: Implement delete bundle API call
            toast.success('Bundle deleted successfully!');
        }
    };

    if (bundlesLoading) {
        return (
            <div className="space-y-6">
                <div className="flex items-center gap-4">
                    <Skeleton className="h-8 w-32" />
                    <Skeleton className="h-8 w-64" />
                </div>
                <Card>
                    <CardContent className="pt-6">
                        <div className="space-y-3">
                            {[...Array(5)].map((_, i) => (
                                <Skeleton key={i} className="h-12 w-full" />
                            ))}
                        </div>
                    </CardContent>
                </Card>
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
                            Quay li Sn phm
                        </Button>
                    </Link>
                    <div>
                        <h1 className="text-3xl font-bold">Bundle Management</h1>
                        <p className="text-muted-foreground">
                            Create and manage product bundles
                        </p>
                    </div>
                </div>

                <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                    <Button onClick={() => setShowCreateDialog(true)}>
                        <Plus className="h-4 w-4 mr-2" />
                        Create Bundle
                    </Button>
                </PermissionGuard>
            </div>

            {/* Search */}
            <Card>
                <CardHeader>
                    <CardTitle>Search Bundles</CardTitle>
                </CardHeader>
                <CardContent>
                    <div className="flex gap-4">
                        <div className="flex-1">
                            <div className="relative">
                                <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
                                <Input
                                    placeholder="Search bundles by name or description..."
                                    value={searchTerm}
                                    onChange={(e) => setSearchTerm(e.target.value)}
                                    className="pl-9"
                                />
                            </div>
                        </div>
                    </div>
                </CardContent>
            </Card>

            {/* Bundles Table */}
            <Card>
                <CardHeader>
                    <div className="flex items-center justify-between">
                        <CardTitle>
                            Bundles ({filteredBundles.length})
                        </CardTitle>
                        <Button variant="outline" onClick={() => window.location.reload()}>
                            Refresh
                        </Button>
                    </div>
                </CardHeader>
                <CardContent>
                    {filteredBundles.length === 0 ? (
                        <div className="text-center py-8">
                            <Package className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                            <h3 className="text-lg font-semibold mb-2">No bundles found</h3>
                            <p className="text-muted-foreground mb-4">
                                {searchTerm ? 'No bundles match your search criteria.' : 'No bundles have been created yet.'}
                            </p>
                            {!searchTerm && (
                                <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                                    <Button onClick={() => setShowCreateDialog(true)}>
                                        <Plus className="h-4 w-4 mr-2" />
                                        Create First Bundle
                                    </Button>
                                </PermissionGuard>
                            )}
                        </div>
                    ) : (
                        <Table>
                            <TableHeader>
                                <TableRow>
                                    <TableHead>Bundle Name</TableHead>
                                    <TableHead>Items</TableHead>
                                    <TableHead>Base Price</TableHead>
                                    <TableHead>Discount</TableHead>
                                    <TableHead>Final Price</TableHead>
                                    <TableHead>Status</TableHead>
                                    <TableHead>Created</TableHead>
                                    <TableHead className="text-right">Actions</TableHead>
                                </TableRow>
                            </TableHeader>
                            <TableBody>
                                {filteredBundles.map((bundle) => (
                                    <TableRow key={bundle.id}>
                                        <TableCell>
                                            <div className="font-medium">{bundle.name}</div>
                                            <div className="text-sm text-muted-foreground">
                                                {bundle.description}
                                            </div>
                                        </TableCell>
                                        <TableCell>
                                            <Badge variant="outline">
                                                {bundle.items.length} items
                                            </Badge>
                                        </TableCell>
                                        <TableCell>{formatCurrency(bundle.basePrice)}</TableCell>
                                        <TableCell>
                                            <Badge variant="secondary">
                                                -{bundle.discountPercentage}%
                                            </Badge>
                                        </TableCell>
                                        <TableCell className="font-medium">
                                            {formatCurrency(bundle.finalPrice)}
                                        </TableCell>
                                        <TableCell>{getStatusBadge(bundle.isActive)}</TableCell>
                                        <TableCell className="text-sm text-muted-foreground">
                                            {new Date(bundle.createdAt).toLocaleDateString()}
                                        </TableCell>
                                        <TableCell className="text-right">
                                            <div className="flex items-center justify-end gap-2">
                                                <Button
                                                    variant="ghost"
                                                    size="sm"
                                                    onClick={() => setSelectedBundle(bundle)}
                                                >
                                                    <Edit className="h-4 w-4" />
                                                </Button>
                                                <PermissionGuard permission={PERMISSIONS.PRODUCTS_DELETE}>
                                                    <Button
                                                        variant="ghost"
                                                        size="sm"
                                                        onClick={() => handleDeleteBundle(bundle.id)}
                                                        className="text-red-600 hover:text-red-700"
                                                    >
                                                        <Trash2 className="h-4 w-4" />
                                                    </Button>
                                                </PermissionGuard>
                                            </div>
                                        </TableCell>
                                    </TableRow>
                                ))}
                            </TableBody>
                        </Table>
                    )}
                </CardContent>
            </Card>

            {/* Create/Edit Bundle Dialog */}
            <Dialog open={showCreateDialog || !!selectedBundle} onOpenChange={(open) => {
                if (!open) {
                    setShowCreateDialog(false);
                    setSelectedBundle(null);
                }
            }}>
                <DialogContent className="max-w-6xl max-h-[90vh] overflow-y-auto">
                    <DialogHeader>
                        <DialogTitle>
                            {selectedBundle ? 'Edit Bundle' : 'Create New Bundle'}
                        </DialogTitle>
                        <DialogDescription>
                            {selectedBundle ? 'Update bundle configuration' : 'Configure your product bundle'}
                        </DialogDescription>
                    </DialogHeader>

                    {productsLoading ? (
                        <div className="flex items-center justify-center py-8">
                            <div className="text-center">
                                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900 mx-auto mb-4"></div>
                                <p>Loading products...</p>
                            </div>
                        </div>
                    ) : (
                        <div className="py-4">
                            {/* Bundle Builder Component would go here */}
                            <div className="text-center py-8">
                                <Package className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                                <h3 className="text-lg font-semibold mb-2">Bundle Builder</h3>
                                <p className="text-muted-foreground mb-4">
                                    Bundle builder component will be integrated here
                                </p>
                                <div className="space-y-2">
                                    <p className="text-sm text-muted-foreground">
                                        Available products: {products?.length || 0}
                                    </p>
                                    <p className="text-sm text-muted-foreground">
                                        Selected bundle: {selectedBundle?.name || 'None'}
                                    </p>
                                </div>
                            </div>
                        </div>
                    )}
                </DialogContent>
            </Dialog>
        </div>
    );
}
