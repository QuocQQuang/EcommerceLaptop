using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Repositories;

namespace EcommerceLaptop.UnitTests.Repositories
{
    public class ProductRepositoryTests
    {
        private readonly ApplicationDbContext _context;
        private readonly ProductRepository _repository;

        public ProductRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            
            _context = new ApplicationDbContext(options);
            _repository = new ProductRepository(_context);
        }

        [Fact]
        public async Task GetBrandsAsync_ReturnsDistinctActiveBrandsOrdered()
        {
            // Arrange
            _context.Products.AddRange(
                new Laptop { Name = "P1", Brand = "BrandA", IsActive = true, Price = 100 },
                new Laptop { Name = "P2", Brand = "BrandB", IsActive = true, Price = 100 },
                new Laptop { Name = "P3", Brand = "BrandA", IsActive = true, Price = 100 }, // Duplicate brand
                new Laptop { Name = "P4", Brand = "BrandC", IsActive = false, Price = 100 } // Inactive
            );
            await _context.SaveChangesAsync();

            // Act
            var brands = await _repository.GetBrandsAsync();

            // Assert
            brands.Should().HaveCount(2);
            brands.Should().ContainInOrder("BrandA", "BrandB");
            brands.Should().NotContain("BrandC");
        }

        [Fact]
        public async Task GetRelatedProductsAsync_SameBrand_ReturnsRelatedProducts()
        {
            // Arrange
            var mainProduct = new Laptop { Id = 1, Name = "Main", Brand = "BrandX", IsActive = true, Price = 1000 };
            var related1 = new Laptop { Id = 2, Name = "Related1", Brand = "BrandX", IsActive = true, Price = 1000 };
            var related2 = new Laptop { Id = 3, Name = "Related2", Brand = "BrandX", IsActive = true, Price = 1000 };
            var other = new Laptop { Id = 4, Name = "Other", Brand = "BrandY", IsActive = true, Price = 1000 };
            var inactive = new Laptop { Id = 5, Name = "Inactive", Brand = "BrandX", IsActive = false, Price = 1000 };

            _context.Products.AddRange(mainProduct, related1, related2, other, inactive);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetRelatedProductsAsync(1, 10);

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(p => p.Id == 2);
            result.Should().Contain(p => p.Id == 3);
            result.Should().NotContain(p => p.Id == 1); // Should not contain self
            result.Should().NotContain(p => p.Id == 4); // Different brand
            result.Should().NotContain(p => p.Id == 5); // Inactive
        }
    }
}
