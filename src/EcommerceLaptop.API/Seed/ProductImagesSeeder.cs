using EcommerceLaptop.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Seed;

public static class ProductImagesSeeder
{
    public static void SeedLaptopsWithoutImages(ApplicationDbContext context, ILogger logger)
    {
        var laptopsWithoutImages = context.Laptops
            .Select(l => new { l.Id, l.Name })
            .Where(l => !context.ProductImages.Any(pi => pi.ProductId == l.Id))
            .ToList();

        if (!laptopsWithoutImages.Any())
        {
            logger.LogInformation("All laptops already have images. Skipping image seeding.");
            return;
        }

        var imageUrls = new List<(string Url, string Alt)>
        {
            ("https://images.unsplash.com/photo-1517336714731-489689fd1ca8?q=80&w=1600&auto=format&fit=crop", "Laptop on desk"),
            ("https://images.unsplash.com/photo-1518770660439-4636190af475?q=80&w=1600&auto=format&fit=crop", "Coding on laptop"),
            ("https://images.unsplash.com/photo-1519389950473-47ba0277781c?q=80&w=1600&auto=format&fit=crop", "Workspace with laptop"),
            ("https://images.unsplash.com/photo-1498050108023-c5249f4df085?q=80&w=1600&auto=format&fit=crop", "Developer laptop"),
            ("https://images.unsplash.com/photo-1498050108023-3e3b5b3a5f59?q=80&w=1600&auto=format&fit=crop", "Laptop with peripherals"),
            // Added URLs from request
            ("https://images.unsplash.com/photo-1496181133206-80ce9b88a853?q=80&w=2071&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D", "Laptop workspace overhead"),
            ("https://images.unsplash.com/photo-1630794180018-433d915c34ac?q=80&w=1332&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D", "Modern laptop setup"),
            ("https://images.unsplash.com/photo-1636211993589-6daf32038bd1?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D", "Laptop with plant"),
            ("https://images.unsplash.com/photo-1710787554722-c3abdde09c44?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D", "Sleek laptop close-up"),
            ("https://plus.unsplash.com/premium_photo-1681666713728-9ed75e148617?q=80&w=1170&auto=format&fit=crop&ixlib=rb-4.1.0&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D", "Premium laptop photo")
        };

        var urlCount = imageUrls.Count;
        var newImages = new List<EcommerceLaptop.Core.Entities.ProductImage>();

        foreach (var p in laptopsWithoutImages)
        {
            for (var k = 0; k < 3; k++)
            {
                var idx = (p.Id - 1 + k) % urlCount;
                var (url, alt) = imageUrls[idx];
                newImages.Add(new EcommerceLaptop.Core.Entities.ProductImage
                {
                    ProductId = p.Id,
                    ImageUrl = url,
                    AltText = string.IsNullOrWhiteSpace(p.Name) ? "Laptop photo" : p.Name + " photo",
                    SortOrder = k,
                    DisplayOrder = k,
                    IsPrimary = k == 0,
                    ImageId = null,
                    DeleteUrl = null
                });
            }
        }

        context.ProductImages.AddRange(newImages);
        context.SaveChanges();
        logger.LogInformation("Seeded product images for {Count} laptops (3 each)", laptopsWithoutImages.Count);
    }
}


