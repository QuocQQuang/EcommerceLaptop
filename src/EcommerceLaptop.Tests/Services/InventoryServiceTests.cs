using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.DTOs.Inventory;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Services;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Tests.Services;

/// <summary>
/// Comprehensive unit tests for InventoryService
/// Tests cover core CRUD operations, stock management, and basic functionality
/// </summary>
public class InventoryServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<IInventoryReservationService> _mockReservationService;
    private readonly Mock<ILogger<InventoryService>> _mockLogger;
    private readonly InventoryService _service;

    public InventoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new ApplicationDbContext(options);
        _mockReservationService = new Mock<IInventoryReservationService>();
        _mockLogger = new Mock<ILogger<InventoryService>>();
        
        _service = new InventoryService(_context, _mockReservationService.Object, _mockLogger.Object);

        SeedTestData();
    }

    private void SeedTestData()
    {
        // Create test products
        var laptop1 = new Laptop
        {
            Id = 1,
            Name = "Test Laptop 1",
            Brand = "Dell",
            Model = "XPS 13",
            Price = 1000.00m,
            SKU = "LAPTOP-001",
            IsActive = true,
            CpuBrand = "Intel",
            CpuModel = "Core i7",
            RamCapacityGB = 16,
            StorageType = "SSD"
        };

        var laptop2 = new Laptop
        {
            Id = 2,
            Name = "Test Laptop 2",
            Brand = "Apple",
            Model = "MacBook Pro",
            Price = 2000.00m,
            SKU = "LAPTOP-002",
            IsActive = true,
            CpuBrand = "Apple",
            CpuModel = "M1",
            RamCapacityGB = 32,
            StorageType = "SSD"
        };

        _context.Products.AddRange(laptop1, laptop2);

        // Create test inventory records
        var inventory1 = new Inventory
        {
            Id = 1,
            ProductId = 1,
            QuantityInStock = 100,
            ReservedQuantity = 0,
            ReorderLevel = 20,
            MaxStockLevel = 500,
            WarehouseLocation = "WH-01",
            LastStockUpdate = DateTime.UtcNow
        };

        var inventory2 = new Inventory
        {
            Id = 2,
            ProductId = 2,
            QuantityInStock = 50,
            ReservedQuantity = 0,
            ReorderLevel = 10,
            MaxStockLevel = 200,
            WarehouseLocation = "WH-01",
            LastStockUpdate = DateTime.UtcNow
        };

        _context.Inventories.AddRange(inventory1, inventory2);

        // Create test inventory transactions
        var transaction1 = new InventoryTransaction
        {
            Id = 1,
            InventoryId = 1,
            Type = InventoryTransactionType.Purchase,
            Quantity = 100,
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            Reference = "INITIAL-STOCK",
            Notes = "Initial stock setup",
            CreatedBy = 1
        };

        _context.InventoryTransactions.Add(transaction1);

        _context.SaveChanges();
    }

    #region Core CRUD Operations Tests

    [Fact]
    public async Task GetInventoryByProductIdAsync_ValidProductId_ReturnsInventoryDto()
    {
        // Arrange
        var productId = 1;

        // Act
        var result = await _service.GetInventoryByProductIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.ProductId.Should().Be(productId);
        result.QuantityInStock.Should().Be(100);
        result.ReorderLevel.Should().Be(20);
        result.WarehouseLocation.Should().Be("WH-01");
    }

    [Fact]
    public async Task GetInventoryByProductIdAsync_InvalidProductId_ReturnsNull()
    {
        // Arrange
        var productId = 999;

        // Act
        var result = await _service.GetInventoryByProductIdAsync(productId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetInventoriesAsync_ValidRequest_ReturnsPagedResults()
    {
        // Arrange
        var request = new InventoryFilterRequest
        {
            Page = 1,
            PageSize = 10,
            WarehouseLocation = "WH-01"
        };

        // Act
        var result = await _service.GetInventoriesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task CreateInventoryAsync_ValidRequest_CreatesInventory()
    {
        // Arrange - Create for a new product that doesn't have inventory yet
        var newProduct = new Laptop
        {
            Id = 3,
            Name = "Test Laptop 3",
            Brand = "HP",
            Model = "EliteBook",
            Price = 1200.00m,
            SKU = "LAPTOP-003",
            IsActive = true,
            CpuBrand = "Intel",
            CpuModel = "Core i5",
            RamCapacityGB = 8,
            StorageType = "SSD"
        };
        _context.Products.Add(newProduct);
        await _context.SaveChangesAsync();

        var request = new CreateInventoryRequest
        {
            ProductId = 3, // New product
            QuantityInStock = 200,
            ReorderLevel = 30,
            MaxStockLevel = 600,
            WarehouseLocation = "WH-02",
            UnitCost = 850.00m
        };

        // Act
        var result = await _service.CreateInventoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ProductId.Should().Be(3);
        result.QuantityInStock.Should().Be(200);
        result.WarehouseLocation.Should().Be("WH-02");

        // Verify database
        var inventoryInDb = await _context.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == 3 && i.WarehouseLocation == "WH-02");
        inventoryInDb.Should().NotBeNull();
    }

    [Fact]
    public async Task UpdateInventoryAsync_ValidRequest_UpdatesInventory()
    {
        // Arrange
        var inventoryId = 1;
        var request = new UpdateInventoryRequest
        {
            QuantityInStock = 150,
            ReorderLevel = 25,
            MaxStockLevel = 550
        };

        // Act
        var result = await _service.UpdateInventoryAsync(inventoryId, request);

        // Assert
        result.Should().NotBeNull();
        result.QuantityInStock.Should().Be(150);
        result.ReorderLevel.Should().Be(25);
        result.MaxStockLevel.Should().Be(550);
    }

    [Fact]
    public async Task ValidateStockLevelsAsync_SufficientStock_ReturnsTrue()
    {
        // Arrange
        var productIds = new List<int> { 1, 2 };

        // Act
        var result = await _service.ValidateStockLevelsAsync(productIds);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region Alerts & Monitoring Tests

    [Fact]
    public async Task GetLowStockAlertsAsync_ProductsBelowReorderLevel_ReturnsAlerts()
    {
        // Arrange
        // Update inventory to be below reorder level
        var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == 1);
        inventory!.QuantityInStock = 15; // Below reorder level of 20
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetLowStockAlertsAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Contain(alert => alert.ProductId == 1);
        
        var alertForProduct1 = result.First(a => a.ProductId == 1);
        alertForProduct1.CurrentStock.Should().Be(15);
        alertForProduct1.ReorderLevel.Should().Be(20);
    }

    [Fact]
    public async Task UpdateReorderLevelsAsync_ValidProductAndLevel_UpdatesReorderLevel()
    {
        // Arrange
        var productId = 1;
        var newReorderLevel = 30;

        // Act
        var result = await _service.UpdateReorderLevelsAsync(productId, newReorderLevel);

        // Assert
        result.Should().BeTrue();

        // Verify update
        var inventory = await _context.Inventories.FirstOrDefaultAsync(i => i.ProductId == productId);
        inventory!.ReorderLevel.Should().Be(newReorderLevel);
    }

    #endregion

    #region Reporting & Analytics Tests

    [Fact]
    public async Task GenerateInventoryReportAsync_ValidRequest_ReturnsReport()
    {
        // Arrange
        var request = new InventoryReportRequest
        {
            ReportType = "Summary",
            WarehouseLocation = "WH-01",
            FromDate = DateTime.UtcNow.AddDays(-30),
            ToDate = DateTime.UtcNow
        };

        // Act
        var result = await _service.GenerateInventoryReportAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.ReportType.Should().Be("Summary");
        result.GeneratedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result.Items.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetTransactionHistoryAsync_ValidProductId_ReturnsTransactions()
    {
        // Arrange
        var productId = 1;
        var fromDate = DateTime.UtcNow.AddDays(-10);
        var toDate = DateTime.UtcNow;

        // Act
        var result = await _service.GetTransactionHistoryAsync(productId, fromDate, toDate);

        // Assert
        result.Should().NotBeEmpty();
        result.Should().OnlyContain(t => t.CreatedAt >= fromDate && t.CreatedAt <= toDate);
    }

    [Fact]
    public async Task GetInventoryKPIsAsync_ValidWarehouse_ReturnsKPIs()
    {
        // Arrange
        var warehouseLocation = "WH-01";

        // Act
        var result = await _service.GetInventoryKPIsAsync(warehouseLocation);

        // Assert
        result.Should().NotBeNull();
        result.ProductsNeedingReorder.Should().BeGreaterThanOrEqualTo(0);
        result.TotalInventoryValue.Should().BeGreaterThan(0);
        result.WarehouseLocation.Should().Be(warehouseLocation);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateInventoryAsync_DuplicateProductAndWarehouse_ThrowsValidationException()
    {
        // Arrange
        var request = new CreateInventoryRequest
        {
            ProductId = 1, // Already exists for WH-01
            QuantityInStock = 100,
            ReorderLevel = 20,
            MaxStockLevel = 500,
            WarehouseLocation = "WH-01",
            UnitCost = 800.00m
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            () => _service.CreateInventoryAsync(request));
    }

    #endregion

    public void Dispose()
    {
        _context.Dispose();
    }
}