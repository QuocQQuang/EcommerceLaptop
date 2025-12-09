using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.Configuration
{
    public class EmailSettings
    {
        public SmtpSettings SmtpSettings { get; set; } = new();
        public SenderSettings SenderSettings { get; set; } = new();
        public TemplateSettings Templates { get; set; } = new();
        public RateLimitingSettings RateLimiting { get; set; } = new();
        public SecuritySettings Security { get; set; } = new();
        public QueueSettings Queue { get; set; } = new();
        public GmailSettings Gmail { get; set; } = new();
        public string DefaultFromEmail { get; set; } = string.Empty;
        public string DefaultFromName { get; set; } = string.Empty;
    }

    public class SmtpSettings
    {
        [Required]
        public string Host { get; set; } = string.Empty;

        [Range(1, 65535)]
        public int Port { get; set; } = 587;

        public bool EnableSsl { get; set; } = true;
        public bool UseDefaultCredentials { get; set; } = false;

        [Required]
        [EmailAddress]
        public string Username { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;

        [Range(1, 300)]
        public int TimeoutSeconds { get; set; } = 30;
    }

    public class SenderSettings
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [EmailAddress]
        public string? ReplyTo { get; set; }

        [EmailAddress]
        public string? NoReplyEmail { get; set; }
    }

    public class TemplateSettings
    {
        public string BasePath { get; set; } = "EmailTemplates";
        public string DefaultLanguage { get; set; } = "vi";
        public string[] SupportedLanguages { get; set; } = { "vi", "en" };
    }

    public class RateLimitingSettings
    {
        public int MaxEmailsPerMinute { get; set; } = 50;
        public int MaxEmailsPerHour { get; set; } = 300;
        public int MaxEmailsPerDay { get; set; } = 1000;
        public bool EnableRateLimiting { get; set; } = true;
    }

    public class SecuritySettings
    {
        public bool EnableEmailEncryption { get; set; } = true;
        public bool EnableEncryption { get; set; } = false;
        public int TokenExpirationMinutes { get; set; } = 1440; // 24 hours
        public int OtpExpirationMinutes { get; set; } = 10;
        public int MaxRetryAttempts { get; set; } = 3;
    }

    public class QueueSettings
    {
        public bool EnableQueue { get; set; } = true;
        public int MaxRetries { get; set; } = 5;
        public int RetryDelaySeconds { get; set; } = 30;
        public int BatchSize { get; set; } = 10;
    }

    public class GmailSettings
    {
        public string SmtpServer { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string AppPassword { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
        public bool UseOAuth2 { get; set; } = false;
        public OAuth2Settings OAuth2 { get; set; } = new();
    }

    public class OAuth2Settings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = "http://localhost:8080/";
        public string[] Scopes { get; set; } = { "https://mail.google.com/" };
        public string CredentialsPath { get; set; } = "credentials/gmail_oauth2.json";
    }
}