using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Inventory;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Core.Services;

/// <summary>
/// Comprehensive inventory management service interface
/// Provides enterprise-grade inventory operations including CRUD, stock management,
/// alerts, reporting, serial number tracking, cycle counting, and demand forecasting
/// </summary>
public interface IInventoryService
{
    #region Core CRUD Operations
    
    /// <summary>
    /// Get inventory details for a specific product
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <returns>Inventory details or null if not found</returns>
    Task<InventoryDto?> GetInventoryByProductIdAsync(int productId);
    
    /// <summary>
    /// Get paginated list of inventories with filtering
    /// </summary>
    /// <param name="request">Filter criteria and pagination parameters</param>
    /// <returns>Paginated inventory results</returns>
    Task<PagedResult<InventoryDto>> GetInventoriesAsync(InventoryFilterRequest request);
    
    /// <summary>
    /// Create new inventory record for a product
    /// </summary>
    /// <param name="request">Inventory creation details</param>
    /// <returns>Created inventory details</returns>
    Task<InventoryDto> CreateInventoryAsync(CreateInventoryRequest request);
    
    /// <summary>
    /// Update existing inventory record
    /// </summary>
    /// <param name="id">Inventory record ID</param>
    /// <param name="request">Update parameters</param>
    /// <returns>Updated inventory details</returns>
    Task<InventoryDto> UpdateInventoryAsync(int id, UpdateInventoryRequest request);
    
    /// <summary>
    /// Delete inventory record (soft delete)
    /// </summary>
    /// <param name="id">Inventory record ID</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteInventoryAsync(int id);
    
    #endregion
    
    #region Stock Operations
    
    /// <summary>
    /// Adjust stock levels for a product (increase or decrease)
    /// </summary>
    /// <param name="request">Stock adjustment details</param>
    /// <returns>True if adjustment successful</returns>
    Task<bool> AdjustStockAsync(StockAdjustmentRequest request);
    
    /// <summary>
    /// Perform bulk stock adjustments for multiple products
    /// </summary>
    /// <param name="requests">List of stock adjustments</param>
    /// <returns>Bulk operation result with success/failure details</returns>
    Task<BulkOperationResult> BulkAdjustStockAsync(List<StockAdjustmentRequest> requests);
    
    /// <summary>
    /// Transfer stock between warehouse locations
    /// </summary>
    /// <param name="request">Transfer details</param>
    /// <returns>True if transfer successful</returns>
    Task<bool> TransferStockAsync(StockTransferRequest request);
    
    /// <summary>
    /// Validate stock levels for multiple products
    /// </summary>
    /// <param name="productIds">List of product IDs to validate</param>
    /// <returns>True if all products have adequate stock</returns>
    Task<bool> ValidateStockLevelsAsync(List<int> productIds);
    
    #endregion
    
    #region Alerts & Monitoring
    
    /// <summary>
    /// Get current low stock alerts
    /// </summary>
    /// <returns>List of products below reorder level</returns>
    Task<List<LowStockAlertDto>> GetLowStockAlertsAsync();
    
    /// <summary>
    /// Generate reorder suggestions based on stock levels and demand
    /// </summary>
    /// <returns>List of suggested reorders with quantities</returns>
    Task<List<ReorderSuggestionDto>> GetReorderSuggestionsAsync();
    
    /// <summary>
    /// Update reorder level for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="newReorderLevel">New reorder threshold</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateReorderLevelsAsync(int productId, int newReorderLevel);
    
    /// <summary>
    /// Get real-time stock alerts (low stock, out of stock, overstock)
    /// </summary>
    /// <returns>List of current alerts</returns>
    Task<List<StockAlertDto>> GetRealTimeAlertsAsync();
    
    #endregion
    
    #region Reporting & Analytics
    
    /// <summary>
    /// Generate comprehensive inventory report
    /// </summary>
    /// <param name="request">Report parameters and filters</param>
    /// <returns>Detailed inventory report</returns>
    Task<InventoryReportDto> GenerateInventoryReportAsync(InventoryReportRequest request);
    
    /// <summary>
    /// Get transaction history for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="fromDate">Start date filter (optional)</param>
    /// <param name="toDate">End date filter (optional)</param>
    /// <returns>List of inventory transactions</returns>
    Task<List<InventoryTransactionDto>> GetTransactionHistoryAsync(int productId, DateTime? fromDate = null, DateTime? toDate = null);
    
    /// <summary>
    /// Generate stock movement report for a date range
    /// </summary>
    /// <param name="fromDate">Report start date</param>
    /// <param name="toDate">Report end date</param>
    /// <returns>Stock movement summary and details</returns>
    Task<StockMovementReportDto> GetStockMovementReportAsync(DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Calculate inventory KPIs and metrics
    /// </summary>
    /// <param name="warehouseLocation">Optional warehouse filter</param>
    /// <returns>Key performance indicators</returns>
    Task<InventoryKPIDto> GetInventoryKPIsAsync(string warehouseLocation = "");
    
    /// <summary>
    /// Calculate overall inventory health score
    /// </summary>
    /// <returns>Health score with recommendations</returns>
    Task<InventoryHealthScoreDto> CalculateInventoryHealthScoreAsync();
    
    #endregion
    
    #region Warehouse Operations
    
    /// <summary>
    /// Get inventory summary for a specific warehouse
    /// </summary>
    /// <param name="warehouseLocation">Warehouse location code</param>
    /// <returns>Warehouse inventory summary</returns>
    Task<List<WarehouseInventoryDto>> GetWarehouseInventoryAsync(string warehouseLocation);
    
    #endregion
    
    #region Serial Number & Asset Tracking
    
    /// <summary>
    /// Assign serial number to a product (critical for electronics)
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="serialNumber">Unique serial number</param>
    /// <param name="batchNumber">Optional batch identifier</param>
    /// <returns>True if assigned successfully</returns>
    Task<bool> AssignSerialNumberAsync(int productId, string serialNumber, string batchNumber = "");
    
    /// <summary>
    /// Get serial numbers for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="onlyAvailable">Filter to available serial numbers only</param>
    /// <returns>List of serial numbers</returns>
    Task<List<SerialNumberDto>> GetSerialNumbersAsync(int productId, bool onlyAvailable = true);
    
    /// <summary>
    /// Reserve a specific serial number for an order
    /// </summary>
    /// <param name="serialNumber">Serial number to reserve</param>
    /// <param name="orderReference">Order reference</param>
    /// <returns>True if reserved successfully</returns>
    Task<bool> ReserveSerialNumberAsync(string serialNumber, string orderReference);
    
    /// <summary>
    /// Find product by serial number
    /// </summary>
    /// <param name="serialNumber">Serial number to search</param>
    /// <returns>Serial number details with product info</returns>
    Task<SerialNumberDto?> GetProductBySerialNumberAsync(string serialNumber);
    
    #endregion
    
    #region Barcode & SKU Management
    
    /// <summary>
    /// Get inventory by barcode
    /// </summary>
    /// <param name="barcode">Product barcode</param>
    /// <returns>Inventory details or null</returns>
    Task<InventoryDto?> GetInventoryByBarcodeAsync(string barcode);
    
    /// <summary>
    /// Get inventory by SKU
    /// </summary>
    /// <param name="sku">Product SKU</param>
    /// <returns>Inventory details or null</returns>
    Task<InventoryDto?> GetInventoryBySKUAsync(string sku);
    
    /// <summary>
    /// Generate barcode for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="format">Barcode format (EAN13, UPC, etc.)</param>
    /// <returns>True if barcode generated successfully</returns>
    Task<bool> GenerateBarcodeAsync(int productId, string format = "EAN13");
    
    /// <summary>
    /// Scan multiple barcodes and return inventory details
    /// </summary>
    /// <param name="barcodes">List of barcodes to scan</param>
    /// <returns>List of inventory details for scanned barcodes</returns>
    Task<List<InventoryDto>> ScanMultipleBarcodesAsync(List<string> barcodes);
    
    #endregion
    
    #region Cycle Counting & Physical Inventory
    
    /// <summary>
    /// Create a new cycle count schedule
    /// </summary>
    /// <param name="request">Cycle count parameters</param>
    /// <returns>Created cycle count details</returns>
    Task<CycleCountDto> CreateCycleCountAsync(CycleCountRequest request);
    
    /// <summary>
    /// Record counted quantities for cycle count
    /// </summary>
    /// <param name="cycleCountId">Cycle count ID</param>
    /// <param name="countedItems">List of counted items with quantities</param>
    /// <returns>True if recorded successfully</returns>
    Task<bool> RecordCycleCountAsync(int cycleCountId, List<CountedItemDto> countedItems);
    
    /// <summary>
    /// Get inventory discrepancies from cycle count
    /// </summary>
    /// <param name="cycleCountId">Cycle count ID</param>
    /// <returns>List of discrepancies found</returns>
    Task<List<InventoryDiscrepancyDto>> GetInventoryDiscrepanciesAsync(int cycleCountId);
    
    /// <summary>
    /// Adjust inventory based on cycle count results
    /// </summary>
    /// <param name="cycleCountId">Cycle count ID</param>
    /// <param name="autoApprove">Auto-approve adjustments if true</param>
    /// <returns>True if adjustments applied successfully</returns>
    Task<bool> AdjustInventoryFromCycleCountAsync(int cycleCountId, bool autoApprove = false);
    
    #endregion
    
    #region Purchase Order Integration
    
    /// <summary>
    /// Get pending purchase orders requiring inventory receipt
    /// </summary>
    /// <returns>List of pending purchase orders</returns>
    Task<List<PurchaseOrderDto>> GetPendingPurchaseOrdersAsync();
    
    /// <summary>
    /// Receive inventory from purchase order
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="receivedItems">List of received items with quantities</param>
    /// <returns>True if received successfully</returns>
    Task<bool> ReceiveInventoryFromPOAsync(int purchaseOrderId, List<ReceivedItemDto> receivedItems);
    
    /// <summary>
    /// Create automatic purchase orders from reorder suggestions
    /// </summary>
    /// <param name="reorderSuggestions">List of items to reorder</param>
    /// <returns>True if purchase orders created successfully</returns>
    Task<bool> CreateAutomaticPurchaseOrderAsync(List<ReorderSuggestionDto> reorderSuggestions);
    
    #endregion
    
    #region Damage & Return Handling
    
    /// <summary>
    /// Record damaged inventory
    /// </summary>
    /// <param name="request">Damage report details</param>
    /// <returns>True if recorded successfully</returns>
    Task<bool> RecordDamagedInventoryAsync(DamageReportRequest request);
    
    /// <summary>
    /// Process customer return and update inventory
    /// </summary>
    /// <param name="request">Return details</param>
    /// <returns>True if processed successfully</returns>
    Task<bool> ProcessCustomerReturnAsync(CustomerReturnRequest request);
    
    /// <summary>
    /// Get damaged inventory items
    /// </summary>
    /// <param name="warehouseLocation">Optional warehouse filter</param>
    /// <returns>List of damaged inventory items</returns>
    Task<List<DamagedInventoryDto>> GetDamagedInventoryAsync(string warehouseLocation = "");
    
    /// <summary>
    /// Dispose of damaged inventory
    /// </summary>
    /// <param name="damageId">Damage record ID</param>
    /// <param name="disposalMethod">Method of disposal</param>
    /// <param name="notes">Disposal notes</param>
    /// <returns>True if disposed successfully</returns>
    Task<bool> DisposeDamagedInventoryAsync(int damageId, string disposalMethod, string notes);
    
    #endregion
    
    #region Forecasting & Demand Planning
    
    /// <summary>
    /// Generate demand forecast for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="forecastDays">Number of days to forecast</param>
    /// <returns>Demand forecast data</returns>
    Task<DemandForecastDto> GenerateDemandForecastAsync(int productId, int forecastDays = 90);
    
    /// <summary>
    /// Get slow-moving inventory items
    /// </summary>
    /// <param name="daysSinceLastSale">Days since last sale threshold</param>
    /// <returns>List of slow-moving items</returns>
    Task<List<SlowMovingItemDto>> GetSlowMovingInventoryAsync(int daysSinceLastSale = 90);
    
    /// <summary>
    /// Get fast-moving inventory items
    /// </summary>
    /// <param name="daysPeriod">Analysis period in days</param>
    /// <returns>List of fast-moving items</returns>
    Task<List<FastMovingItemDto>> GetFastMovingInventoryAsync(int daysPeriod = 30);
    
    /// <summary>
    /// Update forecasting parameters for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="parameters">Forecasting parameters</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateForecastParametersAsync(int productId, ForecastParametersDto parameters);
    
    #endregion
    
    #region Expiry & Warranty Tracking
    
    /// <summary>
    /// Get products with expiring warranties
    /// </summary>
    /// <param name="daysAhead">Days ahead to check for expiry</param>
    /// <returns>List of products with expiring warranties</returns>
    Task<List<ExpiringWarrantyDto>> GetExpiringWarrantiesAsync(int daysAhead = 30);
    
    /// <summary>
    /// Update product expiry date
    /// </summary>
    /// <param name="inventoryId">Inventory ID</param>
    /// <param name="expiryDate">New expiry date</param>
    /// <returns>True if updated successfully</returns>
    Task<bool> UpdateProductExpiryDateAsync(int inventoryId, DateTime expiryDate);
    
    /// <summary>
    /// Get expired inventory items
    /// </summary>
    /// <param name="warehouseLocation">Optional warehouse filter</param>
    /// <returns>List of expired inventory items</returns>
    Task<List<ExpiredInventoryDto>> GetExpiredInventoryAsync(string warehouseLocation = "");
    
    #endregion
}