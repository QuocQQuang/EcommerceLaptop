using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Security.Cryptography;
using EcommerceLaptop.Core.Interfaces.Services;

namespace EcommerceLaptop.Infrastructure.Services
{
    public interface IEmailSecurityService
    {
        /// <summary>
        /// Encrypt email content before sending
        /// </summary>
        Task<string> EncryptEmailContentAsync(string content);

        /// <summary>
        /// Decrypt email content for processing
        /// </summary>
        Task<string> DecryptEmailContentAsync(string encryptedContent);

        /// <summary>
        /// Generate secure email token for verification
        /// </summary>
        Task<string> GenerateSecureTokenAsync(string purpose, int expiryMinutes = 30);

        /// <summary>
        /// Validate secure email token
        /// </summary>
        Task<bool> ValidateSecureTokenAsync(string token, string purpose);

        /// <summary>
        /// Hash email address for privacy
        /// </summary>
        string HashEmailAddress(string email);

        /// <summary>
        /// Generate OTP with specified length
        /// </summary>
        string GenerateOTP(int length = 6);

        /// <summary>
        /// Validate rate limiting for email sending
        /// </summary>
        Task<bool> CheckRateLimitAsync(string identifier, int maxRequests = 5, TimeSpan? timeWindow = null);

        /// <summary>
        /// Log email security event
        /// </summary>
        Task LogSecurityEventAsync(string eventType, string details, string? userId = null);
    }

    public class EmailSecurityService : IEmailSecurityService
    {
        private readonly IDataProtectionProvider _dataProtectionProvider;
        private readonly ILogger<EmailSecurityService> _logger;
        private readonly IDistributedCache _cache;
        private readonly IDataProtector _emailProtector;
        private readonly IDataProtector _tokenProtector;
        private readonly ISystemSettingsService _systemSettingsService;

        public EmailSecurityService(
            IDataProtectionProvider dataProtectionProvider,
            ILogger<EmailSecurityService> logger,
            IDistributedCache cache,
            ISystemSettingsService systemSettingsService)
        {
            _dataProtectionProvider = dataProtectionProvider;
            _logger = logger;
            _cache = cache;
            _systemSettingsService = systemSettingsService;
            _emailProtector = _dataProtectionProvider.CreateProtector("EmailContent");
            _tokenProtector = _dataProtectionProvider.CreateProtector("EmailTokens");
        }

        public Task<string> EncryptEmailContentAsync(string content)
        {
            try
            {
                if (string.IsNullOrEmpty(content))
                    return Task.FromResult(content);

                // Encrypt the content using Data Protection API
                var encrypted = _emailProtector.Protect(content);

                _logger.LogDebug("Email content encrypted successfully");
                return Task.FromResult(encrypted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to encrypt email content");
                throw new InvalidOperationException("Email encryption failed", ex);
            }
        }

        public Task<string> DecryptEmailContentAsync(string encryptedContent)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedContent))
                    return Task.FromResult(encryptedContent);

                // Decrypt the content using Data Protection API
                var decrypted = _emailProtector.Unprotect(encryptedContent);

                _logger.LogDebug("Email content decrypted successfully");
                return Task.FromResult(decrypted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decrypt email content");
                throw new InvalidOperationException("Email decryption failed", ex);
            }
        }

        public async Task<string> GenerateSecureTokenAsync(string purpose, int expiryMinutes = 30)
        {
            try
            {
                var tokenData = new
                {
                    Purpose = purpose,
                    CreatedAt = DateTimeOffset.UtcNow,
                    ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(expiryMinutes),
                    Nonce = Guid.NewGuid().ToString()
                };

                var jsonData = System.Text.Json.JsonSerializer.Serialize(tokenData);
                var protectedToken = _tokenProtector.Protect(jsonData);

                // Store token in cache for validation
                var cacheKey = $"email_token:{HashString(protectedToken)}";
                await _cache.SetStringAsync(cacheKey, purpose, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(expiryMinutes)
                });

                _logger.LogInformation("Secure email token generated for purpose: {Purpose}", purpose);
                return protectedToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate secure token for purpose: {Purpose}", purpose);
                throw new InvalidOperationException("Token generation failed", ex);
            }
        }

        public async Task<bool> ValidateSecureTokenAsync(string token, string purpose)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                    return false;

                // Check cache first
                var cacheKey = $"email_token:{HashString(token)}";
                var cachedPurpose = await _cache.GetStringAsync(cacheKey);

                if (cachedPurpose != purpose)
                    return false;

                // Validate token structure
                var jsonData = _tokenProtector.Unprotect(token);
                var tokenData = System.Text.Json.JsonSerializer.Deserialize<dynamic>(jsonData);

                _logger.LogInformation("Email token validated successfully for purpose: {Purpose}", purpose);

                // Remove token after validation (one-time use)
                await _cache.RemoveAsync(cacheKey);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Invalid email token validation attempt for purpose: {Purpose}", purpose);
                return false;
            }
        }

        public string HashEmailAddress(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(email.ToLowerInvariant()));
            return Convert.ToBase64String(hashedBytes)[..12]; // Take first 12 characters for readability
        }

        public string GenerateOTP(int length = 6)
        {
            const string chars = "0123456789";
            var random = new Random();
            var otp = new StringBuilder();

            for (int i = 0; i < length; i++)
            {
                otp.Append(chars[random.Next(chars.Length)]);
            }

            return otp.ToString();
        }

        public async Task<bool> CheckRateLimitAsync(string identifier, int maxRequests = 5, TimeSpan? timeWindow = null)
        {
            try
            {
                // Load overrides from SystemSettings
                var maxReqSetting = await _systemSettingsService.GetSettingValueAsync<int>("email_rate_limit_max_requests", maxRequests);
                var windowSeconds = await _systemSettingsService.GetSettingValueAsync<int>("email_rate_limit_window_seconds", (int)(timeWindow?.TotalSeconds ?? TimeSpan.FromMinutes(15).TotalSeconds));
                var window = TimeSpan.FromSeconds(windowSeconds);
                var effectiveMax = maxReqSetting > 0 ? maxReqSetting : maxRequests;
                var rateLimitKey = $"rate_limit:email:{HashString(identifier)}";

                var currentCountStr = await _cache.GetStringAsync(rateLimitKey);
                var currentCount = int.TryParse(currentCountStr, out var count) ? count : 0;

                if (currentCount >= effectiveMax)
                {
                    _logger.LogWarning("Rate limit exceeded for identifier: {HashedIdentifier}", HashString(identifier));
                    return false;
                }

                // Increment counter
                await _cache.SetStringAsync(rateLimitKey, (currentCount + 1).ToString(), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = window
                });

                _logger.LogDebug("Rate limit check passed. Count: {Count}/{Max} for identifier: {HashedIdentifier}",
                    currentCount + 1, effectiveMax, HashString(identifier));

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rate limit check failed for identifier: {HashedIdentifier}", HashString(identifier));
                // On error, allow the request to proceed
                return true;
            }
        }

        public async Task LogSecurityEventAsync(string eventType, string details, string? userId = null)
        {
            try
            {
                var securityEvent = new
                {
                    EventType = eventType,
                    Details = details,
                    UserId = userId,
                    Timestamp = DateTimeOffset.UtcNow,
                    Source = "EmailSecurityService"
                };

                var logEntry = System.Text.Json.JsonSerializer.Serialize(securityEvent);
                _logger.LogInformation("Email Security Event: {LogEntry}", logEntry);

                // Store in cache for audit trail (optional)
                var auditKey = $"email_security_audit:{DateTimeOffset.UtcNow:yyyyMMdd}:{Guid.NewGuid()}";
                await _cache.SetStringAsync(auditKey, logEntry, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log security event: {EventType}", eventType);
            }
        }

        private string HashString(string input)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(hashedBytes)[..16];
        }
    }
}