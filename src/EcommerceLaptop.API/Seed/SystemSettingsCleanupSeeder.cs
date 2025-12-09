using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Seed;

public static class SystemSettingsCleanupSeeder
{
    public static void MigrateKeysToSnakeCase(ApplicationDbContext context, ILogger logger)
    {
        var keyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "EnableTwoFactorAuth", "enable_2fa" },
            { "PasswordMinLength", "password_min_length" },
            { "PasswordRequireSpecialChars", "password_require_special_chars" },
            { "EnableRateLimiting", "enable_rate_limiting" },
            { "DefaultRequestsPerMinute", "default_requests_per_minute" },
            { "DefaultRequestsPerHour", "default_requests_per_hour" },
            { "DefaultRequestsPerDay", "default_requests_per_day" },
            { "RateLimitViolationThreshold", "rate_limit_violation_threshold" },
            { "AutoBlockDurationMinutes", "auto_block_duration_minutes" },
            { "EnableIPBlocking", "enable_ip_blocking" },
            { "EnableCIDRSupport", "enable_cidr_support" },
            { "EnableGeoBlocking", "enable_geo_blocking" },
            { "BlockedCountries", "blocked_countries" },
            { "WhitelistCountries", "whitelist_countries" },
            { "EnableTorBlocking", "enable_tor_blocking" },
            { "EnableProxyBlocking", "enable_proxy_blocking" },
            { "EnableRealTimeMonitoring", "enable_real_time_monitoring" },
            { "SecurityAlertThreshold", "security_alert_threshold" },
            { "EnableEmailAlerts", "enable_email_alerts" },
            { "AlertEmailRecipients", "alert_email_recipients" },
            { "EnableSlackAlerts", "enable_slack_alerts" },
            { "SlackWebhookUrl", "slack_webhook_url" },
            { "EnableDashboardAlerts", "enable_dashboard_alerts" },
            { "DashboardRefreshIntervalSeconds", "dashboard_refresh_interval_seconds" },
            { "EnableAPIVersioning", "enable_api_versioning" },
            { "APIVersion", "api_version" },
            { "EnableAPIKeyAuth", "enable_api_key_auth" },
            { "APIKeyHeaderName", "api_key_header_name" },
            { "EnableRequestLogging", "enable_request_logging" },
            { "LogRequestBody", "log_request_body" },
            { "LogResponseBody", "log_response_body" },
            { "MaxRequestSizeMB", "max_request_size_mb" },
            { "SessionTimeoutMinutes", "session_timeout_minutes" },
            { "MaxConcurrentSessions", "max_concurrent_sessions" },
            { "EnableSessionRotation", "enable_session_rotation" },
            { "SessionRotationIntervalMinutes", "session_rotation_interval_minutes" },
            { "EnableConcurrentSessionKickout", "enable_concurrent_session_kickout" },
            { "SessionCookieSecure", "session_cookie_secure" },
            { "SessionCookieHttpOnly", "session_cookie_http_only" },
            { "SessionCookieSameSite", "session_cookie_same_site" },
            { "EnableBruteForceDetection", "enable_brute_force_detection" },
            { "BruteForceThreshold", "brute_force_threshold" },
            { "BruteForceTimeWindowMinutes", "brute_force_time_window_minutes" },
            { "EnableDDoSDetection", "enable_ddos_detection" },
            { "DDoSThreshold", "ddos_threshold" },
            { "DDoSTimeWindowMinutes", "ddos_time_window_minutes" },
            { "EnableSuspiciousActivityDetection", "enable_suspicious_activity_detection" },
            { "SuspiciousActivityScore", "suspicious_activity_score" },
            { "EnableGDPRCompliance", "enable_gdpr_compliance" },
            { "DataRetentionDays", "data_retention_days" },
            { "EnableDataAnonymization", "enable_data_anonymization" },
            { "AnonymizationDelayDays", "anonymization_delay_days" },
            { "EnableAuditTrail", "enable_audit_trail" },
            { "AuditTrailRetentionDays", "audit_trail_retention_days" },
            { "EnableConsentTracking", "enable_consent_tracking" },
            { "ConsentExpiryDays", "consent_expiry_days" },
            { "EnableRedisCaching", "enable_redis_caching" },
            { "RedisConnectionString", "redis_connection_string" },
            { "CacheExpirationMinutes", "cache_expiration_minutes" },
            { "EnableDatabaseConnectionPooling", "enable_database_connection_pooling" },
            { "MaxConnectionPoolSize", "max_connection_pool_size" },
            { "EnableQueryOptimization", "enable_query_optimization" },
            { "SlowQueryThresholdMs", "slow_query_threshold_ms" },
            { "EnableCompression", "enable_compression" },
            { "EnableDebugMode", "enable_debug_mode" },
            { "EnableDetailedLogging", "enable_detailed_logging" },
            { "EnableTestEndpoints", "enable_test_endpoints" },
            { "TestEndpointPrefix", "test_endpoint_prefix" },
            { "EnableMockServices", "enable_mock_services" },
            { "EnablePerformanceProfiling", "enable_performance_profiling" },
            { "ProfilingSampleRate", "profiling_sample_rate" },
            { "EnableSecurityTesting", "enable_security_testing" }
        };

        using var transaction = context.Database.BeginTransaction();
        try
        {
            var settings = context.SystemSettings.AsNoTracking().ToList();
            var totalRenamed = 0;
            var totalDeleted = 0;

            foreach (var s in settings)
            {
                if (keyMap.TryGetValue(s.SettingKey, out var canonical))
                {
                    var existsCanonical = context.SystemSettings.AsNoTracking().Any(x => x.SettingKey == canonical);
                    if (!existsCanonical)
                    {
                        var tracked = context.SystemSettings.First(x => x.Id == s.Id);
                        tracked.SettingKey = canonical;
                        tracked.UpdatedAt = DateTime.UtcNow;
                        totalRenamed++;
                    }
                    else
                    {
                        var dupe = context.SystemSettings.First(x => x.Id == s.Id);
                        context.SystemSettings.Remove(dupe);
                        totalDeleted++;
                    }
                }
            }

            context.SaveChanges();
            transaction.Commit();
            logger.LogInformation("SystemSettings cleanup completed. Renamed: {Renamed}, Deleted duplicates: {Deleted}", totalRenamed, totalDeleted);
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            logger.LogError(ex, "Error during SystemSettings key migration");
            throw;
        }
    }
}


