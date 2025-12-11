namespace EcommerceLaptop.Core.Common;

public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
