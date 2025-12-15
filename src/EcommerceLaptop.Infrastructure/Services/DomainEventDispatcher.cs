using EcommerceLaptop.Core.Common;
using EcommerceLaptop.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace EcommerceLaptop.Infrastructure.Services;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent)
    {
        _logger.LogInformation("Dispatching domain event: {EventName} occurred on {OccurredOn}", 
            domainEvent.GetType().Name, domainEvent.OccurredOn);

        var eventType = domainEvent.GetType();
        var handlerType = typeof(IEventHandler<>).MakeGenericType(eventType);
        
        using (var scope = _serviceProvider.CreateScope())
        {
            var handlers = scope.ServiceProvider.GetServices(handlerType);
            
            foreach (var handler in handlers)
            {
                if (handler == null) continue;

                var method = handler.GetType().GetMethod("HandleAsync");
                if (method != null)
                {
                    await (Task)method.Invoke(handler, new object[] { domainEvent, default(System.Threading.CancellationToken) });
                }
            }
        }
    }
}
