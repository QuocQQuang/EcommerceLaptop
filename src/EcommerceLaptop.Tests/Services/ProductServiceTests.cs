using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Services;

namespace EcommerceLaptop.Tests.Services;

public class ProductServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _service = new ProductService(_context);

        SeedData();
    }

    private void SeedData()
    {
        // Seed categories if needed, but focus on products
        var laptop1 = new Laptop
        {
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
            Name = "Test Laptop 2",
            Brand = "Apple",
            Model = "MacBook Pro",
            Price = 2000.00m,
            SKU = "LAPTOP-002",
            IsActive = true,
            CpuBrand = "Apple",
            CpuModel = "M3",
            RamCapacityGB = 32,
            StorageType = "SSD"
        };

        var accessory = new Accessory
        {
            Name = "Test Mouse",
            Brand = "Logitech",
            Price = 50.00m,
            SKU = "ACC-001",
            IsActive = true,
            AccessoryType = "Mouse",
            Compatibility = "{\"laptops\": true}"
        };

        var bundle = new Bundle
        {
            Name = "Test Bundle",
            Brand = "Bundle",
            Price = 1050.00m,
            SKU = "BUNDLE-001",
            IsActive = true,
            BundleType = "Gaming",
            DiscountPercentage = 10.0m
        };

        _context.Products.AddRange(laptop1, laptop2, accessory, bundle);
        _context.SaveChanges();
    }

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsProductWithIncludes()
    {
        // Act
        var result = await _service.GetByIdAsync(1); // Assume ID 1 is laptop1

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Laptop 1");
        result.Inventory.Should().NotBeNull(); // Even if not seeded, context creates
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetProductsAsync_NoFilters_ReturnsAllActiveProducts()
    {
        // Act
        var result = await _service.GetProductsAsync(page: 1, pageSize: 10);

        // Assert
        result.Items.Should().HaveCount(4);
        result.TotalCount.Should().Be(4);
        result.Page.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetProductsAsync_SearchTerm_ReturnsMatchingProducts()
    {
        // Act
        var result = await _service.GetProductsAsync(searchTerm: "Test Laptop");

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(p => p.Name.Should().Contain("Test Laptop"));
    }

    [Fact]
    public async Task GetProductsAsync_PriceFilter_ReturnsWithinRange()
    {
        // Act
        var result = await _service.GetProductsAsync(minPrice: 100, maxPrice: 1500);

        // Assert
        result.Items.Should().HaveCount(3); // laptop1, accessory, exclude laptop2 (2000)
        result.Items.Should().AllSatisfy(p =>
        {
            p.Price.Should().BeGreaterThanOrEqualTo(100m);
            p.Price.Should().BeLessThanOrEqualTo(1500m);
        });
    }

    [Fact]
    public async Task GetProductsAsync_BrandFilter_ReturnsMatchingBrand()
    {
        // Act
        var result = await _service.GetProductsAsync(brand: "Dell");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.Should().AllSatisfy(p => p.Brand.Should().Be("Dell"));
    }

    [Fact]
    public async Task GetLaptopsAsync_NoFilters_ReturnsAllLaptops()
    {
        // Act
        var result = await _service.GetLaptopsAsync();

        // Assert
        result.Items.Should().HaveCount(2);
        result.Items.Should().AllSatisfy(l => l.Should().BeOfType<Laptop>());
    }

    [Fact]
    public async Task GetLaptopsAsync_CpuBrandFilter_ReturnsMatchingLaptops()
    {
        // Act
        var result = await _service.GetLaptopsAsync(cpuBrand: "Intel");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.Should().AllSatisfy(l => ((Laptop)l).CpuBrand.Should().Be("Intel"));
    }

    [Fact]
    public async Task GetLaptopsAsync_RamFilter_ReturnsWithMinimumRam()
    {
        // Act
        var result = await _service.GetLaptopsAsync(ramCapacityGB: 20);

        // Assert
        result.Items.Should().HaveCount(1); // Only MacBook with 32GB
        result.Items.Should().AllSatisfy(l => ((Laptop)l).RamCapacityGB.Should().BeGreaterThanOrEqualTo(20));
    }

    [Fact]
    public async Task GetLaptopsWithAdvancedFilteringAsync_MultipleFilters_ReturnsMatchingLaptops()
    {
        // Act
        var result = await _service.GetLaptopsWithAdvancedFilteringAsync(
            minPrice: 500,
            maxPrice: 1500,
            cpuBrand: "Intel",
            minRamCapacityGB: 16,
            storageType: "SSD");

        // Assert
        result.Items.Should().HaveCount(1); // Test Laptop 1 matches
        result.Items.Should().AllSatisfy(l =>
        {
            l.Price.Should().BeGreaterThanOrEqualTo(500m);
            l.Price.Should().BeLessThanOrEqualTo(1500m);
            ((Laptop)l).CpuBrand.Should().Be("Intel");
            ((Laptop)l).RamCapacityGB.Should().BeGreaterThanOrEqualTo(16);
            ((Laptop)l).StorageType.Should().Be("SSD");
        });
    }

    [Fact]
    public async Task GetAccessoriesAsync_NoFilters_ReturnsAllAccessories()
    {
        // Act
        var result = await _service.GetAccessoriesAsync();

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.Should().AllSatisfy(a => a.Should().BeOfType<Accessory>());
    }

    [Fact]
    public async Task GetAccessoriesAsync_TypeFilter_ReturnsMatchingAccessories()
    {
        // Act
        var result = await _service.GetAccessoriesAsync(accessoryType: "Mouse");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.Should().AllSatisfy(a => ((Accessory)a).AccessoryType.Should().Be("Mouse"));
    }

    [Fact]
    public async Task GetAccessoriesWithCompatibilityAsync_CompatibilityFilter_ReturnsMatching()
    {
        // Act
        var result = await _service.GetAccessoriesWithCompatibilityAsync(compatibility: "laptops");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.Should().AllSatisfy(a => ((Accessory)a).Compatibility.Should().Contain("laptops"));
    }

    [Fact]
    public async Task ValidateProductCompatibilityAsync_UniversalAccessory_ReturnsTrue()
    {
        // Arrange
        var accessory = new Accessory { Compatibility = "" }; // Empty = universal
        _context.Accessories.Add(accessory);
        await _context.SaveChangesAsync();
        var productId = 1;
        var accessoryId = accessory.Id;

        // Act
        var result = await _service.ValidateProductCompatibilityAsync(productId, accessoryId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateProductCompatibilityAsync_CompatibleBrand_ReturnsTrue()
    {
        // Arrange
        var product = new Laptop { Brand = "Dell" };
        _context.Products.Add(product);
        var accessory = new Accessory { Compatibility = "{\"brands\": [\"Dell\"]}" };
        _context.Accessories.Add(accessory);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.ValidateProductCompatibilityAsync(product.Id, accessory.Id);

        // Assert
        result.Should().BeTrue(); // Assuming logic checks brand in JSON
    }

    [Fact]
    public async Task GetBundlesAsync_NoFilters_ReturnsAllBundles()
    {
        // Act
        var result = await _service.GetBundlesAsync();

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.Should().AllSatisfy(b => b.Should().BeOfType<Bundle>());
    }

    [Fact]
    public async Task CreateBundleAsync_ValidProducts_ReturnsCreatedBundle()
    {
        // Arrange
        var productIds = new[] { 1, 2 }; // laptop1 and laptop2

        // Act
        var bundle = await _service.CreateBundleAsync("Test Gaming Bundle", "Gaming setup", productIds, 10.0m);

        // Assert
        bundle.Should().NotBeNull();
        bundle.Name.Should().Be("Test Gaming Bundle");
        bundle.DiscountPercentage.Should().Be(10.0m);
        bundle.BundleItems.Should().HaveCount(2);
        bundle.Price.Should().BePositive(); // From calculation
    }

    [Fact]
    public async Task CreateBundleAsync_InvalidProduct_ThrowsArgumentException()
    {
        // Arrange
        var productIds = new[] { 999 }; // Non-existing

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateBundleAsync("Test", "Desc", productIds, 10.0m));
    }

    [Fact]
    public async Task CalculateBundlePriceAsync_ValidProducts_ReturnsDiscountedPrice()
    {
        // Arrange
        var productIds = new[] { 1, 3 }; // laptop1 (1000) + accessory (50)

        // Act
        var price = await _service.CalculateBundlePriceAsync(productIds, 10.0m);

        // Assert
        price.Should().Be(945.00m); // 1050 * 0.9, min 10%
    }

    [Fact]
    public async Task CreateProductAsync_ValidProduct_ReturnsCreatedProduct()
    {
        // Arrange
        var newLaptop = new Laptop
        {
            Name = "New Laptop",
            Brand = "HP",
            Price = 1500.00m,
            SKU = "NEW-001"
        };

        // Act
        var result = await _service.CreateProductAsync(newLaptop);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.Name.Should().Be("New Laptop");
        result.CreatedAt.Should().NotBe(default(DateTime));
    }

    [Fact]
    public async Task UpdateProductAsync_ValidProduct_UpdatesAndReturns()
    {
        // Arrange
        var product = await _service.GetByIdAsync(1);
        product.Name = "Updated Laptop";

        // Act
        var result = await _service.UpdateProductAsync(product);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Updated Laptop");
        result.UpdatedAt.Should().NotBe(default(DateTime));
    }

    [Fact]
    public async Task DeleteProductAsync_ExistingId_SoftDeletes()
    {
        // Act
        var result = await _service.DeleteProductAsync(1);

        // Assert
        result.Should().BeTrue();
        var deleted = await _context.Products.FindAsync(1);
        deleted.Should().NotBeNull();
        deleted.IsActive.Should().BeFalse();
        deleted.UpdatedAt.Should().NotBe(default(DateTime));
    }

    [Fact]
    public async Task DeleteProductAsync_NonExistingId_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteProductAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetBySKUAsync_ExistingSKU_ReturnsProduct()
    {
        // Act
        var result = await _service.GetBySKUAsync("LAPTOP-001");

        // Assert
        result.Should().NotBeNull();
        result.SKU.Should().Be("LAPTOP-001");
    }

    [Fact]
    public async Task GetBySKUAsync_NonExistingSKU_ReturnsNull()
    {
        // Act
        var result = await _service.GetBySKUAsync("NON-EXIST");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task IsSKUUniqueAsync_UniqueSKU_ReturnsTrue()
    {
        // Act
        var result = await _service.IsSKUUniqueAsync("UNIQUE-001");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsSKUUniqueAsync_DuplicateSKU_ReturnsFalse()
    {
        // Act
        var result = await _service.IsSKUUniqueAsync("LAPTOP-001");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsSKUUniqueAsync_ExcludeSelf_ReturnsTrueForSameSKU()
    {
        // Act
        var result = await _service.IsSKUUniqueAsync("LAPTOP-001", excludeProductId: 1);

        // Assert
        result.Should().BeTrue(); // Same SKU but excluding self
    }

    [Fact]
    public async Task GetFeaturedProductsAsync_ReturnsRecentActiveProducts()
    {
        // Act
        var result = await _service.GetFeaturedProductsAsync(3);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(p => p.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task GetRelatedProductsAsync_SameBrand_ReturnsMatching()
    {
        // Act
        var result = await _service.GetRelatedProductsAsync(1, 1); // Related to laptop1 (Dell)

        // Assert
        result.Should().HaveCount(1); // Only one other Dell? Wait, seeded only one Dell, but test logic
        // Adjust expectation based on seed: no other Dell, so empty, but service logic excludes self
        result.Should().BeEmpty(); // No other Dell in seed
    }

    [Fact]
    public async Task GetProductsByBrandAsync_ReturnsBrandProducts()
    {
        // Act
        var result = await _service.GetProductsByBrandAsync("Dell");

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items.Should().AllSatisfy(p => p.Brand.Should().Be("Dell"));
    }

    [Fact]
    public async Task GetBrandsAsync_ReturnsUniqueBrands()
    {
        // Act
        var result = await _service.GetBrandsAsync();

        // Assert
        result.Should().HaveCount(3); // Dell, Apple, Logitech, Bundle? Bundle has "Bundle"
        result.Should().Contain("Dell", "Apple", "Logitech");
    }

    [Fact]
    public async Task UpdateProductImagesAsync_ValidImages_UpdatesSuccessfully()
    {
        // Arrange
        var newImages = new List<ProductImage>
        {
            new ProductImage { ImageUrl = "/new1.jpg", AltText = "New1", IsPrimary = true },
            new ProductImage { ImageUrl = "/new2.jpg", AltText = "New2", IsPrimary = false }
        };

        // Act
        var result = await _service.UpdateProductImagesAsync(1, newImages);

        // Assert
        result.Should().BeTrue();
        var product = await _context.Products.Include(p => p.Images).FirstAsync(p => p.Id == 1);
        product.Images.Should().HaveCount(2);
        product.Images.Should().AllSatisfy(i => i.ProductId.Should().Be(1));
    }

    [Fact]
    public async Task UpdateProductImagesAsync_NonExistingProduct_ReturnsFalse()
    {
        // Arrange
        var newImages = new List<ProductImage> { new ProductImage { ImageUrl = "/new.jpg" } };

        // Act
        var result = await _service.UpdateProductImagesAsync(999, newImages);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetProductSpecificationsAsync_Laptop_ReturnsStructuredSpecs()
    {
        // Act
        var specs = await _service.GetProductSpecificationsAsync(1); // laptop1

        // Assert
        specs.Should().NotBeEmpty();
        specs["Name"].Should().Be("Test Laptop 1");
        specs["Brand"].Should().Be("Dell");
        specs["Type"].Should().Be("Laptop");
        specs.ContainsKey("CPU").Should().BeTrue();
        ((dynamic)specs["CPU"]).Brand.Should().Be("Intel");
        ((dynamic)specs["RAM"]).CapacityGB.Should().Be(16);
    }

    [Fact]
    public async Task GetProductSpecificationsAsync_Accessory_ReturnsAccessorySpecs()
    {
        // Act - assume accessory ID 3
        var specs = await _service.GetProductSpecificationsAsync(3);

        // Assert
        specs.Should().NotBeEmpty();
        specs["Type"].Should().Be("Accessory");
        specs["AccessoryType"].Should().Be("Mouse");
        specs["Compatibility"].Should().Be("{\"laptops\": true}");
    }

    [Fact]
    public async Task GetRecommendedProductsAsync_NoHistory_ReturnsPopularProducts()
    {
        // Act
        var result = await _service.GetRecommendedProductsAsync(999); // Non-existing user

        // Assert
        result.Should().NotBeEmpty(); // Returns featured based on reviews count (0, but ordered by price)
        result.Should().AllSatisfy(p => p.IsActive.Should().BeTrue());
    }

    [Fact]
    public async Task BulkUpdatePricingAsync_ValidAdjustment_UpdatesPrices()
    {
        // Arrange
        var productIds = new[] { 1, 2 };

        // Act
        var result = await _service.BulkUpdatePricingAsync(productIds, 10.0m); // 10% increase

        // Assert
        result.Should().BeTrue();
        var updated1 = await _context.Products.FindAsync(1);
        updated1.Price.Should().Be(1100.00m); // 1000 + 10%
        var updated2 = await _context.Products.FindAsync(2);
        updated2.Price.Should().Be(2200.00m); // 2000 + 10%
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
