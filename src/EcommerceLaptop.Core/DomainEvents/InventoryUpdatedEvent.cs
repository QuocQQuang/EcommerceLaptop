using EcommerceLaptop.Core.Common;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.DomainEvents;

public class InventoryUpdatedEvent : IDomainEvent
{
    public Inventory Inventory { get; }
    public string Reason { get; }
    public int QuantityChanged { get; }
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    public InventoryUpdatedEvent(Inventory inventory, int quantityChanged, string reason)
    {
        Inventory = inventory;
        QuantityChanged = quantityChanged;
        Reason = reason;
    }
}
