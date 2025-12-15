using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcommerceLaptop.Core.Interfaces
{
    public interface IVectorDbService
    {
        Task EnsureCollectionExistsAsync(string collectionName);
        Task UpsertAsync(string collectionName, IEnumerable<ProductChunk> chunks, IList<float[]> embeddings);
        Task<List<SearchResult>> SearchAsync(string collectionName, float[] vector, int limit = 10, Dictionary<string, object>? filter = null);
    }

    public class SearchResult
    {
        public string Id { get; set; } = string.Empty;
        public float Score { get; set; }
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
