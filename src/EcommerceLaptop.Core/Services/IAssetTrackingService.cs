using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Core.DTOs.Inventory;

namespace EcommerceLaptop.Core.Services;

public interface IAssetTrackingService
{
    // Serial Numbers
    Task<List<SerialNumberDto>> GetSerialNumbersAsync(int productId, bool activeOnly = true);
    Task<List<SerialNumberDto>> GetAvailableSerialNumbersAsync(int productId);
    Task<bool> AssignSerialNumberAsync(int productId, string serialNumber, string batchNumber = "");
    Task<bool> ReserveSerialNumberAsync(string serialNumber, string orderReference);
    Task<SerialNumberDto?> GetProductBySerialNumberAsync(string serialNumber);

    // Barcodes
    Task<InventoryDto?> GetInventoryByBarcodeAsync(string barcode);
    Task<InventoryDto?> GetInventoryBySKUAsync(string sku);
    Task<bool> GenerateBarcodeAsync(int productId, string format = "EAN13");
    Task<List<InventoryDto>> ScanMultipleBarcodesAsync(List<string> barcodes);
}
