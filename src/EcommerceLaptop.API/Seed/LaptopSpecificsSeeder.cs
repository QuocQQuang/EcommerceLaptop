using EcommerceLaptop.Infrastructure.Data;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace EcommerceLaptop.API.Seed;

public static class LaptopSpecificsSeeder
{
    public static void NormalizeSpecs(ApplicationDbContext context, ILogger logger)
    {
        // Define standardized values (2-3 options per spec) for easier filtering
        var cpuBrands = new[] { "Intel", "AMD" };
        var cpuGenerations = new[] { "Intel 12th", "Intel 13th", "AMD Ryzen 5" };
        var cpuCores = new[] { 4, 8, 12 };
        var cpuBaseClocks = new[] { 2.4m, 3.0m, 3.5m }; // GHz
        var cpuBoostClocks = new[] { 4.0m, 4.5m, 5.0m }; // GHz
        var cpuCaches = new[] { "8MB", "12MB", "16MB" };

        var ramTypes = new[] { "DDR4", "DDR5" };
        var ramCapacities = new[] { 16, 32 }; // GB
        var ramSlots = new[] { 1, 2 };
        var ramSpeeds = new[] { 2666, 3200, 4800 }; // MHz

        var storageTypes = new[] { "SSD", "HDD" };
        var storageCapacities = new[] { 512, 1024 }; // GB
        var storageInterfaces = new[] { "NVMe", "SATA" };

        var gpuTypes = new[] { "Integrated", "Discrete" };
        var gpuBrands = new[] { "NVIDIA", "AMD", "Intel" };
        var gpuVram = new[] { 0, 4, 8 }; // GB

        var displaySizes = new[] { 13.3m, 15.6m, 16.0m };
        var displayResolutions = new[] { "1920x1080", "2560x1440", "3840x2160" };
        var displayPanels = new[] { "IPS", "OLED", "TN" };
        var displayRefreshRates = new[] { 60, 120 }; // Hz

        var colors = new[] { "Black", "Silver", "Gray" };
        var bluetoothVersions = new[] { "5.0", "5.1", "5.2" };
        var warranties = new[] { "12 months", "24 months" };
        var audiences = new[] { "Gaming", "Business", "Student" };

        var laptops = context.Laptops.AsQueryable();

        // Normalize all laptops
        var toNormalize = laptops.ToList();

        if (!toNormalize.Any())
        {
            logger.LogInformation("Laptop specs already standardized. Skipping normalization.");
            return;
        }

        int ClampToClosestInt(int current, int[] allowed)
        {
            return allowed.OrderBy(v => Math.Abs(v - current)).First();
        }
        decimal ClampToClosestDec(decimal current, decimal[] allowed)
        {
            return allowed.OrderBy(v => Math.Abs(v - current)).First();
        }
        string NormalizeToSet(string? current, string[] allowed, string fallback)
        {
            if (string.IsNullOrWhiteSpace(current)) return fallback;
            var exact = allowed.FirstOrDefault(a => a.Equals(current, StringComparison.OrdinalIgnoreCase));
            return exact ?? fallback;
        }

        foreach (var l in toNormalize)
        {
            // CPU
            l.CpuBrand = NormalizeToSet(l.CpuBrand, cpuBrands, cpuBrands[0]);
            l.CpuGeneration = NormalizeToSet(l.CpuGeneration, cpuGenerations, cpuGenerations[0]);
            l.CpuCores = ClampToClosestInt(l.CpuCores <= 0 ? 8 : l.CpuCores, cpuCores);
            l.CpuBaseClockGHz = ClampToClosestDec(l.CpuBaseClockGHz <= 0 ? 3.0m : l.CpuBaseClockGHz, cpuBaseClocks);
            l.CpuBoostClockGHz = ClampToClosestDec(l.CpuBoostClockGHz <= 0 ? 4.5m : l.CpuBoostClockGHz, cpuBoostClocks);
            l.CpuCache = NormalizeToSet(l.CpuCache, cpuCaches, cpuCaches[0]);

            // RAM
            l.RamType = NormalizeToSet(l.RamType, ramTypes, ramTypes[0]);
            l.RamCapacityGB = ClampToClosestInt(l.RamCapacityGB <= 0 ? 16 : l.RamCapacityGB, ramCapacities);
            l.RamSlots = ClampToClosestInt(l.RamSlots <= 0 ? 2 : l.RamSlots, ramSlots);
            l.RamSpeed = ClampToClosestInt(l.RamSpeed <= 0 ? 3200 : l.RamSpeed, ramSpeeds);

            // Storage
            l.StorageType = NormalizeToSet(l.StorageType, storageTypes, storageTypes[0]);
            l.StorageCapacityGB = ClampToClosestInt(l.StorageCapacityGB <= 0 ? 512 : l.StorageCapacityGB, storageCapacities);
            l.StorageInterface = NormalizeToSet(l.StorageInterface, storageInterfaces, storageInterfaces[0]);
            l.NvMeSupport = l.StorageInterface.Equals("NVMe", StringComparison.OrdinalIgnoreCase);

            // GPU
            var gpu = (l.GpuType ?? string.Empty).Trim();
            if (!gpuTypes.Contains(gpu, StringComparer.OrdinalIgnoreCase))
            {
                // Heuristic: if has VRAM > 0 then Discrete else Integrated
                l.GpuType = l.GpuVramGB > 0 ? "Discrete" : "Integrated";
            }
            else
            {
                l.GpuType = gpuTypes.First(x => x.Equals(gpu, StringComparison.OrdinalIgnoreCase));
            }
            l.GpuBrand = NormalizeToSet(l.GpuBrand, gpuBrands, l.GpuType.Equals("Integrated", StringComparison.OrdinalIgnoreCase) ? "Intel" : "NVIDIA");
            l.GpuVramGB = ClampToClosestInt(l.GpuVramGB < 0 ? 0 : l.GpuVramGB, gpuVram);
            if (l.GpuType.Equals("Integrated", StringComparison.OrdinalIgnoreCase))
            {
                l.GpuVramGB = 0;
            }

            // Display
            l.DisplaySizeInches = ClampToClosestDec(l.DisplaySizeInches <= 0 ? 15.6m : l.DisplaySizeInches, displaySizes);
            l.DisplayResolution = NormalizeToSet(l.DisplayResolution, displayResolutions, displayResolutions[0]);
            l.DisplayPanelType = NormalizeToSet(l.DisplayPanelType, displayPanels, displayPanels[0]);
            l.DisplayRefreshRateHz = ClampToClosestInt(l.DisplayRefreshRateHz <= 0 ? 60 : l.DisplayRefreshRateHz, displayRefreshRates);

            // Physical & Connectivity
            l.BatteryCapacityWh = ClampToClosestInt(l.BatteryCapacityWh <= 0 ? 70 : l.BatteryCapacityWh, new[] { 50, 70, 90 });
            l.WeightKg = ClampToClosestDec(l.WeightKg <= 0 ? 1.5m : l.WeightKg, new[] { 1.2m, 1.5m, 2.0m });
            l.Color = NormalizeToSet(l.Color, colors, colors[0]);
            l.BluetoothVersion = NormalizeToSet(l.BluetoothVersion, bluetoothVersions, bluetoothVersions[0]);

            // Business info
            l.WarrantyPeriod = NormalizeToSet(l.WarrantyPeriod, warranties, warranties[0]);
            l.TargetAudience = NormalizeToSet(l.TargetAudience, audiences, audiences[1]);
        }

        context.SaveChanges();
        logger.LogInformation("Standardized specs for {Count} laptops to 2-3 canonical values per field.", toNormalize.Count);
    }
}


