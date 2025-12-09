using EcommerceLaptop.Core.DTOs.Export;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Services;

public interface IReportingService
{
    // Admin Exports
    Task<ExportResultDto> ExportInvoicePdfAsync(int orderId, bool includeDigitalSignature = true);
    Task<ExportResultDto> ExportInvoiceXmlAsync(int orderId);
    Task<ExportResultDto> ExportOrdersToExcelAsync(int page = 1, int pageSize = 1000, string? search = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<ExportResultDto> ExportProductsToExcelAsync(int page = 1, int pageSize = 1000, string? search = null, int? categoryId = null, int? brandId = null, bool? isActive = null);
    Task<ExportResultDto> ExportRevenueReportToExcelAsync(DateTime startDate, DateTime endDate);
    Task<ExportResultDto> ExportInventoryReportToExcelAsync();
    Task<ExportResultDto> ExportUsersToExcelAsync(int page = 1, int pageSize = 1000, string? search = null, bool? isActive = null, int? vipLevel = null);
    Task<ExportResultDto> ExportSecurityEventsToExcelAsync(int page = 1, int pageSize = 1000, string? search = null, string? eventType = null, string? severity = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<ExportResultDto> ExportIPBlockRulesToExcelAsync(int page = 1, int pageSize = 1000, string? search = null, string? type = null, bool? isActive = null);
    Task<ExportResultDto> ExportRateLimitRulesToExcelAsync(int page = 1, int pageSize = 1000, string? search = null, bool? isActive = null);
    Task<ExportResultDto> ExportSecurityReportToExcelAsync(DateTime? startDate = null, DateTime? endDate = null);

    // Customer Exports
    Task<ExportResultDto> ExportCustomerInvoicePdfAsync(int orderId, int userId, bool includeDigitalSignature = false);
    Task<ExportResultDto> ExportCustomerInvoiceXmlAsync(int orderId, int userId);
}
