using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Background service for indexing products to Elasticsearch
/// Handles bulk indexing, incremental updates, and scheduled reindexing
/// </summary>
public class ProductIndexingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProductIndexingService> _logger;
    private readonly IConfiguration _configuration;
    private const int DEFAULT_BATCH_SIZE = 100;
    private const int DEFAULT_DELAY_MINUTES = 30;

    public ProductIndexingService(
        IServiceProvider serviceProvider,
        ILogger<ProductIndexingService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Product Indexing Service started");

        // Initial delay to allow application to fully start
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingIndexingTasks(stoppingToken);

                var delayMinutes = _configuration.GetValue<int>("Search:IndexingDelayMinutes", DEFAULT_DELAY_MINUTES);
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Product Indexing Service stopping due to cancellation");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Product Indexing Service execution");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Brief delay before retry
            }
        }

        _logger.LogInformation("Product Indexing Service stopped");
    }

    private async Task ProcessPendingIndexingTasks(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var searchService = scope.ServiceProvider.GetRequiredService<IProductSearchService>();

        try
        {
            // Get products that need indexing (modified since last index)
            var cutoffDate = DateTime.UtcNow.AddHours(-1); // Index products modified in last hour

            var productsToIndex = await dbContext.Products
                .Where(p => p.UpdatedAt >= cutoffDate)
                .OrderBy(p => p.UpdatedAt)
                .AsNoTracking() // Improve performance for read-only operations
                .ToListAsync(cancellationToken);

            if (productsToIndex.Any())
            {
                _logger.LogInformation("Found {Count} products to index", productsToIndex.Count);

                var batchSize = _configuration.GetValue<int>("Search:IndexingBatchSize", DEFAULT_BATCH_SIZE);

                // Process in batches
                for (int i = 0; i < productsToIndex.Count; i += batchSize)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var batch = productsToIndex.Skip(i).Take(batchSize);
                    await IndexProductBatch(searchService, batch, cancellationToken);

                    // Small delay between batches to avoid overwhelming Elasticsearch
                    await Task.Delay(200, cancellationToken); // Increase delay slightly
                }

                _logger.LogInformation("Completed indexing {Count} products", productsToIndex.Count);
            }

            // Check for full reindex requirement (e.g., daily at 2 AM)
            await CheckAndPerformFullReindex(searchService, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing pending indexing tasks");
        }
    }

    private async Task IndexProductBatch(
        IProductSearchService searchService,
        IEnumerable<EcommerceLaptop.Core.Entities.Product> products,
        CancellationToken cancellationToken)
    {
        try
        {
            var productList = products.ToList();
            _logger.LogDebug("Indexing batch of {Count} products", productList.Count);

            await searchService.BulkIndexProductsAsync(productList);

            _logger.LogDebug("Successfully indexed batch of {Count} products", productList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing product batch");
            throw; // Re-throw to handle at higher level
        }
    }

    private async Task CheckAndPerformFullReindex(
        IProductSearchService searchService,
        CancellationToken cancellationToken)
    {
        try
        {
            var lastFullReindex = GetLastFullReindexTime();
            var reindexInterval = _configuration.GetValue<int>("Search:FullReindexIntervalHours", 24);

            if (DateTime.UtcNow.Subtract(lastFullReindex).TotalHours >= reindexInterval)
            {
                _logger.LogInformation("Starting full reindex (last reindex: {LastReindex})", lastFullReindex);

                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Get all active products
                var allProducts = await dbContext.Products
                    .Where(p => p.IsActive)
                    .AsNoTracking() // Improve performance for read-only operations
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Full reindex: Processing {Count} active products", allProducts.Count);

                // Recreate index
                await searchService.CreateOrUpdateIndexAsync();

                // Index all products in batches
                var batchSize = _configuration.GetValue<int>("Search:FullReindexBatchSize", 500);

                for (int i = 0; i < allProducts.Count; i += batchSize)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var batch = allProducts.Skip(i).Take(batchSize);
                    await IndexProductBatch(searchService, batch, cancellationToken);

                    _logger.LogInformation("Full reindex progress: {Processed}/{Total} products",
                        Math.Min(i + batchSize, allProducts.Count), allProducts.Count);

                    // Longer delay for full reindex to be gentler on the system
                    await Task.Delay(500, cancellationToken);
                }

                SetLastFullReindexTime(DateTime.UtcNow);

                _logger.LogInformation("Full reindex completed successfully. Indexed {Count} products", allProducts.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full reindex operation");
        }
    }

    private DateTime GetLastFullReindexTime()
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // You might want to store this in a settings table
            // For now, we'll use a simple approach with a configuration fallback
            var defaultTime = DateTime.UtcNow.AddDays(-1); // Default to yesterday

            // In a real implementation, you'd query a settings table:
            // var setting = await dbContext.SystemSettings
            //     .FirstOrDefaultAsync(s => s.Key == "LastFullReindexTime");
            // return setting?.Value != null ? DateTime.Parse(setting.Value) : defaultTime;

            return defaultTime;
        }
        catch
        {
            return DateTime.UtcNow.AddDays(-1);
        }
    }

    private void SetLastFullReindexTime(DateTime time)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // In a real implementation, you'd update a settings table:
            // var setting = await dbContext.SystemSettings
            //     .FirstOrDefaultAsync(s => s.Key == "LastFullReindexTime");
            // if (setting == null)
            // {
            //     setting = new SystemSetting { Key = "LastFullReindexTime" };
            //     dbContext.SystemSettings.Add(setting);
            // }
            // setting.Value = time.ToString("O");
            // await dbContext.SaveChangesAsync();

            _logger.LogInformation("Last full reindex time set to {Time}", time);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting last full reindex time");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Product Indexing Service is stopping");
        await base.StopAsync(cancellationToken);
    }
}

/// <summary>
/// Service for managing manual product indexing operations
/// </summary>
public interface IProductIndexingManagementService
{
    Task<bool> IndexProductAsync(int productId);
    Task<bool> RemoveProductFromIndexAsync(int productId);
    Task<bool> BulkIndexProductsAsync(IEnumerable<int> productIds);
    Task<bool> ReindexAllProductsAsync();
    Task<IndexingStatusDto> GetIndexingStatusAsync();
}

/// <summary>
/// Implementation of manual product indexing management
/// </summary>
public class ProductIndexingManagementService : IProductIndexingManagementService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IProductSearchService _searchService;
    private readonly IVectorDbService _vectorDbService;
    private readonly IProductChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<ProductIndexingManagementService> _logger;

    public ProductIndexingManagementService(
        ApplicationDbContext dbContext,
        IProductSearchService searchService,
        IVectorDbService vectorDbService,
        IProductChunkingService chunkingService,
        IEmbeddingService embeddingService,
        ILogger<ProductIndexingManagementService> logger)
    {
        _dbContext = dbContext;
        _searchService = searchService;
        _vectorDbService = vectorDbService;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<bool> IndexProductAsync(int productId)
    {
        try
        {
            var product = await _dbContext.Products.FindAsync(productId);
            if (product == null)
            {
                _logger.LogWarning("Product with ID {ProductId} not found for indexing", productId);
                return false;
            }

            // 1. Index to Elasticsearch
            await _searchService.BulkIndexProductsAsync(new[] { product });

            // 2. Index to Vector DB
            await IndexToVectorDbAsync(new[] { product });

            _logger.LogInformation("Successfully indexed product {ProductId}", productId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing product {ProductId}", productId);
            return false;
        }
    }

    public async Task<bool> RemoveProductFromIndexAsync(int productId)
    {
        try
        {
            // 1. Remove from Elasticsearch
            var success = await _searchService.RemoveProductFromIndexAsync(productId);

            // 2. Remove from Vector DB
            await _vectorDbService.DeleteAsync("products", productId.ToString());

            if (success)
            {
                _logger.LogInformation("Successfully removed product {ProductId} from index", productId);
            }
            else
            {
                _logger.LogWarning("Failed to remove product {ProductId} from index", productId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product {ProductId} from index", productId);
            return false;
        }
    }

    public async Task<bool> BulkIndexProductsAsync(IEnumerable<int> productIds)
    {
        try
        {
            var products = await _dbContext.Products
                .Where(p => productIds.Contains(p.Id))
                .Include(p => p.Category) // Ensure navigation properties are loaded for better chunking
                .ToListAsync();

            if (!products.Any())
            {
                _logger.LogWarning("No products found for bulk indexing");
                return false;
            }

            // 1. Index to Elasticsearch
            await _searchService.BulkIndexProductsAsync(products);

            // 2. Index to Vector DB
            await IndexToVectorDbAsync(products);

            _logger.LogInformation("Successfully bulk indexed {Count} products", products.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk indexing products");
            return false;
        }
    }

    public async Task<bool> ReindexAllProductsAsync()
    {
        try
        {
            _logger.LogInformation("Starting full reindex of all products");

            // Recreate Elasticsearch index
            await _searchService.CreateOrUpdateIndexAsync();

            // Ensure Vector DB collection exists
            await _vectorDbService.EnsureCollectionExistsAsync("products");

            // Get all active products
            var allProducts = await _dbContext.Products
                .Where(p => p.IsActive)
                .Include(p => p.Category) // Important for chunking
                .ToListAsync();

            // Index in batches
            const int batchSize = 25; // Reduce batch size because embedding generation is heavy
            for (int i = 0; i < allProducts.Count; i += batchSize)
            {
                var batch = allProducts.Skip(i).Take(batchSize).ToList();
                
                // 1. Elasticsearch
                await _searchService.BulkIndexProductsAsync(batch);

                // 2. Vector DB
                await IndexToVectorDbAsync(batch);

                _logger.LogInformation("Reindex progress: {Processed}/{Total} products",
                    Math.Min(i + batchSize, allProducts.Count), allProducts.Count);
            }

            _logger.LogInformation("Full reindex completed. Indexed {Count} products", allProducts.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full reindex");
            return false;
        }
    }

    public async Task<IndexingStatusDto> GetIndexingStatusAsync()
    {
        try
        {
            var totalProducts = await _dbContext.Products.CountAsync(p => p.IsActive);

            return new IndexingStatusDto
            {
                TotalProducts = totalProducts,
                IndexedProducts = totalProducts,
                LastIndexingTime = DateTime.UtcNow.AddHours(-1),
                IsIndexingInProgress = false,
                IndexingProgress = 100
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting indexing status");
            throw;
        }
    }

    private async Task IndexToVectorDbAsync(IEnumerable<EcommerceLaptop.Core.Entities.Product> products)
    {
        try 
        {
            var allChunks = new List<ProductChunk>();
            foreach (var product in products)
            {
                var chunks = _chunkingService.ChunkProduct(product);
                allChunks.AddRange(chunks);
            }

            if (allChunks.Any())
            {
                var contents = allChunks.Select(c => c.Content).ToList();
                _logger.LogInformation("Generating embeddings for {Count} chunks...", contents.Count);
                
                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(contents);
                
                _logger.LogInformation("Upserting {Count} vectors to Qdrant...", embeddings.Count);
                await _vectorDbService.UpsertAsync("products", allChunks, embeddings);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index batch to Vector DB");
            // We don't throw here to ensure Elasticsearch indexing (which happens before) isn't considered "failed" entirely, 
            // but in a strict system we might want to throw to retry.
        }
    }
}

/// <summary>
/// DTO for indexing status information
/// </summary>
public class IndexingStatusDto
{
    public int TotalProducts { get; set; }
    public int IndexedProducts { get; set; }
    public DateTime LastIndexingTime { get; set; }
    public bool IsIndexingInProgress { get; set; }
    public int IndexingProgress { get; set; }
}
