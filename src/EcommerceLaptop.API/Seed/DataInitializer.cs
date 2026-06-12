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

        try
        {
            // Step 1: Apply pending migrations (create DB and tables if missing)
            logger.LogInformation("Applying database migrations...");
            context.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to apply database migrations. Application may not work correctly.");
            // Don't continue if migrations fail
            throw;
        }

        // Step 2: Run seeders with try-catch for each to prevent one failure from stopping others
        SafeExecuteSeeder(() => SystemSettingsCleanupSeeder.MigrateKeysToSnakeCase(context, logger),
            "SystemSettingsCleanupSeeder", logger);

        SafeExecuteSeeder(() => SystemSettingsConsolidatedSeeder.Seed(context, logger),
            "SystemSettingsConsolidatedSeeder", logger);

        SafeExecuteSeeder(() => SecurityDataSeeder.SeedIfEmpty(context, logger),
            "SecurityDataSeeder", logger);

        SafeExecuteSeeder(() => OrderDemoDataSeeder.SeedIfEmpty(context, logger),
            "OrderDemoDataSeeder", logger);

        SafeExecuteSeeder(() => ProductDataSeeder.SeedFromJson(context, logger),
            "ProductDataSeeder", logger);

        SafeExecuteSeeder(() => LaptopSpecificsSeeder.NormalizeSpecs(context, logger),
            "LaptopSpecificsSeeder", logger);

        SafeExecuteSeeder(() => VariantDataSeeder.SeedIfMissing(context, logger),
            "VariantDataSeeder", logger);

        SafeExecuteSeeder(() => BlogDataSeeder.SeedIfMissing(context, logger),
            "BlogDataSeeder", logger);

        SafeExecuteSeeder(() => BundleDataSeeder.SeedIfEmpty(context, logger),
            "BundleDataSeeder", logger);

        SafeExecuteSeeder(() => ProductImagesSeeder.SeedLaptopsWithoutImages(context, logger),
            "ProductImagesSeeder", logger);

        logger.LogInformation("Data initialization completed.");
    }

    private static void SafeExecuteSeeder(Action seederAction, string seederName, ILogger logger)
    {
        try
        {
            logger.LogInformation("Running {SeederName}...", seederName);
            seederAction();
            logger.LogInformation("{SeederName} completed successfully.", seederName);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "{SeederName} failed but continuing with other seeders. Error: {Message}",
                seederName, ex.Message);
            // Continue with other seeders instead of crashing the app
        }
    }
}
