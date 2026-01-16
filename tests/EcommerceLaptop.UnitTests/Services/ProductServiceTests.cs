using System;
using EcommerceLaptop.Core.Common;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Infrastructure.Services;
using EcommerceLaptop.Core.DomainEvents;
using EcommerceLaptop.Core.Specifications;

using EcommerceLaptop.Core.Services;

namespace EcommerceLaptop.UnitTests.Services
{
    public class ProductServiceTests
    {
        private readonly Mock<IProductRepository> _mockProductRepo;
        private readonly Mock<IAsyncRepository<Laptop>> _mockLaptopRepo;
        private readonly Mock<IAsyncRepository<Accessory>> _mockAccessoryRepo;
        private readonly Mock<IAsyncRepository<Bundle>> _mockBundleRepo;
        private readonly Mock<IAsyncRepository<ProductImage>> _mockImageRepo;
        private readonly Mock<IAsyncRepository<OrderItem>> _mockOrderItemRepo;
        private readonly Mock<IDomainEventDispatcher> _mockDispatcher;
        private readonly Mock<IProductSearchService> _mockProductSearchService;
        private readonly ProductService _service;

        public ProductServiceTests()
        {
            _mockProductRepo = new Mock<IProductRepository>();
            _mockLaptopRepo = new Mock<IAsyncRepository<Laptop>>();
            _mockAccessoryRepo = new Mock<IAsyncRepository<Accessory>>();
            _mockBundleRepo = new Mock<IAsyncRepository<Bundle>>();
            _mockImageRepo = new Mock<IAsyncRepository<ProductImage>>();
            _mockOrderItemRepo = new Mock<IAsyncRepository<OrderItem>>();
            _mockDispatcher = new Mock<IDomainEventDispatcher>();
            _mockProductSearchService = new Mock<IProductSearchService>();

            _service = new ProductService(
                _mockProductRepo.Object,
                _mockLaptopRepo.Object,
                _mockAccessoryRepo.Object,
                _mockBundleRepo.Object,
                _mockImageRepo.Object,
                _mockOrderItemRepo.Object,
                _mockDispatcher.Object,
                _mockProductSearchService.Object
            );
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsProduct()
        {
            // Arrange
            var productId = 1;
            var product = new Laptop { Id = productId, Name = "Test Laptop" };
            _mockProductRepo.Setup(x => x.GetEntityWithSpec(It.IsAny<ISpecification<Product>>()))
                .ReturnsAsync(product);

            // Act
            var result = await _service.GetByIdAsync(productId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(productId);
            result.Name.Should().Be("Test Laptop");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            _mockProductRepo.Setup(x => x.GetEntityWithSpec(It.IsAny<ISpecification<Product>>()))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _service.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateProductAsync_ValidProduct_ReturnsCreatedProductAndDispatchesEvent()
        {
            // Arrange
            var product = new Laptop { Name = "New Laptop", Price = 999 };
            _mockProductRepo.Setup(x => x.AddAsync(product)).ReturnsAsync(product);

            // Act
            var result = await _service.CreateProductAsync(product);

            // Assert
            result.Should().Be(product);
            _mockDispatcher.Verify(x => x.DispatchAsync(It.IsAny<ProductCreatedEvent>()), Times.Once);
        }

        [Fact]
        public async Task UpdateProductAsync_ValidProduct_UpdatesAndDispatchesEvent()
        {
            // Arrange
            var product = new Laptop { Id = 1, Name = "Updated Laptop" };
            _mockProductRepo.Setup(x => x.UpdateAsync(product)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.UpdateProductAsync(product);

            // Assert
            result.Name.Should().Be("Updated Laptop");
            _mockProductRepo.Verify(x => x.UpdateAsync(product), Times.Once);
            _mockDispatcher.Verify(x => x.DispatchAsync(It.IsAny<ProductUpdatedEvent>()), Times.Once);
        }

        [Fact]
        public async Task DeleteProductAsync_ExistingId_ReturnsTrueAndDispatchesEvent()
        {
            // Arrange
            var productId = 1;
            var product = new Laptop { Id = productId, IsActive = true };
            _mockProductRepo.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
            _mockProductRepo.Setup(x => x.UpdateAsync(product)).Returns(Task.CompletedTask);

            // Act
            var result = await _service.DeleteProductAsync(productId);

            // Assert
            result.Should().BeTrue();
            product.IsActive.Should().BeFalse(); // Soft delete check
            _mockProductRepo.Verify(x => x.UpdateAsync(product), Times.Once);
            _mockDispatcher.Verify(x => x.DispatchAsync(It.IsAny<ProductDeletedEvent>()), Times.Once);
        }

        [Fact]
        public async Task DeleteProductAsync_NonExistingId_ReturnsFalse()
        {
            // Arrange
            _mockProductRepo.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((Product?)null);

            // Act
            var result = await _service.DeleteProductAsync(999);

            // Assert
            result.Should().BeFalse();
            _mockProductRepo.Verify(x => x.UpdateAsync(It.IsAny<Product>()), Times.Never);
            _mockDispatcher.Verify(x => x.DispatchAsync(It.IsAny<IDomainEvent>()), Times.Never);
        }
    }
}
