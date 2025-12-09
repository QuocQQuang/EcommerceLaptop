using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Services;

namespace EcommerceLaptop.Infrastructure.Services
{
    public class EmailQueueBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<EmailQueueBackgroundService> _logger;
        private readonly TimeSpan _processingInterval = TimeSpan.FromMinutes(1); // Process every minute
        private readonly TimeSpan _retryInterval = TimeSpan.FromHours(1); // Retry failed emails every hour

        public EmailQueueBackgroundService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<EmailQueueBackgroundService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Queue Background Service started");

            var lastRetryTime = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceScopeFactory.CreateScope();
                    var emailQueueService = scope.ServiceProvider.GetRequiredService<IEmailQueueService>();

                    // Process all priority queues
                    await ProcessPriorityQueues(emailQueueService);

                    // Check if it's time to retry failed emails
                    if (DateTime.UtcNow - lastRetryTime >= _retryInterval)
                    {
                        var failedEmails = await emailQueueService.GetFailedEmailsAsync();
                        foreach (var emailId in failedEmails.Select(e => e.Id))
                        {
                            await emailQueueService.RetryFailedEmailAsync(emailId);
                        }
                        lastRetryTime = DateTime.UtcNow;
                    }

                    // Log queue status
                    var stats = await emailQueueService.GetQueueStatsAsync();
                    if (stats.PendingCount > 0)
                    {
                        _logger.LogInformation("Processed email queue. Remaining emails: {QueueCount}", stats.PendingCount);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Email Queue Background Service");
                }

                // Wait before next processing cycle
                await Task.Delay(_processingInterval, stoppingToken);
            }

            _logger.LogInformation("Email Queue Background Service stopped");
        }

        private async Task ProcessPriorityQueues(IEmailQueueService emailQueueService)
        {
            // Process urgent emails first
            await ProcessEmailsByPriority(emailQueueService, EmailPriority.Urgent, 50);

            // Process high priority emails
            await ProcessEmailsByPriority(emailQueueService, EmailPriority.High, 30);

            // Process normal priority emails
            await ProcessEmailsByPriority(emailQueueService, EmailPriority.Normal, 20);

            // Process low priority emails
            await ProcessEmailsByPriority(emailQueueService, EmailPriority.Low, 10);
        }

        private async Task ProcessEmailsByPriority(IEmailQueueService emailQueueService, EmailPriority priority, int batchSize)
        {
            try
            {
                var emails = await emailQueueService.GetPendingEmailsAsync(priority, batchSize);

                _logger.LogInformation("Processing {Count} {Priority} priority emails", emails.Count, priority);

                foreach (var email in emails)
                {
                    try
                    {
                        // Mark as processed (placeholder - actual email sending would happen here)
                        await emailQueueService.MarkAsProcessedAsync(email.Id);

                        _logger.LogInformation("Email {EmailId} processed successfully", email.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to process email {EmailId}", email.Id);
                        await emailQueueService.MarkAsFailedAsync(email.Id, ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing {Priority} priority emails", priority);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Email Queue Background Service is stopping");
            await base.StopAsync(stoppingToken);
        }
    }
}