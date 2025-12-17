using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceLaptop.API.Seed;

public static class DataInitializer
{
    public static void Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DataInitializer");

        // Apply pending migrations (create DB if missing)
        context.Database.Migrate();

        // Cleanup v seed SystemSettings (ch Email & Notifications)
        SystemSettingsCleanupSeeder.MigrateKeysToSnakeCase(context, logger);
        SystemSettingsConsolidatedSeeder.Seed(context, logger);

        // Seed security-related data
        SecurityDataSeeder.SeedIfEmpty(context, logger);

        // Seed demo customers, inventory, and 100 orders
        OrderDemoDataSeeder.SeedIfEmpty(context, logger);

        // Seed extended product data from JSON (Laptops from IDs 10+)
        ProductDataSeeder.SeedFromJson(context, logger);

        // Normalize laptop specifications to canonical sets for easier filtering
        LaptopSpecificsSeeder.NormalizeSpecs(context, logger);

        // Seed variants for laptops before creating bundle products
        VariantDataSeeder.SeedIfMissing(context, logger);

        // Seed bundle data with laptops and accessories
        BundleDataSeeder.SeedIfEmpty(context, logger);

        // Seed product images for laptops missing images
        ProductImagesSeeder.SeedLaptopsWithoutImages(context, logger);
    }
}


