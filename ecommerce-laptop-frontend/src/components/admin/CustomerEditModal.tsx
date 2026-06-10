import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import React, { useEffect, useState } from 'react';
import { AdminApiService } from '../../services/AdminApiService';
import { CustomerManagementService } from '../../services/CustomerManagementService';
import { CustomerManagementDto } from '../../types/customer.types';

interface CustomerEditModalProps {
  isOpen: boolean;
  onClose: () => void;
  customerId: number | null;
  onCustomerUpdated: (updatedCustomer: CustomerManagementDto) => void;
}

interface UpdateCustomerForm {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  isActive: boolean;
  emailConfirmed: boolean;
}

const CustomerEditModal: React.FC<CustomerEditModalProps> = ({
  isOpen,
  onClose,
  customerId,
  onCustomerUpdated
}) => {
  const [customer, setCustomer] = useState<CustomerManagementDto | null>(null);
  const [formData, setFormData] = useState<UpdateCustomerForm>({
    firstName: '',
    lastName: '',
    email: '',
    phoneNumber: '',
    isActive: true,
    emailConfirmed: false
  });
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [validationErrors, setValidationErrors] = useState<Record<string, string>>({});

  const adminApiService = new AdminApiService();
  const customerService = new CustomerManagementService(adminApiService);

  useEffect(() => {
    if (isOpen && customerId) {
      loadCustomerData();
    } else if (!isOpen) {
      // Reset form when modal closes
      resetForm();
    }
  }, [isOpen, customerId]);

  const resetForm = () => {
    setCustomer(null);
    setFormData({
      firstName: '',
      lastName: '',
      email: '',
      phoneNumber: '',
      isActive: true,
      emailConfirmed: false
    });
    setError(null);
    setValidationErrors({});
  };

  const loadCustomerData = async () => {
    if (!customerId) return;

    setLoading(true);
    setError(null);

    try {
      const customerData = await customerService.getCustomerDetail(customerId);
      setCustomer(customerData);

      // Populate form with customer data
      setFormData({
        firstName: customerData.firstName || '',
        lastName: customerData.lastName || '',
        email: customerData.email || '',
        phoneNumber: customerData.phoneNumber || '',
        isActive: customerData.isActive,
        emailConfirmed: customerData.emailConfirmed
      });
    } catch (err: any) {
      console.error('Error loading customer data:', err);
      setError(err?.response?.data?.message || 'Không thể tải thông tin khách hàng');
    } finally {
      setLoading(false);
    }
  };

  const validateForm = (): boolean => {
    const errors: Record<string, string> = {};

    if (!formData.firstName.trim()) {
      errors.firstName = 'Tên không được để trống';
    }

    if (!formData.lastName.trim()) {
      errors.lastName = 'Họ không được để trống';
    }

    if (!formData.email.trim()) {
      errors.email = 'Email không được để trống';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      errors.email = 'Email không hợp lệ';
    }

    if (formData.phoneNumber && !/^[0-9\+\-\s\(\)]{10,15}$/.test(formData.phoneNumber.replace(/\s/g, ''))) {
      errors.phoneNumber = 'Số điện thoại không hợp lệ';
    }

    setValidationErrors(errors);
    return Object.keys(errors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    if (!validateForm()) {
      return;
    }

    setSaving(true);
    setError(null);

    try {
      const updateData = {
        firstName: formData.firstName.trim(),
        lastName: formData.lastName.trim(),
        email: formData.email.trim(),
        phoneNumber: formData.phoneNumber.trim() || null,
        isActive: formData.isActive,
        emailConfirmed: formData.emailConfirmed
      };

      const updatedCustomer = await customerService.updateCustomer(customerId!, updateData);

      // Notify parent component
      onCustomerUpdated(updatedCustomer);

      // Close modal
      onClose();
    } catch (err: any) {
      console.error('Error updating customer:', err);

      if (err?.response?.data?.errors) {
        // Handle validation errors from server
        const serverErrors: Record<string, string> = {};
        Object.keys(err.response.data.errors).forEach(key => {
          serverErrors[key.toLowerCase()] = err.response.data.errors[key][0];
        });
        setValidationErrors(serverErrors);
      } else {
        setError(err?.response?.data?.message || 'Không thể cập nhật thông tin khách hàng');
      }
    } finally {
      setSaving(false);
    }
  };

  const handleInputChange = (field: keyof UpdateCustomerForm, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));

    // Clear validation error when user starts typing
    if (validationErrors[field]) {
      setValidationErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  if (!isOpen) return null;

  return (
    <Dialog open={isOpen} onOpenChange={onClose}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>Chỉnh sửa thông tin khách hàng</DialogTitle>
          <DialogDescription>
            Cập nhật thông tin cá nhân và trạng thái của khách hàng
          </DialogDescription>
        </DialogHeader>

        {loading && (
          <div className="flex items-center justify-center h-48">
            <div>Đang tải thông tin khách hàng...</div>
          </div>
        )}

        {error && (
          <div className="bg-red-50 border border-red-200 rounded-lg p-4">
            <div className="text-red-800">{error}</div>
            <Button onClick={loadCustomerData} variant="outline" size="sm" className="mt-2">
              Thử lại
            </Button>
          </div>
        )}

        {customer && !loading && (
          <form onSubmit={handleSubmit} className="space-y-6">
            <div className="grid grid-cols-2 gap-4">
              <div className="space-y-2">
                <Label htmlFor="firstName">Tên *</Label>
                <Input
                  id="firstName"
                  value={formData.firstName}
                  onChange={(e) => handleInputChange('firstName', e.target.value)}
                  placeholder="Nhập tên"
                  disabled={saving}
                />
                {validationErrors.firstName && (
                  <p className="text-sm text-red-600">{validationErrors.firstName}</p>
                )}
              </div>

              <div className="space-y-2">
                <Label htmlFor="lastName">Họ *</Label>
                <Input
                  id="lastName"
                  value={formData.lastName}
                  onChange={(e) => handleInputChange('lastName', e.target.value)}
                  placeholder="Nhập họ"
                  disabled={saving}
                />
                {validationErrors.lastName && (
                  <p className="text-sm text-red-600">{validationErrors.lastName}</p>
                )}
              </div>
            </div>

            <div className="space-y-2">
              <Label htmlFor="email">Email *</Label>
              <Input
                id="email"
                type="email"
                value={formData.email}
                onChange={(e) => handleInputChange('email', e.target.value)}
                placeholder="Nhập địa chỉ email"
                disabled={saving}
              />
              {validationErrors.email && (
                <p className="text-sm text-red-600">{validationErrors.email}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="phoneNumber">Số điện thoại</Label>
              <Input
                id="phoneNumber"
                value={formData.phoneNumber}
                onChange={(e) => handleInputChange('phoneNumber', e.target.value)}
                placeholder="Nhập số điện thoại"
                disabled={saving}
              />
              {validationErrors.phoneNumber && (
                <p className="text-sm text-red-600">{validationErrors.phoneNumber}</p>
              )}
            </div>

            <div className="space-y-4">
              <Label>Trạng thái tài khoản</Label>

              <div className="flex items-center justify-between p-3 border rounded-lg">
                <div>
                  <div className="font-medium">Tài khoản hoạt động</div>
                  <div className="text-sm text-gray-500">
                    Cho phép khách hàng đăng nhập và thực hiện đơn hàng
                  </div>
                </div>
                <Switch
                  checked={formData.isActive}
                  onCheckedChange={(checked) => handleInputChange('isActive', checked)}
                  disabled={saving}
                />
              </div>

              <div className="flex items-center justify-between p-3 border rounded-lg">
                <div>
                  <div className="font-medium">Email đã xác minh</div>
                  <div className="text-sm text-gray-500">
                    Đánh dấu email đã được xác minh
                  </div>
                </div>
                <Switch
                  checked={formData.emailConfirmed}
                  onCheckedChange={(checked) => handleInputChange('emailConfirmed', checked)}
                  disabled={saving}
                />
              </div>
            </div>

            {/* Current status display */}
            <div className="bg-gray-50 p-4 rounded-lg">
              <Label className="text-sm font-medium text-gray-500">Trạng thái hiện tại</Label>
              <div className="flex gap-2 mt-2">
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

            <div className="flex justify-end gap-3 pt-4">
              <Button type="button" variant="outline" onClick={onClose} disabled={saving}>
                Hủy
              </Button>
              <Button type="submit" disabled={saving}>
                {saving ? 'Đang lưu...' : 'Lưu thay đổi'}
              </Button>
            </div>
          </form>
        )}
      </DialogContent>
    </Dialog>
  );
};

export default CustomerEditModal;
