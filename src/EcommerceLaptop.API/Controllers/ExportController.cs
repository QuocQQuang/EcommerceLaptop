using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Services;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController(
    IReportingService reportingService,
    ILogger<ExportController> logger) : BaseApiController(logger)
{
    private readonly IReportingService _reportingService = reportingService;

    /// <summary>
    /// Xut ha n PDF vi ch k s (Admin only)
    /// </summary>
    [HttpGet("invoice/pdf/{orderId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportInvoicePdf(int orderId, bool includeDigitalSignature = true)
    {
        var result = await _reportingService.ExportInvoicePdfAsync(orderId, includeDigitalSignature);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Xut ha n XML (Admin only)
    /// </summary>
    [HttpGet("invoice/xml/{orderId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportInvoiceXml(int orderId)
    {
        var result = await _reportingService.ExportInvoiceXmlAsync(orderId);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Xut danh sch n hng ra Excel (Admin only)
    /// </summary>
    [HttpGet("orders/excel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportOrdersToExcel(
        int page = 1,
        int pageSize = 1000,
        string? search = null,
        string? status = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var result = await _reportingService.ExportOrdersToExcelAsync(page, pageSize, search, status, startDate, endDate);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Xut danh sch sn phm ra Excel (Admin only)
    /// </summary>
    [HttpGet("products/excel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportProductsToExcel(
        int page = 1,
        int pageSize = 1000,
        string? search = null,
        int? categoryId = null,
        int? brandId = null,
        bool? isActive = null)
    {
        var result = await _reportingService.ExportProductsToExcelAsync(page, pageSize, search, categoryId, brandId, isActive);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Xut bo co doanh thu ra Excel (Admin only)
    /// </summary>
    [HttpGet("revenue/excel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportRevenueReportToExcel(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var result = await _reportingService.ExportRevenueReportToExcelAsync(startDate, endDate);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Xut bo co tn kho ra Excel (Admin only)
    /// </summary>
    [HttpGet("inventory/excel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportInventoryReportToExcel()
    {
        var result = await _reportingService.ExportInventoryReportToExcelAsync();
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Xut danh sch khch hng ra Excel (Admin only)
    /// </summary>
    [HttpGet("users/excel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportUsersToExcel(
        int page = 1,
        int pageSize = 1000,
        string? search = null,
        bool? isActive = null,
        int? vipLevel = null)
    {
        var result = await _reportingService.ExportUsersToExcelAsync(page, pageSize, search, isActive, vipLevel);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    #region Customer Export Endpoints

    /// <summary>
    /// Xut ha n PDF cho khch hng
    /// </summary>
    [HttpGet("customer/invoice/pdf/{orderId}")]
    [Authorize]
    public async Task<IActionResult> ExportCustomerInvoicePdf(int orderId, bool includeDigitalSignature = false)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để xuất hóa đơn" });
        }

        var result = await _reportingService.ExportCustomerInvoicePdfAsync(orderId, userId.Value, includeDigitalSignature);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Xut ha n XML cho khch hng
    /// </summary>
    [HttpGet("customer/invoice/xml/{orderId}")]
    [Authorize] 
    public async Task<IActionResult> ExportCustomerInvoiceXml(int orderId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
        {
            return Unauthorized(new { message = "Vui lòng đăng nhập để xuất hóa đơn" });
        }

        var result = await _reportingService.ExportCustomerInvoiceXmlAsync(orderId, userId.Value);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Xut danh sch s kin bo mt ra Excel (Admin only)
    /// </summary>
    [HttpGet("security-events/excel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportSecurityEventsToExcel(
        int page = 1,
        int pageSize = 1000,
        string? search = null,
        string? eventType = null,
        string? severity = null,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var result = await _reportingService.ExportSecurityEventsToExcelAsync(page, pageSize, search, eventType, severity, startDate, endDate);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Xut danh sch IP Block Rules ra Excel (Admin only)
    /// </summary>
    [HttpGet("ip-block-rules/excel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportIPBlockRulesToExcel(
        int page = 1,
        int pageSize = 1000,
        string? search = null,
        string? type = null,
        bool? isActive = null)
    {
        var result = await _reportingService.ExportIPBlockRulesToExcelAsync(page, pageSize, search, type, isActive);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Xut danh sch Rate Limit Rules ra Excel (Admin only)
    /// </summary>
    [HttpGet("rate-limit-rules/excel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportRateLimitRulesToExcel(
        int page = 1,
        int pageSize = 1000,
        string? search = null,
        bool? isActive = null)
    {
        var result = await _reportingService.ExportRateLimitRulesToExcelAsync(page, pageSize, search, isActive);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Xut bo co bo mt tng hp ra Excel (Admin only)
    /// </summary>
    [HttpGet("security-report/excel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportSecurityReportToExcel(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var result = await _reportingService.ExportSecurityReportToExcelAsync(startDate, endDate);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    #endregion
}
