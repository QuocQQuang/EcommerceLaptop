using EcommerceLaptop.Core.Common;

namespace EcommerceLaptop.Core.Interfaces;

public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent);
}
