using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Services.Payment;

namespace EcommerceLaptop.Infrastructure.Services.Payment;

/// <summary>
/// Business logic service for processing payment webhook events
/// Orchestrates order updates, inventory management, and email notifications
/// Following Uncle Bob's Clean Code and SOLID principles
/// </summary>
public class PaymentWebhookBusinessLogicService : IPaymentWebhookBusinessLogicService
{
    private readonly IOrderService _orderService;
    private readonly IOrderWorkflowService _orderWorkflowService;
    private readonly IInventoryReservationService _inventoryService;
    private readonly IEmailService _emailService;
    private readonly IEmailQueueService _emailQueueService;
    private readonly ILogger<PaymentWebhookBusinessLogicService> _logger;

    public PaymentWebhookBusinessLogicService(
        IOrderService orderService,
        IOrderWorkflowService orderWorkflowService,
        IInventoryReservationService inventoryService,
        IEmailService emailService,
        IEmailQueueService emailQueueService,
        ILogger<PaymentWebhookBusinessLogicService> logger)
    {
        _orderService = orderService;
        _orderWorkflowService = orderWorkflowService;
        _inventoryService = inventoryService;
        _emailService = emailService;
        _emailQueueService = emailQueueService;
        _logger = logger;
    }

    /// <summary>
    /// Processes successful payment webhooks across all gateways
    /// </summary>
    public async Task<bool> ProcessPaymentSuccessAsync(PaymentWebhookResult webhookResult)
    {
        try
        {
            _logger.LogInformation("Processing payment success for transaction {TransactionId}", 
                webhookResult.TransactionId);

            // Step 1: Extract order information
            var orderId = await ExtractOrderIdFromWebhookAsync(webhookResult);
            if (orderId == null)
            {
                _logger.LogWarning("Could not extract order ID from webhook transaction {TransactionId}", 
                    webhookResult.TransactionId);
                return false;
            }

            // Step 2: Update order status to paid
            var order = await _orderService.GetOrderDetailsAsync(orderId.Value);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for payment {TransactionId}", 
                    orderId, webhookResult.TransactionId);
                return false;
            }

            // Step 3: Use OrderWorkflowService for state management
            var updateResult = await _orderWorkflowService.UpdateOrderStatusAsync(
                orderId.Value, 
                OrderStatus.Confirmed, 
                $"Payment confirmed via {webhookResult.Gateway} webhook. Transaction: {webhookResult.TransactionId}");

            if (!updateResult)
            {
                _logger.LogError("Failed to update order {OrderId} status to Confirmed", orderId);
                return false;
            }

            // Step 4: Confirm inventory reservations
            await ConfirmInventoryReservationsAsync(orderId.Value);

            // Step 5: Send confirmation emails
            await SendPaymentConfirmationEmailsAsync(order, webhookResult);

            _logger.LogInformation("Successfully processed payment success for order {OrderId}, transaction {TransactionId}", 
                orderId, webhookResult.TransactionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment success for transaction {TransactionId}", 
                webhookResult.TransactionId);
            return false;
        }
    }

    /// <summary>
    /// Processes failed payment webhooks across all gateways
    /// </summary>
    public async Task<bool> ProcessPaymentFailureAsync(PaymentWebhookResult webhookResult)
    {
        try
        {
            _logger.LogInformation("Processing payment failure for transaction {TransactionId}", 
                webhookResult.TransactionId);

            // Step 1: Extract order information
            var orderId = await ExtractOrderIdFromWebhookAsync(webhookResult);
            if (orderId == null)
            {
                _logger.LogWarning("Could not extract order ID from webhook transaction {TransactionId}", 
                    webhookResult.TransactionId);
                return false;
            }

            // Step 2: Get order details
            var order = await _orderService.GetOrderDetailsAsync(orderId.Value);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for failed payment {TransactionId}", 
                    orderId, webhookResult.TransactionId);
                return false;
            }

            // Step 3: Update order status to cancelled due to payment failure
            var updateResult = await _orderWorkflowService.UpdateOrderStatusAsync(
                orderId.Value, 
                OrderStatus.Cancelled, 
                $"Payment failed via {webhookResult.Gateway} webhook. Transaction: {webhookResult.TransactionId}. Reason: {webhookResult.ErrorMessage}");

            if (!updateResult)
            {
                _logger.LogError("Failed to update order {OrderId} status to Cancelled", orderId);
            }

            // Step 4: Release inventory reservations
            await ReleaseInventoryReservationsAsync(orderId.Value);

            // Step 5: Send failure notification emails
            await SendPaymentFailureEmailsAsync(order, webhookResult);

            _logger.LogInformation("Successfully processed payment failure for order {OrderId}, transaction {TransactionId}", 
                orderId, webhookResult.TransactionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment failure for transaction {TransactionId}", 
                webhookResult.TransactionId);
            return false;
        }
    }

    /// <summary>
    /// Processes refund webhooks across all gateways
    /// </summary>
    public async Task<bool> ProcessRefundAsync(PaymentWebhookResult webhookResult)
    {
        try
        {
            _logger.LogInformation("Processing refund for transaction {TransactionId}", 
                webhookResult.TransactionId);

            // Step 1: Extract order information
            var orderId = await ExtractOrderIdFromWebhookAsync(webhookResult);
            if (orderId == null)
            {
                _logger.LogWarning("Could not extract order ID from webhook transaction {TransactionId}", 
                    webhookResult.TransactionId);
                return false;
            }

            // Step 2: Get order details
            var order = await _orderService.GetOrderDetailsAsync(orderId.Value);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for refund {TransactionId}", 
                    orderId, webhookResult.TransactionId);
                return false;
            }

            // Step 3: Update order status to refunded (both partial and full refunds)
            var refundAmount = ExtractRefundAmountFromWebhook(webhookResult);
            var isPartialRefund = refundAmount.HasValue && refundAmount.Value < order.TotalAmount;
            
            var refundType = isPartialRefund ? "Partial" : "Full";
            var statusMessage = $"{refundType} refund processed via {webhookResult.Gateway} webhook. " +
                              $"Transaction: {webhookResult.TransactionId}. " +
                              $"Amount: {refundAmount?.ToString("C") ?? "Full"}";

            var updateResult = await _orderWorkflowService.UpdateOrderStatusAsync(
                orderId.Value, 
                OrderStatus.Refunded, 
                statusMessage);

            if (!updateResult)
            {
                _logger.LogError("Failed to update order {OrderId} status to Refunded", orderId);
            }

            // Step 4: Handle inventory for full refunds
            if (!isPartialRefund)
            {
                await RestoreInventoryFromRefundAsync(orderId.Value);
            }

            // Step 5: Send refund notification emails
            await SendRefundNotificationEmailsAsync(order, webhookResult, refundAmount);

            _logger.LogInformation("Successfully processed refund for order {OrderId}, transaction {TransactionId}", 
                orderId, webhookResult.TransactionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund for transaction {TransactionId}", 
                webhookResult.TransactionId);
            return false;
        }
    }

    /// <summary>
    /// Processes dispute/chargeback webhooks across all gateways
    /// </summary>
    public async Task<bool> ProcessDisputeAsync(PaymentWebhookResult webhookResult)
    {
        try
        {
            _logger.LogInformation("Processing dispute for transaction {TransactionId}", 
                webhookResult.TransactionId);

            // Step 1: Extract order information
            var orderId = await ExtractOrderIdFromWebhookAsync(webhookResult);
            if (orderId == null)
            {
                _logger.LogWarning("Could not extract order ID from webhook transaction {TransactionId}", 
                    webhookResult.TransactionId);
                return false;
            }

            // Step 2: Get order details
            var order = await _orderService.GetOrderDetailsAsync(orderId.Value);
            if (order == null)
            {
                _logger.LogWarning("Order {OrderId} not found for dispute {TransactionId}", 
                    orderId, webhookResult.TransactionId);
                return false;
            }

            // Step 3: Handle dispute - don't change order status, just log and notify
            // Disputes require manual review, so we keep current status
            _logger.LogWarning("Payment dispute received for order {OrderId} via {Gateway}. Transaction: {TransactionId}. " +
                             "Current order status: {CurrentStatus}. Manual review required.",
                orderId.Value, webhookResult.Gateway, webhookResult.TransactionId, order.Status);

            // Add audit trail for dispute
            // Note: In a real system, you might want to add dispute-specific audit entries

            // Step 4: Send admin notification for disputes
            await SendDisputeNotificationEmailsAsync(order, webhookResult);

            _logger.LogInformation("Successfully processed dispute for order {OrderId}, transaction {TransactionId}", 
                orderId, webhookResult.TransactionId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing dispute for transaction {TransactionId}", 
                webhookResult.TransactionId);
            return false;
        }
    }

    /// <summary>
    /// Extracts order ID from webhook data
    /// Supports multiple formats across different gateways
    /// Falls back to database lookup if not found in webhook data
    /// </summary>
    private async Task<int?> ExtractOrderIdFromWebhookAsync(PaymentWebhookResult webhookResult)
    {
        try
        {
            // Try to extract from metadata first (most common)
            if (webhookResult.Data?.ContainsKey("order_id") == true)
            {
                if (int.TryParse(webhookResult.Data["order_id"]?.ToString(), out var orderId))
                {
                    return orderId;
                }
            }

            // Try to extract from reference field
            if (webhookResult.Data?.ContainsKey("reference") == true)
            {
                var reference = webhookResult.Data["reference"]?.ToString();
                if (!string.IsNullOrEmpty(reference) && reference.StartsWith("ORDER_"))
                {
                    var orderIdStr = reference.Replace("ORDER_", "");
                    if (int.TryParse(orderIdStr, out var orderId))
                    {
                        return orderId;
                    }
                }
            }

            // Try to extract from transaction ID pattern
            if (!string.IsNullOrEmpty(webhookResult.TransactionId))
            {
                // Pattern: TXN_ORDER_123_GATEWAY
                var parts = webhookResult.TransactionId.Split('_');
                if (parts.Length >= 3 && parts[0] == "TXN" && parts[1] == "ORDER")
                {
                    if (int.TryParse(parts[2], out var orderId))
                    {
                        return orderId;
                    }
                }
            }

            // **NEW: Database lookup for Stripe and other gateways**
            // If webhook data doesn't contain order ID, lookup by transaction ID
            if (!string.IsNullOrEmpty(webhookResult.TransactionId))
            {
                _logger.LogInformation("Webhook data missing order ID. Looking up order by transaction ID: {TransactionId}", 
                    webhookResult.TransactionId);

                var payment = await _orderService.GetPaymentByTransactionIdAsync(webhookResult.TransactionId);
                if (payment != null)
                {
                    _logger.LogInformation("Found order {OrderId} for transaction {TransactionId} via database lookup", 
                        payment.OrderId, webhookResult.TransactionId);
                    return payment.OrderId;
                }
            }

            _logger.LogWarning("Could not extract order ID from webhook data or database lookup for transaction {TransactionId}", 
                webhookResult.TransactionId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting order ID from webhook for transaction {TransactionId}", 
                webhookResult.TransactionId);
            return null;
        }
    }

    /// <summary>
    /// Extracts refund amount from webhook data
    /// </summary>
    private decimal? ExtractRefundAmountFromWebhook(PaymentWebhookResult webhookResult)
    {
        try
        {
            if (webhookResult.Data?.ContainsKey("refund_amount") == true)
            {
                if (decimal.TryParse(webhookResult.Data["refund_amount"]?.ToString(), out var amount))
                {
                    return amount;
                }
            }

            if (webhookResult.Data?.ContainsKey("amount") == true)
            {
                if (decimal.TryParse(webhookResult.Data["amount"]?.ToString(), out var amount))
                {
                    return amount;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting refund amount from webhook for transaction {TransactionId}", 
                webhookResult.TransactionId);
            return null;
        }
    }

    /// <summary>
    /// Confirms inventory reservations after successful payment
    /// </summary>
    private async Task ConfirmInventoryReservationsAsync(int orderId)
    {
        try
        {
            _logger.LogInformation("Confirming inventory reservations for order {OrderId}", orderId);
            await _inventoryService.ConfirmInventoryReservationAsync(orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming inventory reservations for order {OrderId}", orderId);
        }
    }

    /// <summary>
    /// Releases inventory reservations after payment failure
    /// </summary>
    private async Task ReleaseInventoryReservationsAsync(int orderId)
    {
        try
        {
            _logger.LogInformation("Releasing inventory reservations for order {OrderId}", orderId);
            await _inventoryService.ReleaseInventoryForOrderAsync(orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error releasing inventory reservations for order {OrderId}", orderId);
        }
    }

    /// <summary>
    /// Restores inventory after refund
    /// </summary>
    private async Task RestoreInventoryFromRefundAsync(int orderId)
    {
        try
        {
            _logger.LogInformation("Restoring inventory for refunded order {OrderId}", orderId);
            await _inventoryService.RestockInventoryForOrderAsync(orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring inventory for order {OrderId}", orderId);
        }
    }

    /// <summary>
    /// Sends payment confirmation emails
    /// </summary>
    private async Task SendPaymentConfirmationEmailsAsync(OrderDetailsDto order, PaymentWebhookResult webhookResult)
    {
        try
        {
            // Queue high-priority email to customer
            await _emailQueueService.QueueEmailAsync(
                to: order.CustomerEmail,
                subject: $"Thanh toán thành công - Đơn hàng #{order.Id}",
                body: GeneratePaymentConfirmationEmailBody(order, webhookResult),
                isHtml: true,
                priority: EmailPriority.High);

            // Send admin notification for large orders
            if (order.TotalAmount >= 10000000) // 10 million VND
            {
                await _emailService.SendAdminNotificationAsync(
                    subject: $"Large Order Payment Confirmed - Order #{order.Id}",
                    content: $"Order #{order.Id} payment confirmed. Amount: {order.TotalAmount:C}. Gateway: {webhookResult.Gateway}",
                    priority: "high");
            }

            _logger.LogInformation("Payment confirmation emails queued for order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment confirmation emails for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Sends payment failure emails
    /// </summary>
    private async Task SendPaymentFailureEmailsAsync(OrderDetailsDto order, PaymentWebhookResult webhookResult)
    {
        try
        {
            await _emailQueueService.QueueEmailAsync(
                to: order.CustomerEmail,
                subject: $"Thanh toán không thành công - Đơn hàng #{order.Id}",
                body: GeneratePaymentFailureEmailBody(order, webhookResult),
                isHtml: true,
                priority: EmailPriority.High);

            _logger.LogInformation("Payment failure emails queued for order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment failure emails for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Sends refund notification emails
    /// </summary>
    private async Task SendRefundNotificationEmailsAsync(OrderDetailsDto order, PaymentWebhookResult webhookResult, decimal? refundAmount)
    {
        try
        {
            var isPartialRefund = refundAmount.HasValue && refundAmount.Value < order.TotalAmount;
            var subject = isPartialRefund 
                ? $"Hoàn tiền một phần - Đơn hàng #{order.Id}"
                : $"Hoàn tiền - Đơn hàng #{order.Id}";

            await _emailQueueService.QueueEmailAsync(
                to: order.CustomerEmail,
                subject: subject,
                body: GenerateRefundNotificationEmailBody(order, webhookResult, refundAmount),
                isHtml: true,
                priority: EmailPriority.High);

            _logger.LogInformation("Refund notification emails queued for order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending refund notification emails for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Sends dispute notification emails
    /// </summary>
    private async Task SendDisputeNotificationEmailsAsync(OrderDetailsDto order, PaymentWebhookResult webhookResult)
    {
        try
        {
            // Admin notification for disputes
            await _emailService.SendAdminNotificationAsync(
                subject: $"Payment Dispute - Order #{order.Id}",
                content: $"Payment dispute initiated for Order #{order.Id}. " +
                        $"Amount: {order.TotalAmount:C}. " +
                        $"Gateway: {webhookResult.Gateway}. " +
                        $"Transaction: {webhookResult.TransactionId}",
                priority: "urgent");

            _logger.LogInformation("Dispute notification emails sent for order {OrderId}", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending dispute notification emails for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Generates payment confirmation email body
    /// </summary>
    private string GeneratePaymentConfirmationEmailBody(OrderDetailsDto order, PaymentWebhookResult webhookResult)
    {
        return $@"
            <h2>Cảm ơn bạn đã thanh toán!</h2>
            <p>Đơn hàng #{order.Id} của bạn đã được thanh toán thành công.</p>
            <ul>
                <li><strong>Số tiền:</strong> {order.TotalAmount:C}</li>
                <li><strong>Phương thức:</strong> {webhookResult.Gateway}</li>
                <li><strong>Mã giao dịch:</strong> {webhookResult.TransactionId}</li>
            </ul>
            <p>Chúng tôi sẽ xử lý đơn hàng và giao hàng trong thời gian sớm nhất.</p>";
    }

    /// <summary>
    /// Generates payment failure email body
    /// </summary>
    private string GeneratePaymentFailureEmailBody(OrderDetailsDto order, PaymentWebhookResult webhookResult)
    {
        return $@"
            <h2>Thanh toán không thành công</h2>
            <p>Rất tiếc, thanh toán cho đơn hàng #{order.Id} không thành công.</p>
            <ul>
                <li><strong>Số tiền:</strong> {order.TotalAmount:C}</li>
                <li><strong>Phương thức:</strong> {webhookResult.Gateway}</li>
                <li><strong>Lý do:</strong> {webhookResult.ErrorMessage ?? "Không xác định"}</li>
            </ul>
            <p>Vui lòng thử lại hoặc liên hệ với chúng tôi để được hỗ trợ.</p>";
    }

    /// <summary>
    /// Generates refund notification email body
    /// </summary>
    private string GenerateRefundNotificationEmailBody(OrderDetailsDto order, PaymentWebhookResult webhookResult, decimal? refundAmount)
    {
        var isPartialRefund = refundAmount.HasValue && refundAmount.Value < order.TotalAmount;
        var amountText = refundAmount?.ToString("C") ?? order.TotalAmount.ToString("C");

        return $@"
            <h2>Thông báo hoàn tiền</h2>
            <p>Chúng tôi đã xử lý hoàn tiền cho đơn hàng #{order.Id}.</p>
            <ul>
                <li><strong>Số tiền hoàn:</strong> {amountText}</li>
                <li><strong>Loại hoàn tiền:</strong> {(isPartialRefund ? "Hoàn tiền một phần" : "Hoàn tiền toàn bộ")}</li>
                <li><strong>Phương thức:</strong> {webhookResult.Gateway}</li>
                <li><strong>Mã giao dịch:</strong> {webhookResult.TransactionId}</li>
            </ul>
            <p>Tiền hoàn sẽ được chuyển về tài khoản của bạn trong 3-5 ngày làm việc.</p>";
    }
}