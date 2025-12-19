using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using EcommerceLaptop.API.Controllers;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Services;
using EcommerceLaptop.Infrastructure.Services.Security;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.API.DTOs;
using EcommerceLaptop.Core.DTOs;

namespace EcommerceLaptop.UnitTests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IImageHostingService> _mockImageService;
        private readonly Mock<IAuditLoggingService> _mockAuditService;
        private readonly Mock<ILogger<UsersController>> _mockLogger;
        private readonly UsersController _controller;
        private const int TestUserId = 1;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockImageService = new Mock<IImageHostingService>();
            _mockAuditService = new Mock<IAuditLoggingService>();
            _mockLogger = new Mock<ILogger<UsersController>>();

            _controller = new UsersController(
                _mockUserService.Object,
                _mockImageService.Object,
                _mockAuditService.Object,
                _mockLogger.Object
            );

            SetupControllerContext(TestUserId, new List<string> { "User" });
        }

        private void SetupControllerContext(int? userId, List<string> roles)
        {
            var claims = new List<Claim>();
            if (userId.HasValue)
            {
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));
            }
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var identity = new ClaimsIdentity(claims, "TestAuthType");
            var claimsPrincipal = new ClaimsPrincipal(identity);

            var httpContext = new DefaultHttpContext { User = claimsPrincipal };
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            };
        }

        #region GetProfile Tests

        [Fact]
        public async Task GetProfile_AuthenticatedUser_ReturnsProfile()
        {
            // Arrange
            var user = new User { Id = TestUserId, Email = "test@example.com", FirstName = "Test", LastName = "User" };
            var roles = new List<string> { "User" };
            
            _mockUserService.Setup(x => x.GetByIdAsync(TestUserId))
                .ReturnsAsync(user);
            _mockUserService.Setup(x => x.GetUserRolesAsync(TestUserId))
                .ReturnsAsync(roles);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<UserProfileDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data.Email.Should().Be(user.Email);
        }

        [Fact]
        public async Task GetProfile_UserNotFound_Returns404()
        {
            // Arrange
            _mockUserService.Setup(x => x.GetByIdAsync(TestUserId))
                .ReturnsAsync((User?)null);

            // Act
            var result = await _controller.GetProfile();

            // Assert
            var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
            var response = notFoundResult.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Message.Should().Be("User not found");
        }

        #endregion

        #region ChangePassword Tests

        [Fact]
        public async Task ChangePassword_CorrectCurrentPassword_Success()
        {
            // Arrange
            var request = new ChangePasswordRequest 
            { 
                CurrentPassword = "OldPassword1!", 
                NewPassword = "NewPassword1!",
                ConfirmPassword = "NewPassword1!" 
            };
            var user = new User { Id = TestUserId };
            
            _mockUserService.Setup(x => x.GetByIdAsync(TestUserId))
                .ReturnsAsync(user);
            _mockUserService.Setup(x => x.VerifyPasswordAsync(user, request.CurrentPassword))
                .ReturnsAsync(true);
            _mockUserService.Setup(x => x.VerifyPasswordAsync(user, request.NewPassword))
                .ReturnsAsync(false); // New password is different
            _mockUserService.Setup(x => x.UpdatePasswordAsync(TestUserId, request.NewPassword))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_ReturnsBadRequest()
        {
            // Arrange
            var request = new ChangePasswordRequest 
            { 
                CurrentPassword = "Wrong", 
                NewPassword = "New",
                ConfirmPassword = "New"
            };
            var user = new User { Id = TestUserId };
            
            _mockUserService.Setup(x => x.GetByIdAsync(TestUserId)).ReturnsAsync(user);
            _mockUserService.Setup(x => x.VerifyPasswordAsync(user, request.CurrentPassword)).ReturnsAsync(false);

            // Act
            var result = await _controller.ChangePassword(request);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequest.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Message.Should().Be("Mt khu hin ti khng ng");
        }

        #endregion

        #region UpdateProfile Tests

        [Fact]
        public async Task UpdateProfile_ValidData_ReturnsUpdatedProfile()
        {
            // Arrange
            var request = new UpdateUserProfileRequest { FirstName = "NewFirst", LastName = "NewLast" };
            var user = new User { Id = TestUserId, FirstName = "OldFirst", LastName = "OldLast" };
            var updatedUser = new User { Id = TestUserId, FirstName = "NewFirst", LastName = "NewLast" };
            
            _mockUserService.Setup(x => x.GetByIdAsync(TestUserId)).ReturnsAsync(user);
            _mockUserService.Setup(x => x.UpdateUserAsync(user)).ReturnsAsync(updatedUser);
            _mockUserService.Setup(x => x.GetUserRolesAsync(TestUserId)).ReturnsAsync(new List<string> { "User" });

            // Act
            var result = await _controller.UpdateProfile(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<ApiResponse<UserProfileDto>>().Subject;
            response.Data.FirstName.Should().Be(request.FirstName);
        }

        #endregion

        #region Admin Operations

        [Fact]
        public async Task GetUsers_AsAdmin_ReturnsList()
        {
            // Arrange
            SetupControllerContext(2, new List<string> { "Admin" }); // Switch to Admin
            
            var paginatedList = new PagedResult<User>
            {
                Items = new List<User> { new User { Id = 1, Email = "u1" }, new User { Id = 2, Email = "u2" } },
                Page = 1,
                PageSize = 20,
                TotalCount = 2
            };

            _mockUserService.Setup(x => x.GetUsersAsync(1, 20, null))
                .ReturnsAsync(paginatedList);
            _mockUserService.Setup(x => x.GetUserRolesAsync(It.IsAny<int>()))
                .ReturnsAsync(new List<string> { "User" });

            // Act
            var result = await _controller.GetUsers(1, 20, null);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeAssignableTo<PaginatedApiResponse<UserSummaryDto>>().Subject;
            response.TotalCount.Should().Be(2);
        }

        [Fact]
        public async Task CreateUser_DuplicateEmail_ReturnsError()
        {
            // Arrange
            SetupControllerContext(2, new List<string> { "Admin" });
            var request = new CreateUserRequest 
            { 
                Email = "exist@test.com",
                FirstName = "Test",
                LastName = "User",
                PhoneNumber = "123456789",
                Password = "Password123!"
            };
            
            _mockUserService.Setup(x => x.GetByEmailAsync(request.Email))
                .ReturnsAsync(new User());

            // Act
            var result = await _controller.CreateUser(request);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var response = badRequest.Value.Should().BeAssignableTo<ApiResponse<object>>().Subject;
            response.Message.Should().Contain("User with this email already exists");
        }

        #endregion
    }
    

}
