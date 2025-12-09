using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.API.Controllers.Admin;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Admin;

namespace EcommerceLaptop.Tests.Controllers;

public class AdminUsersControllerTests
{
    private readonly Mock<IUserService> _mockUserService;
    private readonly Mock<ILogger<AdminUsersController>> _mockLogger;
    private readonly AdminUsersController _controller;

    public AdminUsersControllerTests()
    {
        _mockUserService = new Mock<IUserService>();
        _mockLogger = new Mock<ILogger<AdminUsersController>>();
        _controller = new AdminUsersController(_mockUserService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetUsers_ReturnsOkResult_WithDtoList()
    {
        // Arrange
        var users = new List<User>
        {
            new User 
            { 
                Id = 1, 
                Email = "admin@test.com", 
                UserRoles = new List<UserRole> 
                { 
                    new UserRole 
                    { 
                        Role = new Role { Name = "Admin", IsAdminRole = true },
                        RoleId = 1
                    } 
                } 
            }
        };
        var pagedResult = new PagedResult<User>
        {
            Items = users,
            TotalCount = 1,
            Page = 1,
            PageSize = 10
        };

        _mockUserService.Setup(s => s.GetAdminUsersAsync(1, 10, "")).ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetUsers(1, 10, "");

        // Assert
        var actionResult = result.Result as OkObjectResult;
        actionResult.Should().NotBeNull();
        var responseDto = actionResult!.Value as UsersResponseDto;
        responseDto.Should().NotBeNull();
        responseDto!.Users.Should().HaveCount(1);
        responseDto.Users.First().Email.Should().Be("admin@test.com");
    }

    [Fact]
    public async Task CreateUser_ValidUser_ReturnsCreated()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            Email = "newadmin@test.com",
            Password = "Password123!",
            FirstName = "New",
            LastName = "Admin",
            RoleId = 1
        };

        var createdUser = new User
        {
            Id = 2,
            Email = "newadmin@test.com",
            FirstName = "New",
            LastName = "Admin",
            UserRoles = new List<UserRole>
            {
                new UserRole
                {
                    Role = new Role { Name = "Admin", IsAdminRole = true },
                    RoleId = 1
                }
            }
        };

        _mockUserService.Setup(s => s.GetByEmailAsync(request.Email)).ReturnsAsync((User?)null);
        _mockUserService.Setup(s => s.CreateAdminUserAsync(It.IsAny<User>(), request.Password, request.RoleId))
            .ReturnsAsync(createdUser);

        // Setup User for logging
        var userPrincipal = new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]
        {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, "1")
        }, "TestAuth"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext { User = userPrincipal }
        };

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        var actionResult = result.Result as CreatedAtActionResult;
        var dto = actionResult!.Value as AdminUserManagementDto;
        dto.Should().NotBeNull();
        dto!.Email.Should().Be("newadmin@test.com");
    }

    [Fact]
    public async Task CreateUser_ExistingEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new CreateUserRequestDto
        {
            Email = "existing@test.com",
            Password = "Password123!",
            RoleId = 1
        };

        _mockUserService.Setup(s => s.GetByEmailAsync(request.Email)).ReturnsAsync(new User());

        // Act
        var result = await _controller.CreateUser(request);

        // Assert
        var actionResult = result.Result as BadRequestObjectResult;
        actionResult.Should().NotBeNull();
    }
}
