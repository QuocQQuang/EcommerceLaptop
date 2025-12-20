// Admin Security Management Service - Comprehensive security operations

import { adminApiClient } from '@/lib/adminApi';
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

/**
 * Admin Security Management Service Class
 * 
 * Provides comprehensive security management functionality:
 * - IP blocking and whitelist management
 * - Rate limiting rule configuration
 * - Security event monitoring and investigation
 * - Audit log analysis and reporting
 * - Threat intelligence and response
 * - Security policy enforcement
 * 
 * Critical security operations with enhanced logging and validation.
 */
export class AdminSecurityService {
  private readonly client = adminApiClient;

  // ===========================================================================
  //  IP BLOCKING MANAGEMENT
  // ===========================================================================

  private unwrap<T>(payload: any): T {
    // Supports ApiResponse<T>, paginated { items }, or direct T
    if (payload && typeof payload === 'object') {
      if ('data' in payload && payload.data !== undefined) return payload.data as T;
      if ('items' in payload && Array.isArray(payload.items)) return payload.items as T;
    }
    return payload as T;
  }

  /**
   * Get all IP blocking rules
   */
  async getIPBlockRules(): Promise<IPBlockRule[]> {
    const response = await this.client.get(
      '/admin/security/ip-blocks'
    );
    return this.unwrap<IPBlockRule[]>(response.data);
  }

  /**
   * Create new IP blocking rule
   */
  async createIPBlockRule(ruleData: CreateIPBlockRuleRequest): Promise<IPBlockRule> {
    // Backend supports quick block via /block-ip and full create via /ip-rules
    const response = await this.client.post(
      '/admin/security/ip-rules',
      ruleData
    );
    return this.unwrap<IPBlockRule>(response.data);
  }

  /**
   * Update existing IP blocking rule
   */
  async updateIPBlockRule(ruleId: number, ruleData: Partial<CreateIPBlockRuleRequest>): Promise<IPBlockRule> {
    const response = await this.client.put(
      `/admin/security/ip-rules/${ruleId}`,
      ruleData
    );
    return this.unwrap<IPBlockRule>(response.data);
  }

  /**
   * Delete IP blocking rule
   */
  async deleteIPBlockRule(ruleId: number): Promise<boolean> {
    const response = await this.client.delete(
      `/admin/security/ip-rules/${ruleId}`
    );
    const data = response.data as any;
    if (typeof data?.success === 'boolean') return data.success;
    return true;
  }

  // ===========================================================================
  //  RATE LIMITING MANAGEMENT  
  // ===========================================================================

  /**
   * Get all rate limiting rules
   */
  async getRateLimitRules(): Promise<RateLimitRule[]> {
    const response = await this.client.get(
      '/admin/security/rate-limit-rules'
    );
    return this.unwrap<RateLimitRule[]>(response.data);
  }

  /**
   * Create new rate limiting rule
   */
  async createRateLimitRule(ruleData: CreateRateLimitRuleRequest): Promise<RateLimitRule> {
    const response = await this.client.post(
      '/admin/security/rate-limit-rules',
      ruleData
    );
    // Backend returns { success, data } or direct entity depending on wrapper; unwrap handles both
    return this.unwrap<RateLimitRule>(response.data);
  }

  /**
   * Update rate limiting rule
   */
  async updateRateLimitRule(ruleId: number, ruleData: Partial<CreateRateLimitRuleRequest>): Promise<RateLimitRule> {
    const response = await this.client.put(
      `/admin/security/rate-limit-rules/${ruleId}`,
      ruleData
    );
    return this.unwrap<RateLimitRule>(response.data);
  }

  /**
   * Delete rate limiting rule
   */
  async deleteRateLimitRule(ruleId: number): Promise<boolean> {
    const response = await this.client.delete(
      `/admin/security/rate-limit-rules/${ruleId}`
    );
    const data = response.data as any;
    if (typeof data?.success === 'boolean') return data.success;
    return true;
  }

  // ===========================================================================
  //  SECURITY EVENT MONITORING
  // ===========================================================================

  /**
   * Get paginated security events
   */
  async getSecurityEvents(params: SecurityEventsParams = {}): Promise<SecurityEventsResponse> {
    const response = await this.client.get(
      '/admin/security/security-events',
      { params }
    );
    return this.unwrap<SecurityEventsResponse>(response.data);
  }

  /**
   * Get security event details
   */
  async getSecurityEventById(eventId: number): Promise<SecurityEvent> {
    const response = await this.client.get(
      `/admin/security/events/${eventId}`
    );
    return this.unwrap<SecurityEvent>(response.data);
  }

  /**
   * Investigate security event
   */
  async investigateSecurityEvent(eventId: number, investigationData: InvestigateEventRequest): Promise<SecurityEvent> {
    const response = await this.client.post(
      `/admin/security/events/${eventId}/investigate`,
      investigationData
    );
    return this.unwrap<SecurityEvent>(response.data);
  }

  /**
   * Resolve security event
   */
  async resolveSecurityEvent(eventId: number, resolutionData: ResolveEventRequest): Promise<SecurityEvent> {
    const response = await this.client.post(
      `/admin/security/events/${eventId}/resolve`,
      resolutionData
    );
    return this.unwrap<SecurityEvent>(response.data);
  }

  // Audit Logs methods removed as SystemAuditLogs table is deprecated.
  // Use Security Events for security-related logging.

  // ===========================================================================
  //  SECURITY ANALYTICS & REPORTS
  // ===========================================================================

  /**
   * Get security dashboard metrics
   */
  async getSecurityMetrics(days: number = 7): Promise<SecurityMetrics> {
    const response = await this.client.get(
      '/admin/security/metrics',
      { params: { days } }
    );
    return this.unwrap<SecurityMetrics>(response.data);
  }
}

export const adminSecurityService = new AdminSecurityService();