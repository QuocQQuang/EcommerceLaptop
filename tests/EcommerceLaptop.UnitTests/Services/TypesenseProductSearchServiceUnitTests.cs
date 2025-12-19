using System.Collections.Generic;
using System.Threading.Tasks; // Ensure Task is imported
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Typesense;
using Typesense.Setup;
using Xunit;
using SearchResult = EcommerceLaptop.Core.Services.SearchResult<EcommerceLaptop.Core.Entities.Product>;


namespace EcommerceLaptop.UnitTests.Services;

public class TypesenseProductSearchServiceUnitTests
{
    private readonly Mock<ITypesenseClient> _typesenseClientMock;
    private readonly Mock<ILogger<ProductSearchService>> _loggerMock;
    private readonly ProductSearchService _service;

    public TypesenseProductSearchServiceUnitTests()
    {
        _typesenseClientMock = new Mock<ITypesenseClient>();
        _loggerMock = new Mock<ILogger<ProductSearchService>>();
        _service = new ProductSearchService(_typesenseClientMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateOrUpdateIndexAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        _typesenseClientMock.Setup(x => x.RetrieveCollection("products", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TypesenseApiNotFoundException("Not found"));

        var emptyFields = new List<Field>();
        var emptyStrings = new List<string>();
        // Constructor based on error: (string name, int numDocuments, IReadOnlyCollection<Field> fields, string defaultSortingField, IReadOnlyCollection<string> tokenSeparators, IReadOnlyCollection<string> symbolsToIndex, bool enableNestedFields, IDictionary<string, object>? metadata)
        var response = new CollectionResponse("products", 0, emptyFields, "created_at", emptyStrings, emptyStrings, false, null);

        _typesenseClientMock.Setup(x => x.CreateCollection(It.IsAny<Schema>()))
            .ReturnsAsync(response);

        // Act
        var result = await _service.CreateOrUpdateIndexAsync();

        // Assert
        result.Should().BeTrue();
        _typesenseClientMock.Verify(x => x.CreateCollection(It.Is<Schema>(s => s.Name == "products")), Times.Once);
    }
    
    [Fact]
    public async Task IndexProductAsync_ShouldSucceed_WhenProductIsValid()
    {
        // Arrange
        var laptop = new EcommerceLaptop.Core.Entities.Laptop
        {
            Id = 1,
            Name = "Test Laptop",
            Description = "Test Desc",
            Brand = "Brand",
            Model = "Model",
            Price = 1000,
            CpuBrand = "Intel",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Mock client call
        // UpsertDocument<T> returns T
        _typesenseClientMock.Setup(x => x.UpsertDocument<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>("products", It.IsAny<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>()))
            .ReturnsAsync(EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument.FromProduct(laptop));

        // Act
        var result = await _service.IndexProductAsync(laptop);

        // Assert
        result.Should().BeTrue();
        _typesenseClientMock.Verify(x => x.UpsertDocument<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>("products", It.Is<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>(d => d.Id == "1")), Times.Once);
    }

    [Fact]
    public async Task SearchProductsAsync_ShouldReturnResults_WhenSuccessful()
    {
        // Arrange
        var request = new EcommerceLaptop.Core.Services.ProductSearchRequest 
        { 
            Query = "test", 
            Page = 1, 
            PageSize = 10 
        };

        var hits = new List<Typesense.Hit<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>>();
        var facetCounts = new List<Typesense.FacetCount>();
        var searchResult = new Typesense.SearchResult<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>(
            facetCounts,
            0, // found
            0, // outOf
            1, // page
            1, // searchTimeMs
            1, // requestParams/searchCutoff? (int?)
            hits
        );

        _typesenseClientMock.Setup(x => x.Search<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>("products", It.IsAny<Typesense.SearchParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResult);

        // Act
        var result = await _service.SearchProductsAsync(request);

        // Assert
        result.Should().NotBeNull();
        _typesenseClientMock.Verify(x => x.Search<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>("products", It.Is<Typesense.SearchParameters>(p => p.Text == "test"), It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task BulkIndexProductsAsync_ShouldSucceed_WhenProductsAreValid()
    {
        // Arrange
        var products = new List<EcommerceLaptop.Core.Entities.Product>
        {
            new EcommerceLaptop.Core.Entities.Laptop { Id = 1, Name = "Laptop 1", Description = "Desc 1", Brand = "Brand", Model = "Model", Price = 1000, CpuBrand = "Intel", CreatedAt = System.DateTime.UtcNow, UpdatedAt = System.DateTime.UtcNow },
            new EcommerceLaptop.Core.Entities.Laptop { Id = 2, Name = "Laptop 2", Description = "Desc 2", Brand = "Brand", Model = "Model", Price = 2000, CpuBrand = "AMD", CreatedAt = System.DateTime.UtcNow, UpdatedAt = System.DateTime.UtcNow }
        };

        var importResponses = new List<Typesense.ImportResponse>
        {
            new Typesense.ImportResponse(true, "Success"),
            new Typesense.ImportResponse(true, "Success")
        };

        _typesenseClientMock.Setup(x => x.ImportDocuments<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>("products", It.IsAny<List<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>>(), It.IsAny<int>(), It.IsAny<Typesense.ImportType>(), It.IsAny<int?>(), It.IsAny<bool?>()))
            .ReturnsAsync(importResponses);

        // Act
        var result = await _service.BulkIndexProductsAsync(products);

        // Assert
        result.Should().BeTrue();
        _typesenseClientMock.Verify(x => x.ImportDocuments<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>("products", It.Is<List<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>>(l => l.Count == 2), It.IsAny<int>(), It.IsAny<Typesense.ImportType>(), It.IsAny<int?>(), It.IsAny<bool?>()), Times.Once);
    }
    [Fact]
    public async Task RemoveProductFromIndexAsync_ShouldSucceed_WhenIdIsValid()
    {
        // Arrange
        int productId = 1;
        var deletedDoc = new EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument { Id = "1" };

        _typesenseClientMock.Setup(x => x.DeleteDocument<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>("products", productId.ToString()))
            .ReturnsAsync(deletedDoc);

        // Act
        var result = await _service.RemoveProductFromIndexAsync(productId);

        // Assert
        result.Should().BeTrue();
        _typesenseClientMock.Verify(x => x.DeleteDocument<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>("products", productId.ToString()), Times.Once);
    }

    [Fact]
    public async Task FacetedSearchAsync_ShouldReturnFacets_WhenRequestIsValid()
    {
        // Arrange
        var request = new EcommerceLaptop.Core.Services.FacetedSearchRequest
        {
            Query = "laptop",
            FacetFields = new[] { "brand" },
            Page = 1,
            PageSize = 10
        };

        var facetCounts = new List<Typesense.FacetCount>
        {
            // FacetCount(string fieldName, IReadOnlyList<FacetCountHit> counts, FacetStats stats)
            new Typesense.FacetCount("brand", new List<Typesense.FacetCountHit> 
            { 
                // FacetCountHit(string value, int count, string highlighted, Dictionary<string, dynamic>? highlights)
                new Typesense.FacetCountHit("Apple", 10, "Apple", null),
                new Typesense.FacetCountHit("Dell", 5, "Dell", null) 
            }, null)
        };

        // SearchResult(IReadOnlyCollection<FacetCount>, int found, int searchTimeMs, int page, int outOf, int? something, IReadOnlyList<Hit<T>> hits)
        // Error said: SearchResult(IReadOnlyCollection<FacetCount>, int, int, int, int, int?, IReadOnlyList<Hit<T>>)
        var searchResult = new Typesense.SearchResult<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>(
            facetCounts, // facets
            0, // found
            10, // searchTimeMs
            1, // page
            1, // outOf? (or hits per page)
            null, // some int? param
            new List<Typesense.Hit<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>>()); // hits

        _typesenseClientMock.Setup(x => x.Search<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>("products", It.IsAny<Typesense.SearchParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResult);

        // Act
        var result = await _service.FacetedSearchAsync(request);

        // Assert
        result.Facets.Should().ContainKey("brand");
        result.Facets["brand"].Values.Should().HaveCount(2);
        result.Facets["brand"].Values.First(v => v.Value == "Apple").Count.Should().Be(10);
    }

    [Fact]
    public async Task GetSearchSuggestionsAsync_ShouldReturnSuggestions_WhenQueryIsValid()
    {
        // Arrange
        var query = "Lap";
        var laptopA = new EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument { Name = "Laptop A" };
        var laptopB = new EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument { Name = "Laptop B" };

        var hits = new List<Typesense.Hit<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>>
        {
            // Hit(List<Highlight>, T document, long? textMatch, double? vectorDistance, Dictionary<string, double>? geoDistance)
            new Typesense.Hit<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>(null, laptopA, 0, null, null),
            new Typesense.Hit<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>(null, laptopB, 0, null, null)
        };

        var searchResult = new Typesense.SearchResult<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>(
            new List<Typesense.FacetCount>(), // Facets
            2, // Found
            10, // SearchTime
            1, // Page
            10, // OutOf
            null, // RequestParams
            hits); // Hits

        _typesenseClientMock.Setup(x => x.Search<EcommerceLaptop.Infrastructure.Search.Models.ProductSearchDocument>("products", It.Is<Typesense.SearchParameters>(p => p.Text == query && p.QueryBy == "name"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResult);

        // Act
        var result = await _service.GetSearchSuggestionsAsync(query);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain("Laptop A");
        result.Should().Contain("Laptop B");
    }
}
