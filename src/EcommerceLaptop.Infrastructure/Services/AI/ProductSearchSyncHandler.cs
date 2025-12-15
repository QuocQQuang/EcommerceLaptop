using EcommerceLaptop.Core.DomainEvents;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Interfaces.Services;
using Hangfire;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace EcommerceLaptop.Infrastructure.Services.AI;

public class ProductSearchSyncHandler : 
    IEventHandler<ProductUpdatedEvent>,
    IEventHandler<ProductCreatedEvent>,
    IEventHandler<ProductDeletedEvent>
{
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<ProductSearchSyncHandler> _logger;

    public ProductSearchSyncHandler(
        IBackgroundJobClient backgroundJobClient,
        ILogger<ProductSearchSyncHandler> logger)
    {
        _backgroundJobClient = backgroundJobClient;
        _logger = logger;
    }

    public Task HandleAsync(ProductUpdatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scheduling search index update for Product {Id}", domainEvent.Product.Id);
        _backgroundJobClient.Enqueue<IProductIndexingService>(x => x.IndexProductAsync(domainEvent.Product.Id));
        return Task.CompletedTask;
    }

    public Task HandleAsync(ProductCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scheduling search index creation for Product {Id}", domainEvent.Product.Id);
        _backgroundJobClient.Enqueue<IProductIndexingService>(x => x.IndexProductAsync(domainEvent.Product.Id));
        return Task.CompletedTask;
    }

    public Task HandleAsync(ProductDeletedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scheduling search index deletion for Product {Id}", domainEvent.ProductId);
        _backgroundJobClient.Enqueue<IProductIndexingService>(x => x.DeleteProductAsync(domainEvent.ProductId));
        return Task.CompletedTask;
    }
}
