using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs;
using ServicesRefundItemRequest = EcommerceLaptop.Core.Services.RefundItemRequest;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Controller for managing order refunds
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class RefundController(IRefundService refundService, ILogger<RefundController> logger) : BaseApiController(logger)
{
    private readonly IRefundService _refundService = refundService;

    /// <summary>
    /// Get all refund requests with pagination and filtering
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetRefunds(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null)
    {
        var result = await _refundService.GetRefundsAsync(
            pageNumber, pageSize, searchTerm, status, startDate, endDate, minAmount, maxAmount);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get refund by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRefund(int id)
    {
        var result = await _refundService.GetRefundByIdAsync(id);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        if (result.Data == null)
            return NotFound(new { error = "Refund not found" });

        return Ok(result.Data);
    }

    /// <summary>
    /// Get refunds for a specific order
    /// </summary>
    [HttpGet("order/{orderId}")]
    public async Task<IActionResult> GetRefundsByOrder(int orderId)
    {
        var result = await _refundService.GetRefundsByOrderAsync(orderId);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Create a new refund request
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateRefund([FromBody] CreateRefundRequest request)
    {
        // Convert DTO RefundItemRequest to Services RefundItemRequest
        List<ServicesRefundItemRequest>? serviceItems = request.ItemsToRefund?.Select(item => new ServicesRefundItemRequest
        {
            ProductId = item.ProductId,
            Quantity = item.Quantity,
            Reason = item.Reason
        }).ToList();

        var result = await _refundService.CreateRefundAsync(
            request.OrderId,
            request.Amount,
            request.Reason,
            request.RefundType,
            serviceItems);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return CreatedAtAction(nameof(GetRefund), new { id = result.Data.Id }, result.Data);
    }

    /// <summary>
    /// Process a refund (approve/reject)
    /// </summary>
    [HttpPost("{id}/process")]
    public async Task<IActionResult> ProcessRefund(int id, [FromBody] ProcessRefundRequest request)
    {
        var result = await _refundService.ProcessRefundAsync(
            id,
            request.Action,
            request.AdminNotes,
            request.ActualRefundAmount);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Cancel a refund request
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelRefund(int id, [FromBody] CancelRefundRequest request)
    {
        var result = await _refundService.CancelRefundAsync(id, request.Reason);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(new { message = "Refund cancelled successfully" });
    }

    /// <summary>
    /// Get refund statistics
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetRefundStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var result = await _refundService.GetRefundStatisticsAsync(startDate, endDate);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Bulk process refunds
    /// </summary>
    [HttpPost("bulk-process")]
    public async Task<IActionResult> BulkProcessRefunds([FromBody] BulkProcessRefundsRequest request)
    {
        var result = await _refundService.BulkProcessRefundsAsync(
            request.RefundIds,
            request.Action,
            request.AdminNotes);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        return Ok(result.Data);
    }

    /// <summary>
    /// Generate refund report
    /// </summary>
    [HttpGet("reports")]
    public async Task<IActionResult> GenerateRefundReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? status = null,
        [FromQuery] string format = "json")
    {
        var result = await _refundService.GenerateRefundReportAsync(startDate, endDate, status);

        if (!result.IsSuccess)
            return BadRequest(new { error = result.ErrorMessage });

        if (format.ToLower() == "csv")
        {
            // Convert to CSV and return as file
            var csvContent = ConvertToCSV(result.Data);
            var fileName = $"refund-report-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv";
            
            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), 
                "text/csv", fileName);
        }

        return Ok(result.Data);
    }

    private static string ConvertToCSV(List<RefundReportItem> items)
    {
        var csv = new System.Text.StringBuilder();
        csv.AppendLine("RefundId,OrderId,CustomerEmail,Amount,Status,RequestedDate,ProcessedDate,Reason");

        foreach (var item in items)
        {
            csv.AppendLine($"{item.RefundId},{item.OrderId},{item.CustomerEmail}," +
                          $"{item.Amount},{item.Status},{item.RequestedDate:yyyy-MM-dd}," +
                          $"{item.ProcessedDate?.ToString("yyyy-MM-dd")},\"{item.Reason}\"");
        }

        return csv.ToString();
    }
}