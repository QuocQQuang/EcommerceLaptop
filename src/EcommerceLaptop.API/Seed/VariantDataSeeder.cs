using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace EcommerceLaptop.API.Seed;

public static class VariantDataSeeder
{
    public static void SeedIfMissing(ApplicationDbContext context, ILogger logger)
    {
        // Only generate variants for base laptops that have no variants yet
        var baseLaptops = context.Laptops
            .AsNoTracking()
            .Where(p => p.ParentProductId == null)
            .Select(p => new { p.Id, p.Name, p.Price, p.SKU, p.Model, p.BrandId, p.CategoryId })
            .ToList();

        if (!baseLaptops.Any())
        {
            logger.LogInformation("No base laptops found for variant generation.");
            return;
        }

        var existingVariantParents = context.Products
            .AsNoTracking()
            .Where(p => p.ParentProductId != null)
            .Select(p => p.ParentProductId!.Value)
            .Distinct()
            .ToHashSet();

        var now = DateTime.UtcNow;
        var newVariants = new List<Product>();

        foreach (var baseP in baseLaptops)
        {
            if (existingVariantParents.Contains(baseP.Id))
            {
                // Variants already exist for this base product
                continue;
            }

            // Define a simple matrix of variant options
            var ramOptions = new[] { (label: "16GB RAM", priceDelta: 50m), (label: "32GB RAM", priceDelta: 120m) };
            var storageOptions = new[] { (label: "512GB SSD", priceDelta: 40m), (label: "1TB SSD", priceDelta: 100m) };

            int variantIndex = 0;
            foreach (var ram in ramOptions)
            {
                foreach (var ssd in storageOptions)
                {
                    variantIndex++;
                    var variantName = $"{ram.label} + {ssd.label}";
                    var variantSku = $"{baseP.SKU}-{ram.label.Replace(" ", string.Empty).Replace("+", "")}-{ssd.label.Replace(" ", string.Empty)}".ToUpperInvariant();
                    var variantPrice = Math.Max(0, baseP.Price + ram.priceDelta + ssd.priceDelta);

                    newVariants.Add(new Laptop
                    {
                        ParentProductId = baseP.Id,
                        Name = $"{baseP.Name} - {variantName}",
                        Description = variantName,
                        Brand = "", // keep base Brand string unused; BrandId used
                        BrandId = baseP.BrandId,
                        CategoryId = baseP.CategoryId,
                        Model = baseP.Model,
                        Price = variantPrice,
                        SKU = variantSku,
                        VariantName = variantName,
                        VariantSku = variantSku,
                        IsActive = true,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }
            }
        }

        if (newVariants.Count == 0)
        {
            logger.LogInformation("No variants needed. Skipping variant seeding.");
            return;
        }

        context.Products.AddRange(newVariants);
        context.SaveChanges();
        logger.LogInformation("Seeded {Count} product variants for {Parents} base laptops.", newVariants.Count, newVariants.Select(v => v.ParentProductId).Distinct().Count());

        // Ensure each newly created variant has an inventory record
        var variantProductIds = newVariants.Select(v => v.Id).ToList();
        if (variantProductIds.Count > 0)
        {
            var existingVariantInventories = context.Inventories
                .AsNoTracking()
                .Where(i => variantProductIds.Contains(i.ProductId))
                .Select(i => i.ProductId)
                .ToHashSet();

            var inventoriesToAdd = new List<Inventory>();
            foreach (var pid in variantProductIds)
            {
                if (existingVariantInventories.Contains(pid)) continue;
                inventoriesToAdd.Add(new Inventory
                {
                    ProductId = pid,
                    QuantityInStock = 100,
                    ReservedQuantity = 0,
                    ReorderLevel = 10,
                    MaxStockLevel = 200,
                    WarehouseLocation = "WH-VARIANT",
                    LastStockUpdate = DateTime.UtcNow
                });
            }

            if (inventoriesToAdd.Count > 0)
            {
                context.Inventories.AddRange(inventoriesToAdd);
                context.SaveChanges();
                logger.LogInformation("Seeded inventory for {Count} variants.", inventoriesToAdd.Count);
            }
        }
    }
}


