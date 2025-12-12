using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.DTOs;

/// <summary>
/// Request DTO for creating a refund
/// </summary>
public record CreateRefundRequest(
    [Required(ErrorMessage = "Order ID is required")] int OrderId,
    [Required(ErrorMessage = "Amount is required")] [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")] decimal Amount,
    [Required(ErrorMessage = "Reason is required")] [StringLength(1000, ErrorMessage = "Reason cannot exceed 1000 characters")] string Reason,
    [Required(ErrorMessage = "Refund type is required")] string RefundType,
    List<RefundItemRequest>? ItemsToRefund = null);

/// <summary>
/// Request DTO for processing a refund
/// </summary>
public record ProcessRefundRequest(
    [Required(ErrorMessage = "Action is required")] string Action,
    [StringLength(1000, ErrorMessage = "Admin notes cannot exceed 1000 characters")] string? AdminNotes = null,
    [Range(0.01, double.MaxValue, ErrorMessage = "Actual refund amount must be greater than 0")] decimal? ActualRefundAmount = null);

/// <summary>
/// Request DTO for cancelling a refund
/// </summary>
public record CancelRefundRequest(
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")] string? Reason = null);

/// <summary>
/// Request DTO for bulk processing refunds
/// </summary>
public record BulkProcessRefundsRequest(
    [Required(ErrorMessage = "Refund IDs are required")] [MinLength(1, ErrorMessage = "At least one refund ID is required")] List<int> RefundIds,
    [Required(ErrorMessage = "Action is required")] string Action,
    [StringLength(1000, ErrorMessage = "Admin notes cannot exceed 1000 characters")] string? AdminNotes = null);

/// <summary>
/// Request DTO for refunding specific items
/// </summary>
public record RefundItemRequest(
    [Required(ErrorMessage = "Product ID is required")] int ProductId,
    [Required(ErrorMessage = "Quantity is required")] [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than 0")] int Quantity,
    [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")] string? Reason = null);