import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import React, { useEffect, useState } from 'react';
import { AdminApiService } from '../../services/AdminApiService';
import { CustomerManagementService } from '../../services/CustomerManagementService';
import { CustomerManagementDto } from '../../types/customer.types';

interface CustomerDetailModalProps {
  isOpen: boolean;
  onClose: () => void;
  customerId: number | null;
}

interface CustomerOrdersData {
  customerId: number;
  totalOrders: number;
  totalSpent: number;
  averageOrderValue: number;
  firstOrderDate: string | null;
  lastOrderDate: string | null;
  pendingOrders: number;
  completedOrders: number;
  cancelledOrders: number;
  refundedOrders: number;
  recentOrders: Array<{
    orderId: number;
    orderNumber: string;
    orderDate: string;
    total: number;
    status: string;
    itemCount: number;
  }>;
  monthlySpending: Array<{
    year: number;
    month: number;
    amount: number;
    orderCount: number;
  }>;
}

const CustomerDetailModal: React.FC<CustomerDetailModalProps> = ({
  isOpen,
  onClose,
  customerId
}) => {
  const [customer, setCustomer] = useState<CustomerManagementDto | null>(null);
  const [orders, setOrders] = useState<CustomerOrdersData | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const adminApiService = new AdminApiService();
  const customerService = new CustomerManagementService(adminApiService);

  useEffect(() => {
    if (isOpen && customerId) {
      loadCustomerData();
    }
  }, [isOpen, customerId]);

  const loadCustomerData = async () => {
    if (!customerId) return;

    setLoading(true);
    setError(null);

    try {
      // Load customer detail
      const customerData = await customerService.getCustomerDetail(customerId);
      setCustomer(customerData);

      // Load customer orders
      try {
        const ordersData = await customerService.getCustomerOrders(customerId);
        setOrders(ordersData);
      } catch (ordersError) {
        console.warn('Could not load orders data:', ordersError);
        // Continue without orders data
      }
    } catch (err: any) {
      console.error('Error loading customer data:', err);
      setError(err?.response?.data?.message || 'Không thể tải thông tin khách hàng');
    } finally {
      setLoading(false);
    }
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND'
    }).format(amount);
  };

  const formatDate = (date: string | Date) => {
    return new Date(date).toLocaleDateString('vi-VN', {
      year: 'numeric',
      month: 'long',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  if (!isOpen) return null;

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-4xl max-h-[90vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>Chi tiết khách hàng</DialogTitle>
          <DialogDescription>
            Thông tin chi tiết của khách hàng
          </DialogDescription>
        </DialogHeader>

        {loading && (
          <div className="flex items-center justify-center h-48">
            <div>Đang tải thông tin khách hàng...</div>
          </div>
        )}

        {error && (
          <div className="text-center p-8">
            <div className="text-red-600 mb-4">{error}</div>
            <Button onClick={loadCustomerData} variant="outline">
              Thử lại
            </Button>
          </div>
        )}

        {customer && !loading && (
          <Tabs defaultValue="info" className="w-full">
            <TabsList className="grid w-full grid-cols-2">
              <TabsTrigger value="info">Thông tin cá nhân</TabsTrigger>
              <TabsTrigger value="orders">Đơn hàng</TabsTrigger>
            </TabsList>

            <TabsContent value="info" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle>Thông tin khách hàng</CardTitle>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="text-sm font-medium text-gray-500">Họ và tên</label>
                      <p className="text-lg font-semibold">
                        {customer.firstName} {customer.lastName}
                      </p>
                    </div>
                    <div>
                      <label className="text-sm font-medium text-gray-500">Email</label>
                      <p>{customer.email || customer.maskedEmail}</p>
                    </div>
                    <div>
                      <label className="text-sm font-medium text-gray-500">Số điện thoại</label>
                      <p>{customer.phoneNumber || customer.maskedPhoneNumber}</p>
                    </div>
                    <div>
                      <label className="text-sm font-medium text-gray-500">Trạng thái</label>
                      <div className="flex gap-2 mt-1">
                        <Badge variant={customer.isActive ? "default" : "secondary"}>
                          {customer.isActive ? "Đang hoạt động" : "Ngừng hoạt động"}
                        </Badge>
                        <Badge variant={customer.emailConfirmed ? "default" : "destructive"}>
                          {customer.emailConfirmed ? "Đã xác minh" : "Chưa xác minh"}
                        </Badge>
                      </div>
                    </div>
                  </div>

                  {customer.vipTierName && (
                    <div>
                      <label className="text-sm font-medium text-gray-500">Hạng khách hàng</label>
                      <Badge variant="outline" className="mt-1">
                        {customer.vipTierName}
                      </Badge>
                    </div>
                  )}

                  <div className="grid grid-cols-2 gap-4">
                    <div>
                      <label className="text-sm font-medium text-gray-500">Ngày tham gia</label>
                      <p>{formatDate(customer.createdAt)}</p>
                    </div>
                    {customer.lastLoginAt && (
                      <div>
                        <label className="text-sm font-medium text-gray-500">Đăng nhập gần nhất</label>
                        <p>{formatDate(customer.lastLoginAt)}</p>
                      </div>
                    )}
                  </div>
                </CardContent>
              </Card>
            </TabsContent>

            <TabsContent value="orders" className="space-y-4">
              <Card>
                <CardHeader>
                  <CardTitle>Thống kê đơn hàng</CardTitle>
                </CardHeader>
                <CardContent>
                  {orders ? (
                    <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                      <div className="text-center">
                        <div className="text-2xl font-bold text-blue-600">{orders.totalOrders}</div>
                        <div className="text-sm text-gray-500">Tổng đơn hàng</div>
                      </div>
                      <div className="text-center">
                        <div className="text-2xl font-bold text-green-600">
                          {formatCurrency(orders.totalSpent)}
                        </div>
                        <div className="text-sm text-gray-500">Tổng chi tiêu</div>
                      </div>
                      <div className="text-center">
                        <div className="text-2xl font-bold text-purple-600">
                          {formatCurrency(orders.averageOrderValue)}
                        </div>
                        <div className="text-sm text-gray-500">Đơn hàng trung bình</div>
                      </div>
                      <div className="text-center">
                        <div className="text-sm text-gray-500">Đơn hàng gần nhất</div>
                        <div className="font-medium">
                          {orders.lastOrderDate ? formatDate(orders.lastOrderDate) : 'Chưa có'}
                        </div>
                      </div>
                    </div>
                  ) : (
                    <div className="text-center py-8">
                      <p className="text-gray-500">Không thể tải thông tin đơn hàng</p>
                    </div>
                  )}
                </CardContent>
              </Card>

              {orders && orders.recentOrders.length > 0 && (
                <Card>
                  <CardHeader>
                    <CardTitle>Đơn hàng gần đây</CardTitle>
                  </CardHeader>
                  <CardContent>
                    <div className="space-y-2">
                      {orders.recentOrders.slice(0, 5).map((order) => (
                        <div key={order.orderId} className="flex justify-between items-center p-3 border rounded-lg">
                          <div>
                            <div className="font-medium">#{order.orderNumber}</div>
                            <div className="text-sm text-gray-500">{formatDate(order.orderDate)}</div>
                          </div>
                          <div className="text-right">
                            <div className="font-medium">{formatCurrency(order.total)}</div>
                            <Badge variant={order.status === 'Completed' ? 'default' : 'secondary'}>
                              {order.status}
                            </Badge>
                          </div>
                        </div>
                      ))}
                    </div>
                  </CardContent>
                </Card>
              )}
            </TabsContent>

          </Tabs>
        )}

        <div className="flex justify-end gap-2 pt-4">
          <Button variant="outline" onClick={onClose}>
            Đóng
          </Button>
        </div>
      </DialogContent>
    </Dialog>
  );
};

export default CustomerDetailModal;
