using System.Collections.Generic;
using System.Threading.Tasks;

namespace EcommerceLaptop.Core.Interfaces
{
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<IList<float[]>> GenerateEmbeddingsAsync(IList<string> texts);
    }
}
