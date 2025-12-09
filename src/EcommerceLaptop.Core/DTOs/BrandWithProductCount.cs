using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.DTOs;

public class BrandWithProductCount : ProductBrand
{
    public int ProductCount { get; set; }
}