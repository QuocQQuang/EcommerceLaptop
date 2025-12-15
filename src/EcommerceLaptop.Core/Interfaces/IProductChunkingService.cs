using System.Collections.Generic;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Interfaces
{
    public interface IProductChunkingService
    {
        IEnumerable<ProductChunk> ChunkProduct(Product product);
    }

    public class ProductChunk
    {
        public string Id { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }
}
