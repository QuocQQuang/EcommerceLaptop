using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.DTOs;

#region Inventory DTOs

/// <summary>
/// Inventory data transfer object
/// </summary>
public record InventoryDto
{
    public int Id { get; init; }
    public int ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int QuantityInStock { get; init; }
    public int ReservedQuantity { get; init; }
    public int AvailableQuantity => QuantityInStock - ReservedQuantity;
    public int ReorderLevel { get; init; }
    public int MaxStockLevel { get; init; }
    public string WarehouseLocation { get; init; } = string.Empty;
    public DateTime LastStockUpdate { get; init; }
    public bool IsLowStock => QuantityInStock <= ReorderLevel;
}

/// <summary>
/// Inventory transaction DTO
/// </summary>
public record InventoryTransactionDto
{
    public int Id { get; init; }
    public int InventoryId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty; // StockIn, StockOut, Reserved, Released, Adjustment
    public int Quantity { get; init; }
    public string Reference { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public int CreatedBy { get; init; }
    public string CreatedByName { get; init; } = string.Empty;
}

#endregion

#region Inventory Request DTOs

/// <summary>
/// Create inventory request DTO
/// </summary>
public record CreateInventoryRequest
{
    [Required]
    public int ProductId { get; init; }
    
    [Required]
    [Range(0, int.MaxValue)]
    public int QuantityInStock { get; init; }
    
    [Required]
    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; init; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int MaxStockLevel { get; init; }
    
    [Required]
    [StringLength(255)]
    public string WarehouseLocation { get; init; } = string.Empty;
}

/// <summary>
/// Update inventory request DTO
/// </summary>
public record UpdateInventoryRequest
{
    [Range(0, int.MaxValue)]
    public int? QuantityInStock { get; init; }
    
    [Range(0, int.MaxValue)]
    public int? ReorderLevel { get; init; }
    
    [Range(1, int.MaxValue)]
    public int? MaxStockLevel { get; init; }
    
    [StringLength(255)]
    public string? WarehouseLocation { get; init; }
}

/// <summary>
/// Stock adjustment request DTO
/// </summary>
public record StockAdjustmentRequest
{
    [Required]
    public int ProductId { get; init; }
    
    [Required]
    public int Quantity { get; init; } // Positive for stock in, negative for stock out
    
    [Required]
    [StringLength(255)]
    public string Reference { get; init; } = string.Empty;
    
    [StringLength(1000)]
    public string Notes { get; init; } = string.Empty;
}

/// <summary>
/// Reserve stock request DTO
/// </summary>
public record ReserveStockRequest
{
    [Required]
    public int ProductId { get; init; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; init; }
    
    [Required]
    [StringLength(255)]
    public string Reference { get; init; } = string.Empty; // Usually order number
    
    [StringLength(1000)]
    public string Notes { get; init; } = string.Empty;
}

#endregion
