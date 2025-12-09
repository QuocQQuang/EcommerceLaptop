using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services
{
    /// <summary>
    /// Interface for email queue management service
    /// </summary>
    public interface IEmailQueueService
    {
        /// <summary>
        /// Add email to queue for processing
        /// </summary>
        /// <param name="to">Recipient email</param>
        /// <param name="subject">Email subject</param>
        /// <param name="body">Email body</param>
        /// <param name="isHtml">Whether body is HTML</param>
        /// <param name="priority">Email priority</param>
        /// <param name="scheduleAt">When to send (null for immediate)</param>
        /// <returns>Queue item ID</returns>
        Task<string> QueueEmailAsync(string to, string subject, string body, bool isHtml = true,
            EmailPriority priority = EmailPriority.Normal, DateTime? scheduleAt = null);

        /// <summary>
        /// Add templated email to queue
        /// </summary>
        /// <param name="to">Recipient email</param>
        /// <param name="subject">Email subject</param>
        /// <param name="templateName">Template name</param>
        /// <param name="templateData">Template data</param>
        /// <param name="priority">Email priority</param>
        /// <param name="scheduleAt">When to send</param>
        /// <returns>Queue item ID</returns>
        Task<string> QueueTemplatedEmailAsync(string to, string subject, string templateName,
            Dictionary<string, object> templateData, EmailPriority priority = EmailPriority.Normal,
            DateTime? scheduleAt = null);

        /// <summary>
        /// Get pending emails from queue
        /// </summary>
        /// <param name="priority">Priority to process</param>
        /// <param name="batchSize">Number of emails to get</param>
        /// <returns>List of email queue items</returns>
        Task<List<EmailQueueItem>> GetPendingEmailsAsync(EmailPriority priority, int batchSize = 10);

        /// <summary>
        /// Mark email as processed successfully
        /// </summary>
        /// <param name="queueId">Queue item ID</param>
        Task MarkAsProcessedAsync(string queueId);

        /// <summary>
        /// Mark email as failed and schedule retry
        /// </summary>
        /// <param name="queueId">Queue item ID</param>
        /// <param name="error">Error message</param>
        Task MarkAsFailedAsync(string queueId, string error);

        /// <summary>
        /// Get failed emails for manual processing
        /// </summary>
        /// <returns>List of failed email queue items</returns>
        Task<List<EmailQueueItem>> GetFailedEmailsAsync();

        /// <summary>
        /// Retry failed email
        /// </summary>
        /// <param name="queueId">Queue item ID</param>
        Task RetryFailedEmailAsync(string queueId);

        /// <summary>
        /// Get queue statistics
        /// </summary>
        /// <returns>Queue statistics</returns>
        Task<EmailQueueStats> GetQueueStatsAsync();

        /// <summary>
        /// Clean up old processed/failed emails
        /// </summary>
        /// <param name="olderThanDays">Delete items older than this many days</param>
        Task CleanupOldEmailsAsync(int olderThanDays = 30);
    }

    /// <summary>
    /// Email queue statistics
    /// </summary>
    public class EmailQueueStats
    {
        public int PendingCount { get; set; }
        public int ProcessingCount { get; set; }
        public int SentCount { get; set; }
        public int FailedCount { get; set; }
        public int ScheduledCount { get; set; }
        public Dictionary<EmailPriority, int> PendingByPriority { get; set; } = new();
        public DateTime LastProcessedAt { get; set; }
    }
}