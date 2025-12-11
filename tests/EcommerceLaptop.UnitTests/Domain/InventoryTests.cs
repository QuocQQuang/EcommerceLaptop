using EcommerceLaptop.Core.Entities;
using FluentAssertions;
using Xunit;

namespace EcommerceLaptop.UnitTests.Domain
{
    public class InventoryTests
    {
        [Fact]
        public void ReserveStock_ShouldDecreaseAvailable_AndIncreaseReserved()
        {
            // Arrange
            var inventory = new Inventory
            {
                ProductId = 1,
                QuantityInStock = 10,
                ReservedQuantity = 0
            };

            // Act
            inventory.ReserveStock(2, "Order-1", "Reason", 1);

            // Assert
            inventory.ReservedQuantity.Should().Be(2);
            inventory.QuantityInStock.Should().Be(10);
            inventory.AvailableQuantity.Should().Be(8);
        }

        [Fact]
        public void ReserveStock_ShouldThrow_WhenInsufficientStock()
        {
            // Arrange
            var inventory = new Inventory
            {
                ProductId = 1,
                QuantityInStock = 5,
                ReservedQuantity = 0
            };

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => inventory.ReserveStock(10, "Order-1", "Reason", 1));
        }

        [Fact]
        public void ConfirmReservation_ShouldDecreaseStock_AndDecreaseReserved()
        {
            // Arrange
            var inventory = new Inventory
            {
                ProductId = 1,
                QuantityInStock = 10,
                ReservedQuantity = 2
            };

            // Act
            inventory.ConfirmReservation(2, "Order-1", "Sold", 1);

            // Assert
            inventory.ReservedQuantity.Should().Be(0);
            inventory.QuantityInStock.Should().Be(8);
            inventory.AvailableQuantity.Should().Be(8);
        }

        [Fact]
        public void CancelReservation_ShouldDecreaseReserved_AndKeepStock()
        {
            // Arrange
            var inventory = new Inventory
            {
                ProductId = 1,
                QuantityInStock = 10,
                ReservedQuantity = 2
            };

            // Act
            inventory.CancelReservation(2, "Order-1", "Cancelled", 1);

            // Assert
            inventory.ReservedQuantity.Should().Be(0);
            inventory.QuantityInStock.Should().Be(10);
            inventory.AvailableQuantity.Should().Be(10);
        }

        [Fact]
        public void AddStock_ShouldIncreaseStock()
        {
            // Arrange
            var inventory = new Inventory
            {
                ProductId = 1,
                QuantityInStock = 10
            };

            // Act
            inventory.AddStock(5, "Ref-1", "Restock", 1);

            // Assert
            inventory.QuantityInStock.Should().Be(15);
        }

        [Fact]
        public void RemoveStock_ShouldDecreaseStock()
        {
            // Arrange
            var inventory = new Inventory
            {
                ProductId = 1,
                QuantityInStock = 10
            };

            // Act
            inventory.RemoveStock(5, "Ref-1", "Adjustment", 1);

            // Assert
            inventory.QuantityInStock.Should().Be(5);
        }
    }
}
