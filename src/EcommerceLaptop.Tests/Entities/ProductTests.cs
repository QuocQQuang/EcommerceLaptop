using Xunit;
using FluentAssertions;
using EcommerceLaptop.Core.Entities;

namespace EcommerceLaptop.Tests.Entities;

public class ProductTests
{
    [Fact]
    public void Product_BaseProperties_ShouldBeSettable()
    {
        // Arrange & Act
        var product = new Laptop
        {
            Name = "Test Product",
            Description = "Test Description",
            Brand = "Test Brand",
            Model = "Test Model",
            Price = 1000.00m,
            SKU = "TEST-001",
            IsActive = false
        };

        // Assert
        product.Name.Should().Be("Test Product");
        product.Description.Should().Be("Test Description");
        product.Brand.Should().Be("Test Brand");
        product.Model.Should().Be("Test Model");
        product.Price.Should().Be(1000.00m);
        product.SKU.Should().Be("TEST-001");
        product.IsActive.Should().BeFalse();
        product.CreatedAt.Should().NotBe(default(DateTime));
        product.UpdatedAt.Should().NotBe(default(DateTime));
    }

    [Fact]
    public void Product_NavigationProperties_ShouldInitializeEmptyCollections()
    {
        // Arrange & Act
        var product = new Laptop();

        // Assert
        product.Images.Should().NotBeNull().And.BeEmpty();
        product.Reviews.Should().NotBeNull().And.BeEmpty();
        product.OrderItems.Should().NotBeNull().And.BeEmpty();
        product.Inventory.Should().BeNull();
    }

    [Fact]
    public void Laptop_SpecificProperties_ShouldBeSettable()
    {
        // Arrange & Act
        var laptop = new Laptop
        {
            Series = "Pro Series",
            CpuBrand = "Intel",
            CpuModel = "Core i7",
            CpuGeneration = "13th Gen",
            CpuCores = 14,
            CpuBaseClockGHz = 2.5m,
            CpuBoostClockGHz = 5.0m,
            CpuCache = "24MB",
            RamType = "DDR5",
            RamCapacityGB = 32,
            RamSlots = 2,
            RamSpeed = 4800,
            RamUpgradeable = true,
            StorageType = "SSD",
            StorageCapacityGB = 1024,
            StorageInterface = "NVMe",
            NvMeSupport = true,
            GpuType = "Discrete",
            GpuBrand = "NVIDIA",
            GpuModel = "RTX 4060",
            GpuVramGB = 8,
            DisplaySizeInches = 16.0m,
            DisplayResolution = "3840x2160",
            DisplayPanelType = "IPS",
            DisplayRefreshRateHz = 60,
            DisplayTouchscreen = false,
            BatteryCapacityWh = 90,
            WeightKg = 2.1m,
            Dimensions = "35.5 x 24.8 x 1.8 cm",
            Color = "Space Gray",
            Ports = "USB-C, HDMI, Thunderbolt",
            WiFi6Support = true,
            BluetoothSupport = true,
            BluetoothVersion = "5.2",
            WarrantyPeriod = "24 months",
            TargetAudience = "Professional"
        };

        // Assert
        laptop.Series.Should().Be("Pro Series");
        laptop.CpuBrand.Should().Be("Intel");
        laptop.RamCapacityGB.Should().Be(32);
        laptop.GpuVramGB.Should().Be(8);
        laptop.DisplaySizeInches.Should().Be(16.0m);
        laptop.WarrantyPeriod.Should().Be("24 months");
    }

    [Fact]
    public void Accessory_SpecificProperties_ShouldBeSettable()
    {
        // Arrange & Act
        var accessory = new Accessory
        {
            AccessoryType = "Mouse",
            Compatibility = "{\"laptops\": true, \"desktops\": true}",
            Specifications = "{\"dpi\": 16000, \"buttons\": 7}",
            Color = "Black",
            Connectivity = "Wireless"
        };

        // Assert
        accessory.AccessoryType.Should().Be("Mouse");
        accessory.Compatibility.Should().Be("{\"laptops\": true, \"desktops\": true}");
        accessory.Specifications.Should().Be("{\"dpi\": 16000, \"buttons\": 7}");
        accessory.Color.Should().Be("Black");
        accessory.Connectivity.Should().Be("Wireless");
    }

    [Fact]
    public void Bundle_SpecificProperties_ShouldBeSettable()
    {
        // Arrange & Act
        var bundle = new Bundle
        {
            BundleType = "Gaming Setup",
            DiscountPercentage = 15.0m,
            ValidFrom = DateTime.UtcNow,
            ValidTo = DateTime.UtcNow.AddDays(30)
        };

        // Assert
        bundle.BundleType.Should().Be("Gaming Setup");
        bundle.DiscountPercentage.Should().Be(15.0m);
        bundle.ValidFrom.Should().NotBeNull();
        bundle.ValidTo.Should().NotBeNull();
        bundle.BundleItems.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void BundleItem_Properties_ShouldBeSettable()
    {
        // Arrange & Act
        var bundleItem = new BundleItem
        {
            BundleId = 1,
            ProductId = 10,
            Quantity = 1,
            DiscountPercentage = 10.0m
        };

        // Assert
        bundleItem.BundleId.Should().Be(1);
        bundleItem.ProductId.Should().Be(10);
        bundleItem.Quantity.Should().Be(1);
        bundleItem.DiscountPercentage.Should().Be(10.0m);
    }

    [Fact]
    public void ProductImage_Properties_ShouldBeSettable()
    {
        // Arrange & Act
        var image = new ProductImage
        {
            ProductId = 1,
            ImageUrl = "/images/product1.jpg",
            AltText = "Product Image",
            SortOrder = 1,
            IsPrimary = true
        };

        // Assert
        image.ProductId.Should().Be(1);
        image.ImageUrl.Should().Be("/images/product1.jpg");
        image.AltText.Should().Be("Product Image");
        image.SortOrder.Should().Be(1);
        image.IsPrimary.Should().BeTrue();
    }

    [Fact]
    public void Review_Properties_ShouldBeSettable()
    {
        // Arrange & Act
        var review = new Review
        {
            ProductId = 1,
            UserId = 1,
            Rating = 5,
            Title = "Great Product",
            Comment = "Excellent quality and performance.",
            IsVerifiedPurchase = true
        };

        // Assert
        review.ProductId.Should().Be(1);
        review.UserId.Should().Be(1);
        review.Rating.Should().Be(5);
        review.Title.Should().Be("Great Product");
        review.Comment.Should().Be("Excellent quality and performance.");
        review.IsVerifiedPurchase.Should().BeTrue();
        review.CreatedAt.Should().NotBe(default(DateTime));
    }
}