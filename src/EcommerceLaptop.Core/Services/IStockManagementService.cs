using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Inventory;

namespace EcommerceLaptop.Core.Services;

public interface IStockManagementService
{
    // CRUD
    Task<InventoryDto?> GetInventoryByProductIdAsync(int productId);
    Task<PagedResult<InventoryDto>> GetInventoriesAsync(InventoryFilterRequest request);
    Task<InventoryDto> CreateInventoryAsync(CreateInventoryRequest request);
    Task<InventoryDto> UpdateInventoryAsync(int productId, UpdateInventoryRequest request);
    Task<bool> DeleteInventoryAsync(int productId);

    // Stock Operations
    Task<bool> AdjustStockAsync(StockAdjustmentRequest request);
    Task<BulkOperationResult> BulkAdjustStockAsync(List<StockAdjustmentRequest> requests);
    Task<bool> TransferStockAsync(StockTransferRequest request);
    Task<bool> ValidateStockLevelsAsync(List<int> productIds);
    Task<bool> UpdateReorderLevelsAsync(int productId, int newReorderLevel);
    
    // Warehouse
    Task<List<WarehouseInventoryDto>> GetWarehouseInventoryAsync(string warehouseLocation);
}
