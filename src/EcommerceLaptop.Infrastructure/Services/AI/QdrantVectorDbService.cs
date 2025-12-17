using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using Google.Protobuf.Collections;
using EcommerceLaptop.Core.Interfaces;

namespace EcommerceLaptop.Infrastructure.Services.AI
{
    public class QdrantVectorDbService : IVectorDbService
    {
        private readonly QdrantClient _client;
        private readonly int _vectorSize;

        public QdrantVectorDbService(IConfiguration configuration)
        {
            var host = configuration["VectorDb:Endpoint"] ?? "http://localhost:6334";
            _client = new QdrantClient(new Uri(host));
            // Read dimension from config, default to 1536 (OpenAI).
            // Nomic should be 768.
            var dimConfig = configuration["EmbeddingConfig:Dimensions"];
            _vectorSize = int.TryParse(dimConfig, out var size) ? size : 1536; 
        }

        public async Task EnsureCollectionExistsAsync(string collectionName)
        {
            try 
            {
                var collections = await _client.ListCollectionsAsync();
                if (!collections.Contains(collectionName))
                {
                    await CreateCollectionInternal(collectionName);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[VectorDb] Error ensuring collection exists: {ex.Message}");
            }
        }

        private async Task CreateCollectionInternal(string collectionName)
        {
            await _client.CreateCollectionAsync(collectionName, new VectorParams { Size = (ulong)_vectorSize, Distance = Distance.Cosine });
            Console.WriteLine($"[VectorDb] Created collection '{collectionName}' with size {_vectorSize}");
        }

        public async Task UpsertAsync(string collectionName, IEnumerable<ProductChunk> chunks, IList<float[]> embeddings)
        {
            var points = new List<PointStruct>();
            var chunkList = chunks.ToList();

            for (int i = 0; i < chunkList.Count; i++)
            {
                var chunk = chunkList[i];
                var embedding = embeddings[i];
                var uuid = Guid.NewGuid(); // Or derive from chunk.Id

                var payload = new MapField<string, Value>();
                payload.Add("content", new Value { StringValue = chunk.Content });
                payload.Add("chunk_id", new Value { StringValue = chunk.Id });

                foreach (var kvp in chunk.Metadata)
                {
                    if (kvp.Value is string s) payload.Add(kvp.Key, new Value { StringValue = s });
                    else if (kvp.Value is int n) payload.Add(kvp.Key, new Value { IntegerValue = n });
                    else if (kvp.Value is double d) payload.Add(kvp.Key, new Value { DoubleValue = d });
                    else if (kvp.Value is bool b) payload.Add(kvp.Key, new Value { BoolValue = b });
                }

                var point = new PointStruct
                {
                    Id = new PointId { Uuid = uuid.ToString() }, 
                    Vectors = new Vectors { Vector = new Vector { Data = { embedding } } } 
                };
                point.Payload.Add(payload);
                points.Add(point);
            }

            if (points.Any())
            {
                // Ensure size mismatch doesn't crash upsert too?
                // Upsert might also fail if dimension is wrong. Let's catch it here too or let it fail?
                // Let's protect Upsert too as it writes data.
                try 
                {
                    await _client.UpsertAsync(collectionName, points);
                }
                catch (Grpc.Core.RpcException ex) when (ex.Status.Detail.Contains("Vector dimension error") || ex.Status.Detail.Contains("Wrong input: Vector dimension error"))
                {
                     Console.WriteLine($"[VectorDb] Dimension error during Upsert. Recreating collection...");
                     await _client.DeleteCollectionAsync(collectionName);
                     await CreateCollectionInternal(collectionName);
                     // Retry upsert
                     await _client.UpsertAsync(collectionName, points);
                }
            }
        }

        public async Task DeleteAsync(string collectionName, string id)
        {
            // Delete points where "product_id" matches the given id.
            if (long.TryParse(id, out var productId))
            {
                var filter = new Filter
                {
                    Must = {
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "product_id",
                                Match = new Match { Integer = productId }
                            }
                        }
                    }
                };

                await _client.DeleteAsync(collectionName, filter);
            }
        }

        public async Task<List<SearchResult>> SearchAsync(string collectionName, float[] vector, int limit = 10, Dictionary<string, object>? filter = null)
        {
            try
            {
                 // TODO: Implement Filters if neede
                var results = await _client.SearchAsync(
                    collectionName,
                    vector,
                    limit: (ulong)limit
                );
                return MapResults(results);
            }
            catch (Grpc.Core.RpcException ex) when (ex.Status.Detail.Contains("Vector dimension error") || ex.Status.Detail.Contains("Wrong input: Vector dimension error"))
            {
                Console.WriteLine($"[VectorDb] Dimension error during Search. Recreating collection (Data will be lost!)...");
                
                // Recreate triggers empty DB
                await _client.DeleteCollectionAsync(collectionName);
                await CreateCollectionInternal(collectionName);

                // Retry search (will return empty, but won't crash)
                var results = await _client.SearchAsync(
                    collectionName,
                    vector,
                    limit: (ulong)limit
                );
                return MapResults(results);
            }
        }

        private List<SearchResult> MapResults(IReadOnlyList<ScoredPoint> results)
        {
             return results.Select(r => new SearchResult
            {
                Id = r.Id.ToString(),
                Score = r.Score,
                Content = r.Payload.TryGetValue("content", out var contentVal) ? contentVal.StringValue : string.Empty,
                Metadata = r.Payload.ToDictionary(k => k.Key, k => UnpackValue(k.Value))
            }).ToList();
        }

        private object UnpackValue(Value value)
        {
            return value.KindCase switch
            {
                Value.KindOneofCase.StringValue => value.StringValue,
                Value.KindOneofCase.IntegerValue => value.IntegerValue,
                Value.KindOneofCase.DoubleValue => value.DoubleValue,
                Value.KindOneofCase.BoolValue => value.BoolValue,
                Value.KindOneofCase.ListValue => value.ListValue.Values.Select(UnpackValue).ToList(),
                _ => value.ToString()
            };
        }
    }
}
