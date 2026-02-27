using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Repositories;
using EcommerceLaptop.Core.DTOs.Inventory;

namespace EcommerceLaptop.UnitTests.Repositories
{
    public class InventoryRepositoryTests
    {
        private readonly ApplicationDbContext _context;
        private readonly InventoryRepository _repository;

        public InventoryRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            
            _context = new ApplicationDbContext(options);
            _repository = new InventoryRepository(_context);
        }

        [Fact]
        public async Task GetInventoriesAsync_FilterByProductName_ReturnsMatchingInventories()
        {
            // Arrange
            var prod1 = new Laptop { Name = "Gaming Laptop", SKU = "SKU1", Brand = "BrandA", IsActive = true, Price = 1000 };
            var prod2 = new Laptop { Name = "Office Laptop", SKU = "SKU2", Brand = "BrandB", IsActive = true, Price = 500 };
            
            _context.Products.AddRange(prod1, prod2);
            await _context.SaveChangesAsync();

            var inv1 = new Inventory { ProductId = prod1.Id, QuantityInStock = 10, WarehouseLocation = "A1", RowVersion = new byte[8] };
            var inv2 = new Inventory { ProductId = prod2.Id, QuantityInStock = 5, WarehouseLocation = "B1", RowVersion = new byte[8] };

            _context.Inventories.AddRange(inv1, inv2);
            await _context.SaveChangesAsync();

            var filter = new InventoryFilterRequest { ProductName = "Gaming", Page = 1, PageSize = 10 };

            // Act
            var result = await _repository.GetInventoriesAsync(filter);

            // Assert
            result.Items.Should().HaveCount(1);
            result.Items.First().ProductName.Should().Be("Gaming Laptop");
        }

        [Fact]
        public async Task GetLowStockAsync_ReturnsItemsBelowThreshold()
        {
             // Arrange
            var prod1 = new Laptop { Name = "P1", Price = 100, IsActive = true };
            var prod2 = new Laptop { Name = "P2", Price = 100, IsActive = true };
             _context.Products.AddRange(prod1, prod2);
            await _context.SaveChangesAsync();

            // ReorderLevel = 10. 
            // Inv1: Qty 5 (Low)
            // Inv2: Qty 15 (OK)
            var inv1 = new Inventory { ProductId = prod1.Id, QuantityInStock = 5, ReorderLevel = 10, RowVersion = new byte[8] };
            var inv2 = new Inventory { ProductId = prod2.Id, QuantityInStock = 15, ReorderLevel = 10, RowVersion = new byte[8] };

            _context.Inventories.AddRange(inv1, inv2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetLowStockAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().ProductId.Should().Be(prod1.Id);
        }

        [Fact]
        public async Task TransactionMethods_DoNotThrowWithInMemory()
        {
            // Act & Assert
            await _repository.BeginTransactionAsync();
            await _repository.CommitTransactionAsync();
            // Should not throw due to warning suppression
        }
    }
}
