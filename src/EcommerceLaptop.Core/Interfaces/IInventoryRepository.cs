using EcommerceLaptop.Core.Entities;
using System.Linq.Expressions;

namespace EcommerceLaptop.Core.Interfaces;

public interface IInventoryRepository : IAsyncRepository<Inventory>
{
    Task<Inventory?> GetByProductIdAsync(int productId);
    Task<IEnumerable<Inventory>> GetLowStockAsync(int thresholdMultiplier = 1);
    Task<EcommerceLaptop.Core.DTOs.PagedResult<EcommerceLaptop.Core.DTOs.Inventory.InventoryDto>> GetInventoriesAsync(EcommerceLaptop.Core.DTOs.Inventory.InventoryFilterRequest request);
    Task<List<Inventory>> GetInventoriesForReportAsync(EcommerceLaptop.Core.DTOs.Inventory.InventoryReportRequest request);
    Task<List<InventoryTransaction>> GetTransactionsAsync(int productId, DateTime? fromDate, DateTime? toDate);
    Task<List<InventoryTransaction>> GetStockMovementAsync(DateTime startDate, DateTime endDate);
    Task<InventoryTransaction> AddTransactionAsync(InventoryTransaction transaction);
    
    // Serial Number & Product Lookup
    Task<List<SerialNumber>> GetSerialNumbersAsync(int productId, bool activeOnly);
    Task<SerialNumber?> GetSerialNumberByValueAsync(string serialNumber);
    Task<bool> SerialNumberExistsAsync(string serialNumber);
    Task AddSerialNumberAsync(SerialNumber serialNumber);
    Task UpdateSerialNumberAsync(SerialNumber serialNumber);
    
    Task<Inventory?> GetInventoryByBarcodeAsync(string barcode);
    Task<Inventory?> GetInventoryBySkuAsync(string sku);
    Task<Product?> GetProductByIdAsync(int productId);
    Task UpdateProductAsync(Product product);

    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
