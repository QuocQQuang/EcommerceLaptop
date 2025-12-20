using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services.Security;

public class AuditLoggingService : IAuditLoggingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AuditLoggingService> _logger;

    public AuditLoggingService(ApplicationDbContext context, ILogger<AuditLoggingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public Task<ServiceResult<bool>> LogUserActivityAsync(int userId, string activity, string details, string ipAddress, string userAgent)
    {
        try
        {
            // Log to Serilog (which goes to Loki)
            _logger.LogInformation("Details: {Details} | UserActivity: {Activity} | UserId: {UserId} | IP: {IPAddress} | UserAgent: {UserAgent}", 
                details, activity, userId, ipAddress, userAgent);

            return Task.FromResult(ServiceResult<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log user activity");
            return Task.FromResult(ServiceResult<bool>.Failure("Failed to log user activity"));
        }
    }

    public Task<ServiceResult<bool>> LogAdminActivityAsync(int adminId, string activity, string details, string ipAddress, string userAgent, string? targetResource = null)
    {
        try
        {
            // Log to Serilog
            _logger.LogInformation("Details: {Details} | AdminActivity: {Activity} | AdminId: {AdminId} | Target: {TargetResource} | IP: {IPAddress}", 
                details, activity, adminId, targetResource ?? "N/A", ipAddress);

            return Task.FromResult(ServiceResult<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log admin activity");
            return Task.FromResult(ServiceResult<bool>.Failure("Failed to log admin activity"));
        }
    }

    public Task<ServiceResult<bool>> LogSystemEventAsync(string eventType, string description, string? correlationId = null, Dictionary<string, object>? metadata = null)
    {
        try
        {
            _logger.LogInformation("SystemEvent: {EventType} | Description: {Description} | CorrelationId: {CorrelationId} | Metadata: {Metadata}",
                eventType, description, correlationId ?? "N/A", metadata != null ? System.Text.Json.JsonSerializer.Serialize(metadata) : "N/A");

            return Task.FromResult(ServiceResult<bool>.Success(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log system event");
            return Task.FromResult(ServiceResult<bool>.Failure("Failed to log system event"));
        }
    }

    public async Task<ServiceResult<bool>> LogSecurityEventAsync(string eventType, string description, string ipAddress, int? userId = null, int? adminId = null, string? correlationId = null)
    {
        try
        {
            _logger.LogWarning("SecurityEvent: {EventType} | Description: {Description} | IP: {IPAddress} | UserId: {UserId} | AdminId: {AdminId}",
                eventType, description, ipAddress, userId, adminId);

            // Security events now handled by Serilog/Loki
            // _logger.LogWarning already sends to Loki
            
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log security event");
            return ServiceResult<bool>.Failure("Failed to log security event");
        }
    }

    public async Task<ServiceResult<Dictionary<string, int>>> GetActivitySummaryAsync(DateTime from, DateTime to, string? userType = null)
    {
        return await Task.FromResult(ServiceResult<Dictionary<string, int>>.Success(new Dictionary<string, int>()));
    }

    public async Task<ServiceResult<bool>> DeleteOldLogsAsync(DateTime olderThan)
    {
        try
        {
            // Only clean up SecurityEvents
            // Loki handles retention. No-op for SQL.
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete old logs");
            return ServiceResult<bool>.Failure("Failed to delete old logs");
        }
    }

    public async Task LogEventAsync(string entityType, string entityId, string eventType, string eventCategory, object? oldValues, object? newValues, string? userId, string? adminUserId, string? ipAddress, string? userAgent, object? metadata)
    {
        try
        {
            _logger.LogInformation("Event: {EventType} | Entity: {EntityType} | Id: {EntityId} | Category: {EventCategory}", 
                eventType, entityType, entityId, eventCategory);
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log event");
        }
    }
}
