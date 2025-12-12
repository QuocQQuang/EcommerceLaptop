using EcommerceLaptop.Core.DTOs.Inventory;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Specifications;

namespace EcommerceLaptop.Core.Specifications.InventorySpecs;

public class InventoryFilterSpecification : BaseSpecification<Inventory>
{
    public InventoryFilterSpecification(InventoryFilterRequest request, bool isPagingEnabled = true)
        : base(i =>
            (string.IsNullOrEmpty(request.ProductName) || i.Product.Name.Contains(request.ProductName)) &&
            (string.IsNullOrEmpty(request.ProductSKU) || i.Product.SKU.Contains(request.ProductSKU)) &&
            (string.IsNullOrEmpty(request.WarehouseLocation) || i.WarehouseLocation.Contains(request.WarehouseLocation)) &&
            (!request.IsLowStock.HasValue || !request.IsLowStock.Value || i.QuantityInStock <= i.ReorderLevel) &&
            (!request.MinQuantity.HasValue || i.QuantityInStock >= request.MinQuantity.Value) &&
            (!request.MaxQuantity.HasValue || i.QuantityInStock <= request.MaxQuantity.Value)
        )
    {
        AddInclude(i => i.Product);

        if (isPagingEnabled)
        {
            ApplyPaging((request.Page - 1) * request.PageSize, request.PageSize);
        }

        if (!string.IsNullOrEmpty(request.SortBy))
        {
            switch (request.SortBy.ToLower())
            {
                case "productname":
                    if (request.SortDirection?.ToUpper() == "DESC")
                        AddOrderByDescending(i => i.Product.Name);
                    else
                        AddOrderBy(i => i.Product.Name);
                    break;
                case "quantity":
                    if (request.SortDirection?.ToUpper() == "DESC")
                        AddOrderByDescending(i => i.QuantityInStock);
                    else
                        AddOrderBy(i => i.QuantityInStock);
                    break;
                case "laststockupdate":
                    if (request.SortDirection?.ToUpper() == "DESC")
                        AddOrderByDescending(i => i.LastStockUpdate);
                    else
                        AddOrderBy(i => i.LastStockUpdate);
                    break;
                case "warehouse":
                    if (request.SortDirection?.ToUpper() == "DESC")
                        AddOrderByDescending(i => i.WarehouseLocation);
                    else
                        AddOrderBy(i => i.WarehouseLocation);
                    break;
                default:
                    AddOrderBy(i => i.Product.Name);
                    break;
            }
        }
        else
        {
            AddOrderBy(i => i.Product.Name);
        }
    }
}
