using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs.Cart;
using EcommerceLaptop.Infrastructure.Services;
using EcommerceLaptop.Infrastructure.Data;

namespace EcommerceLaptop.UnitTests.Services
{
    public class ShoppingCartServiceTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ShoppingCartService>> _mockLogger;
        private readonly ShoppingCartService _service;

        public ShoppingCartServiceTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            
            _context = new ApplicationDbContext(options);
            _mockLogger = new Mock<ILogger<ShoppingCartService>>();

            _service = new ShoppingCartService(_context, _mockLogger.Object);
        }

        [Fact]
        public async Task AddToCartAsync_ValidProduct_AddsToUserCart()
        {
            // Arrange
            var userId = "1";
            var product = new Laptop { Id = 1, Name = "Laptop", Price = 1000, IsActive = true };
            product.Inventory = new Inventory { ProductId = 1, QuantityInStock = 10 };
            
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var addToCartDto = new AddToCartDto { ProductId = 1, Quantity = 1 };

            // Act
            var result = await _service.AddToCartAsync(addToCartDto, userId);

            // Assert
            result.Should().NotBeNull();
            var cart = await _context.ShoppingCarts.Include(c => c.CartItems).FirstOrDefaultAsync(c => c.UserId == 1);
            cart.Should().NotBeNull();
            cart!.CartItems.Should().HaveCount(1);
            cart.CartItems.First().ProductId.Should().Be(1);
        }

        [Fact]
        public async Task AddToCartAsync_InsufficientStock_ThrowsException()
        {
            // Arrange
            var userId = "1";
            var product = new Laptop { Id = 1, Name = "Laptop", Price = 1000, IsActive = true };
            product.Inventory = new Inventory { ProductId = 1, QuantityInStock = 0 }; // No stock
            
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            var addToCartDto = new AddToCartDto { ProductId = 1, Quantity = 1 };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.AddToCartAsync(addToCartDto, userId));
        }

         [Fact]
        public async Task UpdateCartItemAsync_ValidUpdate_UpdatesQuantity()
        {
            // Arrange
            var userId = "1";
            var product = new Laptop { Id = 1, Name = "Laptop", Price = 1000, IsActive = true };
            product.Inventory = new Inventory { ProductId = 1, QuantityInStock = 10 };
            var cart = new ShoppingCart { UserId = 1, IsActive = true };
            var cartItem = new CartItem { Id = 1, ProductId = 1, Product = product, Quantity = 1, UnitPrice = 1000 };
            cart.CartItems.Add(cartItem);
            
            _context.Products.Add(product);
            _context.ShoppingCarts.Add(cart);
            await _context.SaveChangesAsync();

            var updateDto = new UpdateCartItemDto { CartItemId = 1, Quantity = 2 };

            // Act
            var result = await _service.UpdateCartItemAsync(updateDto, userId);

            // Assert
            var savedItem = await _context.CartItems.FindAsync(1);
            savedItem.Should().NotBeNull();
            savedItem!.Quantity.Should().Be(2);
        }

        [Fact]
        public async Task RemoveFromCartAsync_ExistingItem_RemovesItem()
        {
             // Arrange
            var userId = "1";
            var product = new Laptop { Id = 1, Name = "Laptop", Price = 1000, IsActive = true };
            var cart = new ShoppingCart { UserId = 1, IsActive = true };
            var cartItem = new CartItem { Id = 1, ProductId = 1, Product = product, Quantity = 1, UnitPrice = 1000 };
            cart.CartItems.Add(cartItem);
            
            _context.Products.Add(product);
            _context.ShoppingCarts.Add(cart);
            await _context.SaveChangesAsync();

            var removeDto = new RemoveFromCartDto { CartItemId = 1 };

            // Act
            await _service.RemoveFromCartAsync(removeDto, userId);

            // Assert
            var savedItem = await _context.CartItems.FindAsync(1);
            savedItem.Should().BeNull();
        }
    }
}
