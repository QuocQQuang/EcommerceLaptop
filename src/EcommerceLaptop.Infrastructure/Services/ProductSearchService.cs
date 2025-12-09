using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Nest;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Configuration;
using EcommerceLaptop.Infrastructure.Search.Models;
using System.Diagnostics;

namespace EcommerceLaptop.Infrastructure.Services;

/// <summary>
/// Advanced product search service implementation using Elasticsearch
/// Provides comprehensive search capabilities with high performance and relevancy
/// </summary>
public class ProductSearchService : IProductSearchService
{
    private readonly IElasticClient _elasticClient;
    private readonly ElasticsearchSettings _settings;
    private readonly ILogger<ProductSearchService> _logger;
    private readonly string _productIndexName;

    public ProductSearchService(
        IElasticClient elasticClient,
        IOptions<ElasticsearchSettings> settings,
        ILogger<ProductSearchService> logger)
    {
        _elasticClient = elasticClient;
        _settings = settings.Value;
        _logger = logger;
        _productIndexName = _settings.IndexName;
    }

    public async Task<SearchResult<Product>> SearchProductsAsync(ProductSearchRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Performing product search: Query='{Query}', Page={Page}, PageSize={PageSize}", 
                request.Query, request.Page, request.PageSize);

            var searchDescriptor = new SearchDescriptor<ProductSearchDocument>()
                .Index(_productIndexName)
                .From((request.Page - 1) * request.PageSize)
                .Size(request.PageSize)
                .Query(q => BuildSearchQuery(q, request))
                .Sort(s => BuildSortDescriptor(s, request.SortBy, request.SortDirection));

            var response = await _elasticClient.SearchAsync<ProductSearchDocument>(searchDescriptor);

            if (!response.IsValid)
            {
                _logger.LogError("Elasticsearch search failed: {Error}", response.OriginalException?.Message);
                return new SearchResult<Product>();
            }

            stopwatch.Stop();

            var result = new SearchResult<Product>
            {
                Items = response.Documents.Select(ConvertToProduct).Where(p => p != null).ToList()!,
                TotalCount = (int)(response.Total),
                Page = request.Page,
                PageSize = request.PageSize,
                SearchTime = stopwatch.ElapsedMilliseconds,
                Query = request.Query ?? string.Empty
            };

            _logger.LogInformation("Search completed: Found {TotalCount} products in {SearchTime}ms", 
                result.TotalCount, result.SearchTime);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing product search");
            stopwatch.Stop();
            return new SearchResult<Product>();
        }
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query, int maxSuggestions = 10)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Array.Empty<string>();
            }

            _logger.LogInformation("Getting search suggestions for: '{Query}'", query);

            var searchDescriptor = new SearchDescriptor<ProductSearchDocument>()
                .Index(_productIndexName)
                .Size(maxSuggestions)
                .Query(q => q
                    .Bool(b => b
                        .Should(
                            s => s.Prefix(p => p.Field(f => f.Name).Value(query.ToLower())),
                            s => s.Prefix(p => p.Field(f => f.Brand).Value(query.ToLower())),
                            s => s.Prefix(p => p.Field(f => f.Model).Value(query.ToLower()))
                        )
                    )
                )
                .Source(s => s.Includes(i => i.Fields(f => f.Name, f => f.Brand, f => f.Model)));

            var response = await _elasticClient.SearchAsync<ProductSearchDocument>(searchDescriptor);

            if (!response.IsValid)
            {
                _logger.LogError("Search suggestions failed: {Error}", response.OriginalException?.Message);
                return Array.Empty<string>();
            }

            var suggestions = response.Documents
                .SelectMany(d => new[] { d.Name, d.Brand, d.Model })
                .Where(s => !string.IsNullOrEmpty(s) && s.ToLower().Contains(query.ToLower()))
                .Distinct()
                .Take(maxSuggestions)
                .ToArray();

            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions");
            return Array.Empty<string>();
        }
    }

    public async Task<FacetedSearchResult<Product>> FacetedSearchAsync(FacetedSearchRequest request)
    {
        try
        {
            _logger.LogInformation("Performing faceted search: Query='{Query}'", request.Query);

            var searchDescriptor = new SearchDescriptor<ProductSearchDocument>()
                .Index(_productIndexName)
                .From((request.Page - 1) * request.PageSize)
                .Size(request.PageSize)
                .Query(q => BuildSearchQuery(q, request))
                .Aggregations(a => BuildFacetAggregations(a));

            var response = await _elasticClient.SearchAsync<ProductSearchDocument>(searchDescriptor);

            if (!response.IsValid)
            {
                _logger.LogError("Faceted search failed: {Error}", response.OriginalException?.Message);
                return new FacetedSearchResult<Product>();
            }

            var result = new FacetedSearchResult<Product>
            {
                Items = response.Documents.Select(ConvertToProduct).Where(p => p != null).ToList()!,
                TotalCount = (int)(response.Total),
                Page = request.Page,
                PageSize = request.PageSize,
                Query = request.Query ?? string.Empty,
                Facets = ExtractFacets(response.Aggregations)
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing faceted search");
            return new FacetedSearchResult<Product>();
        }
    }

    public async Task<bool> IndexProductAsync(Product product)
    {
        try
        {
            _logger.LogInformation("Indexing product: {ProductId}", product.Id);

            var document = ProductSearchDocument.FromProduct(product);
            document.GenerateSearchTags();

            var response = await _elasticClient.IndexDocumentAsync(document);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to index product {ProductId}: {Error}", 
                    product.Id, response.OriginalException?.Message);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing product {ProductId}", product.Id);
            return false;
        }
    }

    public async Task<bool> BulkIndexProductsAsync(IEnumerable<Product> products)
    {
        try
        {
            var documents = products.Select(p => 
            {
                var doc = ProductSearchDocument.FromProduct(p);
                doc.GenerateSearchTags();
                return doc;
            }).ToList();

            _logger.LogInformation("Bulk indexing {Count} products", documents.Count);

            var bulkRequest = new BulkRequest(_productIndexName)
            {
                Operations = documents.Select(d => new BulkIndexOperation<ProductSearchDocument>(d)).Cast<IBulkOperation>().ToList()
            };

            var response = await _elasticClient.BulkAsync(bulkRequest);

            if (!response.IsValid || response.Errors)
            {
                _logger.LogError("Bulk indexing failed: {Error}", response.OriginalException?.Message);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk indexing products");
            return false;
        }
    }

    public async Task<bool> RemoveProductFromIndexAsync(int productId)
    {
        try
        {
            _logger.LogInformation("Removing product from index: {ProductId}", productId);

            var response = await _elasticClient.DeleteAsync<ProductSearchDocument>(productId, d => d.Index(_productIndexName));

            if (!response.IsValid)
            {
                _logger.LogError("Failed to remove product {ProductId} from index: {Error}", 
                    productId, response.OriginalException?.Message);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product {ProductId} from index", productId);
            return false;
        }
    }

    public async Task<bool> UpdateProductInIndexAsync(Product product)
    {
        try
        {
            // For now, we'll just re-index the product
            return await IndexProductAsync(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product {ProductId} in index", product.Id);
            return false;
        }
    }

    public async Task<bool> CreateOrUpdateIndexAsync()
    {
        try
        {
            _logger.LogInformation("Creating or updating search index: {IndexName}", _productIndexName);

            var existsResponse = await _elasticClient.Indices.ExistsAsync(_productIndexName);

            if (existsResponse.Exists)
            {
                _logger.LogInformation("Index {IndexName} already exists", _productIndexName);
                return true;
            }

            var createIndexResponse = await _elasticClient.Indices.CreateAsync(_productIndexName, c => c
                .Map<ProductSearchDocument>(m => m.AutoMap())
                .Settings(s => s
                    .NumberOfShards(1)
                    .NumberOfReplicas(0)
                ));

            if (!createIndexResponse.IsValid)
            {
                _logger.LogError("Failed to create index {IndexName}: {Error}", 
                    _productIndexName, createIndexResponse.OriginalException?.Message);
                return false;
            }

            _logger.LogInformation("Successfully created index: {IndexName}", _productIndexName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating or updating index");
            return false;
        }
    }

    public async Task<bool> DeleteIndexAsync()
    {
        try
        {
            _logger.LogInformation("Deleting search index: {IndexName}", _productIndexName);

            var response = await _elasticClient.Indices.DeleteAsync(_productIndexName);

            if (!response.IsValid)
            {
                _logger.LogError("Failed to delete index {IndexName}: {Error}", 
                    _productIndexName, response.OriginalException?.Message);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting index");
            return false;
        }
    }

    private QueryContainer BuildSearchQuery(QueryContainerDescriptor<ProductSearchDocument> q, ProductSearchRequest request)
    {
        var queries = new List<QueryContainer>();

        // Basic query matching
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            queries.Add(q.MultiMatch(m => m
                .Fields(f => f
                    .Field(p => p.Name, boost: 2.0)
                    .Field(p => p.Description)
                    .Field(p => p.Brand, boost: 1.5)
                    .Field(p => p.Model, boost: 1.5)
                )
                .Query(request.Query)
                .Type(TextQueryType.BestFields)
                .Fuzziness(Fuzziness.Auto)
            ));
        }

        // Active filter
        if (request.IsActive.HasValue)
        {
            queries.Add(q.Term(t => t.Field(f => f.IsActive).Value(request.IsActive.Value)));
        }

        // Brand filter
        if (request.Brands?.Any() == true)
        {
            queries.Add(q.Terms(t => t.Field(f => f.Brand).Terms(request.Brands)));
        }

        // Product type filter
        if (request.ProductTypes?.Any() == true)
        {
            queries.Add(q.Terms(t => t.Field(f => f.ProductType).Terms(request.ProductTypes)));
        }

        // Price range filter
        if (request.MinPrice.HasValue || request.MaxPrice.HasValue)
        {
            queries.Add(q.Range(r => 
            {
                var range = r.Field(f => f.Price);
                if (request.MinPrice.HasValue)
                    range = range.GreaterThanOrEquals((double)request.MinPrice.Value);
                if (request.MaxPrice.HasValue)
                    range = range.LessThanOrEquals((double)request.MaxPrice.Value);
                return range;
            }));
        }

        return queries.Any() 
            ? q.Bool(b => b.Must(queries.ToArray()))
            : q.MatchAll();
    }

    private SortDescriptor<ProductSearchDocument> BuildSortDescriptor(
        SortDescriptor<ProductSearchDocument> s, 
        string? sortBy, 
        string? sortDirection)
    {
        var isAscending = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.ToLower() switch
        {
            "price" => isAscending ? s.Ascending(p => p.Price) : s.Descending(p => p.Price),
            "name" => isAscending ? s.Ascending(p => p.Name.Suffix("keyword")) : s.Descending(p => p.Name.Suffix("keyword")),
            "created" => isAscending ? s.Ascending(p => p.CreatedAt) : s.Descending(p => p.CreatedAt),
            "rating" => isAscending ? s.Ascending(p => p.AverageRating) : s.Descending(p => p.AverageRating),
            _ => s.Descending(SortSpecialField.Score).Descending(p => p.CreatedAt)
        };
    }

    private AggregationContainerDescriptor<ProductSearchDocument> BuildFacetAggregations(
        AggregationContainerDescriptor<ProductSearchDocument> a)
    {
        return a
            .Terms("brands", t => t.Field(f => f.Brand).Size(10))
            .Terms("product_types", t => t.Field(f => f.ProductType).Size(10))
            .Range("price_ranges", r => r.Field(f => f.Price).Ranges(
                ranges => ranges.To(500),
                ranges => ranges.From(500).To(1000),
                ranges => ranges.From(1000).To(2000),
                ranges => ranges.From(2000)
            ));
    }

    private Dictionary<string, FacetResult> ExtractFacets(IReadOnlyDictionary<string, IAggregate> aggregations)
    {
        var facets = new Dictionary<string, FacetResult>();

        foreach (var (key, agg) in aggregations)
        {
            if (agg is BucketAggregate bucketAgg)
            {
                facets[key] = new FacetResult
                {
                    Field = key,
                    Values = bucketAgg.Items.OfType<KeyedBucket<object>>().Select(b => new FacetValue
                    {
                        Value = b.Key.ToString() ?? string.Empty,
                        Count = (int)(b.DocCount ?? 0)
                    }).ToList(),
                    TotalCount = bucketAgg.Items.Sum(i => (int)(((KeyedBucket<object>)i).DocCount ?? 0))
                };
            }
        }

        return facets;
    }

    private Product? ConvertToProduct(ProductSearchDocument document)
    {
        try
        {
            // For now, we'll create a basic product representation
            // In a real implementation, you might need to fetch the full product from the database
            // or store more complete information in the search document

            if (document.ProductType == "Laptop")
            {
                return new Laptop
                {
                    Id = document.Id,
                    Name = document.Name,
                    Description = document.Description,
                    Brand = document.Brand,
                    Model = document.Model,
                    Price = document.Price,
                    SKU = document.SKU,
                    IsActive = document.IsActive,
                    CreatedAt = document.CreatedAt,
                    UpdatedAt = document.UpdatedAt,
                    Series = document.Series ?? string.Empty,
                    CpuBrand = document.CpuBrand ?? string.Empty,
                    CpuModel = document.CpuModel ?? string.Empty,
                    CpuGeneration = document.CpuGeneration ?? string.Empty,
                    CpuCores = document.CpuCores ?? 0,
                    RamType = document.RamType ?? string.Empty,
                    RamCapacityGB = document.RamCapacityGB ?? 0,
                    StorageType = document.StorageType ?? string.Empty,
                    StorageCapacityGB = document.StorageCapacityGB ?? 0,
                    GpuType = document.GpuType ?? string.Empty,
                    GpuBrand = document.GpuBrand ?? string.Empty,
                    GpuModel = document.GpuModel ?? string.Empty,
                    DisplaySizeInches = document.ScreenSizeInches ?? 0,
                    DisplayResolution = document.ScreenResolution ?? string.Empty,
                    WeightKg = document.WeightKg ?? 0,
                    Color = document.AvailableColors?.FirstOrDefault() ?? string.Empty
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting search document to product");
            return null;
        }
    }

    public async Task<SearchAnalytics> GetSearchAnalyticsAsync(DateTime from, DateTime to)
    {
        try
        {
            _logger.LogInformation("Getting search analytics from {From} to {To}", from, to);

            // For now, return empty analytics. In a real implementation, 
            // you would query the analytics index or database
            await Task.CompletedTask;
            
            return new SearchAnalytics
            {
                TotalSearches = 0,
                AverageResponseTime = 0,
                TopQueries = new List<TopSearchQuery>(),
                ZeroResultQueries = new List<ZeroResultQuery>(),
                SearchesByDay = new Dictionary<string, long>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search analytics");
            return new SearchAnalytics();
        }
    }

    public async Task RecordSearchQueryAsync(string query, int resultsCount, long responseTime)
    {
        try
        {
            _logger.LogInformation("Recording search query: '{Query}', Results: {ResultsCount}, Time: {ResponseTime}ms", 
                query, resultsCount, responseTime);

            // For now, just log the query. In a real implementation,
            // you would store this in an analytics index or database
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording search query");
        }
    }
}