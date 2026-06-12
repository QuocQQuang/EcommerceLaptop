'use client';

import CustomerManagementDashboard from '@/components/admin/CustomerManagementDashboard';

export default function CustomersPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Quản lý khách hàng</h1>
        <p className="text-muted-foreground">
          Quản lý thông tin khách hàng với bảo mật và quyền riêng tư
        </p>
      </div>

      <CustomerManagementDashboard />
    </div>
  );
}
