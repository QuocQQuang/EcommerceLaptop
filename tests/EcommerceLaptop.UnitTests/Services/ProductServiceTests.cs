using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

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
        // private readonly Mock<ILogger<ProductService>> _mockLogger; // Not in constructor based on file usage (check usage)
        private readonly ProductService _service;

        public ProductServiceTests()
        {
            _mockProductRepo = new Mock<IProductRepository>();
            _mockLaptopRepo = new Mock<IAsyncRepository<Laptop>>();
            _mockAccessoryRepo = new Mock<IAsyncRepository<Accessory>>();
            _mockBundleRepo = new Mock<IAsyncRepository<Bundle>>();
            _mockImageRepo = new Mock<IAsyncRepository<ProductImage>>();
            _mockOrderItemRepo = new Mock<IAsyncRepository<OrderItem>>();
            var mockDispatcher = new Mock<IDomainEventDispatcher>();
            
            _service = new ProductService(
                _mockProductRepo.Object,
                _mockLaptopRepo.Object,
                _mockAccessoryRepo.Object,
                _mockBundleRepo.Object,
                _mockImageRepo.Object,
                _mockOrderItemRepo.Object,
                mockDispatcher.Object
            );
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnProduct_WhenExists()
        {
            // Arrange
            var product = new Laptop { Id = 1, Name = "Laptop A" };
            _mockProductRepo.Setup(r => r.GetEntityWithSpec(It.IsAny<ISpecification<Product>>()))
                .ReturnsAsync(product);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Laptop A");
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
        {
            // Arrange
            _mockProductRepo.Setup(r => r.GetEntityWithSpec(It.IsAny<ISpecification<Product>>()))
                .ReturnsAsync((Product?)null);

            // Act
            var result = await _service.GetByIdAsync(1);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetProductsAsync_ShouldReturnPagedResult()
        {
             // Arrange
            var products = new List<Product>
            {
                new Laptop { Id = 1, Name = "Laptop A" },
                new Accessory { Id = 2, Name = "Mouse B" }
            };
            
            _mockProductRepo.Setup(r => r.GetAsync(It.IsAny<ISpecification<Product>>()))
                .ReturnsAsync(products);
            
            _mockProductRepo.Setup(r => r.CountAsync(It.IsAny<ISpecification<Product>>()))
                .ReturnsAsync(2);

            // Act
            var result = await _service.GetProductsAsync(page: 1, pageSize: 10);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(2);
            result.TotalCount.Should().Be(2);
        }
    }
}
