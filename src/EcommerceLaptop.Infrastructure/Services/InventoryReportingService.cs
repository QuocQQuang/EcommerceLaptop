using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Inventory;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Interfaces;

namespace EcommerceLaptop.Infrastructure.Services;

public class InventoryReportingService : IInventoryReportingService
{
    private readonly IInventoryRepository _repository;
    private readonly ILogger<InventoryReportingService> _logger;

    public InventoryReportingService(IInventoryRepository repository, ILogger<InventoryReportingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<LowStockAlertDto>> GetLowStockAlertsAsync()
    {
        _logger.LogInformation("Getting low stock alerts");
        var lowStockItems = await _repository.GetLowStockAsync();

        return lowStockItems.Select(inventory => new LowStockAlertDto
        {
            ProductId = inventory.ProductId,
            ProductName = inventory.Product.Name,
            SKU = inventory.Product.SKU,
            CurrentStock = inventory.QuantityInStock,
            ReorderLevel = inventory.ReorderLevel,
            RecommendedOrderQuantity = inventory.MaxStockLevel - inventory.QuantityInStock,
            WarehouseLocation = inventory.WarehouseLocation,
            DaysOutOfStock = CalculateDaysWithoutStock(inventory),
            Severity = CalculateAlertSeverity(inventory)
        }).ToList();
    }

    public async Task<List<ReorderSuggestionDto>> GetReorderSuggestionsAsync()
    {
        _logger.LogInformation("Getting reorder suggestions");
        var lowStockItems = await _repository.GetLowStockAsync(thresholdMultiplier: 2);

        return lowStockItems.Select(inventory => new ReorderSuggestionDto
        {
            ProductId = inventory.ProductId,
            ProductName = inventory.Product.Name,
            SKU = inventory.Product.SKU,
            CurrentStock = inventory.QuantityInStock,
            ReorderLevel = inventory.ReorderLevel,
            MaxStockLevel = inventory.MaxStockLevel,
            SuggestedOrderQuantity = inventory.MaxStockLevel - inventory.QuantityInStock,
            EstimatedCost = CalculateEstimatedReorderCost(inventory),
            LeadTimeDays = 7,
            SuggestedOrderDate = DateTime.UtcNow
        }).ToList();
    }

    public async Task<List<StockAlertDto>> GetStockAlertsAsync(AlertSeverity severity)
    {
        _logger.LogInformation("Getting stock alerts for severity {Severity}", severity);
        var inventories = await _repository.GetAllAsync();
        var alerts = new List<StockAlertDto>();

        foreach (var inventory in inventories)
        {
            var alertSeverity = CalculateAlertSeverity(inventory);
            if (severity == AlertSeverity.All || alertSeverity == severity)
            {
                alerts.Add(new StockAlertDto
                {
                    ProductId = inventory.ProductId,
                    ProductName = inventory.Product?.Name ?? "Unknown",
                    SKU = inventory.Product?.SKU ?? "Unknown",
                    AlertType = GetAlertType(inventory),
                    Severity = alertSeverity,
                    Message = GenerateAlertMessage(inventory),
                    CreatedAt = DateTime.UtcNow,
                    IsRead = false,
                    WarehouseLocation = inventory.WarehouseLocation,
                    ActionRequired = GenerateActionRequired(inventory)
                });
            }
        }
        return alerts.OrderByDescending(a => a.Severity).ToList();
    }

    public async Task<List<StockAlertDto>> GetRealTimeAlertsAsync()
    {
        return await GetStockAlertsAsync(AlertSeverity.All);
    }

    public async Task<InventoryReportDto> GenerateInventoryReportAsync(InventoryReportRequest request)
    {
        _logger.LogInformation("Generating inventory report for type {ReportType}", request.ReportType);
        var inventories = await _repository.GetInventoriesForReportAsync(request);

        var items = inventories.Select(i => new InventoryItemReportDto
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? "Unknown",
            SKU = i.Product?.SKU ?? "Unknown",
            Brand = i.Product?.Brand ?? "Unknown",
            QuantityInStock = i.QuantityInStock,
            ReservedQuantity = 0,
            AvailableQuantity = i.QuantityInStock,
            UnitCost = 100, 
            TotalValue = i.QuantityInStock * 100,
            WarehouseLocation = i.WarehouseLocation,
            LastMovement = i.LastStockUpdate,
            MovementType = "Stock Update",
            DaysSinceLastMovement = (DateTime.UtcNow - i.LastStockUpdate).Days
        }).ToList();

        var summary = new InventoryReportSummaryDto
        {
            TotalProducts = inventories.Count,
            TotalValue = inventories.Sum(i => i.QuantityInStock * 100),
            LowStockItems = inventories.Count(i => i.QuantityInStock <= i.ReorderLevel),
            OutOfStockItems = inventories.Count(i => i.QuantityInStock == 0),
            OverstockItems = inventories.Count(i => i.QuantityInStock > i.MaxStockLevel),
            TotalQuantity = inventories.Sum(i => i.QuantityInStock),
            AverageStockTurnover = 0,
            WarehouseCount = inventories.Select(i => i.WarehouseLocation).Distinct().Count(),
            InventoryAccuracy = 100
        };

        return new InventoryReportDto
        {
            ReportType = request.ReportType,
            GeneratedAt = DateTime.UtcNow,
            GeneratedBy = "System",
            WarehouseLocation = request.WarehouseLocation ?? "All",
            Items = items,
            Summary = summary,
            LowStockItems = inventories.Where(i => i.QuantityInStock <= i.ReorderLevel)
                .Select(i => new LowStockAlertDto
                {
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? "Unknown",
                    SKU = i.Product?.SKU ?? "Unknown",
                    Brand = i.Product?.Brand ?? "Unknown",
                    CurrentStock = i.QuantityInStock,
                    ReorderLevel = i.ReorderLevel,
                    RecommendedOrderQuantity = i.MaxStockLevel - i.QuantityInStock,
                    WarehouseLocation = i.WarehouseLocation,
                    LastStockUpdate = i.LastStockUpdate,
                    Severity = CalculateAlertSeverity(i),
                    DaysOutOfStock = CalculateDaysWithoutStock(i),
                    EstimatedDailySales = 10 
                }).ToList()
        };
    }

    public async Task<List<InventoryTransactionDto>> GetInventoryTransactionsAsync(int productId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        _logger.LogInformation("Getting inventory transactions for product {ProductId}", productId);
        var transactions = await _repository.GetTransactionsAsync(productId, fromDate, toDate);

        return transactions.Select(t => new InventoryTransactionDto
        {
            Id = t.Id,
            InventoryId = t.InventoryId,
            ProductName = t.Inventory?.Product?.Name ?? "Unknown",
            ProductSKU = t.Inventory?.Product?.SKU ?? "Unknown",
            Type = t.Type.ToString(),
            Quantity = t.Quantity,
            Reference = t.Reference,
            Notes = t.Notes,
            CreatedAt = t.CreatedAt,
            CreatedBy = t.CreatedBy,
            WarehouseLocation = t.Inventory?.WarehouseLocation ?? "Unknown"
        }).ToList();
    }

    public async Task<StockMovementReportDto> GenerateStockMovementReportAsync(DateTime startDate, DateTime endDate)
    {
        _logger.LogInformation("Generating stock movement report from {StartDate} to {EndDate}", startDate, endDate);
        var transactions = await _repository.GetStockMovementAsync(startDate, endDate);

        var movements = transactions.Select(t => new StockMovementItemDto
        {
            TransactionDate = t.CreatedAt,
            TransactionType = t.Type.ToString(),
            ProductId = t.Inventory.ProductId,
            ProductName = t.Inventory?.Product?.Name ?? "Unknown",
            SKU = t.Inventory?.Product?.SKU ?? "Unknown",
            Quantity = t.Quantity,
            Reference = t.Reference,
            WarehouseLocation = t.Inventory?.WarehouseLocation ?? "Unknown"
        }).ToList();

        var summary = new StockMovementSummaryDto
        {
            TotalTransactions = transactions.Count,
            TotalInbound = transactions.Where(t => t.Quantity > 0).Sum(t => t.Quantity),
            TotalOutbound = Math.Abs(transactions.Where(t => t.Quantity < 0).Sum(t => t.Quantity)),
            NetMovement = transactions.Sum(t => t.Quantity),
            TopMovementTypes = transactions.GroupBy(t => t.Type.ToString())
                .OrderByDescending(g => g.Count()).Take(5).Select(g => g.Key).ToList()
        };

        return new StockMovementReportDto
        {
            FromDate = startDate,
            ToDate = endDate,
            Movements = movements,
            Summary = summary
        };
    }

    public async Task<InventoryKPIDto> GetInventoryKPIsAsync(string warehouseLocation = "")
    {
        _logger.LogInformation("Calculating inventory KPIs for warehouse: {WarehouseLocation}", warehouseLocation);
        var request = new InventoryReportRequest { WarehouseLocation = warehouseLocation };
        var inventories = await _repository.GetInventoriesForReportAsync(request);
        var transactions = await _repository.GetStockMovementAsync(DateTime.MinValue, DateTime.MaxValue);
        if (!string.IsNullOrEmpty(warehouseLocation))
        {
            transactions = transactions.Where(t => t.Inventory.WarehouseLocation == warehouseLocation).ToList();
        }

        return new InventoryKPIDto
        {
            InventoryTurnoverRatio = CalculateStockTurnoverRate(inventories, transactions),
            DaysOfInventoryOnHand = CalculateDaysOfInventoryOnHand(inventories),
            StockoutRate = CalculateStockoutPercentage(inventories),
            InventoryAccuracy = 95.0m,
            TotalInventoryValue = inventories.Sum(i => i.QuantityInStock * 100m),
            ProductsNeedingReorder = inventories.Count(i => i.QuantityInStock <= i.ReorderLevel),
            OverstockItems = inventories.Count(i => i.QuantityInStock > i.MaxStockLevel * 0.9m),
            DeadStockValue = CalculateDeadStockValue(inventories),
            CarryingCostPercentage = 5.0m,
            FillRate = CalculateFillRate(inventories),
            CalculatedAt = DateTime.UtcNow,
            WarehouseLocation = warehouseLocation ?? "All Warehouses"
        };
    }

    public async Task<InventoryHealthScoreDto> GetInventoryHealthScoreAsync()
    {
        _logger.LogInformation("Calculating inventory health score");
        var inventories = (await _repository.GetAllAsync()).ToList();

        var totalProducts = inventories.Count;
        var lowStockCount = inventories.Count(i => i.QuantityInStock <= i.ReorderLevel);
        var excessStockCount = inventories.Count(i => i.QuantityInStock > i.MaxStockLevel * 0.9m);
        var optimalStockCount = totalProducts - lowStockCount - excessStockCount;
        var healthScore = totalProducts > 0 ? (decimal)optimalStockCount / totalProducts * 100 : 0;

        return new InventoryHealthScoreDto
        {
            OverallScore = (double)Math.Round(healthScore, 1),
            StockLevelScore = CalculateStockLevelScore(inventories),
            TurnoverScore = CalculateTurnoverScore(inventories),
            AccuracyScore = 95.0,
            WarehouseEfficiencyScore = CalculateWarehouseEfficiencyScore(inventories),
            ServiceLevelScore = (double)CalculateServiceLevel(inventories),
            Recommendations = GenerateHealthRecommendations(inventories),
            CriticalIssues = GenerateCriticalIssues(inventories),
            CalculatedAt = DateTime.UtcNow,
            WarehouseLocation = "All Warehouses"
        };
    }

    // Helpers
    private int CalculateDaysWithoutStock(Inventory inventory) => 0; // Stub

    private AlertSeverity CalculateAlertSeverity(Inventory inventory)
    {
        if (inventory.QuantityInStock == 0) return AlertSeverity.Critical;
        if (inventory.QuantityInStock <= inventory.ReorderLevel * 0.5) return AlertSeverity.High;
        if (inventory.QuantityInStock <= inventory.ReorderLevel) return AlertSeverity.Medium;
        return AlertSeverity.Low;
    }

    private decimal CalculateEstimatedReorderCost(Inventory inventory)
    {
        var recommendedQuantity = inventory.MaxStockLevel - inventory.QuantityInStock;
        return recommendedQuantity * 100m; 
    }

    private string GetAlertType(Inventory inventory)
    {
        if (inventory.QuantityInStock == 0) return "OUT_OF_STOCK";
        if (inventory.QuantityInStock <= inventory.ReorderLevel) return "LOW_STOCK";
        if (inventory.QuantityInStock > inventory.MaxStockLevel * 0.9m) return "EXCESS_STOCK";
        return "NORMAL";
    }

    private string GenerateAlertMessage(Inventory inventory)
    {
        return GetAlertType(inventory) switch
        {
            "OUT_OF_STOCK" => $"Product {inventory.Product?.Name} is out of stock",
            "LOW_STOCK" => $"Product {inventory.Product?.Name} is below reorder level ({inventory.QuantityInStock}/{inventory.ReorderLevel})",
            "EXCESS_STOCK" => $"Product {inventory.Product?.Name} has excess stock ({inventory.QuantityInStock}/{inventory.MaxStockLevel})",
            _ => $"Product {inventory.Product?.Name} stock level is normal"
        };
    }

    private string GenerateActionRequired(Inventory inventory)
    {
        return inventory.QuantityInStock switch
        {
            0 => "Urgent reorder required",
            var qty when qty <= inventory.ReorderLevel => "Schedule reorder",
            var qty when qty > inventory.MaxStockLevel => "Consider reducing stock",
            _ => "No action required"
        };
    }

    private decimal CalculateStockTurnoverRate(IEnumerable<Inventory> inventories, IEnumerable<InventoryTransaction> transactions)
    {
        var totalSales = transactions.Where(t => t.Type == InventoryTransactionType.Sale).Sum(t => Math.Abs(t.Quantity));
        var averageInventory = inventories.Any() ? inventories.Average(i => i.QuantityInStock) : 0;
        return averageInventory > 0 ? totalSales / (decimal)averageInventory : 0;
    }

    private decimal CalculateFillRate(IList<Inventory> inventories)
    {
        var availableProducts = inventories.Count(i => i.QuantityInStock > 0);
        return inventories.Count > 0 ? (decimal)availableProducts / inventories.Count * 100 : 0;
    }

    private decimal CalculateStockoutPercentage(IList<Inventory> inventories)
    {
        var stockoutProducts = inventories.Count(i => i.QuantityInStock == 0);
        return inventories.Count > 0 ? (decimal)stockoutProducts / inventories.Count * 100 : 0;
    }

    private decimal CalculateServiceLevel(IList<Inventory> inventories)
    {
        var adequateStockProducts = inventories.Count(i => i.QuantityInStock > i.ReorderLevel);
        return inventories.Count > 0 ? (decimal)adequateStockProducts / inventories.Count * 100 : 0;
    }

    private int CalculateDaysOfInventoryOnHand(IList<Inventory> inventories)
    {
        if (!inventories.Any()) return 0;
        var averageStock = inventories.Average(i => i.QuantityInStock);
        var estimatedDailyConsumption = 5; 
        return estimatedDailyConsumption > 0 ? (int)(averageStock / estimatedDailyConsumption) : 999;
    }

    private decimal CalculateDeadStockValue(IEnumerable<Inventory> inventories)
    {
        var deadStockItems = inventories.Where(i => i.LastStockUpdate < DateTime.UtcNow.AddDays(-90));
        return deadStockItems.Sum(i => i.QuantityInStock * 50m);
    }

    private double CalculateStockLevelScore(IList<Inventory> inventories)
    {
        if (!inventories.Any()) return 100.0;
        var optimalCount = inventories.Count(i => i.QuantityInStock > i.ReorderLevel && i.QuantityInStock <= i.MaxStockLevel * 0.8m);
        return (double)optimalCount / inventories.Count * 100;
    }

    private double CalculateTurnoverScore(IList<Inventory> inventories)
    {
        if (!inventories.Any()) return 100.0;
        var averageUtilization = inventories.Average(i => (double)i.QuantityInStock / Math.Max(i.MaxStockLevel, 1));
        return Math.Min(100, averageUtilization * 100);
    }

    private double CalculateWarehouseEfficiencyScore(IList<Inventory> inventories)
    {
        if (!inventories.Any()) return 100.0;
        var stockedItems = inventories.Count(i => i.QuantityInStock > 0);
        return (double)stockedItems / inventories.Count * 100;
    }

    private List<string> GenerateHealthRecommendations(IList<Inventory> inventories)
    {
        var recommendations = new List<string>();
        var lowStockCount = inventories.Count(i => i.QuantityInStock <= i.ReorderLevel);
        if (lowStockCount > 0) recommendations.Add($"Review and reorder {lowStockCount} low stock items");
        var excessStockCount = inventories.Count(i => i.QuantityInStock > i.MaxStockLevel * 0.9m);
        if (excessStockCount > 0) recommendations.Add($"Consider promotions for {excessStockCount} excess stock items");
        var outdatedStockCount = inventories.Count(i => i.LastStockUpdate < DateTime.UtcNow.AddDays(-30));
        if (outdatedStockCount > 0) recommendations.Add($"Perform stock audit for {outdatedStockCount} items not updated in 30+ days");
        return recommendations;
    }

    private List<string> GenerateCriticalIssues(IList<Inventory> inventories)
    {
        var issues = new List<string>();
        var outOfStockCount = inventories.Count(i => i.QuantityInStock == 0);
        if (outOfStockCount > 0) issues.Add($"{outOfStockCount} products are out of stock");
        var criticalLowStock = inventories.Count(i => i.QuantityInStock > 0 && i.QuantityInStock <= i.ReorderLevel * 0.5m);
        if (criticalLowStock > 0) issues.Add($"{criticalLowStock} products are critically low on stock");
        var excessStock = inventories.Count(i => i.QuantityInStock > i.MaxStockLevel);
        if (excessStock > 0) issues.Add($"{excessStock} products exceed maximum stock levels");
        return issues;
    }
}
