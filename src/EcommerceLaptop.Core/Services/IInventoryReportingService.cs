using EcommerceLaptop.Core.DTOs.Inventory;

namespace EcommerceLaptop.Core.Services;

public interface IInventoryReportingService
{
    // Alerts
    Task<List<LowStockAlertDto>> GetLowStockAlertsAsync();
    Task<List<ReorderSuggestionDto>> GetReorderSuggestionsAsync();
    Task<List<StockAlertDto>> GetStockAlertsAsync(AlertSeverity severity);
    Task<List<StockAlertDto>> GetRealTimeAlertsAsync();

    // Reporting
    Task<InventoryReportDto> GenerateInventoryReportAsync(InventoryReportRequest request);
    Task<List<InventoryTransactionDto>> GetInventoryTransactionsAsync(int productId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<StockMovementReportDto> GenerateStockMovementReportAsync(DateTime startDate, DateTime endDate);
    Task<InventoryKPIDto> GetInventoryKPIsAsync(string warehouseLocation = "");
    Task<InventoryHealthScoreDto> GetInventoryHealthScoreAsync();
}
