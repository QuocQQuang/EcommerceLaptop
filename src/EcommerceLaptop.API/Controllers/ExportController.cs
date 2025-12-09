using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceLaptop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController : BaseApiController
{
    private readonly IPdfExportService _pdfExportService;
    private readonly IExcelExportService _excelExportService;
    private readonly ApplicationDbContext _context;

    public ExportController(
        IPdfExportService pdfExportService,
        IExcelExportService excelExportService,
        ApplicationDbContext context,
        ILogger<ExportController> logger) : base(logger)
    {
        _pdfExportService = pdfExportService;
        _excelExportService = excelExportService;
        _context = context;
    }

    /// <summary>
    /// Xut ha n PDF vi ch k s (Admin only) - Ch cho php xut ha n n hng  xc nhn tr ln
    /// </summary>
    /// <param name="orderId">ID n hng</param>
    /// <param name="includeDigitalSignature">C bao gm ch k s hay khng (mc nh: true)</param>
    /// <returns>File PDF ha n</returns>
    [HttpGet("invoice/pdf/{orderId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportInvoicePdf(int orderId, bool includeDigitalSignature = true)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound(new { message = "Khng tm thy n hng" });
            }

            // Ch cho php xut ha n cho n hng  xc nhn tr ln (tr  hy)
            if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Cancelled)
            {
                return BadRequest(new
                {
                    message = "Ch c th xut ha n cho n hng  xc nhn tr ln",
                    currentStatus = order.Status.ToString(),
                    allowedStatuses = new[] { "Confirmed", "Processing", "Shipped", "Delivered", "Returned", "Refunded" }
                });
            }

            var pdfBytes = await _pdfExportService.ExportInvoicePdfAsync(order, includeDigitalSignature);

            var fileName = $"HoaDon_{order.OrderNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting invoice PDF for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Li khi xut ha n PDF" });
        }
    }

    /// <summary>
    /// Xut ha n XML (Admin only) - Ch cho php xut ha n n hng  xc nhn tr ln
    /// </summary>
    /// <param name="orderId">ID n hng</param>
    /// <returns>File XML ha n</returns>
    [HttpGet("invoice/xml/{orderId}")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportInvoiceXml(int orderId)
    {
        try
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound(new { message = "Khng tm thy n hng" });
            }

            // Ch cho php xut ha n cho n hng  xc nhn tr ln (tr  hy)
            if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Cancelled)
            {
                return BadRequest(new
                {
                    message = "Ch c th xut ha n cho n hng  xc nhn tr ln",
                    currentStatus = order.Status.ToString(),
                    allowedStatuses = new[] { "Confirmed", "Processing", "Shipped", "Delivered", "Returned", "Refunded" }
                });
            }

            var xmlBytes = await _pdfExportService.ExportInvoiceXmlAsync(order);

            var fileName = $"HoaDon_{order.OrderNumber}_{DateTime.Now:yyyyMMddHHmmss}.xml";

            return File(xmlBytes, "application/xml", fileName);
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
    /// <param name="page">Trang (mc nh: 1)</param>
    /// <param name="pageSize">S lng mi trang (mc nh: 1000)</param>
    /// <param name="search">Tm kim theo m n hng hoc tn khch hng</param>
    /// <param name="status">Lc theo trng thi n hng</param>
    /// <param name="startDate">Ngy bt u (format: yyyy-MM-dd)</param>
    /// <param name="endDate">Ngy kt thc (format: yyyy-MM-dd)</param>
    /// <returns>File Excel danh sch n hng</returns>
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
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.OrderNumber.Contains(search) || (o.User.FirstName + " " + o.User.LastName).Contains(search));
            }

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
            {
                query = query.Where(o => o.Status == orderStatus);
            }

            if (startDate.HasValue)
            {
                query = query.Where(o => o.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(o => o.OrderDate <= endDate.Value.AddDays(1));
            }

            // Get orders
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var excelBytes = await _excelExportService.ExportOrdersToExcelAsync(orders);

            var fileName = $"DanhSachDonHang_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
    /// <param name="page">Trang (mc nh: 1)</param>
    /// <param name="pageSize">S lng mi trang (mc nh: 1000)</param>
    /// <param name="search">Tm kim theo tn sn phm</param>
    /// <param name="categoryId">Lc theo danh mc</param>
    /// <param name="brandId">Lc theo thng hiu</param>
    /// <param name="isActive">Lc theo trng thi hot ng</param>
    /// <returns>File Excel danh sch sn phm</returns>
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
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductBrand)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.Name.Contains(search));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            if (brandId.HasValue)
            {
                query = query.Where(p => p.BrandId == brandId.Value);
            }

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            // Get products
            var products = await query
                .OrderBy(p => p.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var excelBytes = await _excelExportService.ExportProductsToExcelAsync(products);

            var fileName = $"DanhSachSanPham_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
    /// <param name="startDate">Ngy bt u (format: yyyy-MM-dd)</param>
    /// <param name="endDate">Ngy kt thc (format: yyyy-MM-dd)</param>
    /// <returns>File Excel bo co doanh thu</returns>
    [HttpGet("revenue/excel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportRevenueReportToExcel(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        try
        {
            if (startDate > endDate)
            {
                return BadRequest(new { message = "Ngy bt u khng c ln hn ngy kt thc" });
            }

            var excelBytes = await _excelExportService.ExportRevenueReportToExcelAsync(startDate, endDate);

            var fileName = $"BaoCaoDoanhThu_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
    /// <returns>File Excel bo co tn kho</returns>
    [HttpGet("inventory/excel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportInventoryReportToExcel()
    {
        try
        {
            var excelBytes = await _excelExportService.ExportInventoryReportToExcelAsync();

            var fileName = $"BaoCaoTonKho_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
    /// <param name="page">Trang (mc nh: 1)</param>
    /// <param name="pageSize">S lng mi trang (mc nh: 1000)</param>
    /// <param name="search">Tm kim theo tn hoc email</param>
    /// <param name="isActive">Lc theo trng thi hot ng</param>
    /// <param name="vipLevel">Lc theo cp  VIP</param>
    /// <returns>File Excel danh sch khch hng</returns>
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
            var query = _context.Users
                .Include(u => u.Orders)
                .AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => (u.FirstName + " " + u.LastName).Contains(search) || u.Email.Contains(search));
            }

            if (isActive.HasValue)
            {
                query = query.Where(u => u.IsActive == isActive.Value);
            }

            if (vipLevel.HasValue)
            {
                query = query.Where(u => u.VipTierId == vipLevel.Value);
            }

            // Get users
            var users = await query
                .OrderBy(u => u.FirstName + " " + u.LastName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var excelBytes = await _excelExportService.ExportUsersToExcelAsync(users);

            var fileName = $"DanhSachKhachHang_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting users to Excel");
            return StatusCode(500, new { message = "Li khi xut danh sch khch hng" });
        }
    }

    #region Customer Export Endpoints

    /// <summary>
    /// Xut ha n PDF cho khch hng (khng cn quyn admin) - Ch cho php xut ha n n hng  xc nhn tr ln
    /// </summary>
    /// <param name="orderId">ID n hng</param>
    /// <param name="includeDigitalSignature">C bao gm ch k s hay khng (mc nh: false)</param>
    /// <returns>File PDF ha n</returns>
    [HttpGet("customer/invoice/pdf/{orderId}")]
    [Authorize] // Ch cn ng nhp, khng cn quyn admin
    public async Task<IActionResult> ExportCustomerInvoicePdf(int orderId, bool includeDigitalSignature = false)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Vui lng ng nhp  xut ha n" });
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId.Value);

            if (order == null)
            {
                return NotFound(new { message = "Khng tm thy n hng hoc bn khng c quyn truy cp" });
            }

            // Ch cho php xut ha n cho n hng  xc nhn tr ln (tr  hy)
            if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Cancelled)
            {
                return BadRequest(new
                {
                    message = "Ch c th xut ha n cho n hng  xc nhn tr ln",
                    currentStatus = order.Status.ToString(),
                    allowedStatuses = new[] { "Confirmed", "Processing", "Shipped", "Delivered", "Returned", "Refunded" }
                });
            }

            var pdfBytes = await _pdfExportService.ExportInvoicePdfAsync(order, includeDigitalSignature);

            var fileName = $"HoaDon_{order.OrderNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting customer invoice PDF for order {OrderId}", orderId);
            return StatusCode(500, new { message = "Li khi xut ha n PDF" });
        }
    }

    /// <summary>
    /// Xut ha n XML cho khch hng (khng cn quyn admin) - Ch cho php xut ha n n hng  xc nhn tr ln
    /// </summary>
    /// <param name="orderId">ID n hng</param>
    /// <returns>File XML ha n</returns>
    [HttpGet("customer/invoice/xml/{orderId}")]
    [Authorize] // Ch cn ng nhp, khng cn quyn admin
    public async Task<IActionResult> ExportCustomerInvoiceXml(int orderId)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                return Unauthorized(new { message = "Vui lng ng nhp  xut ha n" });
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId.Value);

            if (order == null)
            {
                return NotFound(new { message = "Khng tm thy n hng hoc bn khng c quyn truy cp" });
            }

            // Ch cho php xut ha n cho n hng  xc nhn tr ln (tr  hy)
            if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Cancelled)
            {
                return BadRequest(new
                {
                    message = "Ch c th xut ha n cho n hng  xc nhn tr ln",
                    currentStatus = order.Status.ToString(),
                    allowedStatuses = new[] { "Confirmed", "Processing", "Shipped", "Delivered", "Returned", "Refunded" }
                });
            }

            var xmlBytes = await _pdfExportService.ExportInvoiceXmlAsync(order);

            var fileName = $"HoaDon_{order.OrderNumber}_{DateTime.Now:yyyyMMddHHmmss}.xml";

            return File(xmlBytes, "application/xml", fileName);
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
    /// <param name="page">Trang (mc nh: 1)</param>
    /// <param name="pageSize">S lng mi trang (mc nh: 1000)</param>
    /// <param name="search">Tm kim theo m t hoc IP</param>
    /// <param name="eventType">Lc theo loi s kin</param>
    /// <param name="severity">Lc theo mc  nghim trng</param>
    /// <param name="startDate">Ngy bt u (format: yyyy-MM-dd)</param>
    /// <param name="endDate">Ngy kt thc (format: yyyy-MM-dd)</param>
    /// <returns>File Excel danh sch s kin bo mt</returns>
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
            var query = _context.SecurityEvents.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(e => e.Description.Contains(search) || e.IPAddress.Contains(search));
            }

            if (!string.IsNullOrEmpty(eventType))
            {
                query = query.Where(e => e.EventType == eventType);
            }

            if (!string.IsNullOrEmpty(severity))
            {
                query = query.Where(e => e.Severity == severity);
            }

            if (startDate.HasValue)
            {
                query = query.Where(e => e.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(e => e.CreatedAt <= endDate.Value.AddDays(1));
            }

            // Get events
            var events = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var excelBytes = await _excelExportService.ExportSecurityEventsToExcelAsync(events);

            var fileName = $"DanhSachSuKienBaoMat_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
    /// <param name="page">Trang (mc nh: 1)</param>
    /// <param name="pageSize">S lng mi trang (mc nh: 1000)</param>
    /// <param name="search">Tm kim theo IP hoc l do</param>
    /// <param name="type">Lc theo loi (blacklist/whitelist)</param>
    /// <param name="isActive">Lc theo trng thi hot ng</param>
    /// <returns>File Excel danh sch IP Block Rules</returns>
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
            var query = _context.IPBlockRules.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.IPAddress.Contains(search) || r.Reason.Contains(search));
            }

            if (!string.IsNullOrEmpty(type))
            {
                query = query.Where(r => r.Type == type);
            }

            if (isActive.HasValue)
            {
                query = query.Where(r => r.IsActive == isActive.Value);
            }

            // Get rules
            var rules = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var excelBytes = await _excelExportService.ExportIPBlockRulesToExcelAsync(rules);

            var fileName = $"DanhSachIPBlockRules_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
    /// <param name="page">Trang (mc nh: 1)</param>
    /// <param name="pageSize">S lng mi trang (mc nh: 1000)</param>
    /// <param name="search">Tm kim theo tn hoc endpoint</param>
    /// <param name="isActive">Lc theo trng thi hot ng</param>
    /// <returns>File Excel danh sch Rate Limit Rules</returns>
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
            var query = _context.RateLimitRules.AsQueryable();

            // Apply filters
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r => r.Name.Contains(search) || r.Endpoint.Contains(search));
            }

            if (isActive.HasValue)
            {
                query = query.Where(r => r.IsActive == isActive.Value);
            }

            // Get rules
            var rules = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var excelBytes = await _excelExportService.ExportRateLimitRulesToExcelAsync(rules);

            var fileName = $"DanhSachRateLimitRules_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
    /// <param name="startDate">Ngy bt u (format: yyyy-MM-dd)</param>
    /// <param name="endDate">Ngy kt thc (format: yyyy-MM-dd)</param>
    /// <returns>File Excel bo co bo mt tng hp</returns>
    [HttpGet("security-report/excel")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> ExportSecurityReportToExcel(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var excelBytes = await _excelExportService.ExportSecurityReportToExcelAsync(startDate, endDate);

            var fileName = $"BaoCaoBaoMat_{startDate?.ToString("yyyyMMdd") ?? "all"}_{endDate?.ToString("yyyyMMdd") ?? "all"}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting security report to Excel");
            return StatusCode(500, new { message = "Li khi xut bo co bo mt tng hp" });
        }
    }

    #endregion
}
