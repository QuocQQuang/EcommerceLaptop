using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces.Services;
using EcommerceLaptop.Core.Services;

namespace EcommerceLaptop.Infrastructure.Jobs
{
    public class ProductIndexingJob : IProductIndexingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductChunkingService _chunkingService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorDbService _vectorDbService;
        private readonly IProductSearchService _searchService;
        private readonly ILogger<ProductIndexingJob> _logger;

        private const int BatchSize = 100;
        private const string CollectionName = "products";

        public ProductIndexingJob(
            ApplicationDbContext context,
            IProductChunkingService chunkingService,
            IEmbeddingService embeddingService,
            IVectorDbService vectorDbService,
            IProductSearchService searchService,
            ILogger<ProductIndexingJob> logger)
        {
            _context = context;
            _chunkingService = chunkingService;
            _embeddingService = embeddingService;
            _vectorDbService = vectorDbService;
            _searchService = searchService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting product indexing job...");

            try
            {
                // Ensure collection exists
                await _vectorDbService.EnsureCollectionExistsAsync(CollectionName);
                
                // Ensure ES index exists
                await _searchService.CreateOrUpdateIndexAsync();

                int processedCount = 0;
                int offset = 0;

                while (true)
                {
                    var products = await _context.Products
                        .Include(p => p.Category)
                        .Include(p => p.ProductBrand)
                        .Skip(offset)
                        .Take(BatchSize)
                        .ToListAsync();

                    if (!products.Any()) break;

                    await ProcessBatchAsync(products);
                    
                    // Also sync to Elasticsearch
                    await _searchService.BulkIndexProductsAsync(products);

                    processedCount += products.Count;
                    offset += BatchSize;
                    _logger.LogInformation($"Indexed {processedCount} products...");
                }

                _logger.LogInformation("Product indexing job completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during product indexing job.");
                throw; 
            }
        }

        public async Task IndexProductAsync(int productId)
        {
             _logger.LogInformation("Indexing product {ProductId}...", productId);
             try 
             {
                 var product = await _context.Products
                        .Include(p => p.Category)
                        .Include(p => p.ProductBrand)
                        .FirstOrDefaultAsync(p => p.Id == productId);
                 
                 // Note: If product is inactive/soft-deleted, we might want to remove it instead?
                 // For now, if it exists in DB, we upsert. If logic requires filtering active, we should check IsActive.
                 if (product == null)
                 {
                     _logger.LogWarning("Product {ProductId} not found for indexing. Attempting removal in case it existed.", productId);
                     await DeleteProductAsync(productId);
                     return;
                 }
                 
                 await ProcessBatchAsync(new List<Product> { product });
                 
                 // Sync to ES
                 await _searchService.IndexProductAsync(product);
                 
                 _logger.LogInformation("Product {ProductId} indexed successfully.", productId);
             }
             catch (Exception ex)
             {
                 _logger.LogError(ex, "Error indexing product {ProductId}.", productId);
                 throw;
             }
        }

        public async Task DeleteProductAsync(int productId)
        {
            _logger.LogInformation("Deleting product {ProductId} from index...", productId);
            try
            {
                // Logic to delete from vector DB
                await _vectorDbService.DeleteAsync(CollectionName, productId.ToString());
                
                // Sync to ES
                await _searchService.RemoveProductFromIndexAsync(productId);
                
                 _logger.LogInformation("Product {ProductId} deleted from index.", productId);
            }
            catch (Exception ex)
            {
                 _logger.LogError(ex, "Error deleting product {ProductId} from index.", productId);
                 throw;
            }
        }

        private async Task ProcessBatchAsync(List<Product> products)
        {
            var allChunks = new List<ProductChunk>();

            foreach (var product in products)
            {
                var chunks = _chunkingService.ChunkProduct(product);
                allChunks.AddRange(chunks);
            }

            if (allChunks.Any())
            {
                var texts = allChunks.Select(c => c.Content).ToList();
                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts);
                await _vectorDbService.UpsertAsync(CollectionName, allChunks, embeddings);
            }
        }
    }
}
