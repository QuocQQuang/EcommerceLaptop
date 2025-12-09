using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.Entities;
using System.Text;
using System.Text.Json;

namespace EcommerceLaptop.Infrastructure.Services;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AdminDashboardService> _logger;

    public AdminDashboardService(ApplicationDbContext context, ILogger<AdminDashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardKpisDto> GetDashboardKpisAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var fromDate = startDate ?? DateTime.Today.AddDays(-30);
            var toDate = endDate ?? DateTime.Today.AddDays(1);
            var previousPeriodStart = fromDate.AddDays(-(toDate - fromDate).Days);

            // Sales calculations
            var currentPeriodSales = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt < toDate &&
                           (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Shipped))
                .SumAsync(o => o.TotalAmount);

            var previousPeriodSales = await _context.Orders
                .Where(o => o.CreatedAt >= previousPeriodStart && o.CreatedAt < fromDate &&
                           (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Shipped))
                .SumAsync(o => o.TotalAmount);

            var salesChange = previousPeriodSales > 0
                ? ((currentPeriodSales - previousPeriodSales) / previousPeriodSales) * 100
                : 0;

            // Orders calculations
            var currentPeriodOrders = await _context.Orders
                .CountAsync(o => o.CreatedAt >= fromDate && o.CreatedAt < toDate);

            var previousPeriodOrders = await _context.Orders
                .CountAsync(o => o.CreatedAt >= previousPeriodStart && o.CreatedAt < fromDate);

            var ordersChange = previousPeriodOrders > 0
                ? ((decimal)(currentPeriodOrders - previousPeriodOrders) / previousPeriodOrders) * 100
                : 0;

            // Low stock count (using Inventory table)
            var lowStockCount = await _context.Inventories
                .CountAsync(i => i.QuantityInStock <= i.ReorderLevel);

            // Total customers
            var totalCustomers = await _context.Users.CountAsync(u => u.IsActive);

            // Current period revenue (same as sales for this KPI)
            var revenueChange = salesChange;

            return new DashboardKpisDto
            {
                TodaySales = new KpiMetricDto
                {
                    Value = currentPeriodSales,
                    Change = salesChange,
                    IsPositive = salesChange >= 0,
                    Unit = "VND",
                    Period = "vs last period"
                },
                NewOrders = new KpiMetricDto
                {
                    Value = currentPeriodOrders,
                    Change = ordersChange,
                    IsPositive = ordersChange >= 0,
                    Unit = "orders",
                    Period = "vs last period"
                },
                LowStock = new KpiValueDto
                {
                    Value = lowStockCount,
                    Label = "Low Stock Alerts"
                },
                Visitors = new KpiMetricDto
                {
                    Value = await _context.UserActivityLogs.CountAsync(ual => ual.CreatedAt >= fromDate && ual.CreatedAt < toDate),
                    Change = 15.5m,
                    IsPositive = true,
                    Unit = "visitors",
                    Period = "vs last period"
                },
                Revenue = new KpiMetricDto
                {
                    Value = currentPeriodSales,
                    Change = revenueChange,
                    IsPositive = revenueChange >= 0,
                    Unit = "VND",
                    Period = "vs last period"
                },
                TotalCustomers = new KpiValueDto
                {
                    Value = totalCustomers,
                    Label = "Total Customers"
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard KPIs");
            throw;
        }
    }

    public async Task<IEnumerable<ChartDataPointDto>> GetSalesTrendAsync(int days = 30)
    {
        try
        {
            var fromDate = DateTime.Today.AddDays(-days);

            var salesData = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate &&
                           (o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Shipped))
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new ChartDataPointDto
                {
                    Name = g.Key.ToString("MMM dd"),
                    Sales = g.Sum(o => o.TotalAmount),
                    Orders = g.Count(),
                    Date = g.Key
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            return salesData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sales trend");
            throw;
        }
    }

    public async Task<PagedResponseDto<DashboardOrderDto>> GetRecentOrdersAsync(int page = 1, int pageSize = 10, string? status = null)
    {
        try
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
                {
                    query = query.Where(o => o.Status == parsedStatus);
                }
            }

            var totalItems = await query.CountAsync();

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new DashboardOrderDto
                {
                    Id = o.Id,
                    OrderNumber = o.OrderNumber,
                    CustomerName = o.User != null ? $"{o.User.FirstName} {o.User.LastName}".Trim() : "Guest",
                    CustomerEmail = o.User.Email,
                    Total = o.TotalAmount,
                    Status = o.Status.ToString(),
                    StatusColor = GetOrderStatusColor(o.Status),
                    CreatedAt = o.CreatedAt,
                    ItemCount = o.OrderItems.Count
                })
                .ToListAsync();

            return new PagedResponseDto<DashboardOrderDto>(orders, totalItems, page, pageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent orders");
            throw;
        }
    }

    public async Task<IEnumerable<LowStockProductDto>> GetLowStockProductsAsync(int threshold = 10)
    {
        try
        {
            var lowStockProducts = await _context.Products
                .Include(p => p.Inventory)
                .Include(p => p.Images)
                .Include(p => p.Category)
                .Where(p => p.Inventory != null && p.Inventory.QuantityInStock <= threshold)
                .Select(p => new LowStockProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    SKU = p.SKU,
                    CurrentStock = p.Inventory!.QuantityInStock,
                    MinStock = p.Inventory.ReorderLevel,
                    Category = p.Category != null ? p.Category.Name : p.Brand,
                    Brand = p.Brand,
                    Price = p.Price,
                    ImageUrl = p.Images.FirstOrDefault(pi => pi.IsPrimary)!.ImageUrl ?? "",
                    StockStatus = p.Inventory.QuantityInStock == 0 ? "Out" :
                                 p.Inventory.QuantityInStock <= p.Inventory.ReorderLevel / 2 ? "Critical" : "Low"
                })
                .OrderBy(p => p.CurrentStock)
                .ToListAsync();

            return lowStockProducts;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting low stock products");
            throw;
        }
    }

    public async Task<IEnumerable<ProductPerformanceDto>> GetProductPerformanceAsync(int days = 30, int top = 10)
    {
        try
        {
            var fromDate = DateTime.Today.AddDays(-days);

            var orderItemsQuery = _context.OrderItems
                .Include(oi => oi.Product)
                .ThenInclude(p => p.Images)
                .Include(oi => oi.Product.Reviews)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.CreatedAt >= fromDate &&
                            (oi.Order.Status == OrderStatus.Confirmed || oi.Order.Status == OrderStatus.Shipped));

            var groupedData = await orderItemsQuery
                .GroupBy(oi => oi.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product.Name,
                    SKU = g.First().Product.SKU,
                    TotalSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalPrice),
                    Category = g.First().Product.Category != null ? g.First().Product.Category.Name : g.First().Product.Brand,
                    ImageUrl = g.First().Product.Images
                        .Where(pi => pi.IsPrimary)
                        .Select(pi => pi.ImageUrl)
                        .FirstOrDefault() ?? "",
                    Rating = g.First().Product.Reviews.Any() ? (decimal)g.First().Product.Reviews.Average(r => r.Rating) : 0m,
                    ReviewCount = g.First().Product.Reviews.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .Take(top)
                .ToListAsync();

            var productIds = groupedData.Select(g => g.ProductId).ToList();
            var productIdStrings = productIds.Select(id => id.ToString()).ToList();
            var viewsRaw = await _context.UserActivityLogs
                .Where(ual => ual.EntityType == "Product" && ual.CreatedAt >= fromDate && ual.EntityId != null && productIdStrings.Contains(ual.EntityId))
                .GroupBy(ual => ual.EntityId!)
                .Select(g => new { ProductIdString = g.Key, Views = g.Count() })
                .ToListAsync();
            var viewsData = viewsRaw
                .Select(x => new { Key = int.TryParse(x.ProductIdString, out var id) ? id : 0, x.Views })
                .Where(x => x.Key != 0)
                .ToDictionary(x => x.Key, x => x.Views);

            var productPerformance = groupedData.Select(g => new ProductPerformanceDto
            {
                ProductId = g.ProductId,
                ProductName = g.ProductName,
                SKU = g.SKU,
                TotalSold = g.TotalSold,
                Revenue = g.Revenue,
                Views = viewsData.GetValueOrDefault(g.ProductId, 0),
                ConversionRate = g.TotalSold > 0 ? (decimal)g.TotalSold / (viewsData.GetValueOrDefault(g.ProductId, 1) + 1) * 100 : 0m,
                Category = g.Category,
                ImageUrl = g.ImageUrl ?? "",
                Rating = (decimal)(g.Rating > 0 ? g.Rating : 0m),
                ReviewCount = g.ReviewCount
            }).ToList();

            return productPerformance;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product performance");
            throw;
        }
    }

    public async Task<IEnumerable<ChartDataPointDto>> GetTopCategoriesAsync(int days = 30, int top = 10)
    {
        try
        {
            var fromDate = DateTime.Today.AddDays(-days);

            var topCategories = await _context.OrderItems
                .Include(oi => oi.Product)
                .Include(oi => oi.Product.Category)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.CreatedAt >= fromDate &&
                            (oi.Order.Status == OrderStatus.Delivered || oi.Order.Status == OrderStatus.Shipped))
                .GroupBy(oi => oi.Product.Category != null ? oi.Product.Category.Name : oi.Product.Brand)
                .Select(g => new ChartDataPointDto
                {
                    Name = g.Key,
                    Sales = g.Sum(oi => oi.TotalPrice),
                    Orders = g.Count(),
                    Date = DateTime.Today
                })
                .OrderByDescending(c => c.Sales)
                .Take(top)
                .ToListAsync();

            return topCategories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting top categories");
            throw;
        }
    }

    public async Task<byte[]> ExportDashboardReportAsync(string format, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var fromDate = startDate ?? DateTime.Today.AddDays(-30);
            var toDate = endDate ?? DateTime.Today;

            var kpis = await GetDashboardKpisAsync(fromDate, toDate);
            var salesTrend = await GetSalesTrendAsync(30);

            if (format.ToLower() == "csv")
            {
                var csv = new StringBuilder();
                csv.AppendLine("Dashboard Report");
                csv.AppendLine($"Period: {fromDate:yyyy-MM-dd} to {toDate:yyyy-MM-dd}");
                csv.AppendLine();
                csv.AppendLine("KPIs");
                csv.AppendLine($"Total Sales,{kpis.TodaySales.Value}");
                csv.AppendLine($"New Orders,{kpis.NewOrders.Value}");
                csv.AppendLine($"Revenue,{kpis.Revenue.Value}");
                csv.AppendLine($"Low Stock Items,{kpis.LowStock.Value}");
                csv.AppendLine($"Total Customers,{kpis.TotalCustomers.Value}");

                return Encoding.UTF8.GetBytes(csv.ToString());
            }
            else
            {
                // Mock PDF generation - would use a PDF library
                var json = JsonSerializer.Serialize(new { kpis, salesTrend }, new JsonSerializerOptions { WriteIndented = true });
                return Encoding.UTF8.GetBytes(json);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting dashboard report");
            throw;
        }
    }

    public async Task<byte[]> ExportDashboardReportAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        return await ExportDashboardReportAsync("csv", startDate, endDate);
    }

    public async Task<DashboardKpisDto> GetDashboardOverviewAsync()
    {
        return await GetDashboardKpisAsync();
    }

    public async Task<PagedResponseDto<SecurityEventDto>> GetSecurityEventsAsync(int page = 1, int limit = 5, string? severity = null)
    {
        try
        {
            var query = _context.SecurityEvents.AsQueryable();

            if (!string.IsNullOrWhiteSpace(severity))
            {
                query = query.Where(al => al.Severity == severity);
            }

            var totalItems = await query.CountAsync();

            var events = await query
                .OrderByDescending(al => al.CreatedAt)
                .Skip((page - 1) * limit)
                .Take(limit)
                .Select(se => new SecurityEventDto
                {
                    Id = se.Id,
                    Description = se.Description,
                    Severity = se.Severity,
                    IpAddress = se.IPAddress,
                    CreatedAt = se.CreatedAt,
                    UserAgent = se.UserAgent ?? "",
                    EventType = se.EventType
                })
                .ToListAsync();

            return new PagedResponseDto<SecurityEventDto>(events, totalItems, page, limit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting security events");
            throw;
        }
    }

    private static string GetOrderStatusColor(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.Pending => "warning",
            OrderStatus.Confirmed => "info",
            OrderStatus.Processing => "info",
            OrderStatus.Shipped => "primary",
            OrderStatus.Delivered => "success",
            OrderStatus.Cancelled => "danger",
            OrderStatus.Returned => "secondary",
            OrderStatus.Refunded => "secondary",
            _ => "secondary"
        };
    }
}