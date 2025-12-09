using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.API.DTOs;

#region Inventory DTOs

/// <summary>
/// Inventory data transfer object
/// </summary>
public class InventoryDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int QuantityInStock { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity => QuantityInStock - ReservedQuantity;
    public int ReorderLevel { get; set; }
    public int MaxStockLevel { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastStockUpdate { get; set; }
    public bool IsLowStock => QuantityInStock <= ReorderLevel;
}

/// <summary>
/// Inventory transaction DTO
/// </summary>
public class InventoryTransactionDto
{
    public int Id { get; set; }
    public int InventoryId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // StockIn, StockOut, Reserved, Released, Adjustment
    public int Quantity { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
}

#endregion

#region Inventory Request DTOs

/// <summary>
/// Create inventory request DTO
/// </summary>
public class CreateInventoryRequest
{
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [Range(0, int.MaxValue)]
    public int QuantityInStock { get; set; }
    
    [Required]
    [Range(0, int.MaxValue)]
    public int ReorderLevel { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int MaxStockLevel { get; set; }
    
    [Required]
    [StringLength(255)]
    public string WarehouseLocation { get; set; } = string.Empty;
}

/// <summary>
/// Update inventory request DTO
/// </summary>
public class UpdateInventoryRequest
{
    [Range(0, int.MaxValue)]
    public int? QuantityInStock { get; set; }
    
    [Range(0, int.MaxValue)]
    public int? ReorderLevel { get; set; }
    
    [Range(1, int.MaxValue)]
    public int? MaxStockLevel { get; set; }
    
    [StringLength(255)]
    public string? WarehouseLocation { get; set; }
}

/// <summary>
/// Stock adjustment request DTO
/// </summary>
public class StockAdjustmentRequest
{
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    public int Quantity { get; set; } // Positive for stock in, negative for stock out
    
    [Required]
    [StringLength(255)]
    public string Reference { get; set; } = string.Empty;
    
    [StringLength(1000)]
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Reserve stock request DTO
/// </summary>
public class ReserveStockRequest
{
    [Required]
    public int ProductId { get; set; }
    
    [Required]
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
    
    [Required]
    [StringLength(255)]
    public string Reference { get; set; } = string.Empty; // Usually order number
    
    [StringLength(1000)]
    public string Notes { get; set; } = string.Empty;
}

#endregion
