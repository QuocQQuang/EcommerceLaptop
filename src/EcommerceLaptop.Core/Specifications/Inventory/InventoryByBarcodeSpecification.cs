using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.InventorySpecs;

public class InventoryByBarcodeSpecification : BaseSpecification<Inventory>
{
    public InventoryByBarcodeSpecification(string barcode)
        : base(i => i.Product.Barcode == barcode)
    {
        AddInclude(i => i.Product);
    }
}
