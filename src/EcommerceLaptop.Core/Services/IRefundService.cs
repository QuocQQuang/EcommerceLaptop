using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.ValueObjects;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Service interface for managing order refunds
/// </summary>
public interface IRefundService
{
    /// <summary>
    /// Get paginated list of refund requests
    /// </summary>
    Task<ServiceResult<PaginatedList<RefundDto>>> GetRefundsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        decimal? minAmount = null,
        decimal? maxAmount = null);

    /// <summary>
    /// Get refund by ID
    /// </summary>
    Task<ServiceResult<RefundDto?>> GetRefundByIdAsync(int id);

    /// <summary>
    /// Get all refunds for a specific order
    /// </summary>
    Task<ServiceResult<List<RefundDto>>> GetRefundsByOrderAsync(int orderId);

    /// <summary>
    /// Create a new refund request
    /// </summary>
    Task<ServiceResult<Refund>> CreateRefundAsync(
        int orderId,
        decimal amount,
        string reason,
        string refundType,
        List<RefundItemRequest>? itemsToRefund = null);

    /// <summary>
    /// Process a refund request (approve/reject)
    /// </summary>
    Task<ServiceResult<Refund>> ProcessRefundAsync(
        int refundId,
        string action,
        string? adminNotes = null,
        decimal? actualRefundAmount = null);

    /// <summary>
    /// Cancel a refund request
    /// </summary>
    Task<ServiceResult<bool>> CancelRefundAsync(int refundId, string? reason = null);

    /// <summary>
    /// Get refund statistics
    /// </summary>
    Task<ServiceResult<RefundStatistics>> GetRefundStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null);

    /// <summary>
    /// Bulk process multiple refunds
    /// </summary>
    Task<ServiceResult<BulkRefundProcessResult>> BulkProcessRefundsAsync(
        List<int> refundIds,
        string action,
        string? adminNotes = null);

    /// <summary>
    /// Generate refund report
    /// </summary>
    Task<ServiceResult<List<RefundReportItem>>> GenerateRefundReportAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? status = null);

    /// <summary>
    /// Check if order is eligible for refund
    /// </summary>
    Task<ServiceResult<RefundEligibility>> CheckRefundEligibilityAsync(int orderId);

    /// <summary>
    /// Calculate refund amount based on order items and policies
    /// </summary>
    Task<ServiceResult<RefundCalculation>> CalculateRefundAmountAsync(
        int orderId,
        List<RefundItemRequest>? itemsToRefund = null);
}

/// <summary>
/// DTO for refund information with related data
/// </summary>
public class RefundDto
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal RequestedAmount { get; set; }
    public decimal? ActualRefundAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string RefundType { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? ProcessedBy { get; set; }
    public string? AdminNotes { get; set; }
    public string? PaymentMethod { get; set; }
    public string? TransactionId { get; set; }
    public List<RefundItemDto> Items { get; set; } = new();
}

/// <summary>
/// DTO for refund item details
/// </summary>
public class RefundItemDto
{
    public int RefundItemId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSku { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Refund statistics
/// </summary>
public class RefundStatistics
{
    public int TotalRefundRequests { get; set; }
    public int PendingRefunds { get; set; }
    public int ApprovedRefunds { get; set; }
    public int RejectedRefunds { get; set; }
    public int CancelledRefunds { get; set; }
    public decimal TotalRefundAmount { get; set; }
    public decimal AverageRefundAmount { get; set; }
    public decimal RefundRate { get; set; } // Percentage of orders refunded
    public Dictionary<string, int> RefundReasonBreakdown { get; set; } = new();
    public Dictionary<string, decimal> RefundAmountByStatus { get; set; } = new();
    public List<DailyRefundSummary> DailyBreakdown { get; set; } = new();
}

/// <summary>
/// Daily refund summary for charts
/// </summary>
public class DailyRefundSummary
{
    public DateTime Date { get; set; }
    public int RefundCount { get; set; }
    public decimal TotalAmount { get; set; }
}

/// <summary>
/// Bulk refund processing result
/// </summary>
public class BulkRefundProcessResult
{
    public int TotalProcessed { get; set; }
    public int SuccessfullyProcessed { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<int> ProcessedRefundIds { get; set; } = new();
}

/// <summary>
/// Refund report item
/// </summary>
public class RefundReportItem
{
    public int RefundId { get; set; }
    public int OrderId { get; set; }
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedDate { get; set; }
    public DateTime? ProcessedDate { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Refund eligibility check result
/// </summary>
public class RefundEligibility
{
    public bool IsEligible { get; set; }
    public string Reason { get; set; } = string.Empty;
    public decimal MaxRefundAmount { get; set; }
    public List<string> Restrictions { get; set; } = new();
    public DateTime? CutoffDate { get; set; }
}

/// <summary>
/// Refund amount calculation result
/// </summary>
public class RefundCalculation
{
    public decimal SubtotalRefund { get; set; }
    public decimal TaxRefund { get; set; }
    public decimal ShippingRefund { get; set; }
    public decimal TotalRefund { get; set; }
    public decimal RestockingFee { get; set; }
    public List<RefundItemCalculation> ItemCalculations { get; set; } = new();
}

/// <summary>
/// Individual item refund calculation
/// </summary>
public class RefundItemCalculation
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int RefundQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal ItemSubtotal { get; set; }
    public decimal ItemTax { get; set; }
    public decimal ItemTotal { get; set; }
}

/// <summary>
/// Request for refunding specific items
/// </summary>
public class RefundItemRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string? Reason { get; set; }
}