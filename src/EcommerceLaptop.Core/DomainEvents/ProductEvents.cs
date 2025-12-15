using EcommerceLaptop.Core.Common;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.DomainEvents;

public class ProductCreatedEvent : IDomainEvent
{
    public Product Product { get; }
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    public ProductCreatedEvent(Product product)
    {
        Product = product;
    }
}

public class ProductUpdatedEvent : IDomainEvent
{
    public Product Product { get; }
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    public ProductUpdatedEvent(Product product)
    {
        Product = product;
    }
}

public class ProductDeletedEvent : IDomainEvent
{
    public int ProductId { get; }
    public DateTime OccurredOn { get; } = DateTime.UtcNow;

    public ProductDeletedEvent(int productId)
    {
        ProductId = productId;
    }
}
