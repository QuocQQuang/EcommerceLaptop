import type { Product } from '@/types/api';
import type { Brand } from '../catalog/types';
import type { ProductFormData, ProductUpdateDto } from './types';

const findBrandIdByName = (brands: Brand[], brandName: string): string => {
  const brand = brands?.find(b => b.name.toLowerCase() === brandName.toLowerCase());
  return brand?.id?.toString() || '';
};

const withUnit = (value: unknown, unit: string): string => {
  if (value === null || value === undefined || value === '') return '';
  const text = value.toString().trim();
  return text.toLowerCase().includes(unit.trim().toLowerCase()) ? text : `${text}${unit}`;
};

const cpuCoresLabel = (value: unknown): string => {
  if (value === null || value === undefined || value === '') return '';
  const text = value.toString().trim();
  return /cores?/i.test(text) ? text : `${text} cores`;
};

const displaySizeLabel = (value: unknown): string => {
  if (value === null || value === undefined || value === '') return '';
  const text = value.toString().trim();
  return text.includes('"') ? text : `${text}"`;
};

const normalizeDisplayResolution = (value?: string): string => {
  if (!value) return '';
  const known: Record<string, string> = {
    '1366x768': '1366x768 (HD)',
    '1920x1080': '1920x1080 (FHD)',
    '2560x1440': '2560x1440 (QHD)',
    '2560x1600': '2560x1600 (WQXGA)',
    '2880x1800': '2880x1800 (Retina)',
    '3200x2000': '3200x2000 (3.2K)',
    '3840x2160': '3840x2160 (4K UHD)',
    '5120x2880': '5120x2880 (5K)',
    '6016x3384': '6016x3384 (6K)'
  };
  return known[value] || value;
};

export const mapProductToForm = (product: Product, brands: Brand[] = []): ProductFormData => ({
  name: product.name || '',
  sku: product.sku || '',
  description: product.description || '',
  categoryId: product.categoryId?.toString() || product.categories?.[0]?.id?.toString() || '',
  brandId: product.brandId?.toString() || findBrandIdByName(brands, product.brand || ''),
  price: product.price?.toString() || '',
  stock: product.stockQuantity?.toString() || '',
  weight: product.weightKg?.toString() || product.weight?.toString() || '',
  dimensions: product.dimensions || '',
  status: (product.isActive ? 'active' : 'inactive') as 'active' | 'inactive',
  images: product.images?.map(i => i.imageUrl) || [],
  productType: (product.productType || product.type || 'Laptop') as 'Laptop' | 'Accessory' | 'Bundle',

  inventory: {
    quantityInStock: product.inventory?.quantityInStock?.toString() || product.stockQuantity?.toString() || '',
    reservedQuantity: product.inventory?.reservedQuantity?.toString() || '0',
    reorderLevel: product.inventory?.reorderLevel?.toString() || '5',
    maxStockLevel: product.inventory?.maxStockLevel?.toString() || '100',
    warehouseLocation: product.inventory?.warehouseLocation || ''
  },

  series: product.series || '',
  model: product.model || '',
  cpuBrand: product.cpuBrand || '',
  cpuModel: product.cpuModel || '',
  cpuGeneration: product.cpuGeneration || '',
  cpuCores: cpuCoresLabel(product.cpuCores),
  cpuBaseClockGHz: product.cpuBaseClockGHz?.toString() || '',
  cpuBoostClockGHz: product.cpuBoostClockGHz?.toString() || '',
  cpuCache: product.cpuCache || '',
  ramType: product.ramType || '',
  ramCapacityGB: withUnit(product.ramCapacityGB, 'GB'),
  ramSlots: product.ramSlots?.toString() || '',
  ramSpeed: withUnit(product.ramSpeed, ' MHz'),
  ramUpgradeable: product.ramUpgradeable || false,
  storageType: product.storageType || '',
  storageCapacityGB: product.storageCapacityGB
    ? Number(product.storageCapacityGB) >= 1024 && Number(product.storageCapacityGB) % 1024 === 0
      ? `${Number(product.storageCapacityGB) / 1024}TB`
      : `${product.storageCapacityGB}GB`
    : '',
  storageInterface: product.storageInterface || '',
  nvMeSupport: product.nvMeSupport || false,
  gpuType: product.gpuType || '',
  gpuBrand: product.gpuBrand || '',
  gpuModel: product.gpuModel || '',
  gpuVramGB: withUnit(product.gpuVramGB, 'GB'),
  displaySizeInches: displaySizeLabel(product.displaySizeInches),
  displayResolution: normalizeDisplayResolution(product.displayResolution),
  displayPanelType: product.displayPanelType || '',
  displayRefreshRateHz: withUnit(product.displayRefreshRateHz, 'Hz'),
  displayTouchscreen: product.displayTouchscreen || false,
  batteryCapacityWh: withUnit(product.batteryCapacityWh, 'Wh'),
  weightKg: product.weightKg?.toString() || '',
  color: product.color || '',
  ports: product.ports || '',
  wiFi6Support: product.wiFi6Support || false,
  bluetoothSupport: product.bluetoothSupport || false,
  bluetoothVersion: product.bluetoothVersion || '',
  warrantyPeriod: product.warrantyPeriod || '',
  targetAudience: product.targetAudience || '',

  accessoryType: product.accessoryType || '',
  compatibility: product.compatibility || '',
  specificationDetails: product.specifications_json || '',
  connectivity: product.connectivity || '',

  bundleType: product.bundleType || '',
  discountPercentage: product.discountPercentage?.toString() || '',
  validFrom: product.validFrom || '',
  validTo: product.validTo || '',
  bundleItems: product.bundleItems?.map(item => ({
    productId: item.productId.toString(),
    quantity: item.quantity.toString(),
    discountPercentage: item.discountPercentage.toString()
  })) || [],

  cpu: product.cpuBrand && product.cpuModel
    ? `${product.cpuBrand} ${product.cpuModel} ${product.cpuGeneration || ''}`.trim()
    : product.cpuBrand || '',
  ram: product.ramCapacityGB && product.ramType
    ? `${product.ramCapacityGB}GB ${product.ramType} ${product.ramSpeed ? `${product.ramSpeed}MHz` : ''}`.trim()
    : product.ramCapacityGB ? `${product.ramCapacityGB}GB` : '',
  storage: product.storageCapacityGB && product.storageType
    ? `${product.storageCapacityGB}GB ${product.storageType} ${product.storageInterface || ''}`.trim()
    : product.storageCapacityGB ? `${product.storageCapacityGB}GB SSD` : '',
  gpu: product.gpuBrand && product.gpuModel
    ? `${product.gpuBrand} ${product.gpuModel} ${product.gpuVramGB ? `${product.gpuVramGB}GB` : ''}`.trim()
    : product.gpuBrand || '',
  display: product.displaySizeInches && product.displayResolution
    ? `${product.displaySizeInches}" ${product.displayResolution} ${product.displayPanelType || ''} ${product.displayRefreshRateHz ? `${product.displayRefreshRateHz}Hz` : ''}`.trim()
    : product.displaySizeInches ? `${product.displaySizeInches}" Display` : '',
  battery: product.batteryCapacityWh
    ? `${product.batteryCapacityWh}Wh`
    : product.battery?.toString() || '',
  weight_kg: product.weightKg?.toString() || product.weight?.toString() || ''
});

export const mapFormToUpdatePayload = (formData: ProductFormData): ProductUpdateDto => {
  const parseCpu = (cpu?: string): { brand?: string; model?: string; generation?: string } => {
    if (!cpu) return {};
    const parts = cpu.split(/\s+/).filter(Boolean);
    const brand = parts[0];
    const genMatch = cpu.match(/(\d+\w*\s*Gen)/i);
    return {
      brand,
      model: parts.slice(1).join(' ').replace(/\s*\d+\w*\s*Gen/i, '').trim() || undefined,
      generation: genMatch ? genMatch[1] : undefined
    };
  };

  const parseRam = (ram?: string): { capacityGB?: number; type?: string; speed?: number } => {
    if (!ram) return {};
    const capacity = ram.match(/(\d+)\s*GB/i);
    const type = ram.match(/(DDR\d|LPDDR\d)/i);
    const speed = ram.match(/(\d{3,5})\s*MHz/i);
    return {
      capacityGB: capacity ? Number(capacity[1]) : undefined,
      type: type ? type[1].toUpperCase() : undefined,
      speed: speed ? Number(speed[1]) : undefined
    };
  };

  const parseStorage = (storage?: string): { capacityGB?: number; type?: string; iface?: string } => {
    if (!storage) return {};
    const capacity = storage.match(/(\d+(?:\.\d+)?)\s*(TB|GB)/i);
    const type = storage.match(/(SSD|HDD)/i);
    const iface = storage.match(/(NVMe|SATA|PCIe)/i);
    let capacityGB: number | undefined;
    if (capacity) {
      const val = Number(capacity[1]);
      capacityGB = capacity[2].toUpperCase() === 'TB' ? Math.round(val * 1024) : Math.round(val);
    }
    return {
      capacityGB,
      type: type ? type[1].toUpperCase() : undefined,
      iface: iface ? iface[1].toUpperCase() : undefined
    };
  };

  const parseDisplay = (display?: string): { sizeInches?: number; resolution?: string; panel?: string; refreshHz?: number } => {
    if (!display) return {};
    const size = display.match(/(\d{1,2}(?:\.\d{1,2})?)\s*"/);
    const resolution = display.match(/(\d{3,4}x\d{3,4})/i);
    const panel = display.match(/\b(IPS|OLED|TN|VA|Mini-LED|QLED)\b/i);
    const refresh = display.match(/(\d{2,3})\s*Hz/i);
    return {
      sizeInches: size ? Number(size[1]) : undefined,
      resolution: resolution ? resolution[1] : undefined,
      panel: panel ? panel[1].toUpperCase() : undefined,
      refreshHz: refresh ? Number(refresh[1]) : undefined
    };
  };

  const parseBatteryWh = (battery?: string): number | undefined => {
    if (!battery) return undefined;
    const m = battery.match(/(\d+(?:\.\d+)?)\s*Wh/i);
    return m ? Number(m[1]) : undefined;
  };

  const parseWeightKg = (w?: string): number | undefined => {
    if (!w) return undefined;
    const kg = w.match(/(\d+(?:\.\d+)?)\s*kg/i);
    if (kg) return Number(kg[1]);
    const just = w.match(/(\d+(?:\.\d+)?)/);
    return just ? Number(just[1]) : undefined;
  };

  const parseCapacityGB = (value?: string): number | undefined => {
    if (!value) return undefined;
    const match = value.match(/(\d+(?:\.\d+)?)\s*(TB|GB)?/i);
    if (!match) return undefined;
    const amount = Number(match[1]);
    return match[2]?.toUpperCase() === 'TB' ? Math.round(amount * 1024) : Math.round(amount);
  };

  const parseGpu = (gpu?: string): { brand?: string; model?: string; vramGB?: number } => {
    if (!gpu) return {};
    const vram = gpu.match(/(\d+)\s*GB/i);
    const parts = gpu.replace(/(\d+)\s*GB/i, '').trim().split(/\s+/);
    const brand = parts[0];
    const model = parts.slice(1).join(' ').trim();
    return {
      brand: brand || undefined,
      model: model || undefined,
      vramGB: vram ? Number(vram[1]) : undefined
    };
  };

  const payload: ProductUpdateDto = {
    Name: formData.name,
    SKU: formData.sku,
    Description: formData.description,
    Model: formData.model || '',
    BrandId: parseInt(formData.brandId) || 0,
    CategoryId: formData.categoryId ? parseInt(formData.categoryId) : undefined,
    Price: parseFloat(formData.price) || 0,
    IsActive: formData.status === 'active',
    StockQuantity: formData.inventory?.quantityInStock ? parseInt(formData.inventory.quantityInStock) : (parseInt(formData.stock) || 0),
    ProductType: formData.productType
  };

  if (formData.productType === 'Laptop') {
    payload.Series = formData.series || '';
    if (formData.model) (payload as any).Model = formData.model;
    if (formData.cpuBrand) (payload as any).CpuBrand = formData.cpuBrand;
    if (formData.cpuModel) (payload as any).CpuModel = formData.cpuModel;
    if (formData.cpuGeneration) (payload as any).CpuGeneration = formData.cpuGeneration;
    if (formData.cpuCores) (payload as any).CpuCores = parseInt(formData.cpuCores);
    if (formData.cpuBaseClockGHz) (payload as any).CpuBaseClockGHz = parseFloat(formData.cpuBaseClockGHz);
    if (formData.cpuBoostClockGHz) (payload as any).CpuBoostClockGHz = parseFloat(formData.cpuBoostClockGHz);
    if (formData.cpuCache) (payload as any).CpuCache = formData.cpuCache;
    if (formData.ramType) (payload as any).RamType = formData.ramType;
    if (formData.ramCapacityGB) (payload as any).RamCapacityGB = parseInt(formData.ramCapacityGB);
    if (formData.ramSlots) (payload as any).RamSlots = parseInt(formData.ramSlots);
    if (formData.ramSpeed) (payload as any).RamSpeed = parseInt(formData.ramSpeed);
    if (formData.ramUpgradeable !== undefined) (payload as any).RamUpgradeable = formData.ramUpgradeable;
    if (formData.storageType) (payload as any).StorageType = formData.storageType;
    if (formData.storageCapacityGB) {
      const storageCapacityGB = parseCapacityGB(formData.storageCapacityGB);
      if (typeof storageCapacityGB === 'number') (payload as any).StorageCapacityGB = storageCapacityGB;
    }
    if (formData.storageInterface) (payload as any).StorageInterface = formData.storageInterface;
    if (formData.nvMeSupport !== undefined) (payload as any).NvMeSupport = formData.nvMeSupport;
    if (formData.gpuType) (payload as any).GpuType = formData.gpuType;
    if (formData.gpuBrand) (payload as any).GpuBrand = formData.gpuBrand;
    if (formData.gpuModel) (payload as any).GpuModel = formData.gpuModel;
    if (formData.gpuVramGB) (payload as any).GpuVramGB = parseInt(formData.gpuVramGB);
    if (formData.displaySizeInches) (payload as any).DisplaySizeInches = parseFloat(formData.displaySizeInches);
    if (formData.displayResolution) (payload as any).DisplayResolution = formData.displayResolution;
    if (formData.displayPanelType) (payload as any).DisplayPanelType = formData.displayPanelType;
    if (formData.displayRefreshRateHz) (payload as any).DisplayRefreshRateHz = parseInt(formData.displayRefreshRateHz);
    if (formData.displayTouchscreen !== undefined) (payload as any).DisplayTouchscreen = formData.displayTouchscreen;
    if (formData.batteryCapacityWh) (payload as any).BatteryCapacityWh = parseInt(formData.batteryCapacityWh);
    if (formData.weightKg) (payload as any).WeightKg = parseFloat(formData.weightKg);
    if (formData.color) (payload as any).Color = formData.color;
    if (formData.ports) (payload as any).Ports = formData.ports;
    if (formData.wiFi6Support !== undefined) (payload as any).WiFi6Support = formData.wiFi6Support;
    if (formData.bluetoothSupport !== undefined) (payload as any).BluetoothSupport = formData.bluetoothSupport;
    if (formData.bluetoothVersion) (payload as any).BluetoothVersion = formData.bluetoothVersion;
    if (formData.warrantyPeriod) (payload as any).WarrantyPeriod = formData.warrantyPeriod;
    if (formData.targetAudience) (payload as any).TargetAudience = formData.targetAudience;

    const cpu = parseCpu(formData.cpu);
    if (cpu.brand) (payload as any).CpuBrand = cpu.brand;
    if (cpu.model) (payload as any).CpuModel = cpu.model;
    if (cpu.generation) (payload as any).CpuGeneration = cpu.generation;

    const ram = parseRam(formData.ram);
    if (typeof ram.capacityGB === 'number') (payload as any).RamCapacityGB = ram.capacityGB;
    if (ram.type) (payload as any).RamType = ram.type;
    if (typeof ram.speed === 'number') (payload as any).RamSpeed = ram.speed;

    const storage = parseStorage(formData.storage);
    if (storage.type) (payload as any).StorageType = storage.type;
    if (typeof storage.capacityGB === 'number') (payload as any).StorageCapacityGB = storage.capacityGB;
    if (storage.iface) (payload as any).StorageInterface = storage.iface;

    const gpu = parseGpu(formData.gpu);
    if (gpu.brand) (payload as any).GpuBrand = gpu.brand;
    if (gpu.model) (payload as any).GpuModel = gpu.model;
    if (typeof gpu.vramGB === 'number') (payload as any).GpuVramGB = gpu.vramGB;

    const disp = parseDisplay(formData.display);
    if (typeof disp.sizeInches === 'number') (payload as any).DisplaySizeInches = disp.sizeInches;
    if (disp.resolution) (payload as any).DisplayResolution = disp.resolution;
    if (disp.panel) (payload as any).DisplayPanelType = disp.panel;
    if (typeof disp.refreshHz === 'number') (payload as any).DisplayRefreshRateHz = disp.refreshHz;

    const batteryWh = parseBatteryWh(formData.battery);
    if (typeof batteryWh === 'number') (payload as any).BatteryCapacityWh = batteryWh;

    const weightKg = parseWeightKg(formData.weight_kg);
    if (typeof weightKg === 'number') (payload as any).WeightKg = weightKg;
  } else if (formData.productType === 'Accessory') {
    if (formData.accessoryType) {
      (payload as any).AccessoryType = formData.accessoryType;
      (payload as any).Type = formData.accessoryType;
    }
    if (formData.compatibility) (payload as any).Compatibility = formData.compatibility;
    if (formData.specificationDetails) (payload as any).SpecificationDetails = formData.specificationDetails;
    if (formData.connectivity) (payload as any).Connectivity = formData.connectivity;
    if (formData.color) (payload as any).Color = formData.color;
  } else if (formData.productType === 'Bundle') {
    if (formData.bundleType) (payload as any).BundleType = formData.bundleType;
    if (formData.discountPercentage) (payload as any).DiscountPercentage = parseFloat(formData.discountPercentage);
    if (formData.validFrom) (payload as any).ValidFrom = formData.validFrom;
    if (formData.validTo) (payload as any).ValidTo = formData.validTo;
    if (formData.bundleItems) (payload as any).BundleItems = formData.bundleItems.map(item => ({
      ProductId: parseInt(item.productId),
      Quantity: parseInt(item.quantity),
      DiscountPercentage: parseFloat(item.discountPercentage)
    }));
  }

  return payload;
};

export const mapFormToCreatePayload = (formData: ProductFormData, brands: Brand[] = []): any => {
  const payload: any = mapFormToUpdatePayload(formData);
  const brand = brands.find(b => b.id === Number(payload.BrandId));
  if (brand?.name) {
    payload.Brand = brand.name;
  }
  return payload;
};
