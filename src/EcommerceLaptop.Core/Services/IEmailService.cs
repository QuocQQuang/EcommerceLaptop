using System.Threading.Tasks;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services
{
    /// <summary>
    /// Email priority levels
    /// </summary>
    public enum EmailPriority
    {
        /// <summary>
        /// Critical emails (security alerts, system failures)
        /// </summary>
        Urgent = 1,

        /// <summary>
        /// Important emails (password resets, order confirmations)
        /// </summary>
        High = 2,

        /// <summary>
        /// Normal emails (regular notifications)
        /// </summary>
        Normal = 3,

        /// <summary>
        /// Low priority emails (marketing, newsletters)
        /// </summary>
        Low = 4
    }

    /// <summary>
    /// Email delivery status types
    /// </summary>
    public enum EmailDeliveryStatusType
    {
        /// <summary>
        /// Email is queued and waiting to be sent
        /// </summary>
        Pending = 0,

        /// <summary>
        /// Email is being processed/sent
        /// </summary>
        Sending = 1,

        /// <summary>
        /// Email was sent successfully
        /// </summary>
        Sent = 2,

        /// <summary>
        /// Email delivery failed
        /// </summary>
        Failed = 3,

        /// <summary>
        /// Email was delivered to recipient
        /// </summary>
        Delivered = 4
    }

    public interface IEmailService
    {
        // Authentication & Account Management
        Task<bool> SendPasswordResetEmailAsync(User user, string callbackUrl);
        Task<bool> SendEmailConfirmationEmailAsync(User user, string callbackUrl);
        Task<bool> SendWelcomeEmailAsync(User user);
        Task<bool> SendPasswordChangedNotificationAsync(User user);

        // OTP & Two-Factor Authentication
        Task<bool> SendOtpEmailAsync(User user, string otpCode, string purpose = "verification");
        Task<bool> SendTwoFactorTokenEmailAsync(User user, string token);

        // Order & Transaction Related
        Task<bool> SendOrderConfirmationEmailAsync(User user, string orderId);
        Task<bool> SendOrderStatusUpdateEmailAsync(User user, string orderId, string newStatus);
        Task<bool> SendShippingNotificationEmailAsync(User user, string orderId, string trackingNumber);
        Task<bool> SendOrderCancellationEmailAsync(User user, string orderId, string reason);
        Task<bool> SendRefundNotificationEmailAsync(User user, string orderId, decimal refundAmount);
        Task<bool> SendPaymentSuccessEmailWithInvoiceAsync(User user, Order order, byte[] invoicePdf);

        // System Notifications
        Task<bool> SendAccountLockedNotificationAsync(User user, string reason);
        Task<bool> SendSecurityAlertEmailAsync(User user, string alertType, string details);
        Task<bool> SendNewsletterEmailAsync(User user, string subject, string content);
        Task<bool> SendPromotionalEmailAsync(User user, string subject, string content, string? promoCode = null);

        // Custom Email
        Task<bool> SendCustomEmailAsync(string toEmail, string toName, string subject, string htmlContent, string? textContent = null);
        Task<bool> SendBulkEmailAsync(List<EmailRecipient> recipients, string subject, string htmlContent, string? textContent = null);

        // Admin & Support
        Task<bool> SendAdminNotificationAsync(string subject, string content, string priority = "normal");
        Task<bool> SendSupportTicketEmailAsync(User user, string ticketNumber, string subject, string message);

        // Queue Management
        Task<bool> QueueEmailAsync(EmailQueueItem emailItem);
        Task ProcessEmailQueueAsync();
        Task<EmailDeliveryStatus> GetEmailStatusAsync(string emailId);
    }

    public class EmailRecipient
    {
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public Dictionary<string, object>? PersonalizationData { get; set; }
    }

    public class EmailQueueItem
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ToEmail { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string? TextContent { get; set; }
        public string EmailType { get; set; } = string.Empty;
        public Dictionary<string, object>? TemplateData { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ScheduledAt { get; set; }
        public int RetryCount { get; set; } = 0;
        public EmailPriority Priority { get; set; } = EmailPriority.Normal;
        public EmailDeliveryStatusType Status { get; set; } = EmailDeliveryStatusType.Pending;
        public DateTime? SentAt { get; set; }
        public int MaxRetries { get; set; } = 3;
        public string? ErrorMessage { get; set; }
        public string? TemplateName { get; set; }
        public string? TrackingId { get; set; }
    }

    /// <summary>
    /// Email delivery status result
    /// </summary>
    public class EmailDeliveryStatus
    {
        public string Id { get; set; } = string.Empty;
        public EmailDeliveryStatusType Status { get; set; } = EmailDeliveryStatusType.Pending;
        public DateTime? SentAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public string? Message { get; set; }
        public string? ErrorMessage { get; set; }
        public int RetryCount { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    }
}
