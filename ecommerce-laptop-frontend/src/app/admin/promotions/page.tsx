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
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from '@/components/ui/dialog';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  PermissionGuard,
  useAdminAuth
} from '@/contexts/AdminAuthContext';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { PERMISSIONS } from '@/lib/admin-api';
import { formatCurrencyPrice } from '@/lib/currency';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  AlertTriangle,
  CheckCircle,
  Clock,
  Copy,
  Edit,
  Eye,
  Filter,
  MoreHorizontal,
  Percent,
  Plus,
  Search,
  Tag,
  Trash2,
  TrendingUp,
  XCircle
} from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';

interface Promotion {
  id: number;
  name: string;
  code: string;
  description: string;
  type: 'percentage' | 'fixed_amount' | 'free_shipping';
  value: number;
  minimumOrderAmount?: number;
  maxDiscount?: number;
  usageLimit?: number;
  usageCount: number;
  startDate: string;
  endDate: string;
  isActive: boolean;
  status: 'draft' | 'active' | 'expired' | 'disabled';
  createdAt: string;
  updatedAt: string;
}

interface PromotionUsage {
  id: number;
  promotionId: number;
  customerName: string;
  customerEmail: string;
  orderCode: string;
  discountAmount: number;
  usedAt: string;
}

// Mock API calls - Replace with real API
const usePromotionsQuery = () => {
  return useQuery({
    queryKey: ['admin', 'promotions'],
    queryFn: async (): Promise<{ promotions: Promotion[]; totalCount: number }> => {
      await new Promise(resolve => setTimeout(resolve, 1000));

      // Mock data - replace with real API call
      const promotions: Promotion[] = [
        {
          id: 1,
          name: 'Khuyn mi Tt 2025',
          code: 'TET2025',
          description: 'Gim gi c bit cho dp Tt Nguyn n',
          type: 'percentage',
          value: 15,
          minimumOrderAmount: 5000000,
          maxDiscount: 1000000,
          usageLimit: 1000,
          usageCount: 234,
          startDate: '2025-01-20T00:00:00Z',
          endDate: '2025-02-15T23:59:59Z',
          isActive: true,
          status: 'active',
          createdAt: '2025-01-15T10:00:00Z',
          updatedAt: '2025-01-22T14:30:00Z'
        },
        {
          id: 2,
          name: 'Min ph vn chuyn',
          code: 'FREESHIP50',
          description: 'Min ph vn chuyn cho n hng t 5 triu',
          type: 'free_shipping',
          value: 0,
          minimumOrderAmount: 5000000,
          usageLimit: undefined,
          usageCount: 1567,
          startDate: '2025-01-01T00:00:00Z',
          endDate: '2025-12-31T23:59:59Z',
          isActive: true,
          status: 'active',
          createdAt: '2025-01-01T00:00:00Z',
          updatedAt: '2025-01-20T09:15:00Z'
        },
        {
          id: 3,
          name: 'Gim c nh 500K',
          code: 'SAVE500K',
          description: 'Gim ngay 500.000 cho n hng t 10 triu',
          type: 'fixed_amount',
          value: 500000,
          minimumOrderAmount: 10000000,
          usageLimit: 500,
          usageCount: 123,
          startDate: '2025-01-10T00:00:00Z',
          endDate: '2025-02-10T23:59:59Z',
          isActive: true,
          status: 'active',
          createdAt: '2025-01-08T16:20:00Z',
          updatedAt: '2025-01-18T11:45:00Z'
        },
        {
          id: 4,
          name: 'Black Friday 2025',
          code: 'BF2025',
          description: 'Gim gi Black Friday -  kt thc',
          type: 'percentage',
          value: 30,
          minimumOrderAmount: 2000000,
          maxDiscount: 2000000,
          usageLimit: 2000,
          usageCount: 1876,
          startDate: '2025-11-25T00:00:00Z',
          endDate: '2025-11-30T23:59:59Z',
          isActive: false,
          status: 'expired',
          createdAt: '2025-11-20T08:00:00Z',
          updatedAt: '2025-12-01T00:00:00Z'
        }
      ];

      return { promotions, totalCount: promotions.length };
    },
    staleTime: 30 * 1000,
  });
};

const useDeletePromotionMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (promotionId: number) => {
      await new Promise(resolve => setTimeout(resolve, 1000));
      console.log('Deleting promotion:', promotionId);
      return { success: true };
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'promotions'] });
    }
  });
};

const useTogglePromotionMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async ({ id, isActive }: { id: number; isActive: boolean }) => {
      await new Promise(resolve => setTimeout(resolve, 1000));
      console.log('Toggling promotion:', id, isActive);
      return { success: true };
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'promotions'] });
    }
  });
};

const useDuplicatePromotionMutation = () => {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: async (promotionId: number) => {
      await new Promise(resolve => setTimeout(resolve, 1000));
      console.log('Duplicating promotion:', promotionId);
      return { success: true, newId: Date.now() };
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['admin', 'promotions'] });
    }
  });
};

// Vietnamese formatting utilities - moved inside component

const formatDate = (dateString: string) => {
  return new Intl.DateTimeFormat('vi-VN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).format(new Date(dateString));
};

const formatDateTime = (dateString: string) => {
  return new Intl.DateTimeFormat('vi-VN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(dateString));
};

const getPromotionTypeBadge = (type: string) => {
  const typeConfig = {
    percentage: { label: 'Phn trm', variant: 'default' as const, icon: Percent },
    fixed_amount: { label: 'Gim c nh', variant: 'secondary' as const, icon: Tag },
    free_shipping: { label: 'Min ph ship', variant: 'outline' as const, icon: TrendingUp }
  };

  const config = typeConfig[type as keyof typeof typeConfig];
  const Icon = config?.icon || Tag;

  return (
    <Badge variant={config?.variant || 'outline'} className="flex items-center gap-1">
      <Icon className="h-3 w-3" />
      {config?.label || type}
    </Badge>
  );
};

const getStatusBadge = (status: string) => {
  const statusConfig = {
    draft: { label: 'Nhp', variant: 'outline' as const, icon: Clock },
    active: { label: 'ang hot ng', variant: 'default' as const, icon: CheckCircle },
    expired: { label: ' ht hn', variant: 'secondary' as const, icon: XCircle },
    disabled: { label: 'Tm dng', variant: 'destructive' as const, icon: AlertTriangle }
  };

  const config = statusConfig[status as keyof typeof statusConfig];
  const Icon = config?.icon || Clock;

  return (
    <Badge variant={config?.variant || 'outline'} className="flex items-center gap-1">
      <Icon className="h-3 w-3" />
      {config?.label || status}
    </Badge>
  );
};

const getPromotionValue = (promotion: Promotion) => {
  switch (promotion.type) {
    case 'percentage':
      return `${promotion.value}%`;
    case 'fixed_amount':
      return formatCurrency(promotion.value);
    case 'free_shipping':
      return 'Min ph';
    default:
      return promotion.value.toString();
  }
};

const isPromotionExpired = (endDate: string) => {
  return new Date(endDate) < new Date();
};

const getUsageProgress = (usageCount: number, usageLimit?: number) => {
  if (!usageLimit) return null;
  return Math.min((usageCount / usageLimit) * 100, 100);
};

export default function PromotionsPage() {
  const { selectedCurrency } = useCurrencyContext();
  const router = useRouter();
  const { user } = useAdminAuth();

  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('all');
  const [typeFilter, setTypeFilter] = useState<string>('all');
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);

  // Helper functions
  const formatCurrency = (amount: number) => {
    return formatCurrencyPrice(amount, selectedCurrency);
  };
  const [selectedPromotion, setSelectedPromotion] = useState<Promotion | null>(null);

  const {
    data,
    isLoading,
    error,
    refetch
  } = usePromotionsQuery();

  const deletePromotionMutation = useDeletePromotionMutation();
  const togglePromotionMutation = useTogglePromotionMutation();
  const duplicatePromotionMutation = useDuplicatePromotionMutation();

  // Filter promotions based on search and filters
  const filteredPromotions = data?.promotions?.filter(promotion => {
    const matchesSearch = promotion.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      promotion.code.toLowerCase().includes(searchTerm.toLowerCase()) ||
      promotion.description.toLowerCase().includes(searchTerm.toLowerCase());

    const matchesStatus = statusFilter === 'all' || promotion.status === statusFilter;
    const matchesType = typeFilter === 'all' || promotion.type === typeFilter;

    return matchesSearch && matchesStatus && matchesType;
  }) || [];

  const handleDeletePromotion = async () => {
    if (!selectedPromotion) return;

    try {
      await deletePromotionMutation.mutateAsync(selectedPromotion.id);
      setShowDeleteDialog(false);
      setSelectedPromotion(null);
    } catch (error) {
      console.error('Error deleting promotion:', error);
    }
  };

  const handleTogglePromotion = async (promotion: Promotion) => {
    try {
      await togglePromotionMutation.mutateAsync({
        id: promotion.id,
        isActive: !promotion.isActive
      });
    } catch (error) {
      console.error('Error toggling promotion:', error);
    }
  };

  const handleDuplicatePromotion = async (promotion: Promotion) => {
    try {
      await duplicatePromotionMutation.mutateAsync(promotion.id);
    } catch (error) {
      console.error('Error duplicating promotion:', error);
    }
  };

  if (error) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <AlertTriangle className="h-12 w-12 text-red-500 mx-auto mb-4" />
          <h3 className="text-lg font-semibold">C li xy ra</h3>
          <p className="text-muted-foreground mb-4">
            Khng th ti danh sch khuyn mi. Vui lng th li.
          </p>
          <Button onClick={() => refetch()}>Th li</Button>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Qun l khuyn mi</h1>
          <p className="text-muted-foreground">
            Qun l cc chng trnh khuyn mi v m gim gi
          </p>
        </div>

        <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
          <Link href="/promotions/add">
            <Button>
              <Plus className="h-4 w-4 mr-2" />
              Thm khuyn mi
            </Button>
          </Link>
        </PermissionGuard>
      </div>

      {/* Filters and Search */}
      <Card>
        <CardHeader>
          <CardTitle>B lc</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="flex flex-col sm:flex-row gap-4">
            <div className="flex-1">
              <div className="relative">
                <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 text-muted-foreground h-4 w-4" />
                <Input
                  placeholder="Tm kim tn, m hoc m t khuyn mi..."
                  value={searchTerm}
                  onChange={(e) => setSearchTerm(e.target.value)}
                  className="pl-10"
                />
              </div>
            </div>

            <div className="flex gap-2">
              <Select value={statusFilter} onValueChange={setStatusFilter}>
                <SelectTrigger className="w-40">
                  <SelectValue placeholder="Trng thi" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tt c trng thi</SelectItem>
                  <SelectItem value="active">ang hot ng</SelectItem>
                  <SelectItem value="draft">Nhp</SelectItem>
                  <SelectItem value="expired"> ht hn</SelectItem>
                  <SelectItem value="disabled">Tm dng</SelectItem>
                </SelectContent>
              </Select>

              <Select value={typeFilter} onValueChange={setTypeFilter}>
                <SelectTrigger className="w-40">
                  <SelectValue placeholder="Loi khuyn mi" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Tt c loi</SelectItem>
                  <SelectItem value="percentage">Phn trm</SelectItem>
                  <SelectItem value="fixed_amount">Gim c nh</SelectItem>
                  <SelectItem value="free_shipping">Min ph ship</SelectItem>
                </SelectContent>
              </Select>

              <Button variant="outline">
                <Filter className="h-4 w-4 mr-2" />
                Lc nng cao
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Promotions Table */}
      <Card>
        <CardHeader>
          <CardTitle>
            Danh sch khuyn mi ({filteredPromotions.length})
          </CardTitle>
          <CardDescription>
            Qun l tt c cc chng trnh khuyn mi v m gim gi ca ca hng
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-3">
              {[...Array(5)].map((_, i) => (
                <div key={i} className="h-16 bg-muted animate-pulse rounded" />
              ))}
            </div>
          ) : filteredPromotions.length === 0 ? (
            <div className="text-center py-12">
              <Tag className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
              <h3 className="text-lg font-semibold mb-2">Cha c khuyn mi no</h3>
              <p className="text-muted-foreground mb-4">
                {searchTerm || statusFilter !== 'all' || typeFilter !== 'all'
                  ? 'Khng tm thy khuyn mi no ph hp vi b lc.'
                  : 'Bt u to chng trnh khuyn mi u tin ca bn.'}
              </p>
              {(!searchTerm && statusFilter === 'all' && typeFilter === 'all') && (
                <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                  <Link href="/promotions/add">
                    <Button>
                      <Plus className="h-4 w-4 mr-2" />
                      Thm khuyn mi
                    </Button>
                  </Link>
                </PermissionGuard>
              )}
            </div>
          ) : (
            <div className="overflow-x-auto">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Tn khuyn mi</TableHead>
                    <TableHead>M</TableHead>
                    <TableHead>Loi</TableHead>
                    <TableHead>Gi tr</TableHead>
                    <TableHead>Thi gian</TableHead>
                    <TableHead>S dng</TableHead>
                    <TableHead>Trng thi</TableHead>
                    <TableHead className="text-right">Thao tc</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {filteredPromotions.map((promotion) => {
                    const usageProgress = getUsageProgress(promotion.usageCount, promotion.usageLimit);
                    const isExpired = isPromotionExpired(promotion.endDate);

                    return (
                      <TableRow key={promotion.id}>
                        <TableCell>
                          <div>
                            <div className="font-medium">{promotion.name}</div>
                            <div className="text-sm text-muted-foreground">
                              {promotion.description}
                            </div>
                          </div>
                        </TableCell>
                        <TableCell>
                          <code className="bg-muted px-2 py-1 rounded text-sm">
                            {promotion.code}
                          </code>
                        </TableCell>
                        <TableCell>
                          {getPromotionTypeBadge(promotion.type)}
                        </TableCell>
                        <TableCell className="font-medium">
                          {getPromotionValue(promotion)}
                          {promotion.minimumOrderAmount && (
                            <div className="text-xs text-muted-foreground">
                              n ti thiu {formatCurrency(promotion.minimumOrderAmount)}
                            </div>
                          )}
                        </TableCell>
                        <TableCell>
                          <div className="text-sm">
                            <div>{formatDate(promotion.startDate)}</div>
                            <div className="text-muted-foreground">
                              n {formatDate(promotion.endDate)}
                            </div>
                            {isExpired && (
                              <Badge variant="destructive" className="mt-1">
                                 ht hn
                              </Badge>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="space-y-1">
                            <div className="text-sm">
                              {promotion.usageCount}
                              {promotion.usageLimit && ` / ${promotion.usageLimit}`}
                            </div>
                            {usageProgress !== null && (
                              <div className="w-20 bg-muted rounded-full h-2">
                                <div
                                  className={`h-2 rounded-full ${usageProgress >= 90 ? 'bg-red-500' :
                                    usageProgress >= 70 ? 'bg-yellow-500' :
                                      'bg-green-500'
                                    }`}
                                  style={{ width: `${usageProgress}%` }}
                                />
                              </div>
                            )}
                          </div>
                        </TableCell>
                        <TableCell>
                          {getStatusBadge(promotion.status)}
                        </TableCell>
                        <TableCell className="text-right">
                          <DropdownMenu>
                            <DropdownMenuTrigger asChild>
                              <Button variant="ghost" className="h-8 w-8 p-0">
                                <MoreHorizontal className="h-4 w-4" />
                              </Button>
                            </DropdownMenuTrigger>
                            <DropdownMenuContent align="end">
                              <DropdownMenuLabel>Thao tc</DropdownMenuLabel>

                              <PermissionGuard permission={PERMISSIONS.PRODUCTS_READ}>
                                <DropdownMenuItem
                                  onClick={() => router.push(`/promotions/${promotion.id}`)}
                                >
                                  <Eye className="h-4 w-4 mr-2" />
                                  Xem chi tit
                                </DropdownMenuItem>
                              </PermissionGuard>

                              <PermissionGuard permission={PERMISSIONS.PRODUCTS_WRITE}>
                                <DropdownMenuItem
                                  onClick={() => router.push(`/promotions/${promotion.id}/edit`)}
                                >
                                  <Edit className="h-4 w-4 mr-2" />
                                  Chnh sa
                                </DropdownMenuItem>

                                <DropdownMenuItem
                                  onClick={() => handleDuplicatePromotion(promotion)}
                                  disabled={duplicatePromotionMutation.isPending}
                                >
                                  <Copy className="h-4 w-4 mr-2" />
                                  Nhn bn
                                </DropdownMenuItem>

                                <DropdownMenuItem
                                  onClick={() => handleTogglePromotion(promotion)}
                                  disabled={togglePromotionMutation.isPending}
                                >
                                  {promotion.isActive ? (
                                    <>
                                      <XCircle className="h-4 w-4 mr-2" />
                                      Tm dng
                                    </>
                                  ) : (
                                    <>
                                      <CheckCircle className="h-4 w-4 mr-2" />
                                      Kch hot
                                    </>
                                  )}
                                </DropdownMenuItem>

                                <DropdownMenuSeparator />

                                <DropdownMenuItem
                                  className="text-red-600"
                                  onClick={() => {
                                    setSelectedPromotion(promotion);
                                    setShowDeleteDialog(true);
                                  }}
                                >
                                  <Trash2 className="h-4 w-4 mr-2" />
                                  Xa
                                </DropdownMenuItem>
                              </PermissionGuard>
                            </DropdownMenuContent>
                          </DropdownMenu>
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Delete Confirmation Dialog */}
      <Dialog open={showDeleteDialog} onOpenChange={setShowDeleteDialog}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Xc nhn xa khuyn mi</DialogTitle>
            <DialogDescription>
              Bn c chc chn mun xa khuyn mi &quot;{selectedPromotion?.name}&quot;?
              Hnh ng ny khng th hon tc.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button
              variant="outline"
              onClick={() => setShowDeleteDialog(false)}
            >
              Hy b
            </Button>
            <Button
              variant="destructive"
              onClick={handleDeletePromotion}
              disabled={deletePromotionMutation.isPending}
            >
              {deletePromotionMutation.isPending ? 'ang xa...' : 'Xa khuyn mi'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}