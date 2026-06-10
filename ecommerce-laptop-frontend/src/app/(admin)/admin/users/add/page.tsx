'use client';

import { Avatar, AvatarFallback, AvatarImage } from '@/components/ui/avatar';
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
  Form,
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';
import { Separator } from '@/components/ui/separator';
import { Switch } from '@/components/ui/switch';
import { Textarea } from '@/components/ui/textarea';
import {
  PermissionGuard,
  useAdminAuth
} from '@/contexts/AdminAuthContext';
import { PERMISSIONS, hasPermission } from '@/lib/admin-api';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery } from '@tanstack/react-query';
import {
  AlertTriangle,
  ArrowLeft,
  Eye,
  EyeOff,
  FileImage,
  Lock,
  Mail,
  Phone,
  Save,
  Shield,
  User
} from 'lucide-react';
import { useRouter } from 'next/navigation';
import React, { useState } from 'react';
import { useForm } from 'react-hook-form';
import { toast } from 'sonner';
import * as z from 'zod';

interface UserRole {
  id: number;
  name: string;
  displayName: string;
  description: string;
  permissions: string[];
  isSystemRole: boolean;
}

// Form validation schema
const addUserSchema = z.object({
  firstName: z.string()
    .min(1, 'Họ là bắt buộc')
    .min(2, 'Họ phải có ít nhất 2 ký tự')
    .max(50, 'Họ không được quá 50 ký tự'),
  lastName: z.string()
    .min(1, 'Tên là bắt buộc')
    .min(2, 'Tên phải có ít nhất 2 ký tự')
    .max(50, 'Tên không được quá 50 ký tự'),
  email: z.string()
    .min(1, 'Email là bắt buộc')
    .email('Email không hợp lệ')
    .max(100, 'Email không được quá 100 ký tự'),
  phone: z.string()
    .optional()
    .refine((val) => !val || /^[0-9]{10,11}$/.test(val), {
      message: 'Số điện thoại phải có 10-11 chữ số'
    }),
  password: z.string()
    .min(8, 'Mật khẩu phải có ít nhất 8 ký tự')
    .regex(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/, 'Mật khẩu phải có ít nhất 1 chữ hoa, 1 chữ thường và 1 số'),
  confirmPassword: z.string()
    .min(1, 'Xác nhận mật khẩu là bắt buộc'),
  roleId: z.number()
    .min(1, 'Vui lòng chọn vai trò'),
  isActive: z.boolean(),
  bio: z.string().optional(),
  sendWelcomeEmail: z.boolean(),
}).refine((data) => data.password === data.confirmPassword, {
  message: 'Mật khẩu xác nhận không khớp',
  path: ['confirmPassword'],
});

type AddUserFormData = z.infer<typeof addUserSchema>;

// Mock API calls - Replace with real API
const useUserRolesQuery = () => {
  return useQuery({
    queryKey: ['admin', 'user-roles'],
    queryFn: async (): Promise<UserRole[]> => {
      await new Promise(resolve => setTimeout(resolve, 800));

      return [
        {
          id: 1,
          name: 'super_admin',
          displayName: 'Super Admin',
          description: 'Quyền cao nhất, có thể truy cập tất cả chức năng',
          permissions: ['*'],
          isSystemRole: true
        },
        {
          id: 2,
          name: 'manager',
          displayName: 'Quản lý',
          description: 'Quản lý đơn hàng, sản phẩm và nhân viên',
          permissions: ['orders:read', 'orders:write', 'products:read', 'products:write', 'users:read'],
          isSystemRole: false
        },
        {
          id: 3,
          name: 'staff',
          displayName: 'Nhân viên',
          description: 'Xem đơn hàng và sản phẩm, không có quyền chỉnh sửa',
          permissions: ['orders:read', 'products:read'],
          isSystemRole: false
        },
        {
          id: 4,
          name: 'content_manager',
          displayName: 'Quản lý nội dung',
          description: 'Quản lý blog, tin tức và nội dung website',
          permissions: ['content:read', 'content:write', 'media:read', 'media:write'],
          isSystemRole: false
        }
      ];
    },
    staleTime: 5 * 60 * 1000,
  });
};

const useCreateUserMutation = () => {
  return useMutation({
    mutationFn: async (data: AddUserFormData) => {
      await new Promise(resolve => setTimeout(resolve, 2000));
      console.log('Creating user:', data);
      return { success: true, id: Math.random().toString() };
    }
  });
};

const getRoleBadge = (role: UserRole) => {
  const variants = {
    super_admin: 'destructive' as const,
    manager: 'default' as const,
    staff: 'secondary' as const,
    content_manager: 'outline' as const
  };

  return (
    <Badge variant={variants[role.name as keyof typeof variants] || 'outline'}>
      <Shield className="h-3 w-3 mr-1" />
      {role.displayName}
    </Badge>
  );
};

const getPermissionCount = (permissions: string[]) => {
  if (permissions.includes('*')) return 'Tất cả quyền';
  return `${permissions.length} quyền`;
};

export default function AddUserPage() {
  const router = useRouter();
  const { user } = useAdminAuth();

  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [selectedAvatar, setSelectedAvatar] = useState<string | null>(null);

  const {
    data: roles,
    isLoading: rolesLoading
  } = useUserRolesQuery();

  const canAssignRolePermissions = user ? hasPermission(user.permissions || [], PERMISSIONS.ROLES_MANAGE) : false;

  const createUserMutation = useCreateUserMutation();

  const form = useForm<AddUserFormData>({
    resolver: zodResolver(addUserSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      password: '',
      confirmPassword: '',
      roleId: 0,
      isActive: true,
      bio: '',
      sendWelcomeEmail: true,
    },
  });

  const selectedRole = roles?.find(role => role.id === form.watch('roleId'));

  const onSubmit = async (data: AddUserFormData) => {
    try {
      await createUserMutation.mutateAsync(data);
      toast.success('Tạo người dùng thành công!');
      router.push('/users');
    } catch (error) {
      toast.error('Có lỗi xảy ra khi tạo người dùng');
      console.error('Error creating user:', error);
    }
  };

  const handleAvatarUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e) => {
        setSelectedAvatar(e.target?.result as string);
      };
      reader.readAsDataURL(file);
    }
  };

  return (
    <PermissionGuard permission={PERMISSIONS.USERS_WRITE}>
      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center gap-4">
          <Button
            variant="outline"
            size="sm"
            onClick={() => router.back()}
          >
            <ArrowLeft className="h-4 w-4 mr-2" />
            Quay lại
          </Button>

          <div>
            <h1 className="text-3xl font-bold">Thêm người dùng mới</h1>
            <p className="text-muted-foreground">
              Tạo tài khoản admin mới cho hệ thống
            </p>
          </div>
        </div>

        <Form {...form}>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-8">
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8">
              {/* Main Form */}
              <div className="lg:col-span-2 space-y-8">
                {/* Basic Information */}
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <User className="h-5 w-5" />
                      Thông tin cơ bản
                    </CardTitle>
                    <CardDescription>
                      Thông tin cá nhân của người dùng
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-6">
                    {/* Avatar Upload */}
                    <div className="flex items-center gap-6">
                      <div className="relative">
                        <Avatar className="h-24 w-24">
                          <AvatarImage src={selectedAvatar || undefined} />
                          <AvatarFallback className="text-lg">
                            {form.watch('firstName')?.charAt(0) || ''}
                            {form.watch('lastName')?.charAt(0) || ''}
                          </AvatarFallback>
                        </Avatar>
                        <Button
                          type="button"
                          size="sm"
                          variant="outline"
                          className="absolute -bottom-2 -right-2 h-8 w-8 rounded-full p-0"
                          onClick={() => document.getElementById('avatar-upload')?.click()}
                        >
                          <FileImage className="h-3 w-3" />
                        </Button>
                        <input
                          id="avatar-upload"
                          type="file"
                          accept="image/*"
                          className="hidden"
                          onChange={handleAvatarUpload}
                        />
                      </div>
                      <div>
                        <Label className="font-medium">Ảnh đại diện</Label>
                        <p className="text-sm text-muted-foreground">
                          Tải lên ảnh đại diện cho người dùng (không bắt buộc)
                        </p>
                      </div>
                    </div>

                    <Separator />

                    {/* Name Fields */}
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      <FormField
                        control={form.control}
                        name="firstName"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Họ *</FormLabel>
                            <FormControl>
                              <Input placeholder="Nhập họ..." {...field} />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />

                      <FormField
                        control={form.control}
                        name="lastName"
                        render={({ field }) => (
                          <FormItem>
                            <FormLabel>Tên *</FormLabel>
                            <FormControl>
                              <Input placeholder="Nhập tên..." {...field} />
                            </FormControl>
                            <FormMessage />
                          </FormItem>
                        )}
                      />
                    </div>

                    {/* Contact Information */}
                    <FormField
                      control={form.control}
                      name="email"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel className="flex items-center gap-2">
                            <Mail className="h-4 w-4" />
                            Email *
                          </FormLabel>
                          <FormControl>
                            <Input
                              type="email"
                              placeholder="user@example.com"
                              {...field}
                            />
                          </FormControl>
                          <FormDescription>
                            Email sẽ được sử dụng để đăng nhập và nhận thông báo
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="phone"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel className="flex items-center gap-2">
                            <Phone className="h-4 w-4" />
                            Số điện thoại
                          </FormLabel>
                          <FormControl>
                            <Input
                              placeholder="0912345678"
                              {...field}
                            />
                          </FormControl>
                          <FormDescription>
                            Số điện thoại liên hệ (không bắt buộc)
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    {/* Bio */}
                    <FormField
                      control={form.control}
                      name="bio"
                      render={({ field }) => (
                        <FormItem>
                            <FormLabel>Giới thiệu</FormLabel>
                          <FormControl>
                            <Textarea
                              placeholder="Giới thiệu ngắn về người dùng..."
                              className="resize-none"
                              rows={3}
                              {...field}
                            />
                          </FormControl>
                          <FormDescription>
                            Mô tả ngắn về người dùng (không bắt buộc)
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </CardContent>
                </Card>

                {/* Security Settings */}
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Lock className="h-5 w-5" />
                      Cài đặt bảo mật
                    </CardTitle>
                    <CardDescription>
                      Mật khẩu và các thiết lập bảo mật
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-6">
                    <FormField
                      control={form.control}
                      name="password"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Mật khẩu *</FormLabel>
                          <FormControl>
                            <div className="relative">
                              <Input
                                type={showPassword ? 'text' : 'password'}
                                placeholder="Nhập mật khẩu..."
                                {...field}
                              />
                              <Button
                                type="button"
                                variant="ghost"
                                size="sm"
                                className="absolute right-0 top-0 h-full px-3"
                                onClick={() => setShowPassword(!showPassword)}
                              >
                                {showPassword ? (
                                  <EyeOff className="h-4 w-4" />
                                ) : (
                                  <Eye className="h-4 w-4" />
                                )}
                              </Button>
                            </div>
                          </FormControl>
                          <FormDescription>
                            Mật khẩu phải có ít nhất 8 ký tự, bao gồm chữ hoa, chữ thường và số
                          </FormDescription>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="confirmPassword"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Xác nhận mật khẩu *</FormLabel>
                          <FormControl>
                            <div className="relative">
                              <Input
                                type={showConfirmPassword ? 'text' : 'password'}
                                placeholder="Nhập lại mật khẩu..."
                                {...field}
                              />
                              <Button
                                type="button"
                                variant="ghost"
                                size="sm"
                                className="absolute right-0 top-0 h-full px-3"
                                onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                              >
                                {showConfirmPassword ? (
                                  <EyeOff className="h-4 w-4" />
                                ) : (
                                  <Eye className="h-4 w-4" />
                                )}
                              </Button>
                            </div>
                          </FormControl>
                          <FormMessage />
                        </FormItem>
                      )}
                    />
                  </CardContent>
                </Card>
              </div>

              {/* Sidebar */}
              <div className="space-y-6">
                {/* Role Selection */}
                <Card>
                  <CardHeader>
                    <CardTitle className="flex items-center gap-2">
                      <Shield className="h-5 w-5" />
                      Vai trò & Quyền hạn
                    </CardTitle>
                    <CardDescription>
                      Chọn vai trò và cấp quyền cho người dùng
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <FormField
                      control={form.control}
                      name="roleId"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Vai trò *</FormLabel>
                          <Select
                            onValueChange={(value) => field.onChange(parseInt(value))}
                            value={field.value?.toString() || ''}
                          >
                            <FormControl>
                              <SelectTrigger>
                                <SelectValue placeholder="Chọn vai trò..." />
                              </SelectTrigger>
                            </FormControl>
                            <SelectContent>
                              {rolesLoading ? (
                                <div className="p-2 text-center text-sm text-muted-foreground">
                                  Đang tải...
                                </div>
                              ) : (
                                roles
                                  ?.filter((role) => {
                                    const hasRolePerms = role.permissions.some((p) => p.startsWith('roles:'));
                                    if (hasRolePerms && !canAssignRolePermissions) {
                                      return false;
                                    }
                                    return true;
                                  })
                                  .map((role) => (
                                    <SelectItem key={role.id} value={role.id.toString()}>
                                      <div className="flex items-center gap-2">
                                        <Shield className="h-4 w-4" />
                                        {role.displayName}
                                      </div>
                                    </SelectItem>
                                  ))
                              )}
                            </SelectContent>
                          </Select>
                          <FormMessage />
                        </FormItem>
                      )}
                    />

                    {/* Role Preview */}
                    {selectedRole && (
                      <div className="p-4 border rounded-lg bg-muted/50">
                        <div className="space-y-3">
                          <div className="flex items-center justify-between">
                            {getRoleBadge(selectedRole)}
                            {selectedRole.isSystemRole && (
                              <Badge variant="outline" className="text-xs">
                                Hệ thống
                              </Badge>
                            )}
                          </div>

                          <div>
                            <p className="text-sm font-medium mb-1">Mô tả:</p>
                            <p className="text-sm text-muted-foreground">
                              {selectedRole.description}
                            </p>
                          </div>

                          <div>
                            <p className="text-sm font-medium mb-1">Quyền hạn:</p>
                            <p className="text-sm text-muted-foreground">
                              {getPermissionCount(selectedRole.permissions)}
                            </p>
                          </div>

                          {selectedRole.isSystemRole && (
                            <div className="flex items-start gap-2 p-2 bg-amber-50 border border-amber-200 rounded text-amber-800">
                              <AlertTriangle className="h-4 w-4 mt-0.5 flex-shrink-0" />
                              <p className="text-xs">
                                Vai trò hệ thống có quyền hạn cao, hãy cân nhắc kỹ trước khi gán.
                              </p>
                            </div>
                          )}
                        </div>
                      </div>
                    )}
                  </CardContent>
                </Card>

                {/* Account Settings */}
                <Card>
                  <CardHeader>
                    <CardTitle>Cài đặt tài khoản</CardTitle>
                    <CardDescription>
                      Trạng thái và các tùy chọn khác
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <FormField
                      control={form.control}
                      name="isActive"
                      render={({ field }) => (
                        <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4">
                          <div className="space-y-0.5">
                            <FormLabel className="text-base">
                              Tài khoản hoạt động
                            </FormLabel>
                            <FormDescription>
                              Cho phép người dùng đăng nhập và sử dụng hệ thống
                            </FormDescription>
                          </div>
                          <FormControl>
                            <Switch
                              checked={field.value}
                              onCheckedChange={field.onChange}
                            />
                          </FormControl>
                        </FormItem>
                      )}
                    />

                    <FormField
                      control={form.control}
                      name="sendWelcomeEmail"
                      render={({ field }) => (
                        <FormItem className="flex flex-row items-center justify-between rounded-lg border p-4">
                          <div className="space-y-0.5">
                            <FormLabel className="text-base">
                              Gửi email chào mừng
                            </FormLabel>
                            <FormDescription>
                              Gửi email chào mừng và hướng dẫn đăng nhập
                            </FormDescription>
                          </div>
                          <FormControl>
                            <Switch
                              checked={field.value}
                              onCheckedChange={field.onChange}
                            />
                          </FormControl>
                        </FormItem>
                      )}
                    />
                  </CardContent>
                </Card>

                {/* Action Buttons */}
                <Card>
                  <CardContent className="pt-6">
                    <div className="space-y-3">
                      <Button
                        type="submit"
                        className="w-full"
                        disabled={createUserMutation.isPending}
                      >
                        {createUserMutation.isPending ? (
                          <>Đang tạo...</>
                        ) : (
                          <>
                            <Save className="h-4 w-4 mr-2" />
                            Tạo người dùng
                          </>
                        )}
                      </Button>

                      <Button
                        type="button"
                        variant="outline"
                        className="w-full"
                        onClick={() => router.back()}
                        disabled={createUserMutation.isPending}
                      >
                        Hủy bỏ
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              </div>
            </div>
          </form>
        </Form>
      </div>
    </PermissionGuard>
  );
}