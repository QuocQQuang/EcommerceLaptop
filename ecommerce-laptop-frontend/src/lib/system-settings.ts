import { tokenManager } from '@/lib/admin-api';
import axios from 'axios';

// UI-facing shape used by the admin settings page
export type UiSettingType = 'string' | 'number' | 'boolean' | 'json';

export interface UiSystemSetting {
  id: string | number;
  category: string;
  key: string;
  value: string;
  description: string;
  type: UiSettingType;
  isEditable: boolean;
  requiresRestart: boolean;
}

// Backend entity shape (inferred from .NET model)
interface BackendSystemSetting {
  id: number;
  category: string;
  settingKey: string;
  settingValue?: string | null;
  dataType?: string; // string | int | boolean | json
  isEncrypted?: boolean;
  description?: string | null;
  createdAt?: string;
  updatedAt?: string;
}

// Response of GET /api/SystemSettings => Dictionary<string, List<SystemSetting>>
type BackendGroupedSettings = Record<string, BackendSystemSetting[]>;

function mapDataTypeToUiType(dt?: string): UiSettingType {
  switch ((dt || 'string').toLowerCase()) {
    case 'boolean':
      return 'boolean';
    case 'int':
    case 'number':
      return 'number';
    case 'json':
      return 'json';
    default:
      return 'string';
  }
}

// GET all settings (flattened array for UI)
export async function fetchAllSystemSettings(): Promise<UiSystemSetting[]> {
  const apiBase = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5129/api';

  // Ensure we always have a token (fallback to cookie-based endpoint if memory token is missing)
  let token = tokenManager.getAccessToken();
  if (!token && typeof window !== 'undefined') {
    try {
      const resp = await fetch('/api/admin/token', { credentials: 'include' });
      if (resp.ok) {
        const data = await resp.json();
        if (data?.success && data?.token) {
          token = data.token as string;
          tokenManager.setAccessToken(token);
        }
      }
    } catch {
      // ignore; will proceed without auth header (backend will 401), then caller UI can handle
    }
  }

  const doRequest = async (): Promise<BackendGroupedSettings> => {
    const { data } = await axios.get<BackendGroupedSettings>(`${apiBase}/SystemSettings`, {
      headers: token ? { Authorization: `Bearer ${token}` } : undefined,
    });
    return data;
  };

  try {
    const data = await doRequest();
    const flattened: UiSystemSetting[] = [];
    for (const [category, list] of Object.entries(data || {})) {
      for (const item of list) {
        flattened.push({
          id: String(item.id),
          category: category || item.category || 'General',
          key: item.settingKey,
          value: item.settingValue ?? '',
          description: item.description ?? '',
          type: mapDataTypeToUiType(item.dataType),
          // Until backend provides flags, default to editable and no restart required
          isEditable: true,
          requiresRestart: false,
        });
      }
    }
    return flattened;
  } catch (err: any) {
    // Retry once if unauthorized by reloading token from cookie
    if ((err?.response?.status === 401 || err?.response?.status === 403) && typeof window !== 'undefined') {
      try {
        const resp = await fetch('/api/admin/token', { credentials: 'include' });
        if (resp.ok) {
          const t = await resp.json();
          if (t?.success && t?.token) {
            token = t.token as string;
            tokenManager.setAccessToken(token);
            const { data } = await axios.get<BackendGroupedSettings>(`${apiBase}/SystemSettings`, {
              headers: { Authorization: `Bearer ${token}` },
            });
            const flattened: UiSystemSetting[] = [];
            for (const [category, list] of Object.entries(data || {})) {
              for (const item of list) {
                flattened.push({
                  id: String(item.id),
                  category: category || item.category || 'General',
                  key: item.settingKey,
                  value: item.settingValue ?? '',
                  description: item.description ?? '',
                  type: mapDataTypeToUiType(item.dataType),
                  isEditable: true,
                  requiresRestart: false,
                });
              }
            }
            return flattened;
          }
        }
      } catch {
        // fall through to throw original error
      }
    }
    throw err;
  }
}

// PUT batch update to /api/SystemSettings/batch
export interface UpdateSettingDto {
  key: string;
  value: string;
  category?: string;
  description?: string | null;
}

export async function updateSystemSettingsBatch(updates: UpdateSettingDto[]): Promise<void> {
  // Backend expects: List<SystemSettingUpdateRequest> { key, value, category, description }
  const apiBase = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5129/api';
  let token = tokenManager.getAccessToken();
  if (!token && typeof window !== 'undefined') {
    try {
      const resp = await fetch('/api/admin/token', { credentials: 'include' });
      if (resp.ok) {
        const data = await resp.json();
        if (data?.success && data?.token) {
          token = data.token as string;
          tokenManager.setAccessToken(token);
        }
      }
    } catch { }
  }
  await axios.put(`${apiBase}/SystemSettings/batch`, updates, {
    headers: token ? { Authorization: `Bearer ${token}` } : undefined,
  });
}

// Helper functions to map SystemSettings to UI-specific shapes
export function mapToSMTPSettings(settings: UiSystemSetting[]): SMTPSettings {
  const getSettingValue = (key: string, defaultValue: any) => {
    const setting = settings.find(s => s.key === key);
    return setting?.value ?? defaultValue;
  };

  return {
    host: getSettingValue('smtp_host', 'smtp.gmail.com'),
    port: parseInt(getSettingValue('smtp_port', '587')),
    username: getSettingValue('smtp_username', ''),
    password: getSettingValue('smtp_password', ''),
    fromEmail: getSettingValue('smtp_from_email', ''),
    fromName: getSettingValue('smtp_from_name', ''),
    enableSSL: getSettingValue('smtp_enable_ssl', 'false') === 'true',
    enableTLS: getSettingValue('smtp_enable_tls', 'true') === 'true',
  };
}

export function mapFromSMTPSettings(smtp: SMTPSettings): UpdateSettingDto[] {
  return [
    { key: 'smtp_host', value: smtp.host, category: 'Email' },
    { key: 'smtp_port', value: String(smtp.port), category: 'Email' },
    { key: 'smtp_username', value: smtp.username, category: 'Email' },
    { key: 'smtp_password', value: smtp.password, category: 'Email' },
    { key: 'smtp_from_email', value: smtp.fromEmail, category: 'Email' },
    { key: 'smtp_from_name', value: smtp.fromName, category: 'Email' },
    { key: 'smtp_enable_ssl', value: String(smtp.enableSSL), category: 'Email' },
    { key: 'smtp_enable_tls', value: String(smtp.enableTLS), category: 'Email' },
  ];
}

export interface PaymentGatewaySettings {
  id: string;
  name: string;
  isEnabled: boolean;
  config: Record<string, string>;
  testMode: boolean;
}

export function mapToPaymentGateways(settings: UiSystemSetting[]): PaymentGatewaySettings[] {
  const getSettingValue = (key: string, defaultValue: any) => {
    const setting = settings.find(s => s.key === key);
    return setting?.value ?? defaultValue;
  };

  return [
    {
      id: 'vnpay',
      name: 'VNPay',
      isEnabled: getSettingValue('vnpay_enabled', 'false') === 'true',
      testMode: getSettingValue('vnpay_test_mode', 'true') === 'true',
      config: {
        tmnCode: getSettingValue('vnpay_tmn_code', ''),
        hashSecret: getSettingValue('vnpay_hash_secret', ''),
        url: getSettingValue('vnpay_url', 'https://sandbox-web.vnpay.vn'),
      }
    },
    {
      id: 'zalopay',
      name: 'ZaloPay',
      isEnabled: getSettingValue('zalopay_enabled', 'false') === 'true',
      testMode: getSettingValue('zalopay_test_mode', 'true') === 'true',
      config: {
        appId: getSettingValue('zalopay_app_id', ''),
        key1: getSettingValue('zalopay_key1', ''),
        key2: getSettingValue('zalopay_key2', ''),
        endpoint: getSettingValue('zalopay_endpoint', 'https://sb-openapi.zalopay.vn'),
      }
    },
    {
      id: 'momo',
      name: 'MoMo',
      isEnabled: getSettingValue('momo_enabled', 'false') === 'true',
      testMode: getSettingValue('momo_test_mode', 'true') === 'true',
      config: {
        partnerCode: getSettingValue('momo_partner_code', ''),
        accessKey: getSettingValue('momo_access_key', ''),
        secretKey: getSettingValue('momo_secret_key', ''),
        endpoint: getSettingValue('momo_endpoint', 'https://test-payment.momo.vn'),
      }
    }
  ];
}

export function mapFromPaymentGateway(gateway: PaymentGatewaySettings): UpdateSettingDto[] {
  const prefix = gateway.id;
  const updates: UpdateSettingDto[] = [
    { key: `${prefix}_enabled`, value: String(gateway.isEnabled), category: 'Payments' },
    { key: `${prefix}_test_mode`, value: String(gateway.testMode), category: 'Payments' },
  ];

  // Add config entries
  Object.entries(gateway.config).forEach(([configKey, configValue]) => {
    updates.push({
      key: `${prefix}_${configKey.toLowerCase()}`,
      value: configValue,
      category: 'Payments'
    });
  });

  return updates;
}

export interface EmailNotificationSettings {
  id: string;
  eventType: string;
  eventName: string;
  description: string;
  category: 'user' | 'order' | 'system' | 'marketing';
  isEnabled: boolean;
  recipients: {
    toUser: boolean;
    toAdmin: boolean;
    toCustomEmails: string[];
  };
  template: {
    subject: string;
    bodyTemplate: string;
  };
}

export function mapToEmailNotifications(settings: UiSystemSetting[]): EmailNotificationSettings[] {
  // Read master switch (default true if not set)
  const master = settings.find(s => s.key === 'email_notifications_enabled')?.value;
  const masterEnabled = master === undefined ? true : master === 'true';

  const defaultNotifications: EmailNotificationSettings[] = [
    {
      id: 'user_registered',
      eventType: 'user_registered',
      eventName: 'ng k ti khon',
      description: 'Gi email cho mng khi ngi dng ng k ti khon mi',
      category: 'user',
      isEnabled: masterEnabled && (settings.find(s => s.key === 'email_notification_user_registered')?.value === 'true'),
      recipients: {
        toUser: true,
        toAdmin: false,
        toCustomEmails: []
      },
      template: {
        subject: 'Cho mng bn n vi Laptop',
        bodyTemplate: 'welcome_user_template'
      }
    },
    {
      id: 'order_created',
      eventType: 'order_created',
      eventName: 'n hng mi',
      description: 'Thng bo khi c n hng mi c to',
      category: 'order',
      isEnabled: masterEnabled && (settings.find(s => s.key === 'email_notification_order_created')?.value === 'true'),
      recipients: {
        toUser: true,
        toAdmin: true,
        toCustomEmails: ['sales@company.com']
      },
      template: {
        subject: 'Xc nhn n hng #{orderId}',
        bodyTemplate: 'order_confirmation_template'
      }
    },
    {
      id: 'order_paid',
      eventType: 'order_paid',
      eventName: 'Thanh ton thnh cng',
      description: 'Thng bo khi n hng c thanh ton thnh cng',
      category: 'order',
      isEnabled: masterEnabled && (settings.find(s => s.key === 'email_notification_order_paid')?.value === 'true'),
      recipients: {
        toUser: true,
        toAdmin: true,
        toCustomEmails: ['accounting@company.com']
      },
      template: {
        subject: 'Thanh ton n hng #{orderId} thnh cng',
        bodyTemplate: 'payment_success_template'
      }
    }
  ];

  return defaultNotifications;
}

export function mapFromEmailNotifications(notifications: EmailNotificationSettings[]): UpdateSettingDto[] {
  return notifications.map(notif => ({
    key: `email_notification_${notif.eventType}`,
    value: String(notif.isEnabled),
    category: 'Notifications'
  }));
}

// UI shape interfaces (moved from page.tsx for reuse)
export interface SMTPSettings {
  host: string;
  port: number;
  username: string;
  password: string;
  fromEmail: string;
  fromName: string;
  enableSSL: boolean;
  enableTLS: boolean;
}

// Email Rate Limit Settings
export interface EmailRateLimitSettings {
  maxRequests: number;
  windowSeconds: number;
  enableRateLimiting: boolean;
  cooldownMinutes: number;
}

export function mapToEmailRateLimitSettings(settings: UiSystemSetting[]): EmailRateLimitSettings {
  const getSettingValue = (key: string, defaultValue: any) => {
    const setting = settings.find(s => s.key === key);
    return setting?.value ?? defaultValue;
  };

  return {
    maxRequests: parseInt(getSettingValue('email_rate_limit_max_requests', '5')),
    windowSeconds: parseInt(getSettingValue('email_rate_limit_window_seconds', '900')), // 15 minutes
    enableRateLimiting: getSettingValue('email_rate_limit_enabled', 'true') === 'true',
    cooldownMinutes: parseInt(getSettingValue('email_rate_limit_cooldown_minutes', '60'))
  };
}

export function mapFromEmailRateLimitSettings(rateLimit: EmailRateLimitSettings): UpdateSettingDto[] {
  return [
    {
      key: 'email_rate_limit_max_requests',
      value: String(rateLimit.maxRequests),
      category: 'Notifications',
      description: 'S lng email ti a c php gi trong khong thi gian'
    },
    {
      key: 'email_rate_limit_window_seconds',
      value: String(rateLimit.windowSeconds),
      category: 'Notifications',
      description: 'Khong thi gian tnh bng giy  p dng rate limit'
    },
    {
      key: 'email_rate_limit_enabled',
      value: String(rateLimit.enableRateLimiting),
      category: 'Notifications',
      description: 'Bt/tt tnh nng rate limiting cho email'
    },
    {
      key: 'email_rate_limit_cooldown_minutes',
      value: String(rateLimit.cooldownMinutes),
      category: 'Notifications',
      description: 'Thi gian ch (cooldown) tnh bng pht khi vt qu rate limit'
    }
  ];
}