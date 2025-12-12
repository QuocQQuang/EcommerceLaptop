using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.InventorySpecs;

public class InventoryByProductSpecification : BaseSpecification<Inventory>
{
    public InventoryByProductSpecification(int productId)
        : base(i => i.ProductId == productId)
    {
        AddInclude(i => i.Product);
    }
}
