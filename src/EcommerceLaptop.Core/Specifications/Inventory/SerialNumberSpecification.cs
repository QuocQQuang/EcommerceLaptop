using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.InventorySpecs;

public class SerialNumberSpecification : BaseSpecification<SerialNumber>
{
    public SerialNumberSpecification(int productId, bool activeOnly)
        : base(s => s.ProductId == productId && (!activeOnly || (s.Status == SerialNumberStatus.Available || s.Status == SerialNumberStatus.Reserved)))
    {
        // No Includes currently needed based on previous repo
    }
}
