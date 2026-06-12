using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services
{
    /// <summary>
    /// Background service để xử lý email queue
    /// </summary>
    public class EmailBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EmailBackgroundService> _logger;

        public EmailBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<EmailBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var emailQueueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    // Lấy email từ queue và gửi đi
                    await ProcessEmailQueueAsync(emailQueueService, emailService);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing email queue");
                }

                // Chờ 30 giây trước khi check lại
                await Task.Delay(30000, stoppingToken);
            }

            _logger.LogInformation("Email Background Service stopped");
        }

        private async Task ProcessEmailQueueAsync(IEmailQueueService emailQueueService, IEmailService emailService)
        {
            try
            {
                // Xử lý email priority cao trước
                var highPriorityEmails = await emailQueueService.GetPendingEmailsAsync(EmailPriority.High, 5);
                if (highPriorityEmails.Any())
                {
                    foreach (var email in highPriorityEmails)
                    {
                        await ProcessEmailAsync(emailService, email, emailQueueService);
                    }
                    return;
                }

                // Xử lý email priority trung bình
                var mediumPriorityEmails = await emailQueueService.GetPendingEmailsAsync(EmailPriority.Normal, 5);
                if (mediumPriorityEmails.Any())
                {
                    foreach (var email in mediumPriorityEmails)
                    {
                        await ProcessEmailAsync(emailService, email, emailQueueService);
                    }
                    return;
                }

                // Xử lý email priority thấp
                var lowPriorityEmails = await emailQueueService.GetPendingEmailsAsync(EmailPriority.Low, 5);
                if (lowPriorityEmails.Any())
                {
                    foreach (var email in lowPriorityEmails)
                    {
                        await ProcessEmailAsync(emailService, email, emailQueueService);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing email queue items");
            }
        }

        private async Task ProcessEmailAsync(IEmailService emailService, EmailQueueItem emailItem, IEmailQueueService emailQueueService)
        {
            try
            {
                _logger.LogInformation("Processing email from queue: {EmailId}", emailItem.Id);

                // Gửi email - dùng method SendCustomEmailAsync có sẵn
                var success = await emailService.SendCustomEmailAsync(
                    emailItem.ToEmail,
                    emailItem.ToName,
                    emailItem.Subject,
                    emailItem.HtmlContent,
                    emailItem.TextContent);

                if (success)
                {
                    await emailQueueService.MarkAsProcessedAsync(emailItem.Id);
                    _logger.LogInformation("Email processed successfully: {EmailId}", emailItem.Id);
                }
                else
                {
                    await emailQueueService.MarkAsFailedAsync(emailItem.Id, "Failed to send email");
                    _logger.LogWarning("Email failed to send: {EmailId}", emailItem.Id);
                }
            }
            catch (Exception ex)
            {
                await emailQueueService.MarkAsFailedAsync(emailItem.Id, ex.Message);
                _logger.LogError(ex, "Failed to process email: {EmailId}", emailItem.Id);
            }
        }
    }
}