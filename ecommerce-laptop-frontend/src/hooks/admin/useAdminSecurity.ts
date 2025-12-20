// Admin Security Management React Query Hooks

import { adminSecurityService } from '@/services/adminSecurityService';
import {
  AuditLog,
  AuditLogsParams,
  AuditLogsResponse,
  CreateIPBlockRuleRequest,
  CreateRateLimitRuleRequest,
  InvestigateEventRequest,
  IPBlockRule,
  RateLimitRule,
  ResolveEventRequest,
  SecurityEvent,
  SecurityEventsParams,
  SecurityEventsResponse,
  SecurityMetrics
} from '@/types/admin';
import { useMutation, UseMutationOptions, useQuery, useQueryClient, UseQueryOptions } from '@tanstack/react-query';
import { toast } from 'sonner';

/**
 * Query Keys for Admin Security Management
 * 
 * Organized query keys for security-related data caching:
 * - IP blocking rules and management
 * - Rate limiting configurations
 * - Security events and investigations
 * - Audit logs and compliance reports
 */
export const adminSecurityQueryKeys = {
  all: ['admin', 'security'] as const,

  // IP Blocking
  ipBlocks: () => [...adminSecurityQueryKeys.all, 'ip-blocks'] as const,

  // Rate Limiting
  rateLimits: () => [...adminSecurityQueryKeys.all, 'rate-limits'] as const,

  // Security Events
  events: () => [...adminSecurityQueryKeys.all, 'events'] as const,
  eventList: (params: SecurityEventsParams) => [...adminSecurityQueryKeys.events(), 'list', params] as const,
  event: (id: number) => [...adminSecurityQueryKeys.events(), 'detail', id] as const,

  // Audit Logs
  auditLogs: () => [...adminSecurityQueryKeys.all, 'audit-logs'] as const,
  auditLogList: (params: AuditLogsParams) => [...adminSecurityQueryKeys.auditLogs(), 'list', params] as const,
  auditLog: (id: number) => [...adminSecurityQueryKeys.auditLogs(), 'detail', id] as const,

  // Security Metrics
  metrics: () => [...adminSecurityQueryKeys.all, 'metrics'] as const,
};

// =============================================================================
//  IP BLOCKING HOOKS
// =============================================================================

/**
 * Hook: useIPBlockRules
 * 
 * Fetches all IP blocking rules with real-time updates.
 * 
 * Features:
 * - Active blacklist and whitelist rules
 * - Expiration tracking and notifications
 * - Threat level analytics
 * - Geographic insights
 * - Rule effectiveness metrics
 * 
 * @param options React Query configuration options
 * @returns Query state with IP blocking rules
 * 
 * @example
 * ```typescript
 * const { data: ipRules, isLoading } = useIPBlockRules();
 * 
 * return (
 *   <IPBlockTable
 *     rules={ipRules}
 *     onBlock={handleBlockIP}
 *     onUnblock={handleUnblockIP}
 *   />
 * );
 * ```
 */
export function useIPBlockRules(
  options?: UseQueryOptions<IPBlockRule[], Error>
) {
  return useQuery({
    queryKey: adminSecurityQueryKeys.ipBlocks(),
    queryFn: () => adminSecurityService.getIPBlockRules(),
    staleTime: 2 * 60 * 1000, // 2 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
    refetchInterval: 5 * 60 * 1000, // Auto-refresh every 5 minutes
    ...options,
  });
}

/**
 * Hook: useCreateIPBlockRule
 * 
 * Mutation for creating new IP blocking rules with validation.
 * 
 * @param options Mutation configuration options
 * @returns Mutation state with create function
 */
export function useCreateIPBlockRule(
  options?: UseMutationOptions<IPBlockRule, Error, CreateIPBlockRuleRequest>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (ruleData: CreateIPBlockRuleRequest) => adminSecurityService.createIPBlockRule(ruleData),
    onSuccess: (newRule) => {
      queryClient.invalidateQueries({ queryKey: adminSecurityQueryKeys.ipBlocks() });
      toast.success(`IP ${newRule.ipAddress} ${newRule.type === 'blacklist' ? 'blocked' : 'whitelisted'} successfully`);
    },
    onError: (error) => {
      console.error('Failed to create IP block rule:', error);
      toast.error(`Failed to create IP block rule: ${error.message}`);
    },
    ...options,
  });
}

/**
 * Hook: useDeleteIPBlockRule
 * 
 * Mutation for deleting IP blocking rules.
 */
export function useDeleteIPBlockRule(
  options?: UseMutationOptions<boolean, Error, number>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (ruleId: number) => adminSecurityService.deleteIPBlockRule(ruleId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: adminSecurityQueryKeys.ipBlocks() });
      toast.success('IP block rule removed successfully');
    },
    onError: (error) => {
      console.error('Failed to delete IP block rule:', error);
      toast.error(`Failed to delete IP block rule: ${error.message}`);
    },
    ...options,
  });
}

// =============================================================================
//  RATE LIMITING HOOKS
// =============================================================================

/**
 * Hook: useRateLimitRules
 * 
 * Fetches all rate limiting rules with usage analytics.
 * 
 * @param options React Query configuration options
 * @returns Query state with rate limiting rules
 */
export function useRateLimitRules(
  options?: UseQueryOptions<RateLimitRule[], Error>
) {
  return useQuery({
    queryKey: adminSecurityQueryKeys.rateLimits(),
    queryFn: () => adminSecurityService.getRateLimitRules(),
    staleTime: 5 * 60 * 1000, // 5 minutes
    gcTime: 15 * 60 * 1000, // 15 minutes
    ...options,
  });
}

/**
 * Hook: useCreateRateLimitRule
 * 
 * Mutation for creating new rate limiting rules.
 */
export function useCreateRateLimitRule(
  options?: UseMutationOptions<RateLimitRule, Error, CreateRateLimitRuleRequest>
) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (ruleData: CreateRateLimitRuleRequest) => adminSecurityService.createRateLimitRule(ruleData),
    onSuccess: (newRule) => {
      queryClient.invalidateQueries({ queryKey: adminSecurityQueryKeys.rateLimits() });
      toast.success(`Rate limit rule created for ${newRule.endpoint}`);
    },
    onError: (error) => {
      console.error('Failed to create rate limit rule:', error);
      toast.error(`Failed to create rate limit rule: ${error.message}`);
    },
    ...options,
  });
}

// =============================================================================
//  SECURITY EVENT HOOKS
// =============================================================================

/**
 * Hook: useSecurityEvents
 * 
 * Fetches paginated security events with advanced filtering.
 * 
 * Features:
 * - Real-time event monitoring
 * - Severity-based filtering
 * - Investigation status tracking
 * - User and IP correlation
 * - Time-based analysis
 * 
 * @param params Event filtering and pagination parameters
 * @param options React Query configuration options
 * @returns Query state with paginated security events
 * 
 * @example
 * ```typescript
 * const { data, isLoading } = useSecurityEvents({
 *   page: 1,
 *   limit: 20,
 *   severity: 'high',
 *   from: '2025-01-01'
 * });
 * 
 * const { events, totalCount } = data || {};
 * 
 * return (
 *   <SecurityEventTable
 *     events={events}
 *     totalCount={totalCount}
 *     onInvestigate={handleInvestigate}
 *     onResolve={handleResolve}
 *   />
 * );
 * ```
 */
export function useSecurityEvents(
  params: SecurityEventsParams = {},
  options?: UseQueryOptions<SecurityEventsResponse, Error>
) {
  return useQuery({
    queryKey: adminSecurityQueryKeys.eventList(params),
    queryFn: () => adminSecurityService.getSecurityEvents(params),
    staleTime: 1 * 60 * 1000, // 1 minute
    gcTime: 5 * 60 * 1000, // 5 minutes
    refetchInterval: 2 * 60 * 1000, // Auto-refresh every 2 minutes
    ...options,
  });
}

/**
 * Hook: useSecurityEvent
 * 
 * Fetches individual security event details.
 * 
 * @param eventId Security event identifier
 * @param options React Query configuration options
 * @returns Query state with detailed event information
 */
export function useSecurityEvent(
  eventId: number,
  options?: UseQueryOptions<SecurityEvent, Error>
) {
  return useQuery({
    queryKey: adminSecurityQueryKeys.event(eventId),
    queryFn: () => adminSecurityService.getSecurityEventById(eventId),
    staleTime: 5 * 60 * 1000, // 5 minutes
    enabled: !!eventId,
    ...options,
  });
}

/**
 * Hook: useInvestigateSecurityEvent
 * 
 * Mutation for investigating security events.
 * 
 * @param options Mutation configuration options
 * @returns Mutation state with investigate function
 */
export function useInvestigateSecurityEvent(
  options?: UseMutationOptions<SecurityEvent, Error, { eventId: number; data: InvestigateEventRequest }>
) {
  const queryClient = useQueryClient();

  return useMutation<SecurityEvent, Error, { eventId: number; data: InvestigateEventRequest }>({
    mutationFn: ({ eventId, data }) => adminSecurityService.investigateSecurityEvent(eventId, data),
    onSuccess: (updatedEvent, { eventId }) => {
      queryClient.setQueryData(adminSecurityQueryKeys.event(eventId), updatedEvent);
      queryClient.invalidateQueries({ queryKey: adminSecurityQueryKeys.events() });
      toast.success('Security event investigation started');
    },
    onError: (error) => {
      console.error('Failed to investigate security event:', error);
      toast.error(`Failed to investigate event: ${error.message}`);
    },
    ...options,
  });
}

/**
 * Hook: useResolveSecurityEvent
 * 
 * Mutation for resolving security events.
 * 
 * @param options Mutation configuration options
 * @returns Mutation state with resolve function
 */
export function useResolveSecurityEvent(
  options?: UseMutationOptions<SecurityEvent, Error, { eventId: number; data: ResolveEventRequest }>
) {
  const queryClient = useQueryClient();

  return useMutation<SecurityEvent, Error, { eventId: number; data: ResolveEventRequest }>({
    mutationFn: ({ eventId, data }) => adminSecurityService.resolveSecurityEvent(eventId, data),
    onSuccess: (resolvedEvent, { eventId }) => {
      queryClient.setQueryData(adminSecurityQueryKeys.event(eventId), resolvedEvent);
      queryClient.invalidateQueries({ queryKey: adminSecurityQueryKeys.events() });
      toast.success('Security event resolved successfully');
    },
    onError: (error) => {
      console.error('Failed to resolve security event:', error);
      toast.error(`Failed to resolve event: ${error.message}`);
    },
    ...options,
  });
}

// =============================================================================
//  SECURITY METRICS HOOKS
// =============================================================================

/**
 * Hook: useSecurityMetrics
 * 
 * Fetches comprehensive security dashboard metrics.
 * 
 * Features:
 * - Active threat indicators
 * - Security event statistics
 * - IP blocking effectiveness
 * - Rate limiting analytics
 * - Investigation progress tracking
 * 
 * @param options React Query configuration options
 * @returns Query state with security metrics
 */
export function useSecurityMetrics(
  options?: UseQueryOptions<SecurityMetrics, Error>
) {
  return useQuery({
    queryKey: adminSecurityQueryKeys.metrics(),
    queryFn: () => adminSecurityService.getSecurityMetrics(),
    staleTime: 2 * 60 * 1000, // 2 minutes
    gcTime: 10 * 60 * 1000, // 10 minutes
    refetchInterval: 3 * 60 * 1000, // Auto-refresh every 3 minutes
    ...options,
  });
}

/**
 * Utility: Security Query Invalidation
 * 
 * Helper functions for manual cache invalidation and refresh.
 */
export const invalidateAdminSecurity = {
  all: (queryClient: any) => {
    return queryClient.invalidateQueries({ queryKey: adminSecurityQueryKeys.all });
  },
  ipBlocks: (queryClient: any) => {
    return queryClient.invalidateQueries({ queryKey: adminSecurityQueryKeys.ipBlocks() });
  },
  rateLimits: (queryClient: any) => {
    return queryClient.invalidateQueries({ queryKey: adminSecurityQueryKeys.rateLimits() });
  },
  events: (queryClient: any) => {
    return queryClient.invalidateQueries({ queryKey: adminSecurityQueryKeys.events() });
  },
  metrics: (queryClient: any) => {
    return queryClient.invalidateQueries({ queryKey: adminSecurityQueryKeys.metrics() });
  },
};