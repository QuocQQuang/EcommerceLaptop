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
    .min(1, 'H l bt buc')
    .min(2, 'H phi c t nht 2 k t')
    .max(50, 'H khng c qu 50 k t'),
  lastName: z.string()
    .min(1, 'Tn l bt buc')
    .min(2, 'Tn phi c t nht 2 k t')
    .max(50, 'Tn khng c qu 50 k t'),
  email: z.string()
    .min(1, 'Email l bt buc')
    .email('Email khng hp l')
    .max(100, 'Email khng c qu 100 k t'),
  phone: z.string()
    .optional()
    .refine((val) => !val || /^[0-9]{10,11}$/.test(val), {
      message: 'S in thoi phi c 10-11 ch s'
    }),
  password: z.string()
    .min(8, 'Mt khu phi c t nht 8 k t')
    .regex(/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)/, 'Mt khu phi c t nht 1 ch hoa, 1 ch thng v 1 s'),
  confirmPassword: z.string()
    .min(1, 'Xc nhn mt khu l bt buc'),
  roleId: z.number()
    .min(1, 'Vui lng chn vai tr'),
  isActive: z.boolean(),
  bio: z.string().optional(),
  sendWelcomeEmail: z.boolean(),
}).refine((data) => data.password === data.confirmPassword, {
  message: 'Mt khu xc nhn khng khp',
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
          description: 'Quyn cao nht, c th truy cp tt c chc nng',
          permissions: ['*'],
          isSystemRole: true
        },
        {
          id: 2,
          name: 'manager',
          displayName: 'Qun l',
          description: 'Qun l n hng, sn phm v nhn vin',
          permissions: ['orders:read', 'orders:write', 'products:read', 'products:write', 'users:read'],
          isSystemRole: false
        },
        {
          id: 3,
          name: 'staff',
          displayName: 'Nhn vin',
          description: 'Xem n hng v sn phm, khng c quyn chnh sa',
          permissions: ['orders:read', 'products:read'],
          isSystemRole: false
        },
        {
          id: 4,
          name: 'content_manager',
          displayName: 'Qun l ni dung',
          description: 'Qun l blog, tin tc v ni dung website',
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
  if (permissions.includes('*')) return 'Tt c quyn';
  return `${permissions.length} quyn`;
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
      toast.success('To ngi dng thnh cng!');
      router.push('/users');
    } catch (error) {
      toast.error('C li xy ra khi to ngi dng');
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
            Quay li
          </Button>

          <div>
            <h1 className="text-3xl font-bold">Thm ngi dng mi</h1>
            <p className="text-muted-foreground">
              To ti khon admin mi cho h thng
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
                      Thng tin c bn
                    </CardTitle>
                    <CardDescription>
                      Thng tin c nhn ca ngi dng
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
                        <Label className="font-medium">nh i din</Label>
                        <p className="text-sm text-muted-foreground">
                          Ti ln nh i din cho ngi dng (khng bt buc)
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
                            <FormLabel>H *</FormLabel>
                            <FormControl>
                              <Input placeholder="Nhp h..." {...field} />
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
                            <FormLabel>Tn *</FormLabel>
                            <FormControl>
                              <Input placeholder="Nhp tn..." {...field} />
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
                            Email s c s dng  ng nhp v nhn thng bo
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
                            S in thoi
                          </FormLabel>
                          <FormControl>
                            <Input
                              placeholder="0912345678"
                              {...field}
                            />
                          </FormControl>
                          <FormDescription>
                            S in thoi lin h (khng bt buc)
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
                          <FormLabel>Gii thiu</FormLabel>
                          <FormControl>
                            <Textarea
                              placeholder="Gii thiu ngn v ngi dng..."
                              className="resize-none"
                              rows={3}
                              {...field}
                            />
                          </FormControl>
                          <FormDescription>
                            M t ngn v ngi dng (khng bt buc)
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
                      Ci t bo mt
                    </CardTitle>
                    <CardDescription>
                      Mt khu v cc thit lp bo mt
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-6">
                    <FormField
                      control={form.control}
                      name="password"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Mt khu *</FormLabel>
                          <FormControl>
                            <div className="relative">
                              <Input
                                type={showPassword ? 'text' : 'password'}
                                placeholder="Nhp mt khu..."
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
                            Mt khu phi c t nht 8 k t, bao gm ch hoa, ch thng v s
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
                          <FormLabel>Xc nhn mt khu *</FormLabel>
                          <FormControl>
                            <div className="relative">
                              <Input
                                type={showConfirmPassword ? 'text' : 'password'}
                                placeholder="Nhp li mt khu..."
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
                      Vai tr & Quyn hn
                    </CardTitle>
                    <CardDescription>
                      Chn vai tr v cp quyn cho ngi dng
                    </CardDescription>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <FormField
                      control={form.control}
                      name="roleId"
                      render={({ field }) => (
                        <FormItem>
                          <FormLabel>Vai tr *</FormLabel>
                          <Select
                            onValueChange={(value) => field.onChange(parseInt(value))}
                            value={field.value?.toString() || ''}
                          >
                            <FormControl>
                              <SelectTrigger>
                                <SelectValue placeholder="Chn vai tr..." />
                              </SelectTrigger>
                            </FormControl>
                            <SelectContent>
                              {rolesLoading ? (
                                <div className="p-2 text-center text-sm text-muted-foreground">
                                  ang ti...
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
                                H thng
                              </Badge>
                            )}
                          </div>

                          <div>
                            <p className="text-sm font-medium mb-1">M t:</p>
                            <p className="text-sm text-muted-foreground">
                              {selectedRole.description}
                            </p>
                          </div>

                          <div>
                            <p className="text-sm font-medium mb-1">Quyn hn:</p>
                            <p className="text-sm text-muted-foreground">
                              {getPermissionCount(selectedRole.permissions)}
                            </p>
                          </div>

                          {selectedRole.isSystemRole && (
                            <div className="flex items-start gap-2 p-2 bg-amber-50 border border-amber-200 rounded text-amber-800">
                              <AlertTriangle className="h-4 w-4 mt-0.5 flex-shrink-0" />
                              <p className="text-xs">
                                Vai tr h thng c quyn hn cao, hy cn nhc k trc khi gn.
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
                    <CardTitle>Ci t ti khon</CardTitle>
                    <CardDescription>
                      Trng thi v cc ty chn khc
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
                              Ti khon hot ng
                            </FormLabel>
                            <FormDescription>
                              Cho php ngi dng ng nhp v s dng h thng
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
                              Gi email cho mng
                            </FormLabel>
                            <FormDescription>
                              Gi email cho mng v hng dn ng nhp
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
                          <>ang to...</>
                        ) : (
                          <>
                            <Save className="h-4 w-4 mr-2" />
                            To ngi dng
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
                        Hy b
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