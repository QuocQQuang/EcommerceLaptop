import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { useToast } from '@/hooks/use-toast';
import React, { useEffect, useState } from 'react';
import { AdminApiService } from '../../services/AdminApiService';
import { CustomerManagementService } from '../../services/CustomerManagementService';
import {
  CustomerManagementDto,
  CustomerPermissions,
  CustomerSearchParameters,
  CustomerStatisticsDto,
  PagedResult
} from '../../types/customer.types';
import CustomerDetailModal from './CustomerDetailModal';
import CustomerEditModal from './CustomerEditModal';

const CustomerManagementDashboard: React.FC = () => {
  const { toast } = useToast();
  // Use centralized logger so DebugLogger can show API activity
  const [logger, setLogger] = useState<any>(null);

  useEffect(() => {
    // Dynamically import logger to avoid server-side imports
    import('@/lib/logger').then((loggerModule) => {
      setLogger(loggerModule.default);
    });
  }, []);

  // Initialize services
  const adminApiService = new AdminApiService();
  const customerService = new CustomerManagementService(adminApiService);

  const [customers, setCustomers] = useState<CustomerManagementDto[]>([]);
  const [statistics, setStatistics] = useState<CustomerStatisticsDto | null>(null);
  const [permissions, setPermissions] = useState<CustomerPermissions>({
    canRead: false,
    canWrite: false,
    canDelete: false,
    canManage: false,
    canViewPersonalData: false,
    canViewSecurityInfo: false,
    canExport: false
  });
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [pageSize] = useState(10);

  // Modal states
  const [detailModalOpen, setDetailModalOpen] = useState(false);
  const [editModalOpen, setEditModalOpen] = useState(false);
  const [selectedCustomerId, setSelectedCustomerId] = useState<number | null>(null);
  const [selectedCustomers, setSelectedCustomers] = useState<number[]>([]);

  // Initialize services - TODO: Enable when API is ready
  // const customerService = new CustomerManagementService(adminApiService);

  useEffect(() => {
    initializeDashboard();
  }, []);

  useEffect(() => {
    if (permissions.canRead) {
      loadCustomers();
    }
  }, [currentPage, searchTerm, permissions.canRead]);

  const initializeDashboard = async () => {
    try {
      setLoading(true);

      // Load permissions first
      try {
        const permissionResults = await Promise.all([
          customerService.hasPermission('customers:read'),
          customerService.hasPermission('customers:write'),
          customerService.hasPermission('customers:delete'),
          customerService.hasPermission('customers:manage'),
          customerService.hasPermission('customers:export')
        ]);

        setPermissions({
          canRead: permissionResults[0],
          canWrite: permissionResults[1],
          canDelete: permissionResults[2],
          canManage: permissionResults[3],
          canViewPersonalData: permissionResults[0], // Same as read for now
          canViewSecurityInfo: permissionResults[3], // Same as manage
          canExport: permissionResults[4]
        });

        // Load statistics if user can read
        if (permissionResults[0]) {
          try {
            const stats = await customerService.getCustomerStatistics();
            setStatistics(stats);
          } catch (statsError) {
            logger.warn('Could not load statistics, using defaults', { error: statsError });
            // Fallback to mock data if API call fails
            setStatistics({
              totalCustomers: 0,
              activeCustomers: 0,
              newCustomersThisMonth: 0,
              newCustomersToday: 0,
              emailVerifiedCustomers: 0,
              unverifiedCustomers: 0,
              customersLoggedInToday: 0,
              customersLoggedInThisWeek: 0,
              inactiveCustomers30Days: 0,
              averageCustomerValue: 0,
              vipTierStats: [],
              registrationTrends: []
            });
          }
        }
      } catch (permissionError) {
        logger.warn('Could not check permissions, using restricted access', { error: permissionError });
        // If permission check fails, set all to false
        setPermissions({
          canRead: false,
          canWrite: false,
          canDelete: false,
          canManage: false,
          canViewPersonalData: false,
          canViewSecurityInfo: false,
          canExport: false
        });
      }

    } catch (error) {
      if (logger) logger.error('Error initializing dashboard', { error });
      toast({
        title: "Lỗi",
        description: "Không thể khởi tạo bảng điều khiển quản lý khách hàng",
        variant: "destructive",
      });
    } finally {
      setLoading(false);
    }
  };

  const loadCustomers = async () => {
    try {
      // Implement actual API call
      const searchParams: CustomerSearchParameters = {
        searchTerm: searchTerm || undefined,
        page: currentPage,
        pageSize: pageSize,
        sortBy: 'createdAt',
        sortOrder: 'desc'
      };

      const result: PagedResult<CustomerManagementDto> = await customerService.getCustomers(searchParams);
      setCustomers(result.items);
      setTotalCount(result.totalCount);

    } catch (error) {
      logger.warn('Failed to load customers from API, using fallback', { error });

      // Fallback to mock data if API call fails
      const mockCustomers: CustomerManagementDto[] = [
        {
          id: 1,
          firstName: "Nguyễn",
          lastName: "Văn A",
          email: "nguyenvana@example.com",
          maskedEmail: "ng***@***.com",
          phoneNumber: "0123456789",
          maskedPhoneNumber: "012***789",
          isActive: true,
          emailConfirmed: true,
          createdAt: new Date('2024-01-15'),
          lastLoginAt: new Date('2024-03-10'),
          totalOrders: 15,
          totalSpent: 15000000,
          lastOrderDate: new Date('2024-03-08'),
          vipTierId: 2,
          vipTierName: "Gold",
          failedLoginAttempts: 0,
          lockedUntil: undefined,
          canViewDetails: true,
          canEdit: true,
          canDelete: true,
          canViewPersonalData: true
        },
        {
          id: 2,
          firstName: "Trần",
          lastName: "Thị B",
          email: "tranthib@example.com",
          maskedEmail: "tr***@***.com",
          phoneNumber: "0987654321",
          maskedPhoneNumber: "098***321",
          isActive: true,
          emailConfirmed: false,
          createdAt: new Date('2024-02-20'),
          lastLoginAt: new Date('2024-03-09'),
          totalOrders: 8,
          totalSpent: 8500000,
          lastOrderDate: new Date('2024-03-05'),
          vipTierId: 1,
          vipTierName: "Silver",
          failedLoginAttempts: 1,
          lockedUntil: undefined,
          canViewDetails: true,
          canEdit: true,
          canDelete: true,
          canViewPersonalData: true
        }
      ];

      setCustomers(mockCustomers);
      setTotalCount(mockCustomers.length);

      toast({
        title: "Warning",
        description: "Using demo data. API connection failed.",
        variant: "default",
      });
    }
  };

  const handleSearch = () => {
    setCurrentPage(1);
    loadCustomers();
  };

  const handleDeactivateCustomer = async (customerId: number) => {
    try {
      await customerService.deactivateCustomer(customerId, "Deactivated by admin");

      toast({
        title: "Thành công",
        description: "Đã vô hiệu hóa tài khoản khách hàng",
        variant: "success",
      });

      // Reload the list to reflect changes
      await loadCustomers();
    } catch (error) {
      if (logger) logger.error('Error deactivating customer', { error });
      toast({
        title: "Lỗi",
        description: "Không thể vô hiệu hóa tài khoản khách hàng",
        variant: "destructive",
      });
    }
  };

  const handleReactivateCustomer = async (customerId: number) => {
    try {
      await customerService.reactivateCustomer(customerId, "Reactivated by admin");

      toast({
        title: "Thành công",
        description: "Đã kích hoạt lại tài khoản khách hàng",
        variant: "success",
      });

      // Reload the list to reflect changes
      await loadCustomers();
    } catch (error) {
      if (logger) logger.error('Error reactivating customer', { error });
      toast({
        title: "Lỗi",
        description: "Không thể kích hoạt lại tài khoản khách hàng",
        variant: "destructive",
      });
    }
  };

  const handleViewCustomerDetail = (customerId: number) => {
    setSelectedCustomerId(customerId);
    setDetailModalOpen(true);
  };

  const handleEditCustomer = (customerId: number) => {
    setSelectedCustomerId(customerId);
    setEditModalOpen(true);
  };

  const handleCustomerUpdated = (updatedCustomer: CustomerManagementDto) => {
    // Update the customer in the local list
    setCustomers(prev =>
      prev.map(customer =>
        customer.id === updatedCustomer.id ? updatedCustomer : customer
      )
    );

    toast({
      title: "Thành công",
      description: "Đã cập nhật thông tin khách hàng",
      variant: "success",
    });
  };

  const handleExportCustomers = async () => {
    try {
      const exportData = {
        format: 'Excel', // or 'CSV', 'PDF'
        includePersonalData: permissions.canViewPersonalData,
        includeOrderHistory: true,
        searchTerm: searchTerm || undefined,
        // Add other filters if needed
      };

      const blob = await customerService.exportCustomers(exportData);

      // Create download link
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `customers_export_${new Date().toISOString().split('T')[0]}.xlsx`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);

      toast({
        title: "Thành công",
        description: "Đã xuất danh sách khách hàng",
        variant: "success",
      });
    } catch (error) {
      if (logger) logger.error('Error exporting customers', { error });
      toast({
        title: "Lỗi",
        description: "Không thể xuất danh sách khách hàng",
        variant: "destructive",
      });
    }
  };

  const handleBulkOperations = async () => {
    if (selectedCustomers.length === 0) {
      toast({
        title: "Thông báo",
        description: "Vui lòng chọn ít nhất một khách hàng",
      });
      return;
    }

    // This is a placeholder - you can implement specific bulk operations
    toast({
      title: "Thông báo",
      description: `Đã chọn ${selectedCustomers.length} khách hàng để thực hiện thao tác hàng loạt`,
    });
  };

  const formatCurrency = (amount: number) => {
    return new Intl.NumberFormat('vi-VN', {
      style: 'currency',
      currency: 'VND'
    }).format(amount);
  };

  const formatDate = (date: Date | string) => {
    return new Date(date).toLocaleDateString('vi-VN');
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-lg">Đang tải bảng điều khiển quản lý khách hàng...</div>
      </div>
    );
  }

  if (!permissions.canRead) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-center">
          <h2 className="text-xl font-semibold mb-2">Từ chối truy cập</h2>
          <p className="text-gray-600">Bạn không có quyền truy cập chức năng quản lý khách hàng.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <h1 className="text-3xl font-bold">Quản lý khách hàng</h1>
        <div className="flex gap-2">
          {permissions.canExport && (
            <Button variant="outline" onClick={handleExportCustomers}>
              Xuất danh sách admin
            </Button>
          )}
          {permissions.canManage && (
            <Button onClick={handleBulkOperations}>
              Thao tác hàng loạt ({selectedCustomers.length})
            </Button>
          )}
        </div>
      </div>

      {/* Statistics Cards */}
      {statistics && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Tổng số khách hàng</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{statistics.totalCustomers}</div>
              <p className="text-xs text-muted-foreground">
                {statistics.activeCustomers} đang hoạt động
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Khách hàng mới trong tháng</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{statistics.newCustomersThisMonth}</div>
              <p className="text-xs text-muted-foreground">
                {statistics.newCustomersToday} hôm nay
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Giá trị trung bình</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{formatCurrency(statistics.averageCustomerValue)}</div>
              <p className="text-xs text-muted-foreground">mỗi khách hàng</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Email đã xác minh</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{statistics.emailVerifiedCustomers}</div>
              <p className="text-xs text-muted-foreground">
                {statistics.unverifiedCustomers} chưa xác minh
              </p>
            </CardContent>
          </Card>
        </div>
      )}

      {/* Search and Filters */}
      <Card>
        <CardContent className="pt-6">
          <div className="flex gap-4">
            <Input
              placeholder="Tìm khách hàng theo tên hoặc email..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="flex-1"
            />
            <Button onClick={handleSearch}>Tìm kiếm</Button>
          </div>
        </CardContent>
      </Card>

      {/* Customer List */}
      <Card>
        <CardHeader>
          <CardTitle>Khách hàng ({totalCount})</CardTitle>
        </CardHeader>
        <CardContent>
          <div className="space-y-4">
            {customers.map((customer) => (
              <div key={customer.id} className="flex items-center justify-between p-4 border rounded-lg">
                <div className="flex-1">
                  <div className="flex items-center gap-3">
                    <div>
                      <h3 className="font-semibold">
                        {customer.firstName} {customer.lastName}
                      </h3>
                      <p className="text-sm text-gray-600">
                        {permissions.canViewPersonalData ? customer.email : customer.maskedEmail}
                      </p>
                    </div>
                    <div className="flex gap-2">
                      <Badge variant={customer.isActive ? "default" : "secondary"}>
                        {customer.isActive ? "Đang hoạt động" : "Ngừng hoạt động"}
                      </Badge>
                      <Badge variant={customer.emailConfirmed ? "default" : "destructive"}>
                        {customer.emailConfirmed ? "Đã xác minh" : "Chưa xác minh"}
                      </Badge>
                      {customer.vipTierName && (
                        <Badge variant="outline">{customer.vipTierName}</Badge>
                      )}
                    </div>
                  </div>
                  <div className="mt-2 text-sm text-gray-500">
                    <span>Đơn hàng: {customer.totalOrders}</span>
                    <span className="ml-4">Chi tiêu: {formatCurrency(customer.totalSpent)}</span>
                    <span className="ml-4">Tham gia: {formatDate(customer.createdAt)}</span>
                    {customer.lastLoginAt && (
                      <span className="ml-4">Đăng nhập gần nhất: {formatDate(customer.lastLoginAt)}</span>
                    )}
                  </div>
                </div>

                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleViewCustomerDetail(customer.id)}
                  >
                    Xem chi tiết
                  </Button>
                  {permissions.canWrite && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleEditCustomer(customer.id)}
                    >
                      Chỉnh sửa
                    </Button>
                  )}
                  {permissions.canDelete && customer.isActive && (
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => handleDeactivateCustomer(customer.id)}
                    >
                      Vô hiệu hóa
                    </Button>
                  )}
                  {permissions.canManage && !customer.isActive && (
                    <Button
                      variant="default"
                      size="sm"
                      onClick={() => handleReactivateCustomer(customer.id)}
                    >
                      Kích hoạt lại
                    </Button>
                  )}
                </div>
              </div>
            ))}
          </div>

          {/* Pagination */}
          <div className="flex justify-between items-center mt-6">
            <div className="text-sm text-gray-500">
              Hiển thị {((currentPage - 1) * pageSize) + 1} đến {Math.min(currentPage * pageSize, totalCount)} trong tổng số {totalCount} khách hàng
            </div>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={currentPage === 1}
                onClick={() => setCurrentPage(currentPage - 1)}
              >
                Trước
              </Button>
              <Button
                variant="outline"
                size="sm"
                disabled={currentPage * pageSize >= totalCount}
                onClick={() => setCurrentPage(currentPage + 1)}
              >
                Sau
              </Button>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Modals */}
      <CustomerDetailModal
        isOpen={detailModalOpen}
        onClose={() => {
          setDetailModalOpen(false);
          setSelectedCustomerId(null);
        }}
        customerId={selectedCustomerId}
      />

      <CustomerEditModal
        isOpen={editModalOpen}
        onClose={() => {
          setEditModalOpen(false);
          setSelectedCustomerId(null);
        }}
        customerId={selectedCustomerId}
        onCustomerUpdated={handleCustomerUpdated}
      />
    </div>
  );
};

export default CustomerManagementDashboard;
