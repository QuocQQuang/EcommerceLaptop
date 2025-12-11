using AutoMapper;
using EcommerceLaptop.API.DTOs;
using EcommerceLaptop.Core.Entities;
using CoreOrder = EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.DTOs.Admin;

namespace EcommerceLaptop.API.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // Product Image Mapping
        CreateMap<ProductImage, ProductImageDto>();

        // Inventory Mapping
        CreateMap<Inventory, InventoryDto>()
            .ForMember(d => d.ProductName, opt => opt.Ignore()); // Loaded separately or not needed

        // Bundle Item Mapping
        CreateMap<BundleItem, BundleItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty))
            .ForMember(d => d.ProductSku, opt => opt.MapFrom(s => s.Product != null ? s.Product.SKU : string.Empty))
            .ForMember(d => d.ProductImageUrl, opt => opt.MapFrom(s => s.Product != null && s.Product.Images != null 
                ? (s.Product.Images.FirstOrDefault(i => i.IsPrimary) ?? s.Product.Images.FirstOrDefault())!.ImageUrl 
                : null))
            .ForMember(d => d.Brand, opt => opt.MapFrom(s => s.Product != null ? s.Product.Brand : string.Empty))
            .ForMember(d => d.OriginalPrice, opt => opt.MapFrom(s => s.Product != null ? s.Product.Price : 0))
            .ForMember(d => d.DiscountedPrice, opt => opt.MapFrom(s => CalculateDiscountedPrice(s.Product != null ? s.Product.Price : 0, s.DiscountPercentage)))
            .ForMember(d => d.TotalPrice, opt => opt.MapFrom(s => CalculateDiscountedPrice(s.Product != null ? s.Product.Price : 0, s.DiscountPercentage) * s.Quantity))
            .ForMember(d => d.IsAvailable, opt => opt.MapFrom(s => s.Product != null ? s.Product.IsActive : false))
            .ForMember(d => d.StockQuantity, opt => opt.MapFrom(s => s.Product != null && s.Product.Inventory != null ? s.Product.Inventory.AvailableQuantity : 0));

        // Base Product Mapping
        CreateMap<Product, ProductDto>()
            .Include<Laptop, LaptopDto>()
            .Include<Accessory, AccessoryDto>()
            .Include<Bundle, BundleDto>()
            .ForMember(d => d.ProductType, opt => opt.MapFrom(s => s.GetType().Name))
            .ForMember(d => d.StockQuantity, opt => opt.MapFrom(s => s.Inventory != null ? s.Inventory.AvailableQuantity : 0))
            .ForMember(d => d.ImageUrl, opt => opt.MapFrom(s => s.Images != null 
                ? (s.Images.FirstOrDefault(i => i.IsPrimary) ?? s.Images.FirstOrDefault())!.ImageUrl 
                : null))
            .ForMember(d => d.Specifications, opt => opt.MapFrom((src, dest) => GenerateProductSpecifications(src)))
            .ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images ?? new List<ProductImage>()))
            .ForMember(d => d.Variants, opt => opt.MapFrom(s => s.Variants ?? new List<Product>()));

        // Laptop Mapping
        CreateMap<Laptop, LaptopDto>()
            .IncludeBase<Product, ProductDto>();

        // Accessory Mapping
        CreateMap<Accessory, AccessoryDto>()
            .IncludeBase<Product, ProductDto>();

        // Bundle Mapping
        CreateMap<Bundle, BundleDto>()
            .IncludeBase<Product, ProductDto>()
            .ForMember(d => d.BundleItems, opt => opt.MapFrom(s => s.BundleItems ?? new List<BundleItem>()));

        // Order Item Mapping
        CreateMap<OrderItem, CoreOrder.OrderItemDto>()
            .ForMember(d => d.ProductName, opt => opt.MapFrom(s => s.Product != null ? s.Product.Name : string.Empty));

        // Order Mapping
        CreateMap<Order, CoreOrder.OrderDto>()
            .ForMember(d => d.PaymentStatus, opt => opt.MapFrom(s => MapPaymentStatusForAdmin(s.Payments)))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.OrderItems));

        // Order Details Mapping
        CreateMap<Order, CoreOrder.OrderDetailsDto>()
            .IncludeBase<Order, CoreOrder.OrderDto>()
            .ForMember(d => d.CustomerId, opt => opt.MapFrom(s => s.UserId))
            .ForMember(d => d.CustomerEmail, opt => opt.MapFrom(s => s.User != null ? s.User.Email : string.Empty))
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.User != null ? $"{s.User.FirstName} {s.User.LastName}".Trim() : string.Empty))
            .ForMember(d => d.ShippingAddress, opt => opt.MapFrom(s => FormatShippingAddress(s)))
            .ForMember(d => d.EstimatedDeliveryDate, opt => opt.MapFrom(s => CalculateEstimatedDeliveryDate(s.CreatedAt)))
            .ForMember(d => d.AuditTrail, opt => opt.MapFrom(s => s.Audits));

        // Order Audit Mapping
        CreateMap<OrderAudit, CoreOrder.OrderAuditDto>()
            .ForMember(d => d.ChangedBy, opt => opt.MapFrom(s => s.ChangedByUser != null ? s.ChangedByUser.Email : "System"));

        // Admin Order Mapping
        CreateMap<Order, AdminOrderDto>()
            .ForMember(d => d.CustomerName, opt => opt.MapFrom(s => s.User != null ? $"{s.User.FirstName} {s.User.LastName}".Trim() : string.Empty))
            .ForMember(d => d.CustomerEmail, opt => opt.MapFrom(s => s.User != null ? s.User.Email : string.Empty))
            .ForMember(d => d.CustomerPhone, opt => opt.MapFrom(s => s.User != null ? s.User.PhoneNumber : string.Empty))
            .ForMember(d => d.ShippingAddress, opt => opt.MapFrom(s => FormatShippingAddress(s)))
            .ForMember(d => d.EstimatedDeliveryDate, opt => opt.MapFrom(s => CalculateEstimatedDeliveryDate(s.CreatedAt)))
            .ForMember(d => d.PaymentStatus, opt => opt.MapFrom(s => MapPaymentStatusForAdmin(s.Payments)))
            .ForMember(d => d.PaymentMethod, opt => opt.MapFrom(s => GetPaymentMethod(s.Payments)))
            .ForMember(d => d.Items, opt => opt.MapFrom(s => s.OrderItems));

        // Admin User Management Mapping
        CreateMap<User, AdminUserManagementDto>()
            .ForMember(d => d.FullName, opt => opt.MapFrom(s => $"{s.FirstName} {s.LastName}".Trim()))
            .ForMember(d => d.RoleId, opt => opt.MapFrom(s => s.UserRoles.FirstOrDefault(ur => ur.Role.IsAdminRole) != null ? s.UserRoles.FirstOrDefault(ur => ur.Role.IsAdminRole)!.RoleId : 0))
            .ForMember(d => d.RoleName, opt => opt.MapFrom(s => s.UserRoles.FirstOrDefault(ur => ur.Role.IsAdminRole) != null ? s.UserRoles.FirstOrDefault(ur => ur.Role.IsAdminRole)!.Role.Name : "Unknown"));
    }

    private static decimal CalculateDiscountedPrice(decimal originalPrice, decimal discountPercentage)
    {
        return originalPrice * (1 - discountPercentage / 100);
    }

    private List<ProductSpecificationDto> GenerateProductSpecifications(Product product)
    {
        var specifications = new List<ProductSpecificationDto>();
        int displayOrder = 0;

        switch (product)
        {
            case Laptop laptop:
                // CPU Specifications
                AddSpec(specifications, ref displayOrder, "CPU Brand", laptop.CpuBrand, "Processor");
                AddSpec(specifications, ref displayOrder, "CPU Model", laptop.CpuModel, "Processor");
                AddSpec(specifications, ref displayOrder, "CPU Cores", laptop.CpuCores > 0 ? laptop.CpuCores.ToString() : null, "Processor");
                AddSpec(specifications, ref displayOrder, "Base Clock", laptop.CpuBaseClockGHz > 0 ? $"{laptop.CpuBaseClockGHz} GHz" : null, "Processor");
                AddSpec(specifications, ref displayOrder, "Boost Clock", laptop.CpuBoostClockGHz > 0 ? $"{laptop.CpuBoostClockGHz} GHz" : null, "Processor");

                // RAM Specifications
                AddSpec(specifications, ref displayOrder, "RAM Type", laptop.RamType, "Memory");
                AddSpec(specifications, ref displayOrder, "RAM Capacity", laptop.RamCapacityGB > 0 ? $"{laptop.RamCapacityGB} GB" : null, "Memory");
                AddSpec(specifications, ref displayOrder, "RAM Speed", laptop.RamSpeed > 0 ? $"{laptop.RamSpeed} MHz" : null, "Memory");

                // Storage Specifications
                AddSpec(specifications, ref displayOrder, "Storage Type", laptop.StorageType, "Storage");
                AddSpec(specifications, ref displayOrder, "Storage Capacity", laptop.StorageCapacityGB > 0 ? $"{laptop.StorageCapacityGB} GB" : null, "Storage");

                // GPU Specifications
                AddSpec(specifications, ref displayOrder, "GPU Brand", laptop.GpuBrand, "Graphics");
                AddSpec(specifications, ref displayOrder, "GPU Model", laptop.GpuModel, "Graphics");
                AddSpec(specifications, ref displayOrder, "VRAM", laptop.GpuVramGB > 0 ? $"{laptop.GpuVramGB} GB" : null, "Graphics");

                // Display Specifications
                AddSpec(specifications, ref displayOrder, "Display Size", laptop.DisplaySizeInches > 0 ? $"{laptop.DisplaySizeInches}\"" : null, "Display");
                AddSpec(specifications, ref displayOrder, "Resolution", laptop.DisplayResolution, "Display");
                AddSpec(specifications, ref displayOrder, "Refresh Rate", laptop.DisplayRefreshRateHz > 0 ? $"{laptop.DisplayRefreshRateHz} Hz" : null, "Display");

                // Physical Specifications
                AddSpec(specifications, ref displayOrder, "Weight", laptop.WeightKg > 0 ? $"{laptop.WeightKg} kg" : null, "Physical");
                AddSpec(specifications, ref displayOrder, "Dimensions", laptop.Dimensions, "Physical");
                AddSpec(specifications, ref displayOrder, "Battery", laptop.BatteryCapacityWh > 0 ? $"{laptop.BatteryCapacityWh} Wh" : null, "Physical");
                break;

            case Accessory accessory:
                AddSpec(specifications, ref displayOrder, "Type", accessory.AccessoryType, "General");
                AddSpec(specifications, ref displayOrder, "Compatibility", accessory.Compatibility, "General");
                AddSpec(specifications, ref displayOrder, "Details", accessory.Specifications, "General");
                AddSpec(specifications, ref displayOrder, "Color", accessory.Color, "Design");
                AddSpec(specifications, ref displayOrder, "Connectivity", accessory.Connectivity, "Technical");
                break;

            case Bundle bundle:
                AddSpec(specifications, ref displayOrder, "Discount", bundle.DiscountPercentage > 0 ? $"{bundle.DiscountPercentage}%" : null, "Bundle");
                AddSpec(specifications, ref displayOrder, "Valid From", bundle.ValidFrom?.ToString("yyyy-MM-dd"), "Bundle");
                AddSpec(specifications, ref displayOrder, "Valid To", bundle.ValidTo?.ToString("yyyy-MM-dd"), "Bundle");
                break;
        }

        // Common specifications
        AddSpec(specifications, ref displayOrder, "Brand", product.Brand, "General");
        AddSpec(specifications, ref displayOrder, "Model", product.Model, "General");
        AddSpec(specifications, ref displayOrder, "SKU", product.SKU, "General");

        return specifications.OrderBy(s => s.Category).ThenBy(s => s.DisplayOrder).ToList();
    }

    private static void AddSpec(List<ProductSpecificationDto> specs, ref int order, string name, string? value, string category)
    {
        if (!string.IsNullOrEmpty(value))
        {
            order++;
            specs.Add(new ProductSpecificationDto 
            { 
                Id = order, 
                Name = name, 
                Value = value, 
                Category = category, 
                DisplayOrder = order 
            });
        }
    }

    private string MapPaymentStatusForAdmin(ICollection<EcommerceLaptop.Core.Entities.Payment>? payments)
    {
        var latestPayment = payments?.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        return latestPayment?.Status switch
        {
            PaymentStatus.Completed => "paid",
            PaymentStatus.Failed => "failed",
            PaymentStatus.Cancelled => "failed",
            PaymentStatus.Refunded => "refunded",
            PaymentStatus.Pending => "pending",
            null => "pending",
            _ => "pending"
        };
    }

    private string GetPaymentMethod(ICollection<EcommerceLaptop.Core.Entities.Payment>? payments)
    {
        var latestPayment = payments?.OrderByDescending(p => p.CreatedAt).FirstOrDefault();
        return latestPayment?.Gateway.ToString() ?? "Card"; // Default fallback
    }

    private string FormatShippingAddress(Order order)
    {
        var parts = new[] { order.ShippingStreet, order.ShippingCity, order.ShippingProvince, order.ShippingPostalCode, order.ShippingCountry }
            .Where(p => !string.IsNullOrWhiteSpace(p));
        return string.Join(", ", parts);
    }

    private DateTime? CalculateEstimatedDeliveryDate(DateTime orderDate)
    {
        // Simple calculation: 5-7 business days
        return orderDate.AddDays(5);
    }
}
