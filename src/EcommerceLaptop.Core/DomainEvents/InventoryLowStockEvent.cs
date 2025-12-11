using EcommerceLaptop.Core.Common;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.DomainEvents;

public class InventoryLowStockEvent : IDomainEvent
{
    public Inventory Inventory { get; }
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    public InventoryLowStockEvent(Inventory inventory)
    {
        Inventory = inventory;
    }
}
