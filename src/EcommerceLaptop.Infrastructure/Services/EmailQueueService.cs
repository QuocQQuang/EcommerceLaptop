using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Configuration;

namespace EcommerceLaptop.Infrastructure.Services
{
    /// <summary>
    /// Redis-based email queue service for managing email delivery
    /// </summary>
    public class EmailQueueService : IEmailQueueService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<EmailQueueService> _logger;
        private readonly EmailSettings _emailSettings;
        private readonly string QUEUE_KEY_PREFIX = "email_queue";

        public EmailQueueService(
            IDistributedCache cache,
            ILogger<EmailQueueService> logger,
            IOptions<EmailSettings> emailSettings)
        {
            _cache = cache;
            _logger = logger;
            _emailSettings = emailSettings.Value;
        }

        public async Task<string> QueueEmailAsync(string to, string subject, string body, bool isHtml = true,
            EmailPriority priority = EmailPriority.Normal, DateTime? scheduleAt = null)
        {
            try
            {
                var emailItem = new EmailQueueItem
                {
                    Id = Guid.NewGuid().ToString(),
                    ToEmail = to,
                    Subject = subject,
                    HtmlContent = body,
                    Priority = priority,
                    Status = EmailDeliveryStatusType.Pending,
                    CreatedAt = DateTime.UtcNow,
                    ScheduledAt = scheduleAt
                };

                var cacheKey = GetQueueKey(priority, emailItem.Id);
                var serializedEmail = JsonSerializer.Serialize(emailItem);

                await _cache.SetStringAsync(cacheKey, serializedEmail, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromDays(7)
                });

                _logger.LogInformation("Email queued with ID {EmailId} for {ToEmail}", emailItem.Id, to);
                return emailItem.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue email to {ToEmail}", to);
                throw;
            }
        }

        public async Task<string> QueueTemplatedEmailAsync(string to, string subject, string templateName,
            Dictionary<string, object> templateData, EmailPriority priority = EmailPriority.Normal,
            DateTime? scheduleAt = null)
        {
            try
            {
                var emailItem = new EmailQueueItem
                {
                    Id = Guid.NewGuid().ToString(),
                    ToEmail = to,
                    Subject = subject,
                    TemplateName = templateName,
                    TemplateData = templateData,
                    Priority = priority,
                    Status = EmailDeliveryStatusType.Pending,
                    CreatedAt = DateTime.UtcNow,
                    ScheduledAt = scheduleAt
                };

                var cacheKey = GetQueueKey(priority, emailItem.Id);
                var serializedEmail = JsonSerializer.Serialize(emailItem);

                await _cache.SetStringAsync(cacheKey, serializedEmail, new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromDays(7)
                });

                _logger.LogInformation("Templated email queued with ID {EmailId} for {ToEmail}", emailItem.Id, to);
                return emailItem.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue templated email to {ToEmail}", to);
                throw;
            }
        }

        public async Task<List<EmailQueueItem>> GetPendingEmailsAsync(EmailPriority priority, int batchSize = 10)
        {
            try
            {
                var emails = new List<EmailQueueItem>();
                var searchPattern = GetQueueKeyPattern(priority);

                // Simple implementation - in production you'd use Redis SCAN or similar
                // For now, just return empty list as placeholder
                //_logger.LogInformation("Getting pending emails for priority {Priority}", priority);
                return emails;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get pending emails for priority {Priority}", priority);
                return new List<EmailQueueItem>();
            }
        }

        public async Task MarkAsProcessedAsync(string queueId)
        {
            try
            {
                // Implementation would update email status to processed
                _logger.LogInformation("Marking email {QueueId} as processed", queueId);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark email {QueueId} as processed", queueId);
            }
        }

        public async Task MarkAsFailedAsync(string queueId, string error)
        {
            try
            {
                // Implementation would update email status to failed
                _logger.LogWarning("Marking email {QueueId} as failed: {Error}", queueId, error);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark email {QueueId} as failed", queueId);
            }
        }

        public async Task<List<EmailQueueItem>> GetFailedEmailsAsync()
        {
            try
            {
                // Implementation would return failed emails
                return new List<EmailQueueItem>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get failed emails");
                return new List<EmailQueueItem>();
            }
        }

        public async Task RetryFailedEmailAsync(string queueId)
        {
            try
            {
                _logger.LogInformation("Retrying failed email {QueueId}", queueId);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retry email {QueueId}", queueId);
            }
        }

        public async Task<EmailQueueStats> GetQueueStatsAsync()
        {
            try
            {
                return new EmailQueueStats
                {
                    PendingCount = 0,
                    ProcessingCount = 0,
                    SentCount = 0,
                    FailedCount = 0,
                    ScheduledCount = 0,
                    PendingByPriority = new Dictionary<EmailPriority, int>(),
                    LastProcessedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get queue stats");
                throw;
            }
        }

        public async Task CleanupOldEmailsAsync(int olderThanDays = 30)
        {
            try
            {
                _logger.LogInformation("Cleaning up emails older than {Days} days", olderThanDays);
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup old emails");
            }
        }

        private string GetQueueKey(EmailPriority priority, string emailId)
        {
            return $"{QUEUE_KEY_PREFIX}_{priority.ToString().ToLower()}_{emailId}";
        }

        private string GetQueueKeyPattern(EmailPriority priority)
        {
            return $"{QUEUE_KEY_PREFIX}_{priority.ToString().ToLower()}_*";
        }
    }
}