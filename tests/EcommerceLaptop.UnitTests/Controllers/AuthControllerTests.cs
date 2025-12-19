using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using EcommerceLaptop.API.Controllers;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.API.DTOs;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Services.Security;
using EcommerceLaptop.Core.DTOs.Admin;

namespace EcommerceLaptop.UnitTests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IAuditLoggingService> _mockAuditService;
        private readonly Mock<ILogger<AuthController>> _mockLogger;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _mockAuthService = new Mock<IAuthService>();
            _mockAuditService = new Mock<IAuditLoggingService>();
            _mockLogger = new Mock<ILogger<AuthController>>();

            _controller = new AuthController(
                _mockAuthService.Object,
                _mockAuditService.Object,
                _mockLogger.Object
            );

            SetupControllerContext();
        }

        private void SetupControllerContext(string userId = "1")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim("context", "Customer")
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
        public async Task Login_ValidCredentials_ReturnsOk()
        {
            // Arrange
            var request = new UnifiedLoginRequest("test@example.com", "password");
            
            var userDto = new UnifiedUserDto(
                1, "test@example.com", "First", "Last", null, null, true, true, DateTime.Now, null, null, UserType.Customer, new List<EcommerceLaptop.Core.Entities.RoleDto>(), new List<AdminPermissionDto>(), 0, null, null
            );

            var authResult = new UnifiedAuthResult(
                true, null, "token", "refresh", DateTime.Now.AddHours(1), 3600, userDto, AuthContext.Customer
            );

            _mockAuthService.Setup(x => x.AuthenticateAsync(request.Email, request.Password, It.IsAny<AuthContext>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(authResult);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { success = true, data = authResult });
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            var request = new UnifiedLoginRequest("test@example.com", "wrong");
            var authResult = new UnifiedAuthResult(
                false, "Invalid credentials", null, null, default, 0, null, AuthContext.Customer
            );

            _mockAuthService.Setup(x => x.AuthenticateAsync(request.Email, request.Password, It.IsAny<AuthContext>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(authResult);

            // Act
            var result = await _controller.Login(request);

            // Assert
            var unauthorizedResult = result.Should().BeOfType<UnauthorizedObjectResult>().Subject;
            unauthorizedResult.Value.Should().BeEquivalentTo(new { success = false, error = "Invalid credentials" });
        }

        [Fact]
        public async Task Register_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new UnifiedRegisterRequest { Email = "new@example.com", Password = "password", FirstName = "First", LastName = "Last", ConfirmPassword = "password" };
            
            var userDto = new UnifiedUserDto(
                1, "new@example.com", "First", "Last", null, null, true, true, DateTime.Now, null, null, UserType.Customer, new List<EcommerceLaptop.Core.Entities.RoleDto>(), new List<AdminPermissionDto>(), 0, null, null
            );

            var authResult = new UnifiedAuthResult(
                true, null, "token", "refresh", DateTime.Now.AddHours(1), 3600, userDto, AuthContext.Customer
            );

            _mockAuthService.Setup(x => x.RegisterAsync(It.IsAny<UnifiedRegisterRequest>()))
                .ReturnsAsync(authResult);

            // Act
            var result = await _controller.Register(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { success = true });
        }

        [Fact]
        public async Task Logout_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new LogoutRequest { RefreshToken = "token" };
            _mockAuthService.Setup(x => x.LogoutAsync(1, It.IsAny<AuthContext>(), "token"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Logout(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { success = true, message = "Logged out successfully" });
        }

        [Fact]
        public async Task ChangePassword_ValidRequest_ReturnsOk()
        {
            // Arrange
            var request = new UnifiedChangePasswordRequest { CurrentPassword = "old", NewPassword = "new", ConfirmPassword = "new" };
            _mockAuthService.Setup(x => x.ChangePasswordAsync(1, "old", "new"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(new { success = true, message = "Password changed successfully" });
        }
    }
}
