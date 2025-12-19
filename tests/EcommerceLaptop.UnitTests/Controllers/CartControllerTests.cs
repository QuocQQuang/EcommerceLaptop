using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using EcommerceLaptop.API.Controllers;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs.Cart;

namespace EcommerceLaptop.UnitTests.Controllers
{
    public class CartControllerTests
    {
        private readonly Mock<IShoppingCartService> _mockCartService;
        private readonly Mock<ILogger<CartController>> _mockLogger;
        private readonly CartController _controller;
        private const string TestUserId = "test-user-id";

        public CartControllerTests()
        {
            _mockCartService = new Mock<IShoppingCartService>();
            _mockLogger = new Mock<ILogger<CartController>>();
            _controller = new CartController(_mockCartService.Object, _mockLogger.Object);
            
            // Setup default authenticated user context
            SetupControllerContext(TestUserId);
        }

        private void SetupControllerContext(string? userId)
        {
            var claims = new List<Claim>();
            if (!string.IsNullOrEmpty(userId))
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));
            }
            
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);
            
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = claimsPrincipal }
            };
        }

        #region GetCart Tests

        [Fact]
        public async Task GetCart_WithValidUserId_ReturnsOk()
        {
            // Arrange
            var expectedCart = new CartResponseDto();
            _mockCartService.Setup(x => x.GetCartAsync(TestUserId, null))
                .ReturnsAsync(expectedCart);

            // Act
            var result = await _controller.GetCart();

            // Assert
            var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            actionResult.Value.Should().Be(expectedCart);
        }

        [Fact]
        public async Task GetCart_WithValidSessionId_ReturnsOk()
        {
            // Arrange
            SetupControllerContext(null); // Unauthenticated
            var sessionId = "test-session";
            var expectedCart = new CartResponseDto();
            _mockCartService.Setup(x => x.GetCartAsync(null, sessionId))
                .ReturnsAsync(expectedCart);

            // Act
            var result = await _controller.GetCart(sessionId);

            // Assert
            var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            actionResult.Value.Should().Be(expectedCart);
        }

        [Fact]
        public async Task GetCart_NoAuthOrSession_ReturnsBadRequest()
        {
            // Arrange
            SetupControllerContext(null); // Unauthenticated

            // Act
            var result = await _controller.GetCart(null);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Either authentication or session ID is required");
        }

        #endregion

        #region AddToCart Tests

        [Fact]
        public async Task AddToCart_ValidItem_ReturnsOk()
        {
            // Arrange
            var dto = new AddToCartDto { ProductId = 1, Quantity = 1 };
            var expectedCart = new CartResponseDto();
            _mockCartService.Setup(x => x.AddToCartAsync(dto, TestUserId))
                .ReturnsAsync(expectedCart);

            // Act
            var result = await _controller.AddToCart(dto);

            // Assert
            var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            actionResult.Value.Should().Be(expectedCart);
        }

        [Fact]
        public async Task AddToCart_MissingAuthAndSession_ReturnsBadRequest()
        {
            // Arrange
            SetupControllerContext(null);
            var dto = new AddToCartDto { ProductId = 1, Quantity = 1 }; // No SessionId

            // Act
            var result = await _controller.AddToCart(dto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        #endregion

        #region UpdateCartItem Tests

        [Fact]
        public async Task UpdateCartItem_ValidUpdate_ReturnsOk()
        {
            // Arrange
            var dto = new UpdateCartItemDto { CartItemId = 1, Quantity = 2 };
            var expectedCart = new CartResponseDto();
            _mockCartService.Setup(x => x.UpdateCartItemAsync(dto, TestUserId))
                .ReturnsAsync(expectedCart);

            // Act
            var result = await _controller.UpdateCartItem(dto);

            // Assert
            var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            actionResult.Value.Should().Be(expectedCart);
        }

        #endregion

        #region RemoveFromCart Tests

        [Fact]
        public async Task RemoveFromCart_ValidRequest_ReturnsOk()
        {
            // Arrange
            var dto = new RemoveFromCartDto { CartItemId = 1 };
            var expectedCart = new CartResponseDto();
            _mockCartService.Setup(x => x.RemoveFromCartAsync(dto, TestUserId))
                .ReturnsAsync(expectedCart);

            // Act
            var result = await _controller.RemoveFromCart(dto);

            // Assert
            var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            actionResult.Value.Should().Be(expectedCart);
        }

        [Fact]
        public async Task RemoveCartItem_ById_ReturnsOk()
        {
            // Arrange
            int cartItemId = 99;
            var expectedCart = new CartResponseDto();
            _mockCartService.Setup(x => x.RemoveFromCartAsync(It.Is<RemoveFromCartDto>(d => d.CartItemId == cartItemId), TestUserId))
                .ReturnsAsync(expectedCart);

            // Act
            var result = await _controller.RemoveCartItem(cartItemId);

            // Assert
            var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            actionResult.Value.Should().Be(expectedCart);
        }

        #endregion

        #region ClearCart Tests

        [Fact]
        public async Task ClearCart_ValidRequest_ReturnsOk()
        {
            // Arrange
            var expectedCart = new CartResponseDto();
            _mockCartService.Setup(x => x.ClearCartAsync(TestUserId, null))
                .ReturnsAsync(expectedCart);

            // Act
            var result = await _controller.ClearCart();

            // Assert
            var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            actionResult.Value.Should().Be(expectedCart);
        }

        #endregion

        #region Discount Tests

        [Fact]
        public async Task ApplyDiscount_ValidCode_ReturnsOk()
        {
            // Arrange
            var dto = new ApplyDiscountDto { DiscountCode = "SAVE10" };
            var expectedCart = new CartResponseDto();
            _mockCartService.Setup(x => x.ApplyDiscountAsync(dto, TestUserId))
                .ReturnsAsync(expectedCart);

            // Act
            var result = await _controller.ApplyDiscount(dto);

            // Assert
            var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            actionResult.Value.Should().Be(expectedCart);
        }

        [Fact]
        public async Task RemoveDiscount_ReturnsOk()
        {
            // Arrange
            var expectedCart = new CartResponseDto();
            _mockCartService.Setup(x => x.RemoveDiscountAsync(TestUserId, null))
                .ReturnsAsync(expectedCart);

            // Act
            var result = await _controller.RemoveDiscount();

            // Assert
            var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            actionResult.Value.Should().Be(expectedCart);
        }

        #endregion

        #region Shipping Tests

        [Fact]
        public async Task CalculateShipping_ValidAddress_ReturnsCost()
        {
            // Arrange
            string address = "123 Test St";
            decimal expectedCost = 50.0m;
            _mockCartService.Setup(x => x.CalculateShippingAsync(TestUserId, null, address))
                .ReturnsAsync(expectedCost);

            // Act
            var result = await _controller.CalculateShipping(address);

            // Assert
            var actionResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            actionResult.Value.Should().Be(expectedCost);
        }

        [Fact]
        public async Task CalculateShipping_MissingAddress_ReturnsBadRequest()
        {
            // Act
            var result = await _controller.CalculateShipping("");

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>()
                .Which.Value.Should().Be("Shipping address is required");
        }

        #endregion
    }
}
