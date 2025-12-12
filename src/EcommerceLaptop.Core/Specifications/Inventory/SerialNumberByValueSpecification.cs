using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.Specifications.InventorySpecs;

public class SerialNumberByValueSpecification : BaseSpecification<SerialNumber>
{
    public SerialNumberByValueSpecification(string value)
        : base(s => s.Value == value)
    {
    }
}
