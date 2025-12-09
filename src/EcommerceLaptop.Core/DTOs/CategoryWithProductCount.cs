using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Core.DTOs;

public class CategoryWithProductCount : ProductCategory
{
    public int ProductCount { get; set; }
    public new List<CategoryWithProductCount> Children { get; set; } = new();
}