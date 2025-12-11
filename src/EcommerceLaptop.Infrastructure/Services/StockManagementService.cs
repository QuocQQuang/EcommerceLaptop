using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Inventory;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Infrastructure.Services;

public class StockManagementService : IStockManagementService
{
    private readonly IInventoryRepository _repository;
    private readonly ILogger<StockManagementService> _logger;

    public StockManagementService(IInventoryRepository repository, ILogger<StockManagementService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<InventoryDto?> GetInventoryByProductIdAsync(int productId)
    {
        _logger.LogInformation("Getting inventory for product {ProductId}", productId);
        var inventory = await _repository.GetByProductIdAsync(productId);
        if (inventory == null)
        {
            _logger.LogWarning("No inventory found for product {ProductId}", productId);
            return null;
        }
        return MapToInventoryDto(inventory);
    }

    public async Task<PagedResult<InventoryDto>> GetInventoriesAsync(InventoryFilterRequest request)
    {
        return await _repository.GetInventoriesAsync(request);
    }

    public async Task<InventoryDto> CreateInventoryAsync(CreateInventoryRequest request)
    {
        _logger.LogInformation("Creating inventory for product {ProductId}", request.ProductId);

        var existingInventory = await _repository.GetByProductIdAsync(request.ProductId);
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

        // Add initial transaction logic if needed, or let the factory/add method handle it. 
        // Since Factory just creates object, we might want to log initial stock as a transaction?
        // The original logic did. Let's replicate this "Initial Stock" transaction logic by calling AddStock? 
        // BUT Inventory.Create sets the initial stock directly.
        // We can manually add the transaction to the entity's list for initial creation to keep it consistent.
        
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

        inventory = await _repository.AddAsync(inventory);
        // Repository AddAsync should save the graph including the transaction.

        _logger.LogInformation("Created inventory for product {ProductId} with {Quantity} units", request.ProductId, request.QuantityInStock);

        return MapToInventoryDto(inventory);
    }

    public async Task<InventoryDto> UpdateInventoryAsync(int productId, UpdateInventoryRequest request)
    {
        _logger.LogInformation("Updating inventory for product {ProductId}", productId);
        var inventory = await _repository.GetByProductIdAsync(productId);
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

        await _repository.UpdateAsync(inventory);

        return MapToInventoryDto(inventory);
    }

    public async Task<bool> DeleteInventoryAsync(int productId)
    {
        _logger.LogInformation("Deleting inventory for product {ProductId}", productId);
        var inventory = await _repository.GetByProductIdAsync(productId);
        if (inventory == null) return false;

        if (inventory.ReservedQuantity > 0)
        {
            throw new ValidationException($"Cannot delete inventory with reserved stock: {inventory.ReservedQuantity} units reserved");
        }

        await _repository.DeleteAsync(inventory);
        return true;
    }

    public async Task<bool> AdjustStockAsync(StockAdjustmentRequest request)
    {
        _logger.LogInformation("Adjusting stock for product {ProductId} by {Quantity}", request.ProductId, request.Quantity);
        
        // Note: Transaction logic depends on DB provider support. Assuming repository handles it or we rely on SaveChanges.
        await _repository.BeginTransactionAsync();
        try
        {
            var inventory = await _repository.GetByProductIdAsync(request.ProductId);
            if (inventory == null) throw new ValidationException($"Inventory not found for product {request.ProductId}");

            if (request.Quantity > 0)
            {
                inventory.AddStock(request.Quantity, request.Reference, request.Notes ?? "Stock Adjustment", 1);
            }
            else if (request.Quantity < 0)
            {
                inventory.RemoveStock(Math.Abs(request.Quantity), request.Reference, request.Notes ?? "Stock Adjustment", 1);
            }

            await _repository.UpdateAsync(inventory);
            await _repository.CommitTransactionAsync();
            return true;
        }
        catch
        {
            await _repository.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<BulkOperationResult> BulkAdjustStockAsync(List<StockAdjustmentRequest> requests)
    {
        _logger.LogInformation("Bulk stock adjustment for {Count} items", requests.Count);
        var result = new BulkOperationResult { TotalItems = requests.Count, StartedAt = DateTime.UtcNow, OperationType = "BulkStockAdjustment" };

        await _repository.BeginTransactionAsync();
        try
        {
            foreach (var request in requests)
            {
                try
                {
                    var inventory = await _repository.GetByProductIdAsync(request.ProductId);
                    if (inventory == null) throw new Exception("Inventory not found");

                    if (request.Quantity > 0)
                    {
                        inventory.AddStock(request.Quantity, request.Reference, request.Notes ?? "Bulk Adjustment", 1);
                    }
                    else if (request.Quantity < 0)
                    {
                        inventory.RemoveStock(Math.Abs(request.Quantity), request.Reference, request.Notes ?? "Bulk Adjustment", 1);
                    }

                    await _repository.UpdateAsync(inventory);
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
            await _repository.CommitTransactionAsync();
            result.CompletedAt = DateTime.UtcNow;
            result.IsSuccess = result.FailedItems == 0;
            return result;
        }
        catch
        {
            await _repository.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> TransferStockAsync(StockTransferRequest request)
    {
        _logger.LogInformation("Transferring {Quantity} from {From} to {To} for product {ProductId}", request.Quantity, request.FromWarehouse, request.ToWarehouse, request.ProductId);
        await _repository.BeginTransactionAsync();
        try
        {
            var fromList = await _repository.GetAsync(i => i.ProductId == request.ProductId && i.WarehouseLocation == request.FromWarehouse);
            var fromInventory = fromList.FirstOrDefault();
            
            var toList = await _repository.GetAsync(i => i.ProductId == request.ProductId && i.WarehouseLocation == request.ToWarehouse);
            var toInventory = toList.FirstOrDefault();

            if (fromInventory == null) throw new ValidationException("Source inventory not found");
            
            // Use Domain Methods
            fromInventory.RemoveStock(request.Quantity, request.Reference, $"Transfer to {request.ToWarehouse}", 1);

            if (toInventory == null)
            {
                toInventory = Inventory.Create(
                    request.ProductId,
                    0, // Start with 0 then AddStock
                    fromInventory.ReorderLevel,
                    fromInventory.MaxStockLevel,
                    request.ToWarehouse
                );
                // We need to add it to context first or just add stock? 
                // AddStock works on instance.
                await _repository.AddAsync(toInventory); // Add to context
            }

            toInventory.AddStock(request.Quantity, request.Reference, $"Transfer from {request.FromWarehouse}", 1);

            await _repository.UpdateAsync(fromInventory);
            await _repository.UpdateAsync(toInventory);

            await _repository.CommitTransactionAsync();
            return true;
        }
        catch
        {
            await _repository.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> ValidateStockLevelsAsync(List<int> productIds)
    {
        var inventories = await _repository.GetAsync(i => productIds.Contains(i.ProductId));
        return inventories.All(i => i.QuantityInStock >= 0 && i.QuantityInStock <= i.MaxStockLevel);
    }

    public async Task<bool> UpdateReorderLevelsAsync(int productId, int newReorderLevel)
    {
        var inventory = await _repository.GetByProductIdAsync(productId);
        if (inventory == null) return false;
        inventory.ReorderLevel = newReorderLevel;
        await _repository.UpdateAsync(inventory);
        return true;
    }

    public async Task<List<WarehouseInventoryDto>> GetWarehouseInventoryAsync(string warehouseLocation)
    {
        var inventories = (await _repository.GetAsync(i => i.WarehouseLocation == warehouseLocation)).ToList();
        
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
