using System.Text.Json;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Seed;

public static class ProductDataSeeder
{
    public static void SeedFromJson(ApplicationDbContext context, ILogger logger)
    {
        // Check if we already have these products to avoid duplicates


        var jsonPath = Path.Combine(AppContext.BaseDirectory, "Seed", "Data", "laptops.json");
        if (!File.Exists(jsonPath))
        {
            // Try development path
            jsonPath = Path.Combine(Directory.GetCurrentDirectory(), "Seed", "Data", "laptops.json");
        }

        if (!File.Exists(jsonPath))
        {
            logger.LogWarning($"Laptop seed data file not found at {jsonPath}");
            return;
        }

        try
        {
            var jsonString = File.ReadAllText(jsonPath);
            var laptops = JsonSerializer.Deserialize<List<Laptop>>(jsonString);

            if (laptops == null || !laptops.Any())
            {
                logger.LogWarning("No laptops found in JSON seed file.");
                return;
            }

            // Ensure IDs don't conflict with existing ones (if any)
            var maxId = context.Products.Any() ? context.Products.Max(p => p.Id) : 0;
            
            foreach (var laptop in laptops)
            {
                // Check uniqueness by SKU
                if (context.Laptops.Any(p => p.SKU == laptop.SKU))
                {
                     continue; 
                }

                // Reset ID to allow database to generate it (Identity column)
                laptop.Id = 0;

                // IMPORTANT: Reset dates
                laptop.CreatedAt = DateTime.UtcNow;
                laptop.UpdatedAt = DateTime.UtcNow;

                context.Laptops.Add(laptop);
            }

            context.SaveChanges();
            logger.LogInformation($"Seeded new laptops from JSON.");

            // Also ensure Inventory exists for them
            EnsureInventory(context, laptops, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to seed laptops from JSON. Inner: {InnerMessage}", ex.InnerException?.Message);
            // Critical: Clear bad entities from tracker so subsequent seeders don't crash trying to save them again
            context.ChangeTracker.Clear();
        }
    }

    private static void EnsureInventory(ApplicationDbContext context, List<Laptop> laptops, ILogger logger)
    {
        var inventories = new List<Inventory>();
        foreach (var laptop in laptops)
        {
             if (context.Inventories.Any(i => i.ProductId == laptop.Id)) continue;
             
             inventories.Add(new Inventory
             {
                 ProductId = laptop.Id,
                 QuantityInStock = 50,
                 ReservedQuantity = 0,
                 ReorderLevel = 10,
                 MaxStockLevel = 100,
                 WarehouseLocation = "WH-JSON",
                 LastStockUpdate = DateTime.UtcNow
             });
        }

        if (inventories.Any())
        {
            context.Inventories.AddRange(inventories);
            context.SaveChanges();
            logger.LogInformation($"Seeded inventory for {inventories.Count} JSON laptops.");
        }
    }
}
