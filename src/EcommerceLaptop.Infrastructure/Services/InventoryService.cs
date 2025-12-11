using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Inventory;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Services;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Facade for Inventory operations.
/// Delegates to specialized services: StockManagementService, AssetTrackingService, InventoryReportingService.
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly IStockManagementService _stockService;
    private readonly IAssetTrackingService _assetService;
    private readonly IInventoryReportingService _reportingService;

    public InventoryService(
        IStockManagementService stockService,
        IAssetTrackingService assetService,
        IInventoryReportingService reportingService)
    {
        _stockService = stockService;
        _assetService = assetService;
        _reportingService = reportingService;
    }

    // ==========================================
    // Stock Management Delegate
    // ==========================================

    public Task<InventoryDto?> GetInventoryByProductIdAsync(int productId)
        => _stockService.GetInventoryByProductIdAsync(productId);

    public Task<PagedResult<InventoryDto>> GetInventoriesAsync(InventoryFilterRequest request)
        => _stockService.GetInventoriesAsync(request);

    public Task<InventoryDto> CreateInventoryAsync(CreateInventoryRequest request)
        => _stockService.CreateInventoryAsync(request);

    public Task<InventoryDto> UpdateInventoryAsync(int productId, UpdateInventoryRequest request)
        => _stockService.UpdateInventoryAsync(productId, request);

    public Task<bool> DeleteInventoryAsync(int productId)
        => _stockService.DeleteInventoryAsync(productId);

    public Task<bool> AdjustStockAsync(StockAdjustmentRequest request)
        => _stockService.AdjustStockAsync(request);

    public Task<BulkOperationResult> BulkAdjustStockAsync(List<StockAdjustmentRequest> requests)
        => _stockService.BulkAdjustStockAsync(requests);

    public Task<bool> TransferStockAsync(StockTransferRequest request)
        => _stockService.TransferStockAsync(request);

    public Task<bool> ValidateStockLevelsAsync(List<int> productIds)
        => _stockService.ValidateStockLevelsAsync(productIds);
        
    public Task<bool> UpdateReorderLevelsAsync(int productId, int newReorderLevel)
        => _stockService.UpdateReorderLevelsAsync(productId, newReorderLevel);

    public Task<List<WarehouseInventoryDto>> GetWarehouseInventoryAsync(string warehouseLocation)
        => _stockService.GetWarehouseInventoryAsync(warehouseLocation);

    // ==========================================
    // Asset Tracking Delegate
    // ==========================================

    public Task<List<SerialNumberDto>> GetSerialNumbersAsync(int productId, bool activeOnly = true)
        => _assetService.GetSerialNumbersAsync(productId, activeOnly);

    public Task<List<SerialNumberDto>> GetAvailableSerialNumbersAsync(int productId)
        => _assetService.GetAvailableSerialNumbersAsync(productId);

    public Task<bool> AssignSerialNumberAsync(int productId, string serialNumber, string batchNumber = "")
        => _assetService.AssignSerialNumberAsync(productId, serialNumber, batchNumber);

    public Task<bool> ReserveSerialNumberAsync(string serialNumber, string orderReference)
        => _assetService.ReserveSerialNumberAsync(serialNumber, orderReference);

    public Task<SerialNumberDto?> GetProductBySerialNumberAsync(string serialNumber)
        => _assetService.GetProductBySerialNumberAsync(serialNumber);

    public Task<InventoryDto?> GetInventoryByBarcodeAsync(string barcode)
        => _assetService.GetInventoryByBarcodeAsync(barcode);

    public Task<InventoryDto?> GetInventoryBySKUAsync(string sku)
        => _assetService.GetInventoryBySKUAsync(sku);

    public Task<bool> GenerateBarcodeAsync(int productId, string format = "EAN13")
        => _assetService.GenerateBarcodeAsync(productId, format);

    public Task<List<InventoryDto>> ScanMultipleBarcodesAsync(List<string> barcodes)
        => _assetService.ScanMultipleBarcodesAsync(barcodes);

    // ==========================================
    // Inventory Reporting Delegate
    // ==========================================

    public Task<List<LowStockAlertDto>> GetLowStockAlertsAsync()
        => _reportingService.GetLowStockAlertsAsync();

    public Task<List<ReorderSuggestionDto>> GetReorderSuggestionsAsync()
        => _reportingService.GetReorderSuggestionsAsync();

    public Task<List<StockAlertDto>> GetStockAlertsAsync(AlertSeverity severity)
        => _reportingService.GetStockAlertsAsync(severity);
        
    public Task<List<StockAlertDto>> GetRealTimeAlertsAsync()
        => _reportingService.GetRealTimeAlertsAsync();

    public Task<InventoryReportDto> GenerateInventoryReportAsync(InventoryReportRequest request)
        => _reportingService.GenerateInventoryReportAsync(request);

    public Task<List<InventoryTransactionDto>> GetInventoryTransactionsAsync(int productId, DateTime? fromDate = null, DateTime? toDate = null)
        => _reportingService.GetInventoryTransactionsAsync(productId, fromDate, toDate);

    public Task<List<InventoryTransactionDto>> GetTransactionHistoryAsync(int productId, DateTime? fromDate = null, DateTime? toDate = null)
        => _reportingService.GetInventoryTransactionsAsync(productId, fromDate, toDate);

    public Task<StockMovementReportDto> GenerateStockMovementReportAsync(DateTime startDate, DateTime endDate)
        => _reportingService.GenerateStockMovementReportAsync(startDate, endDate);

    public Task<StockMovementReportDto> GetStockMovementReportAsync(DateTime startDate, DateTime endDate)
        => _reportingService.GenerateStockMovementReportAsync(startDate, endDate);

    public Task<InventoryKPIDto> GetInventoryKPIsAsync(string warehouseLocation = "")
        => _reportingService.GetInventoryKPIsAsync(warehouseLocation);

    public Task<InventoryHealthScoreDto> GetInventoryHealthScoreAsync()
        => _reportingService.GetInventoryHealthScoreAsync();

    public Task<InventoryHealthScoreDto> CalculateInventoryHealthScoreAsync()
        => _reportingService.GetInventoryHealthScoreAsync();


    // ==========================================
    // Future / Placeholders
    // ==========================================

    public Task<CycleCountDto> CreateCycleCountAsync(CycleCountRequest request) => throw new NotImplementedException("Phase 3");
    public Task<bool> RecordCycleCountAsync(int cycleCountId, List<CountedItemDto> countedItems) => throw new NotImplementedException("Phase 3");
    public Task<List<InventoryDiscrepancyDto>> GetInventoryDiscrepanciesAsync(int cycleCountId) => throw new NotImplementedException("Phase 3");
    public Task<bool> AdjustInventoryFromCycleCountAsync(int productId, bool adjustmentApproved) => throw new NotImplementedException("Phase 3");

    public Task<List<PurchaseOrderDto>> GetPendingPurchaseOrdersAsync() => throw new NotImplementedException("Phase 4");
    public Task<bool> ReceiveInventoryFromPOAsync(int purchaseOrderId, List<ReceivedItemDto> receivedItems) => throw new NotImplementedException("Phase 4");
    public Task<bool> CreateAutomaticPurchaseOrderAsync(List<ReorderSuggestionDto> suggestions) => throw new NotImplementedException("Phase 4");

    public Task<bool> RecordDamagedInventoryAsync(DamageReportRequest request) => throw new NotImplementedException("Phase 5");
    public Task<bool> ProcessCustomerReturnAsync(CustomerReturnRequest request) => throw new NotImplementedException("Phase 5");
    public Task<List<DamagedInventoryDto>> GetDamagedInventoryAsync(string warehouseLocation = "") => throw new NotImplementedException("Phase 5");
    public Task<bool> DisposeDamagedInventoryAsync(int damageId, string disposalMethod, string notes) => throw new NotImplementedException("Phase 5");

    public Task<List<SlowMovingItemDto>> GetSlowMovingInventoryAsync(int daysPeriod = 90) => throw new NotImplementedException("Phase 6");
    public Task<List<FastMovingItemDto>> GetFastMovingInventoryAsync(int daysPeriod = 30) => throw new NotImplementedException("Phase 6");
    public Task<bool> UpdateForecastParametersAsync(int productId, ForecastParametersDto parameters) => throw new NotImplementedException("Phase 6");
    public Task<DemandForecastDto> GenerateDemandForecastAsync(int productId, int forecastDays = 90) => throw new NotImplementedException("Phase 6");

    public Task<bool> UpdateProductExpiryDateAsync(int productId, DateTime expiryDate) => throw new NotImplementedException("Phase 7");
    public Task<List<ExpiredInventoryDto>> GetExpiredInventoryAsync(string warehouseLocation = "") => throw new NotImplementedException("Phase 7");
    public Task<List<ExpiringWarrantyDto>> GetExpiringWarrantiesAsync(int daysAhead = 30) => throw new NotImplementedException("Phase 7");
}
