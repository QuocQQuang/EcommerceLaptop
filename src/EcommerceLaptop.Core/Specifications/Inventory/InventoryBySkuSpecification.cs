using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.InventorySpecs;

public class InventoryBySkuSpecification : BaseSpecification<Inventory>
{
    public InventoryBySkuSpecification(string sku)
        : base(i => i.Product.SKU == sku)
    {
        AddInclude(i => i.Product);
    }
}
