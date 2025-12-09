using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.DTOs.Admin;

/// <summary>
/// Admin logs response DTO for dashboard logs view
/// </summary>
public class AdminLogsResponseDto
{
    public List<AdminLogEntryDto> Logs { get; set; } = new List<AdminLogEntryDto>();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public LogsFilterDto? Filters { get; set; }
    public LogsSummaryDto? Summary { get; set; }
}

/// <summary>
/// Individual log entry for admin dashboard
/// </summary>
public class AdminLogEntryDto
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string EventCategory { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? AdminId { get; set; }
    public string? AdminName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Severity { get; set; } = "Info";
    public Dictionary<string, object>? Metadata { get; set; }
    public string? TargetResource { get; set; }
    public bool? IsInvestigated { get; set; }
}

/// <summary>
/// Security event DTO specifically for security logs
/// </summary>
public class SecurityEventLogDto
{
    public int Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public int? AdminUserId { get; set; }
    public string? AdminUserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsInvestigated { get; set; }
    public string? InvestigationNotes { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
    public string RiskLevel { get; set; } = "Low"; // Low, Medium, High, Critical
}

/// <summary>
/// System audit log DTO for system activity logs
/// </summary>
public class SystemAuditLogDto
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventCategory { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? AdminUserId { get; set; }
    public string? AdminUserName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}

/// <summary>
/// Filters for log queries
/// </summary>
public class LogsFilterDto
{
    public string? EventType { get; set; }
    public string? EventCategory { get; set; }
    public string? UserId { get; set; }
    public string? AdminId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Severity { get; set; }
    public bool? OnlyInvestigated { get; set; }
    public string? SearchTerm { get; set; }
}

/// <summary>
/// Summary statistics for logs
/// </summary>
public class LogsSummaryDto
{
    public int TotalEvents { get; set; }
    public int SecurityEvents { get; set; }
    public int AuditEvents { get; set; }
    public int UninvestigatedEvents { get; set; }
    public int CriticalEvents { get; set; }
    public int WarningEvents { get; set; }
    public Dictionary<string, int> EventTypeCounts { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, int> UserActivityCounts { get; set; } = new Dictionary<string, int>();
}

/// <summary>
/// Query parameters for logs endpoint
/// </summary>
public class LogsQueryParametersDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? LogType { get; set; } = "all"; // "security", "audit", "all"
    public string? EventType { get; set; }
    public string? EventCategory { get; set; }
    public string? UserId { get; set; }
    public string? AdminId { get; set; }
    public string? IpAddress { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string? Severity { get; set; }
    public bool? OnlyInvestigated { get; set; }
    public string? SearchTerm { get; set; }
    public string SortBy { get; set; } = "CreatedAt";
    public string SortDirection { get; set; } = "desc";
}