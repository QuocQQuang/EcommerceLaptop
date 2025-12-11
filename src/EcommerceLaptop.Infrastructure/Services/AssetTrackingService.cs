using Microsoft.Extensions.Logging;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Inventory;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Services;
using EcommerceLaptop.Core.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace EcommerceLaptop.Infrastructure.Services;

public class AssetTrackingService : IAssetTrackingService
{
    private readonly IInventoryRepository _repository;
    private readonly ILogger<AssetTrackingService> _logger;

    public AssetTrackingService(IInventoryRepository repository, ILogger<AssetTrackingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<List<SerialNumberDto>> GetSerialNumbersAsync(int productId, bool activeOnly = true)
    {
        _logger.LogInformation("Getting serial numbers for product {ProductId}", productId);
        var serials = await _repository.GetSerialNumbersAsync(productId, activeOnly);
        
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
         
         if (await _repository.SerialNumberExistsAsync(serialNumber))
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
         
         await _repository.AddSerialNumberAsync(sn);
         return true;
    }

    public async Task<bool> ReserveSerialNumberAsync(string serialNumber, string orderReference)
    {
        var sn = await _repository.GetSerialNumberByValueAsync(serialNumber);
        if (sn == null) return false;
        
        if (sn.Status != SerialNumberStatus.Available)
        {
            throw new ValidationException($"Serial number {serialNumber} is not available (Status: {sn.Status})");
        }
        
        sn.Status = SerialNumberStatus.Reserved;
        sn.OrderReference = orderReference;
        
        await _repository.UpdateSerialNumberAsync(sn);
        return true;
    }

    public async Task<SerialNumberDto?> GetProductBySerialNumberAsync(string serialNumber)
    {
        var sn = await _repository.GetSerialNumberByValueAsync(serialNumber);
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
        var inventory = await _repository.GetInventoryByBarcodeAsync(barcode);
        return inventory != null ? MapToInventoryDto(inventory) : null;
    }

    public async Task<InventoryDto?> GetInventoryBySKUAsync(string sku)
    {
        var inventory = await _repository.GetInventoryBySkuAsync(sku);
        return inventory != null ? MapToInventoryDto(inventory) : null;
    }

    public async Task<bool> GenerateBarcodeAsync(int productId, string format = "EAN13")
    {
        var product = await _repository.GetProductByIdAsync(productId);
        if (product == null) return false;
        
        // Simple logic for demonstration
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
        var random = new Random().Next(1000, 9999).ToString();
        product.Barcode = $"{productId}{timestamp.Substring(timestamp.Length - 6)}{random}"; 
        
        await _repository.UpdateProductAsync(product);
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
