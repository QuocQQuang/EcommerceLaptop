using EcommerceLaptop.Core.Common;

namespace EcommerceLaptop.Core.Interfaces;

public interface IEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
