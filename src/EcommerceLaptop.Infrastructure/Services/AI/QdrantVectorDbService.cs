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
            var host = configuration["VectorDb:Endpoint"] ?? "http://localhost:6333";
            _client = new QdrantClient(new Uri(host));
            _vectorSize = 1536; // OpenAI text-embedding-3-small
        }

        public async Task EnsureCollectionExistsAsync(string collectionName)
        {
            var collections = await _client.ListCollectionsAsync();
            if (!collections.Contains(collectionName))
            {
                await _client.CreateCollectionAsync(collectionName, new VectorParams { Size = (ulong)_vectorSize, Distance = Distance.Cosine });
            }
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
                await _client.UpsertAsync(collectionName, points);
            }
        }

        public async Task RemoveAsync(string collectionName, string id)
        {
            // Delete all points with matching product_id in payload
            // Since we use random UUIDs for point IDs, we filter by product_id metadata
            var filter = new Filter
            {
                Must =
                {
                    new Condition
                    {
                        Field = new FieldCondition
                        {
                            Key = "product_id",
                            Match = new Match { Integer = int.Parse(id) }
                        }
                    }
                }
            };

            await _client.DeleteAsync(collectionName, filter);
        }

        public async Task<List<SearchResult>> SearchAsync(string collectionName, float[] vector, int limit = 10, Dictionary<string, object>? filter = null)
        {
            // TODO: Implement Filters if needed

            var results = await _client.SearchAsync(
                collectionName,
                vector,
                limit: (ulong)limit
            );

            return results.Select(r => new SearchResult
            {
                Id = r.Id.ToString(),
                Score = r.Score,
                Content = r.Payload.TryGetValue("content", out var contentVal) ? contentVal.StringValue : string.Empty,
                Metadata = r.Payload.ToDictionary(k => k.Key, k => (object)k.Value.ToString()!) // Simplified payload conversion
            }).ToList();
        }
    }
}
