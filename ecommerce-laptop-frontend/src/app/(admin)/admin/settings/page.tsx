'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle
} from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { useAdminAuth } from '@/contexts/AdminAuthContext';
import { hasPermission, secureAdminBackendApi } from '@/lib/admin-api';
import {
  fetchAllSystemSettings,
  mapFromEmailNotifications,
  mapFromEmailRateLimitSettings,
  mapFromPaymentGateway,
  mapFromSMTPSettings,
  mapToEmailNotifications,
  mapToEmailRateLimitSettings,
  mapToPaymentGateways,
  mapToSMTPSettings,
  updateSystemSettingsBatch,
  type EmailNotificationSettings,
  type EmailRateLimitSettings,
  type PaymentGatewaySettings,
  type SMTPSettings
} from '@/lib/system-settings';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  AlertTriangle,
  Bell,
  CheckCircle,
  CreditCard,
  Database,
  Globe,
  Mail,
  RefreshCw,
  Save,
  Settings as SettingsIcon,
  Shield
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

// Types for settings
interface SystemSettings {
  id: string;
  category: string;
  key: string;
  value: string;
  description: string;
  type: 'string' | 'number' | 'boolean' | 'json';
  isEditable: boolean;
  requiresRestart: boolean;
}

// Interface definitions moved to system-settings.ts

// Real API function for system settings
const getSystemSettings = async (): Promise<SystemSettings[]> => {
  const data = await fetchAllSystemSettings();
  return data as SystemSettings[];
};

const getSMTPSettings = async (): Promise<SMTPSettings> => {
  const allSettings = await fetchAllSystemSettings();
  return mapToSMTPSettings(allSettings);
};

const getPaymentGateways = async (): Promise<PaymentGatewaySettings[]> => {
  const allSettings = await fetchAllSystemSettings();
  return mapToPaymentGateways(allSettings);
};

const getEmailNotificationSettings = async (): Promise<EmailNotificationSettings[]> => {
  const allSettings = await fetchAllSystemSettings();
  return mapToEmailNotifications(allSettings);
};

const getEmailRateLimitSettings = async (): Promise<EmailRateLimitSettings> => {
  const allSettings = await fetchAllSystemSettings();
  return mapToEmailRateLimitSettings(allSettings);
};

const updateSystemSettings = async (settings: Partial<SystemSettings>[]): Promise<void> => {
  // Map edited UI settings to backend DTOs (key, value, optional category/description)
  const payload = settings.map(s => ({
    key: s.key!,
    value: String(s.value ?? ''),
    category: s.category,
    description: s.description
  }));
  await updateSystemSettingsBatch(payload);
};

const updateSMTPSettings = async (settings: SMTPSettings): Promise<void> => {
  const payload = mapFromSMTPSettings(settings);
  await updateSystemSettingsBatch(payload);
};

const updatePaymentGateway = async (gateway: PaymentGatewaySettings): Promise<void> => {
  const payload = mapFromPaymentGateway(gateway);
  await updateSystemSettingsBatch(payload);
};

const updateEmailNotificationSettings = async (settings: EmailNotificationSettings[]): Promise<void> => {
  const payload = mapFromEmailNotifications(settings);
  await updateSystemSettingsBatch(payload);
};

const updateEmailRateLimitSettings = async (settings: EmailRateLimitSettings): Promise<void> => {
  const payload = mapFromEmailRateLimitSettings(settings);
  await updateSystemSettingsBatch(payload);
};

const testEmailNotification = async (eventType: string): Promise<boolean> => {
  // Mock API call
  await new Promise(resolve => setTimeout(resolve, 2000));
  return Math.random() > 0.2; // 80% success rate for demo
};

const testSMTPConnection = async (): Promise<boolean> => {
  // Mock API call
  await new Promise(resolve => setTimeout(resolve, 2000));
  return Math.random() > 0.3; // 70% success rate for demo
};

const testPaymentGateway = async (gatewayId: string): Promise<boolean> => {
  // Mock API call
  await new Promise(resolve => setTimeout(resolve, 2000));
  return Math.random() > 0.2; // 80% success rate for demo
};

export default function SettingsPage() {
  const { user } = useAdminAuth();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState('general');
  const [editedSettings, setEditedSettings] = useState<Record<string, string>>({});
  const [smtpSettings, setSMTPSettings] = useState<SMTPSettings | null>(null);
  const [emailNotifications, setEmailNotifications] = useState<EmailNotificationSettings[]>([]);
  const [emailRateLimitSettings, setEmailRateLimitSettings] = useState<EmailRateLimitSettings | null>(null);
  const [testingConnection, setTestingConnection] = useState<string | null>(null);

  // Permission checks (align with backend permission constants)
  // user.permissions is already an array of strings like 'settings:read'
  const userPermissions = (user?.permissions as string[]) || [];
  const canManageSettings =
    hasPermission(userPermissions, 'settings:read') ||
    hasPermission(userPermissions, 'settings:manage') ||
    hasPermission(userPermissions, 'settings:write');
  // Payments and security settings are part of system settings UI; gate by settings:manage or specific read perms
  const canManagePayments =
    hasPermission(userPermissions, 'settings:manage') ||
    hasPermission(userPermissions, 'settings:write');
  const canManageSecurity =
    hasPermission(userPermissions, 'security:read') ||
    hasPermission(userPermissions, 'security:manage') ||
    hasPermission(userPermissions, 'settings:manage');

  // Queries
  const { data: systemSettings, isLoading: settingsLoading } = useQuery({
    queryKey: ['system-settings'],
    queryFn: getSystemSettings,
    staleTime: 30 * 60 * 1000,
  });

  const { data: smtpData, isLoading: smtpLoading } = useQuery({
    queryKey: ['smtp-settings'],
    queryFn: getSMTPSettings,
    staleTime: 30 * 60 * 1000,
    enabled: canManageSettings
  });

  const { data: paymentGateways, isLoading: paymentsLoading } = useQuery({
    queryKey: ['payment-gateways'],
    queryFn: getPaymentGateways,
    staleTime: 30 * 60 * 1000,
    enabled: canManagePayments
  });

  const {
    data: emailNotificationsData,
    isLoading: emailNotificationsLoading,
    refetch: emailNotificationsRefetch
  } = useQuery({
    queryKey: ['email-notification-settings'],
    queryFn: getEmailNotificationSettings,
    staleTime: 30 * 60 * 1000,
    enabled: canManageSettings
  });

  const {
    data: emailRateLimitData,
    isLoading: emailRateLimitLoading,
    refetch: emailRateLimitRefetch
  } = useQuery({
    queryKey: ['email-rate-limit-settings'],
    queryFn: getEmailRateLimitSettings,
    staleTime: 30 * 60 * 1000,
    enabled: canManageSettings
  });

  // Initialize SMTP settings when data loads
  useEffect(() => {
    if (smtpData) {
      setSMTPSettings(smtpData);
    }
  }, [smtpData]);

  // Initialize email notifications when data loads
  useEffect(() => {
    if (emailNotificationsData) {
      setEmailNotifications(emailNotificationsData);
    }
  }, [emailNotificationsData]);

  // Initialize email rate limit settings when data loads
  useEffect(() => {
    if (emailRateLimitData) {
      setEmailRateLimitSettings(emailRateLimitData);
    }
  }, [emailRateLimitData]);

  // Mutations
  const settingsMutation = useMutation({
    mutationFn: updateSystemSettings,
    onSuccess: () => {
      toast.success('Cài đặt đã được cập nhật thành công');
      queryClient.invalidateQueries({ queryKey: ['system-settings'] });
      setEditedSettings({});
    },
    onError: () => {
      toast.error('Có lỗi xảy ra khi cập nhật cài đặt');
    }
  });

  const smtpMutation = useMutation({
    mutationFn: updateSMTPSettings,
    onSuccess: () => {
      toast.success('Cài đặt SMTP đã được cập nhật thành công');
      queryClient.invalidateQueries({ queryKey: ['smtp-settings'] });
    },
    onError: () => {
      toast.error('Có lỗi xảy ra khi cập nhật cài đặt SMTP');
    }
  });

  const paymentMutation = useMutation({
    mutationFn: updatePaymentGateway,
    onSuccess: () => {
      toast.success('Cài đặt cổng thanh toán đã được cập nhật');
      queryClient.invalidateQueries({ queryKey: ['payment-gateways'] });
    },
    onError: () => {
      toast.error('Có lỗi xảy ra khi cập nhật cổng thanh toán');
    }
  });

  const emailNotificationsMutation = useMutation({
    mutationFn: updateEmailNotificationSettings,
    onSuccess: () => {
      toast.success('Cài đặt email notifications đã được cập nhật');
      queryClient.invalidateQueries({ queryKey: ['email-notification-settings'] });
    },
    onError: () => {
      toast.error('Có lỗi xảy ra khi cập nhật email notifications');
    }
  });

  const emailRateLimitMutation = useMutation({
    mutationFn: updateEmailRateLimitSettings,
    onSuccess: () => {
      toast.success('Cài đặt rate limit email đã được cập nhật');
      queryClient.invalidateQueries({ queryKey: ['email-rate-limit-settings'] });
    },
    onError: () => {
      toast.error('Có lỗi xảy ra khi cập nhật rate limit email');
    }
  });

  // Handlers
  const handleSettingChange = (setting: SystemSettings, value: string) => {
    setEditedSettings(prev => ({
      ...prev,
      [setting.id]: value
    }));
  };

  const handleSaveSettings = () => {
    // Map edited values by id to payloads that include the backend key
    const updatedSettings = Object.entries(editedSettings).map(([id, value]) => {
      const original = systemSettings?.find(s => String(s.id) === String(id));
      return {
        key: original?.key,
        value: String(value),
        category: original?.category,
        description: original?.description,
      } as Partial<SystemSettings>;
    }).filter(x => x.key);

    settingsMutation.mutate(updatedSettings);
  };

  const handleSMTPChange = (field: keyof SMTPSettings, value: string | number | boolean) => {
    if (!smtpSettings) return;

    setSMTPSettings(prev => ({
      ...prev!,
      [field]: value
    }));
  };

  const handleSaveSMTP = () => {
    if (!smtpSettings) return;
    smtpMutation.mutate(smtpSettings);
  };

  const handleTestConnection = async (type: 'smtp' | 'payment' | 'email-notification', id?: string) => {
    setTestingConnection(type === 'smtp' ? 'smtp' : type === 'email-notification' ? `email-${id}` : id!);

    try {
      let success = false;
      if (type === 'smtp') {
        // Use secure client that handles cookie session and token fetching
        const resp = await secureAdminBackendApi.authenticatedRequest<{ success: boolean }>({
          method: 'POST',
          url: '/SystemSettings/test-email'
        });
        success = !!resp?.success;
      } else if (type === 'email-notification') {
        // Call dedicated notification test endpoint with event type
        const resp = await secureAdminBackendApi.authenticatedRequest<{ success: boolean }>({
          method: 'POST',
          url: '/SystemSettings/test-notification',
          data: { eventType: id || 'test_event' }
        });
        success = !!resp?.success;
      } else {
        success = await testPaymentGateway(id!);
      }

      if (success) {
        toast.success(`Kết nối ${type === 'smtp' ? 'SMTP' : type === 'email-notification' ? 'email notification' : 'cổng thanh toán'} thành công`);
      } else {
        toast.error(`Kết nối ${type === 'smtp' ? 'SMTP' : type === 'email-notification' ? 'email notification' : 'cổng thanh toán'} thất bại`);
      }
    } catch (error) {
      toast.error('Có lỗi xảy ra khi kiểm tra kết nối');
    } finally {
      setTestingConnection(null);
    }
  };

  const handleTogglePaymentGateway = (gateway: PaymentGatewaySettings) => {
    paymentMutation.mutate({
      ...gateway,
      isEnabled: !gateway.isEnabled
    });
  };

  const handleToggleEmailNotification = (notificationId: string) => {
    const updatedNotifications = emailNotifications.map(notification =>
      notification.id === notificationId
        ? { ...notification, isEnabled: !notification.isEnabled }
        : notification
    );
    setEmailNotifications(updatedNotifications);
    emailNotificationsMutation.mutate(updatedNotifications);
  };

  const handleUpdateEmailRecipients = (notificationId: string, recipients: EmailNotificationSettings['recipients']) => {
    const updatedNotifications = emailNotifications.map(notification =>
      notification.id === notificationId
        ? { ...notification, recipients }
        : notification
    );
    setEmailNotifications(updatedNotifications);
    emailNotificationsMutation.mutate(updatedNotifications);
  };

  const handleRateLimitChange = (field: keyof EmailRateLimitSettings, value: string | number | boolean) => {
    if (!emailRateLimitSettings) return;

    setEmailRateLimitSettings(prev => ({
      ...prev!,
      [field]: value
    }));
  };

  const handleSaveRateLimit = () => {
    if (!emailRateLimitSettings) return;
    emailRateLimitMutation.mutate(emailRateLimitSettings);
  };

  const getCategoryIcon = (category: string) => {
    switch (category) {
      case 'user':
        return '';
      case 'order':
        return '';
      case 'system':
        return '';
      case 'marketing':
        return '';
      default:
        return '';
    }
  };

  const getCategoryColor = (category: string) => {
    switch (category) {
      case 'user':
        return 'bg-blue-100 text-blue-800';
      case 'order':
        return 'bg-green-100 text-green-800';
      case 'system':
        return 'bg-red-100 text-red-800';
      case 'marketing':
        return 'bg-purple-100 text-purple-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (!canManageSettings) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="text-center">
          <Shield className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
          <h3 className="text-lg font-semibold">Không có quyền truy cập</h3>
          <p className="text-muted-foreground">
            Bạn không có quyền truy cập trang cài đặt hệ thống
          </p>
        </div>
      </div>
    );
  }

  const groupedSettings = systemSettings?.reduce((acc, setting) => {
    if (!acc[setting.category]) {
      acc[setting.category] = [];
    }
    acc[setting.category].push(setting);
    return acc;
  }, {} as Record<string, SystemSettings[]>) || {};

  const hasUnsavedChanges = Object.keys(editedSettings).length > 0;
  const hasRestartRequired = systemSettings?.some(s =>
    editedSettings[s.id] !== undefined && s.requiresRestart
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold">Cài đặt hệ thống</h1>
          <p className="text-muted-foreground">
            Quản lý cấu hình và tùy chọn hệ thống
          </p>
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
        <TabsList className="grid w-full grid-cols-5">
          <TabsTrigger value="general" className="space-x-2">
            <SettingsIcon className="h-4 w-4" />
            <span>Chung</span>
          </TabsTrigger>
          <TabsTrigger value="email" disabled={!canManageSettings}>
            <Mail className="h-4 w-4" />
            <span>Email</span>
          </TabsTrigger>
          <TabsTrigger value="payments" disabled={!canManagePayments}>
            <CreditCard className="h-4 w-4" />
            <span>Thanh toán</span>
          </TabsTrigger>
          <TabsTrigger value="security" disabled={!canManageSecurity}>
            <Shield className="h-4 w-4" />
            <span>Bảo mật</span>
          </TabsTrigger>
          <TabsTrigger value="notifications" disabled={!canManageSettings}>
            <Bell className="h-4 w-4" />
            <span>Thông báo</span>
          </TabsTrigger>
        </TabsList>

        {/* General Settings */}
        <TabsContent value="general" className="space-y-6">
          {settingsLoading ? (
            <div className="space-y-4">
              {[1, 2, 3].map(i => (
                <Card key={i}>
                  <CardContent className="p-6">
                    <div className="animate-pulse space-y-3">
                      <div className="h-4 bg-muted rounded w-1/4"></div>
                      <div className="h-10 bg-muted rounded"></div>
                      <div className="h-3 bg-muted rounded w-3/4"></div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          ) : (
            <>
              {Object.entries(groupedSettings).map(([category, settings]) => (
                <Card key={category}>
                  <CardHeader>
                    <CardTitle className="capitalize flex items-center space-x-2">
                      {category === 'general' && <Globe className="h-5 w-5" />}
                      {category === 'security' && <Shield className="h-5 w-5" />}
                      {category === 'database' && <Database className="h-5 w-5" />}
                      <span>
                        {category === 'general' ? 'Cài đặt chung' :
                          category === 'security' ? 'Bảo mật' :
                            category === 'database' ? 'Cơ sở dữ liệu' : category}
                      </span>
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-6">
                    {settings.map(setting => (
                      <div key={setting.id} className="space-y-2">
                        <div className="flex items-center justify-between">
                          <Label htmlFor={setting.key} className="text-sm font-medium">
                            {setting.key.replace(/_/g, ' ').replace(/\b\w/g, l => l.toUpperCase())}
                          </Label>
                          <div className="flex items-center space-x-2">
                            {setting.requiresRestart && (
                              <Badge variant="outline" className="text-xs">
                                <AlertTriangle className="h-3 w-3 mr-1" />
                                Cần khởi động lại
                              </Badge>
                            )}
                            {!setting.isEditable && (
                              <Badge variant="secondary" className="text-xs">
                                Ch c
                              </Badge>
                            )}
                          </div>
                        </div>

                        {setting.type === 'boolean' ? (
                          <div className="flex items-center space-x-2">
                            <Switch
                              id={setting.key}
                              checked={editedSettings[setting.id] !== undefined ?
                                editedSettings[setting.id] === 'true' : setting.value === 'true'}
                              onCheckedChange={(checked) =>
                                handleSettingChange(setting, checked.toString())}
                              disabled={!setting.isEditable}
                            />
                            <Label htmlFor={setting.key} className="text-sm text-muted-foreground">
                              {setting.value === 'true' ? 'Bật' : 'Tắt'}
                            </Label>
                          </div>
                        ) : setting.type === 'number' ? (
                          <Input
                            id={setting.key}
                            type="number"
                            value={editedSettings[setting.id] !== undefined ?
                              editedSettings[setting.id] : setting.value}
                            onChange={(e) => handleSettingChange(setting, e.target.value)}
                            disabled={!setting.isEditable}
                          />
                        ) : (
                          <Input
                            id={setting.key}
                            type={setting.key.includes('password') ? 'password' : 'text'}
                            value={editedSettings[setting.id] !== undefined ?
                              editedSettings[setting.id] : setting.value}
                            onChange={(e) => handleSettingChange(setting, e.target.value)}
                            disabled={!setting.isEditable}
                          />
                        )}

                        <p className="text-xs text-muted-foreground">
                          {setting.description}
                        </p>
                      </div>
                    ))}
                  </CardContent>
                </Card>
              ))}

              {hasUnsavedChanges && (
                <Card className="border-orange-200 bg-orange-50">
                  <CardContent className="p-4">
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-2">
                        <AlertTriangle className="h-5 w-5 text-orange-600" />
                        <div>
                          <p className="font-medium text-orange-800">
                            Có thay đổi chưa lưu
                          </p>
                          {hasRestartRequired && (
                            <p className="text-sm text-orange-600">
                              Một số thay đổi yêu cầu khởi động lại hệ thống
                            </p>
                          )}
                        </div>
                      </div>
                      <Button
                        onClick={handleSaveSettings}
                        disabled={settingsMutation.isPending}
                        size="sm"
                      >
                        {settingsMutation.isPending ? (
                          <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                        ) : (
                          <Save className="h-4 w-4 mr-2" />
                        )}
                        Lưu thay đổi
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              )}
            </>
          )}
        </TabsContent>

        {/* Email Settings */}
        <TabsContent value="email" className="space-y-6">
          {smtpLoading ? (
            <Card>
              <CardContent className="p-6">
                <div className="animate-pulse space-y-4">
                  {[1, 2, 3, 4].map(i => (
                    <div key={i} className="space-y-2">
                      <div className="h-4 bg-muted rounded w-1/4"></div>
                      <div className="h-10 bg-muted rounded"></div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          ) : (
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                  <Mail className="h-5 w-5" />
                  <span>Cài đặt SMTP</span>
                </CardTitle>
              </CardHeader>
              <CardContent className="space-y-6">
                {smtpSettings && (
                  <>
                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-2">
                        <Label htmlFor="smtp-host">SMTP Host</Label>
                        <Input
                          id="smtp-host"
                          value={smtpSettings.host}
                          onChange={(e) => handleSMTPChange('host', e.target.value)}
                        />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="smtp-port">Port</Label>
                        <Input
                          id="smtp-port"
                          type="number"
                          value={smtpSettings.port}
                          onChange={(e) => handleSMTPChange('port', parseInt(e.target.value))}
                        />
                      </div>
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-2">
                        <Label htmlFor="smtp-username">Username</Label>
                        <Input
                          id="smtp-username"
                          value={smtpSettings.username}
                          onChange={(e) => handleSMTPChange('username', e.target.value)}
                        />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="smtp-password">Password</Label>
                        <Input
                          id="smtp-password"
                          type="password"
                          value={smtpSettings.password}
                          onChange={(e) => handleSMTPChange('password', e.target.value)}
                        />
                      </div>
                    </div>

                    <div className="grid grid-cols-2 gap-4">
                      <div className="space-y-2">
                        <Label htmlFor="from-email">From Email</Label>
                        <Input
                          id="from-email"
                          type="email"
                          value={smtpSettings.fromEmail}
                          onChange={(e) => handleSMTPChange('fromEmail', e.target.value)}
                        />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor="from-name">From Name</Label>
                        <Input
                          id="from-name"
                          value={smtpSettings.fromName}
                          onChange={(e) => handleSMTPChange('fromName', e.target.value)}
                        />
                      </div>
                    </div>

                    <div className="flex items-center space-x-6">
                      <div className="flex items-center space-x-2">
                        <Switch
                          id="enable-ssl"
                          checked={smtpSettings.enableSSL}
                          onCheckedChange={(checked) => handleSMTPChange('enableSSL', checked)}
                        />
                        <Label htmlFor="enable-ssl">Enable SSL</Label>
                      </div>
                      <div className="flex items-center space-x-2">
                        <Switch
                          id="enable-tls"
                          checked={smtpSettings.enableTLS}
                          onCheckedChange={(checked) => handleSMTPChange('enableTLS', checked)}
                        />
                        <Label htmlFor="enable-tls">Enable TLS</Label>
                      </div>
                    </div>

                    <div className="flex items-center space-x-4 pt-4 border-t">
                      <Button
                        onClick={handleSaveSMTP}
                        disabled={smtpMutation.isPending}
                      >
                        {smtpMutation.isPending ? (
                          <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                        ) : (
                          <Save className="h-4 w-4 mr-2" />
                        )}
                        Lưu cài đặt
                      </Button>
                      <Button
                        variant="outline"
                        onClick={() => handleTestConnection('smtp')}
                        disabled={testingConnection === 'smtp'}
                      >
                        {testingConnection === 'smtp' ? (
                          <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                        ) : (
                          <CheckCircle className="h-4 w-4 mr-2" />
                        )}
                        Kiểm tra kết nối
                      </Button>
                    </div>
                  </>
                )}
              </CardContent>
            </Card>
          )}
        </TabsContent>

        {/* Payment Settings */}
        <TabsContent value="payments" className="space-y-6">
          {paymentsLoading ? (
            <div className="space-y-4">
              {[1, 2, 3].map(i => (
                <Card key={i}>
                  <CardContent className="p-6">
                    <div className="animate-pulse space-y-3">
                      <div className="h-6 bg-muted rounded w-1/3"></div>
                      <div className="h-4 bg-muted rounded w-2/3"></div>
                      <div className="h-10 bg-muted rounded"></div>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          ) : (
            <div className="space-y-6">
              {paymentGateways?.map(gateway => (
                <Card key={gateway.id}>
                  <CardHeader>
                    <div className="flex items-center justify-between">
                      <div className="flex items-center space-x-3">
                        <CreditCard className="h-5 w-5" />
                        <CardTitle>{gateway.name}</CardTitle>
                        <div className="flex items-center space-x-2">
                          <Badge variant={gateway.isEnabled ? "default" : "secondary"}>
                            {gateway.isEnabled ? 'đang hoạt động' : 'Tạm dừng'}
                          </Badge>
                          {gateway.testMode && (
                            <Badge variant="outline">Test Mode</Badge>
                          )}
                        </div>
                      </div>
                      <Switch
                        checked={gateway.isEnabled}
                        onCheckedChange={() => handleTogglePaymentGateway(gateway)}
                        disabled={paymentMutation.isPending}
                      />
                    </div>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <div className="grid grid-cols-2 gap-4">
                      {Object.entries(gateway.config).map(([key, value]) => (
                        <div key={key} className="space-y-2">
                          <Label htmlFor={`${gateway.id}-${key}`}>
                            {key.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase())}
                          </Label>
                          <Input
                            id={`${gateway.id}-${key}`}
                            type={key.toLowerCase().includes('secret') || key.toLowerCase().includes('key') ? 'password' : 'text'}
                            value={value}
                            readOnly
                            className="bg-muted"
                          />
                        </div>
                      ))}
                    </div>

                    <div className="flex items-center space-x-4 pt-4 border-t">
                      <Button
                        variant="outline"
                        onClick={() => handleTestConnection('payment', gateway.id)}
                        disabled={testingConnection === gateway.id || !gateway.isEnabled}
                      >
                        {testingConnection === gateway.id ? (
                          <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                        ) : (
                          <CheckCircle className="h-4 w-4 mr-2" />
                        )}
                        Kiểm tra kết nối
                      </Button>
                    </div>
                  </CardContent>
                </Card>
              ))}
            </div>
          )}
        </TabsContent>

        {/* Security Settings */}
        <TabsContent value="security" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center space-x-2">
                <Shield className="h-5 w-5" />
                <span>Cài đặt bảo mật</span>
              </CardTitle>
            </CardHeader>
            <CardContent>
              <p className="text-muted-foreground">
                Các cài đặt bảo mật được quản lý trong tab &quot;Chung&quot; ở trên.
              </p>
            </CardContent>
          </Card>
        </TabsContent>

        {/* Email Notifications */}
        <TabsContent value="notifications" className="space-y-6">
          <Card>
            <CardHeader>
              <CardTitle className="flex items-center space-x-2">
                <Bell className="h-5 w-5" />
                <span>Cài đặt thông báo email</span>
              </CardTitle>
            </CardHeader>
            <CardContent className="space-y-6">
              {emailNotificationsLoading ? (
                <div className="flex items-center justify-center py-8">
                  <RefreshCw className="h-6 w-6 animate-spin" />
                  <span className="ml-2">Đang tải cài đặt thông báo...</span>
                </div>
              ) : (
                <div className="space-y-8">
                  {/* Group notifications by category */}
                  {['user', 'order', 'system', 'marketing'].map((category) => {
                    const categoryNotifications = emailNotifications.filter(n => n.category === category);
                    if (categoryNotifications.length === 0) return null;

                    return (
                      <div key={category} className="space-y-4">
                        <h3 className="text-lg font-semibold flex items-center space-x-2">
                          <span className={`px-2 py-1 rounded-full text-sm ${getCategoryColor(category)}`}>
                            {getCategoryIcon(category)}
                          </span>
                          <span>
                            {category === 'user' && 'Thông báo người dùng'}
                            {category === 'order' && 'Thông báo đơn hàng'}
                            {category === 'system' && 'Thông báo hệ thống'}
                            {category === 'marketing' && 'Thông báo marketing'}
                          </span>
                        </h3>

                        <div className="grid gap-4">
                          {categoryNotifications.map((notification) => (
                            <Card key={notification.id} className="p-4">
                              <div className="flex items-start justify-between">
                                <div className="space-y-2 flex-1">
                                  <div className="flex items-center space-x-2">
                                    <h4 className="font-medium">{notification.eventName}</h4>
                                    <Switch
                                      checked={notification.isEnabled}
                                      onCheckedChange={() => handleToggleEmailNotification(notification.id)}
                                    />
                                  </div>
                                  <p className="text-sm text-muted-foreground">
                                    {notification.description}
                                  </p>

                                  {notification.isEnabled && (
                                    <div className="space-y-2 pt-2">
                                      <Label className="text-sm font-medium">Người nhận:</Label>
                                      <div className="flex flex-wrap gap-2">
                                        {notification.recipients.toUser && (
                                          <Badge variant="secondary" className="text-xs">
                                            Người dùng
                                          </Badge>
                                        )}
                                        {notification.recipients.toAdmin && (
                                          <Badge variant="secondary" className="text-xs">
                                            Quản trị viên
                                          </Badge>
                                        )}
                                        {notification.recipients.toCustomEmails.map((email, index) => (
                                          <Badge
                                            key={index}
                                            variant="secondary"
                                            className="text-xs"
                                          >
                                            {email}
                                          </Badge>
                                        ))}
                                      </div>

                                      <div className="flex space-x-2 pt-2">
                                        <Button
                                          size="sm"
                                          variant="outline"
                                          onClick={() => handleTestConnection('email-notification', notification.eventType)}
                                        >
                                          Gửi thử
                                        </Button>
                                      </div>
                                    </div>
                                  )}
                                </div>
                              </div>
                            </Card>
                          ))}
                        </div>
                      </div>
                    );
                  })}

                  {/* Global notification settings */}
                  <Card className="mt-8">
                    <CardHeader>
                      <CardTitle className="text-base">Cài đặt chung</CardTitle>
                    </CardHeader>
                    <CardContent className="space-y-4">
                      <div className="flex items-center justify-between">
                        <div>
                          <Label className="font-medium">Bật tất cả thông báo</Label>
                          <p className="text-sm text-muted-foreground">
                            Bật/tắt tất cả thông báo email cùng lúc
                          </p>
                        </div>
                        <Switch
                          checked={emailNotifications.length > 0 && emailNotifications.every(n => n.isEnabled)}
                          onCheckedChange={(checked) => {
                            // Update per-event toggles in UI
                            const updatedNotifications = emailNotifications.map(n => ({ ...n, isEnabled: checked }));
                            setEmailNotifications(updatedNotifications);
                            emailNotificationsMutation.mutate(updatedNotifications);
                            // Also persist master switch to backend using batch update
                            const payload = [{ key: 'email_notifications_enabled', value: String(checked), category: 'Notifications' }];
                            updateSystemSettingsBatch(payload).catch(() => {/* ignore toast here; UI already shows errors via mutation if needed */ });
                          }}
                        />
                      </div>

                      <div className="flex space-x-2 pt-4">
                        <Button
                          onClick={() => emailNotificationsRefetch()}
                          variant="outline"
                          disabled={emailNotificationsLoading}
                        >
                          <RefreshCw className={`h-4 w-4 mr-2 ${emailNotificationsLoading ? 'animate-spin' : ''}`} />
                          Làm mới
                        </Button>
                        <Button
                          onClick={() => handleTestConnection('email-notification', 'test_all')}
                          variant="outline"
                        >
                          <Mail className="h-4 w-4 mr-2" />
                          Kiểm tra tất cả
                        </Button>
                      </div>
                    </CardContent>
                  </Card>

                  {/* Email Rate Limit Settings */}
                  <Card className="mt-8">
                    <CardHeader>
                      <CardTitle className="text-base flex items-center space-x-2">
                        <Shield className="h-5 w-5" />
                        <span>Cài đặt Rate Limit Email</span>
                      </CardTitle>
                      <p className="text-sm text-muted-foreground">
                        Cấu hình giới hạn số lượng email gửi để tránh spam và bảo vệ hệ thống
                      </p>
                    </CardHeader>
                    <CardContent className="space-y-6">
                      {emailRateLimitLoading ? (
                        <div className="flex items-center justify-center py-8">
                          <RefreshCw className="h-6 w-6 animate-spin" />
                          <span className="ml-2">Đang tải cài đặt rate limit...</span>
                        </div>
                      ) : emailRateLimitSettings ? (
                        <>
                          <div className="grid grid-cols-2 gap-6">
                            <div className="space-y-2">
                              <Label htmlFor="max-requests">Số email tối đa</Label>
                              <Input
                                id="max-requests"
                                type="number"
                                min="1"
                                max="100"
                                value={emailRateLimitSettings.maxRequests}
                                onChange={(e) => handleRateLimitChange('maxRequests', parseInt(e.target.value) || 5)}
                              />
                              <p className="text-xs text-muted-foreground">
                                Số lượng email tối đa được phép gửi trong khoảng thời gian
                              </p>
                            </div>

                            <div className="space-y-2">
                              <Label htmlFor="window-seconds">Khoảng thời gian (giây)</Label>
                              <Input
                                id="window-seconds"
                                type="number"
                                min="60"
                                max="3600"
                                value={emailRateLimitSettings.windowSeconds}
                                onChange={(e) => handleRateLimitChange('windowSeconds', parseInt(e.target.value) || 900)}
                              />
                              <p className="text-xs text-muted-foreground">
                                Khoảng thời gian tính bằng giây (60-3600)
                              </p>
                            </div>
                          </div>

                          <div className="grid grid-cols-2 gap-6">
                            <div className="space-y-2">
                              <Label htmlFor="cooldown-minutes">Thời gian chờ (phút)</Label>
                              <Input
                                id="cooldown-minutes"
                                type="number"
                                min="1"
                                max="1440"
                                value={emailRateLimitSettings.cooldownMinutes}
                                onChange={(e) => handleRateLimitChange('cooldownMinutes', parseInt(e.target.value) || 60)}
                              />
                              <p className="text-xs text-muted-foreground">
                                Thời gian chờ khi vượt quá rate limit (1-1440 phút)
                              </p>
                            </div>

                            <div className="space-y-2">
                              <div className="flex items-center space-x-2">
                                <Switch
                                  id="enable-rate-limiting"
                                  checked={emailRateLimitSettings.enableRateLimiting}
                                  onCheckedChange={(checked) => handleRateLimitChange('enableRateLimiting', checked)}
                                />
                                <Label htmlFor="enable-rate-limiting">Bật Rate Limiting</Label>
                              </div>
                              <p className="text-xs text-muted-foreground">
                                Bật/tắt tính năng giới hạn số lượng email
                              </p>
                            </div>
                          </div>

                          <div className="flex items-center space-x-4 pt-4 border-t">
                            <Button
                              onClick={handleSaveRateLimit}
                              disabled={emailRateLimitMutation.isPending}
                            >
                              {emailRateLimitMutation.isPending ? (
                                <RefreshCw className="h-4 w-4 mr-2 animate-spin" />
                              ) : (
                                <Save className="h-4 w-4 mr-2" />
                              )}
                              Lưu cài đặt Rate Limit
                            </Button>
                            <Button
                              onClick={() => emailRateLimitRefetch()}
                              variant="outline"
                              disabled={emailRateLimitLoading}
                            >
                              <RefreshCw className={`h-4 w-4 mr-2 ${emailRateLimitLoading ? 'animate-spin' : ''}`} />
                              Làm mới
                            </Button>
                          </div>

                          {/* Rate Limit Info */}
                          <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                            <div className="flex items-start space-x-2">
                              <AlertTriangle className="h-5 w-5 text-blue-600 mt-0.5" />
                              <div className="space-y-1">
                                <h4 className="text-sm font-medium text-blue-800">Thông tin Rate Limit hiện tại</h4>
                                <div className="text-sm text-blue-700 space-y-1">
                                  <p>Tối đa <strong>{emailRateLimitSettings.maxRequests}</strong> email trong <strong>{Math.floor(emailRateLimitSettings.windowSeconds / 60)} phút</strong></p>
                                  <p>Thời gian chờ: <strong>{emailRateLimitSettings.cooldownMinutes} phút</strong> khi vượt quá giới hạn</p>
                                  <p>Trạng thái: <strong>{emailRateLimitSettings.enableRateLimiting ? 'Đang bật' : 'Đang tắt'}</strong></p>
                                </div>
                              </div>
                            </div>
                          </div>
                        </>
                      ) : (
                        <div className="text-center py-8 text-muted-foreground">
                          Không thể tải cài đặt rate limit
                        </div>
                      )}
                    </CardContent>
                  </Card>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}
