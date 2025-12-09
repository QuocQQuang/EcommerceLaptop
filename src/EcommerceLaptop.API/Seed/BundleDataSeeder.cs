using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Seed;

public static class BundleDataSeeder
{
    public static void SeedIfEmpty(ApplicationDbContext context, ILogger logger)
    {
        var hasAnyBundles = context.Bundles.Any();
        if (hasAnyBundles)
        {
            logger.LogInformation("Bundles already exist. Skipping bundle seeding.");
            return;
        }

        var seedDate = DateTime.UtcNow;
        var random = new Random(12345); // Fixed seed for consistent results

        // Get existing products
        var laptops = context.Laptops.ToList();
        var accessories = context.Accessories.ToList();

        if (!laptops.Any() || !accessories.Any())
        {
            logger.LogWarning("No laptops or accessories found. Cannot create bundles.");
            return;
        }

        var bundles = new List<Bundle>();
        var bundleItems = new List<BundleItem>();
        var inventories = new List<Inventory>();

        // Create 5 different bundles
        for (int i = 1; i <= 5; i++)
        {
            var laptop = laptops[random.Next(laptops.Count)];
            var selectedAccessories = accessories.OrderBy(_ => random.Next()).Take(random.Next(1, 4)).ToList();

            var bundleId = 100 + i;
            var bundleName = GetBundleName(i, laptop.Name);
            var bundleType = GetBundleType(i);
            var discountPercentage = GetDiscountPercentage(i);

            // Calculate bundle price (laptop + accessories with discount)
            var laptopPrice = laptop.Price;
            var accessoryPrices = selectedAccessories.Sum(a => a.Price);
            var totalPrice = laptopPrice + accessoryPrices;
            var discountedPrice = totalPrice * (1 - discountPercentage / 100);

            var bundle = new Bundle
            {
                Id = bundleId,
                Name = bundleName,
                Description = GetBundleDescription(i, laptop.Name, selectedAccessories),
                Brand = "TechStore",
                BrandId = 19, // TechStore ProductBrand ID
                CategoryId = 3, // Bundles category
                Model = $"BUNDLE-{i:000}",
                Price = Math.Round(discountedPrice, 2),
                SKU = $"BUNDLE-{i:000}",
                IsActive = true,
                CreatedAt = seedDate,
                UpdatedAt = seedDate,
                BundleType = bundleType,
                DiscountPercentage = discountPercentage,
                ValidFrom = seedDate,
                ValidTo = seedDate.AddDays(90) // Valid for 90 days
            };

            bundles.Add(bundle);

            // Add laptop as main item (no discount for laptop)
            bundleItems.Add(new BundleItem
            {
                Id = (i - 1) * 10 + 1,
                BundleId = bundleId,
                ProductId = laptop.Id,
                Quantity = 1,
                DiscountPercentage = 0 // No discount for main laptop
            });

            // Add accessories with varying discounts
            for (int j = 0; j < selectedAccessories.Count; j++)
            {
                var accessory = selectedAccessories[j];
                var accessoryDiscount = GetAccessoryDiscount(j, discountPercentage);

                bundleItems.Add(new BundleItem
                {
                    Id = (i - 1) * 10 + 2 + j,
                    BundleId = bundleId,
                    ProductId = accessory.Id,
                    Quantity = 1,
                    DiscountPercentage = accessoryDiscount
                });
            }

            // Create inventory for bundle
            inventories.Add(new Inventory
            {
                Id = 100 + i,
                ProductId = bundleId,
                QuantityInStock = random.Next(5, 20),
                ReservedQuantity = 0,
                ReorderLevel = 2,
                MaxStockLevel = 30,
                WarehouseLocation = $"WH-BUNDLE-{i:000}",
                LastStockUpdate = seedDate
            });
        }

        // Add to context
        context.Bundles.AddRange(bundles);
        context.BundleItems.AddRange(bundleItems);
        context.Inventories.AddRange(inventories);

        context.SaveChanges();

        logger.LogInformation("Seeded {BundleCount} bundles with {BundleItemCount} bundle items and {InventoryCount} inventory records.",
            bundles.Count, bundleItems.Count, inventories.Count);
    }

    private static string GetBundleName(int index, string laptopName)
    {
        var bundleNames = new[]
        {
            "Professional Workstation Bundle",
            "Gaming Powerhouse Bundle",
            "Creative Studio Bundle",
            "Business Productivity Bundle",
            "Student Essentials Bundle"
        };

        return $"{bundleNames[index - 1]} - {laptopName}";
    }

    private static string GetBundleType(int index)
    {
        var bundleTypes = new[] { "Professional", "Gaming", "Creative", "Business", "Student" };
        return bundleTypes[index - 1];
    }

    private static decimal GetDiscountPercentage(int index)
    {
        var discounts = new[] { 12.0m, 15.0m, 18.0m, 10.0m, 20.0m };
        return discounts[index - 1];
    }

    private static decimal GetAccessoryDiscount(int accessoryIndex, decimal bundleDiscount)
    {
        // First accessory gets higher discount, subsequent ones get less
        var multiplier = accessoryIndex == 0 ? 1.5m : accessoryIndex == 1 ? 1.2m : 0.8m;
        return Math.Min(bundleDiscount * multiplier, 30.0m); // Cap at 30%
    }

    private static string GetBundleDescription(int index, string laptopName, List<Accessory> accessories)
    {
        var descriptions = new[]
        {
            $"Complete professional setup featuring {laptopName} with essential productivity accessories. Perfect for office work, video conferencing, and business applications.",
            $"Ultimate gaming experience with {laptopName} paired with high-performance gaming accessories. Designed for competitive gaming and content creation.",
            $"Creative professional bundle including {laptopName} and specialized accessories for designers, developers, and content creators.",
            $"Efficient business solution combining {laptopName} with productivity-focused accessories. Ideal for corporate environments and remote work.",
            $"Student-friendly package with {laptopName} and essential accessories for learning, studying, and light productivity tasks."
        };

        var accessoryNames = string.Join(", ", accessories.Take(2).Select(a => a.Name));
        var baseDescription = descriptions[index - 1];

        if (accessories.Count > 2)
        {
            return $"{baseDescription} Includes: {accessoryNames} and {accessories.Count - 2} more accessories.";
        }
        else
        {
            return $"{baseDescription} Includes: {accessoryNames}.";
        }
    }
}



