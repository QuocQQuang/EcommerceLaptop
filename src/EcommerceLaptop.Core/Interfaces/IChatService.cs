using System.Collections.Generic;
using System.Threading.Tasks;
using EcommerceLaptop.Core.Models.AI;

namespace EcommerceLaptop.Core.Interfaces
{
    public interface IChatService
    {
        IAsyncEnumerable<ChatResponseChunk> ProcessMessageAsync(ChatRequest request);
    }
}
