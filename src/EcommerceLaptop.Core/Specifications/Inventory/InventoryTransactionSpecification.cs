using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.InventorySpecs;

public class InventoryTransactionSpecification : BaseSpecification<InventoryTransaction>
{
    public InventoryTransactionSpecification(int productId, DateTime? fromDate, DateTime? toDate)
        : base(t => 
            t.Inventory.ProductId == productId &&
            (!fromDate.HasValue || t.CreatedAt >= fromDate.Value) &&
            (!toDate.HasValue || t.CreatedAt <= toDate.Value))
    {
        AddInclude(t => t.Inventory);
        AddInclude(t => t.Inventory.Product);
        AddOrderByDescending(t => t.CreatedAt);
    }
    
     public InventoryTransactionSpecification(DateTime fromDate, DateTime toDate)
        : base(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate)
    {
        AddInclude(t => t.Inventory);
        AddInclude(t => t.Inventory.Product);
        AddOrderByDescending(t => t.CreatedAt);
    }
}
