using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcommerceLaptop.API.Authorization;
using EcommerceLaptop.Core.Constants;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs.Admin;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Admin dashboard controller providing KPIs, statistics, and chart data
/// </summary>
[ApiController]
[Route("api/admin/dashboard")]
[Route("admin/dashboard")]
[RequireAdmin]
public class AdminDashboardController : BaseApiController
{
    private readonly ILogger<AdminDashboardController> _logger;
    private readonly IAdminDashboardService _dashboardService;

    public AdminDashboardController(
        IAdminDashboardService dashboardService,
        ILogger<AdminDashboardController> logger) : base(logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Get combined dashboard overview (KPIs, sales trends, recent orders)
    /// </summary>
    /// <param name="dateFrom">Start date for KPI calculation (ISO format)</param>
    /// <param name="dateTo">End date for KPI calculation (ISO format)</param>
    /// <returns>Combined overview payload</returns>
    [HttpGet("overview")]
    [RequireAdminPermission(AdminPermissions.DashboardRead)]
    public async Task<IActionResult> GetDashboardOverview(
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null)
    {
        try
        {
            var fromDate = string.IsNullOrEmpty(dateFrom) ? DateTime.Today.AddDays(-30) : DateTime.Parse(dateFrom);
            var toDate = string.IsNullOrEmpty(dateTo) ? DateTime.Today.AddDays(1) : DateTime.Parse(dateTo);

            var kpisTask = _dashboardService.GetDashboardKpisAsync(fromDate, toDate);
            var salesTrendTask = _dashboardService.GetSalesTrendAsync(30);
            var recentOrdersTask = _dashboardService.GetRecentOrdersAsync(1, 10);

            await Task.WhenAll(kpisTask, salesTrendTask, recentOrdersTask);

            var overview = new
            {
                kpis = kpisTask.Result,
                salesTrends = salesTrendTask.Result,
                recentOrders = recentOrdersTask.Result.Items
            };

            return Ok(new
            {
                success = true,
                data = overview,
                message = "Dashboard overview retrieved successfully"
            });
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Invalid date format. Use ISO format (YYYY-MM-DD)" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard overview");
            return StatusCode(500, new { message = "Error retrieving dashboard overview" });
        }
    }

    /// <summary>
    /// Get dashboard KPIs (Key Performance Indicators)
    /// </summary>
    /// <param name="dateFrom">Start date for data filtering (ISO format)</param>
    /// <param name="dateTo">End date for data filtering (ISO format)</param>
    /// <returns>Dashboard KPI metrics</returns>
    [HttpGet("kpis")]
    [RequireAdminPermission(AdminPermissions.DashboardRead)]
    public async Task<IActionResult> GetDashboardKpis(
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null)
    {
        try
        {
            var fromDate = string.IsNullOrEmpty(dateFrom) ? DateTime.Today : DateTime.Parse(dateFrom);
            var toDate = string.IsNullOrEmpty(dateTo) ? DateTime.Today.AddDays(1) : DateTime.Parse(dateTo);

            var kpis = await _dashboardService.GetDashboardKpisAsync(fromDate, toDate);

            return Ok(new
            {
                success = true,
                data = kpis,
                message = "Dashboard KPIs retrieved successfully"
            });
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Invalid date format. Use ISO format (YYYY-MM-DD)" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard KPIs");
            return StatusCode(500, new { message = "Error retrieving dashboard data" });
        }
    }

    /// <summary>
    /// Get sales trend data for charts
    /// </summary>
    /// <param name="days">Number of days to retrieve data for (default: 7)</param>
    /// <returns>Sales trend chart data</returns>
    [HttpGet("sales-trend")]
    [RequireAdminPermission(AdminPermissions.DashboardRead)]
    public async Task<IActionResult> GetSalesTrend([FromQuery] int days = 7)
    {
        try
        {
            if (days <= 0 || days > 365)
            {
                return BadRequest(new { message = "Days must be between 1 and 365" });
            }

            var salesTrend = await _dashboardService.GetSalesTrendAsync(days);

            return Ok(new
            {
                success = true,
                data = salesTrend,
                message = "Sales trend data retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sales trend data for {Days} days", days);
            return StatusCode(500, new { message = "Error retrieving sales trend data" });
        }
    }

    /// <summary>
    /// Get recent orders for dashboard overview
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 10)</param>
    /// <param name="status">Filter by order status</param>
    /// <returns>Recent orders list</returns>
    [HttpGet("recent-orders")]
    [RequireAdminPermission(AdminPermissions.OrdersRead)]
    public async Task<IActionResult> GetRecentOrders(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? status = null)
    {
        try
        {
            if (page <= 0) page = 1;
            if (limit <= 0 || limit > 100) limit = 10;

            var recentOrders = await _dashboardService.GetRecentOrdersAsync(page, limit, status);

            return Ok(new
            {
                success = true,
                data = recentOrders.Items,
                pagination = new
                {
                    page,
                    limit,
                    totalItems = recentOrders.TotalItems,
                    totalPages = recentOrders.TotalPages,
                    hasNext = recentOrders.HasNext,
                    hasPrevious = recentOrders.HasPrevious
                },
                message = "Recent orders retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving recent orders for page {Page}, limit {Limit}", page, limit);
            return StatusCode(500, new { message = "Error retrieving recent orders" });
        }
    }

    /// <summary>
    /// Get low stock products for dashboard alerts
    /// </summary>
    /// <param name="threshold">Stock threshold (default: 10)</param>
    /// <returns>Low stock products list</returns>
    [HttpGet("low-stock")]
    [RequireAdminPermission(AdminPermissions.ProductsRead)]
    public async Task<IActionResult> GetLowStockProducts([FromQuery] int threshold = 10)
    {
        try
        {
            if (threshold < 0) threshold = 10;

            var lowStockProducts = await _dashboardService.GetLowStockProductsAsync(threshold);

            return Ok(new
            {
                success = true,
                data = lowStockProducts,
                message = "Low stock products retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving low stock products with threshold {Threshold}", threshold);
            return StatusCode(500, new { message = "Error retrieving low stock products" });
        }
    }

    /// <summary>
    /// Export dashboard report
    /// </summary>
    /// <param name="format">Export format (csv, pdf)</param>
    /// <param name="dateFrom">Start date for report</param>
    /// <param name="dateTo">End date for report</param>
    /// <returns>Report file</returns>
    [HttpPost("export-report")]
    [RequireAdminPermission(AdminPermissions.DashboardRead)]
    public async Task<IActionResult> ExportDashboardReport(
        [FromQuery] string format = "csv",
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null)
    {
        try
        {
            if (format != "csv" && format != "pdf")
            {
                return BadRequest(new { message = "Format must be 'csv' or 'pdf'" });
            }

            var fromDate = string.IsNullOrEmpty(dateFrom) ? DateTime.Today.AddDays(-30) : DateTime.Parse(dateFrom);
            var toDate = string.IsNullOrEmpty(dateTo) ? DateTime.Today : DateTime.Parse(dateTo);

            var reportData = await _dashboardService.ExportDashboardReportAsync(fromDate, toDate);

            var contentType = format == "pdf" ? "application/pdf" : "text/csv";
            var fileName = $"dashboard-report-{DateTime.Now:yyyy-MM-dd}.{format}";

            return File(reportData, contentType, fileName);
        }
        catch (FormatException)
        {
            return BadRequest(new { message = "Invalid date format. Use ISO format (YYYY-MM-DD)" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting dashboard report in {Format} format", format);
            return StatusCode(500, new { message = "Error exporting dashboard report" });
        }
    }

    /// <summary>
    /// Get product performance statistics
    /// </summary>
    /// <param name="days">Number of days for analysis (default: 30)</param>
    /// <returns>Product performance data</returns>
    [HttpGet("product-performance")]
    [RequireAdminPermission(AdminPermissions.ProductsRead)]
    public async Task<IActionResult> GetProductPerformance([FromQuery] int days = 30)
    {
        try
        {
            if (days <= 0 || days > 365) days = 30;

            var productPerformance = await _dashboardService.GetProductPerformanceAsync(days);

            return Ok(new
            {
                success = true,
                data = productPerformance,
                message = "Product performance data retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product performance for {Days} days", days);
            return StatusCode(500, new { message = "Error retrieving product performance data" });
        }
    }

    /// <summary>
    /// Test endpoint to verify dashboard service works - NO AUTH REQUIRED FOR TESTING
    /// </summary>
    /// <returns>Basic dashboard data for testing</returns>
    [HttpGet("test")]
    [AllowAnonymous]
    public async Task<IActionResult> TestDashboard()
    {
        try
        {
            var fromDate = DateTime.UtcNow.AddDays(-30);
            var toDate = DateTime.UtcNow;

            var kpis = await _dashboardService.GetDashboardKpisAsync(fromDate, toDate);

            return Ok(new
            {
                success = true,
                data = kpis,
                message = "Dashboard test successful"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in dashboard test");
            return StatusCode(500, new { message = "Dashboard test failed", error = ex.Message });
        }
    }

    /// <summary>
    /// Get security events for dashboard monitoring
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="limit">Items per page (default: 5)</param>
    /// <param name="severity">Filter by severity level</param>
    /// <returns>Security events list</returns>
    [HttpGet("security-events")]
    [RequireAdminPermission(AdminPermissions.SecurityRead)]
    public async Task<IActionResult> GetSecurityEvents(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 5,
        [FromQuery] string? severity = null)
    {
        try
        {
            if (page <= 0) page = 1;
            if (limit <= 0 || limit > 100) limit = 5;

            var securityEvents = await _dashboardService.GetSecurityEventsAsync(page, limit, severity);

            return Ok(new
            {
                success = true,
                data = securityEvents.Items,
                pagination = new
                {
                    page,
                    limit,
                    totalItems = securityEvents.TotalItems,
                    totalPages = securityEvents.TotalPages,
                    hasNext = securityEvents.HasNext,
                    hasPrevious = securityEvents.HasPrevious
                },
                message = "Security events retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving security events for page {Page}, limit {Limit}", page, limit);
            return StatusCode(500, new { message = "Error retrieving security events" });
        }
    }
}