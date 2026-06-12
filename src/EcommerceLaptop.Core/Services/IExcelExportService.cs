using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

public interface IExcelExportService
{
    /// <summary>
    /// Xuất danh sách đơn hàng ra Excel
    /// </summary>
    /// <param name="orders">Danh sách đơn hàng</param>
    /// <param name="fileName">Tên file</param>
    /// <returns>Byte array của file Excel</returns>
    Task<byte[]> ExportOrdersToExcelAsync(IEnumerable<Order> orders, string fileName = "orders");

    /// <summary>
    /// Xuất danh sách sản phẩm ra Excel
    /// </summary>
    /// <param name="products">Danh sách sản phẩm</param>
    /// <param name="fileName">Tên file</param>
    /// <returns>Byte array của file Excel</returns>
    Task<byte[]> ExportProductsToExcelAsync(IEnumerable<Product> products, string fileName = "products");

    /// <summary>
    /// Xuất báo cáo doanh thu ra Excel
    /// </summary>
    /// <param name="startDate">Ngày bắt đầu</param>
    /// <param name="endDate">Ngày kết thúc</param>
    /// <param name="fileName">Tên file</param>
    /// <returns>Byte array của file Excel</returns>
    Task<byte[]> ExportRevenueReportToExcelAsync(DateTime startDate, DateTime endDate, string fileName = "revenue_report");

    /// <summary>
    /// Xuất báo cáo tồn kho ra Excel
    /// </summary>
    /// <param name="fileName">Tên file</param>
    /// <returns>Byte array của file Excel</returns>
    Task<byte[]> ExportInventoryReportToExcelAsync(string fileName = "inventory_report");

    /// <summary>
    /// Xuất danh sách khách hàng ra Excel
    /// </summary>
    /// <param name="users">Danh sách khách hàng</param>
    /// <param name="fileName">Tên file</param>
    /// <returns>Byte array của file Excel</returns>
    Task<byte[]> ExportUsersToExcelAsync(IEnumerable<User> users, string fileName = "users");

    /// <summary>
    /// Xuất danh sách sự kiện bảo mật ra Excel
    /// </summary>
    /// <param name="securityEvents">Danh sách sự kiện bảo mật</param>
    /// <param name="fileName">Tên file</param>
    /// <returns>Byte array của file Excel</returns>
    Task<byte[]> ExportSecurityEventsToExcelAsync(IEnumerable<SecurityEvent> securityEvents, string fileName = "security_events");

    /// <summary>
    /// Xuất danh sách IP Block Rules ra Excel
    /// </summary>
    /// <param name="ipBlockRules">Danh sách IP Block Rules</param>
    /// <param name="fileName">Tên file</param>
    /// <returns>Byte array của file Excel</returns>
    Task<byte[]> ExportIPBlockRulesToExcelAsync(IEnumerable<IPBlockRule> ipBlockRules, string fileName = "ip_block_rules");

    /// <summary>
    /// Xuất danh sách Rate Limit Rules ra Excel
    /// </summary>
    /// <param name="rateLimitRules">Danh sách Rate Limit Rules</param>
    /// <param name="fileName">Tên file</param>
    /// <returns>Byte array của file Excel</returns>
    Task<byte[]> ExportRateLimitRulesToExcelAsync(IEnumerable<RateLimitRule> rateLimitRules, string fileName = "rate_limit_rules");

    /// <summary>
    /// Xuất báo cáo bảo mật tổng hợp ra Excel
    /// </summary>
    /// <param name="startDate">Ngày bắt đầu (nullable)</param>
    /// <param name="endDate">Ngày kết thúc (nullable)</param>
    /// <param name="fileName">Tên file</param>
    /// <returns>Byte array của file Excel</returns>
    Task<byte[]> ExportSecurityReportToExcelAsync(DateTime? startDate = null, DateTime? endDate = null, string fileName = "security_report");
}
