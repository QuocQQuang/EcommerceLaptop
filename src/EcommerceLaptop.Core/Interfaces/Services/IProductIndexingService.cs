using System.Threading.Tasks;

namespace EcommerceLaptop.Core.Interfaces.Services;

public interface IProductIndexingService
{
    Task IndexProductAsync(int productId);
    Task DeleteProductAsync(int productId);
    Task ExecuteAsync(); // Keep the full re-index method exposed if needed via interface, or just referencing the job class for full re-index.
}
