using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Inventory;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Specifications.InventorySpecs;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Infrastructure.Services;

public class AssetTrackingService : IAssetTrackingService
{
    private readonly IAsyncRepository<SerialNumber> _serialNumberRepository;
    private readonly IAsyncRepository<Inventory> _inventoryRepository;
    private readonly IAsyncRepository<Product> _productRepository;
    private readonly ILogger<AssetTrackingService> _logger;

    public AssetTrackingService(
        IAsyncRepository<SerialNumber> serialNumberRepository,
        IAsyncRepository<Inventory> inventoryRepository,
        IAsyncRepository<Product> productRepository,
        ILogger<AssetTrackingService> logger)
    {
        _serialNumberRepository = serialNumberRepository;
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _logger = logger;
    }

    public async Task<List<SerialNumberDto>> GetSerialNumbersAsync(int productId, bool activeOnly = true)
    {
        _logger.LogInformation("Getting serial numbers for product {ProductId}", productId);
        var serials = await _serialNumberRepository.GetAsync(new SerialNumberSpecification(productId, activeOnly));
        
        return serials.Select(s => new SerialNumberDto
        {
            Id = s.Id,
            ProductId = s.ProductId,
            Value = s.Value,
            Status = s.Status.ToString(),
            BatchNumber = s.BatchNumber,
            DateReceived = s.DateReceived,
            DateSold = s.DateSold,
            OrderReference = s.OrderReference ?? string.Empty
        }).ToList();
    }

    public async Task<List<SerialNumberDto>> GetAvailableSerialNumbersAsync(int productId)
    {
        return await GetSerialNumbersAsync(productId, activeOnly: true);
    }

    public async Task<bool> AssignSerialNumberAsync(int productId, string serialNumber, string batchNumber = "")
    {
         _logger.LogInformation("Assigning new serial number {SerialNumber} for product {ProductId}", serialNumber, productId);
         
         var existing = await _serialNumberRepository.GetEntityWithSpec(new SerialNumberByValueSpecification(serialNumber));
         if (existing != null)
         {
             throw new ValidationException($"Serial number {serialNumber} already exists");
         }
         
         var sn = new SerialNumber
         {
             ProductId = productId,
             Value = serialNumber,
             Status = SerialNumberStatus.Available,
             BatchNumber = batchNumber,
             DateReceived = DateTime.UtcNow
         };
         
         await _serialNumberRepository.AddAsync(sn);
         return true;
    }

    public async Task<bool> ReserveSerialNumberAsync(string serialNumber, string orderReference)
    {
        var sn = await _serialNumberRepository.GetEntityWithSpec(new SerialNumberByValueSpecification(serialNumber));
        if (sn == null) return false;
        
        if (sn.Status != SerialNumberStatus.Available)
        {
            throw new ValidationException($"Serial number {serialNumber} is not available (Status: {sn.Status})");
        }
        
        sn.Status = SerialNumberStatus.Reserved;
        sn.OrderReference = orderReference;
        
        await _serialNumberRepository.UpdateAsync(sn);
        return true;
    }

    public async Task<SerialNumberDto?> GetProductBySerialNumberAsync(string serialNumber)
    {
        var sn = await _serialNumberRepository.GetEntityWithSpec(new SerialNumberByValueSpecification(serialNumber));
        if (sn == null) return null;
        
        return new SerialNumberDto
        {
            Id = sn.Id,
            ProductId = sn.ProductId,
            Value = sn.Value,
            Status = sn.Status.ToString(),
            BatchNumber = sn.BatchNumber,
            DateReceived = sn.DateReceived,
            DateSold = sn.DateSold,
            OrderReference = sn.OrderReference ?? string.Empty
        };
    }

    public async Task<InventoryDto?> GetInventoryByBarcodeAsync(string barcode)
    {
        var inventory = await _inventoryRepository.GetEntityWithSpec(new InventoryByBarcodeSpecification(barcode));
        return inventory != null ? MapToInventoryDto(inventory) : null;
    }

    public async Task<InventoryDto?> GetInventoryBySKUAsync(string sku)
    {
        // SKU is unique? Assuming yes.
        var inventory = await _inventoryRepository.GetEntityWithSpec(new InventoryBySkuSpecification(sku));
        return inventory != null ? MapToInventoryDto(inventory) : null;
    }

    public async Task<bool> GenerateBarcodeAsync(int productId, string format = "EAN13")
    {
        var product = await _productRepository.GetByIdAsync(productId);
        if (product == null) return false;
        
        // Simple logic for demonstration
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var random = new Random().Next(1000, 9999).ToString();
        product.Barcode = $"{productId}{timestamp.Substring(timestamp.Length - 6)}{random}"; 
        
        await _productRepository.UpdateAsync(product);
        return true;
    }

    public async Task<List<InventoryDto>> ScanMultipleBarcodesAsync(List<string> barcodes)
    {
        var inventories = new List<InventoryDto>();
        foreach(var barcode in barcodes.Distinct())
        {
            var inv = await GetInventoryByBarcodeAsync(barcode);
            if(inv != null) inventories.Add(inv);
        }
        return inventories;
    }
    
    private InventoryDto MapToInventoryDto(Inventory inventory)
    {
        return new InventoryDto
        {
            Id = inventory.Id,
            ProductId = inventory.ProductId,
            ProductName = inventory.Product?.Name ?? string.Empty,
            ProductSKU = inventory.Product?.SKU ?? string.Empty,
            ProductBrand = inventory.Product?.Brand ?? string.Empty,
            QuantityInStock = inventory.QuantityInStock,
            ReservedQuantity = inventory.ReservedQuantity,
            ReorderLevel = inventory.ReorderLevel,
            MaxStockLevel = inventory.MaxStockLevel,
            WarehouseLocation = inventory.WarehouseLocation,
            LastStockUpdate = inventory.LastStockUpdate,
            UnitCost = 100m 
        };
    }
}
