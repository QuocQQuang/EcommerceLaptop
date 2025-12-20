'use client';

import { ExportButtons } from '@/components/admin/ExportButtons';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Card,
  CardContent,
  CardHeader,
  CardTitle
} from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
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
import { Switch } from '@/components/ui/switch';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs';
import { Textarea } from '@/components/ui/textarea';
import { useAdminAuth } from '@/contexts/AdminAuthContext';
import { useExport } from '@/hooks/useExport';
import { getSecurityEvents } from '@/lib/admin-api';
import { adminSecurityService } from '@/services/adminSecurityService';
import { SecurityEvent } from '@/types/admin';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { formatDistanceToNow } from 'date-fns';
import { vi } from 'date-fns/locale';
import {
  Activity,
  AlertTriangle,
  Ban,
  CheckCircle,
  Clock,
  Eye,
  Lock,
  Plus,
  RefreshCw,
  Search,
  Shield,
  Trash2,
  TrendingUp,
  Users,
  XCircle,
  Zap
} from 'lucide-react';
import { useEffect, useState } from 'react';
import { toast } from 'sonner';

// TypeScript Interfaces
interface IPBlockRule {
  id: string;
  ipAddress: string;
  ipRange?: string;
  type: 'blacklist' | 'whitelist';
  reason: string;
  isActive: boolean;
  expiresAt?: Date;
  createdAt: Date;
  createdBy: string;
  lastActivity?: Date;
  attemptCount: number;
}

interface RateLimitRule {
  id: number | string;
  name: string;
  endpoint: string;
  method: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'ALL';
  requestsPerMinute: number;
  requestsPerHour: number;
  requestsPerDay: number;
  isActive: boolean;
  ipWhitelist?: string[] | string;
  userRoleExceptions?: string[] | string;
  createdAt?: Date;
  updatedAt?: Date;
}

// SecurityEvent is now imported from types/admin.ts

interface SecurityMetrics {
  totalBlocked: number;
  rateLimitViolations: number;
  activeRules: number;
  suspiciousIPs: number;
  todayBlocked: number;
  last24hEvents: SecurityEvent[];
  topBlockedIPs: Array<{ ip: string; count: number; lastSeen: Date }>;
  rateLimitStats: Array<{ endpoint: string; violations: number; lastViolation: Date }>;
}

// Live API wrappers using adminSecurityService
const fetchIPBlockRules = async (): Promise<IPBlockRule[]> => {
  const rules = await adminSecurityService.getIPBlockRules();
  return (rules || []).map((r: any) => ({
    id: r.id,
    ipAddress: r.ipAddress,
    type: r.type,
    reason: r.reason,
    isActive: r.isActive,
    expiresAt: r.expiresAt ? new Date(r.expiresAt) : undefined,
    createdAt: r.createdAt ? new Date(r.createdAt) : new Date(),
    createdBy: r.createdBy ?? '',
    lastActivity: r.lastViolation ? new Date(r.lastViolation) : undefined,
    attemptCount: r.violationCount ?? 0,
  }));
};
const fetchRateLimitRules = async (): Promise<RateLimitRule[]> => {
  const rules = await adminSecurityService.getRateLimitRules();
  return (rules || []).map((r: any) => ({
    id: r.id,
    name: r.name,
    endpoint: r.endpoint,
    method: r.httpMethod ?? r.method,
    requestsPerMinute: r.requestsPerMinute,
    requestsPerHour: r.requestsPerHour,
    requestsPerDay: r.requestsPerDay,
    isActive: r.isActive,
    ipWhitelist: Array.isArray(r.ipWhitelist) ? r.ipWhitelist : (typeof r.ipWhitelist === 'string' ? JSON.parse(r.ipWhitelist || '[]') : []),
    userRoleExceptions: Array.isArray(r.userRoleExceptions) ? r.userRoleExceptions : (typeof r.userRoleExceptions === 'string' ? JSON.parse(r.userRoleExceptions || '[]') : []),
    createdAt: r.createdAt ? new Date(r.createdAt) : undefined,
    updatedAt: r.updatedAt ? new Date(r.updatedAt) : undefined,
  }));
};

const fetchSecurityMetrics = async (): Promise<SecurityMetrics> => {
  const m = await adminSecurityService.getSecurityMetrics(7);
  return {
    totalBlocked: m.totalBlocked,
    rateLimitViolations: m.rateLimitViolations,
    activeRules: m.activeRules,
    suspiciousIPs: m.suspiciousIPs,
    todayBlocked: m.todayBlocked,
    last24hEvents: [],
    topBlockedIPs: (m.topBlockedIPs || []).map(x => ({ ip: x.ip, count: x.count, lastSeen: new Date(x.lastSeen) })),
    rateLimitStats: (m.rateLimitStats || []).map(x => ({ endpoint: x.endpoint, violations: x.violations, lastViolation: new Date(x.lastViolation) })),
  };
};

const updateRateLimitRule = async (rule: Partial<RateLimitRule>): Promise<void> => {
  if (!rule.id) return;
  await adminSecurityService.updateRateLimitRule(Number(rule.id), {
    name: rule.name as string,
    endpoint: rule.endpoint as string,
    method: rule.method as any,
    requestsPerMinute: rule.requestsPerMinute as number,
    requestsPerHour: rule.requestsPerHour as number,
    requestsPerDay: rule.requestsPerDay as number,
    isActive: rule.isActive as boolean,
    description: undefined,
    ipWhitelist: rule.ipWhitelist as any,
    userRoleExceptions: rule.userRoleExceptions as any,
  } as any);
};

const createRateLimitRule = async (rule: Omit<RateLimitRule, 'id' | 'createdAt' | 'updatedAt'>): Promise<void> => {
  await adminSecurityService.createRateLimitRule({
    name: rule.name,
    endpoint: rule.endpoint,
    method: rule.method,
    requestsPerMinute: rule.requestsPerMinute,
    requestsPerHour: rule.requestsPerHour,
    requestsPerDay: rule.requestsPerDay,
    isActive: rule.isActive,
    ipWhitelist: rule.ipWhitelist as any,
    userRoleExceptions: rule.userRoleExceptions as any,
  } as any);
};

const deleteRateLimitRule = async (id: string | number): Promise<void> => {
  await adminSecurityService.deleteRateLimitRule(Number(id));
};

export default function SecurityPage() {
  const { user, hasPermission } = useAdminAuth();
  const queryClient = useQueryClient();
  const { exportSecurityEvents, exportIPBlockRules, exportRateLimitRules, exportSecurityReport } = useExport();
  const [activeTab, setActiveTab] = useState('overview');
  const [searchTerm, setSearchTerm] = useState('');
  const [showActiveOnly, setShowActiveOnly] = useState(false);
  const [showAddIPDialog, setShowAddIPDialog] = useState(false);
  const [showAddRateLimitDialog, setShowAddRateLimitDialog] = useState(false);

  // Filter states for security events
  const [eventTypeFilter, setEventTypeFilter] = useState('all');
  const [severityFilter, setSeverityFilter] = useState('all');
  const [dateRangeFilter, setDateRangeFilter] = useState('all');
  const [currentPage, setCurrentPage] = useState(1);
  const [pageSize] = useState(20);
  const [newIPRule, setNewIPRule] = useState<Partial<IPBlockRule>>({
    type: 'blacklist',
    isActive: true
  });
  const [newRateLimitRule, setNewRateLimitRule] = useState<Partial<RateLimitRule>>({
    method: 'ALL',
    isActive: true,
    ipWhitelist: [],
    userRoleExceptions: []
  });

  // Permission checks
  const canManageSecurity = hasPermission('security:manage');
  const canViewSecurity = hasPermission('security:read') || canManageSecurity;

  // Queries
  const { data: ipRules = [], isLoading: ipRulesLoading, refetch: refetchIPRules } = useQuery({
    queryKey: ['ip-block-rules'],
    queryFn: fetchIPBlockRules,
    staleTime: 30 * 1000, // 30 seconds
    enabled: canViewSecurity
  });

  const { data: rateLimitRules = [], isLoading: rateLimitRulesLoading, refetch: refetchRateLimitRules } = useQuery({
    queryKey: ['rate-limit-rules'],
    queryFn: fetchRateLimitRules,
    staleTime: 30 * 1000,
    enabled: canViewSecurity
  });

  const { data: securityEventsData, isLoading: securityEventsLoading, refetch: refetchSecurityEvents } = useQuery({
    queryKey: ['security-events', searchTerm, eventTypeFilter, severityFilter, dateRangeFilter],
    queryFn: () => {
      const params: any = {
        page: 1,
        limit: 100
      };

      if (searchTerm) params.search = searchTerm;
      if (eventTypeFilter !== 'all') params.eventType = eventTypeFilter;
      if (severityFilter !== 'all') params.severity = severityFilter;
      if (dateRangeFilter !== 'all') {
        const now = new Date();
        switch (dateRangeFilter) {
          case 'today':
            params.from = now.toISOString().split('T')[0];
            break;
          case 'week':
            const weekAgo = new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000);
            params.from = weekAgo.toISOString().split('T')[0];
            break;
          case 'month':
            const monthAgo = new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000);
            params.from = monthAgo.toISOString().split('T')[0];
            break;
        }
      }

      return getSecurityEvents(params);
    },
    staleTime: 10 * 1000, // 10 seconds for real-time feel
    enabled: canViewSecurity
  });

  const securityEvents = securityEventsData?.items || [];  //  Changed from 'events' to 'items'

  // Client-side filtering for additional filtering (search, eventType, severity, date range)
  const filteredSecurityEvents = securityEvents.filter(event => {
    // Text search by description or IP
    if (
      searchTerm &&
      !event.description.toLowerCase().includes(searchTerm.toLowerCase()) &&
      !(event.ipAddress || '').toLowerCase().includes(searchTerm.toLowerCase())
    ) {
      return false;
    }

    // Event type filter
    if (eventTypeFilter !== 'all' && event.eventType !== eventTypeFilter) {
      return false;
    }

    // Severity filter (API stores capitalized severities like "Medium")
    if (severityFilter !== 'all' && event.severity !== severityFilter) {
      return false;
    }

    // Date range filter (createdAt >= from)
    if (dateRangeFilter !== 'all') {
      const created = new Date(event.createdAt).getTime();
      const now = Date.now();
      let fromTs = 0;
      if (dateRangeFilter === 'today') {
        const d = new Date();
        d.setHours(0, 0, 0, 0);
        fromTs = d.getTime();
      } else if (dateRangeFilter === 'week') {
        fromTs = now - 7 * 24 * 60 * 60 * 1000;
      } else if (dateRangeFilter === 'month') {
        fromTs = now - 30 * 24 * 60 * 60 * 1000;
      }
      if (created < fromTs) {
        return false;
      }
    }

    return true;
  });

  // Pagination
  const totalPages = Math.ceil(filteredSecurityEvents.length / pageSize);
  const paginatedEvents = filteredSecurityEvents.slice(
    (currentPage - 1) * pageSize,
    currentPage * pageSize
  );

  const { data: securityMetrics, isLoading: metricsLoading, refetch: refetchMetrics } = useQuery({
    queryKey: ['security-metrics'],
    queryFn: fetchSecurityMetrics,
    staleTime: 30 * 1000,
    enabled: canViewSecurity
  });

  // Mutations
  const ipRuleMutation = useMutation({
    mutationFn: async (rule: any) => {
      const payload = {
        ipAddress: rule.ipAddress,
        type: rule.type,
        reason: rule.reason,
        isActive: rule.isActive,
        expiresAt: rule.expiresAt ? new Date(rule.expiresAt).toISOString() : undefined,
        threatLevel: 'medium',
      } as any;
      await adminSecurityService.updateIPBlockRule(Number(rule.id), payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ip-block-rules'] });
      queryClient.invalidateQueries({ queryKey: ['security-metrics'] });
      toast.success('Quy tc IP  c cp nht');
    },
    onError: () => {
      toast.error('C li xy ra khi cp nht quy tc IP');
    }
  });

  const createIPRuleMutation = useMutation({
    mutationFn: async (data: any) => {
      const payload = {
        ipAddress: data.ipAddress,
        type: data.type,
        reason: data.reason,
        isActive: data.isActive,
        expiresAt: data.expiresAt ? new Date(data.expiresAt).toISOString() : undefined,
        threatLevel: 'medium',
        countryCode: data.countryCode,
      } as any;
      await adminSecurityService.createIPBlockRule(payload);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ip-block-rules'] });
      queryClient.invalidateQueries({ queryKey: ['security-metrics'] });
      setShowAddIPDialog(false);
      setNewIPRule({ type: 'blacklist', isActive: true });
      toast.success('Quy tc IP mi  c to');
    },
    onError: () => {
      toast.error('C li xy ra khi to quy tc IP');
    }
  });

  const deleteIPRuleMutation = useMutation({
    mutationFn: async (id: any) => {
      await adminSecurityService.deleteIPBlockRule(Number(id));
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ip-block-rules'] });
      queryClient.invalidateQueries({ queryKey: ['security-metrics'] });
      toast.success('Quy tc IP  c xa');
    },
    onError: () => {
      toast.error('C li xy ra khi xa quy tc IP');
    }
  });

  const rateLimitMutation = useMutation({
    mutationFn: updateRateLimitRule,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rate-limit-rules'] });
      toast.success('Quy tc gii hn tc   c cp nht');
    },
    onError: () => {
      toast.error('C li xy ra khi cp nht quy tc gii hn tc ');
    }
  });

  const createRateLimitMutation = useMutation({
    mutationFn: createRateLimitRule,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rate-limit-rules'] });
      setShowAddRateLimitDialog(false);
      setNewRateLimitRule({
        method: 'ALL',
        isActive: true,
        ipWhitelist: [],
        userRoleExceptions: []
      });
      toast.success('Quy tc gii hn tc  mi  c to');
    },
    onError: () => {
      toast.error('C li xy ra khi to quy tc gii hn tc ');
    }
  });

  const deleteRateLimitMutation = useMutation({
    mutationFn: deleteRateLimitRule,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['rate-limit-rules'] });
      toast.success('Quy tc gii hn tc   c xa');
    },
    onError: () => {
      toast.error('C li xy ra khi xa quy tc gii hn tc ');
    }
  });

  // Auto-refresh for real-time monitoring
  useEffect(() => {
    if (!canViewSecurity) return;

    const interval = setInterval(() => {
      refetchSecurityEvents();
      refetchMetrics();
    }, 30 * 1000); // Refresh every 30 seconds

    return () => clearInterval(interval);
  }, [canViewSecurity, refetchSecurityEvents, refetchMetrics]);

  // Helper functions
  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case 'critical': return 'bg-red-100 text-red-800 border-red-200';
      case 'high': return 'bg-orange-100 text-orange-800 border-orange-200';
      case 'medium': return 'bg-yellow-100 text-yellow-800 border-yellow-200';
      case 'low': return 'bg-blue-100 text-blue-800 border-blue-200';
      default: return 'bg-gray-100 text-gray-800 border-gray-200';
    }
  };

  const getEventTypeIcon = (type: string) => {
    switch (type) {
      case 'failed_login': return <Users className="h-4 w-4" />;
      case 'password_changed': return <Lock className="h-4 w-4" />;
      case 'ip_rule_deleted': return <Ban className="h-4 w-4" />;
      case 'ip_blocked': return <Ban className="h-4 w-4" />;
      case 'rate_limit_exceeded': return <Clock className="h-4 w-4" />;
      case 'suspicious_activity': return <AlertTriangle className="h-4 w-4" />;
      case 'login_attempt': return <Users className="h-4 w-4" />;
      default: return <Shield className="h-4 w-4" />;
    }
  };

  const handleToggleIPRule = (rule: IPBlockRule) => {
    ipRuleMutation.mutate({
      ...rule,
      isActive: !rule.isActive
    });
  };

  const handleToggleRateLimitRule = (rule: RateLimitRule) => {
    rateLimitMutation.mutate({
      ...rule,
      isActive: !rule.isActive
    });
  };

  const handleCreateIPRule = () => {
    if (!newIPRule.ipAddress || !newIPRule.reason) {
      toast.error('Vui lng in y  thng tin');
      return;
    }

    createIPRuleMutation.mutate({
      ...newIPRule,
      createdAt: new Date(),
      createdBy: user?.fullName || user?.email || 'admin',
      attemptCount: 0
    } as Omit<IPBlockRule, 'id' | 'createdAt' | 'attemptCount'>);
  };

  const handleCreateRateLimitRule = () => {
    if (!newRateLimitRule.name || !newRateLimitRule.endpoint) {
      toast.error('Vui lng in y  thng tin');
      return;
    }

    createRateLimitMutation.mutate({
      ...newRateLimitRule,
      requestsPerMinute: newRateLimitRule.requestsPerMinute || 60,
      requestsPerHour: newRateLimitRule.requestsPerHour || 1000,
      requestsPerDay: newRateLimitRule.requestsPerDay || 10000,
      createdAt: new Date(),
      updatedAt: new Date()
    } as Omit<RateLimitRule, 'id' | 'createdAt' | 'updatedAt'>);
  };

  if (!canViewSecurity) {
    return (
      <div className="container mx-auto py-8">
        <Card>
          <CardContent className="flex items-center justify-center py-16">
            <div className="text-center space-y-4">
              <Lock className="h-12 w-12 mx-auto text-muted-foreground" />
              <h3 className="text-lg font-semibold">Khng c quyn truy cp</h3>
              <p className="text-muted-foreground">
                Bn khng c quyn xem trang bo mt ny.
              </p>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="container mx-auto py-8 space-y-8">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-bold tracking-tight">Bo mt h thng</h1>
          <p className="text-muted-foreground">
            Qun l IP blocking, rate limiting v gim st bo mt
          </p>
        </div>
        <div className="flex space-x-2">
          <Button
            variant="outline"
            onClick={() => {
              refetchIPRules();
              refetchRateLimitRules();
              refetchSecurityEvents();
              refetchMetrics();
            }}
          >
            <RefreshCw className="h-4 w-4 mr-2" />
            Lm mi
          </Button>
          <ExportButtons
            type="security-events"
            options={{ search: searchTerm }}
            className="flex items-center"
          />
          <ExportButtons
            type="ip-block-rules"
            options={{ search: searchTerm }}
            className="flex items-center"
          />
          <ExportButtons
            type="rate-limit-rules"
            className="flex items-center"
          />
        </div>
      </div>

      <Tabs value={activeTab} onValueChange={setActiveTab} className="space-y-6">
        <TabsList className="grid w-full grid-cols-4">
          <TabsTrigger value="overview" className="space-x-2">
            <Activity className="h-4 w-4" />
            <span>Tng quan</span>
          </TabsTrigger>
          <TabsTrigger value="ip-blocking" disabled={!canManageSecurity}>
            <Ban className="h-4 w-4" />
            <span>IP Blocking</span>
          </TabsTrigger>
          <TabsTrigger value="rate-limiting" disabled={!canManageSecurity}>
            <Clock className="h-4 w-4" />
            <span>Rate Limiting</span>
          </TabsTrigger>
          <TabsTrigger value="monitoring">
            <Eye className="h-4 w-4" />
            <span>Gim st</span>
          </TabsTrigger>
        </TabsList>

        {/* Overview Tab */}
        <TabsContent value="overview" className="space-y-6">
          {/* Security Metrics */}
          <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Tng s IP b chn</CardTitle>
                <Ban className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{securityMetrics?.totalBlocked || 0}</div>
                <p className="text-xs text-muted-foreground">
                  {securityMetrics?.todayBlocked || 0} trong hm nay
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Vi phm Rate Limit</CardTitle>
                <Clock className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{securityMetrics?.rateLimitViolations || 0}</div>
                <p className="text-xs text-muted-foreground">
                  Trong 24h qua
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Quy tc ang hot ng</CardTitle>
                <Shield className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{securityMetrics?.activeRules || 0}</div>
                <p className="text-xs text-muted-foreground">
                  Quy tc bo mt
                </p>
              </CardContent>
            </Card>

            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">IP ng nghi</CardTitle>
                <AlertTriangle className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{securityMetrics?.suspiciousIPs || 0}</div>
                <p className="text-xs text-muted-foreground">
                  Cn theo di
                </p>
              </CardContent>
            </Card>
          </div>

          {/* Recent Events */}
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="flex items-center space-x-2">
                  <Activity className="h-5 w-5" />
                  <span>S kin bo mt gn y</span>
                </CardTitle>
                <ExportButtons
                  type="security-report"
                  className="text-sm"
                />
              </div>
            </CardHeader>
            <CardContent>
              {securityEventsLoading ? (
                <div className="flex items-center justify-center py-8">
                  <RefreshCw className="h-6 w-6 animate-spin" />
                  <span className="ml-2">ang ti s kin...</span>
                </div>
              ) : (
                <div className="space-y-3">
                  {filteredSecurityEvents.slice(0, 5).map((event) => (
                    <div key={event.id} className="flex items-center justify-between p-3 border rounded-lg">
                      <div className="flex items-center space-x-3">
                        {getEventTypeIcon(event.eventType)}
                        <div>
                          <div className="font-medium">{event.description}</div>
                          <div className="text-sm text-muted-foreground">
                            IP: {event.ipAddress}  {formatDistanceToNow(new Date(event.createdAt), { addSuffix: true, locale: vi })}
                          </div>
                        </div>
                      </div>
                      <div className="flex items-center space-x-2">
                        <Badge className={getSeverityColor(event.severity.toLowerCase())}>
                          {event.severity.toUpperCase()}
                        </Badge>
                        {(event as any).wasBlocked && (
                          <Badge variant="destructive"> chn</Badge>
                        )}
                      </div>
                    </div>
                  ))}
                  {filteredSecurityEvents.length === 0 && (
                    <p className="text-center py-8 text-muted-foreground">
                      Khng c s kin bo mt no gn y
                    </p>
                  )}
                </div>
              )}
            </CardContent>
          </Card>

          {/* Top Statistics */}
          <div className="grid gap-6 md:grid-cols-2">
            {/* Top Blocked IPs */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                  <TrendingUp className="h-5 w-5" />
                  <span>IP b chn nhiu nht</span>
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {securityMetrics?.topBlockedIPs.map((item, index) => (
                    <div key={item.ip} className="flex items-center justify-between">
                      <div className="flex items-center space-x-2">
                        <Badge variant="outline">{index + 1}</Badge>
                        <span className="font-mono text-sm">{item.ip}</span>
                      </div>
                      <div className="text-right">
                        <div className="font-medium">{item.count} ln</div>
                        <div className="text-xs text-muted-foreground">
                          {formatDistanceToNow(item.lastSeen, { addSuffix: true, locale: vi })}
                        </div>
                      </div>
                    </div>
                  )) || []}
                </div>
              </CardContent>
            </Card>

            {/* Rate Limit Statistics */}
            <Card>
              <CardHeader>
                <CardTitle className="flex items-center space-x-2">
                  <Zap className="h-5 w-5" />
                  <span>Endpoint vi phm Rate Limit</span>
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="space-y-3">
                  {securityMetrics?.rateLimitStats.map((item, index) => (
                    <div key={item.endpoint} className="flex items-center justify-between">
                      <div className="flex items-center space-x-2">
                        <Badge variant="outline">{index + 1}</Badge>
                        <span className="font-mono text-sm">{item.endpoint}</span>
                      </div>
                      <div className="text-right">
                        <div className="font-medium">{item.violations} vi phm</div>
                        <div className="text-xs text-muted-foreground">
                          {formatDistanceToNow(item.lastViolation, { addSuffix: true, locale: vi })}
                        </div>
                      </div>
                    </div>
                  )) || []}
                </div>
              </CardContent>
            </Card>
          </div>
        </TabsContent>

        {/* IP Blocking Tab */}
        <TabsContent value="ip-blocking" className="space-y-6">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="flex items-center space-x-2">
                  <Ban className="h-5 w-5" />
                  <span>Qun l IP Blocking</span>
                </CardTitle>
                <div className="flex items-center space-x-2">
                  <ExportButtons
                    type="ip-block-rules"
                    options={{ search: searchTerm }}
                    className="text-sm"
                  />
                  <Dialog open={showAddIPDialog} onOpenChange={setShowAddIPDialog}>
                    <DialogTrigger asChild>
                      <Button>
                        <Plus className="h-4 w-4 mr-2" />
                        Thm quy tc IP
                      </Button>
                    </DialogTrigger>
                    <DialogContent className="sm:max-w-[425px]">
                      <DialogHeader>
                        <DialogTitle>Thm quy tc IP mi</DialogTitle>
                        <DialogDescription>
                          To quy tc chn hoc cho php IP address.
                        </DialogDescription>
                      </DialogHeader>
                      <div className="grid gap-4 py-4">
                        <div className="grid grid-cols-4 items-center gap-4">
                          <Label htmlFor="ip-address" className="text-right">
                            IP Address
                          </Label>
                          <Input
                            id="ip-address"
                            placeholder="192.168.1.100 hoc 192.168.1.0/24"
                            className="col-span-3"
                            value={newIPRule.ipAddress || ''}
                            onChange={(e) => setNewIPRule(prev => ({ ...prev, ipAddress: e.target.value }))}
                          />
                        </div>
                        <div className="grid grid-cols-4 items-center gap-4">
                          <Label htmlFor="ip-type" className="text-right">
                            Loi
                          </Label>
                          <Select
                            value={newIPRule.type}
                            onValueChange={(value: 'blacklist' | 'whitelist') =>
                              setNewIPRule(prev => ({ ...prev, type: value }))
                            }
                          >
                            <SelectTrigger className="col-span-3">
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="blacklist">Blacklist (Chn)</SelectItem>
                              <SelectItem value="whitelist">Whitelist (Cho php)</SelectItem>
                            </SelectContent>
                          </Select>
                        </div>
                        <div className="grid grid-cols-4 items-center gap-4">
                          <Label htmlFor="ip-reason" className="text-right">
                            L do
                          </Label>
                          <Textarea
                            id="ip-reason"
                            placeholder="L do p dng quy tc ny..."
                            className="col-span-3"
                            value={newIPRule.reason || ''}
                            onChange={(e) => setNewIPRule(prev => ({ ...prev, reason: e.target.value }))}
                          />
                        </div>
                        <div className="grid grid-cols-4 items-center gap-4">
                          <Label htmlFor="ip-expires" className="text-right">
                            Ht hn
                          </Label>
                          <Input
                            id="ip-expires"
                            type="datetime-local"
                            className="col-span-3"
                            onChange={(e) => setNewIPRule(prev => ({
                              ...prev,
                              expiresAt: e.target.value ? new Date(e.target.value) : undefined
                            }))}
                          />
                        </div>
                      </div>
                      <DialogFooter>
                        <Button
                          type="submit"
                          onClick={handleCreateIPRule}
                          disabled={createIPRuleMutation.isPending}
                        >
                          {createIPRuleMutation.isPending ? (
                            <RefreshCw className="h-4 w-4 animate-spin mr-2" />
                          ) : null}
                          To quy tc
                        </Button>
                      </DialogFooter>
                    </DialogContent>
                  </Dialog>
                </div>
              </div>
            </CardHeader>
            <CardContent>
              {/* IP Blocking Controls */}
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center space-x-4">
                  <div className="relative">
                    <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                      placeholder="Tm kim theo IP address hoc l do..."
                      className="pl-8 w-64"
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                    />
                  </div>
                  <div className="flex items-center space-x-2">
                    <Switch
                      id="ip-active-only"
                      checked={showActiveOnly}
                      onCheckedChange={setShowActiveOnly}
                    />
                    <Label htmlFor="ip-active-only" className="text-sm">
                      Ch hin th rule ang hot ng
                    </Label>
                  </div>
                </div>
                <div className="text-sm text-muted-foreground">
                  {ipRules.filter(rule => {
                    const matchesSearch = rule.ipAddress.toLowerCase().includes(searchTerm.toLowerCase()) ||
                      rule.reason.toLowerCase().includes(searchTerm.toLowerCase());
                    const matchesActiveFilter = !showActiveOnly || rule.isActive;
                    return matchesSearch && matchesActiveFilter;
                  }).length} / {ipRules.length} quy tc
                </div>
              </div>

              {/* IP Rules Table */}
              {ipRulesLoading ? (
                <div className="flex items-center justify-center py-8">
                  <RefreshCw className="h-6 w-6 animate-spin" />
                  <span className="ml-2">ang ti quy tc IP...</span>
                </div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>IP Address</TableHead>
                      <TableHead>Loi</TableHead>
                      <TableHead>L do</TableHead>
                      <TableHead>Trng thi</TableHead>
                      <TableHead>Ht hn</TableHead>
                      <TableHead>Hot ng cui</TableHead>
                      <TableHead>Thao tc</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {ipRules
                      .filter(rule => {
                        // Filter by search term
                        const matchesSearch = rule.ipAddress.toLowerCase().includes(searchTerm.toLowerCase()) ||
                          rule.reason.toLowerCase().includes(searchTerm.toLowerCase());

                        // Filter by active status if enabled
                        const matchesActiveFilter = !showActiveOnly || rule.isActive;

                        return matchesSearch && matchesActiveFilter;
                      })
                      .map((rule) => (
                        <TableRow key={rule.id}>
                          <TableCell className="font-mono text-sm">
                            {rule.ipAddress}
                          </TableCell>
                          <TableCell>
                            <Badge variant={rule.type === 'blacklist' ? 'destructive' : 'default'}>
                              {rule.type === 'blacklist' ? 'Chn' : 'Cho php'}
                            </Badge>
                          </TableCell>
                          <TableCell className="max-w-xs truncate">
                            {rule.reason}
                          </TableCell>
                          <TableCell>
                            <div className="flex items-center space-x-2">
                              <Switch
                                checked={rule.isActive}
                                onCheckedChange={() => handleToggleIPRule(rule)}
                              />
                              <span className="text-sm">
                                {rule.isActive ? 'Hot ng' : 'Tm dng'}
                              </span>
                            </div>
                          </TableCell>
                          <TableCell>
                            {rule.expiresAt ? (
                              <span className="text-sm">
                                {formatDistanceToNow(rule.expiresAt, { addSuffix: true, locale: vi })}
                              </span>
                            ) : (
                              <span className="text-muted-foreground">Vnh vin</span>
                            )}
                          </TableCell>
                          <TableCell>
                            {rule.lastActivity ? (
                              <span className="text-sm">
                                {formatDistanceToNow(rule.lastActivity, { addSuffix: true, locale: vi })}
                                <div className="text-xs text-muted-foreground">
                                  {rule.attemptCount} ln th
                                </div>
                              </span>
                            ) : (
                              <span className="text-muted-foreground">Cha c</span>
                            )}
                          </TableCell>
                          <TableCell>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => deleteIPRuleMutation.mutate(rule.id)}
                              disabled={deleteIPRuleMutation.isPending}
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* Rate Limiting Tab */}
        <TabsContent value="rate-limiting" className="space-y-6">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="flex items-center space-x-2">
                  <Clock className="h-5 w-5" />
                  <span>Qun l Rate Limiting</span>
                </CardTitle>
                <div className="flex items-center space-x-2">
                  <ExportButtons
                    type="rate-limit-rules"
                    className="text-sm"
                  />
                  <Dialog open={showAddRateLimitDialog} onOpenChange={setShowAddRateLimitDialog}>
                    <DialogTrigger asChild>
                      <Button>
                        <Plus className="h-4 w-4 mr-2" />
                        Thm quy tc Rate Limit
                      </Button>
                    </DialogTrigger>
                    <DialogContent className="sm:max-w-[525px]">
                      <DialogHeader>
                        <DialogTitle>Thm quy tc Rate Limit mi</DialogTitle>
                        <DialogDescription>
                          To quy tc gii hn tc  request cho endpoint.
                        </DialogDescription>
                      </DialogHeader>
                      <div className="grid gap-4 py-4">
                        <div className="grid grid-cols-4 items-center gap-4">
                          <Label htmlFor="rule-name" className="text-right">
                            Tn quy tc
                          </Label>
                          <Input
                            id="rule-name"
                            placeholder="API Login Limit"
                            className="col-span-3"
                            value={newRateLimitRule.name || ''}
                            onChange={(e) => setNewRateLimitRule(prev => ({ ...prev, name: e.target.value }))}
                          />
                        </div>
                        <div className="grid grid-cols-4 items-center gap-4">
                          <Label htmlFor="rule-endpoint" className="text-right">
                            Endpoint
                          </Label>
                          <Input
                            id="rule-endpoint"
                            placeholder="/api/auth/login"
                            className="col-span-3"
                            value={newRateLimitRule.endpoint || ''}
                            onChange={(e) => setNewRateLimitRule(prev => ({ ...prev, endpoint: e.target.value }))}
                          />
                        </div>
                        <div className="grid grid-cols-4 items-center gap-4">
                          <Label htmlFor="rule-method" className="text-right">
                            Phng thc
                          </Label>
                          <Select
                            value={newRateLimitRule.method}
                            onValueChange={(value: 'GET' | 'POST' | 'PUT' | 'DELETE' | 'ALL') =>
                              setNewRateLimitRule(prev => ({ ...prev, method: value }))
                            }
                          >
                            <SelectTrigger className="col-span-3">
                              <SelectValue />
                            </SelectTrigger>
                            <SelectContent>
                              <SelectItem value="ALL">Tt c</SelectItem>
                              <SelectItem value="GET">GET</SelectItem>
                              <SelectItem value="POST">POST</SelectItem>
                              <SelectItem value="PUT">PUT</SelectItem>
                              <SelectItem value="DELETE">DELETE</SelectItem>
                            </SelectContent>
                          </Select>
                        </div>
                        <div className="grid grid-cols-4 items-center gap-4">
                          <Label htmlFor="rule-per-minute" className="text-right">
                            Per pht
                          </Label>
                          <Input
                            id="rule-per-minute"
                            type="number"
                            placeholder="60"
                            className="col-span-3"
                            value={newRateLimitRule.requestsPerMinute || ''}
                            onChange={(e) => setNewRateLimitRule(prev => ({
                              ...prev,
                              requestsPerMinute: parseInt(e.target.value) || 0
                            }))}
                          />
                        </div>
                        <div className="grid grid-cols-4 items-center gap-4">
                          <Label htmlFor="rule-per-hour" className="text-right">
                            Per gi
                          </Label>
                          <Input
                            id="rule-per-hour"
                            type="number"
                            placeholder="1000"
                            className="col-span-3"
                            value={newRateLimitRule.requestsPerHour || ''}
                            onChange={(e) => setNewRateLimitRule(prev => ({
                              ...prev,
                              requestsPerHour: parseInt(e.target.value) || 0
                            }))}
                          />
                        </div>
                        <div className="grid grid-cols-4 items-center gap-4">
                          <Label htmlFor="rule-per-day" className="text-right">
                            Per ngy
                          </Label>
                          <Input
                            id="rule-per-day"
                            type="number"
                            placeholder="10000"
                            className="col-span-3"
                            value={newRateLimitRule.requestsPerDay || ''}
                            onChange={(e) => setNewRateLimitRule(prev => ({
                              ...prev,
                              requestsPerDay: parseInt(e.target.value) || 0
                            }))}
                          />
                        </div>
                      </div>
                      <DialogFooter>
                        <Button
                          type="submit"
                          onClick={handleCreateRateLimitRule}
                          disabled={createRateLimitMutation.isPending}
                        >
                          {createRateLimitMutation.isPending ? (
                            <RefreshCw className="h-4 w-4 animate-spin mr-2" />
                          ) : null}
                          To quy tc
                        </Button>
                      </DialogFooter>
                    </DialogContent>
                  </Dialog>
                </div>
              </div>
            </CardHeader>
            <CardContent>
              {/* Rate Limit Rules Controls */}
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center space-x-4">
                  <div className="relative">
                    <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                      placeholder="Tm kim theo tn hoc endpoint..."
                      className="pl-8 w-64"
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                    />
                  </div>
                  <div className="flex items-center space-x-2">
                    <Switch
                      id="active-only"
                      checked={showActiveOnly}
                      onCheckedChange={setShowActiveOnly}
                    />
                    <Label htmlFor="active-only" className="text-sm">
                      Ch hin th rule ang hot ng
                    </Label>
                  </div>
                </div>
                <div className="text-sm text-muted-foreground">
                  {rateLimitRules.filter(rule => {
                    const matchesSearch = rule.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                      rule.endpoint.toLowerCase().includes(searchTerm.toLowerCase());
                    const matchesActiveFilter = !showActiveOnly || rule.isActive;
                    return matchesSearch && matchesActiveFilter;
                  }).length} / {rateLimitRules.length} quy tc
                </div>
              </div>

              {/* Rate Limit Rules Table */}
              {rateLimitRulesLoading ? (
                <div className="flex items-center justify-center py-8">
                  <RefreshCw className="h-6 w-6 animate-spin" />
                  <span className="ml-2">ang ti quy tc Rate Limit...</span>
                </div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Tn / Endpoint</TableHead>
                      <TableHead>Phng thc</TableHead>
                      <TableHead>Gii hn</TableHead>
                      <TableHead>Trng thi</TableHead>
                      <TableHead>Cp nht</TableHead>
                      <TableHead>Thao tc</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {rateLimitRules
                      .filter(rule => {
                        // Filter by search term
                        const matchesSearch = rule.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                          rule.endpoint.toLowerCase().includes(searchTerm.toLowerCase());

                        // Filter by active status if enabled
                        const matchesActiveFilter = !showActiveOnly || rule.isActive;

                        return matchesSearch && matchesActiveFilter;
                      })
                      .map((rule) => (
                        <TableRow key={rule.id}>
                          <TableCell>
                            <div>
                              <div className="font-medium">{rule.name}</div>
                              <div className="text-sm text-muted-foreground font-mono">
                                {rule.endpoint}
                              </div>
                            </div>
                          </TableCell>
                          <TableCell>
                            <Badge variant="outline">
                              {rule.method}
                            </Badge>
                          </TableCell>
                          <TableCell>
                            <div className="text-sm space-y-1">
                              <div>{rule.requestsPerMinute}/pht</div>
                              <div className="text-muted-foreground">
                                {rule.requestsPerHour}/gi, {rule.requestsPerDay}/ngy
                              </div>
                            </div>
                          </TableCell>
                          <TableCell>
                            <div className="flex items-center space-x-2">
                              <Switch
                                checked={rule.isActive}
                                onCheckedChange={() => handleToggleRateLimitRule(rule)}
                              />
                              <span className="text-sm">
                                {rule.isActive ? 'Hot ng' : 'Tm dng'}
                              </span>
                            </div>
                          </TableCell>
                          <TableCell>
                            <span className="text-sm">
                              {rule.updatedAt ? formatDistanceToNow(rule.updatedAt, { addSuffix: true, locale: vi }) : 'N/A'}
                            </span>
                          </TableCell>
                          <TableCell>
                            <Button
                              variant="ghost"
                              size="sm"
                              onClick={() => deleteRateLimitMutation.mutate(rule.id)}
                              disabled={deleteRateLimitMutation.isPending}
                            >
                              <Trash2 className="h-4 w-4" />
                            </Button>
                          </TableCell>
                        </TableRow>
                      ))}
                  </TableBody>
                </Table>
              )}
            </CardContent>
          </Card>
        </TabsContent>

        {/* Monitoring Tab */}
        <TabsContent value="monitoring" className="space-y-6">
          <Card>
            <CardHeader>
              <div className="flex items-center justify-between">
                <CardTitle className="flex items-center space-x-2">
                  <Eye className="h-5 w-5" />
                  <span>Gim st bo mt theo thi gian thc</span>
                </CardTitle>
                <ExportButtons
                  type="security-events"
                  options={{
                    search: searchTerm,
                    eventType: eventTypeFilter !== 'all' ? eventTypeFilter : undefined,
                    severity: severityFilter !== 'all' ? severityFilter : undefined,
                    startDate: dateRangeFilter !== 'all' ? (() => {
                      const now = new Date();
                      switch (dateRangeFilter) {
                        case 'today':
                          return now.toISOString().split('T')[0];
                        case 'week':
                          return new Date(now.getTime() - 7 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
                        case 'month':
                          return new Date(now.getTime() - 30 * 24 * 60 * 60 * 1000).toISOString().split('T')[0];
                        default:
                          return undefined;
                      }
                    })() : undefined
                  }}
                  className="text-sm"
                />
              </div>
            </CardHeader>
            <CardContent>
              {/* Search and Filters */}
              <div className="mb-6 flex flex-col sm:flex-row gap-4">
                <div className="flex items-center space-x-2 text-sm text-muted-foreground">
                  <span>Hin th {filteredSecurityEvents.length} / {securityEvents.length} s kin</span>
                </div>
                <div className="flex-1">
                  <div className="relative">
                    <Search className="absolute left-2 top-2.5 h-4 w-4 text-muted-foreground" />
                    <Input
                      placeholder="Tm kim theo IP, endpoint, hoc l do..."
                      className="pl-8"
                      value={searchTerm}
                      onChange={(e) => setSearchTerm(e.target.value)}
                    />
                  </div>
                </div>
                <Select value={eventTypeFilter} onValueChange={setEventTypeFilter}>
                  <SelectTrigger className="w-[180px]">
                    <SelectValue placeholder="Loi s kin" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tt c</SelectItem>
                    <SelectItem value="ip_access_allowed">IP c php</SelectItem>
                    <SelectItem value="ip_access_denied">IP b t chi</SelectItem>
                    <SelectItem value="rate_limit_exceeded">Vt Rate Limit</SelectItem>
                    <SelectItem value="suspicious_activity">Hot ng ng nghi</SelectItem>
                    <SelectItem value="login_attempt">Th ng nhp</SelectItem>
                    <SelectItem value="authentication_failed">Xc thc tht bi</SelectItem>
                  </SelectContent>
                </Select>
                <Select value={severityFilter} onValueChange={setSeverityFilter}>
                  <SelectTrigger className="w-[150px]">
                    <SelectValue placeholder="Mc " />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tt c</SelectItem>
                    <SelectItem value="Critical">Critical</SelectItem>
                    <SelectItem value="High">High</SelectItem>
                    <SelectItem value="Medium">Medium</SelectItem>
                    <SelectItem value="Low">Low</SelectItem>
                  </SelectContent>
                </Select>
                <Select value={dateRangeFilter} onValueChange={setDateRangeFilter}>
                  <SelectTrigger className="w-[150px]">
                    <SelectValue placeholder="Thi gian" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">Tt c</SelectItem>
                    <SelectItem value="today">Hm nay</SelectItem>
                    <SelectItem value="week">7 ngy qua</SelectItem>
                    <SelectItem value="month">30 ngy qua</SelectItem>
                  </SelectContent>
                </Select>
                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => {
                    setSearchTerm('');
                    setEventTypeFilter('all');
                    setSeverityFilter('all');
                    setDateRangeFilter('all');
                  }}
                  className="whitespace-nowrap"
                >
                  <RefreshCw className="h-4 w-4 mr-2" />
                  Reset
                </Button>
              </div>

              {/* Security Events Table */}
              {securityEventsLoading ? (
                <div className="flex items-center justify-center py-8">
                  <RefreshCw className="h-6 w-6 animate-spin" />
                  <span className="ml-2">ang ti s kin bo mt...</span>
                </div>
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow>
                      <TableHead>Thi gian</TableHead>
                      <TableHead>Loi s kin</TableHead>
                      <TableHead>IP Address</TableHead>
                      <TableHead>Correlation ID</TableHead>
                      <TableHead>M t</TableHead>
                      <TableHead>Mc </TableHead>
                      <TableHead>Trng thi</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {paginatedEvents
                      .map((event) => (
                        <TableRow key={event.id}>
                          <TableCell>
                            <span className="text-sm">
                              {formatDistanceToNow(new Date(event.createdAt), { addSuffix: true, locale: vi })}
                            </span>
                          </TableCell>
                          <TableCell>
                            <div className="flex items-center space-x-2">
                              {getEventTypeIcon(event.eventType)}
                              <span className="text-sm">
                                {event.eventType === 'failed_login' && 'ng nhp tht bi'}
                                {event.eventType === 'password_changed' && 'i mt khu'}
                                {event.eventType === 'ip_rule_deleted' && 'Xa quy tc IP'}
                                {event.eventType === 'suspicious_activity' && 'Hot ng ng nghi'}
                                {!['failed_login', 'password_changed', 'ip_rule_deleted', 'suspicious_activity'].includes(event.eventType) && event.eventType}
                              </span>
                            </div>
                          </TableCell>
                          <TableCell className="font-mono text-sm">
                            {event.ipAddress || '-'}
                          </TableCell>
                          <TableCell className="font-mono text-sm">
                            {(event as any).correlationId ? (event as any).correlationId.substring(0, 20) + '...' : '-'}
                          </TableCell>
                          <TableCell className="max-w-xs truncate">
                            {event.description}
                          </TableCell>
                          <TableCell>
                            <Badge className={getSeverityColor(event.severity.toLowerCase())}>
                              {event.severity.toUpperCase()}
                            </Badge>
                          </TableCell>
                          <TableCell>
                            {(event as any).wasBlocked ? (
                              <Badge variant="destructive">
                                <XCircle className="h-3 w-3 mr-1" />
                                 chn
                              </Badge>
                            ) : (
                              <Badge variant="secondary">
                                <CheckCircle className="h-3 w-3 mr-1" />
                                {(event as any).status === 'logged' ? ' ghi nhn' : 'Cho php'}
                              </Badge>
                            )}
                          </TableCell>
                        </TableRow>
                      ))}
                  </TableBody>
                </Table>
              )}

              {filteredSecurityEvents.length === 0 && !securityEventsLoading && (
                <div className="text-center py-8">
                  <Shield className="h-12 w-12 mx-auto text-muted-foreground mb-4" />
                  <h3 className="text-lg font-semibold mb-2">Khng c s kin bo mt</h3>
                  <p className="text-muted-foreground">
                    H thng ang hot ng bnh thng, khng c s kin bo mt no c ghi nhn.
                  </p>
                </div>
              )}

              {/* Pagination */}
              {totalPages > 1 && (
                <div className="flex items-center justify-between mt-6">
                  <div className="text-sm text-muted-foreground">
                    Hin th {(currentPage - 1) * pageSize + 1} - {Math.min(currentPage * pageSize, filteredSecurityEvents.length)} ca {filteredSecurityEvents.length} s kin
                  </div>
                  <div className="flex items-center space-x-2">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                      disabled={currentPage === 1}
                    >
                      Trc
                    </Button>
                    <div className="flex items-center space-x-1">
                      {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                        const page = i + 1;
                        return (
                          <Button
                            key={page}
                            variant={currentPage === page ? "default" : "outline"}
                            size="sm"
                            onClick={() => setCurrentPage(page)}
                            className="w-8 h-8 p-0"
                          >
                            {page}
                          </Button>
                        );
                      })}
                    </div>
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                      disabled={currentPage === totalPages}
                    >
                      Sau
                    </Button>
                  </div>
                </div>
              )}
            </CardContent>
          </Card>
        </TabsContent>
      </Tabs>
    </div>
  );
}