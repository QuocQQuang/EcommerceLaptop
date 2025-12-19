using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Search.Models;
using Microsoft.Extensions.Logging;
using Typesense;
using System.Threading.Tasks;

// Define default SearchResult to be the Core one, or better yet, just use full qualification for the return type
using SearchResult = EcommerceLaptop.Core.Services.SearchResult<EcommerceLaptop.Core.Entities.Product>;
using FacetedSearchResult = EcommerceLaptop.Core.Services.FacetedSearchResult<EcommerceLaptop.Core.Entities.Product>;

namespace EcommerceLaptop.Infrastructure.Services;

public class ProductSearchService : IProductSearchService
{
    private readonly ITypesenseClient _client;
    private readonly ILogger<ProductSearchService> _logger;

    public ProductSearchService(ITypesenseClient client, ILogger<ProductSearchService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<bool> BulkIndexProductsAsync(IEnumerable<Product> products)
    {
        try
        {
            var documents = products.Select(ProductSearchDocument.FromProduct).ToList();
            if (!documents.Any()) return true;

            var results = await _client.ImportDocuments("products", documents, 40, ImportType.Upsert);
            var failedItems = results.Where(r => !r.Success).ToList();
            
            if (failedItems.Any())
            {
                _logger.LogError("Bulk index failed for {FailedCount} items. First error: {Error}", failedItems.Count, failedItems.First().Error);
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

    public async Task<bool> CreateOrUpdateIndexAsync()
    {
        try
        {
            try 
            {
                await _client.RetrieveCollection("products");
                await _client.DeleteCollection("products");
            }
            catch (TypesenseApiNotFoundException)
            {
                // Collection does not exist, proceed to create
            }

            var schema = new Schema(
                "products",
                new List<Field>
                {
                    new Field("id", FieldType.String, false),
                    new Field("name", FieldType.String, false),
                    new Field("description", FieldType.String, false),
                    new Field("brand", FieldType.String, true),
                    new Field("model", FieldType.String, false),
                    new Field("price", FieldType.Float, true),
                    new Field("sku", FieldType.String, false),
                    new Field("is_active", FieldType.Bool, false),
                    new Field("product_type", FieldType.String, true),
                    new Field("created_at", FieldType.Int64, false),
                    new Field("updated_at", FieldType.Int64, false),
                    new Field("cpu_brand", FieldType.String, true, true),
                    new Field("ram_gb", FieldType.Int32, true, true),
                    new Field("storage_gb", FieldType.Int32, true, true),
                    new Field("screen_size", FieldType.Float, true, true),
                    new Field("average_rating", FieldType.Float, true, true),
                    new Field("review_count", FieldType.Int32, false),
                    new Field("in_stock", FieldType.Bool, true)
                },
                "created_at"
            );

            await _client.CreateCollection(schema);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Typesense index");
            return false;
        }
    }

    public async Task<bool> DeleteIndexAsync()
    {
        try
        {
            await _client.DeleteCollection("products");
            return true;
        }
        catch (TypesenseApiNotFoundException)
        {
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Typesense index");
            return false;
        }
    }

    public async Task<FacetedSearchResult> FacetedSearchAsync(FacetedSearchRequest request)
    {
        try
        {
            var searchParameters = new SearchParameters(request.Query, "name,description,brand,model,sku,cpu_brand")
            {
                Page = request.Page,
                PerPage = request.PageSize,
                FacetBy = request.FacetFields != null ? string.Join(",", request.FacetFields) : null
            };

            var searchResult = await _client.Search<ProductSearchDocument>("products", searchParameters);

            var result = new FacetedSearchResult
            {
                Items = searchResult.Hits.Select(h => (EcommerceLaptop.Core.Entities.Product)new Laptop
                {
                    Id = int.Parse(h.Document.Id),
                    Name = h.Document.Name,
                    Description = h.Document.Description,
                    Brand = h.Document.Brand,
                    Model = h.Document.Model,
                    Price = h.Document.Price,
                    CpuBrand = h.Document.CpuBrand ?? string.Empty,
                    RamCapacityGB = h.Document.RamCapacityGB ?? 0,
                    StorageCapacityGB = h.Document.StorageCapacityGB ?? 0,
                    DisplaySizeInches = h.Document.ScreenSizeInches ?? 0,
                }).ToList(),
                TotalCount = searchResult.Found,
                Page = searchResult.Page,
                PageSize = request.PageSize,
                SearchTime = searchResult.SearchTimeMs
            };

            if (searchResult.FacetCounts != null)
            {
                foreach (var facet in searchResult.FacetCounts)
                {
                    result.Facets[facet.FieldName] = new FacetResult
                    {
                        Field = facet.FieldName,
                        Values = facet.Counts.Select(c => new FacetValue
                        {
                            Value = c.Value,
                            Count = c.Count
                        }).ToList()
                    };
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing faceted search in Typesense");
            return new FacetedSearchResult();
        }
    }

    public async Task<IEnumerable<string>> GetSearchSuggestionsAsync(string query, int maxSuggestions = 10)
    {
        try
        {
            var searchParameters = new SearchParameters(query, "name")
            {
                Prefix = true,
                PerPage = maxSuggestions,
                Page = 1,
                IncludeFields = "name"
            };

            var searchResult = await _client.Search<ProductSearchDocument>("products", searchParameters);

            return searchResult.Hits
                .Select(h => h.Document.Name)
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting search suggestions for {Query}", query);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<bool> IndexProductAsync(EcommerceLaptop.Core.Entities.Product product)
    {
        return await IndexProductAsyncImplementation(product);
    }
    
    public async Task<bool> UpdateProductInIndexAsync(Product product)
    {
        return await IndexProductAsyncImplementation(product);
    }

    private async Task<bool> IndexProductAsyncImplementation(EcommerceLaptop.Core.Entities.Product product)
    {
        try
        {
            var document = ProductSearchDocument.FromProduct(product);
            await _client.UpsertDocument("products", document);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error indexing product {ProductId}", product.Id);
            return false;
        }
    }

    public async Task<bool> RemoveProductFromIndexAsync(int productId)
    {
        try
        {
            await _client.DeleteDocument<ProductSearchDocument>("products", productId.ToString());
            return true;
        }
        catch (TypesenseApiNotFoundException)
        {
            // Document not found, technically a success as it's gone
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing product {ProductId} from Typesense index", productId);
            return false;
        }
    }

    public async Task<EcommerceLaptop.Core.Services.SearchResult<EcommerceLaptop.Core.Entities.Product>> SearchProductsAsync(ProductSearchRequest request)
    {
        try
        {
            var searchParameters = new SearchParameters(request.Query, "name,description,brand,model,sku,cpu_brand")
            {
                Page = request.Page,
                PerPage = request.PageSize
                // Add FilterBy, SortBy later
            };

            var searchResult = await _client.Search<ProductSearchDocument>("products", searchParameters);

            return new EcommerceLaptop.Core.Services.SearchResult<EcommerceLaptop.Core.Entities.Product>
            {
                Items = searchResult.Hits.Select(h => (EcommerceLaptop.Core.Entities.Product)new Laptop 
                { 
                    Id = int.Parse(h.Document.Id),
                    Name = h.Document.Name,
                    Description = h.Document.Description,
                    Brand = h.Document.Brand,
                    Model = h.Document.Model,
                    Price = h.Document.Price,
                    CpuBrand = h.Document.CpuBrand ?? string.Empty,
                    RamCapacityGB = h.Document.RamCapacityGB ?? 0,
                    StorageCapacityGB = h.Document.StorageCapacityGB ?? 0,
                    DisplaySizeInches = h.Document.ScreenSizeInches ?? 0,
                    // Map other fields as needed
                }).ToList(),
                TotalCount = searchResult.Found,
                Page = searchResult.Page,
                PageSize = request.PageSize,
                SearchTime = searchResult.SearchTimeMs
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products in Typesense");
            return new EcommerceLaptop.Core.Services.SearchResult<EcommerceLaptop.Core.Entities.Product> 
            { 
                Items = new List<EcommerceLaptop.Core.Entities.Product>(), 
                TotalCount = 0, 
                Page = request.Page, 
                PageSize = request.PageSize 
            };
        }
    }


    
    public async Task<SearchAnalytics> GetSearchAnalyticsAsync(DateTime from, DateTime to)
    {
        // Typesense does not have built-in analytics API equivalent to Elasticsearch's aggregations
        // For now, we return an empty result to avoid breaking the frontend
        _logger.LogWarning("GetSearchAnalyticsAsync called but not implemented for Typesense backend");
        
        return Task.FromResult(new SearchAnalytics
        {
            TotalSearches = 0,
            AverageResponseTime = 0,
            TopQueries = new List<TopSearchQuery>(),
            ZeroResultQueries = new List<ZeroResultQuery>(),
            SearchesByDay = new Dictionary<string, long>()
        }).Result;
    }

    public async Task RecordSearchQueryAsync(string query, int resultsCount, long responseTime)
    {
        // Typesense can have analytics enabled via configuration, but for this migration
        // we will just log the query for minimal observability
        _logger.LogTrace("Search Query: {Query}, Results: {Results}, Time: {Time}ms", query, resultsCount, responseTime);
        await Task.CompletedTask;
    }
}