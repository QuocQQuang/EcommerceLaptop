using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Infrastructure.Jobs
{
    public class ProductIndexingJob
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductChunkingService _chunkingService;
        private readonly IEmbeddingService _embeddingService;
        private readonly IVectorDbService _vectorDbService;
        private readonly ILogger<ProductIndexingJob> _logger;

        private const int BatchSize = 100;
        private const string CollectionName = "products";

        public ProductIndexingJob(
            ApplicationDbContext context,
            IProductChunkingService chunkingService,
            IEmbeddingService embeddingService,
            IVectorDbService vectorDbService,
            ILogger<ProductIndexingJob> logger)
        {
            _context = context;
            _chunkingService = chunkingService;
            _embeddingService = embeddingService;
            _vectorDbService = vectorDbService;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("Starting product indexing job...");

            try
            {
                // Ensure collection exists
                await _vectorDbService.EnsureCollectionExistsAsync(CollectionName);

                int processedCount = 0;
                int offset = 0;

                while (true)
                {
                    var products = await _context.Products
                        .Include(p => p.Category)
                        .Include(p => p.ProductBrand)
                        // Strategies strategies if Specs are needed, but inheritance usually loads base fields.
                        // For full data, we might need simple cast or eager load.
                        // EF Core will load correct derived type if TPH is used.
                        .Skip(offset)
                        .Take(BatchSize)
                        .ToListAsync();

                    if (!products.Any()) break;

                    var allChunks = new List<ProductChunk>();

                    // 1. Chunking
                    foreach (var product in products)
                    {
                        var chunks = _chunkingService.ChunkProduct(product);
                        allChunks.AddRange(chunks);
                    }

                    if (allChunks.Any())
                    {
                        // 2. Embedding (Batch)
                        var texts = allChunks.Select(c => c.Content).ToList();
                        var embeddings = await _embeddingService.GenerateEmbeddingsAsync(texts);

                        // 3. Upsert
                        await _vectorDbService.UpsertAsync(CollectionName, allChunks, embeddings);
                    }

                    processedCount += products.Count;
                    offset += BatchSize;
                    _logger.LogInformation($"Indexed {processedCount} products...");
                }

                _logger.LogInformation("Product indexing job completed successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during product indexing job.");
                throw; // Rethrow to let Hangfire retry
            }
        }
    }
}
