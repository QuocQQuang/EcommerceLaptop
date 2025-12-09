using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs.Inventory;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.Controllers;

/// <summary>
/// Comprehensive inventory management controller
/// Provides enterprise-grade inventory operations including CRUD, stock management,
/// alerts, reporting, serial number tracking, cycle counting, and demand forecasting
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class InventoryController : BaseApiController
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger)
        : base(logger)
    {
        _inventoryService = inventoryService;
    }

    #region Core CRUD Operations

    /// <summary>
    /// Get inventory details for a specific product
    /// </summary>
    /// <param name="productId">Product identifier</param>
    /// <returns>Inventory details</returns>
    [HttpGet("product/{productId}")]
    [Authorize(Roles = "Admin,Manager,Warehouse,Sales")]
    public async Task<IActionResult> GetInventoryByProductId(int productId)
    {
        try
        {
            if (productId <= 0)
                return ErrorResponse("Invalid product ID", 400);

            var inventory = await _inventoryService.GetInventoryByProductIdAsync(productId);
            if (inventory == null)
                return ErrorResponse("Inventory not found for this product", 404);

            return SuccessResponse(inventory, "Inventory retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetInventoryByProductId));
        }
    }

    /// <summary>
    /// Get paginated list of inventories with filtering
    /// </summary>
    /// <param name="request">Filter criteria and pagination parameters</param>
    /// <returns>Paginated inventory results</returns>
    [HttpPost("search")]
    [Authorize(Roles = "Admin,Manager,Warehouse,Sales")]
    public async Task<IActionResult> GetInventories([FromBody] InventoryFilterRequest request)
    {
        try
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;

            var result = await _inventoryService.GetInventoriesAsync(request);
            return PaginatedResponse(result.Items, result.TotalCount, result.Page, result.PageSize);
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetInventories));
        }
    }

    /// <summary>
    /// Create new inventory record for a product
    /// </summary>
    /// <param name="request">Inventory creation details</param>
    /// <returns>Created inventory details</returns>
    [HttpPost]
    [Authorize(Policy = "RequirePermission:products:manage")]
    public async Task<IActionResult> CreateInventory([FromBody] CreateInventoryRequest request)
    {
        try
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;

            var inventory = await _inventoryService.CreateInventoryAsync(request);
            return SuccessResponse(inventory, "Inventory created successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(CreateInventory));
        }
    }

    /// <summary>
    /// Update existing inventory record
    /// </summary>
    /// <param name="id">Inventory record ID</param>
    /// <param name="request">Update parameters</param>
    /// <returns>Updated inventory details</returns>
    [HttpPut("{id}")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    public async Task<IActionResult> UpdateInventory(int id, [FromBody] UpdateInventoryRequest request)
    {
        try
        {
            if (id <= 0)
                return ErrorResponse("Invalid inventory ID", 400);

            var validation = ValidateModelState();
            if (validation != null) return validation;

            var inventory = await _inventoryService.UpdateInventoryAsync(id, request);
            return SuccessResponse(inventory, "Inventory updated successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(UpdateInventory));
        }
    }

    /// <summary>
    /// Delete inventory record (soft delete)
    /// </summary>
    /// <param name="id">Inventory record ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DeleteInventory(int id)
    {
        try
        {
            if (id <= 0)
                return ErrorResponse("Invalid inventory ID", 400);

            var result = await _inventoryService.DeleteInventoryAsync(id);
            if (!result)
                return ErrorResponse("Failed to delete inventory", 400);

            return SuccessResponse(true, "Inventory deleted successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(DeleteInventory));
        }
    }

    #endregion

    #region Stock Operations

    /// <summary>
    /// Adjust stock levels for a product (increase or decrease)
    /// </summary>
    /// <param name="request">Stock adjustment details</param>
    /// <returns>Success status</returns>
    [HttpPost("adjust-stock")]
    [Authorize(Policy = "RequirePermission:products:manage")]
    public async Task<IActionResult> AdjustStock([FromBody] StockAdjustmentRequest request)
    {
        try
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;

            var result = await _inventoryService.AdjustStockAsync(request);
            if (!result)
                return ErrorResponse("Failed to adjust stock", 400);

            return SuccessResponse(true, "Stock adjusted successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(AdjustStock));
        }
    }

    /// <summary>
    /// Perform bulk stock adjustments for multiple products
    /// </summary>
    /// <param name="requests">List of stock adjustments</param>
    /// <returns>Bulk operation result</returns>
    [HttpPost("bulk-adjust-stock")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> BulkAdjustStock([FromBody] List<StockAdjustmentRequest> requests)
    {
        try
        {
            if (requests == null || !requests.Any())
                return ErrorResponse("No adjustment requests provided", 400);

            var validation = ValidateModelState();
            if (validation != null) return validation;

            var result = await _inventoryService.BulkAdjustStockAsync(requests);
            return SuccessResponse(result, "Bulk stock adjustment completed");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(BulkAdjustStock));
        }
    }

    /// <summary>
    /// Transfer stock between warehouse locations
    /// </summary>
    /// <param name="request">Transfer details</param>
    /// <returns>Success status</returns>
    [HttpPost("transfer-stock")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> TransferStock([FromBody] StockTransferRequest request)
    {
        try
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;

            var result = await _inventoryService.TransferStockAsync(request);
            if (!result)
                return ErrorResponse("Failed to transfer stock", 400);

            return SuccessResponse(true, "Stock transferred successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(TransferStock));
        }
    }

    /// <summary>
    /// Validate stock levels for multiple products
    /// </summary>
    /// <param name="productIds">List of product IDs to validate</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate-stock")]
    [Authorize(Roles = "Admin,Manager,Warehouse,Sales")]
    public async Task<IActionResult> ValidateStockLevels([FromBody] List<int> productIds)
    {
        try
        {
            if (productIds == null || !productIds.Any())
                return ErrorResponse("No product IDs provided", 400);

            var result = await _inventoryService.ValidateStockLevelsAsync(productIds);
            return SuccessResponse(result, "Stock validation completed");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(ValidateStockLevels));
        }
    }

    #endregion

    #region Alerts & Monitoring

    /// <summary>
    /// Get current low stock alerts
    /// </summary>
    /// <returns>List of low stock alerts</returns>
    [HttpGet("alerts/low-stock")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> GetLowStockAlerts()
    {
        try
        {
            var alerts = await _inventoryService.GetLowStockAlertsAsync();
            return SuccessResponse(alerts, "Low stock alerts retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetLowStockAlerts));
        }
    }

    /// <summary>
    /// Generate reorder suggestions based on stock levels and demand
    /// </summary>
    /// <returns>List of reorder suggestions</returns>
    [HttpGet("alerts/reorder-suggestions")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> GetReorderSuggestions()
    {
        try
        {
            var suggestions = await _inventoryService.GetReorderSuggestionsAsync();
            return SuccessResponse(suggestions, "Reorder suggestions generated successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetReorderSuggestions));
        }
    }

    /// <summary>
    /// Update reorder level for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="newReorderLevel">New reorder threshold</param>
    /// <returns>Success status</returns>
    [HttpPut("reorder-level/{productId}")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> UpdateReorderLevel(int productId, [FromBody] int newReorderLevel)
    {
        try
        {
            if (productId <= 0)
                return ErrorResponse("Invalid product ID", 400);

            if (newReorderLevel < 0)
                return ErrorResponse("Reorder level cannot be negative", 400);

            var result = await _inventoryService.UpdateReorderLevelsAsync(productId, newReorderLevel);
            if (!result)
                return ErrorResponse("Failed to update reorder level", 400);

            return SuccessResponse(true, "Reorder level updated successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(UpdateReorderLevel));
        }
    }

    /// <summary>
    /// Get real-time stock alerts
    /// </summary>
    /// <returns>List of current alerts</returns>
    [HttpGet("alerts/real-time")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> GetRealTimeAlerts()
    {
        try
        {
            var alerts = await _inventoryService.GetRealTimeAlertsAsync();
            return SuccessResponse(alerts, "Real-time alerts retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetRealTimeAlerts));
        }
    }

    #endregion

    #region Reporting & Analytics

    /// <summary>
    /// Generate comprehensive inventory report
    /// </summary>
    /// <param name="request">Report parameters and filters</param>
    /// <returns>Inventory report</returns>
    [HttpPost("reports/inventory")]
    public async Task<IActionResult> GenerateInventoryReport([FromBody] InventoryReportRequest request)
    {
        try
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;

            var report = await _inventoryService.GenerateInventoryReportAsync(request);
            return SuccessResponse(report, "Inventory report generated successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GenerateInventoryReport));
        }
    }

    /// <summary>
    /// Get transaction history for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="fromDate">Start date filter</param>
    /// <param name="toDate">End date filter</param>
    /// <returns>Transaction history</returns>
    [HttpGet("transactions/{productId}")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> GetTransactionHistory(
        int productId,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            if (productId <= 0)
                return ErrorResponse("Invalid product ID", 400);

            var transactions = await _inventoryService.GetTransactionHistoryAsync(productId, fromDate, toDate);
            return SuccessResponse(transactions, "Transaction history retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetTransactionHistory));
        }
    }

    /// <summary>
    /// Generate stock movement report for a date range
    /// </summary>
    /// <param name="fromDate">Report start date</param>
    /// <param name="toDate">Report end date</param>
    /// <returns>Stock movement report</returns>
    [HttpGet("reports/stock-movement")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetStockMovementReport(
        [FromQuery, Required] DateTime fromDate,
        [FromQuery, Required] DateTime toDate)
    {
        try
        {
            if (fromDate >= toDate)
                return ErrorResponse("From date must be before to date", 400);

            if ((toDate - fromDate).TotalDays > 365)
                return ErrorResponse("Date range cannot exceed 365 days", 400);

            var report = await _inventoryService.GetStockMovementReportAsync(fromDate, toDate);
            return SuccessResponse(report, "Stock movement report generated successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetStockMovementReport));
        }
    }

    /// <summary>
    /// Calculate inventory KPIs and metrics
    /// </summary>
    /// <param name="warehouseLocation">Optional warehouse filter</param>
    /// <returns>KPI metrics</returns>
    [HttpGet("kpis")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetInventoryKPIs([FromQuery] string warehouseLocation = "")
    {
        try
        {
            var kpis = await _inventoryService.GetInventoryKPIsAsync(warehouseLocation);
            return SuccessResponse(kpis, "Inventory KPIs calculated successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetInventoryKPIs));
        }
    }

    /// <summary>
    /// Calculate overall inventory health score
    /// </summary>
    /// <returns>Health score with recommendations</returns>
    [HttpGet("health-score")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetInventoryHealthScore()
    {
        try
        {
            var healthScore = await _inventoryService.CalculateInventoryHealthScoreAsync();
            return SuccessResponse(healthScore, "Inventory health score calculated successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetInventoryHealthScore));
        }
    }

    #endregion

    #region Warehouse Operations

    /// <summary>
    /// Get inventory summary for a specific warehouse
    /// </summary>
    /// <param name="warehouseLocation">Warehouse location code</param>
    /// <returns>Warehouse inventory summary</returns>
    [HttpGet("warehouse/{warehouseLocation}")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> GetWarehouseInventory(string warehouseLocation)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(warehouseLocation))
                return ErrorResponse("Warehouse location is required", 400);

            var inventory = await _inventoryService.GetWarehouseInventoryAsync(warehouseLocation);
            return SuccessResponse(inventory, "Warehouse inventory retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetWarehouseInventory));
        }
    }

    #endregion

    #region Serial Number & Asset Tracking

    /// <summary>
    /// Assign serial number to a product
    /// </summary>
    /// <param name="request">Serial number assignment details</param>
    /// <returns>Success status</returns>
    [HttpPost("serial-numbers/assign")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> AssignSerialNumber([FromBody] AssignSerialNumberRequest request)
    {
        try
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;

            var result = await _inventoryService.AssignSerialNumberAsync(
                request.ProductId, request.SerialNumber, request.BatchNumber);

            if (!result)
                return ErrorResponse("Failed to assign serial number", 400);

            return SuccessResponse(true, "Serial number assigned successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(AssignSerialNumber));
        }
    }

    /// <summary>
    /// Get serial numbers for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="onlyAvailable">Filter to available serial numbers only</param>
    /// <returns>List of serial numbers</returns>
    [HttpGet("serial-numbers/product/{productId}")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> GetSerialNumbers(int productId, [FromQuery] bool onlyAvailable = true)
    {
        try
        {
            if (productId <= 0)
                return ErrorResponse("Invalid product ID", 400);

            var serialNumbers = await _inventoryService.GetSerialNumbersAsync(productId, onlyAvailable);
            return SuccessResponse(serialNumbers, "Serial numbers retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetSerialNumbers));
        }
    }

    /// <summary>
    /// Reserve a specific serial number for an order
    /// </summary>
    /// <param name="request">Reservation details</param>
    /// <returns>Success status</returns>
    [HttpPost("serial-numbers/reserve")]
    [Authorize(Roles = "Admin,Manager,Warehouse,Sales")]
    public async Task<IActionResult> ReserveSerialNumber([FromBody] ReserveSerialNumberRequest request)
    {
        try
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;

            var result = await _inventoryService.ReserveSerialNumberAsync(
                request.SerialNumber, request.OrderReference);

            if (!result)
                return ErrorResponse("Failed to reserve serial number", 400);

            return SuccessResponse(true, "Serial number reserved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(ReserveSerialNumber));
        }
    }

    /// <summary>
    /// Find product by serial number
    /// </summary>
    /// <param name="serialNumber">Serial number to search</param>
    /// <returns>Serial number details with product info</returns>
    [HttpGet("serial-numbers/{serialNumber}")]
    [Authorize(Roles = "Admin,Manager,Warehouse,Sales")]
    public async Task<IActionResult> GetProductBySerialNumber(string serialNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(serialNumber))
                return ErrorResponse("Serial number is required", 400);

            var serialNumberInfo = await _inventoryService.GetProductBySerialNumberAsync(serialNumber);
            if (serialNumberInfo == null)
                return ErrorResponse("Serial number not found", 404);

            return SuccessResponse(serialNumberInfo, "Product found by serial number");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetProductBySerialNumber));
        }
    }

    #endregion

    #region Barcode & SKU Management

    /// <summary>
    /// Get inventory by barcode
    /// </summary>
    /// <param name="barcode">Product barcode</param>
    /// <returns>Inventory details</returns>
    [HttpGet("barcode/{barcode}")]
    [Authorize(Roles = "Admin,Manager,Warehouse,Sales")]
    public async Task<IActionResult> GetInventoryByBarcode(string barcode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return ErrorResponse("Barcode is required", 400);

            var inventory = await _inventoryService.GetInventoryByBarcodeAsync(barcode);
            if (inventory == null)
                return ErrorResponse("Inventory not found for this barcode", 404);

            return SuccessResponse(inventory, "Inventory retrieved by barcode");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetInventoryByBarcode));
        }
    }

    /// <summary>
    /// Get inventory by SKU
    /// </summary>
    /// <param name="sku">Product SKU</param>
    /// <returns>Inventory details</returns>
    [HttpGet("sku/{sku}")]
    [Authorize(Roles = "Admin,Manager,Warehouse,Sales")]
    public async Task<IActionResult> GetInventoryBySKU(string sku)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sku))
                return ErrorResponse("SKU is required", 400);

            var inventory = await _inventoryService.GetInventoryBySKUAsync(sku);
            if (inventory == null)
                return ErrorResponse("Inventory not found for this SKU", 404);

            return SuccessResponse(inventory, "Inventory retrieved by SKU");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetInventoryBySKU));
        }
    }

    /// <summary>
    /// Generate barcode for a product
    /// </summary>
    /// <param name="request">Barcode generation details</param>
    /// <returns>Success status</returns>
    [HttpPost("barcode/generate")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GenerateBarcode([FromBody] GenerateBarcodeRequest request)
    {
        try
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;

            var result = await _inventoryService.GenerateBarcodeAsync(request.ProductId, request.Format);
            if (!result)
                return ErrorResponse("Failed to generate barcode", 400);

            return SuccessResponse(true, "Barcode generated successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GenerateBarcode));
        }
    }

    /// <summary>
    /// Scan multiple barcodes and return inventory details
    /// </summary>
    /// <param name="barcodes">List of barcodes to scan</param>
    /// <returns>List of inventory details</returns>
    [HttpPost("barcode/scan-multiple")]
    [Authorize(Roles = "Admin,Manager,Warehouse,Sales")]
    public async Task<IActionResult> ScanMultipleBarcodes([FromBody] List<string> barcodes)
    {
        try
        {
            if (barcodes == null || !barcodes.Any())
                return ErrorResponse("No barcodes provided", 400);

            var inventories = await _inventoryService.ScanMultipleBarcodesAsync(barcodes);
            return SuccessResponse(inventories, "Barcodes scanned successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(ScanMultipleBarcodes));
        }
    }

    #endregion

    #region Cycle Counting & Physical Inventory

    /// <summary>
    /// Create a new cycle count schedule
    /// </summary>
    /// <param name="request">Cycle count parameters</param>
    /// <returns>Created cycle count details</returns>
    [HttpPost("cycle-counts")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> CreateCycleCount([FromBody] CycleCountRequest request)
    {
        try
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;

            var cycleCount = await _inventoryService.CreateCycleCountAsync(request);
            return SuccessResponse(cycleCount, "Cycle count created successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(CreateCycleCount));
        }
    }

    /// <summary>
    /// Record counted quantities for cycle count
    /// </summary>
    /// <param name="cycleCountId">Cycle count ID</param>
    /// <param name="countedItems">List of counted items with quantities</param>
    /// <returns>Success status</returns>
    [HttpPost("cycle-counts/{cycleCountId}/record")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> RecordCycleCount(int cycleCountId, [FromBody] List<CountedItemDto> countedItems)
    {
        try
        {
            if (cycleCountId <= 0)
                return ErrorResponse("Invalid cycle count ID", 400);

            if (countedItems == null || !countedItems.Any())
                return ErrorResponse("No counted items provided", 400);

            var result = await _inventoryService.RecordCycleCountAsync(cycleCountId, countedItems);
            if (!result)
                return ErrorResponse("Failed to record cycle count", 400);

            return SuccessResponse(true, "Cycle count recorded successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(RecordCycleCount));
        }
    }

    /// <summary>
    /// Get inventory discrepancies from cycle count
    /// </summary>
    /// <param name="cycleCountId">Cycle count ID</param>
    /// <returns>List of discrepancies</returns>
    [HttpGet("cycle-counts/{cycleCountId}/discrepancies")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> GetInventoryDiscrepancies(int cycleCountId)
    {
        try
        {
            if (cycleCountId <= 0)
                return ErrorResponse("Invalid cycle count ID", 400);

            var discrepancies = await _inventoryService.GetInventoryDiscrepanciesAsync(cycleCountId);
            return SuccessResponse(discrepancies, "Inventory discrepancies retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetInventoryDiscrepancies));
        }
    }

    /// <summary>
    /// Adjust inventory based on cycle count results
    /// </summary>
    /// <param name="cycleCountId">Cycle count ID</param>
    /// <param name="autoApprove">Auto-approve adjustments if true</param>
    /// <returns>Success status</returns>
    [HttpPost("cycle-counts/{cycleCountId}/adjust")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> AdjustInventoryFromCycleCount(int cycleCountId, [FromQuery] bool autoApprove = false)
    {
        try
        {
            if (cycleCountId <= 0)
                return ErrorResponse("Invalid cycle count ID", 400);

            var result = await _inventoryService.AdjustInventoryFromCycleCountAsync(cycleCountId, autoApprove);
            if (!result)
                return ErrorResponse("Failed to adjust inventory from cycle count", 400);

            return SuccessResponse(true, "Inventory adjusted from cycle count successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(AdjustInventoryFromCycleCount));
        }
    }

    #endregion

    #region Purchase Order Integration

    /// <summary>
    /// Get pending purchase orders requiring inventory receipt
    /// </summary>
    /// <returns>List of pending purchase orders</returns>
    [HttpGet("purchase-orders/pending")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> GetPendingPurchaseOrders()
    {
        try
        {
            var purchaseOrders = await _inventoryService.GetPendingPurchaseOrdersAsync();
            return SuccessResponse(purchaseOrders, "Pending purchase orders retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetPendingPurchaseOrders));
        }
    }

    /// <summary>
    /// Receive inventory from purchase order
    /// </summary>
    /// <param name="purchaseOrderId">Purchase order ID</param>
    /// <param name="receivedItems">List of received items with quantities</param>
    /// <returns>Success status</returns>
    [HttpPost("purchase-orders/{purchaseOrderId}/receive")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> ReceiveInventoryFromPO(int purchaseOrderId, [FromBody] List<ReceivedItemDto> receivedItems)
    {
        try
        {
            if (purchaseOrderId <= 0)
                return ErrorResponse("Invalid purchase order ID", 400);

            if (receivedItems == null || !receivedItems.Any())
                return ErrorResponse("No received items provided", 400);

            var result = await _inventoryService.ReceiveInventoryFromPOAsync(purchaseOrderId, receivedItems);
            if (!result)
                return ErrorResponse("Failed to receive inventory from purchase order", 400);

            return SuccessResponse(true, "Inventory received from purchase order successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(ReceiveInventoryFromPO));
        }
    }

    /// <summary>
    /// Create automatic purchase orders from reorder suggestions
    /// </summary>
    /// <param name="reorderSuggestions">List of items to reorder</param>
    /// <returns>Success status</returns>
    [HttpPost("purchase-orders/auto-create")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateAutomaticPurchaseOrder([FromBody] List<ReorderSuggestionDto> reorderSuggestions)
    {
        try
        {
            if (reorderSuggestions == null || !reorderSuggestions.Any())
                return ErrorResponse("No reorder suggestions provided", 400);

            var result = await _inventoryService.CreateAutomaticPurchaseOrderAsync(reorderSuggestions);
            if (!result)
                return ErrorResponse("Failed to create automatic purchase orders", 400);

            return SuccessResponse(true, "Automatic purchase orders created successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(CreateAutomaticPurchaseOrder));
        }
    }

    #endregion

    #region Damage & Return Handling

    /// <summary>
    /// Record damaged inventory
    /// </summary>
    /// <param name="request">Damage report details</param>
    /// <returns>Success status</returns>
    [HttpPost("damage/record")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> RecordDamagedInventory([FromBody] DamageReportRequest request)
    {
        try
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;

            var result = await _inventoryService.RecordDamagedInventoryAsync(request);
            if (!result)
                return ErrorResponse("Failed to record damaged inventory", 400);

            return SuccessResponse(true, "Damaged inventory recorded successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(RecordDamagedInventory));
        }
    }

    /// <summary>
    /// Process customer return and update inventory
    /// </summary>
    /// <param name="request">Return details</param>
    /// <returns>Success status</returns>
    [HttpPost("returns/process")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> ProcessCustomerReturn([FromBody] CustomerReturnRequest request)
    {
        try
        {
            var validation = ValidateModelState();
            if (validation != null) return validation;

            var result = await _inventoryService.ProcessCustomerReturnAsync(request);
            if (!result)
                return ErrorResponse("Failed to process customer return", 400);

            return SuccessResponse(true, "Customer return processed successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(ProcessCustomerReturn));
        }
    }

    /// <summary>
    /// Get damaged inventory items
    /// </summary>
    /// <param name="warehouseLocation">Optional warehouse filter</param>
    /// <returns>List of damaged inventory items</returns>
    [HttpGet("damage")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> GetDamagedInventory([FromQuery] string warehouseLocation = "")
    {
        try
        {
            var damagedItems = await _inventoryService.GetDamagedInventoryAsync(warehouseLocation);
            return SuccessResponse(damagedItems, "Damaged inventory retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetDamagedInventory));
        }
    }

    /// <summary>
    /// Dispose of damaged inventory
    /// </summary>
    /// <param name="damageId">Damage record ID</param>
    /// <param name="request">Disposal details</param>
    /// <returns>Success status</returns>
    [HttpPost("damage/{damageId}/dispose")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> DisposeDamagedInventory(int damageId, [FromBody] DisposalRequest request)
    {
        try
        {
            if (damageId <= 0)
                return ErrorResponse("Invalid damage ID", 400);

            var validation = ValidateModelState();
            if (validation != null) return validation;

            var result = await _inventoryService.DisposeDamagedInventoryAsync(
                damageId, request.DisposalMethod, request.Notes);

            if (!result)
                return ErrorResponse("Failed to dispose damaged inventory", 400);

            return SuccessResponse(true, "Damaged inventory disposed successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(DisposeDamagedInventory));
        }
    }

    #endregion

    #region Forecasting & Demand Planning

    /// <summary>
    /// Generate demand forecast for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="forecastDays">Number of days to forecast</param>
    /// <returns>Demand forecast data</returns>
    [HttpGet("forecast/demand/{productId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GenerateDemandForecast(int productId, [FromQuery] int forecastDays = 90)
    {
        try
        {
            if (productId <= 0)
                return ErrorResponse("Invalid product ID", 400);

            if (forecastDays <= 0 || forecastDays > 365)
                return ErrorResponse("Forecast days must be between 1 and 365", 400);

            var forecast = await _inventoryService.GenerateDemandForecastAsync(productId, forecastDays);
            return SuccessResponse(forecast, "Demand forecast generated successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GenerateDemandForecast));
        }
    }

    /// <summary>
    /// Get slow-moving inventory items
    /// </summary>
    /// <param name="daysSinceLastSale">Days since last sale threshold</param>
    /// <returns>List of slow-moving items</returns>
    [HttpGet("analysis/slow-moving")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetSlowMovingInventory([FromQuery] int daysSinceLastSale = 90)
    {
        try
        {
            if (daysSinceLastSale <= 0)
                return ErrorResponse("Days since last sale must be positive", 400);

            var slowMovingItems = await _inventoryService.GetSlowMovingInventoryAsync(daysSinceLastSale);
            return SuccessResponse(slowMovingItems, "Slow-moving inventory retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetSlowMovingInventory));
        }
    }

    /// <summary>
    /// Get fast-moving inventory items
    /// </summary>
    /// <param name="daysPeriod">Analysis period in days</param>
    /// <returns>List of fast-moving items</returns>
    [HttpGet("analysis/fast-moving")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> GetFastMovingInventory([FromQuery] int daysPeriod = 30)
    {
        try
        {
            if (daysPeriod <= 0)
                return ErrorResponse("Analysis period must be positive", 400);

            var fastMovingItems = await _inventoryService.GetFastMovingInventoryAsync(daysPeriod);
            return SuccessResponse(fastMovingItems, "Fast-moving inventory retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetFastMovingInventory));
        }
    }

    /// <summary>
    /// Update forecasting parameters for a product
    /// </summary>
    /// <param name="productId">Product ID</param>
    /// <param name="parameters">Forecasting parameters</param>
    /// <returns>Success status</returns>
    [HttpPut("forecast/parameters/{productId}")]
    [Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> UpdateForecastParameters(int productId, [FromBody] ForecastParametersDto parameters)
    {
        try
        {
            if (productId <= 0)
                return ErrorResponse("Invalid product ID", 400);

            var validation = ValidateModelState();
            if (validation != null) return validation;

            var result = await _inventoryService.UpdateForecastParametersAsync(productId, parameters);
            if (!result)
                return ErrorResponse("Failed to update forecast parameters", 400);

            return SuccessResponse(true, "Forecast parameters updated successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(UpdateForecastParameters));
        }
    }

    #endregion

    #region Expiry & Warranty Tracking

    /// <summary>
    /// Get products with expiring warranties
    /// </summary>
    /// <param name="daysAhead">Days ahead to check for expiry</param>
    /// <returns>List of products with expiring warranties</returns>
    [HttpGet("warranties/expiring")]
    [Authorize(Roles = "Admin,Manager,Sales")]
    public async Task<IActionResult> GetExpiringWarranties([FromQuery] int daysAhead = 30)
    {
        try
        {
            if (daysAhead <= 0)
                return ErrorResponse("Days ahead must be positive", 400);

            var expiringWarranties = await _inventoryService.GetExpiringWarrantiesAsync(daysAhead);
            return SuccessResponse(expiringWarranties, "Expiring warranties retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetExpiringWarranties));
        }
    }

    /// <summary>
    /// Update product expiry date
    /// </summary>
    /// <param name="inventoryId">Inventory ID</param>
    /// <param name="request">Expiry date update request</param>
    /// <returns>Success status</returns>
    [HttpPut("expiry/{inventoryId}")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> UpdateProductExpiryDate(int inventoryId, [FromBody] UpdateExpiryDateRequest request)
    {
        try
        {
            if (inventoryId <= 0)
                return ErrorResponse("Invalid inventory ID", 400);

            var validation = ValidateModelState();
            if (validation != null) return validation;

            var result = await _inventoryService.UpdateProductExpiryDateAsync(inventoryId, request.ExpiryDate);
            if (!result)
                return ErrorResponse("Failed to update expiry date", 400);

            return SuccessResponse(true, "Expiry date updated successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(UpdateProductExpiryDate));
        }
    }

    /// <summary>
    /// Get expired inventory items
    /// </summary>
    /// <param name="warehouseLocation">Optional warehouse filter</param>
    /// <returns>List of expired inventory items</returns>
    [HttpGet("expired")]
    [Authorize(Roles = "Admin,Manager,Warehouse")]
    public async Task<IActionResult> GetExpiredInventory([FromQuery] string warehouseLocation = "")
    {
        try
        {
            var expiredItems = await _inventoryService.GetExpiredInventoryAsync(warehouseLocation);
            return SuccessResponse(expiredItems, "Expired inventory retrieved successfully");
        }
        catch (Exception ex)
        {
            return HandleException(ex, nameof(GetExpiredInventory));
        }
    }

    #endregion
}

#region Request DTOs for Controller Actions

/// <summary>
/// Request DTO for assigning serial numbers
/// </summary>
public class AssignSerialNumberRequest
{
    [Required]
    public int ProductId { get; set; }

    [Required]
    [StringLength(100)]
    public string SerialNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string BatchNumber { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for reserving serial numbers
/// </summary>
public class ReserveSerialNumberRequest
{
    [Required]
    [StringLength(100)]
    public string SerialNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string OrderReference { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for generating barcodes
/// </summary>
public class GenerateBarcodeRequest
{
    [Required]
    public int ProductId { get; set; }

    [StringLength(20)]
    public string Format { get; set; } = "EAN13";
}

/// <summary>
/// Request DTO for disposing damaged inventory
/// </summary>
public class DisposalRequest
{
    [Required]
    [StringLength(100)]
    public string DisposalMethod { get; set; } = string.Empty;

    [StringLength(500)]
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Request DTO for updating expiry dates
/// </summary>
public class UpdateExpiryDateRequest
{
    [Required]
    public DateTime ExpiryDate { get; set; }
}

#endregion