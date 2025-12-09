using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.DTOs;

/// <summary>
/// Request DTO for creating a refund
/// </summary>
public class CreateRefundRequest
{
    [Required(ErrorMessage = "Order ID is required")]
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Reason is required")]
    [StringLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")]
    public string Reason { get; set; } = string.Empty;

    [Required(ErrorMessage = "Refund type is required")]
    public string RefundType { get; set; } = string.Empty; // "full", "partial", "item"

    public List<RefundItemRequest>? ItemsToRefund { get; set; }
}

/// <summary>
/// Request DTO for processing a refund
/// </summary>
public class ProcessRefundRequest
{
    [Required(ErrorMessage = "Action is required")]
    public string Action { get; set; } = string.Empty; // "approve", "reject"

    [StringLength(1000, ErrorMessage = "Admin notes cannot exceed 1000 characters")]
    public string? AdminNotes { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Actual refund amount must be greater than 0")]
    public decimal? ActualRefundAmount { get; set; }
}

/// <summary>
/// Request DTO for cancelling a refund
/// </summary>
public class CancelRefundRequest
{
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }
}

/// <summary>
/// Request DTO for bulk processing refunds
/// </summary>
public class BulkProcessRefundsRequest
{
    [Required(ErrorMessage = "Refund IDs are required")]
    [MinLength(1, ErrorMessage = "At least one refund ID is required")]
    public List<int> RefundIds { get; set; } = new();

    [Required(ErrorMessage = "Action is required")]
    public string Action { get; set; } = string.Empty; // "approve", "reject"

    [StringLength(1000, ErrorMessage = "Admin notes cannot exceed 1000 characters")]
    public string? AdminNotes { get; set; }
}

/// <summary>
/// Request DTO for refunding specific items
/// </summary>
public class RefundItemRequest
{
    [Required(ErrorMessage = "Product ID is required")]
    public int ProductId { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")]
    public int Quantity { get; set; }

    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
    public string? Reason { get; set; }
}