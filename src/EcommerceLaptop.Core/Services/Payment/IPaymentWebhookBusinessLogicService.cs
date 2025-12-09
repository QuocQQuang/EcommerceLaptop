using EcommerceLaptop.Core.DTOs.Payment;

namespace EcommerceLaptop.Core.Services.Payment;

/// <summary>
/// Interface for payment webhook business logic processing
/// Orchestrates order updates, inventory management, and email notifications
/// Following Uncle Bob's Clean Code and SOLID principles
/// </summary>
public interface IPaymentWebhookBusinessLogicService
{
    /// <summary>
    /// Processes successful payment webhooks across all gateways
    /// Confirms order payment, confirms inventory reservations, sends confirmation emails
    /// </summary>
    /// <param name="webhookResult">Webhook processing result from payment gateway</param>
    /// <returns>True if business logic processing succeeded</returns>
    Task<bool> ProcessPaymentSuccessAsync(PaymentWebhookResult webhookResult);

    /// <summary>
    /// Processes failed payment webhooks across all gateways
    /// Updates order status, releases inventory reservations, sends failure notifications
    /// </summary>
    /// <param name="webhookResult">Webhook processing result from payment gateway</param>
    /// <returns>True if business logic processing succeeded</returns>
    Task<bool> ProcessPaymentFailureAsync(PaymentWebhookResult webhookResult);

    /// <summary>
    /// Processes refund webhooks across all gateways
    /// Updates order status, handles inventory restoration, sends refund notifications
    /// </summary>
    /// <param name="webhookResult">Webhook processing result from payment gateway</param>
    /// <returns>True if business logic processing succeeded</returns>
    Task<bool> ProcessRefundAsync(PaymentWebhookResult webhookResult);

    /// <summary>
    /// Processes dispute/chargeback webhooks across all gateways
    /// Updates order status, sends admin notifications
    /// </summary>
    /// <param name="webhookResult">Webhook processing result from payment gateway</param>
    /// <returns>True if business logic processing succeeded</returns>
    Task<bool> ProcessDisputeAsync(PaymentWebhookResult webhookResult);
}