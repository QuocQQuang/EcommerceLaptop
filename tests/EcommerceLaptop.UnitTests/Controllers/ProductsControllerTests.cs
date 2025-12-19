using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using AutoMapper;
using System.Text.Json;
using EcommerceLaptop.API.Controllers;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Services.Security;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.API.DTOs;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Inventory;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace EcommerceLaptop.UnitTests.Controllers
{
    public class ProductsControllerTests
    {
        private readonly Mock<IProductService> _mockProductService;
        private readonly Mock<IImageHostingService> _mockImageService;
        private readonly Mock<IAuditLoggingService> _mockAuditService;
        private readonly Mock<ILogger<ProductsController>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<IInventoryService> _mockInventoryService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly ProductsController _controller;

        public ProductsControllerTests()
        {
            _mockProductService = new Mock<IProductService>();
            _mockImageService = new Mock<IImageHostingService>();
            _mockAuditService = new Mock<IAuditLoggingService>();
            _mockLogger = new Mock<ILogger<ProductsController>>();
            _mockCache = new Mock<IMemoryCache>();
            _mockInventoryService = new Mock<IInventoryService>();
            _mockMapper = new Mock<IMapper>();

            _controller = new ProductsController(
                _mockProductService.Object,
                _mockImageService.Object,
                _mockAuditService.Object,
                _mockLogger.Object,
                _mockCache.Object,
                _mockInventoryService.Object,
                _mockMapper.Object
            );

            SetupControllerContext();
            SetupCacheMock();
        }

        private void SetupControllerContext()
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "Admin")
            }, "mock"));

            var httpContext = new DefaultHttpContext { User = user };
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        private void SetupCacheMock()
        {
            // Setup CreateEntry to return a mock entry so Set extension method works
            var mockCacheEntry = new Mock<ICacheEntry>();
            _mockCache.Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Returns(mockCacheEntry.Object);
        }

        [Fact]
        public async Task GetProduct_ExistingId_ReturnsOk()
        {
            // Arrange
            int productId = 1;
            var product = new Laptop { Id = productId, Name = "Test Laptop", IsActive = true };
            var productDto = new ProductDto { Id = productId, Name = "Test Laptop" };

            _mockProductService.Setup(x => x.GetByIdAsync(productId))
                .ReturnsAsync(product);
            _mockMapper.Setup(x => x.Map<ProductDto>(product))
                .Returns(productDto);

            // Act
            var result = await _controller.GetProduct(productId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<ProductDto>>().Subject;
            response.Data.Id.Should().Be(productId);
        }


        [Fact]
        public async Task GetProduct_NotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockProductService.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((Product?)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.GetProduct(999));
        }

        [Fact]
        public async Task GetProducts_ReturnsPaginatedList()
        {
            // Arrange
            var products = new List<Product> { new Laptop { Id = 1, Name = "P1" } };
            var pagedResult = new PagedResult<Product>
            {
                Items = products,
                TotalCount = 1,
                Page = 1,
                PageSize = 20
            };
            var productDtos = new List<ProductDto> { new ProductDto { Id = 1, Name = "P1" } };

            // Mock Cache miss to force service call
            object cacheValue;
            _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out cacheValue))
                .Returns(false);
            
            // Fix explicit argument matching for optional parameters
            _mockProductService.Setup(x => x.GetProductsAsync(
                It.IsAny<int>(), 
                It.IsAny<int>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<string>(), 
                It.IsAny<decimal?>(), 
                It.IsAny<decimal?>(), 
                It.IsAny<bool?>(), 
                It.IsAny<string>()))
                .ReturnsAsync(pagedResult);

            _mockMapper.Setup(x => x.Map<List<ProductDto>>(products))
                .Returns(productDtos);

            // Act
            var result = await _controller.GetProducts();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<PaginatedApiResponse<ProductDto>>().Subject;
            response.TotalCount.Should().Be(1);
        }

        [Fact]
        public async Task CreateProduct_ValidLaptop_ReturnsCreated()
        {
            // Arrange
            var laptopJson = JsonSerializer.Serialize(new
            {
                ProductType = "Laptop",
                Name = "New Laptop",
                Price = 1000m,
                SKU = "LAP-001",
                Brand = "Dell"
            });
            var jsonElement = JsonDocument.Parse(laptopJson).RootElement;
            
            var createdProduct = new Laptop { Id = 1, Name = "New Laptop", SKU = "LAP-001" };
            var productDto = new ProductDto { Id = 1, Name = "New Laptop" };

            _mockProductService.Setup(x => x.IsSKUUniqueAsync(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync(true);
            _mockProductService.Setup(x => x.CreateProductAsync(It.IsAny<Product>())).ReturnsAsync(createdProduct);
            _mockMapper.Setup(x => x.Map<ProductDto>(createdProduct)).Returns(productDto);

            // Act
            var result = await _controller.CreateProduct(jsonElement);

            // Assert
            var createdResult = result.Should().BeOfType<CreatedAtActionResult>().Subject;
            // The controller wraps the OkObjectResult inside the CreatedAtActionResult values, resulting in double wrapping
            var innerResult = createdResult.Value.Should().BeOfType<OkObjectResult>().Subject;
            var response = innerResult.Value.Should().BeAssignableTo<ApiResponse<ProductDto>>().Subject;
            response.Data.Name.Should().Be("New Laptop");
        }

        [Fact]
        public async Task CreateProduct_DuplicateSKU_ReturnsBadRequest()
        {
            // Arrange
            var laptopJson = JsonSerializer.Serialize(new
            {
                ProductType = "Laptop",
                Name = "Laptop",
                SKU = "EXISTING",
                Price = 100
            });
            var jsonElement = JsonDocument.Parse(laptopJson).RootElement;

            _mockProductService.Setup(x => x.IsSKUUniqueAsync(It.IsAny<string>(), It.IsAny<int?>())).ReturnsAsync(false);

            // Act
            var result = await _controller.CreateProduct(jsonElement);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequest.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Message.Should().Be("SKU already exists");
        }

        [Fact]
        public async Task DeleteProduct_ExistingId_ReturnsSuccess()
        {
            // Arrange
            int productId = 1;
            var product = new Laptop { Id = productId, Name = "To Delete" };
            _mockProductService.Setup(x => x.GetByIdAsync(productId)).ReturnsAsync(product);
            _mockProductService.Setup(x => x.DeleteProductAsync(productId)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteProduct(productId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            // Use dynamic to access anonymous type properties or verify specific structure
            // The actual type is ApiResponse<ImmutableAnonymousType> which is internal
            var value = okResult.Value;
            value.Should().NotBeNull();
            
            // Check that it has a Message property containing "success"
            var messageProperty = value.GetType().GetProperty("Message");
            messageProperty.Should().NotBeNull();
            var message = messageProperty.GetValue(value) as string;
            message.Should().Contain("deleted successfully");
        }
    }
}
