using EcommerceLaptop.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EcommerceLaptop.IntegrationTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove DbContext internals (Options and Context itself)
            // This removes IConfigureOptions<DbContextOptions> which prevents SqlServer configuration
            var descriptors = services.Where(d =>
                d.ServiceType.Name.Contains("DbContextOptions") ||
                d.ServiceType == typeof(ApplicationDbContext)).ToList();
            
            foreach (var d in descriptors)
            {
                services.Remove(d);
            }
            
            // Remove connection if any
            var dbConnectionDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(System.Data.Common.DbConnection));
            if (dbConnectionDescriptor != null) services.Remove(dbConnectionDescriptor);
            
            // Add ApplicationDbContext using an in-memory database
            services.AddDbContext<ApplicationDbContext>((container, options) =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting")
                       .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });

            // Build the service provider.
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context (ApplicationDbContext).
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                
                // Ensure the database is created.
                db.Database.EnsureCreated();
            }
        });
    }
}
