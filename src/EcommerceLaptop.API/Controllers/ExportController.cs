using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Services;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController : BaseApiController
{
    private readonly IReportingService _reportingService;

    public ExportController(
        IReportingService reportingService,
        ILogger<ExportController> logger) : base(logger)
    {
        _reportingService = reportingService;
    }

    /// <summary>
    /// Xut ha n PDF vi ch k s (Admin only)
    /// </summary>
    [HttpGet("invoice/pdf/{orderId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportInvoicePdf(int orderId, bool includeDigitalSignature = true)
    {
        try
        {
            var result = await _reportingService.ExportInvoicePdfAsync(orderId, includeDigitalSignature);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting invoice PDF for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Li khi xut ha n PDF" });
        }
    }

    /// <summary>
    /// Xut ha n XML (Admin only)
    /// </summary>
    [HttpGet("invoice/xml/{orderId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportInvoiceXml(int orderId)
    {
        try
        {
            var result = await _reportingService.ExportInvoiceXmlAsync(orderId);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting invoice XML for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Li khi xut ha n XML" });
        }
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
        try
        {
            var result = await _reportingService.ExportOrdersToExcelAsync(page, pageSize, search, status, startDate, endDate);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting orders to Excel");
            return StatusCode(500, new { message = "Li khi xut danh sch n hng" });
        }
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
        try
        {
            var result = await _reportingService.ExportProductsToExcelAsync(page, pageSize, search, categoryId, brandId, isActive);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting products to Excel");
            return StatusCode(500, new { message = "Li khi xut danh sch sn phm" });
        }
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
        try
        {
            var result = await _reportingService.ExportRevenueReportToExcelAsync(startDate, endDate);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting revenue report to Excel");
            return StatusCode(500, new { message = "Li khi xut bo co doanh thu" });
        }
    }

    /// <summary>
    /// Xut bo co tn kho ra Excel (Admin only)
    /// </summary>
    [HttpGet("inventory/excel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportInventoryReportToExcel()
    {
        try
        {
            var result = await _reportingService.ExportInventoryReportToExcelAsync();
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting inventory report to Excel");
            return StatusCode(500, new { message = "Li khi xut bo co tn kho" });
        }
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
        try
        {
            var result = await _reportingService.ExportUsersToExcelAsync(page, pageSize, search, isActive, vipLevel);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users to Excel");
            return StatusCode(500, new { message = "Li khi xut danh sch khch hng" });
        }
    }

    #region Customer Export Endpoints

    /// <summary>
    /// Xut ha n PDF cho khch hng
    /// </summary>
    [HttpGet("customer/invoice/pdf/{orderId}")]
    [Authorize]
    public async Task<IActionResult> ExportCustomerInvoicePdf(int orderId, bool includeDigitalSignature = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Vui lng ng nhp  xut ha n" });
            }

            var result = await _reportingService.ExportCustomerInvoicePdfAsync(orderId, userId.Value, includeDigitalSignature);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting customer invoice PDF for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Li khi xut ha n PDF" });
        }
    }

    /// <summary>
    /// Xut ha n XML cho khch hng
    /// </summary>
    [HttpGet("customer/invoice/xml/{orderId}")]
    [Authorize] 
    public async Task<IActionResult> ExportCustomerInvoiceXml(int orderId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Vui lng ng nhp  xut ha n" });
            }

            var result = await _reportingService.ExportCustomerInvoiceXmlAsync(orderId, userId.Value);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting customer invoice XML for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Li khi xut ha n XML" });
        }
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
        try
        {
            var result = await _reportingService.ExportSecurityEventsToExcelAsync(page, pageSize, search, eventType, severity, startDate, endDate);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting security events to Excel");
            return StatusCode(500, new { message = "Li khi xut danh sch s kin bo mt" });
        }
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
        try
        {
            var result = await _reportingService.ExportIPBlockRulesToExcelAsync(page, pageSize, search, type, isActive);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting IP block rules to Excel");
            return StatusCode(500, new { message = "Li khi xut danh sch IP Block Rules" });
        }
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
        try
        {
            var result = await _reportingService.ExportRateLimitRulesToExcelAsync(page, pageSize, search, isActive);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting rate limit rules to Excel");
            return StatusCode(500, new { message = "Li khi xut danh sch Rate Limit Rules" });
        }
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
        try
        {
            var result = await _reportingService.ExportSecurityReportToExcelAsync(startDate, endDate);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting security report to Excel");
            return StatusCode(500, new { message = "Li khi xut bo co bo mt tng hp" });
        }
    }

    #endregion
}
