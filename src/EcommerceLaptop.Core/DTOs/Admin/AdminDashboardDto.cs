using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs.Order;

namespace EcommerceLaptop.Core.DTOs.Admin;

/// <summary>
/// Dashboard KPIs response DTO
/// </summary>
public class DashboardKpisDto
{
    public KpiMetricDto TodaySales { get; set; } = new();
    public KpiMetricDto NewOrders { get; set; } = new();
    public KpiValueDto LowStock { get; set; } = new();
    public KpiMetricDto Visitors { get; set; } = new();
    public KpiMetricDto Revenue { get; set; } = new();
    public KpiValueDto TotalCustomers { get; set; } = new();
}

/// <summary>
/// KPI metric with change tracking
/// </summary>
public class KpiMetricDto
{
    public decimal Value { get; set; }
    public decimal Change { get; set; }
    public bool IsPositive { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Period { get; set; } = string.Empty;
}

/// <summary>
/// Simple KPI value
/// </summary>
public class KpiValueDto
{
    public int Value { get; set; }
    public string Label { get; set; } = string.Empty;
}

/// <summary>
/// Chart data point for sales trends
/// </summary>
public class ChartDataPointDto
{
    public string Name { get; set; } = string.Empty; // e.g., "T1", "Monday", "2025-01-15"
    public decimal Sales { get; set; }
    public int Orders { get; set; }
    public DateTime Date { get; set; }
}

/// <summary>
/// Recent order summary for dashboard
/// </summary>
public class DashboardOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string StatusColor { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ItemCount { get; set; }
    public string FirstProductName { get; set; } = string.Empty;
}

/// <summary>
/// Admin order DTO with complete customer and payment information
/// </summary>
public class AdminOrderDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? EstimatedDeliveryDate { get; set; }

    // Customer Information
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;

    // Shipping Information
    public string ShippingAddress { get; set; } = string.Empty;

    // Payment Information
    public string PaymentStatus { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;

    // Order Items
    public List<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
}

/// <summary>
/// Low stock product summary
/// </summary>
public class LowStockProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MinStock { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string StockStatus { get; set; } = string.Empty; // "Low", "Critical", "Out"
}

/// <summary>
/// Product performance summary
/// </summary>
public class ProductPerformanceDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int TotalSold { get; set; }
    public decimal Revenue { get; set; }
    public int Views { get; set; }
    public decimal ConversionRate { get; set; }
    public string Category { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
}

/// <summary>
/// Paginated response wrapper
/// </summary>
public class PagedResponseDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }

    public PagedResponseDto(List<T> items, int totalItems, int page, int pageSize)
    {
        Items = items;
        TotalItems = totalItems;
        CurrentPage = page;
        PageSize = pageSize;
        TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
        HasNext = page < TotalPages;
        HasPrevious = page > 1;
    }
}

/// <summary>
/// Security event for dashboard monitoring
/// </summary>
public class SecurityEventDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty; // "low", "medium", "high", "critical"
    public string IpAddress { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string UserAgent { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // "login_failed", "ip_blocked", etc.
}

/// <summary>
/// Category distribution for charts
/// </summary>
public class CategoryDistributionDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public string Color { get; set; } = string.Empty;
}