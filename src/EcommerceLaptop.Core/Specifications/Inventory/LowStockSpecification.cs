using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Specifications;

namespace EcommerceLaptop.Core.Specifications.InventorySpecs;

public class LowStockSpecification : BaseSpecification<Inventory>
{
    public LowStockSpecification(int thresholdMultiplier = 1)
        : base(i => i.QuantityInStock <= i.ReorderLevel * thresholdMultiplier)
    {
        AddInclude(i => i.Product);
    }
}
