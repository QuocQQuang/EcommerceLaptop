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
        title: "Li",
        description: "Khng th khi to bng iu khin qun l khch hng",
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
          firstName: "Nguyn",
          lastName: "Vn A",
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
          firstName: "Trn",
          lastName: "Th B",
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
        title: "Thnh cng",
        description: " v hiu ha ti khon khch hng",
        variant: "success",
      });

      // Reload the list to reflect changes
      await loadCustomers();
    } catch (error) {
      if (logger) logger.error('Error deactivating customer', { error });
      toast({
        title: "Li",
        description: "Khng th v hiu ha ti khon khch hng",
        variant: "destructive",
      });
    }
  };

  const handleReactivateCustomer = async (customerId: number) => {
    try {
      await customerService.reactivateCustomer(customerId, "Reactivated by admin");

      toast({
        title: "Thnh cng",
        description: " kch hot li ti khon khch hng",
        variant: "success",
      });

      // Reload the list to reflect changes
      await loadCustomers();
    } catch (error) {
      if (logger) logger.error('Error reactivating customer', { error });
      toast({
        title: "Li",
        description: "Khng th kch hot li ti khon khch hng",
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
      title: "Thnh cng",
      description: " cp nht thng tin khch hng",
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
        title: "Thnh cng",
        description: " xut danh sch khch hng",
        variant: "success",
      });
    } catch (error) {
      if (logger) logger.error('Error exporting customers', { error });
      toast({
        title: "Li",
        description: "Khng th xut danh sch khch hng",
        variant: "destructive",
      });
    }
  };

  const handleBulkOperations = async () => {
    if (selectedCustomers.length === 0) {
      toast({
        title: "Thng bo",
        description: "Vui lng chn t nht mt khch hng",
      });
      return;
    }

    // This is a placeholder - you can implement specific bulk operations
    toast({
      title: "Thng bo",
      description: ` chn ${selectedCustomers.length} khch hng  thc hin thao tc hng lot`,
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
        <div className="text-lg">ang ti bng iu khin qun l khch hng...</div>
      </div>
    );
  }

  if (!permissions.canRead) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-center">
          <h2 className="text-xl font-semibold mb-2">T chi truy cp</h2>
          <p className="text-gray-600">Bn khng c quyn truy cp chc nng qun l khch hng.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex justify-between items-center">
        <h1 className="text-3xl font-bold">Qun l khch hng</h1>
        <div className="flex gap-2">
          {permissions.canExport && (
            <Button variant="outline" onClick={handleExportCustomers}>
              Xut danh sch admin
            </Button>
          )}
          {permissions.canManage && (
            <Button onClick={handleBulkOperations}>
              Thao tc hng lot ({selectedCustomers.length})
            </Button>
          )}
        </div>
      </div>

      {/* Statistics Cards */}
      {statistics && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Tng s khch hng</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{statistics.totalCustomers}</div>
              <p className="text-xs text-muted-foreground">
                {statistics.activeCustomers} ang hot ng
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Khch hng mi trong thng</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{statistics.newCustomersThisMonth}</div>
              <p className="text-xs text-muted-foreground">
                {statistics.newCustomersToday} hm nay
              </p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Gi tr trung bnh</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{formatCurrency(statistics.averageCustomerValue)}</div>
              <p className="text-xs text-muted-foreground">mi khch hng</p>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
              <CardTitle className="text-sm font-medium">Email  xc minh</CardTitle>
            </CardHeader>
            <CardContent>
              <div className="text-2xl font-bold">{statistics.emailVerifiedCustomers}</div>
              <p className="text-xs text-muted-foreground">
                {statistics.unverifiedCustomers} cha xc minh
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
              placeholder="Tm khch hng theo tn hoc email..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="flex-1"
            />
            <Button onClick={handleSearch}>Tm kim</Button>
          </div>
        </CardContent>
      </Card>

      {/* Customer List */}
      <Card>
        <CardHeader>
          <CardTitle>Khch hng ({totalCount})</CardTitle>
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
                        {customer.isActive ? "ang hot ng" : "Ngng hot ng"}
                      </Badge>
                      <Badge variant={customer.emailConfirmed ? "default" : "destructive"}>
                        {customer.emailConfirmed ? " xc minh" : "Cha xc minh"}
                      </Badge>
                      {customer.vipTierName && (
                        <Badge variant="outline">{customer.vipTierName}</Badge>
                      )}
                    </div>
                  </div>
                  <div className="mt-2 text-sm text-gray-500">
                    <span>n hng: {customer.totalOrders}</span>
                    <span className="ml-4">Chi tiu: {formatCurrency(customer.totalSpent)}</span>
                    <span className="ml-4">Tham gia: {formatDate(customer.createdAt)}</span>
                    {customer.lastLoginAt && (
                      <span className="ml-4">ng nhp gn nht: {formatDate(customer.lastLoginAt)}</span>
                    )}
                  </div>
                </div>

                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    size="sm"
                    onClick={() => handleViewCustomerDetail(customer.id)}
                  >
                    Xem chi tit
                  </Button>
                  {permissions.canWrite && (
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => handleEditCustomer(customer.id)}
                    >
                      Chnh sa
                    </Button>
                  )}
                  {permissions.canDelete && customer.isActive && (
                    <Button
                      variant="destructive"
                      size="sm"
                      onClick={() => handleDeactivateCustomer(customer.id)}
                    >
                      V hiu ha
                    </Button>
                  )}
                  {permissions.canManage && !customer.isActive && (
                    <Button
                      variant="default"
                      size="sm"
                      onClick={() => handleReactivateCustomer(customer.id)}
                    >
                      Kch hot li
                    </Button>
                  )}
                </div>
              </div>
            ))}
          </div>

          {/* Pagination */}
          <div className="flex justify-between items-center mt-6">
            <div className="text-sm text-gray-500">
              Hin th {((currentPage - 1) * pageSize) + 1} n {Math.min(currentPage * pageSize, totalCount)} trong tng s {totalCount} khch hng
            </div>
            <div className="flex gap-2">
              <Button
                variant="outline"
                size="sm"
                disabled={currentPage === 1}
                onClick={() => setCurrentPage(currentPage - 1)}
              >
                Trc
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