using EcommerceLaptop.Core.Common;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.DomainEvents;

public class OrderCreatedEvent : IDomainEvent
{
    public Order Order { get; }
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    public OrderCreatedEvent(Order order)
    {
        Order = order;
    }
}
