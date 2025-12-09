using Xunit;
using FluentAssertions;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Tests.Entities;

public class UserTests
{
    [Fact]
    public void User_Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var user = new User
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        // Assert
        user.Email.Should().Be("test@example.com");
        user.FirstName.Should().Be("Test");
        user.LastName.Should().Be("User");
        user.IsActive.Should().BeTrue();
        user.CreatedAt.Should().NotBe(default(DateTime));
        user.UpdatedAt.Should().NotBe(default(DateTime));
    }

    [Fact]
    public void User_Properties_ShouldBeSettable()
    {
        // Arrange
        var user = new User();

        // Act
        user.Email = "updated@example.com";
        user.PasswordHash = "hashedpassword";
        user.PhoneNumber = "+84123456789";
        user.IsActive = false;

        // Assert
        user.Email.Should().Be("updated@example.com");
        user.PasswordHash.Should().Be("hashedpassword");
        user.PhoneNumber.Should().Be("+84123456789");
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void User_NavigationProperties_ShouldInitializeEmptyCollections()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.UserRoles.Should().NotBeNull().And.BeEmpty();
        user.Addresses.Should().NotBeNull().And.BeEmpty();
        user.Orders.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Role_Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var role = new Role
        {
            Name = "Admin",
            Description = "Administrator role"
        };

        // Assert
        role.Name.Should().Be("Admin");
        role.Description.Should().Be("Administrator role");
        role.UserRoles.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Address_Constructor_ShouldInitializeProperties()
    {
        // Arrange & Act
        var address = new Address
        {
            UserId = 1,
            Street = "123 Test Street",
            City = "Ho Chi Minh City",
            Province = "Ho Chi Minh",
            IsDefault = true
        };

        // Assert
        address.UserId.Should().Be(1);
        address.Street.Should().Be("123 Test Street");
        address.City.Should().Be("Ho Chi Minh City");
        address.Province.Should().Be("Ho Chi Minh");
        address.IsDefault.Should().BeTrue();
        address.CreatedAt.Should().NotBe(default(DateTime));
    }
}