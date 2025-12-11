using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Interfaces;

public interface IProductRepository : IAsyncRepository<Product>
{
    Task<IEnumerable<string>> GetBrandsAsync();
    Task<IEnumerable<Product>> GetRelatedProductsAsync(int productId, int count);
}
