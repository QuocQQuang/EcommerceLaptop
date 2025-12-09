using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

public interface IExcelExportService
{
    /// <summary>
    /// Xut danh sch n hng ra Excel
    /// </summary>
    /// <param name="orders">Danh sch n hng</param>
    /// <param name="fileName">Tn file</param>
    /// <returns>Byte array ca file Excel</returns>
    Task<byte[]> ExportOrdersToExcelAsync(IEnumerable<Order> orders, string fileName = "orders");

    /// <summary>
    /// Xut danh sch sn phm ra Excel
    /// </summary>
    /// <param name="products">Danh sch sn phm</param>
    /// <param name="fileName">Tn file</param>
    /// <returns>Byte array ca file Excel</returns>
    Task<byte[]> ExportProductsToExcelAsync(IEnumerable<Product> products, string fileName = "products");

    /// <summary>
    /// Xut bo co doanh thu ra Excel
    /// </summary>
    /// <param name="startDate">Ngy bt u</param>
    /// <param name="endDate">Ngy kt thc</param>
    /// <param name="fileName">Tn file</param>
    /// <returns>Byte array ca file Excel</returns>
    Task<byte[]> ExportRevenueReportToExcelAsync(DateTime startDate, DateTime endDate, string fileName = "revenue_report");

    /// <summary>
    /// Xut bo co tn kho ra Excel
    /// </summary>
    /// <param name="fileName">Tn file</param>
    /// <returns>Byte array ca file Excel</returns>
    Task<byte[]> ExportInventoryReportToExcelAsync(string fileName = "inventory_report");

    /// <summary>
    /// Xut danh sch khch hng ra Excel
    /// </summary>
    /// <param name="users">Danh sch khch hng</param>
    /// <param name="fileName">Tn file</param>
    /// <returns>Byte array ca file Excel</returns>
    Task<byte[]> ExportUsersToExcelAsync(IEnumerable<User> users, string fileName = "users");

    /// <summary>
    /// Xut danh sch s kin bo mt ra Excel
    /// </summary>
    /// <param name="securityEvents">Danh sch s kin bo mt</param>
    /// <param name="fileName">Tn file</param>
    /// <returns>Byte array ca file Excel</returns>
    Task<byte[]> ExportSecurityEventsToExcelAsync(IEnumerable<SecurityEvent> securityEvents, string fileName = "security_events");

    /// <summary>
    /// Xut danh sch IP Block Rules ra Excel
    /// </summary>
    /// <param name="ipBlockRules">Danh sch IP Block Rules</param>
    /// <param name="fileName">Tn file</param>
    /// <returns>Byte array ca file Excel</returns>
    Task<byte[]> ExportIPBlockRulesToExcelAsync(IEnumerable<IPBlockRule> ipBlockRules, string fileName = "ip_block_rules");

    /// <summary>
    /// Xut danh sch Rate Limit Rules ra Excel
    /// </summary>
    /// <param name="rateLimitRules">Danh sch Rate Limit Rules</param>
    /// <param name="fileName">Tn file</param>
    /// <returns>Byte array ca file Excel</returns>
    Task<byte[]> ExportRateLimitRulesToExcelAsync(IEnumerable<RateLimitRule> rateLimitRules, string fileName = "rate_limit_rules");

    /// <summary>
    /// Xut bo co bo mt tng hp ra Excel
    /// </summary>
    /// <param name="startDate">Ngy bt u (nullable)</param>
    /// <param name="endDate">Ngy kt thc (nullable)</param>
    /// <param name="fileName">Tn file</param>
    /// <returns>Byte array ca file Excel</returns>
    Task<byte[]> ExportSecurityReportToExcelAsync(DateTime? startDate = null, DateTime? endDate = null, string fileName = "security_report");
}
