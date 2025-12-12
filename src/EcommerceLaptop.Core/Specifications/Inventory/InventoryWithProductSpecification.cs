using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.InventorySpecs;

public class InventoryWithProductSpecification : BaseSpecification<Inventory>
{
    public InventoryWithProductSpecification()
    {
        AddInclude(i => i.Product);
    }
    
    public InventoryWithProductSpecification(int productId) 
        : base(i => i.ProductId == productId)
    {
        AddInclude(i => i.Product);
    }
}
