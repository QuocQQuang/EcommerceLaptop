'use client';

import {
  Avatar,
  AvatarFallback
} from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent
} from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue
} from '@/components/ui/select';
import { Skeleton } from '@/components/ui/skeleton';
import { Switch } from '@/components/ui/switch';
import { PermissionGuard, useAdminAuth } from '@/contexts/AdminAuthContext';
import {
  createUser,
  deleteUser,
  getRoles,
  getUsers,
  handleApiError,
  PERMISSIONS,
  toggleUserStatus,
  updateUser
} from '@/lib/admin-api';
import { AdminUser, CreateAdminUserRequest, Role, UpdateAdminUserRequest } from '@/types/admin';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Edit, Eye, EyeOff, Search, Trash2, UserPlus } from 'lucide-react';
import React, { useState } from 'react';
import { toast } from 'sonner';

const ROLES_COLORS = {
  SystemAdmin: 'bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200',
  ProductAdmin: 'bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200',
  SalesAdmin: 'bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200',
  PromotionManager: 'bg-purple-100 text-purple-800 dark:bg-purple-900 dark:text-purple-200',
  Customer: 'bg-blue-100 text-blue-800 dark:bg-blue-900 dark:text-blue-200'
};

const UserStatusBadge = ({ isActive }: { isActive: boolean }) => (
  <Badge variant={isActive ? 'default' : 'secondary'}>
    {isActive ? 'Hoạt động' : 'Đã khóa'}
  </Badge>
);

const UserRoleBadge = ({ roleName }: { roleName: string }) => (
  <Badge className={ROLES_COLORS[roleName as keyof typeof ROLES_COLORS] || 'bg-gray-100 text-gray-800 dark:bg-gray-900 dark:text-gray-200'}>
    {roleName}
  </Badge>
);

interface UserFormData {
  email: string;
  firstName: string;
  lastName: string;
  password?: string;
  roleId: string | null;
  isActive: boolean;
}

const UserFormDialog = ({
  user,
  open,
  onOpenChange,
  mode
}: {
  user?: AdminUser;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  mode: 'create' | 'edit';
}) => {
  const queryClient = useQueryClient();
  const [formData, setFormData] = useState<UserFormData>({
    email: user?.email || '',
    firstName: user?.firstName || '',
    lastName: user?.lastName || '',
    password: '',
    roleId: user?.roleId ? user.roleId.toString() : null,
    isActive: user?.isActive ?? true,
  });
  const [showPassword, setShowPassword] = useState(false);

  const { data: roles, isLoading: rolesLoading } = useQuery({
    queryKey: ['roles'],
    queryFn: getRoles,
    staleTime: 30 * 60 * 1000,
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateAdminUserRequest) => createUser(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success('Tạo người dùng thành công');
      onOpenChange(false);
      resetForm();
    },
    onError: (error) => {
      toast.error(handleApiError(error));
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdateAdminUserRequest }) => updateUser(id.toString(), data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success('Cập nhật người dùng thành công');
      onOpenChange(false);
    },
    onError: (error) => {
      toast.error(handleApiError(error));
    },
  });

  const resetForm = () => {
    setFormData({
      email: '',
      firstName: '',
      lastName: '',
      password: '',
      roleId: null,
      isActive: true,
    });
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    if (mode === 'create') {
      if (!formData.password) {
        toast.error('Vui lòng nhập mật khẩu');
        return;
      }
      if (!formData.roleId) {
        toast.error('Vui lòng chọn vai trò');
        return;
      }
      createMutation.mutate({
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        password: formData.password,
        roleId: Number(formData.roleId),
        isActive: formData.isActive,
      });
    } else if (user) {
      if (!formData.roleId) {
        toast.error('Vui lòng chọn vai trò');
        return;
      }
      const updateData: UpdateAdminUserRequest = {
        firstName: formData.firstName,
        lastName: formData.lastName,
        email: formData.email,
        roleId: Number(formData.roleId),
        isActive: formData.isActive,
      };
      updateMutation.mutate({ id: Number(user.id), data: updateData });
    }
  };

  const isLoading = createMutation.isPending || updateMutation.isPending;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>
            {mode === 'create' ? 'Tạo người dùng mới' : 'Sửa thông tin người dùng'}
          </DialogTitle>
          <DialogDescription>
            {mode === 'create'
              ? 'Tạo tài khoản mới cho hệ thống'
              : `Cập nhật thông tin cho ${user?.fullName}`
            }
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <div className="space-y-2">
              <Label htmlFor="firstName">Họ</Label>
              <Input
                id="firstName"
                value={formData.firstName}
                onChange={(e) => setFormData(prev => ({ ...prev, firstName: e.target.value }))}
                required
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="lastName">Tn</Label>
              <Input
                id="lastName"
                value={formData.lastName}
                onChange={(e) => setFormData(prev => ({ ...prev, lastName: e.target.value }))}
                required
              />
            </div>
          </div>

          <div className="space-y-2">
            <Label htmlFor="email">Email</Label>
            <Input
              id="email"
              type="email"
              value={formData.email}
              onChange={(e) => setFormData(prev => ({ ...prev, email: e.target.value }))}
              required
              disabled={mode === 'edit'} // Email cannot be changed
            />
          </div>


          {mode === 'create' && (
            <div className="space-y-2">
              <Label htmlFor="password">Mật khẩu</Label>
              <div className="relative">
                <Input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  value={formData.password}
                  onChange={(e) => setFormData(prev => ({ ...prev, password: e.target.value }))}
                  required
                />
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  className="absolute right-0 top-0 h-full px-3"
                  onClick={() => setShowPassword(!showPassword)}
                >
                  {showPassword ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
                </Button>
              </div>
            </div>
          )}

          <div className="space-y-2">
            <Label htmlFor="role">Vai trò</Label>
            <Select
              value={formData.roleId || ''}
              onValueChange={(value) => setFormData(prev => ({ ...prev, roleId: value || null }))}
              required
            >
              <SelectTrigger>
                <SelectValue placeholder="Chọn vai trò" />
              </SelectTrigger>
              <SelectContent>
                {rolesLoading ? (
                  <div className="p-2">
                    <Skeleton className="h-4 w-full" />
                  </div>
                ) : (
                  roles?.map((role: Role) => (
                    <SelectItem key={role.id} value={role.id.toString()}>
                      {role.name} - {role.description}
                    </SelectItem>
                  ))
                )}
              </SelectContent>
            </Select>
          </div>

          <div className="flex items-center space-x-2">
            <Switch
              id="isActive"
              checked={formData.isActive}
              onCheckedChange={(checked) => setFormData(prev => ({ ...prev, isActive: checked }))}
            />
            <Label htmlFor="isActive">Tài khoản hoạt động</Label>
          </div>

          <DialogFooter>
            <Button
              type="button"
              variant="outline"
              onClick={() => onOpenChange(false)}
              disabled={isLoading}
            >
              Hủy
            </Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'đang xử lý...' : (mode === 'create' ? 'Tạo tài khoản' : 'Cập nhật')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};

const UserActions = ({ user }: { user: AdminUser }) => {
  const [showEdit, setShowEdit] = useState(false);
  const [showDelete, setShowDelete] = useState(false);
  const queryClient = useQueryClient();
  const { isSuperAdmin } = useAdminAuth();

  const toggleStatusMutation = useMutation({
    mutationFn: (id: number) => toggleUserStatus(id.toString()),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success(`${user.isActive ? 'Khóa' : 'Mở khóa'} tài khoản thành công`);
    },
    onError: (error) => {
      toast.error(handleApiError(error));
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => deleteUser(id.toString()),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      toast.success('Xóa tài khoản thành công');
      setShowDelete(false);
    },
    onError: (error) => {
      toast.error(handleApiError(error));
    },
  });

  // Super admins can't modify other super admins unless they are the same user
  const canModify = isSuperAdmin || (user.roleName !== 'SystemAdmin' && user.roleName !== 'SuperAdmin');

  if (!canModify) {
    return null;
  }

  return (
    <div className="flex items-center space-x-2">
      <PermissionGuard permission={PERMISSIONS.USERS_WRITE}>
        <Button
          variant="outline"
          size="sm"
          onClick={() => setShowEdit(true)}
        >
          <Edit className="h-3 w-3 mr-1" />
            Sửa
          </Button>
      </PermissionGuard>

      <PermissionGuard permission={PERMISSIONS.USERS_WRITE}>
        <Button
          variant="outline"
          size="sm"
          onClick={() => toggleStatusMutation.mutate(user.id)}
          disabled={toggleStatusMutation.isPending}
        >
          {user.isActive ? 'Khóa' : 'Mở khóa'}
        </Button>
      </PermissionGuard>

      <PermissionGuard permission={PERMISSIONS.USERS_DELETE}>
        {user.roleName !== 'SystemAdmin' && user.roleName !== 'SuperAdmin' && (
          <Button
            variant="destructive"
            size="sm"
            onClick={() => setShowDelete(true)}
          >
            <Trash2 className="h-3 w-3 mr-1" />
            Xóa
          </Button>
        )}
      </PermissionGuard>

      {/* Edit Dialog */}
      <UserFormDialog
        user={user}
        open={showEdit}
        onOpenChange={setShowEdit}
        mode="edit"
      />

      {/* Delete Dialog */}
      <Dialog open={showDelete} onOpenChange={setShowDelete}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Xác nhận xóa</DialogTitle>
            <DialogDescription>
              Bạn có chắc muốn xóa tài khoản <strong>{user.fullName}</strong>?
              Hành động này không thể hoàn tác.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDelete(false)}>
              Hủy
            </Button>
            <Button
              variant="destructive"
              onClick={() => deleteMutation.mutate(user.id)}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? 'đang xóa...' : 'Xóa tài khoản'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
};

export default function UsersPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [limit, setLimit] = useState(10);
  const [showCreate, setShowCreate] = useState(false);

  const { data: usersData, isLoading, error } = useQuery({
    queryKey: ['users', page, limit, search],
    queryFn: () => getUsers(page, limit, search),
    staleTime: 5 * 60 * 1000,
    enabled: true,
  });

  // Load roles to determine which roles have 'roles:*' permissions
  const { data: rolesData } = useQuery<Role[]>({
    queryKey: ['roles'],
    queryFn: getRoles,
    staleTime: 30 * 60 * 1000,
  });

  if (error) {
    return (
      <div className="flex items-center justify-center min-h-[400px]">
        <div className="text-center">
          <h2 className="text-xl font-semibold text-destructive mb-2">Lỗi tải dữ liệu</h2>
          <p className="text-muted-foreground">Không thể tải danh sách người dùng</p>
        </div>
      </div>
    );
  }

  const users = usersData?.users || [];
  const total = usersData?.totalCount || 0;
  const totalPages = usersData?.totalPages || 1;
  const pageSize = usersData?.pageSize || limit;
  const currentPage = usersData?.currentPage || page;

  // Build a set of roleIds that have any 'roles:*' permission
  const roleIdsWithRolePerms = new Set<number>(
    (rolesData || [])
      .filter(r => Array.isArray(r.permissions) && r.permissions.some(p => (p.name || '').startsWith('roles:')))
      .map(r => r.id)
  );

  // Hide users whose role has any role-management permission
  const filteredUsers = users.filter((u: AdminUser) => !roleIdsWithRolePerms.has(u.roleId));

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Quản lý Admin</h1>
          <p className="text-muted-foreground">
            Quản lý tài khoản và phân quyền người dùng hệ thống
          </p>
        </div>

        <div className="flex gap-2">
          <PermissionGuard permission={PERMISSIONS.USERS_WRITE}>
            <Button onClick={() => setShowCreate(true)}>
              <UserPlus className="h-4 w-4 mr-2" />
              Tạo người dùng mới
            </Button>
          </PermissionGuard>
        </div>
      </div>

      {/* Search and Filters */}
      <Card>
        <CardContent className="p-6">
          <div className="flex flex-col sm:flex-row gap-4 items-center">
            <div className="relative flex-1 max-w-sm">
              <Search className="absolute left-3 top-3 h-4 w-4 text-muted-foreground" />
              <Input
                placeholder="Tìm kiếm người dùng..."
                className="pl-10"
                value={search}
                onChange={(e) => {
                  setSearch(e.target.value);
                  setPage(1);
                }}
              />
            </div>

            <div className="flex items-center gap-2">
              <span className="text-sm text-muted-foreground">Hiển thị:</span>
              <Select
                value={limit.toString()}
                onValueChange={(value) => {
                  setLimit(Number(value));
                  setPage(1);
                }}
              >
                <SelectTrigger className="w-20">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="10">10</SelectItem>
                  <SelectItem value="25">25</SelectItem>
                  <SelectItem value="50">50</SelectItem>
                </SelectContent>
              </Select>
            </div>
          </div>
        </CardContent>
      </Card>

      {/* Users Table */}
      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-8">
              <div className="space-y-4">
                {Array.from({ length: 5 }).map((_, i) => (
                  <div key={i} className="flex items-center space-x-4 p-4 border-b">
                    <Skeleton className="h-10 w-10 rounded-full" />
                    <div className="flex-1 space-y-2">
                      <Skeleton className="h-4 w-48" />
                      <Skeleton className="h-3 w-32" />
                    </div>
                    <div className="space-x-2">
                      <Skeleton className="h-8 w-16" />
                      <Skeleton className="h-8 w-16" />
                    </div>
                  </div>
                ))}
              </div>
            </div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-muted/50">
                  <tr>
                    <th className="h-12 px-4 text-left align-middle font-medium">Người dùng</th>
                    <th className="h-12 px-4 text-left align-middle font-medium">Email</th>
                    <th className="h-12 px-4 text-left align-middle font-medium">Vai trò</th>
                    <th className="h-12 px-4 text-left align-middle font-medium">Trạng thái</th>
                    <th className="h-12 px-4 text-left align-middle font-medium">Ngày tạo</th>
                    <th className="h-12 px-4 text-right align-middle font-medium">Thao tác</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredUsers.map((user: AdminUser) => (
                    <tr key={user.id} className="border-b hover:bg-accent/50">
                      <td className="p-4">
                        <div className="flex items-center">
                          <Avatar className="h-10 w-10">
                            <AvatarFallback className="bg-primary text-primary-foreground">
                              {user.firstName?.[0]}{user.lastName?.[0]}
                            </AvatarFallback>
                          </Avatar>
                          <div className="ml-3">
                            <p className="text-sm font-medium">{user.fullName}</p>
                            <p className="text-xs text-muted-foreground">ID: {user.id.toString().slice(-8)}</p>
                          </div>
                        </div>
                      </td>
                      <td className="p-4">
                        <div className="text-sm">{user.email}</div>
                      </td>
                      <td className="p-4">
                        <UserRoleBadge roleName={user.roleName} />
                      </td>
                      <td className="p-4">
                        <UserStatusBadge isActive={user.isActive} />
                      </td>
                      <td className="p-4">
                        <div className="text-sm">
                          {new Date(user.createdAt).toLocaleString('vi-VN')}
                        </div>
                      </td>
                      <td className="p-4 text-right">
                        <UserActions user={user} />
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>

              {/* Pagination */}
              {total > 0 && (
                <div className="flex items-center justify-between px-6 py-3 border-t">
                  <div className="text-sm text-muted-foreground">
                    Hiển thị {((currentPage - 1) * pageSize) + 1} đến{' '}
                    {Math.min(currentPage * pageSize, total)} của {total} kết quả
                  </div>
                  <div className="flex items-center space-x-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setPage(p => Math.max(1, p - 1))}
                      disabled={currentPage === 1}
                    >
                      Trước
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setPage(p => p + 1)}
                      disabled={currentPage >= totalPages}
                    >
                      Sau
                    </Button>
                  </div>
                </div>
              )}
            </div>
          )}
        </CardContent>
      </Card>

      {/* Create User Dialog */}
      <UserFormDialog
        open={showCreate}
        onOpenChange={setShowCreate}
        mode="create"
      />
    </div>
  );
}