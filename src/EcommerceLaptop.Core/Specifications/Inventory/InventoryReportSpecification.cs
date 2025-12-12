using EcommerceLaptop.Core.DTOs.Inventory;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.InventorySpecs;

public class InventoryReportSpecification : BaseSpecification<Inventory>
{
    public InventoryReportSpecification(InventoryReportRequest request)
        : base(i =>
            (!request.FromDate.HasValue || i.LastStockUpdate >= request.FromDate.Value) &&
            (!request.ToDate.HasValue || i.LastStockUpdate <= request.ToDate.Value) &&
            (string.IsNullOrEmpty(request.WarehouseLocation) || i.WarehouseLocation == request.WarehouseLocation) &&
            (string.IsNullOrEmpty(request.Brand) || i.Product.Brand == request.Brand)
        )
    {
        AddInclude(i => i.Product);
    }
}
