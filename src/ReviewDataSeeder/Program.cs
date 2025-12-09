using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EcommerceLaptop.Infrastructure.Data;
using ReviewDataSeeder;

// To configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// To host
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // ng k DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // ng k seeder
        services.AddScoped<ReviewSeeder>();
    })
    .Build();

Console.WriteLine(" Bt u seed Review data...");
Console.WriteLine(" S to 100 reviews ting Vit cho cc sn phm");

try
{
    using var scope = host.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<ReviewSeeder>();

    await seeder.SeedReviewsAsync();

    Console.WriteLine("\n Hon thnh seed review data!");
}
catch (Exception ex)
{
    Console.WriteLine($" Li khi seed data: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();