using EcommerceLaptop.Core.Common;
using EcommerceLaptop.Core.DomainEvents;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("EcommerceLaptop.Infrastructure")]
[assembly: InternalsVisibleTo("EcommerceLaptop.API")]
[assembly: InternalsVisibleTo("EcommerceLaptop")]
[assembly: InternalsVisibleTo("EcommerceLaptop.UnitTests")]

namespace EcommerceLaptop.Core.Entities;

public class Inventory : BaseEntity
{
    // Id is inherited from BaseEntity
    public int ProductId { get; set; }
    public int QuantityInStock { get; internal set; } // Internal setter for seeding/infrastructure
    public int ReservedQuantity { get; internal set; }
    public int ReorderLevel { get; set; }
    public int MaxStockLevel { get; set; }
    public string WarehouseLocation { get; set; } = string.Empty;
    public DateTime LastStockUpdate { get; internal set; }

    // Optimistic concurrency token  prevents overselling when concurrent orders
    // EF Core will include this in UPDATE WHERE clause automatically
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    // Calculated property
    public int AvailableQuantity => QuantityInStock - ReservedQuantity;

    // Navigation properties
    public Product Product { get; set; } = null!;
    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();

    // Domain Methods

    public void AddStock(int quantity, string reference, string reason, int userId)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        QuantityInStock += quantity;
        LastStockUpdate = DateTime.UtcNow;

        AddDomainEvent(new InventoryUpdatedEvent(this, quantity, reason));

        var transaction = new InventoryTransaction
        {
            Type = InventoryTransactionType.Purchase, // Or Adjustment
            Quantity = quantity,
            Reference = reference,
            Reason = reason,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            Notes = reason
        };
        Transactions.Add(transaction);

        CheckLowStock();
    }

    public void RemoveStock(int quantity, string reference, string reason, int userId)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (AvailableQuantity < quantity) throw new InvalidOperationException("Insufficient stock.");

        QuantityInStock -= quantity;
        LastStockUpdate = DateTime.UtcNow;

        AddDomainEvent(new InventoryUpdatedEvent(this, -quantity, reason));

        var transaction = new InventoryTransaction
        {
            Type = InventoryTransactionType.Sale, // Or Adjustment
            Quantity = -quantity,
            Reference = reference,
            Reason = reason,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            Notes = reason
        };
        Transactions.Add(transaction);

        CheckLowStock();
    }

    public void ReserveStock(int quantity, string reference, string reason, int userId)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (AvailableQuantity < quantity) throw new InvalidOperationException("Insufficient available stock to reserve.");

        ReservedQuantity += quantity;
        LastStockUpdate = DateTime.UtcNow;

        var transaction = new InventoryTransaction
        {
            Type = InventoryTransactionType.Reservation,
            Quantity = quantity,
            Reference = reference,
            Reason = reason,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            Notes = reason
        };
        Transactions.Add(transaction);

        CheckLowStock();
    }

    public void CancelReservation(int quantity, string reference, string reason, int userId)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (ReservedQuantity < quantity) throw new InvalidOperationException("Cannot release more than reserved.");

        ReservedQuantity -= quantity;
        LastStockUpdate = DateTime.UtcNow;

        var transaction = new InventoryTransaction
        {
            Type = InventoryTransactionType.Release,
            Quantity = -quantity,
            Reference = reference,
            Reason = reason,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = userId,
            Notes = reason
        };
        Transactions.Add(transaction);
    }

    public void ConfirmReservation(int quantity, string reference, string reason, int userId)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (ReservedQuantity < quantity) throw new InvalidOperationException("Cannot confirm more than reserved.");

        // Decrement reserved quantity silently (as it's being converted to sale)
        ReservedQuantity -= quantity;

        // Remove from stock (logs Sale transaction)
        RemoveStock(quantity, reference, reason, userId);
    }

    private void CheckLowStock()
    {
        if (AvailableQuantity <= ReorderLevel)
        {
            AddDomainEvent(new InventoryLowStockEvent(this));
        }
    }

    // Helper to initialize for EF or Factory
    public static Inventory Create(int productId, int initialStock, int reorderLevel, int maxStock, string location)
    {
        return new Inventory
        {
            ProductId = productId,
            QuantityInStock = initialStock,
            ReorderLevel = reorderLevel,
            MaxStockLevel = maxStock,
            WarehouseLocation = location,
            LastStockUpdate = DateTime.UtcNow
        };
    }
}

public class InventoryTransaction
{
    public int Id { get; set; }
    public int InventoryId { get; set; }
    public InventoryTransactionType Type { get; set; }
    public int Quantity { get; set; }
    public string Reference { get; set; } = string.Empty; // Order ID, Adjustment ID, etc.
    public string Reason { get; set; } = string.Empty; // Added Reason property
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int CreatedBy { get; set; } // User ID

    // Navigation properties
    public Inventory Inventory { get; set; } = null!;
}

public enum InventoryTransactionType
{
    Purchase = 1,     // Stock in from supplier
    Sale = 2,         // Stock out to customer
    Adjustment = 3,   // Manual adjustment
    Reservation = 4,  // Reserved for order
    Release = 5,      // Released from reservation
    Return = 6,       // Customer return
    Damage = 7,       // Damaged goods
    Transfer = 8      // Transfer between warehouses
}

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CouponType Type { get; set; }
    public decimal Value { get; set; } // Percentage or fixed amount
    public decimal MinimumOrderAmount { get; set; }
    public decimal MaximumDiscountAmount { get; set; }
    public int UsageLimit { get; set; }
    public int UsedCount { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}

public enum CouponType
{
    Percentage = 1,
    FixedAmount = 2,
    FreeShipping = 3
}

public class Campaign
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CampaignType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string BannerImageUrl { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public ICollection<CampaignProduct> CampaignProducts { get; set; } = new List<CampaignProduct>();
}

public class CampaignProduct
{
    public int Id { get; set; }
    public int CampaignId { get; set; }
    public int ProductId { get; set; }
    public decimal DiscountPercentage { get; set; }
    public decimal? FixedDiscountAmount { get; set; }
    public decimal? SpecialPrice { get; set; }

    // Navigation properties
    public Campaign Campaign { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public enum CampaignType
{
    FlashSale = 1,
    Seasonal = 2,
    ProductLaunch = 3,
    Clearance = 4,
    BrandPromotion = 5
}
