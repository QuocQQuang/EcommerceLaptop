using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Infrastructure.Services.Security;

/// <summary>
/// Service for security event tracking and correlation
/// </summary>
public interface ISecurityEventService
{
    Task<ServiceResult<bool>> LogEventAsync(string eventType, string description, int? userId = null, int? adminUserId = null, string? ipAddress = null, string? userAgent = null, string? correlationId = null, Dictionary<string, object>? metadata = null);
    Task<ServiceResult<List<SecurityEvent>>> GetEventsAsync(string? eventType = null, DateTime? from = null, DateTime? to = null, int? userId = null, int? adminUserId = null, int page = 1, int pageSize = 50);
    Task<ServiceResult<List<SecurityEvent>>> GetEventsByCorrelationAsync(string correlationId);
    Task<ServiceResult<Dictionary<string, int>>> GetEventStatisticsAsync(DateTime from, DateTime to);
    Task<ServiceResult<bool>> MarkEventInvestigatedAsync(int eventId, int adminUserId, string notes);
    Task<ServiceResult<List<SecurityEvent>>> GetSuspiciousActivityAsync(DateTime from, DateTime to, int threshold = 10);
    Task<ServiceResult<bool>> CreateAlertRuleAsync(string name, string eventType, int threshold, TimeSpan window, string? description = null, int? adminUserId = null);
    Task<ServiceResult<bool>> DeleteOldEventsAsync(DateTime olderThan);

    // Aggregated security metrics for admin dashboard
    Task<ServiceResult<EcommerceLaptop.Core.DTOs.Admin.SecurityMetricsDto>> GetSecurityMetricsAsync(int days = 7);

    // Additional queries used by admin controller
    Task<ServiceResult<List<SecurityEvent>>> GetEventsByIPAsync(string ipAddress, int hours = 24);
    Task<ServiceResult<List<SecurityEvent>>> GetRelatedEventsAsync(int eventId);
}