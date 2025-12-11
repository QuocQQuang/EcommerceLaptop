using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Linq.Expressions;
using EcommerceLaptop.Core.DTOs.Inventory; // Added namespace

namespace EcommerceLaptop.UnitTests.Services
{
    public class StockManagementServiceTests
    {
        private readonly Mock<IInventoryRepository> _mockRepository;
        private readonly Mock<ILogger<StockManagementService>> _mockLogger;
        private readonly StockManagementService _service;

        public StockManagementServiceTests()
        {
            _mockRepository = new Mock<IInventoryRepository>();
            _mockLogger = new Mock<ILogger<StockManagementService>>();
            _service = new StockManagementService(_mockRepository.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task AdjustStockAsync_ShouldAddStock_WhenQuantityIsPositive()
        {
            // Arrange
            var inventory = new Inventory { ProductId = 1, QuantityInStock = 10 };
            _mockRepository.Setup(r => r.GetByProductIdAsync(1)).ReturnsAsync(inventory);
            
            var request = new StockAdjustmentRequest
            {
                ProductId = 1,
                Quantity = 5,
                Reference = "Adj-1",
                Notes = "Restock"
            };

            // Act
            await _service.AdjustStockAsync(request);

            // Assert
            inventory.QuantityInStock.Should().Be(15);
            _mockRepository.Verify(r => r.UpdateAsync(inventory), Times.Once);
        }

        [Fact]
        public async Task AdjustStockAsync_ShouldRemoveStock_WhenQuantityIsNegative()
        {
            // Arrange
            var inventory = new Inventory { ProductId = 1, QuantityInStock = 10 };
            _mockRepository.Setup(r => r.GetByProductIdAsync(1)).ReturnsAsync(inventory);

            var request = new StockAdjustmentRequest
            {
                ProductId = 1,
                Quantity = -5,
                Reference = "Adj-2",
                Notes = "Correction"
            };

            // Act
            await _service.AdjustStockAsync(request);

            // Assert
            inventory.QuantityInStock.Should().Be(5);
            _mockRepository.Verify(r => r.UpdateAsync(inventory), Times.Once);
        }

        [Fact]
        public async Task TransferStockAsync_ShouldMoveStockBetweenLocations()
        {
            // Arrange
            var fromInventory = new Inventory { ProductId = 1, QuantityInStock = 10, WarehouseLocation = "WH-A" };
            var toInventory = new Inventory { ProductId = 1, QuantityInStock = 5, WarehouseLocation = "WH-B" };

            // Mock GetAsync to return correct list based on expression logic simulation
            // We use a callback or returns list of everything, assuming service filters? 
            // Actually service calls GetAsync(i => i.ProductId == ... && i.WarehouseLocation == ...) which is specific.
            
            _mockRepository.Setup(r => r.GetAsync(It.IsAny<Expression<Func<Inventory, bool>>>()))
                .ReturnsAsync((Expression<Func<Inventory, bool>> predicate) => 
                {
                    var list = new List<Inventory> { fromInventory, toInventory };
                    return list.AsQueryable().Where(predicate).ToList();
                });

            var request = new StockTransferRequest
            {
                ProductId = 1,
                FromWarehouse = "WH-A",
                ToWarehouse = "WH-B",
                Quantity = 3,
                Reference = "Transfer-1"
            };

            // Act
            await _service.TransferStockAsync(request);

            // Assert
            fromInventory.QuantityInStock.Should().Be(7);
            toInventory.QuantityInStock.Should().Be(8);
            _mockRepository.Verify(r => r.UpdateAsync(fromInventory), Times.Once);
            _mockRepository.Verify(r => r.UpdateAsync(toInventory), Times.Once);
        }

        private IReadOnlyList<Inventory> newList(Inventory item)
        {
            return new List<Inventory> { item }.AsReadOnly();
        }
    }
}
