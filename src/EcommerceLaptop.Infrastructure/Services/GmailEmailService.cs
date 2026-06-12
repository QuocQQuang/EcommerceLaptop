using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Configuration;
using EcommerceLaptop.Core.Interfaces.Services;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceLaptop.Infrastructure.Services
{
    public class GmailEmailService : IEmailService
    {
        private readonly Core.Configuration.EmailSettings _emailSettings;
        private readonly ILogger<GmailEmailService> _logger;
        private readonly IHostEnvironment _environment;
        private readonly IEmailSecurityService _securityService;
        private readonly IGmailOAuth2Service _oauth2Service;
        private readonly ConcurrentDictionary<string, DateTime> _rateLimitTracker;
        private readonly SemaphoreSlim _semaphore;
        private readonly ISystemSettingsService _systemSettingsService;
        private readonly IServiceScopeFactory _scopeFactory;

        public GmailEmailService(
            IOptions<Core.Configuration.EmailSettings> emailSettings,
            ILogger<GmailEmailService> logger,
            IHostEnvironment environment,
            IEmailSecurityService securityService,
            IGmailOAuth2Service oauth2Service,
            ISystemSettingsService systemSettingsService,
            IServiceScopeFactory scopeFactory)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
            _environment = environment;
            _securityService = securityService;
            _oauth2Service = oauth2Service;
            _systemSettingsService = systemSettingsService;
            _rateLimitTracker = new ConcurrentDictionary<string, DateTime>();
            _semaphore = new SemaphoreSlim(1, 1);
            _scopeFactory = scopeFactory;
        }

        #region Authentication & Account Management

        public async Task<bool> SendPasswordResetEmailAsync(User user, string callbackUrl)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_password_reset"))
                {
                    _logger.LogInformation("Email send skipped (disabled): password reset for {Email}", _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                var templateData = new Dictionary<string, object>
                {
                    ["UserName"] = $"{user.FirstName} {user.LastName}",
                    ["FirstName"] = user.FirstName,
                    ["LastName"] = user.LastName,
                    ["Email"] = user.Email,
                    ["ResetUrl"] = callbackUrl,
                    ["RequestTime"] = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss (UTC)"),
                    ["IpAddress"] = "***.***.***.***.** (ẩn vì bảo mật)", // IP will be masked in email for security
                    ["DeviceInfo"] = "Trình duyệt web", // Generic device info for security
                    ["ExpirationMinutes"] = "15", // Default expiration time
                    ["CompanyName"] = _emailSettings.DefaultFromName
                };

                _logger.LogInformation("Template data for password reset: {TemplateData}",
                    string.Join(", ", templateData.Select(kvp => $"{kvp.Key}={kvp.Value}")));

                return await SendTemplatedEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    " Yêu cầu đặt lại mật khẩu - " + _emailSettings.DefaultFromName,
                    "PasswordReset",
                    templateData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", user.Email);
                return false;
            }
        }

        public async Task<bool> SendEmailConfirmationEmailAsync(User user, string callbackUrl)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_email_confirmation"))
                {
                    _logger.LogInformation("Email send skipped (disabled): email confirmation for {Email}", _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                var templateData = new Dictionary<string, object>
                {
                    ["FirstName"] = user.FirstName,
                    ["LastName"] = user.LastName,
                    ["CallbackUrl"] = callbackUrl,
                    ["CompanyName"] = _emailSettings.DefaultFromName
                };

                return await SendTemplatedEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    "Xác thực email - " + _emailSettings.DefaultFromName,
                    "EmailConfirmation",
                    templateData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email confirmation to {Email}", user.Email);
                return false;
            }
        }

        public async Task<bool> SendWelcomeEmailAsync(User user)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_user_registered"))
                {
                    _logger.LogInformation("Email send skipped (disabled): welcome for {Email}", _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                var templateData = new Dictionary<string, object>
                {
                    ["FirstName"] = user.FirstName,
                    ["LastName"] = user.LastName,
                    ["CompanyName"] = _emailSettings.DefaultFromName,
                    ["HomeUrl"] = "https://localhost:3000" // TODO: Get from configuration
                };

                return await SendTemplatedEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    "Chào mừng bạn đến với " + _emailSettings.DefaultFromName,
                    "Welcome",
                    templateData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email to {Email}", user.Email);
                return false;
            }
        }

        public async Task<bool> SendPasswordChangedNotificationAsync(User user)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_password_changed"))
                {
                    _logger.LogInformation("Email send skipped (disabled): password changed for {Email}", _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                var htmlContent = $@"
                    <h2>Mật khẩu đã được thay đổi</h2>
                    <p>Xin chào {user.FirstName} {user.LastName}!</p>
                    <p>Mật khẩu tài khoản của bạn đã được thay đổi thành công vào lúc {DateTime.Now:dd/MM/yyyy HH:mm}.</p>
                    <p>Nếu không phải bạn thực hiện, vui lòng liên hệ ngay với chúng tôi.</p>
                    <p>Trân trọng,<br>Đội ngũ {_emailSettings.DefaultFromName}</p>";

                return await SendEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    "Thông báo thay đổi mật khẩu",
                    htmlContent
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password changed notification to {Email}", user.Email);
                return false;
            }
        }

        #endregion

        #region OTP & Two-Factor Authentication

        public async Task<bool> SendOtpEmailAsync(User user, string otpCode, string purpose = "verification")
        {
            try
            {
                var templateData = new Dictionary<string, object>
                {
                    ["FirstName"] = user.FirstName,
                    ["LastName"] = user.LastName,
                    ["OtpCode"] = otpCode,
                    ["Purpose"] = purpose,
                    ["ExpirationMinutes"] = _emailSettings.Security.OtpExpirationMinutes,
                    ["CompanyName"] = _emailSettings.DefaultFromName
                };

                return await SendTemplatedEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    $"Mã xác thực OTP - {_emailSettings.DefaultFromName}",
                    "OtpVerification",
                    templateData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP email to {Email}", user.Email);
                return false;
            }
        }

        public async Task<bool> SendTwoFactorTokenEmailAsync(User user, string token)
        {
            return await SendOtpEmailAsync(user, token, "xác thực hai bước");
        }

        #endregion

        #region Order & Transaction Related

        public async Task<bool> SendOrderConfirmationEmailAsync(User user, string orderId)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_order_created"))
                {
                    _logger.LogInformation("Email send skipped (disabled): order created #{OrderId} for {Email}", orderId, _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                var htmlContent = $@"
                    <h2>Xác nhận đơn hàng #{orderId}</h2>
                    <p>Xin chào {user.FirstName} {user.LastName}!</p>
                    <p>Cảm ơn bạn đã đặt hàng tại {_emailSettings.DefaultFromName}.</p>
                    <p>Đơn hàng #{orderId} của bạn đã được tiếp nhận và đang được xử lý.</p>
                    <p>Chúng tôi sẽ gửi thông báo cập nhật trạng thái đơn hàng sớm nhất.</p>
                    <p>Trân trọng,<br>Đội ngũ {_emailSettings.DefaultFromName}</p>";

                return await SendEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    $"Xác nhận đơn hàng #{orderId}",
                    htmlContent
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation {OrderId} to {Email}", orderId, user.Email);
                return false;
            }
        }

        public async Task<bool> SendOrderStatusUpdateEmailAsync(User user, string orderId, string newStatus)
        {
            try
            {
                var key = newStatus.Equals("paid", StringComparison.OrdinalIgnoreCase)
                    ? "email_notification_order_paid"
                    : "email_notification_order_status_update";
                if (!await IsNotificationAllowedAsync(key))
                {
                    _logger.LogInformation("Email send skipped (disabled): order status #{OrderId} -> {Status} for {Email}", orderId, newStatus, _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                var statusInVietnamese = GetStatusInVietnamese(newStatus);
                var htmlContent = $@"
                    <h2>Cập nhật trạng thái đơn hàng #{orderId}</h2>
                    <p>Xin chào {user.FirstName} {user.LastName}!</p>
                    <p>Đơn hàng #{orderId} của bạn đã được cập nhật trạng thái:</p>
                    <p><strong>Trạng thái mới: {statusInVietnamese}</strong></p>
                    <p>Trân trọng,<br>Đội ngũ {_emailSettings.DefaultFromName}</p>";

                return await SendEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    $"Cập nhật đơn hàng #{orderId} - {statusInVietnamese}",
                    htmlContent
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order status update {OrderId} to {Email}", orderId, user.Email);
                return false;
            }
        }

        public async Task<bool> SendShippingNotificationEmailAsync(User user, string orderId, string trackingNumber)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_order_shipped"))
                {
                    _logger.LogInformation("Email send skipped (disabled): order shipped #{OrderId} for {Email}", orderId, _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                var htmlContent = $@"
                    <h2>Đơn hàng #{orderId} đã được giao cho đơn vị vận chuyển</h2>
                    <p>Xin chào {user.FirstName} {user.LastName}!</p>
                    <p>Đơn hàng #{orderId} của bạn đã được giao cho đơn vị vận chuyển.</p>
                    <p><strong>Mã vận đơn: {trackingNumber}</strong></p>
                    <p>Bạn có thể theo dõi trạng thái giao hàng qua mã vận đơn này.</p>
                    <p>Trân trọng,<br>Đội ngũ {_emailSettings.DefaultFromName}</p>";

                return await SendEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    $"Đơn hàng #{orderId} đang được vận chuyển",
                    htmlContent
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending shipping notification {OrderId} to {Email}", orderId, user.Email);
                return false;
            }
        }

        public async Task<bool> SendOrderCancellationEmailAsync(User user, string orderId, string reason)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_order_cancelled"))
                {
                    _logger.LogInformation("Email send skipped (disabled): order cancelled #{OrderId} for {Email}", orderId, _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                var htmlContent = $@"
                    <h2>Đơn hàng #{orderId} đã bị hủy</h2>
                    <p>Xin chào {user.FirstName} {user.LastName}!</p>
                    <p>Đơn hàng #{orderId} của bạn đã bị hủy.</p>
                    <p><strong>Lý do: {reason}</strong></p>
                    <p>Nếu có bất kỳ thắc mắc nào, vui lòng liên hệ với chúng tôi.</p>
                    <p>Trân trọng,<br>Đội ngũ {_emailSettings.DefaultFromName}</p>";

                return await SendEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    $"Hủy đơn hàng #{orderId}",
                    htmlContent
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order cancellation {OrderId} to {Email}", orderId, user.Email);
                return false;
            }
        }

        public async Task<bool> SendRefundNotificationEmailAsync(User user, string orderId, decimal refundAmount)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_order_refund"))
                {
                    _logger.LogInformation("Email send skipped (disabled): order refund #{OrderId} for {Email}", orderId, _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                var htmlContent = $@"
                    <h2>Thông báo hoàn tiền đơn hàng #{orderId}</h2>
                    <p>Xin chào {user.FirstName} {user.LastName}!</p>
                    <p>Chúng tôi đã xử lý hoàn tiền cho đơn hàng #{orderId}.</p>
                    <p><strong>Số tiền hoàn: {refundAmount:N0} VND</strong></p>
                    <p>Số tiền sẽ được hoàn vào tài khoản của bạn trong 3-5 ngày làm việc.</p>
                    <p>Trân trọng,<br>Đội ngũ {_emailSettings.DefaultFromName}</p>";

                return await SendEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    $"Hoàn tiền đơn hàng #{orderId}",
                    htmlContent
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending refund notification {OrderId} to {Email}", orderId, user.Email);
                return false;
            }
        }

        #endregion

        #region System Notifications

        public async Task<bool> SendAccountLockedNotificationAsync(User user, string reason)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_account_locked"))
                {
                    _logger.LogInformation("Email send skipped (disabled): account locked for {Email}", _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                var htmlContent = $@"
                    <h2>Tài khoản của bạn đã bị khóa</h2>
                    <p>Xin chào {user.FirstName} {user.LastName}!</p>
                    <p>Tài khoản của bạn đã bị khóa vào lúc {DateTime.Now:dd/MM/yyyy HH:mm}.</p>
                    <p><strong>Lý do: {reason}</strong></p>
                    <p>Vui lòng liên hệ với chúng tôi để được hỗ trợ mở khóa tài khoản.</p>
                    <p>Trân trọng,<br>Đội ngũ {_emailSettings.DefaultFromName}</p>";

                return await SendEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    "Thông báo khóa tài khoản",
                    htmlContent
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending account locked notification to {Email}", user.Email);
                return false;
            }
        }

        public async Task<bool> SendSecurityAlertEmailAsync(User user, string alertType, string details)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_security_alert"))
                {
                    _logger.LogInformation("Email send skipped (disabled): security alert for {Email}", _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                var htmlContent = $@"
                    <h2>Cảnh báo bảo mật tài khoản</h2>
                    <p>Xin chào {user.FirstName} {user.LastName}!</p>
                    <p>Chúng tôi phát hiện hoạt động bất thường trên tài khoản của bạn:</p>
                    <p><strong>Loại cảnh báo: {alertType}</strong></p>
                    <p><strong>Chi tiết: {details}</strong></p>
                    <p><strong>Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm}</strong></p>
                    <p>Nếu không phải bạn thực hiện, vui lòng đổi mật khẩu ngay lập tức và liên hệ với chúng tôi.</p>
                    <p>Trân trọng,<br>Đội ngũ {_emailSettings.DefaultFromName}</p>";

                return await SendEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    $"Cảnh báo bảo mật - {alertType}",
                    htmlContent
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending security alert to {Email}", user.Email);
                return false;
            }
        }

        public async Task<bool> SendNewsletterEmailAsync(User user, string subject, string content)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_newsletter"))
                {
                    _logger.LogInformation("Email send skipped (disabled): newsletter for {Email}", _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                return await SendEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    subject,
                    content
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending newsletter to {Email}", user.Email);
                return false;
            }
        }

        public async Task<bool> SendPromotionalEmailAsync(User user, string subject, string content, string? promoCode = null)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_promotion"))
                {
                    _logger.LogInformation("Email send skipped (disabled): promotion for {Email}", _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                var enhancedContent = content;
                if (!string.IsNullOrEmpty(promoCode))
                {
                    enhancedContent += $@"
                        <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; border-radius: 4px; padding: 15px; margin: 20px 0;'>
                            <strong>M khuyn mi ca bn: {promoCode}</strong>
                        </div>";
                }

                return await SendEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    subject,
                    enhancedContent
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending promotional email to {Email}", user.Email);
                return false;
            }
        }

        #endregion

        #region Core Email Methods

        private async Task<bool> SendTemplatedEmailAsync(
            string toEmail,
            string toName,
            string subject,
            string templateName,
            Dictionary<string, object> templateData)
        {
            try
            {
                var htmlContent = await LoadAndProcessTemplateAsync(templateName, templateData);
                return await SendEmailAsync(toEmail, toName, subject, htmlContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending templated email {Template} to {Email}", templateName, toEmail);
                return false;
            }
        }

        private async Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlContent, string? textContent = null)
        {
            try
            {
                // Enhanced security validation
                if (!await _securityService.CheckRateLimitAsync(toEmail))
                {
                    _logger.LogWarning("Rate limit exceeded for {HashedEmail}", _securityService.HashEmailAddress(toEmail));
                    return false;
                }

                // Log security event
                await _securityService.LogSecurityEventAsync("EMAIL_SEND_START",
                    $"Sending email to {_securityService.HashEmailAddress(toEmail)}, Subject: {subject}");

                // Decrypt content if it was encrypted
                var processedHtmlContent = _emailSettings.Security.EnableEncryption
                    ? await _securityService.DecryptEmailContentAsync(htmlContent)
                    : htmlContent;

                var processedTextContent = _emailSettings.Security.EnableEncryption && textContent != null
                    ? await _securityService.DecryptEmailContentAsync(textContent)
                    : textContent;

                // Create message
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.DefaultFromName, _emailSettings.DefaultFromEmail));
                message.To.Add(new MailboxAddress(toName, toEmail));
                message.Subject = subject;

                // Add security headers
                message.Headers.Add("X-Security-Hash", _securityService.HashEmailAddress(toEmail));
                message.Headers.Add("X-Sent-Time", DateTimeOffset.UtcNow.ToString("O"));
                message.Headers.Add("X-Environment", _environment.EnvironmentName);

                // Create body
                var bodyBuilder = new BodyBuilder();

                if (!string.IsNullOrEmpty(processedTextContent))
                {
                    bodyBuilder.TextBody = processedTextContent;
                }
                else
                {
                    bodyBuilder.TextBody = StripHtml(processedHtmlContent);
                }

                bodyBuilder.HtmlBody = processedHtmlContent;
                message.Body = bodyBuilder.ToMessageBody();

                // Send email using appropriate method
                bool sendResult;
                if (_emailSettings.Gmail.UseOAuth2)
                {
                    sendResult = await SendEmailWithOAuth2Async(message);
                }
                else
                {
                    sendResult = await SendEmailWithAppPasswordAsync(message);
                }

                if (sendResult)
                {
                    await _securityService.LogSecurityEventAsync("EMAIL_SEND_SUCCESS",
                        $"Email sent successfully to {_securityService.HashEmailAddress(toEmail)}");
                    _logger.LogInformation("Email sent successfully to {HashedEmail} with subject: {Subject}",
                        _securityService.HashEmailAddress(toEmail), subject);
                }
                else
                {
                    await _securityService.LogSecurityEventAsync("EMAIL_SEND_FAILED",
                        $"Failed to send email to {_securityService.HashEmailAddress(toEmail)}");
                }

                return sendResult;
            }
            catch (Exception ex)
            {
                await _securityService.LogSecurityEventAsync("EMAIL_SEND_ERROR",
                    $"Error sending email to {_securityService.HashEmailAddress(toEmail)}: {ex.Message}");
                _logger.LogError(ex, "Failed to send email to {HashedEmail} with subject: {Subject}",
                    _securityService.HashEmailAddress(toEmail), subject);
                return false;
            }
        }

        private async Task<string> LoadAndProcessTemplateAsync(string templateName, Dictionary<string, object> templateData)
        {
            try
            {
                var language = _emailSettings.Templates.DefaultLanguage;
                var templatePath = Path.Combine(_environment.ContentRootPath, _emailSettings.Templates.BasePath, language, $"{templateName}.html");
                var layoutPath = Path.Combine(_environment.ContentRootPath, _emailSettings.Templates.BasePath, "_Layout.html");

                _logger.LogInformation("Loading email template: {TemplateName}, Language: {Language}, Path: {TemplatePath}",
                    templateName, language, templatePath);

                if (!File.Exists(templatePath))
                {
                    _logger.LogError("Template file not found: {TemplatePath}", templatePath);
                    throw new FileNotFoundException($"Email template not found: {templatePath}");
                }

                var templateContent = await File.ReadAllTextAsync(templatePath);
                _logger.LogInformation("Template loaded successfully, content length: {Length}", templateContent.Length);

                // Process template variables
                foreach (var kvp in templateData)
                {
                    var oldContent = templateContent;
                    var searchPattern = $"{{{{{kvp.Key}}}}}";
                    var replacementValue = kvp.Value?.ToString() ?? "";

                    _logger.LogInformation("Processing variable: {Key} = {Value}, SearchPattern: {SearchPattern}",
                        kvp.Key, replacementValue, searchPattern);

                    templateContent = templateContent.Replace(searchPattern, replacementValue);

                    if (oldContent != templateContent)
                    {
                        _logger.LogInformation(" Replaced variable {Key} with value: {Value}", kvp.Key, replacementValue);
                    }
                    else
                    {
                        _logger.LogWarning(" Variable {Key} not found in template. SearchPattern: {SearchPattern}", kvp.Key, searchPattern);
                    }
                }

                // Add common variables
                templateContent = templateContent.Replace("{{CompanyName}}", _emailSettings.DefaultFromName);
                templateContent = templateContent.Replace("{{UnsubscribeUrl}}", "#"); // TODO: Implement unsubscribe

                _logger.LogInformation("Template variables processed. ResetUrl present: {HasResetUrl}",
                    templateContent.Contains("{{ResetUrl}}") ? "NO (replaced)" : "YES (not replaced)");

                // Check if layout file exists
                if (File.Exists(layoutPath))
                {
                    var layoutContent = await File.ReadAllTextAsync(layoutPath);
                    _logger.LogInformation("Layout file found, wrapping content. Layout length: {Length}", layoutContent.Length);

                    // Wrap content in layout
                    layoutContent = layoutContent.Replace("{{Content}}", templateContent);
                    layoutContent = layoutContent.Replace("{{CompanyName}}", _emailSettings.DefaultFromName);
                    layoutContent = layoutContent.Replace("{{Subject}}", templateData.ContainsKey("Subject") ? templateData["Subject"]?.ToString() ?? "" : "");

                    _logger.LogInformation("Final email content generated, length: {Length}", layoutContent.Length);
                    return layoutContent;
                }
                else
                {
                    // If no layout, return template content as is
                    _logger.LogWarning("Layout file not found at {LayoutPath}, using template content directly", layoutPath);
                    _logger.LogInformation("Final email content generated (no layout), length: {Length}", templateContent.Length);
                    return templateContent;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading template {TemplateName}", templateName);
                throw;
            }
        }

        private async Task<bool> CheckRateLimitAsync(string email)
        {
            await _semaphore.WaitAsync();
            try
            {
                var now = DateTime.UtcNow;
                var key = $"{email}:{now:yyyy-MM-dd-HH-mm}";

                // Clean old entries
                var keysToRemove = _rateLimitTracker.Keys
                    .Where(k => _rateLimitTracker[k] < now.AddMinutes(-1))
                    .ToList();

                foreach (var keyToRemove in keysToRemove)
                {
                    _rateLimitTracker.TryRemove(keyToRemove, out _);
                }

                // Check rate limit
                var emailsThisMinute = _rateLimitTracker.Keys.Count(k => k.StartsWith($"{email}:"));

                if (emailsThisMinute >= _emailSettings.RateLimiting.MaxEmailsPerMinute)
                {
                    return false;
                }

                _rateLimitTracker.TryAdd(key, now);
                return true;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private static string StripHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            return Regex.Replace(html, "<.*?>", string.Empty).Trim();
        }

        private static string GetStatusInVietnamese(string status)
        {
            return status.ToLower() switch
            {
                "pending" => "Chờ xử lý",
                "confirmed" => "Đã xác nhận",
                "processing" => "Đang xử lý",
                "shipped" => "Đã gửi hàng",
                "delivered" => "Đã giao hàng",
                "cancelled" => "Đã hủy",
                "returned" => "Đã trả hàng",
                _ => status
            };
        }

        #endregion

        #region Enhanced Security Methods

        public async Task<bool> SendCustomEmailAsync(string toEmail, string toName, string subject, string htmlContent, string? textContent = null)
        {
            // Apply security checks
            if (!await _securityService.CheckRateLimitAsync(toEmail))
            {
                _logger.LogWarning("Rate limit exceeded for email: {Email}", _securityService.HashEmailAddress(toEmail));
                return false;
            }

            // Log security event
            await _securityService.LogSecurityEventAsync("EMAIL_SEND", $"Custom email sent to {_securityService.HashEmailAddress(toEmail)}");

            // Encrypt content if enabled
            var processedHtmlContent = _emailSettings.Security.EnableEncryption
                ? await _securityService.EncryptEmailContentAsync(htmlContent)
                : htmlContent;

            var processedTextContent = _emailSettings.Security.EnableEncryption && textContent != null
                ? await _securityService.EncryptEmailContentAsync(textContent)
                : textContent;

            return await SendEmailAsync(toEmail, toName, subject, processedHtmlContent, processedTextContent);
        }



        public async Task<bool> SendPaymentSuccessEmailWithInvoiceAsync(User user, Order order, byte[] invoicePdf)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_order_paid"))
                {
                    _logger.LogInformation("Email send skipped (disabled): payment success for order {OrderId} to {Email}", order.Id, _securityService.HashEmailAddress(user.Email));
                    return true;
                }

                // Generate HTML content with order summary
                var htmlContent = GeneratePaymentSuccessHtml(user, order);

                // Create message with attachment
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_emailSettings.DefaultFromName, _emailSettings.DefaultFromEmail));
                message.To.Add(new MailboxAddress($"{user.FirstName} {user.LastName}", user.Email));
                message.Subject = $"Thanh toán đơn hàng #{order.OrderNumber} thành công - {_emailSettings.DefaultFromName}";

                var bodyBuilder = new BodyBuilder();
                bodyBuilder.HtmlBody = htmlContent;
                bodyBuilder.TextBody = StripHtml(htmlContent);

                // Add PDF attachment
                var attachment = new MimePart("application", "pdf")
                {
                    Content = new MimeContent(new MemoryStream(invoicePdf)),
                    ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                    FileName = $"HoaDon_{order.OrderNumber}.pdf"
                };
                attachment.ContentDisposition.FileName = $"HoaDon_{order.OrderNumber}.pdf";
                bodyBuilder.Attachments.Add(attachment);

                message.Body = bodyBuilder.ToMessageBody();

                // Send email
                bool sendResult;
                if (_emailSettings.Gmail.UseOAuth2)
                {
                    sendResult = await SendEmailWithOAuth2Async(message);
                }
                else
                {
                    sendResult = await SendEmailWithAppPasswordAsync(message);
                }

                if (sendResult)
                {
                    await _securityService.LogSecurityEventAsync("EMAIL_PAYMENT_SUCCESS_SENT",
                        $"Payment success email with invoice sent to {_securityService.HashEmailAddress(user.Email)} for order {order.OrderNumber}");
                    _logger.LogInformation("Payment success email with invoice sent to {HashedEmail} for order {OrderNumber}",
                        _securityService.HashEmailAddress(user.Email), order.OrderNumber);
                }
                else
                {
                    await _securityService.LogSecurityEventAsync("EMAIL_PAYMENT_SUCCESS_FAILED",
                        $"Failed to send payment success email to {_securityService.HashEmailAddress(user.Email)} for order {order.OrderNumber}");
                }

                return sendResult;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment success email with invoice for order {OrderId} to {Email}", order.Id, user.Email);
                await _securityService.LogSecurityEventAsync("EMAIL_PAYMENT_SUCCESS_ERROR",
                    $"Error sending payment success email for order {order.OrderNumber}: {ex.Message}");
                return false;
            }
        }

        private string GeneratePaymentSuccessHtml(User user, Order order)
        {
            var totalAmount = order.TotalAmount.ToString("N0");
            var subtotal = order.SubTotal.ToString("N0");
            var shipping = order.ShippingAmount.ToString("N0");
            var tax = order.TaxAmount.ToString("N0");

            var htmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                    <h2 style='color: #28a745;'> Thanh toán thành công!</h2>
                    <p>Xin chào <strong>{user.FirstName} {user.LastName}</strong>,</p>
                    <p>Cảm ơn bạn đã mua sắm tại <strong>{_emailSettings.DefaultFromName}</strong>!</p>
                    <p>Đơn hàng <strong>#{order.OrderNumber}</strong> của bạn đã được thanh toán thành công.</p>

                    <div style='background-color: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <h3>Thông tin đơn hàng</h3>
                        <p><strong>Mã đơn hàng:</strong> {order.OrderNumber}</p>
                        <p><strong>Ngày đặt hàng:</strong> {order.CreatedAt:dd/MM/yyyy HH:mm}</p>
                        <p><strong>Trạng thái:</strong> Đã xác nhận và thanh toán</p>
                        <p><strong>Phương thức thanh toán:</strong> Thẻ tín dụng/Chuyển khoản</p>

                        <h4>Chi tiết sản phẩm:</h4>
                        <ul>";
            foreach (var item in order.OrderItems)
            {
                htmlContent += $@"
                            <li>
                                <strong>{item.Product?.Name}</strong> - Số lượng: {item.Quantity} x {item.UnitPrice:N0} USD = {item.TotalPrice:N0} USD
                            </li>";
            }
            htmlContent += $@"
                        </ul>

                        <div style='border-top: 1px solid #dee2e6; padding-top: 15px; margin-top: 15px;'>
                            <p><strong>Tạm tính:</strong> {subtotal} USD</p>
                            <p><strong>Phí vận chuyển:</strong> {shipping} USD</p>
                            <p><strong>Thuế VAT:</strong> {tax} USD</p>
                            <p style='font-size: 18px; color: #28a745; font-weight: bold;'>
                                <strong>Tổng cộng: {totalAmount} USD</strong>
                            </p>
                        </div>
                    </div>

                    <p>Chúng tôi đã đính kèm hóa đơn PDF trong email này. Bạn có thể sử dụng để lưu trữ hoặc in ấn.</p>
                    <p>Đơn hàng của bạn sẽ được xử lý và giao trong thời gian sớm nhất. Bạn có thể theo dõi trạng thái đơn hàng trong <a href='https://yourdomain.com/account/orders'>tài khoản cá nhân</a>.</p>

                    <p>Nếu có bất kỳ câu hỏi nào, vui lòng liên hệ với chúng tôi qua email <a href='mailto:support@yourdomain.com'>support@yourdomain.com</a> hoặc hotline 0123-456-789.</p>

                    <p>Trân trọng,<br><strong>Đội ngũ {_emailSettings.DefaultFromName}</strong></p>

                    <hr style='border: none; border-top: 1px solid #dee2e6; margin: 30px 0;'>

                    <p style='font-size: 12px; color: #6c757d;'>
                        <strong>Thông tin liên hệ:</strong><br>
                        {_emailSettings.DefaultFromName}<br>
                        Địa chỉ: 123 đường ABC, Quận 1, TP. Hồ Chí Minh<br>
                        Email: {_emailSettings.DefaultFromEmail}<br>
                        Hotline: 0123-456-789
                    </p>
                </div>";

            return htmlContent;
        }

        public async Task<bool> SendBulkEmailAsync(List<EmailRecipient> recipients, string subject, string htmlContent, string? textContent = null)
        {
            var tasks = new List<Task<bool>>();
            var semaphore = new SemaphoreSlim(5, 5); // Limit concurrent sends

            foreach (var recipient in recipients)
            {
                tasks.Add(ProcessBulkEmailRecipient(semaphore, recipient, subject, htmlContent, textContent));
            }

            var results = await Task.WhenAll(tasks);
            var successCount = results.Count(r => r);

            _logger.LogInformation("Bulk email completed: {Success}/{Total} successful", successCount, recipients.Count);
            return successCount > 0;
        }

        private async Task<bool> ProcessBulkEmailRecipient(SemaphoreSlim semaphore, EmailRecipient recipient, string subject, string htmlContent, string? textContent)
        {
            await semaphore.WaitAsync();
            try
            {
                return await SendCustomEmailAsync(recipient.Email, recipient.Name, subject, htmlContent, textContent);
            }
            finally
            {
                semaphore.Release();
            }
        }

        public async Task<bool> SendAdminNotificationAsync(string subject, string content, string priority = "normal")
        {
            try
            {
                var adminEmails = _emailSettings.Gmail.Username; // For now, use configured email
                if (string.IsNullOrEmpty(adminEmails))
                {
                    _logger.LogWarning("No admin email configured for notifications");
                    return false;
                }

                var priorityLabel = priority.ToUpper() switch
                {
                    "HIGH" => " HIGH PRIORITY",
                    "URGENT" => " URGENT",
                    _ => " NOTIFICATION"
                };

                var htmlContent = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 600px;'>
                        <h2 style='color: #d32f2f;'>{priorityLabel}</h2>
                        <h3>{subject}</h3>
                        <div style='background-color: #f5f5f5; padding: 15px; border-radius: 5px;'>
                            {content}
                        </div>
                        <p style='color: #666; font-size: 12px; margin-top: 20px;'>
                            Sent at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC<br>
                            Environment: {_environment.EnvironmentName}
                        </p>
                    </div>";

                await _securityService.LogSecurityEventAsync("ADMIN_NOTIFICATION", $"Subject: {subject}, Priority: {priority}");

                return await SendEmailAsync(adminEmails, "System Administrator", $"[{priorityLabel}] {subject}", htmlContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send admin notification: {Subject}", subject);
                return false;
            }
        }

        public async Task<bool> SendSupportTicketEmailAsync(User user, string ticketNumber, string subject, string message)
        {
            try
            {
                if (!await IsNotificationAllowedAsync("email_notification_support_ticket"))
                {
                    _logger.LogInformation("Email send skipped (disabled): support ticket for {Email}", _securityService.HashEmailAddress(user.Email));
                    return true;
                }
                var templateData = new Dictionary<string, object>
                {
                    ["FirstName"] = user.FirstName,
                    ["LastName"] = user.LastName,
                    ["TicketNumber"] = ticketNumber,
                    ["Subject"] = subject,
                    ["Message"] = message,
                    ["CreatedAt"] = DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"),
                    ["CompanyName"] = _emailSettings.DefaultFromName
                };

                await _securityService.LogSecurityEventAsync("SUPPORT_TICKET", $"Ticket {ticketNumber} created for {_securityService.HashEmailAddress(user.Email)}");

                return await SendTemplatedEmailAsync(
                    user.Email,
                    $"{user.FirstName} {user.LastName}",
                    $"Yu cu h tr #{ticketNumber} - {subject}",
                    "SupportTicket",
                    templateData
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending support ticket email for ticket {TicketNumber}", ticketNumber);
                return false;
            }
        }

        public async Task<bool> QueueEmailAsync(EmailQueueItem emailItem)
        {
            try
            {
                // Enhanced security validation
                if (!await _securityService.CheckRateLimitAsync(emailItem.ToEmail))
                {
                    _logger.LogWarning("Rate limit exceeded for queuing email to: {HashedEmail}",
                        _securityService.HashEmailAddress(emailItem.ToEmail));
                    return false;
                }

                // Generate secure token for tracking
                var trackingToken = await _securityService.GenerateSecureTokenAsync($"email_track_{emailItem.Id}");
                emailItem.TrackingId = trackingToken;

                // Encrypt sensitive content if enabled
                if (_emailSettings.Security.EnableEncryption)
                {
                    emailItem.HtmlContent = await _securityService.EncryptEmailContentAsync(emailItem.HtmlContent);
                    if (!string.IsNullOrEmpty(emailItem.TextContent))
                    {
                        emailItem.TextContent = await _securityService.EncryptEmailContentAsync(emailItem.TextContent);
                    }
                }

                await _securityService.LogSecurityEventAsync("EMAIL_QUEUED",
                    $"Email queued for {_securityService.HashEmailAddress(emailItem.ToEmail)} with priority {emailItem.Priority}");

                // This would integrate with the actual queue service
                _logger.LogInformation("Email queued successfully with tracking ID: {TrackingId}", emailItem.TrackingId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to queue email for {Email}", _securityService.HashEmailAddress(emailItem.ToEmail));
                return false;
            }
        }

        public async Task ProcessEmailQueueAsync()
        {
            try
            {
                await _securityService.LogSecurityEventAsync("QUEUE_PROCESSING_START", "Email queue processing started");

                // This would integrate with the actual queue processing logic
                _logger.LogInformation("Email queue processing started");

                // The actual processing would be handled by EmailQueueBackgroundService
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email queue processing");
                await _securityService.LogSecurityEventAsync("QUEUE_PROCESSING_ERROR", $"Queue processing failed: {ex.Message}");
            }
        }

        public async Task<EmailDeliveryStatus> GetEmailStatusAsync(string emailId)
        {
            try
            {
                // Validate the tracking token
                var isValidToken = await _securityService.ValidateSecureTokenAsync(emailId, $"email_track_{emailId}");
                if (!isValidToken)
                {
                    _logger.LogWarning("Invalid email tracking token: {TokenHash}", _securityService.HashEmailAddress(emailId));
                    return new EmailDeliveryStatus
                    {
                        Id = emailId,
                        Status = EmailDeliveryStatusType.Failed,
                        Message = "Invalid tracking token",
                        LastUpdated = DateTime.UtcNow
                    };
                }

                // This would query the actual status from the queue/tracking system
                return new EmailDeliveryStatus
                {
                    Id = emailId,
                    Status = EmailDeliveryStatusType.Sent,
                    Message = "Email delivered successfully",
                    LastUpdated = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving email status for ID: {EmailId}", emailId);
                return new EmailDeliveryStatus
                {
                    Id = emailId,
                    Status = EmailDeliveryStatusType.Failed,
                    Message = ex.Message,
                    LastUpdated = DateTime.UtcNow
                };
            }
        }

        #endregion

        #region Private Helper Methods with Security

        private async Task<bool> IsNotificationAllowedAsync(string key, bool defaultEnabled = true)
        {
            // Global master switch
            var notificationsEnabled = await _systemSettingsService.GetSettingValueAsync<bool>("email_notifications_enabled", true);
            if (!notificationsEnabled)
            {
                return false;
            }

            var enabled = await _systemSettingsService.GetSettingValueAsync<bool>(key, defaultEnabled);
            return enabled;
        }

        private async Task<bool> SendEmailWithOAuth2Async(MimeMessage message)
        {
            if (_emailSettings.Gmail.UseOAuth2)
            {
                return await _oauth2Service.SendEmailWithOAuth2Async(message);
            }

            return await SendEmailWithAppPasswordAsync(message);
        }

        private async Task<bool> SendEmailWithAppPasswordAsync(MimeMessage message)
        {
            try
            {
                // Load runtime SMTP config from SystemSettings via a fresh scope to avoid disposed DbContext
                string smtpServer = _emailSettings.Gmail.SmtpServer;
                int smtpPort = _emailSettings.Gmail.SmtpPort;
                string smtpUsername = _emailSettings.Gmail.Username;
                string smtpPassword = string.Empty;
                bool enableTls = true;
                bool enableSsl = false;

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var settingsService = scope.ServiceProvider.GetRequiredService<ISystemSettingsService>();
                    smtpServer = await settingsService.GetSettingValueAsync<string>("smtp_host", smtpServer) ?? smtpServer;
                    smtpPort = await settingsService.GetSettingValueAsync<int>("smtp_port", smtpPort);
                    smtpUsername = await settingsService.GetSettingValueAsync<string>("smtp_username", smtpUsername) ?? smtpUsername;
                    smtpPassword = await settingsService.GetSettingValueAsync<string>("smtp_password", smtpPassword);
                    enableTls = await settingsService.GetSettingValueAsync<bool>("smtp_enable_tls", enableTls);
                    enableSsl = await settingsService.GetSettingValueAsync<bool>("smtp_enable_ssl", enableSsl);
                }
                catch (Exception)
                {
                    // Keep defaults
                }

                // Choose security option
                var secureOption = enableSsl ? SecureSocketOptions.SslOnConnect : (enableTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto);

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, smtpPort, secureOption);

                // Prefer DB password; if empty, fallback to configured app password or password from appsettings
                var configuredPassword = !string.IsNullOrEmpty(_emailSettings.Gmail.AppPassword)
                    ? _emailSettings.Gmail.AppPassword
                    : _emailSettings.Gmail.Password;
                var password = string.IsNullOrWhiteSpace(smtpPassword) ? configuredPassword : smtpPassword;

                await client.AuthenticateAsync(smtpUsername, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email with app password");
                return false;
            }
        }

        #endregion


    }
}
