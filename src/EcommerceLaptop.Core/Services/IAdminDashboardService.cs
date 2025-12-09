using EcommerceLaptop.Core.DTOs.Admin;

namespace EcommerceLaptop.Core.Services;

public interface IAdminDashboardService
{
    /// <summary>
    /// Gets dashboard KPIs for a specific date range
    /// </summary>
    Task<DashboardKpisDto> GetDashboardKpisAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets sales trend data for charting
    /// </summary>
    Task<IEnumerable<ChartDataPointDto>> GetSalesTrendAsync(int days = 30);

    /// <summary>
    /// Gets recent orders with pagination
    /// </summary>
    Task<PagedResponseDto<DashboardOrderDto>> GetRecentOrdersAsync(int page = 1, int pageSize = 10, string? status = null);

    /// <summary>
    /// Gets products with low stock alerts
    /// </summary>
    Task<IEnumerable<LowStockProductDto>> GetLowStockProductsAsync(int threshold = 10);

    /// <summary>
    /// Gets product performance data
    /// </summary>
    Task<IEnumerable<ProductPerformanceDto>> GetProductPerformanceAsync(int days = 30, int top = 10);

    /// <summary>
    /// Gets top categories by sales
    /// </summary>
    Task<IEnumerable<ChartDataPointDto>> GetTopCategoriesAsync(int days = 30, int top = 10);

    /// <summary>
    /// Exports dashboard data as report
    /// </summary>
    Task<byte[]> ExportDashboardReportAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Gets basic dashboard overview
    /// </summary>
    Task<DashboardKpisDto> GetDashboardOverviewAsync();

    /// <summary>
    /// Gets security events for dashboard monitoring
    /// </summary>
    Task<PagedResponseDto<SecurityEventDto>> GetSecurityEventsAsync(int page = 1, int limit = 5, string? severity = null);

    /// <summary>
    /// Gets category distribution for charts
    /// </summary>
    Task<IEnumerable<CategoryDistributionDto>> GetCategoryDistributionAsync();
}