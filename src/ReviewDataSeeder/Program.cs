using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using EcommerceLaptop.Infrastructure.Data;
using ReviewDataSeeder;

// Tạo configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

// Tạo host
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Đăng ký DbContext
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Đăng ký seeder
        services.AddScoped<ReviewSeeder>();
    })
    .Build();

Console.WriteLine("=== Bắt đầu seed Review data ===");
Console.WriteLine("--> Sẽ tạo 100 reviews tiếng Việt cho các sản phẩm");

try
{
    using var scope = host.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<ReviewSeeder>();

    await seeder.SeedReviewsAsync();

    Console.WriteLine("\n=== Hoàn thành seed review data! ===");
}
catch (Exception ex)
{
    Console.WriteLine($"### Lỗi khi seed data: {ex.Message}");
    Console.WriteLine($"Stack trace: {ex.StackTrace}");
}

Console.WriteLine("\nPress any key to exit...");
Console.ReadKey();
