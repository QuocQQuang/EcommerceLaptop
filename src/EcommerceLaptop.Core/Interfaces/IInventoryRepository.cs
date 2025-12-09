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
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}
