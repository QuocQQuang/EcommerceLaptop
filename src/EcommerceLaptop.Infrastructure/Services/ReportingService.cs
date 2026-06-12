using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.DTOs.Export;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Services.Security;

namespace EcommerceLaptop.Infrastructure.Services;

public class ReportingService : IReportingService
{
    private readonly ApplicationDbContext _context;
    private readonly IPdfExportService _pdfExportService;
    private readonly IExcelExportService _excelExportService;
    private readonly ISecurityEventService _securityEventService;
    private readonly ILogger<ReportingService> _logger;

    public ReportingService(
        ApplicationDbContext context,
        IPdfExportService pdfExportService,
        IExcelExportService excelExportService,
        ISecurityEventService securityEventService,
        ILogger<ReportingService> logger)
    {
        _context = context;
        _pdfExportService = pdfExportService;
        _excelExportService = excelExportService;
        _securityEventService = securityEventService;
        _logger = logger;
    }

    public async Task<ExportResultDto> ExportInvoicePdfAsync(int orderId, bool includeDigitalSignature = true)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new KeyNotFoundException("Không tìm thấy đơn hàng");

        if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Cancelled)
            throw new ValidationException("Chỉ có thể xuất hóa đơn cho đơn hàng đã xác nhận trở lên");

        var pdfBytes = await _pdfExportService.ExportInvoicePdfAsync(order, includeDigitalSignature);
        var fileName = $"HoaDon_{order.OrderNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

        return new ExportResultDto
        {
            FileContent = pdfBytes,
            FileName = fileName,
            ContentType = "application/pdf"
        };
    }

    public async Task<ExportResultDto> ExportInvoiceXmlAsync(int orderId)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            throw new KeyNotFoundException("Không tìm thấy đơn hàng");

        if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Cancelled)
            throw new ValidationException("Chỉ có thể xuất hóa đơn cho đơn hàng đã xác nhận trở lên");

        var xmlBytes = await _pdfExportService.ExportInvoiceXmlAsync(order);
        var fileName = $"HoaDon_{order.OrderNumber}_{DateTime.Now:yyyyMMddHHmmss}.xml";

        return new ExportResultDto
        {
            FileContent = xmlBytes,
            FileName = fileName,
            ContentType = "application/xml"
        };
    }

    public async Task<ExportResultDto> ExportOrdersToExcelAsync(int page = 1, int pageSize = 1000, string? search = null, string? status = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
            .AsQueryable();

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

        var orders = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var excelBytes = await _excelExportService.ExportOrdersToExcelAsync(orders);
        var fileName = $"DanhSachDonHang_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

        return new ExportResultDto
        {
            FileContent = excelBytes,
            FileName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<ExportResultDto> ExportProductsToExcelAsync(int page = 1, int pageSize = 1000, string? search = null, int? categoryId = null, int? brandId = null, bool? isActive = null)
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Include(p => p.ProductBrand)
            .AsQueryable();

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

        var products = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var excelBytes = await _excelExportService.ExportProductsToExcelAsync(products);
        var fileName = $"DanhSachSanPham_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

        return new ExportResultDto
        {
            FileContent = excelBytes,
            FileName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<ExportResultDto> ExportRevenueReportToExcelAsync(DateTime startDate, DateTime endDate)
    {
        if (startDate > endDate)
            throw new ValidationException("Ngày bắt đầu không được lớn hơn ngày kết thúc");

        var excelBytes = await _excelExportService.ExportRevenueReportToExcelAsync(startDate, endDate);
        var fileName = $"BaoCaoDoanhThu_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.xlsx";

        return new ExportResultDto
        {
            FileContent = excelBytes,
            FileName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<ExportResultDto> ExportInventoryReportToExcelAsync()
    {
        var excelBytes = await _excelExportService.ExportInventoryReportToExcelAsync();
        var fileName = $"BaoCaoTonKho_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

        return new ExportResultDto
        {
            FileContent = excelBytes,
            FileName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<ExportResultDto> ExportUsersToExcelAsync(int page = 1, int pageSize = 1000, string? search = null, bool? isActive = null, int? vipLevel = null)
    {
        var query = _context.Users
            .Include(u => u.Orders)
            .AsQueryable();

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

        var users = await query
            .OrderBy(u => u.FirstName + " " + u.LastName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var excelBytes = await _excelExportService.ExportUsersToExcelAsync(users);
        var fileName = $"DanhSachKhachHang_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

        return new ExportResultDto
        {
            FileContent = excelBytes,
            FileName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<ExportResultDto> ExportCustomerInvoicePdfAsync(int orderId, int userId, bool includeDigitalSignature = false)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
            throw new KeyNotFoundException("Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập");

        if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Cancelled)
            throw new ValidationException("Chỉ có thể xuất hóa đơn cho đơn hàng đã xác nhận trở lên");

        var pdfBytes = await _pdfExportService.ExportInvoicePdfAsync(order, includeDigitalSignature);
        var fileName = $"HoaDon_{order.OrderNumber}_{DateTime.Now:yyyyMMddHHmmss}.pdf";

        return new ExportResultDto
        {
            FileContent = pdfBytes,
            FileName = fileName,
            ContentType = "application/pdf"
        };
    }

    public async Task<ExportResultDto> ExportCustomerInvoiceXmlAsync(int orderId, int userId)
    {
        var order = await _context.Orders
            .Include(o => o.User)
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId);

        if (order == null)
            throw new KeyNotFoundException("Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập");

        if (order.Status == OrderStatus.Pending || order.Status == OrderStatus.Cancelled)
            throw new ValidationException("Chỉ có thể xuất hóa đơn cho đơn hàng đã xác nhận trở lên");

        var xmlBytes = await _pdfExportService.ExportInvoiceXmlAsync(order);
        var fileName = $"HoaDon_{order.OrderNumber}_{DateTime.Now:yyyyMMddHHmmss}.xml";

        return new ExportResultDto
        {
            FileContent = xmlBytes,
            FileName = fileName,
            ContentType = "application/xml"
        };
    }

    public async Task<ExportResultDto> ExportSecurityEventsToExcelAsync(int page = 1, int pageSize = 1000, string? search = null, string? eventType = null, string? severity = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var safePage = Math.Max(page, 1);
        var safePageSize = Math.Clamp(pageSize, 1, 1000);

        var result = await _securityEventService.GetEventsAsync(
            eventType: eventType,
            severity: severity,
            from: startDate,
            to: endDate,
            page: safePage,
            pageSize: safePageSize);

        if (!result.IsSuccess || result.Data == null)
        {
            throw new InvalidOperationException(result.ErrorMessage ?? "Failed to retrieve security events");
        }

        var events = result.Data.Items.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            events = events.Where(e =>
                (e.EventType?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (e.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (e.IPAddress?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (e.UserAgent?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || (e.Details?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        var excelBytes = await _excelExportService.ExportSecurityEventsToExcelAsync(events);
        var fileName = $"DanhSachSuKienBaoMat_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

        return new ExportResultDto
        {
            FileContent = excelBytes,
            FileName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<ExportResultDto> ExportIPBlockRulesToExcelAsync(int page = 1, int pageSize = 1000, string? search = null, string? type = null, bool? isActive = null)
    {
        var query = _context.IPBlockRules.AsQueryable();

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

        var rules = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var excelBytes = await _excelExportService.ExportIPBlockRulesToExcelAsync(rules);
        var fileName = $"DanhSachIPBlockRules_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

        return new ExportResultDto
        {
            FileContent = excelBytes,
            FileName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<ExportResultDto> ExportRateLimitRulesToExcelAsync(int page = 1, int pageSize = 1000, string? search = null, bool? isActive = null)
    {
        var query = _context.RateLimitRules.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(r => r.Name.Contains(search) || r.Endpoint.Contains(search));
        }

        if (isActive.HasValue)
        {
            query = query.Where(r => r.IsActive == isActive.Value);
        }

        var rules = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var excelBytes = await _excelExportService.ExportRateLimitRulesToExcelAsync(rules);
        var fileName = $"DanhSachRateLimitRules_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

        return new ExportResultDto
        {
            FileContent = excelBytes,
            FileName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }

    public async Task<ExportResultDto> ExportSecurityReportToExcelAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var excelBytes = await _excelExportService.ExportSecurityReportToExcelAsync(startDate, endDate);
        var fileName = $"BaoCaoBaoMat_{startDate?.ToString("yyyyMMdd") ?? "all"}_{endDate?.ToString("yyyyMMdd") ?? "all"}_{DateTime.Now:yyyyMMddHHmmss}.xlsx";

        return new ExportResultDto
        {
            FileContent = excelBytes,
            FileName = fileName,
            ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    }
}
