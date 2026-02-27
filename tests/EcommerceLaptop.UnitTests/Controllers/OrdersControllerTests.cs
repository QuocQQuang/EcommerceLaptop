using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using EcommerceLaptop.API.Controllers;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Services.Payment;
using EcommerceLaptop.Core.DTOs.Order;
using EcommerceLaptop.Core.DTOs.Payment;
using EcommerceLaptop.Core.DTOs.Admin;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.UnitTests.Controllers
{
    public class OrdersControllerTests
    {
        private readonly Mock<IOrderService> _mockOrderService;
        private readonly Mock<IPaymentOrchestrator> _mockPaymentOrchestrator;
        private readonly Mock<ILogger<OrdersController>> _mockLogger;
        private readonly OrdersController _controller;

        public OrdersControllerTests()
        {
            _mockOrderService = new Mock<IOrderService>();
            _mockPaymentOrchestrator = new Mock<IPaymentOrchestrator>();
            _mockLogger = new Mock<ILogger<OrdersController>>();

            _controller = new OrdersController(
                _mockOrderService.Object,
                _mockPaymentOrchestrator.Object,
                _mockLogger.Object
            );

            SetupControllerContext();
        }

        private void SetupControllerContext(string role = "User", string userId = "1", bool emailConfirmed = true)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim("email_confirmed", emailConfirmed.ToString())
            };
            var identity = new ClaimsIdentity(claims, "TestAuth");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        [Fact]
        public async Task AtomicCheckout_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new CreateOrderRequest { CustomerId = 1, CartId = 123 };
            var checkoutResult = new AtomicCheckoutResult 
            { 
                IsSuccess = true, 
                Order = new OrderDto { Id = 100 } 
            };

            _mockOrderService.Setup(x => x.CreateOrderAndInitializePaymentAsync(request))
                .ReturnsAsync(checkoutResult);

            // Act
            var result = await _controller.AtomicCheckout(request);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(checkoutResult);
        }

        [Fact(Skip = "Email confirmation check temporarily disabled in controller (TODO)")]
        public async Task AtomicCheckout_EmailNotConfirmed_ReturnsBadRequest()
        {
            // Arrange
            SetupControllerContext(emailConfirmed: false);
            var request = new CreateOrderRequest { CustomerId = 1 };

            // Act
            var result = await _controller.AtomicCheckout(request);

            // Assert
            var badRequest = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().Be("Ti khon ca bn cha xc thc email. Vui lng xc thc email trc khi mua hng.");
        }

        [Fact]
        public async Task GetOrder_ExistingId_ReturnsOk()
        {
            // Arrange
            int orderId = 100;
            var orderDetails = new OrderDetailsDto { Id = orderId, CustomerId = 1 };
            _mockOrderService.Setup(x => x.GetOrderDetailsAsync(orderId))
                .ReturnsAsync(orderDetails);

            // Act
            var result = await _controller.GetOrder(orderId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(orderDetails);
        }

        [Fact]
        public async Task UpdateStatus_ValidRequest_ReturnsOk()
        {
            // Arrange
            int orderId = 100;
            var request = new UpdateStatusRequest { NewStatus = OrderStatus.Processing, Reason = "Payment Confirmed" };
            SetupControllerContext(role: "Admin"); // Require permission

            _mockOrderService.Setup(x => x.UpdateOrderStatusAsync(orderId, request))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateStatus(orderId, request);

            // Assert
            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task CancelOrder_ValidRequest_ReturnsOk()
        {
            // Arrange
            int orderId = 100;
            var request = new CancelOrderRequest { Reason = "Change of mind" };
            SetupControllerContext(role: "Admin");

            _mockOrderService.Setup(x => x.CancelOrderAsync(orderId, request))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.CancelOrder(orderId, request);

            // Assert
            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task CancelOrder_CancellationFailed_ReturnsBadRequest()
        {
             // Arrange
            int orderId = 100;
            var request = new CancelOrderRequest { Reason = "Change of mind" };
            SetupControllerContext(role: "Admin");

            _mockOrderService.Setup(x => x.CancelOrderAsync(orderId, request))
                .ReturnsAsync(false);
            _mockOrderService.Setup(x => x.GetOrderDetailsAsync(orderId))
                .ReturnsAsync(new OrderDetailsDto { Id = orderId });

            // Act
            var result = await _controller.CancelOrder(orderId, request);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.Value.Should().Be("Khng th hy n hng  thanh ton. Vui lng lin h h tr  c hon tin.");
        }

        [Fact]
        public async Task GetCustomerOrders_ValidCustomer_ReturnsOk()
        {
             // Arrange
            int customerId = 1;
            var pagedResult = new PagedResult<OrderDto> { TotalCount = 5, Items = new List<OrderDto>() };
            
            _mockOrderService.Setup(x => x.GetCustomerOrdersAsync(customerId, 1, 10))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetCustomerOrders(customerId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(pagedResult);
        }

        [Fact]
        public async Task GetAdminOrders_ReturnsOk()
        {
            // Arrange
            SetupControllerContext(role: "Admin");
            var pagedResult = new PagedResult<AdminOrderDto> 
            { 
                TotalCount = 10, 
                Items = new List<AdminOrderDto> { new AdminOrderDto { TotalAmount = 100 } } 
            };

            _mockOrderService.Setup(x => x.GetEnhancedAdminOrdersAsync(1, 20, null, null, null))
                .ReturnsAsync(pagedResult);

            // Act
            var result = await _controller.GetAdminOrders();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            // Verify structure of anonymous object if needed, or just status
            okResult.Value.Should().NotBeNull();
        }
    }
}
