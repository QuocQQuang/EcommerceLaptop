using EcommerceLaptop.Core.DTOs;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.DTOs.Inventory;

#region Core Inventory DTOs

/// <summary>
/// Inventory data transfer object
/// </summary>
public class InventoryDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public string ProductBrand { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity => QuantityInStock - ReservedQuantity;
    public int ReorderLevel { get; set; }
    public int MaxStockLevel { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastStockUpdate { get; set; }
    public bool IsLowStock => QuantityInStock <= ReorderLevel;
    public decimal UnitCost { get; set; }
    public decimal TotalValue => QuantityInStock * UnitCost;
}

/// <summary>
/// Inventory transaction DTO
/// </summary>
public class InventoryTransactionDto
{
    public int Id { get; set; }
    public int InventoryId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductSKU { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // StockIn, StockOut, Reserved, Released, Adjustment
    public int Quantity { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public string WarehouseLocation { get; set; } = string.Empty;
}

#endregion

#region Request DTOs

/// <summary>
/// Inventory filter request with pagination
/// </summary>
public class InventoryFilterRequest : PagedRequest
{
    public string? WarehouseLocation { get; set; }
    public bool? IsLowStock { get; set; }
    public string? ProductName { get; set; }
    public string? ProductSKU { get; set; }
    public string? Brand { get; set; }
    public int? MinQuantity { get; set; }
    public int? MaxQuantity { get; set; }
    public DateTime? LastUpdatedFrom { get; set; }
    public DateTime? LastUpdatedTo { get; set; }
    public string? SortBy { get; set; } = "ProductName";
    public string? SortDirection { get; set; } = "ASC";
}

/// <summary>
/// Create inventory request DTO
/// </summary>
public class CreateInventoryRequest
{
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative")]
    public int QuantityInStock { get; set; }
    
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Reorder level must be non-negative")]
    public int ReorderLevel { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Max stock level must be positive")]
    public int MaxStockLevel { get; set; }
    
    [Required]
    [StringLength(255, ErrorMessage = "Warehouse location cannot exceed 255 characters")]
    public string WarehouseLocation { get; set; } = string.Empty;
    
    [Range(0, double.MaxValue, ErrorMessage = "Unit cost must be non-negative")]
    public decimal UnitCost { get; set; }
}

/// <summary>
/// Update inventory request DTO
/// </summary>
public class UpdateInventoryRequest
{
    [Range(0, int.MaxValue, ErrorMessage = "Quantity must be non-negative")]
    public int? QuantityInStock { get; set; }
    
    [Range(0, int.MaxValue, ErrorMessage = "Reorder level must be non-negative")]
    public int? ReorderLevel { get; set; }
    
    [Range(1, int.MaxValue, ErrorMessage = "Max stock level must be positive")]
    public int? MaxStockLevel { get; set; }
    
    [StringLength(255, ErrorMessage = "Warehouse location cannot exceed 255 characters")]
    public string? WarehouseLocation { get; set; }
    
    [Range(0, double.MaxValue, ErrorMessage = "Unit cost must be non-negative")]
    public decimal? UnitCost { get; set; }
}

/// <summary>
/// Stock adjustment request DTO
/// </summary>
public class StockAdjustmentRequest
{
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    public int Quantity { get; set; } // Positive for stock in, negative for stock out
    
    [Required]
    [StringLength(255, ErrorMessage = "Reference cannot exceed 255 characters")]
    public string Reference { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
    
    [StringLength(255, ErrorMessage = "Warehouse location cannot exceed 255 characters")]
    public string WarehouseLocation { get; set; } = string.Empty;
    
    [Range(0, double.MaxValue, ErrorMessage = "Unit value must be non-negative")]
    public decimal UnitValue { get; set; }
}

/// <summary>
/// Stock transfer request DTO
/// </summary>
public class StockTransferRequest
{
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be positive")]
    public int Quantity { get; set; }
    
    [Required]
    [StringLength(255, ErrorMessage = "Source warehouse cannot exceed 255 characters")]
    public string FromWarehouse { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255, ErrorMessage = "Destination warehouse cannot exceed 255 characters")]
    public string ToWarehouse { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255, ErrorMessage = "Reference cannot exceed 255 characters")]
    public string Reference { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Inventory report request DTO
/// </summary>
public class InventoryReportRequest
{
    public string ReportType { get; set; } = "Current"; // Current, Historical, Forecast
    public string? WarehouseLocation { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool IncludeLowStock { get; set; } = true;
    public bool IncludeOutOfStock { get; set; } = true;
    public bool IncludeOverstock { get; set; } = false;
    public string? ProductCategory { get; set; }
    public string? Brand { get; set; }
    public string ExportFormat { get; set; } = "JSON"; // JSON, PDF, Excel, CSV
}

#endregion

#region Alert & Monitoring DTOs

/// <summary>
/// Low stock alert DTO
/// </summary>
public class LowStockAlertDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
    public int RecommendedOrderQuantity { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastStockUpdate { get; set; }
    public AlertSeverity Severity { get; set; }
    public int DaysOutOfStock { get; set; }
    public decimal EstimatedDailySales { get; set; }
}

/// <summary>
/// Reorder suggestion DTO
/// </summary>
public class ReorderSuggestionDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int ReorderLevel { get; set; }
    public int MaxStockLevel { get; set; }
    public int SuggestedOrderQuantity { get; set; }
    public decimal EstimatedCost { get; set; }
    public int AverageMonthlySales { get; set; }
    public int LeadTimeDays { get; set; }
    public DateTime SuggestedOrderDate { get; set; }
    public string SupplierName { get; set; } = string.Empty;
    public decimal UnitCost { get; set; }
}

/// <summary>
/// Stock alert DTO for real-time monitoring
/// </summary>
public class StockAlertDto
{
    public int Id { get; set; }
    public string AlertType { get; set; } = string.Empty; // LowStock, OutOfStock, Overstock, Expiry
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public string ActionRequired { get; set; } = string.Empty;
}

/// <summary>
/// Alert severity enumeration
/// </summary>
public enum AlertSeverity
{
    All = 0,
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

/// <summary>
/// Reorder priority enumeration
/// </summary>
public enum ReorderPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

#endregion

#region Reporting DTOs

/// <summary>
/// Inventory report DTO
/// </summary>
public class InventoryReportDto
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public string WarehouseLocation { get; set; } = string.Empty;
    public List<InventoryItemReportDto> Items { get; set; } = new();
    public InventoryReportSummaryDto Summary { get; set; } = new();
    public List<LowStockAlertDto> LowStockItems { get; set; } = new();
    public List<OverstockItemDto> OverstockItems { get; set; } = new();
}

/// <summary>
/// Inventory item report DTO
/// </summary>
public class InventoryItemReportDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastMovement { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public int DaysSinceLastMovement { get; set; }
}

/// <summary>
/// Inventory report summary DTO
/// </summary>
public class InventoryReportSummaryDto
{
    public int TotalProducts { get; set; }
    public decimal TotalValue { get; set; }
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }
    public int OverstockItems { get; set; }
    public int TotalQuantity { get; set; }
    public decimal AverageStockTurnover { get; set; }
    public int WarehouseCount { get; set; }
    public decimal InventoryAccuracy { get; set; }
}

/// <summary>
/// Stock movement report DTO
/// </summary>
public class StockMovementReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public List<StockMovementItemDto> Movements { get; set; } = new();
    public StockMovementSummaryDto Summary { get; set; } = new();
}

/// <summary>
/// Stock movement item DTO
/// </summary>
public class StockMovementItemDto
{
    public DateTime TransactionDate { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string WarehouseLocation { get; set; } = string.Empty;
    public string CreatedByName { get; set; } = string.Empty;
    public decimal UnitValue { get; set; }
    public decimal TotalValue { get; set; }
}

/// <summary>
/// Stock movement summary DTO
/// </summary>
public class StockMovementSummaryDto
{
    public int TotalTransactions { get; set; }
    public int TotalInbound { get; set; }
    public int TotalOutbound { get; set; }
    public int NetMovement { get; set; }
    public decimal TotalInboundValue { get; set; }
    public decimal TotalOutboundValue { get; set; }
    public decimal NetValue { get; set; }
    public List<string> TopMovementTypes { get; set; } = new();
}

/// <summary>
/// Warehouse inventory DTO
/// </summary>
public class WarehouseInventoryDto
{
    public string WarehouseLocation { get; set; } = string.Empty;
    public string WarehouseName { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public int LowStockItems { get; set; }
    public int OutOfStockItems { get; set; }
    public DateTime LastUpdated { get; set; }
    public decimal UtilizationPercentage { get; set; }
    public int CapacityLimit { get; set; }
}

/// <summary>
/// Overstock item DTO
/// </summary>
public class OverstockItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public int MaxStockLevel { get; set; }
    public int ExcessQuantity { get; set; }
    public decimal ExcessValue { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public int DaysOfExcess { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
}

#endregion

#region KPI & Analytics DTOs

/// <summary>
/// Inventory KPI DTO
/// </summary>
public class InventoryKPIDto
{
    public decimal InventoryTurnoverRatio { get; set; }
    public int DaysOfInventoryOnHand { get; set; }
    public decimal StockoutRate { get; set; }
    public decimal InventoryAccuracy { get; set; }
    public decimal TotalInventoryValue { get; set; }
    public int ProductsNeedingReorder { get; set; }
    public int OverstockItems { get; set; }
    public decimal DeadStockValue { get; set; }
    public decimal CarryingCostPercentage { get; set; }
    public decimal FillRate { get; set; }
    public DateTime CalculatedAt { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
}

/// <summary>
/// Inventory health score DTO
/// </summary>
public class InventoryHealthScoreDto
{
    public double OverallScore { get; set; } // 0-100
    public double StockLevelScore { get; set; }
    public double TurnoverScore { get; set; }
    public double AccuracyScore { get; set; }
    public double WarehouseEfficiencyScore { get; set; }
    public double ServiceLevelScore { get; set; }
    public List<string> Recommendations { get; set; } = new();
    public List<string> CriticalIssues { get; set; } = new();
    public DateTime CalculatedAt { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
}

#endregion

#region Serial Number & Asset Tracking DTOs

/// <summary>
/// Serial number DTO
/// </summary>
public class SerialNumberDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime ManufactureDate { get; set; }
    public DateTime? SoldDate { get; set; }
    public DateTime? WarrantyExpiry { get; set; }
    public string Status { get; set; } = string.Empty; // Available, Reserved, Sold, Defective, Returned
    public string OrderReference { get; set; } = string.Empty;
    public string WarehouseLocation { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

#endregion

#region Cycle Counting DTOs

/// <summary>
/// Cycle count DTO
/// </summary>
public class CycleCountDto
{
    public int Id { get; set; }
    public string CountName { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public DateTime? StartedDate { get; set; }
    public DateTime? CompletedDate { get; set; }
    public string Status { get; set; } = string.Empty; // Scheduled, InProgress, Completed, Cancelled
    public string WarehouseLocation { get; set; } = string.Empty;
    public List<int> ProductIds { get; set; } = new();
    public string AssignedTo { get; set; } = string.Empty;
    public string AssignedToName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int CountedItems { get; set; }
    public int DiscrepanciesFound { get; set; }
}

/// <summary>
/// Cycle count request DTO
/// </summary>
public class CycleCountRequest
{
    [Required]
    [StringLength(255, ErrorMessage = "Count name cannot exceed 255 characters")]
    public string CountName { get; set; } = string.Empty;
    
    [Required]
    public DateTime ScheduledDate { get; set; }
    
    [Required]
    [StringLength(255, ErrorMessage = "Warehouse location cannot exceed 255 characters")]
    public string WarehouseLocation { get; set; } = string.Empty;
    
    [Required]
    public List<int> ProductIds { get; set; } = new();
    
    [StringLength(255, ErrorMessage = "Assigned to cannot exceed 255 characters")]
    public string AssignedTo { get; set; } = string.Empty;
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Counted item DTO
/// </summary>
public class CountedItemDto
{
    [Required]
    public int ProductId { get; set; }
    
    public string SKU { get; set; } = string.Empty;
    
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "System quantity must be non-negative")]
    public int SystemQuantity { get; set; }
    
    [Required]
    [Range(0, int.MaxValue, ErrorMessage = "Counted quantity must be non-negative")]
    public int CountedQuantity { get; set; }
    
    [Required]
    [StringLength(255, ErrorMessage = "Counted by cannot exceed 255 characters")]
    public string CountedBy { get; set; } = string.Empty;
    
    [Required]
    public DateTime CountedAt { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Inventory discrepancy DTO
/// </summary>
public class InventoryDiscrepancyDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int SystemQuantity { get; set; }
    public int CountedQuantity { get; set; }
    public int Variance { get; set; }
    public decimal VarianceValue { get; set; }
    public decimal VariancePercentage { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool RequiresApproval { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime CountedAt { get; set; }
    public string CountedBy { get; set; } = string.Empty;
}

#endregion

#region Purchase Order DTOs

/// <summary>
/// Purchase order DTO
/// </summary>
public class PurchaseOrderDto
{
    public int Id { get; set; }
    public string PONumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string SupplierCode { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public DateTime? ActualDeliveryDate { get; set; }
    public string Status { get; set; } = string.Empty; // Pending, Approved, Sent, PartiallyReceived, Received, Cancelled
    public List<POLineItemDto> LineItems { get; set; } = new();
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "VND";
    public string Notes { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}

/// <summary>
/// Purchase order line item DTO
/// </summary>
public class POLineItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int OrderedQuantity { get; set; }
    public int ReceivedQuantity { get; set; }
    public int RemainingQuantity => OrderedQuantity - ReceivedQuantity;
    public decimal UnitCost { get; set; }
    public decimal TotalCost => OrderedQuantity * UnitCost;
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Received item DTO
/// </summary>
public class ReceivedItemDto
{
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Received quantity must be positive")]
    public int ReceivedQuantity { get; set; }
    
    [Required]
    [Range(0, double.MaxValue, ErrorMessage = "Actual unit cost must be non-negative")]
    public decimal ActualUnitCost { get; set; }
    
    [Required]
    [StringLength(50, ErrorMessage = "Condition cannot exceed 50 characters")]
    public string Condition { get; set; } = string.Empty; // Good, Damaged, Defective
    
    public List<string> SerialNumbers { get; set; } = new();
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
    
    [StringLength(255, ErrorMessage = "Warehouse location cannot exceed 255 characters")]
    public string WarehouseLocation { get; set; } = string.Empty;
}

#endregion

#region Damage & Return DTOs

/// <summary>
/// Damage report request DTO
/// </summary>
public class DamageReportRequest
{
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Damaged quantity must be positive")]
    public int DamagedQuantity { get; set; }
    
    [Required]
    [StringLength(100, ErrorMessage = "Damage type cannot exceed 100 characters")]
    public string DamageType { get; set; } = string.Empty; // Physical, Water, Electrical, etc.
    
    [Required]
    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
    public string Description { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255, ErrorMessage = "Reported by cannot exceed 255 characters")]
    public string ReportedBy { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255, ErrorMessage = "Warehouse location cannot exceed 255 characters")]
    public string WarehouseLocation { get; set; } = string.Empty;
    
    public List<string> SerialNumbers { get; set; } = new();
    
    public bool IsInsuranceClaim { get; set; }
    
    [StringLength(500, ErrorMessage = "Root cause cannot exceed 500 characters")]
    public string RootCause { get; set; } = string.Empty;
}

/// <summary>
/// Damaged inventory DTO
/// </summary>
public class DamagedInventoryDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int DamagedQuantity { get; set; }
    public string DamageType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime ReportedDate { get; set; }
    public string ReportedBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // Reported, Investigating, Approved, Disposed
    public decimal EstimatedLoss { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public bool IsInsuranceClaim { get; set; }
    public string RootCause { get; set; } = string.Empty;
}

/// <summary>
/// Customer return request DTO
/// </summary>
public class CustomerReturnRequest
{
    [Required]
    [StringLength(255, ErrorMessage = "Order number cannot exceed 255 characters")]
    public string OrderNumber { get; set; } = string.Empty;
    
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Return quantity must be positive")]
    public int ReturnQuantity { get; set; }
    
    [Required]
    [StringLength(500, ErrorMessage = "Return reason cannot exceed 500 characters")]
    public string ReturnReason { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255, ErrorMessage = "Customer name cannot exceed 255 characters")]
    public string CustomerName { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50, ErrorMessage = "Condition cannot exceed 50 characters")]
    public string Condition { get; set; } = string.Empty; // New, Used, Damaged
    
    public bool IsWithinWarranty { get; set; }
    
    [StringLength(255, ErrorMessage = "Serial number cannot exceed 255 characters")]
    public string SerialNumber { get; set; } = string.Empty;
    
    public bool IsRefundable { get; set; }
    
    [StringLength(1000, ErrorMessage = "Notes cannot exceed 1000 characters")]
    public string Notes { get; set; } = string.Empty;
}

#endregion

#region Forecasting & Demand Planning DTOs

/// <summary>
/// Demand forecast DTO
/// </summary>
public class DemandForecastDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public Dictionary<DateTime, int> ForecastedDemand { get; set; } = new();
    public double Accuracy { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string ForecastMethod { get; set; } = string.Empty;
    public int RecommendedReorderQuantity { get; set; }
    public DateTime RecommendedReorderDate { get; set; }
    public List<string> FactorsConsidered { get; set; } = new();
    public double ConfidenceLevel { get; set; }
}

/// <summary>
/// Forecast parameters DTO
/// </summary>
public class ForecastParametersDto
{
    [Required]
    [Range(1, 365, ErrorMessage = "Seasonality period must be between 1 and 365 days")]
    public int SeasonalityPeriod { get; set; }
    
    [Required]
    [Range(0.1, 1.0, ErrorMessage = "Trend smoothing factor must be between 0.1 and 1.0")]
    public double TrendSmoothingFactor { get; set; }
    
    [Required]
    [Range(0.1, 1.0, ErrorMessage = "Seasonal smoothing factor must be between 0.1 and 1.0")]
    public double SeasonalSmoothingFactor { get; set; }
    
    [Required]
    [Range(30, 730, ErrorMessage = "History period must be between 30 and 730 days")]
    public int HistoryPeriodDays { get; set; }
    
    [Required]
    [Range(1.0, 3.0, ErrorMessage = "Safety stock factor must be between 1.0 and 3.0")]
    public double SafetyStockFactor { get; set; }
}

/// <summary>
/// Slow moving item DTO
/// </summary>
public class SlowMovingItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int CurrentStock { get; set; }
    public DateTime LastSaleDate { get; set; }
    public int DaysSinceLastSale { get; set; }
    public decimal CarryingCost { get; set; }
    public decimal EstimatedValue { get; set; }
    public string RecommendedAction { get; set; } = string.Empty;
    public string WarehouseLocation { get; set; } = string.Empty;
    public int AverageMonthlyMovement { get; set; }
}

/// <summary>
/// Fast moving item DTO
/// </summary>
public class FastMovingItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public int SalesVolume { get; set; }
    public decimal Revenue { get; set; }
    public int DaysAnalyzed { get; set; }
    public double VelocityScore { get; set; }
    public int CurrentStock { get; set; }
    public int RecommendedStockLevel { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
}

#endregion

#region Warranty & Expiry DTOs

/// <summary>
/// Expiring warranty DTO
/// </summary>
public class ExpiringWarrantyDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public DateTime WarrantyExpiry { get; set; }
    public int DaysUntilExpiry { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string OrderReference { get; set; } = string.Empty;
    public DateTime PurchaseDate { get; set; }
    public string WarrantyType { get; set; } = string.Empty;
}

/// <summary>
/// Expired inventory DTO
/// </summary>
public class ExpiredInventoryDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public int ExpiredQuantity { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysExpired { get; set; }
    public decimal EstimatedLoss { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public bool CanBeDiscounted { get; set; }
}

#endregion

#region Bulk Operation DTOs

/// <summary>
/// Bulk operation result DTO
/// </summary>
public class BulkOperationResult
{
    public bool IsSuccess { get; set; }
    public int TotalItems { get; set; }
    public int SuccessfulItems { get; set; }
    public int FailedItems { get; set; }
    public List<BulkOperationError> Errors { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public TimeSpan Duration => CompletedAt - StartedAt;
    public string OperationType { get; set; } = string.Empty;
}

/// <summary>
/// Bulk operation error DTO
/// </summary>
public class BulkOperationError
{
    public int Index { get; set; }
    public string ItemIdentifier { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}

#endregion