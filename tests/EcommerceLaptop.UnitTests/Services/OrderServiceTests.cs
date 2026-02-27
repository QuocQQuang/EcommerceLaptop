using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using AutoMapper;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Infrastructure.Services;
using EcommerceLaptop.Infrastructure.Data;
using AddressVO = EcommerceLaptop.Core.ValueObjects.Address;

namespace EcommerceLaptop.UnitTests.Services
{
    public class OrderServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<IInventoryReservationService> _mockInventoryService;
        private readonly Mock<IEmailService> _mockEmailService;
        private readonly Mock<IOrderWorkflowService> _mockWorkflowService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<OrderService>> _mockLogger;
        private readonly OrderService _service;

        public OrderServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            
            _context = new ApplicationDbContext(options);
            _mockInventoryService = new Mock<IInventoryReservationService>();
            _mockEmailService = new Mock<IEmailService>();
            _mockWorkflowService = new Mock<IOrderWorkflowService>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<OrderService>>();

            _service = new OrderService(
                _context,
                _mockInventoryService.Object,
                _mockEmailService.Object,
                _mockWorkflowService.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task CreateOrderFromCartAsync_ValidCart_CreatesOrder()
        {
            // Arrange
            var userId = 1;
            var cartId = 1;
            var product = new Laptop { Id = 1, Name = "Laptop", Price = 1000, IsActive = true };
            var cart = new ShoppingCart { Id = cartId, UserId = userId, IsActive = true };
            var cartItem = new CartItem { Id = 1, ProductId = 1, Product = product, Quantity = 1, UnitPrice = 1000 };
            cart.CartItems.Add(cartItem);

            _context.Users.Add(new User { Id = userId, Email = "test@test.com", FirstName = "Test", LastName = "User" });
            _context.Products.Add(product);
            _context.Inventories.Add(new Inventory { ProductId = 1, QuantityInStock = 10, ReservedQuantity = 0, RowVersion = new byte[8] });
            _context.ShoppingCarts.Add(cart);
            await _context.SaveChangesAsync();

            _mockInventoryService.Setup(x => x.ValidateCartItemsAvailabilityAsync(It.IsAny<IEnumerable<CartItem>>()))
                .ReturnsAsync(new InventoryValidationResult { IsValid = true, Errors = new List<string>() });
            
            _mockInventoryService.Setup(x => x.ReserveInventoryInternalAsync(It.IsAny<int>()))
                .ReturnsAsync(true);

            _mockMapper.Setup(x => x.Map<OrderDto>(It.IsAny<Order>()))
                .Returns((Order order) => new OrderDto 
                { 
                    Id = order.Id, 
                    OrderNumber = order.OrderNumber, 
                    Status = order.Status, 
                    TotalAmount = order.TotalAmount, 
                    CreatedAt = order.CreatedAt,
                    Items = new List<OrderItemDto>() 
                });

            // Act
            var result = await _service.CreateOrderFromCartAsync(cartId, userId, "123 Street, City");

            // Assert
            result.Should().NotBeNull();
            var savedOrder = await _context.Orders.Include(o => o.OrderItems).FirstOrDefaultAsync(o => o.Id == result.Id);
            savedOrder.Should().NotBeNull();
            savedOrder!.UserId.Should().Be(userId);
            savedOrder.OrderItems.Should().HaveCount(1);
            
            // Inventory reservation is now done directly via DbContext (not via IInventoryReservationService)
            // Email is sent via fire-and-forget Task.Run so we don't assert Times.Once here
        }

        [Fact]
        public async Task CreateOrderFromCartAsync_InactiveCart_ThrowsException()
        {
            // Arrange
            var userId = 1;
            var cartId = 1;
            var cart = new ShoppingCart { Id = cartId, UserId = userId, IsActive = false };
            _context.ShoppingCarts.Add(cart);
            await _context.SaveChangesAsync();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => 
                _service.CreateOrderFromCartAsync(cartId, userId, "123 Street"));
        }

        [Fact]
        public async Task GetOrderDetailsAsync_ExistingId_ReturnsOrder()
        {
            // Arrange
            var userId = 1;
            _context.Users.Add(new User { Id = userId, Email = "test@test.com", FirstName = "Test", LastName = "User" });
            
            var order = Order.Create(userId, "ORD-TEST", new AddressVO("Street", "City", "Province", "12345", "Country"));
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();
            
            var expectedDto = new OrderDetailsDto 
            { 
                Id = order.Id, 
                OrderNumber = order.OrderNumber, 
                Status = order.Status, 
                TotalAmount = order.TotalAmount, 
                TaxAmount = order.TaxAmount, 
                ShippingCost = order.ShippingAmount, 
                CreatedAt = order.CreatedAt,
                ShippingAddress = order.ShippingAddress.ToString(),
                Items = new List<OrderItemDto>(),
                AuditTrail = new List<OrderAuditDto>()
            };

            _mockMapper.Setup(x => x.Map<OrderDetailsDto>(It.IsAny<Order>()))
                .Returns(expectedDto);

            // Act
            var result = await _service.GetOrderDetailsAsync(order.Id);

            // Assert
            result.Should().BeEquivalentTo(expectedDto);
        }
    }
}
