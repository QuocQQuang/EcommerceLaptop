using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Helper methods for payment retry functionality
/// </summary>
public static class PaymentRetryHelpers
{
    /// <summary>
    /// Gets the retry count for an order
    /// </summary>
    public static async Task<int> GetRetryCountAsync(int orderId, IOrderService orderService)
    {
        // In a real implementation, this would query the database for retry attempts
        // For now, return 0 as a placeholder
        return await Task.FromResult(0);
    }

    /// <summary>
    /// Gets the last retry time for an order
    /// </summary>
    public static async Task<DateTime?> GetLastRetryTimeAsync(int orderId, IOrderService orderService)
    {
        // In a real implementation, this would query the database for retry attempts
        // For now, return null as a placeholder
        return await Task.FromResult<DateTime?>(null);
    }

    /// <summary>
    /// Records a retry attempt for an order
    /// </summary>
    public static async Task RecordRetryAttemptAsync(
        int orderId,
        PaymentGateway gateway,
        PaymentMethod method,
        string userId,
        ILogger logger)
    {
        // In a real implementation, this would save to the database
        // For now, just log the attempt
        logger.LogInformation("Recording retry attempt for order {OrderId} with gateway {Gateway} and method {Method} by user {UserId}",
            orderId, gateway, method, userId);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Checks if an order is eligible for retry
    /// </summary>
    public static async Task<(bool IsEligible, string? Reason)> IsEligibleForRetryAsync(
        int orderId,
        IOrderService orderService)
    {
        try
        {
            var order = await orderService.GetOrderDetailsAsync(orderId);

            // Check if order exists
            if (order == null)
            {
                return (false, "Order not found");
            }

            // Check if order is in pending status
            if (order.Status != OrderStatus.Pending)
            {
                return (false, "Only pending orders can be retried");
            }

            // Check if order is already paid
            if (order.Status == OrderStatus.Confirmed ||
                order.Status == OrderStatus.Shipped ||
                order.Status == OrderStatus.Delivered)
            {
                return (false, "Order has already been paid");
            }

            // Check retry count
            var retryCount = await GetRetryCountAsync(orderId, orderService);
            var maxRetries = 3; // Could be configurable

            if (retryCount >= maxRetries)
            {
                return (false, $"Maximum retry attempts ({maxRetries}) exceeded");
            }

            // Check time since last retry
            var lastRetryTime = await GetLastRetryTimeAsync(orderId, orderService);
            if (lastRetryTime.HasValue)
            {
                var retryDelay = TimeSpan.FromMinutes(1); // Could be configurable
                if (DateTime.UtcNow - lastRetryTime.Value < retryDelay)
                {
                    var remainingTime = retryDelay - (DateTime.UtcNow - lastRetryTime.Value);
                    return (false, $"Please wait {remainingTime.TotalSeconds:F0} seconds before retrying");
                }
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, $"Error checking retry eligibility: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets retry limits for an order
    /// </summary>
    public static async Task<(int MaxRetries, TimeSpan RetryDelay, double BackoffMultiplier)> GetRetryLimitsAsync(
        int orderId,
        IOrderService orderService)
    {
        // In a real implementation, this could be configurable per order or user
        // For now, return default values
        return await Task.FromResult((
            MaxRetries: 3,
            RetryDelay: TimeSpan.FromMinutes(1),
            BackoffMultiplier: 2.0
        ));
    }
}
