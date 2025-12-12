using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.ValueObjects;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Services.Security;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Service for managing order refunds with audit logging
/// </summary>
public class RefundService : IRefundService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<RefundService> _logger;
    private readonly IAuditLoggingService _auditLoggingService;

    public RefundService(
        ApplicationDbContext context,
        ILogger<RefundService> logger,
        IAuditLoggingService auditLoggingService)
    {
        _context = context;
        _logger = logger;
        _auditLoggingService = auditLoggingService;
    }

    /// <summary>
    /// Get paginated list of refund requests
    /// </summary>
    public async Task<ServiceResult<PaginatedList<RefundDto>>> GetRefundsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        decimal? minAmount = null,
        decimal? maxAmount = null)
    {
        try
        {
            var query = _context.Refunds
                .Include(r => r.Order)
                .ThenInclude(o => o.User)
                .Include(r => r.ProcessedByAdmin)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(r => r.Reason.Contains(searchTerm) ||
                                        r.Order.User.Email.Contains(searchTerm) ||
                                        (r.TransactionId != null && r.TransactionId.Contains(searchTerm)));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(r => r.Status == status);
            }

            if (startDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.CreatedAt <= endDate.Value);
            }

            if (minAmount.HasValue)
            {
                query = query.Where(r => r.Amount >= minAmount.Value);
            }

            if (maxAmount.HasValue)
            {
                query = query.Where(r => r.Amount <= maxAmount.Value);
            }

            var totalCount = await query.CountAsync();
            var refunds = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new RefundDto
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    RequestedAmount = r.Amount,
                    Reason = r.Reason,
                    Status = r.Status,
                    RequestedAt = r.CreatedAt,
                    ProcessedAt = r.ProcessedAt,
                    AdminNotes = r.GatewayResponse, // Using GatewayResponse for admin notes
                    RefundType = r.PaymentGateway ?? "manual",
                    CustomerEmail = r.Order.User.Email,
                    TransactionId = r.TransactionId,
                    PaymentMethod = r.PaymentGateway,
                    ProcessedBy = r.ProcessedByAdmin != null ? r.ProcessedByAdmin.Email : null
                })
                .ToListAsync();

            var paginatedList = new PaginatedList<RefundDto>(refunds, totalCount, pageNumber, pageSize);
            return ServiceResult<PaginatedList<RefundDto>>.Success(paginatedList);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refunds");
            return ServiceResult<PaginatedList<RefundDto>>.Failure("Failed to retrieve refunds");
        }
    }

    /// <summary>
    /// Get refund by ID
    /// </summary>
    public async Task<ServiceResult<RefundDto?>> GetRefundByIdAsync(int id)
    {
        try
        {
            var refund = await _context.Refunds
                .Include(r => r.Order)
                .ThenInclude(o => o.User)
                .Include(r => r.ProcessedByAdmin)
                .Where(r => r.Id == id)
                .Select(r => new RefundDto
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    RequestedAmount = r.Amount,
                    Reason = r.Reason,
                    Status = r.Status,
                    RequestedAt = r.CreatedAt,
                    ProcessedAt = r.ProcessedAt,
                    AdminNotes = r.GatewayResponse,
                    RefundType = r.PaymentGateway ?? "manual",
                    CustomerEmail = r.Order.User.Email,
                    TransactionId = r.TransactionId,
                    PaymentMethod = r.PaymentGateway,
                    ProcessedBy = r.ProcessedByAdmin != null ? r.ProcessedByAdmin.Email : null
                })
                .FirstOrDefaultAsync();

            return ServiceResult<RefundDto?>.Success(refund);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund by ID {RefundId}", id);
            return ServiceResult<RefundDto?>.Failure($"Failed to retrieve refund with ID {id}");
        }
    }

    /// <summary>
    /// Get all refunds for a specific order
    /// </summary>
    public async Task<ServiceResult<List<RefundDto>>> GetRefundsByOrderAsync(int orderId)
    {
        try
        {
            var refunds = await _context.Refunds
                .Include(r => r.Order)
                .ThenInclude(o => o.User)
                .Include(r => r.ProcessedByAdmin)
                .Where(r => r.OrderId == orderId)
                .Select(r => new RefundDto
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    RequestedAmount = r.Amount,
                    Reason = r.Reason,
                    Status = r.Status,
                    RequestedAt = r.CreatedAt,
                    ProcessedAt = r.ProcessedAt,
                    AdminNotes = r.GatewayResponse,
                    RefundType = r.PaymentGateway ?? "manual",
                    CustomerEmail = r.Order.User.Email,
                    TransactionId = r.TransactionId,
                    PaymentMethod = r.PaymentGateway,
                    ProcessedBy = r.ProcessedByAdmin != null ? r.ProcessedByAdmin.Email : null
                })
                .ToListAsync();

            return ServiceResult<List<RefundDto>>.Success(refunds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refunds for order {OrderId}", orderId);
            return ServiceResult<List<RefundDto>>.Failure($"Failed to retrieve refunds for order {orderId}");
        }
    }

    /// <summary>
    /// Create a new refund request
    /// </summary>
    public async Task<ServiceResult<Refund>> CreateRefundAsync(
        int orderId,
        decimal amount,
        string reason,
        string refundType,
        List<RefundItemRequest>? itemsToRefund = null)
    {
        try
        {
            // Validate order exists
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return ServiceResult<Refund>.Failure("Order not found");
            }

            // Validate refund amount
            if (amount <= 0 || amount > order.TotalAmount)
            {
                return ServiceResult<Refund>.Failure("Invalid refund amount");
            }

            // Create refund entity
            var refund = new Refund
            {
                OrderId = orderId,
                Amount = amount,
                Reason = reason,
                Status = "pending",
                PaymentGateway = refundType,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Refunds.Add(refund);
            await _context.SaveChangesAsync();

            // Log audit event
            await _auditLoggingService.LogEventAsync(
                entityType: "Refund",
                entityId: refund.Id.ToString(),
                eventType: "Create",
                eventCategory: "Refund",
                oldValues: null,
                newValues: new { refund.OrderId, refund.Amount, refund.Reason, refund.Status },
                userId: order.UserId.ToString(),
                adminUserId: null,
                ipAddress: null,
                userAgent: null,
                metadata: new { RefundType = refundType, ItemsToRefund = itemsToRefund?.Count ?? 0 }
            );

            _logger.LogInformation("Refund created for order {OrderId} with amount {Amount}", orderId, amount);
            return ServiceResult<Refund>.Success(refund);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating refund for order {OrderId}", orderId);
            return ServiceResult<Refund>.Failure("Failed to create refund request");
        }
    }

    /// <summary>
    /// Update refund status
    /// </summary>
    public async Task<ServiceResult<Refund>> UpdateRefundStatusAsync(
        int refundId,
        string status,
        string? adminNotes = null,
        int? processedByAdminId = null)
    {
        try
        {
            var refund = await _context.Refunds.FindAsync(refundId);
            if (refund == null)
            {
                return ServiceResult<Refund>.Failure("Refund not found");
            }

            var oldStatus = refund.Status;
            refund.Status = status;
            refund.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(adminNotes))
            {
                refund.GatewayResponse = adminNotes;
            }

            if (processedByAdminId.HasValue)
            {
                refund.ProcessedByAdminId = processedByAdminId.Value;
                refund.ProcessedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            // Log audit event
            await _auditLoggingService.LogEventAsync(
                entityType: "Refund",
                entityId: refund.Id.ToString(),
                eventType: "Update",
                eventCategory: "Refund",
                oldValues: new { Status = oldStatus },
                newValues: new { Status = status, AdminNotes = adminNotes },
                userId: refund.Order?.UserId.ToString(),
                adminUserId: processedByAdminId?.ToString(),
                ipAddress: null,
                userAgent: null,
                metadata: new { StatusChange = $"{oldStatus} -> {status}" }
            );

            _logger.LogInformation("Refund {RefundId} status updated from {OldStatus} to {NewStatus}",
                refundId, oldStatus, status);
            return ServiceResult<Refund>.Success(refund);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating refund status for {RefundId}", refundId);
            return ServiceResult<Refund>.Failure("Failed to update refund status");
        }
    }

    /// <summary>
    /// Process refund
    /// </summary>
    public async Task<ServiceResult<Refund>> ProcessRefundAsync(
        int refundId,
        int adminId,
        bool approved,
        string? adminNotes = null)
    {
        try
        {
            var refund = await _context.Refunds
                .Include(r => r.Order)
                .FirstOrDefaultAsync(r => r.Id == refundId);

            if (refund == null)
            {
                return ServiceResult<Refund>.Failure("Refund not found");
            }

            var oldStatus = refund.Status;
            refund.Status = approved ? "approved" : "rejected";
            refund.ProcessedByAdminId = adminId;
            refund.ProcessedAt = DateTime.UtcNow;
            refund.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(adminNotes))
            {
                refund.GatewayResponse = adminNotes;
            }

            await _context.SaveChangesAsync();

            // Log audit event
            await _auditLoggingService.LogEventAsync(
                entityType: "Refund",
                entityId: refund.Id.ToString(),
                eventType: approved ? "Approve" : "Reject",
                eventCategory: "Refund",
                oldValues: new { Status = oldStatus },
                newValues: new { Status = refund.Status, ProcessedBy = adminId, AdminNotes = adminNotes },
                userId: refund.Order?.UserId.ToString(),
                adminUserId: adminId.ToString(),
                ipAddress: null,
                userAgent: null,
                metadata: new
                {
                    Action = approved ? "Approved" : "Rejected",
                    Amount = refund.Amount,
                    OrderId = refund.OrderId
                }
            );

            _logger.LogInformation("Refund {RefundId} {Action} by admin {AdminId}",
                refundId, approved ? "approved" : "rejected", adminId);
            return ServiceResult<Refund>.Success(refund);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refund {RefundId}", refundId);
            return ServiceResult<Refund>.Failure("Failed to process refund");
        }
    }

    /// <summary>
    /// Get refund statistics
    /// </summary>
    public async Task<ServiceResult<RefundStatistics>> GetRefundStatisticsAsync(int days = 30)
    {
        try
        {
            var startDate = DateTime.UtcNow.AddDays(-days);

            var stats = await _context.Refunds
                .Where(r => r.CreatedAt >= startDate)
                .GroupBy(r => 1)
                .Select(g => new RefundStatistics
                {
                    TotalRefundRequests = g.Count(),
                    TotalRefundAmount = g.Sum(r => r.Amount),
                    PendingRefunds = g.Count(r => r.Status == "pending"),
                    ApprovedRefunds = g.Count(r => r.Status == "approved"),
                    RejectedRefunds = g.Count(r => r.Status == "rejected"),
                    CancelledRefunds = g.Count(r => r.Status == "processed"),
                    AverageRefundAmount = g.Average(r => r.Amount)
                })
                .FirstOrDefaultAsync();

            if (stats == null)
            {
                stats = new RefundStatistics();
            }

            return ServiceResult<RefundStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund statistics");
            return ServiceResult<RefundStatistics>.Failure("Failed to retrieve refund statistics");
        }
    }

    /// <summary>
    /// Get refund eligibility for an order
    /// </summary>
    public async Task<ServiceResult<RefundEligibility>> CheckRefundEligibilityAsync(int orderId)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.Refunds)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return ServiceResult<RefundEligibility>.Failure("Order not found");
            }

            var eligibility = new RefundEligibility
            {
                IsEligible = true,
                MaxRefundAmount = order.TotalAmount,
                Restrictions = new List<string>()
            };

            // Check if order is too old (30 days policy)
            if (order.OrderDate < DateTime.UtcNow.AddDays(-30))
            {
                eligibility.IsEligible = false;
                eligibility.Restrictions.Add("Order is older than 30 days");
            }

            // Check existing refunds
            var existingRefunds = order.Refunds.Where(r => r.Status != "rejected").Sum(r => r.Amount);
            if (existingRefunds >= order.TotalAmount)
            {
                eligibility.IsEligible = false;
                eligibility.Restrictions.Add("Order has already been fully refunded");
                eligibility.MaxRefundAmount = 0;
            }
            else
            {
                eligibility.MaxRefundAmount = order.TotalAmount - existingRefunds;
            }

            // Check order status
            if (order.Status != OrderStatus.Delivered && order.Status != OrderStatus.Processing)
            {
                eligibility.Restrictions.Add($"Order status is '{order.Status}' - consider cancellation instead");
            }

            return ServiceResult<RefundEligibility>.Success(eligibility);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking refund eligibility for order {OrderId}", orderId);
            return ServiceResult<RefundEligibility>.Failure("Failed to check refund eligibility");
        }
    }

    /// <summary>
    /// Calculate refund amount including taxes and fees
    /// </summary>
    public async Task<ServiceResult<RefundCalculation>> CalculateRefundAsync(int orderId, List<RefundItemRequest> items)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return ServiceResult<RefundCalculation>.Failure("Order not found");
            }

            var calculation = new RefundCalculation
            {
                ItemCalculations = new List<RefundItemCalculation>()
            };

            decimal totalRefundAmount = 0;

            foreach (var item in items)
            {
                var orderItem = order.OrderItems.FirstOrDefault(oi => oi.ProductId == item.ProductId);
                if (orderItem != null)
                {
                    var refundQty = Math.Min(item.Quantity, orderItem.Quantity);
                    var unitRefund = orderItem.UnitPrice * refundQty;

                    calculation.ItemCalculations.Add(new RefundItemCalculation
                    {
                        ProductId = item.ProductId,
                        ProductName = orderItem.Product.Name,
                        RefundQuantity = refundQty,
                        UnitPrice = orderItem.UnitPrice,
                        ItemSubtotal = unitRefund,
                        ItemTax = 0, // Calculate tax if needed
                        ItemTotal = unitRefund
                    });

                    totalRefundAmount += unitRefund;
                }
            }

            // Calculate proportional shipping and tax refunds
            var itemsRatio = totalRefundAmount / order.SubTotal;
            calculation.SubtotalRefund = totalRefundAmount;
            calculation.ShippingRefund = order.ShippingAmount * itemsRatio;
            calculation.TaxRefund = order.TotalAmount * 0.1m * itemsRatio; // Assuming 10% tax
            calculation.TotalRefund = totalRefundAmount + calculation.ShippingRefund + calculation.TaxRefund;

            return ServiceResult<RefundCalculation>.Success(calculation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating refund for order {OrderId}", orderId);
            return ServiceResult<RefundCalculation>.Failure("Failed to calculate refund amount");
        }
    }

    /// <summary>
    /// Get refunds by user ID
    /// </summary>
    public async Task<ServiceResult<List<RefundDto>>> GetRefundsByUserAsync(int userId)
    {
        try
        {
            var refunds = await _context.Refunds
                .Include(r => r.Order)
                .ThenInclude(o => o.User)
                .Include(r => r.ProcessedByAdmin)
                .Where(r => r.Order.UserId == userId)
                .Select(r => new RefundDto
                {
                    Id = r.Id,
                    OrderId = r.OrderId,
                    RequestedAmount = r.Amount,
                    Reason = r.Reason,
                    Status = r.Status,
                    RequestedAt = r.CreatedAt,
                    ProcessedAt = r.ProcessedAt,
                    AdminNotes = r.GatewayResponse,
                    RefundType = r.PaymentGateway ?? "manual",
                    CustomerEmail = r.Order.User.Email,
                    TransactionId = r.TransactionId,
                    PaymentMethod = r.PaymentGateway,
                    ProcessedBy = r.ProcessedByAdmin != null ? r.ProcessedByAdmin.Email : null
                })
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            return ServiceResult<List<RefundDto>>.Success(refunds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refunds for user {UserId}", userId);
            return ServiceResult<List<RefundDto>>.Failure($"Failed to retrieve refunds for user {userId}");
        }
    }

    /// <summary>
    /// Process refund with action string (interface compatibility)
    /// </summary>
    public async Task<ServiceResult<Refund>> ProcessRefundAsync(
        int refundId,
        string action,
        string? adminNotes = null,
        decimal? actualRefundAmount = null)
    {
        var approved = action.Equals("approve", StringComparison.OrdinalIgnoreCase);
        return await ProcessRefundAsync(refundId, 0, approved, adminNotes); // Use 0 as placeholder admin ID
    }

    /// <summary>
    /// Cancel a refund request
    /// </summary>
    public async Task<ServiceResult<bool>> CancelRefundAsync(int refundId, string? reason = null)
    {
        try
        {
            var refund = await _context.Refunds.FindAsync(refundId);
            if (refund == null)
            {
                return ServiceResult<bool>.Failure("Refund not found");
            }

            if (refund.Status != "pending")
            {
                return ServiceResult<bool>.Failure("Only pending refunds can be cancelled");
            }

            var oldStatus = refund.Status;
            refund.Status = "cancelled";
            refund.UpdatedAt = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(reason))
            {
                refund.GatewayResponse = reason;
            }

            await _context.SaveChangesAsync();

            // Log audit event
            await _auditLoggingService.LogEventAsync(
                entityType: "Refund",
                entityId: refund.Id.ToString(),
                eventType: "Cancel",
                eventCategory: "Refund",
                oldValues: new { Status = oldStatus },
                newValues: new { Status = "cancelled", Reason = reason },
                userId: refund.Order?.UserId.ToString(),
                adminUserId: null,
                ipAddress: null,
                userAgent: null,
                metadata: new { CancelReason = reason }
            );

            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling refund {RefundId}", refundId);
            return ServiceResult<bool>.Failure("Failed to cancel refund");
        }
    }

    /// <summary>
    /// Get refund statistics with date range
    /// </summary>
    public async Task<ServiceResult<RefundStatistics>> GetRefundStatisticsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        try
        {
            var query = _context.Refunds.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt <= endDate.Value);

            var stats = await query
                .GroupBy(r => 1)
                .Select(g => new RefundStatistics
                {
                    TotalRefundRequests = g.Count(),
                    TotalRefundAmount = g.Sum(r => r.Amount),
                    PendingRefunds = g.Count(r => r.Status == "pending"),
                    ApprovedRefunds = g.Count(r => r.Status == "approved"),
                    RejectedRefunds = g.Count(r => r.Status == "rejected"),
                    CancelledRefunds = g.Count(r => r.Status == "processed"),
                    AverageRefundAmount = g.Average(r => r.Amount)
                })
                .FirstOrDefaultAsync();

            if (stats == null)
            {
                stats = new RefundStatistics();
            }

            return ServiceResult<RefundStatistics>.Success(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting refund statistics");
            return ServiceResult<RefundStatistics>.Failure("Failed to retrieve refund statistics");
        }
    }

    /// <summary>
    /// Bulk process multiple refunds
    /// </summary>
    public async Task<ServiceResult<BulkRefundProcessResult>> BulkProcessRefundsAsync(
        List<int> refundIds,
        string action,
        string? adminNotes = null)
    {
        try
        {
            var result = new BulkRefundProcessResult
            {
                TotalProcessed = 0,
                SuccessfullyProcessed = 0,
                Failed = 0,
                Errors = new List<string>(),
                ProcessedRefundIds = new List<int>()
            };

            var approved = action.Equals("approve", StringComparison.OrdinalIgnoreCase);

            foreach (var refundId in refundIds)
            {
                try
                {
                    var processResult = await ProcessRefundAsync(refundId, 0, approved, adminNotes);
                    if (processResult.IsSuccess)
                    {
                        result.SuccessfullyProcessed++;
                        result.ProcessedRefundIds.Add(refundId);
                    }
                    else
                    {
                        result.Failed++;
                        result.Errors.Add($"Refund {refundId}: {processResult.ErrorMessage}");
                    }
                }
                catch (Exception ex)
                {
                    result.Failed++;
                    result.Errors.Add($"Refund {refundId}: {ex.Message}");
                }
            }

            result.TotalProcessed = result.SuccessfullyProcessed + result.Failed;
            return ServiceResult<BulkRefundProcessResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk refund processing");
            return ServiceResult<BulkRefundProcessResult>.Failure("Failed to process refunds in bulk");
        }
    }

    /// <summary>
    /// Generate refund report
    /// </summary>
    public async Task<ServiceResult<List<RefundReportItem>>> GenerateRefundReportAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? status = null)
    {
        try
        {
            var query = _context.Refunds
                .Include(r => r.Order)
                .ThenInclude(o => o.User)
                .Include(r => r.ProcessedByAdmin)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(r => r.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(r => r.CreatedAt <= endDate.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(r => r.Status == status);

            var reportItems = await query
                .Select(r => new RefundReportItem
                {
                    RefundId = r.Id,
                    OrderId = r.OrderId,
                    CustomerEmail = r.Order.User.Email,
                    Amount = r.Amount,
                    Status = r.Status,
                    Reason = r.Reason,
                    RequestedDate = r.CreatedAt,
                    ProcessedDate = r.ProcessedAt
                })
                .OrderByDescending(r => r.RequestedDate)
                .ToListAsync();

            return ServiceResult<List<RefundReportItem>>.Success(reportItems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating refund report");
            return ServiceResult<List<RefundReportItem>>.Failure("Failed to generate refund report");
        }
    }

    /// <summary>
    /// Calculate refund amount (interface method)
    /// </summary>
    public async Task<ServiceResult<RefundCalculation>> CalculateRefundAmountAsync(
        int orderId,
        List<RefundItemRequest>? itemsToRefund = null)
    {
        if (itemsToRefund == null || !itemsToRefund.Any())
        {
            // Default to full order refund calculation
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return ServiceResult<RefundCalculation>.Failure("Order not found");
            }

            var fullRefundCalc = new RefundCalculation
            {
                TotalRefund = order.TotalAmount,
                SubtotalRefund = order.SubTotal,
                ItemCalculations = new List<RefundItemCalculation>(),
                ShippingRefund = order.ShippingAmount,
                TaxRefund = order.TaxAmount,
                RestockingFee = 0
            };

            return ServiceResult<RefundCalculation>.Success(fullRefundCalc);
        }

        return await CalculateRefundAsync(orderId, itemsToRefund);
    }
}