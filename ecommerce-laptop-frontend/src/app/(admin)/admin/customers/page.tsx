'use client';

import CustomerManagementDashboard from '@/components/admin/CustomerManagementDashboard';

export default function CustomersPage() {
  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-bold tracking-tight">Qun l khch hng</h1>
        <p className="text-muted-foreground">
          Qun l thng tin khch hng vi bo mt v quyn ring t
        </p>
      </div>

      <CustomerManagementDashboard />
    </div>
  );
}
