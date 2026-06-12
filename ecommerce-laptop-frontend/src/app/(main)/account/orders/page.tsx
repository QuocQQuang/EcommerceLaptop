'use client';

import { RetryPaymentModal } from '@/components/customer/RetryPaymentModal';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import {
    Dialog,
    DialogContent,
    DialogDescription,
    DialogHeader,
    DialogTitle,
    DialogTrigger,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import {
    Select,
    SelectContent,
    SelectItem,
    SelectTrigger,
    SelectValue,
} from '@/components/ui/select';
import { useCurrencyContext } from '@/contexts/CurrencyContext';
import { PaymentProvider } from '@/contexts/PaymentContext';
import { formatCurrencyPrice } from '@/lib/currency';
import { orderService } from '@/services/orderService';
import {
    CheckCircle,
    Clock,
    Eye,
    Package,
    Search,
    Truck,
    XCircle
} from 'lucide-react';
import { useSession } from 'next-auth/react';
import Link from 'next/link';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

// Order interface matching backend response
interface Order {
    id: number;
    orderNumber: string;
    createdAt: string;
    status: string;
    totalAmount: number;
    items: OrderItem[];
    shippingAddress: string;
    trackingNumber?: string;
    paymentStatus?: string; // Add payment status
}

interface OrderItem {
    id: number;
    productId: number;
    productName: string;
    quantity: number;
    unitPrice: number;
    totalPrice: number;
}

export default function OrdersPage() {
    const { selectedCurrency } = useCurrencyContext();
    const { data: session } = useSession();
    const [orders, setOrders] = useState<Order[]>([]);
    const [searchQuery, setSearchQuery] = useState('');
    const [statusFilter, setStatusFilter] = useState('all');
    const [isLoading, setIsLoading] = useState(true);
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [isCancelling, setIsCancelling] = useState(false);
    const [cancelDialogOpen, setCancelDialogOpen] = useState(false);
    const [cancelReason, setCancelReason] = useState('');
    const [selectedOrderId, setSelectedOrderId] = useState<number | null>(null);
    const [selectedOrderForRetry, setSelectedOrderForRetry] = useState<number | null>(null);

    // Load orders from API
    const loadOrders = async () => {
        if (!session?.user?.id) return;

        setIsLoading(true);
        try {
            const response = await orderService.getUserOrders(parseInt(session.user.id), page, 10);
            setOrders(response.items || []);
            setTotalPages(Math.ceil((response.totalCount || 0) / 10));
        } catch (error) {
            console.error('Error loading orders:', error);
            toast.error('Không thể tải danh sách đơn hàng');
            setOrders([]);
        } finally {
            setIsLoading(false);
        }
    };

    const openCancelDialog = (orderId: number) => {
        setSelectedOrderId(orderId);
        setCancelReason('');
        setCancelDialogOpen(true);
    };

    const handleCancelOrder = async () => {
        if (!selectedOrderId) return;
        try {
            setIsCancelling(true);
            await orderService.cancelOrder(selectedOrderId, cancelReason || 'Khách hàng yêu cầu hủy đơn');
            // Optimistically update UI
            setOrders(prev => prev.map(o => o.id === selectedOrderId ? { ...o, status: 'cancelled' } : o));
            toast.success('Đã hủy đơn hàng thành công');
            setCancelDialogOpen(false);
        } catch (error: any) {
            const message = error?.response?.data?.message || 'Hy n khng thnh cng';
            toast.error(message);
        } finally {
            setIsCancelling(false);
        }
    };

    useEffect(() => {
        loadOrders();
    }, [session, page]);

    const filteredOrders = orders.filter((order: Order) => {
        const matchesSearch = order.orderNumber?.toLowerCase().includes(searchQuery.toLowerCase()) ||
            order.id.toString().includes(searchQuery);
        const matchesStatus = statusFilter === 'all' || order.status === statusFilter;
        return matchesSearch && matchesStatus;
    });

    const getStatusBadge = (status: string) => {
        const statusConfig = {
            pending: { label: 'Chờ xử lý', variant: 'outline' as const, icon: Clock },
            confirmed: { label: 'Đã xác nhận', variant: 'secondary' as const, icon: CheckCircle },
            processing: { label: 'đang xử lý', variant: 'default' as const, icon: Package },
            shipped: { label: 'đang giao', variant: 'default' as const, icon: Truck },
            delivered: { label: 'Đã nhận hàng', variant: 'default' as const, icon: CheckCircle },
            cancelled: { label: 'đã hủy', variant: 'destructive' as const, icon: XCircle },
        } as const;

        const config = statusConfig[status as keyof typeof statusConfig];
        const Icon = (config?.icon) || Clock;

        return (
            <Badge variant={config?.variant || 'outline'} className="flex items-center space-x-1">
                <Icon className="h-3 w-3" />
                <span>{config?.label || status}</span>
            </Badge>
        );
    };

    if (isLoading) {
        return (
            <Card>
                <CardHeader>
                    <div className="h-6 bg-gray-200 rounded w-32 animate-pulse"></div>
                    <div className="h-4 bg-gray-200 rounded w-48 animate-pulse"></div>
                </CardHeader>
                <CardContent>
                    <div className="space-y-4">
                        {[...Array(3)].map((_, i) => (
                            <div key={i} className="h-16 bg-gray-200 rounded animate-pulse"></div>
                        ))}
                    </div>
                </CardContent>
            </Card>
        );
    }

    return (
        <PaymentProvider>
            <Card>
                <CardHeader>
                    <CardTitle>Đơn hàng của tôi</CardTitle>
                    <CardDescription>
                        Theo dõi và quản lý các đơn hàng của bạn
                    </CardDescription>
                </CardHeader>
                <CardContent>
                    {/* Filters */}
                    <div className="flex flex-col sm:flex-row gap-4 mb-6">
                        <div className="relative flex-1 max-w-sm">
                            <Search className="absolute left-3 top-3 h-4 w-4 text-gray-400" />
                            <Input
                                placeholder="Tìm kiếm theo mã đơn hàng..."
                                value={searchQuery}
                                onChange={(e) => setSearchQuery(e.target.value)}
                                className="pl-10"
                            />
                        </div>
                        <Select value={statusFilter} onValueChange={setStatusFilter}>
                            <SelectTrigger className="w-full sm:w-48">
                                <SelectValue placeholder="Lọc theo trạng thái" />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="all">Tất cả</SelectItem>
                                <SelectItem value="pending">Chờ xử lý</SelectItem>
                                <SelectItem value="confirmed">Đã xác nhận</SelectItem>
                                <SelectItem value="processing">đang xử lý</SelectItem>
                                <SelectItem value="shipped">đang giao</SelectItem>
                                <SelectItem value="delivered">Đã nhận hàng</SelectItem>
                                <SelectItem value="cancelled">đã hủy</SelectItem>
                            </SelectContent>
                        </Select>
                    </div>

                    {/* Orders List */}
                    {filteredOrders.length === 0 ? (
                        <div className="text-center py-12">
                            <Package className="h-12 w-12 text-gray-400 mx-auto mb-4" />
                            <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">
                                Không có đơn hàng nào
                            </h3>
                            <p className="text-gray-500 dark:text-gray-400">
                                {searchQuery || statusFilter !== 'all'
                                    ? 'Không tìm thấy đơn hàng phù hợp với bộ lọc'
                                    : 'Bạn chưa có đơn hàng nào. Hãy bắt đầu mua sắm!'
                                }
                            </p>
                        </div>
                    ) : (
                        <div className="space-y-4">
                            {filteredOrders.map((order) => (
                                <div key={order.id} className="border rounded-lg p-4 hover:shadow-md transition-shadow">
                                    <div className="flex justify-between items-start mb-4">
                                        <div>
                                            <h3 className="font-semibold">Đơn hàng {order.id}</h3>
                                            <p className="text-sm text-gray-500">
                                                Ngày đặt: {new Date(order.createdAt).toLocaleDateString('vi-VN')}
                                            </p>
                                        </div>
                                        <div className="text-right">
                                            {getStatusBadge(order.status)}
                                            <p className="text-lg font-semibold mt-1">
                                                {formatCurrencyPrice(order.totalAmount, selectedCurrency)}
                                            </p>
                                        </div>
                                    </div>

                                    {/* Order Items */}
                                    <div className="space-y-2 mb-4">
                                        {order.items.map((item, index) => (
                                            <div key={index} className="flex items-center space-x-3">
                                                <div className="w-12 h-12 bg-gray-100 rounded-md flex items-center justify-center">
                                                    <Package className="h-6 w-6 text-gray-400" />
                                                </div>
                                                <div className="flex-1">
                                                    <p className="font-medium">{item.productName}</p>
                                                    <p className="text-sm text-gray-500">
                                                        Số lượng: {item.quantity} x {formatCurrencyPrice(item.unitPrice, selectedCurrency)}
                                                    </p>
                                                </div>
                                            </div>
                                        ))}
                                    </div>

                                    {/* Actions */}
                                    <div className="flex justify-between items-center pt-4 border-t">
                                        <div>
                                            {order.trackingNumber && (
                                                <p className="text-sm text-gray-500">
                                                    Mã vận đơn: <span className="font-mono">{order.trackingNumber}</span>
                                                </p>
                                            )}
                                        </div>
                                        <div className="space-x-2">
                                            <Link href={`/account/orders/${order.id}`}>
                                                <Button variant="outline" size="sm">
                                                    <Eye className="h-4 w-4 mr-2" />
                                                    Chi tiết
                                                </Button>
                                            </Link>
                                            <Dialog>
                                                <DialogTrigger asChild>
                                                    <Button variant="outline" size="sm">
                                                        <Eye className="h-4 w-4 mr-2" />
                                                        Xem nhanh
                                                    </Button>
                                                </DialogTrigger>
                                                <DialogContent className="max-w-2xl">
                                                    <DialogHeader>
                                                        <DialogTitle>Chi tiết đơn hàng {order.id}</DialogTitle>
                                                        <DialogDescription>
                                                            Thông tin chi tiết về đơn hàng của bạn
                                                        </DialogDescription>
                                                    </DialogHeader>
                                                    <div className="space-y-6">
                                                        {/* Order Status */}
                                                        <div>
                                                            <h4 className="font-medium mb-2">Trạng thái đơn hàng</h4>
                                                            {getStatusBadge(order.status)}
                                                        </div>

                                                        {/* Items */}
                                                        <div>
                                                            <h4 className="font-medium mb-2">Sản phẩm</h4>
                                                            <div className="space-y-2">
                                                                {order.items.map((item, index) => (
                                                                    <div key={index} className="flex justify-between items-center py-2 border-b">
                                                                        <div>
                                                                            <p className="font-medium">{item.productName}</p>
                                                                            <p className="text-sm text-gray-500">
                                                                                Số lượng: {item.quantity}
                                                                            </p>
                                                                        </div>
                                                                        <p className="font-medium">
                                                                            {formatCurrencyPrice(item.totalPrice, selectedCurrency)}
                                                                        </p>
                                                                    </div>
                                                                ))}
                                                            </div>
                                                        </div>

                                                        {/* Shipping Address */}
                                                        <div>
                                                            <h4 className="font-medium mb-2">Địa chỉ giao hàng</h4>
                                                            <div className="bg-gray-50 dark:bg-gray-800 p-3 rounded-md">
                                                                <p className="text-sm text-gray-600 dark:text-gray-400">
                                                                    {order.shippingAddress}
                                                                </p>
                                                            </div>
                                                        </div>

                                                        {/* Total */}
                                                        <div className="border-t pt-4">
                                                            <div className="flex justify-between items-center text-lg font-semibold">
                                                                <span>Tổng cộng:</span>
                                                                <span>{formatCurrencyPrice(order.totalAmount, selectedCurrency)}</span>
                                                            </div>
                                                        </div>
                                                    </div>
                                                </DialogContent>
                                            </Dialog>

                                            {order.status === 'delivered' && (
                                                <Button variant="default" size="sm">
                                                    Mua lại
                                                </Button>
                                            )}

                                            {(order.status === 'pending' || order.status === 'confirmed') &&
                                                order.paymentStatus !== 'paid' &&
                                                order.paymentStatus !== 'completed' && (
                                                    <Button variant="destructive" size="sm" onClick={() => openCancelDialog(order.id)}>
                                                        Hủy đơn
                                                    </Button>
                                                )}
                                            {(order.status === 'pending' || order.status === 'confirmed') &&
                                                (order.paymentStatus === 'paid' || order.paymentStatus === 'completed') && (
                                                    <div className="text-sm text-muted-foreground">
                                                        Đã thanh toán - Không thể hủy
                                                    </div>
                                                )}
                                            {order.status === 'pending' && (
                                                <Button
                                                    variant="default"
                                                    size="sm"
                                                    onClick={() => setSelectedOrderForRetry(order.id)}
                                                >
                                                    Thanh toán lại
                                                </Button>
                                            )}
                                            <RetryPaymentModal
                                                orderId={order.id}
                                                orderAmount={order.totalAmount}
                                                orderNumber={order.orderNumber}
                                                isOpen={selectedOrderForRetry === order.id}
                                                onClose={() => setSelectedOrderForRetry(null)}
                                                onSuccess={() => {
                                                    loadOrders();
                                                    toast.success('Đã khởi tạo thanh toán lại');
                                                }}
                                            />
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}

                    {/* Pagination */}
                    {totalPages > 1 && (
                        <div className="flex justify-center space-x-2 mt-6">
                            <Button
                                variant="outline"
                                onClick={() => setPage(p => Math.max(1, p - 1))}
                                disabled={page === 1}
                            >
                                Trước
                            </Button>

                            {[...Array(totalPages)].map((_, i) => {
                                const pageNumber = i + 1;
                                if (pageNumber === page || Math.abs(pageNumber - page) <= 2) {
                                    return (
                                        <Button
                                            key={pageNumber}
                                            variant={pageNumber === page ? "default" : "outline"}
                                            onClick={() => setPage(pageNumber)}
                                        >
                                            {pageNumber}
                                        </Button>
                                    );
                                }
                                return null;
                            })}

                            <Button
                                variant="outline"
                                onClick={() => setPage(p => Math.min(totalPages, p + 1))}
                                disabled={page === totalPages}
                            >
                                Sau
                            </Button>
                        </div>
                    )}
                    {/* Cancel Order Dialog */}
                    <Dialog open={cancelDialogOpen} onOpenChange={setCancelDialogOpen}>
                        <DialogContent>
                            <DialogHeader>
                                <DialogTitle>Hủy đơn hàng</DialogTitle>
                                <DialogDescription>Vui lòng nhập lý do hủy đơn (không bắt buộc).</DialogDescription>
                            </DialogHeader>
                            <div className="space-y-3">
                                <Input
                                    placeholder="Lý do hủy đơn"
                                    value={cancelReason}
                                    onChange={(e) => setCancelReason(e.target.value)}
                                />
                                <div className="flex justify-end gap-2">
                                    <Button variant="outline" onClick={() => setCancelDialogOpen(false)} disabled={isCancelling}>Đóng</Button>
                                    <Button variant="destructive" onClick={handleCancelOrder} disabled={isCancelling}>
                                        {isCancelling ? 'đang hủy...' : 'Xác nhận hủy'}
                                    </Button>
                                </div>
                            </div>
                        </DialogContent>
                    </Dialog>
                </CardContent>
            </Card>
        </PaymentProvider>
    );
}