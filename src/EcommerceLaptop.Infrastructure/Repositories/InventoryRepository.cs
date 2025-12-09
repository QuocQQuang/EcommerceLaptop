using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceLaptop.Infrastructure.Repositories;

public class InventoryRepository : EfRepository<Inventory>, IInventoryRepository
{
    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _transaction;

    public InventoryRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Inventory?> GetByProductIdAsync(int productId)
    {
        return await _context.Inventories
            .Include(i => i.Product)
            .FirstOrDefaultAsync(i => i.ProductId == productId);
    }

    public async Task<IEnumerable<Inventory>> GetLowStockAsync(int thresholdMultiplier = 1)
    {
        return await _context.Inventories
            .Include(i => i.Product)
            .Where(i => i.QuantityInStock <= i.ReorderLevel * thresholdMultiplier)
            .ToListAsync();
    }

    public async Task<EcommerceLaptop.Core.DTOs.PagedResult<EcommerceLaptop.Core.DTOs.Inventory.InventoryDto>> GetInventoriesAsync(EcommerceLaptop.Core.DTOs.Inventory.InventoryFilterRequest request)
    {
        var query = _context.Inventories
            .Include(i => i.Product)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(request.ProductName))
        {
            query = query.Where(i => i.Product.Name.Contains(request.ProductName));
        }

        if (!string.IsNullOrEmpty(request.ProductSKU))
        {
            query = query.Where(i => i.Product.SKU.Contains(request.ProductSKU));
        }

        if (!string.IsNullOrEmpty(request.WarehouseLocation))
        {
            query = query.Where(i => i.WarehouseLocation.Contains(request.WarehouseLocation));
        }

        if (request.IsLowStock.HasValue && request.IsLowStock.Value)
        {
            query = query.Where(i => i.QuantityInStock <= i.ReorderLevel);
        }

        if (request.MinQuantity.HasValue)
        {
            query = query.Where(i => i.QuantityInStock >= request.MinQuantity.Value);
        }

        if (request.MaxQuantity.HasValue)
        {
            query = query.Where(i => i.QuantityInStock <= request.MaxQuantity.Value);
        }

        // Apply sorting
        var isDescending = request.SortDirection?.ToUpper() == "DESC";
        query = request.SortBy?.ToLower() switch
        {
            "productname" => isDescending ? query.OrderByDescending(i => i.Product.Name) : query.OrderBy(i => i.Product.Name),
            "quantity" => isDescending ? query.OrderByDescending(i => i.QuantityInStock) : query.OrderBy(i => i.QuantityInStock),
            "laststockupdate" => isDescending ? query.OrderByDescending(i => i.LastStockUpdate) : query.OrderBy(i => i.LastStockUpdate),
            "warehouse" => isDescending ? query.OrderByDescending(i => i.WarehouseLocation) : query.OrderBy(i => i.WarehouseLocation),
            _ => query.OrderBy(i => i.Product.Name)
        };

        var totalCount = await query.CountAsync();

        var inventories = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var items = inventories.Select(i => new EcommerceLaptop.Core.DTOs.Inventory.InventoryDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? string.Empty,
            ProductSKU = i.Product?.SKU ?? string.Empty,
            ProductBrand = i.Product?.Brand ?? string.Empty,
            QuantityInStock = i.QuantityInStock,
            ReservedQuantity = i.ReservedQuantity,
            ReorderLevel = i.ReorderLevel,
            MaxStockLevel = i.MaxStockLevel,
            WarehouseLocation = i.WarehouseLocation,
            LastStockUpdate = i.LastStockUpdate,
            UnitCost = 100m // Placeholder
        }).ToList();

        return new EcommerceLaptop.Core.DTOs.PagedResult<EcommerceLaptop.Core.DTOs.Inventory.InventoryDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<List<Inventory>> GetInventoriesForReportAsync(EcommerceLaptop.Core.DTOs.Inventory.InventoryReportRequest request)
    {
        var query = _context.Inventories
            .Include(i => i.Product)
            .AsQueryable();

        if (request.FromDate.HasValue)
        {
            query = query.Where(i => i.LastStockUpdate >= request.FromDate.Value);
        }

        if (request.ToDate.HasValue)
        {
            query = query.Where(i => i.LastStockUpdate <= request.ToDate.Value);
        }

        if (!string.IsNullOrEmpty(request.WarehouseLocation))
        {
            query = query.Where(i => i.WarehouseLocation == request.WarehouseLocation);
        }

        if (!string.IsNullOrEmpty(request.Brand))
        {
            query = query.Where(i => i.Product.Brand == request.Brand);
        }

        return await query.ToListAsync();
    }

    public async Task<List<InventoryTransaction>> GetTransactionsAsync(int productId, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.InventoryTransactions
            .Include(t => t.Inventory)
            .ThenInclude(i => i.Product)
            .Where(t => t.Inventory.ProductId == productId);

        if (fromDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= toDate.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<InventoryTransaction>> GetStockMovementAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.InventoryTransactions
            .Include(t => t.Inventory)
            .ThenInclude(i => i.Product)
            .Where(t => t.CreatedAt >= startDate && t.CreatedAt <= endDate)
            .ToListAsync();
    }

    public async Task<InventoryTransaction> AddTransactionAsync(InventoryTransaction transaction)
    {
        await _context.InventoryTransactions.AddAsync(transaction);
        await _context.SaveChangesAsync();
        return transaction;
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction != null)
        {
            return;
        }
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        try
        {
            await _context.SaveChangesAsync();
            if (_transaction != null)
            {
                await _transaction.CommitAsync();
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync()
    {
        try
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
            }
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }
}
