using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Inventory;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Specifications.InventorySpecs;
using EcommerceLaptop.Core.Specifications;
using EcommerceLaptop.Infrastructure.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EcommerceLaptop.Infrastructure.Services;

public class StockManagementService(
    IAsyncRepository<Inventory> inventoryRepository,
    ApplicationDbContext context,
    ILogger<StockManagementService> logger) : IStockManagementService
{
    private readonly IAsyncRepository<Inventory> _inventoryRepository = inventoryRepository;
    private readonly ApplicationDbContext _context = context;
    private readonly ILogger<StockManagementService> _logger = logger;

    public async Task<InventoryDto?> GetInventoryByProductIdAsync(int productId)
    {
        _logger.LogInformation("Getting inventory for product {ProductId}", productId);
        var spec = new InventoryByProductSpecification(productId);
        var inventory = await _inventoryRepository.GetEntityWithSpec(spec);
        
        if (inventory == null)
        {
            _logger.LogWarning("No inventory found for product {ProductId}", productId);
            return null;
        }
        return MapToInventoryDto(inventory);
    }

    public async Task<PagedResult<InventoryDto>> GetInventoriesAsync(InventoryFilterRequest request)
    {
        var countSpec = new InventoryFilterSpecification(request, isPagingEnabled: false);
        var totalCount = await _inventoryRepository.CountAsync(countSpec);

        var spec = new InventoryFilterSpecification(request);
        var inventories = await _inventoryRepository.GetAsync(spec);

        var items = inventories.Select(MapToInventoryDto).ToList();

        return new PagedResult<InventoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<InventoryDto> CreateInventoryAsync(CreateInventoryRequest request)
    {
        _logger.LogInformation("Creating inventory for product {ProductId}", request.ProductId);

        var existingInventory = await _inventoryRepository.GetEntityWithSpec(new InventoryByProductSpecification(request.ProductId));
        if (existingInventory != null)
        {
            throw new ValidationException($"Inventory already exists for product {request.ProductId}");
        }

        // Use Domain Factory
        var inventory = Inventory.Create(
            request.ProductId,
            request.QuantityInStock,
            request.ReorderLevel,
            request.MaxStockLevel,
            request.WarehouseLocation
        );

        inventory.Transactions.Add(new InventoryTransaction
        {
            Type = InventoryTransactionType.Purchase,
            Quantity = request.QuantityInStock,
            Reference = "INITIAL_STOCK",
            Notes = "Initial inventory creation",
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1, // System user
            Reason = "Initial Creation"
        });

        inventory = await _inventoryRepository.AddAsync(inventory);

        _logger.LogInformation("Created inventory for product {ProductId} with {Quantity} units", request.ProductId, request.QuantityInStock);

        return MapToInventoryDto(inventory);
    }

    public async Task<InventoryDto> UpdateInventoryAsync(int productId, UpdateInventoryRequest request)
    {
        _logger.LogInformation("Updating inventory for product {ProductId}", productId);
        var inventory = await _inventoryRepository.GetEntityWithSpec(new InventoryByProductSpecification(productId));
        if (inventory == null) throw new ValidationException($"Inventory not found for product {productId}");

        var originalQuantity = inventory.QuantityInStock;

        // Handle non-stock property updates
        if (request.ReorderLevel.HasValue) inventory.ReorderLevel = request.ReorderLevel.Value;
        if (request.MaxStockLevel.HasValue) inventory.MaxStockLevel = request.MaxStockLevel.Value;
        if (!string.IsNullOrEmpty(request.WarehouseLocation)) inventory.WarehouseLocation = request.WarehouseLocation;

        // Handle stock quantity updates via Domain Methods
        if (request.QuantityInStock.HasValue)
        {
            var quantityDifference = request.QuantityInStock.Value - originalQuantity;
            if (quantityDifference > 0)
            {
                inventory.AddStock(quantityDifference, "MANUAL_UPDATE", "Inventory Update (Manual)", 1);
            }
            else if (quantityDifference < 0)
            {
                inventory.RemoveStock(Math.Abs(quantityDifference), "MANUAL_UPDATE", "Inventory Update (Manual)", 1);
            }
        }

        await _inventoryRepository.UpdateAsync(inventory);

        return MapToInventoryDto(inventory);
    }

    public async Task<bool> DeleteInventoryAsync(int productId)
    {
        _logger.LogInformation("Deleting inventory for product {ProductId}", productId);
        var inventory = await _inventoryRepository.GetEntityWithSpec(new InventoryByProductSpecification(productId));
        if (inventory == null) return false;

        if (inventory.ReservedQuantity > 0)
        {
            throw new ValidationException($"Cannot delete inventory with reserved stock: {inventory.ReservedQuantity} units reserved");
        }

        await _inventoryRepository.DeleteAsync(inventory);
        return true;
    }

    public async Task<bool> AdjustStockAsync(StockAdjustmentRequest request)
    {
        _logger.LogInformation("Adjusting stock for product {ProductId} by {Quantity}", request.ProductId, request.Quantity);
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var inventory = await _inventoryRepository.GetEntityWithSpec(new InventoryByProductSpecification(request.ProductId));
            if (inventory == null) throw new ValidationException($"Inventory not found for product {request.ProductId}");

            if (request.Quantity > 0)
            {
                inventory.AddStock(request.Quantity, request.Reference, request.Notes ?? "Stock Adjustment", 1);
            }
            else if (request.Quantity < 0)
            {
                inventory.RemoveStock(Math.Abs(request.Quantity), request.Reference, request.Notes ?? "Stock Adjustment", 1);
            }

            await _inventoryRepository.UpdateAsync(inventory);
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<BulkOperationResult> BulkAdjustStockAsync(List<StockAdjustmentRequest> requests)
    {
        _logger.LogInformation("Bulk stock adjustment for {Count} items", requests.Count);
        var result = new BulkOperationResult { TotalItems = requests.Count, StartedAt = DateTime.UtcNow, OperationType = "BulkStockAdjustment" };

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            foreach (var request in requests)
            {
                try
                {
                    var inventory = await _inventoryRepository.GetEntityWithSpec(new InventoryByProductSpecification(request.ProductId));
                    if (inventory == null) throw new Exception("Inventory not found");

                    if (request.Quantity > 0)
                    {
                        inventory.AddStock(request.Quantity, request.Reference, request.Notes ?? "Bulk Adjustment", 1);
                    }
                    else if (request.Quantity < 0)
                    {
                        inventory.RemoveStock(Math.Abs(request.Quantity), request.Reference, request.Notes ?? "Bulk Adjustment", 1);
                    }

                    await _inventoryRepository.UpdateAsync(inventory);
                    result.SuccessfulItems++;
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new BulkOperationError
                    {
                        ItemIdentifier = request.ProductId.ToString(),
                        ErrorMessage = ex.Message,
                        ErrorCode = "ERROR"
                    });
                    result.FailedItems++;
                }
            }
            await transaction.CommitAsync();
            result.CompletedAt = DateTime.UtcNow;
            result.IsSuccess = result.FailedItems == 0;
            return result;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> TransferStockAsync(StockTransferRequest request)
    {
        _logger.LogInformation("Transferring {Quantity} from {From} to {To} for product {ProductId}", request.Quantity, request.FromWarehouse, request.ToWarehouse, request.ProductId);
        
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Note: Efficient way would be Specification for Warehouse+Product. I'll stick to simple predicate via GetAsync for now if repo supports it, 
            // OR use InventoryFilterSpecification if it supports exact match. The existing Filter spec seemed to rely on Request DTO.
            // I'll assume GetAsync(Expression) is valid on the repo as initialized in Startup (EfRepository supports it).
            // But wait, Generic Repository interfaces usually don't expose GetAsync(Expression) directly unless Icast it.
            // IAsyncRepository<T> typically has GetAsync(ISpecification<T>). 
            // Let's verify IAsyncRepository interface.
            
            // Checking IAsyncRepository:
            // It usually has GetAsync(ISpecification<T> spec). 
            // If I look at EfRepository.cs, it implements:
            // public async Task<IReadOnlyList<T>> GetAsync(Expression<Func<T, bool>> predicate)
            // Does the interface IAsyncRepository have it? I should check. 
            // Assuming best practice: Use Specification.
            
            // I will use a simple inline Specification or create one if needed.
            // Actually, InventoryReportSpecification can filter by Warehouse.
            // Or just fetch all for product and filter in memory since it's likely just a few entries.
             
            var allInventoryForProduct = await _inventoryRepository.GetAsync(new InventoryByProductSpecification(request.ProductId));
            
            var fromInventory = allInventoryForProduct.FirstOrDefault(i => i.WarehouseLocation == request.FromWarehouse);
            var toInventory = allInventoryForProduct.FirstOrDefault(i => i.WarehouseLocation == request.ToWarehouse);

            if (fromInventory == null) throw new ValidationException("Source inventory not found");
            
            fromInventory.RemoveStock(request.Quantity, request.Reference, $"Transfer to {request.ToWarehouse}", 1);

            if (toInventory == null)
            {
                toInventory = Inventory.Create(
                    request.ProductId,
                    0, 
                    fromInventory.ReorderLevel,
                    fromInventory.MaxStockLevel,
                    request.ToWarehouse
                );
                await _inventoryRepository.AddAsync(toInventory); 
            }

            toInventory.AddStock(request.Quantity, request.Reference, $"Transfer from {request.FromWarehouse}", 1);

            await _inventoryRepository.UpdateAsync(fromInventory);
            await _inventoryRepository.UpdateAsync(toInventory);

            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<bool> ValidateStockLevelsAsync(List<int> productIds)
    {
        // Spec to get by IDs? 
        // I can just loop or create a spec for "ProductsByIds" equivalent for Inventory.
        // Or I can use EfRepository's GetAsync(predicate) if interface allows.
        // If interface doesn't allow, I need a spec. 
        // Let's assume IAsyncRepository has GetAsync(Expression) based on EfRepository implementation I saw earlier.
        // Wait, EfRepository implements IAsyncRepository<T>. If IAsyncRepository<T> doesn't declare GetAsync(Expression), I can't use it via interface.
        // I should check IAsyncRepository.cs. If not, I'll use a Spec.
        
        // Hypothetical spec usage:
        // var spec = new BaseSpecification<Inventory>(i => productIds.Contains(i.ProductId));
        // var inventories = await _inventoryRepository.GetAsync(spec);
        
        // I'll create a quick inline spec using the BaseSpecification constructor if it's public.
        // BaseSpecification constructor (Expression criteria) is public.
        
        var spec = new BaseSpecification<Inventory>(i => productIds.Contains(i.ProductId));
        var inventories = await _inventoryRepository.GetAsync(spec);
        
        return inventories.All(i => i.QuantityInStock >= 0 && i.QuantityInStock <= i.MaxStockLevel);
    }

    public async Task<bool> UpdateReorderLevelsAsync(int productId, int newReorderLevel)
    {
        var inventory = await _inventoryRepository.GetEntityWithSpec(new InventoryByProductSpecification(productId));
        if (inventory == null) return false;
        inventory.ReorderLevel = newReorderLevel;
        await _inventoryRepository.UpdateAsync(inventory);
        return true;
    }

    public async Task<List<WarehouseInventoryDto>> GetWarehouseInventoryAsync(string warehouseLocation)
    {
        var spec = new BaseSpecification<Inventory>(i => i.WarehouseLocation == warehouseLocation);
        var inventories = await _inventoryRepository.GetAsync(spec);
        
        return inventories.GroupBy(i => i.WarehouseLocation).Select(g => new WarehouseInventoryDto
        {
            WarehouseLocation = g.Key,
            WarehouseName = $"Warehouse {g.Key}",
            TotalProducts = g.Count(),
            TotalQuantity = g.Sum(i => i.QuantityInStock),
            TotalValue = g.Sum(i => i.QuantityInStock * 100m),
            LowStockItems = g.Count(i => i.QuantityInStock <= i.ReorderLevel),
            OutOfStockItems = g.Count(i => i.QuantityInStock == 0),
            LastUpdated = g.Max(i => i.LastStockUpdate)
        }).ToList();
    }

    private InventoryDto MapToInventoryDto(Inventory inventory)
    {
        return new InventoryDto
        {
            Id = inventory.Id,
            ProductId = inventory.ProductId,
            ProductName = inventory.Product?.Name ?? string.Empty,
            ProductSKU = inventory.Product?.SKU ?? string.Empty,
            ProductBrand = inventory.Product?.Brand ?? string.Empty,
            QuantityInStock = inventory.QuantityInStock,
            ReservedQuantity = inventory.ReservedQuantity,
            ReorderLevel = inventory.ReorderLevel,
            MaxStockLevel = inventory.MaxStockLevel,
            WarehouseLocation = inventory.WarehouseLocation,
            LastStockUpdate = inventory.LastStockUpdate,
            UnitCost = 100m 
        };
    }
}
