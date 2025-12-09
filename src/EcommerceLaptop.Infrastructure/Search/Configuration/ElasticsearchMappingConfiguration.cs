using Nest;
using EcommerceLaptop.Infrastructure.Search.Models;

namespace EcommerceLaptop.Infrastructure.Search.Configuration;

/// <summary>
/// Elasticsearch index mapping configuration
/// Optimized for advanced search and aggregations
/// </summary>
public static class ElasticsearchMappingConfiguration
{
    public static CreateIndexDescriptor GetProductIndexMapping(string indexName)
    {
        return new CreateIndexDescriptor(indexName)
            .Settings(s => s
                .NumberOfShards(2)
                .NumberOfReplicas(1)
                .Analysis(a => a
                    .Analyzers(analyzers => analyzers
                        .Standard("standard_analyzer", sa => sa
                            .StopWords("_english_")
                        )
                        .Custom("product_name_analyzer", ca => ca
                            .Tokenizer("standard")
                            .Filters("lowercase", "stop", "snowball")
                        )
                        .Custom("completion_analyzer", ca => ca
                            .Tokenizer("keyword")
                            .Filters("lowercase")
                        )
                        .Custom("search_analyzer", ca => ca
                            .Tokenizer("standard")
                            .Filters("lowercase", "stop", "snowball", "synonym_filter")
                        )
                    )
                    .TokenFilters(tf => tf
                        .Synonym("synonym_filter", sf => sf
                            .Synonyms(
                                "laptop,notebook,computer",
                                "gaming,gamer,game",
                                "ultrabook,thin,slim",
                                "workstation,professional,business",
                                "ssd,solid state drive",
                                "hdd,hard disk drive",
                                "ram,memory",
                                "cpu,processor",
                                "gpu,graphics card,video card"
                            )
                        )
                    )
                )
            )
            .Map<ProductSearchDocument>(m => m
                .AutoMap()
                .Properties(p => p
                    .Keyword(k => k
                        .Name(n => n.Id)
                    )
                    .Text(t => t
                        .Name(n => n.Name)
                        .Analyzer("product_name_analyzer")
                        .Fields(f => f
                            .Keyword(k => k.Name("keyword"))
                            .Completion(c => c.Name("suggest").Analyzer("completion_analyzer"))
                        )
                    )
                    .Text(t => t
                        .Name(n => n.Description)
                        .Analyzer("standard_analyzer")
                        .Fields(f => f
                            .Keyword(k => k.Name("keyword"))
                        )
                    )
                    .Keyword(k => k
                        .Name(n => n.SKU)
                    )
                    .Text(t => t
                        .Name(n => n.Brand)
                        .Fields(f => f
                            .Keyword(k => k.Name("keyword"))
                        )
                    )
                    .Text(t => t
                        .Name(n => n.ProductType)
                        .Fields(f => f
                            .Keyword(k => k.Name("keyword"))
                        )
                    )
                    .Number(n => n
                        .Name(p => p.Price)
                        .Type(NumberType.Double)
                    )
                    .Boolean(b => b
                        .Name(n => n.IsActive)
                    )
                    .Date(d => d
                        .Name(n => n.CreatedAt)
                    )
                    .Date(d => d
                        .Name(n => n.UpdatedAt)
                    )
                    .Keyword(k => k
                        .Name(n => n.Series)
                    )
                    .Keyword(k => k
                        .Name(n => n.CpuBrand)
                    )
                    .Keyword(k => k
                        .Name(n => n.CpuModel)
                    )
                )
            );
    }
}
