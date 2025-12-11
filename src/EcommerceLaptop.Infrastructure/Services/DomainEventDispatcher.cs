using EcommerceLaptop.Core.Common;
using EcommerceLaptop.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.Infrastructure.Services;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(ILogger<DomainEventDispatcher> logger)
    {
        _logger = logger;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent)
    {
        _logger.LogInformation("Dispatching domain event: {EventName} occurred on {OccurredOn}", 
            domainEvent.GetType().Name, domainEvent.OccurredOn);
        
        // Future: Use MediatR or similar to find handlers
        await Task.CompletedTask;
    }
}
