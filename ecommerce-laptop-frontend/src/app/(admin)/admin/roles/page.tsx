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
import { Checkbox } from '@/components/ui/checkbox';
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
import { ScrollArea } from '@/components/ui/scroll-area';
import { Separator } from '@/components/ui/separator';
import { Skeleton } from '@/components/ui/skeleton';
import { Switch } from '@/components/ui/switch';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow
} from '@/components/ui/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { PermissionGuard, useAdminAuth } from '@/contexts/AdminAuthContext';
import {
  createRole,
  deleteRole,
  getPermissions,
  getRoles,
  handleApiError,
  PERMISSIONS,
  updateRole
} from '@/lib/admin-api';
import { CreateRoleRequest, Permission, Role, UpdateRoleRequest } from '@/types/admin';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Edit, Key, Plus, Search, Shield, Trash2, Users } from 'lucide-react';
import React, { useEffect, useMemo, useState } from 'react';
import { toast } from 'sonner';

// =====================================================================
// Permission Constants & Helpers
// =====================================================================

const PermissionModules: Record<string, string> = {
  users: 'Quản lý Admin',
  roles: 'Quản lý vai trò',
  products: 'Quản lý sản phẩm',
  orders: 'Quản lý đơn hàng',
  promotions: 'Quản lý khuyến mãi',
  system: 'Quản trị h thng',
  logs: 'Nhật ký & Kiểm toán',
  security: 'Bảo mật',
  content: 'Quản lý nội dung',
  reports: 'Báo cáo',
  permissions: 'Phân quyền'
};

const PermissionActions: Record<string, { label: string; description: string }> = {
  read: { label: 'Xem', description: 'Quyền xem danh sách và chi tiết.' },
  write: { label: 'Thêm/Sửa', description: 'Quyền tạo mới và cập nhật.' },
  delete: { label: 'Xóa', description: 'Quyền xóa các mục.' },
  manage: { label: 'Quản trị', description: 'Toàn quyền trên module, bao gồm các quyền khác.' },
  export: { label: 'Xuất', description: 'Quyền xuất dữ liệu ra file (CSV, PDF, etc.).' },
};

interface RoleFormData {
  name: string;
  description: string;
  permissionIds: number[];
  isSystem: boolean;
}

// =====================================================================
// New Permission Edit Modal Component (`RoleFormDialog`)
// =====================================================================

const RoleFormDialog = ({
  role,
  open,
  onOpenChange,
  mode
}: {
  role?: Role;
  open: boolean;
  onOpenChange: (open: boolean) => void;
  mode: 'create' | 'edit';
}) => {
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState('basic');
  const [formData, setFormData] = useState<RoleFormData>({
    name: '',
    description: '',
    permissionIds: [],
    isSystem: false,
  });
  const [searchTerm, setSearchTerm] = useState('');
  const [activeModule, setActiveModule] = useState<string | null>(null);

  // Fetch all available permissions
  const { data: permissions = [], isLoading: permissionsLoading } = useQuery<Permission[]>({
    queryKey: ['permissions'],
    queryFn: getPermissions,
    staleTime: 30 * 60 * 1000,
    enabled: open, // only fetch when dialog is open
  });

  // Reset form when role or mode changes
  useEffect(() => {
    if (open) {
      setFormData({
        name: role?.name || '',
        description: role?.description || '',
        permissionIds: role?.permissions?.map(p => p.id) || [],
        isSystem: role?.isSystem || false,
      });
      setSearchTerm(''); // Reset search on open
      setActiveTab('basic');
    }
  }, [role, open]);

  // Mutations for create and update
  const mutationOptions = {
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      toast.success(mode === 'create' ? 'Tạo vai trò thnh cng' : 'Cp nht vai trò thnh cng');
      onOpenChange(false);
    },
    onError: (error: any) => {
      toast.error(handleApiError(error));
    },
  };

  const createMutation = useMutation({
    mutationFn: (data: CreateRoleRequest) => createRole(data),
    ...mutationOptions
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateRoleRequest }) => updateRole(id, data),
    ...mutationOptions
  });

  const isLoading = createMutation.isPending || updateMutation.isPending;

  // Group and filter permissions for display
  const groupedAndFilteredPermissions = useMemo(() => {
    const grouped = (Array.isArray(permissions) ? permissions : []).reduce((acc, p) => {
      const moduleName = p.category || p.name.split(':')[0] || 'other';
      if (!acc[moduleName]) {
        acc[moduleName] = [];
      }
      acc[moduleName].push(p);
      return acc;
    }, {} as Record<string, Permission[]>);

    if (!searchTerm) return grouped;

    const filtered: Record<string, Permission[]> = {};
    for (const moduleName in grouped) {
      const modulePermissions = grouped[moduleName].filter(
        p =>
          p.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
          p.description.toLowerCase().includes(searchTerm.toLowerCase()) ||
          (PermissionModules[moduleName] || moduleName).toLowerCase().includes(searchTerm.toLowerCase())
      );
      if (modulePermissions.length > 0) {
        filtered[moduleName] = modulePermissions;
      }
    }
    return filtered;
  }, [permissions, searchTerm]);

  // Keep active module in sync with available groups
  useEffect(() => {
    const moduleKeys = Object.keys(groupedAndFilteredPermissions || {});
    if (moduleKeys.length === 0) {
      setActiveModule(null);
      return;
    }
    if (!activeModule || !moduleKeys.includes(activeModule)) {
      setActiveModule(moduleKeys[0]);
    }
  }, [groupedAndFilteredPermissions, activeModule]);

  // Handlers
  const handlePermissionChange = (permissionId: number, checked: boolean) => {
    setFormData(prev => ({
      ...prev,
      permissionIds: checked
        ? [...prev.permissionIds, permissionId]
        : prev.permissionIds.filter(id => id !== permissionId),
    }));
  };

  const handleSelectAllModule = (modulePermissions: Permission[], shouldSelect: boolean) => {
    const moduleIds = modulePermissions.map(p => p.id);
    setFormData(prev => ({
      ...prev,
      permissionIds: shouldSelect
        ? [...new Set([...prev.permissionIds, ...moduleIds])] // Add all, remove duplicates
        : prev.permissionIds.filter(id => !moduleIds.includes(id)), // Remove all
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const payload = {
      name: formData.name,
      description: formData.description,
      permissionIds: formData.permissionIds,
      isSystem: formData.isSystem,
    };
    if (mode === 'create') {
      createMutation.mutate(payload);
    } else if (role) {
      updateMutation.mutate({ id: String(role.id), data: payload });
    }
  };

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-4xl max-h-[90vh] flex flex-col">
        <DialogHeader>
          <DialogTitle>{mode === 'create' ? 'Tạo vai trò mới' : `Sửa vai trò: ${role?.name}`}</DialogTitle>
          <DialogDescription>
            {mode === 'create'
              ? 'Định nghĩa vai trò và phân quyền chi tiết cho người dùng hệ thống.'
              : 'Cập nhật thông tin và quyền hạn cho vai trò này.'}
          </DialogDescription>
        </DialogHeader>

        <form onSubmit={handleSubmit} className="flex-1 flex flex-col overflow-hidden">
          <Tabs value={activeTab} onValueChange={setActiveTab} className="h-full flex flex-col">
            <TabsList className="grid w-full grid-cols-2">
              <TabsTrigger value="basic">Thông tin cơ bản</TabsTrigger>
              <TabsTrigger value="permissions">Phân quyền chi tiết</TabsTrigger>
            </TabsList>

            <div className="flex-1 overflow-auto">
              <TabsContent value="basic" className="p-4 space-y-4">
                <div className="space-y-2">
                  <Label htmlFor="name">Tên vai trò</Label>
                  <Input
                    id="name"
                    value={formData.name}
                    onChange={e => setFormData(p => ({ ...p, name: e.target.value }))}
                    placeholder="VD: Quản lý sản phẩm"
                    required
                    disabled={isLoading || formData.isSystem}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="description">Mô tả</Label>
                  <Input
                    id="description"
                    value={formData.description}
                    onChange={e => setFormData(p => ({ ...p, description: e.target.value }))}
                    placeholder="VD: Chịu trách nhiệm quản lý sản phẩm, thương hiệu và danh mục"
                    required
                    disabled={isLoading}
                  />
                </div>
                <div className="flex items-center space-x-2 pt-2">
                  <Switch
                    id="isSystem"
                    checked={formData.isSystem}
                    onCheckedChange={c => setFormData(p => ({ ...p, isSystem: c }))}
                    disabled={isLoading || (mode === 'edit' && role?.isSystem)}
                  />
                  <Label htmlFor="isSystem" className="cursor-pointer">
                    Vai trò hệ thống
                  </Label>
                </div>
                {formData.isSystem && (
                  <p className="text-xs text-amber-600">
                    Vai trò hệ thống có các quyền hạn cố định và không thể bị chỉnh sửa hoặc xóa.
                  </p>
                )}
              </TabsContent>

              <TabsContent value="permissions" className="h-full flex flex-col overflow-hidden">
                <div className="px-4 pt-4 pb-2">
                  <div className="flex items-center justify-between mb-2">
                    <div className="flex-1">
                      <h3 className="text-lg font-medium">Danh sách quyền hạn</h3>
                      <p className="text-sm text-muted-foreground">
                        Chọn các quyền hạn chi tiết cho vai trò này.
                      </p>
                    </div>
                    <Badge variant="secondary">{formData.permissionIds.length} quyền được chọn</Badge>
                  </div>
                  <div className="relative">
                    <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                      placeholder="Tìm kiếm quyền..."
                      value={searchTerm}
                      onChange={e => setSearchTerm(e.target.value)}
                      className="pl-8 w-full"
                      disabled={permissionsLoading}
                    />
                  </div>
                </div>

                <div className="flex-1 px-4 pb-4 h-[calc(100vh-400px)] flex overflow-hidden gap-4">
                  {permissionsLoading ? (
                    <div className="w-full space-y-4 py-4">
                      {Array.from({ length: 4 }).map((_, i) => (
                        <Skeleton key={i} className="h-24 w-full" />
                      ))}
                    </div>
                  ) : (
                    Object.keys(groupedAndFilteredPermissions).length > 0 ? (
                      <div className="flex w-full overflow-hidden">
                        {/* Left menu (modules) */}
                        <ScrollArea className="hidden md:block w-56 shrink-0 border rounded-lg">
                          <div className="p-2">
                            {Object.entries(groupedAndFilteredPermissions).map(([module, modulePermissions]) => {
                              const selectedCount = modulePermissions.filter(p => formData.permissionIds.includes(p.id)).length;
                              return (
                                <button
                                  key={module}
                                  type="button"
                                  onClick={() => setActiveModule(module)}
                                  className={`w-full text-left px-3 py-2 rounded-md mb-1 hover:bg-muted ${activeModule === module ? 'bg-muted' : ''}`}
                                >
                                  <div className="flex items-center justify-between">
                                    <span className="truncate">{PermissionModules[module] || module}</span>
                                    <Badge variant="outline" className="ml-2">
                                      {selectedCount}/{modulePermissions.length}
                                    </Badge>
                                  </div>
                                </button>
                              );
                            })}
                          </div>
                        </ScrollArea>

                        {/* Right content */}
                        <div className="flex-1 ml-0 md:ml-4 overflow-hidden">
                          {activeModule && groupedAndFilteredPermissions[activeModule] ? (
                            <Card>
                              <CardHeader className="flex-row items-center justify-between p-4">
                                <div className="flex items-center space-x-2">
                                  <Shield className="h-5 w-5 text-primary" />
                                  <CardTitle className="text-base">{PermissionModules[activeModule] || activeModule}</CardTitle>
                                  <Badge variant="outline">
                                    {groupedAndFilteredPermissions[activeModule].filter(p => formData.permissionIds.includes(p.id)).length}
                                    {' / '}
                                    {groupedAndFilteredPermissions[activeModule].length}
                                  </Badge>
                                </div>
                                <div className="flex items-center space-x-2">
                                  {(() => {
                                    const modulePermissions = groupedAndFilteredPermissions[activeModule];
                                    const selectedCount = modulePermissions.filter(p => formData.permissionIds.includes(p.id)).length;
                                    const allSelected = selectedCount > 0 && selectedCount === modulePermissions.length;
                                    return (
                                      <>
                                        <Checkbox
                                          id={`select-all-${activeModule}`}
                                          checked={allSelected}
                                          onCheckedChange={(checked) => handleSelectAllModule(modulePermissions, !!checked)}
                                          aria-label={`Select all permissions for ${activeModule}`}
                                        />
                                        <Label htmlFor={`select-all-${activeModule}`} className="text-sm font-normal cursor-pointer">
                                          Chọn tất cả
                                        </Label>
                                      </>
                                    );
                                  })()}
                                </div>
                              </CardHeader>
                              <Separator />
                              <CardContent className="p-0">
                                <ScrollArea className="h-[calc(100vh-500px)]">
                                  <Table>
                                    <TableHeader>
                                      <TableRow>
                                        <TableHead className="w-[50px]"></TableHead>
                                        <TableHead>Tên quyền</TableHead>
                                        <TableHead>Mô tả</TableHead>
                                      </TableRow>
                                    </TableHeader>
                                    <TableBody>
                                      {groupedAndFilteredPermissions[activeModule].map(p => {
                                        const actionKey = p.name.split(':')[1] || 'read';
                                        const action = PermissionActions[actionKey];
                                        return (
                                          <TableRow key={p.id}>
                                            <TableCell className="p-2 text-center">
                                              <Checkbox
                                                checked={formData.permissionIds.includes(p.id)}
                                                onCheckedChange={c => handlePermissionChange(p.id, !!c)}
                                                aria-label={`Select permission ${p.name}`}
                                              />
                                            </TableCell>
                                            <TableCell className="font-medium">
                                              <div className="flex items-center space-x-2">
                                                <span>{action?.label || actionKey}</span>
                                                <Badge variant="outline" className="text-xs">{p.name}</Badge>
                                              </div>
                                            </TableCell>
                                            <TableCell className="text-muted-foreground text-xs">
                                              {p.description}
                                            </TableCell>
                                          </TableRow>
                                        );
                                      })}
                                    </TableBody>
                                  </Table>
                                </ScrollArea>
                              </CardContent>
                            </Card>
                          ) : (
                            <div className="w-full flex items-center justify-center text-muted-foreground">Chọn nhóm quyền để xem chi tiết</div>
                          )}
                        </div>
                      </div>
                    ) : (
                      <div className="w-full text-center py-10">
                        <p className="text-muted-foreground">Không tìm thấy quyền nào.</p>
                      </div>
                    )
                  )}
                </div>
              </TabsContent>
            </div>
          </Tabs>

          <DialogFooter className="mt-auto p-4 border-t">
            <Button
              type="button"
              variant="ghost"
              onClick={() => onOpenChange(false)}
              disabled={isLoading}
            >
              Hủy
            </Button>
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'đang lưu...' : (mode === 'create' ? 'Tạo vai trò' : 'Lưu thay đổi')}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
};

// =====================================================================
// Role Card & Delete Dialog (Minor visual tweaks)
// =====================================================================

const RoleCard = ({ role }: { role: Role }) => {
  const [showEdit, setShowEdit] = useState(false);
  const [showDelete, setShowDelete] = useState(false);
  const queryClient = useQueryClient();
  const { isSuperAdmin } = useAdminAuth();

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteRole(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      toast.success('Xóa vai trò thành công');
      setShowDelete(false);
    },
    onError: (error) => toast.error(handleApiError(error)),
  });

  const canModify = isSuperAdmin || !role.isSystem;

  return (
    <>
      <Card className="h-full flex flex-col">
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center space-x-3">
              <div className="p-2 bg-primary/10 rounded-md">
                <Shield className="h-5 w-5 text-primary" />
              </div>
              <div>
                <CardTitle className="text-lg">{role.name}</CardTitle>
                <CardDescription>{role.description}</CardDescription>
              </div>
            </div>
            {canModify && (
              <div className="flex items-center space-x-1">
                <PermissionGuard permission={PERMISSIONS.ROLES_WRITE}>
                  <Button variant="ghost" size="sm" onClick={() => setShowEdit(true)}>
                    <Edit className="h-4 w-4" />
                  </Button>
                </PermissionGuard>
                <PermissionGuard permission={PERMISSIONS.ROLES_DELETE}>
                  <Button variant="ghost" size="sm" onClick={() => setShowDelete(true)} className="text-destructive hover:text-destructive">
                    <Trash2 className="h-4 w-4" />
                  </Button>
                </PermissionGuard>
              </div>
            )}
          </div>
        </CardHeader>
        <CardContent className="flex-1 space-y-3">
          <div className="flex items-center justify-between text-sm border-t pt-3">
            <span className="text-muted-foreground flex items-center"><Users className="h-4 w-4 mr-1" /> Số người dùng</span>
            <Badge variant="secondary">{role.userCount}</Badge>
          </div>
          <div className="flex items-center justify-between text-sm">
            <span className="text-muted-foreground flex items-center"><Key className="h-4 w-4 mr-1" /> Tổng quyền</span>
            <Badge variant="secondary">{role.permissions?.length || 0}</Badge>
          </div>
          {role.isSystem && (
            <div className="flex items-center justify-between text-sm">
              <span className="text-muted-foreground flex items-center">Loại vai trò</span>
              <Badge variant="outline" className="text-amber-600 border-amber-500">Hệ thống</Badge>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Edit Dialog */}
      {showEdit && <RoleFormDialog role={role} open={showEdit} onOpenChange={setShowEdit} mode="edit" />}

      {/* Delete Dialog */}
      <Dialog open={showDelete} onOpenChange={setShowDelete}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Xác nhận xóa vai trò</DialogTitle>
            <DialogDescription>
              Bạn có chắc chắn muốn xóa vai trò <strong>{role.name}</strong>? Hành động này không thể hoàn tác và có thể ảnh hưởng đến người dùng hiện tại.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setShowDelete(false)}>Hủy</Button>
            <Button
              variant="destructive"
              onClick={() => deleteMutation.mutate(String(role.id))}
              disabled={deleteMutation.isPending}
            >
              {deleteMutation.isPending ? 'đang xóa...' : 'Xóa vĩnh viễn'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </>
  );
};


// =====================================================================
// Main Page Component
// =====================================================================

export default function RolesPage() {
  const [showCreate, setShowCreate] = useState(false);

  const { data: roles, isLoading, error } = useQuery<Role[]>({
    queryKey: ['roles'],
    queryFn: getRoles,
  });

  const { data: permissions } = useQuery<Permission[]>({
    queryKey: ['permissions'],
    queryFn: getPermissions,
    staleTime: 5 * 60 * 1000,
  });


  if (error) {
    return (
      <div className="flex items-center justify-center h-full">
        <div className="text-center">
          <h2 className="text-xl font-semibold text-destructive">Lỗi tải dữ liệu</h2>
          <p className="text-muted-foreground">Không thể tải danh sách vai trò. Vui lòng thử lại sau.</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Quản lý vai trò</h1>
          <p className="text-muted-foreground">
            Tạo, quản lý và phân quyền cho các vai trò trong hệ thống.
          </p>
        </div>
        <PermissionGuard permission={PERMISSIONS.ROLES_WRITE}>
          <Button onClick={() => setShowCreate(true)}>
            <Plus className="h-4 w-4 mr-2" />
            Tạo vai trò mới
          </Button>
        </PermissionGuard>
      </div>

      {/* Stats Cards */}
      <div className="grid gap-4 md:grid-cols-3">
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Tổng vai trò</CardTitle>
            <Shield className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {isLoading ? <Skeleton className="h-8 w-16" /> : <div className="text-2xl font-bold">{roles?.length || 0}</div>}
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Vai trò hệ thống</CardTitle>
            <Shield className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {isLoading ? <Skeleton className="h-8 w-16" /> : <div className="text-2xl font-bold">{roles?.filter(r => r.isSystem).length || 0}</div>}
          </CardContent>
        </Card>
        <Card>
          <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
            <CardTitle className="text-sm font-medium">Tng quyền hạn</CardTitle>
            <Key className="h-4 w-4 text-muted-foreground" />
          </CardHeader>
          <CardContent>
            {isLoading ? <Skeleton className="h-8 w-16" /> : <div className="text-2xl font-bold">{permissions?.length || 0}</div>}
          </CardContent>
        </Card>
      </div>

      {/* Roles Grid */}
      {isLoading ? (
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {Array.from({ length: 6 }).map((_, i) => (
            <Card key={i}><CardHeader><Skeleton className="h-6 w-32" /><Skeleton className="h-4 w-full mt-2" /></CardHeader><CardContent><Skeleton className="h-10 w-full" /></CardContent></Card>
          ))}
        </div>
      ) : (
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
          {roles?.map((role) => (
            <RoleCard key={role.id} role={role} />
          ))}
        </div>
      )}

      {/* Create Role Dialog */}
      {showCreate && <RoleFormDialog open={showCreate} onOpenChange={setShowCreate} mode="create" />}

    </div>
  );
}