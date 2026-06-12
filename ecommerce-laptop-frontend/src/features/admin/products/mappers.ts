import type { Product } from '@/types/api';
import type { Brand } from '../catalog/types';
import type { ProductFormData, ProductUpdateDto } from './types';

const findBrandIdByName = (brands: Brand[], brandName: string): string => {
  const brand = brands?.find(b => b.name.toLowerCase() === brandName.toLowerCase());
  return brand?.id?.toString() || '';
};

const read = <T = any>(source: any, ...keys: string[]): T | undefined => {
  for (const key of keys) {
    const value = source?.[key];
    if (value !== undefined && value !== null) return value as T;
  }
  return undefined;
};

const text = (value: unknown): string => {
  if (value === null || value === undefined) return '';
  return value.toString();
};

const bool = (value: unknown, fallback = false): boolean => {
  if (typeof value === 'boolean') return value;
  if (typeof value === 'string') return value.toLowerCase() === 'true';
  return fallback;
};

const normalizeSpecName = (value: unknown): string => {
  return text(value).toLowerCase().replace(/[^a-z0-9]/g, '');
};

const readSpec = (source: any, ...names: string[]): unknown => {
  const specs = read<any[]>(source, 'specifications', 'Specifications') || [];
  const wanted = new Set(names.map(normalizeSpecName));
  const spec = specs.find(item => wanted.has(normalizeSpecName(read(item, 'name', 'Name'))));
  return read(spec, 'value', 'Value');
};

const readProductValue = (product: any, baseProduct: any, keys: string[], specNames: string[] = keys): unknown => {
  return read(product, ...keys) ??
    read(baseProduct, ...keys) ??
    readSpec(product, ...specNames) ??
    readSpec(baseProduct, ...specNames);
};

const withUnit = (value: unknown, unit: string): string => {
  if (value === null || value === undefined || value === '') return '';
  const text = value.toString().trim();
  return text.toLowerCase().includes(unit.trim().toLowerCase()) ? text : `${text}${unit}`;
};

const compactUnit = (value: unknown, unit: string): string => {
  if (value === null || value === undefined || value === '') return '';
  const match = value.toString().match(/(\d+(?:\.\d+)?)/);
  return match ? `${match[1]}${unit}` : value.toString().trim();
};

const spacedUnit = (value: unknown, unit: string): string => {
  if (value === null || value === undefined || value === '') return '';
  const match = value.toString().match(/(\d+(?:\.\d+)?)/);
  return match ? `${match[1]} ${unit}` : value.toString().trim();
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

const numericText = (value: unknown): string => {
  if (value === null || value === undefined || value === '') return '';
  const match = value.toString().match(/-?\d+(?:\.\d+)?/);
  return match?.[0] ?? '';
};

const storageCapacityLabel = (value: unknown): string => {
  if (value === null || value === undefined || value === '') return '';
  const raw = value.toString().trim();
  const match = raw.match(/(\d+(?:\.\d+)?)\s*(TB|GB)?/i);
  if (!match) return raw;

  const amount = Number(match[1]);
  const unit = match[2]?.toUpperCase();
  if (unit === 'TB') return `${amount}TB`;
  if (unit === 'GB') {
    return amount >= 1024 && amount % 1024 === 0 ? `${amount / 1024}TB` : `${amount}GB`;
  }
  return amount >= 1024 && amount % 1024 === 0 ? `${amount / 1024}TB` : `${amount}GB`;
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

export const mapProductToForm = (product: Product, brands: Brand[] = [], baseProduct?: Product | null): ProductFormData => {
  const raw = product as any;
  const base = baseProduct as any;
  const category = read<any>(raw, 'category', 'Category') ?? read<any>(base, 'category', 'Category');
  const categories = read<any[]>(raw, 'categories', 'Categories') || read<any[]>(base, 'categories', 'Categories') || [];
  const brandName = text(read(raw, 'brand', 'Brand') ?? read(base, 'brand', 'Brand'));
  const inventory = read<any>(raw, 'inventory', 'Inventory') || {};
  const stockQuantity = read(raw, 'stockQuantity', 'StockQuantity');
  const images = read<any[]>(raw, 'images', 'Images') || [];
  const isActive = read(raw, 'isActive', 'IsActive');
  const productType = text(read(raw, 'productType', 'ProductType', 'type', 'Type') ?? read(base, 'productType', 'ProductType', 'type', 'Type') ?? 'Laptop');

  return {
  name: text(read(raw, 'name', 'Name')),
  sku: text(read(raw, 'sku', 'SKU')),
  description: text(read(raw, 'description', 'Description')),
  categoryId: text(
    read(raw, 'categoryId', 'CategoryId') ??
    read(base, 'categoryId', 'CategoryId') ??
    read(category, 'id', 'Id') ??
    read(categories[0], 'id', 'Id')
  ),
  brandId: text(read(raw, 'brandId', 'BrandId') ?? read(base, 'brandId', 'BrandId')) || findBrandIdByName(brands, brandName),
  price: text(read(raw, 'price', 'Price')),
  stock: text(stockQuantity),
  weight: text(readProductValue(raw, base, ['weightKg', 'WeightKg', 'weight', 'Weight'], ['Weight', 'Weight Kg', 'WeightKg'])),
  dimensions: text(readProductValue(raw, base, ['dimensions', 'Dimensions'])),
  status: (isActive === false ? 'inactive' : 'active') as 'active' | 'inactive',
  images: images.map(i => read<string>(i, 'imageUrl', 'ImageUrl')).filter(Boolean) as string[],
  productType: productType as 'Laptop' | 'Accessory' | 'Bundle',

  inventory: {
    quantityInStock: text(read(inventory, 'quantityInStock', 'QuantityInStock', 'availableQuantity', 'AvailableQuantity') ?? stockQuantity),
    reservedQuantity: text(read(inventory, 'reservedQuantity', 'ReservedQuantity') ?? '0'),
    reorderLevel: text(read(inventory, 'reorderLevel', 'ReorderLevel') ?? '5'),
    maxStockLevel: text(read(inventory, 'maxStockLevel', 'MaxStockLevel') ?? '100'),
    warehouseLocation: text(read(inventory, 'warehouseLocation', 'WarehouseLocation'))
  },

  series: text(readProductValue(raw, base, ['series', 'Series'])),
  model: text(readProductValue(raw, base, ['model', 'Model'])),
  cpuBrand: text(readProductValue(raw, base, ['cpuBrand', 'CpuBrand'], ['CPU Brand', 'Cpu Brand'])),
  cpuModel: text(readProductValue(raw, base, ['cpuModel', 'CpuModel'], ['CPU Model', 'Cpu Model'])),
  cpuGeneration: text(readProductValue(raw, base, ['cpuGeneration', 'CpuGeneration'], ['CPU Generation', 'Cpu Generation'])),
  cpuCores: cpuCoresLabel(readProductValue(raw, base, ['cpuCores', 'CpuCores'], ['CPU Cores', 'Cpu Cores'])),
  cpuBaseClockGHz: numericText(readProductValue(raw, base, ['cpuBaseClockGHz', 'CpuBaseClockGHz'], ['Base Clock', 'CPU Base Clock'])),
  cpuBoostClockGHz: numericText(readProductValue(raw, base, ['cpuBoostClockGHz', 'CpuBoostClockGHz'], ['Boost Clock', 'CPU Boost Clock'])),
  cpuCache: text(readProductValue(raw, base, ['cpuCache', 'CpuCache'], ['CPU Cache', 'Cache'])),
  ramType: text(readProductValue(raw, base, ['ramType', 'RamType'], ['RAM Type', 'Ram Type'])),
  ramCapacityGB: compactUnit(readProductValue(raw, base, ['ramCapacityGB', 'RamCapacityGB'], ['RAM Capacity', 'Ram Capacity']), 'GB'),
  ramSlots: text(readProductValue(raw, base, ['ramSlots', 'RamSlots'], ['RAM Slots', 'Ram Slots'])),
  ramSpeed: spacedUnit(readProductValue(raw, base, ['ramSpeed', 'RamSpeed'], ['RAM Speed', 'Ram Speed']), 'MHz'),
  ramUpgradeable: bool(readProductValue(raw, base, ['ramUpgradeable', 'RamUpgradeable'])),
  storageType: text(readProductValue(raw, base, ['storageType', 'StorageType'], ['Storage Type'])),
  storageCapacityGB: storageCapacityLabel(readProductValue(raw, base, ['storageCapacityGB', 'StorageCapacityGB'], ['Storage Capacity'])),
  storageInterface: text(readProductValue(raw, base, ['storageInterface', 'StorageInterface'], ['Storage Interface'])),
  nvMeSupport: bool(readProductValue(raw, base, ['nvMeSupport', 'NvMeSupport', 'NVMeSupport'])),
  gpuType: text(readProductValue(raw, base, ['gpuType', 'GpuType'], ['GPU Type'])),
  gpuBrand: text(readProductValue(raw, base, ['gpuBrand', 'GpuBrand'], ['GPU Brand'])),
  gpuModel: text(readProductValue(raw, base, ['gpuModel', 'GpuModel'], ['GPU Model'])),
  gpuVramGB: compactUnit(readProductValue(raw, base, ['gpuVramGB', 'GpuVramGB'], ['VRAM', 'GPU VRAM']), 'GB'),
  displaySizeInches: displaySizeLabel(readProductValue(raw, base, ['displaySizeInches', 'DisplaySizeInches'], ['Display Size'])),
  displayResolution: normalizeDisplayResolution(text(readProductValue(raw, base, ['displayResolution', 'DisplayResolution'], ['Resolution', 'Display Resolution']))),
  displayPanelType: text(readProductValue(raw, base, ['displayPanelType', 'DisplayPanelType'], ['Display Panel', 'Panel Type'])),
  displayRefreshRateHz: compactUnit(readProductValue(raw, base, ['displayRefreshRateHz', 'DisplayRefreshRateHz'], ['Refresh Rate', 'Display Refresh Rate']), 'Hz'),
  displayTouchscreen: bool(readProductValue(raw, base, ['displayTouchscreen', 'DisplayTouchscreen'], ['Touchscreen'])),
  batteryCapacityWh: compactUnit(readProductValue(raw, base, ['batteryCapacityWh', 'BatteryCapacityWh'], ['Battery', 'Battery Capacity']), 'Wh'),
  weightKg: text(readProductValue(raw, base, ['weightKg', 'WeightKg', 'weight', 'Weight'], ['Weight'])),
  color: text(readProductValue(raw, base, ['color', 'Color'])),
  ports: text(readProductValue(raw, base, ['ports', 'Ports'])),
  wiFi6Support: bool(readProductValue(raw, base, ['wiFi6Support', 'WiFi6Support'])),
  bluetoothSupport: bool(readProductValue(raw, base, ['bluetoothSupport', 'BluetoothSupport'])),
  bluetoothVersion: text(readProductValue(raw, base, ['bluetoothVersion', 'BluetoothVersion'])),
  warrantyPeriod: text(readProductValue(raw, base, ['warrantyPeriod', 'WarrantyPeriod'], ['Warranty Period'])),
  targetAudience: text(readProductValue(raw, base, ['targetAudience', 'TargetAudience'])),

  accessoryType: text(read(raw, 'accessoryType', 'AccessoryType')),
  compatibility: text(read(raw, 'compatibility', 'Compatibility')),
  specificationDetails: text(read(raw, 'specifications_json', 'SpecificationsJson')),
  connectivity: text(read(raw, 'connectivity', 'Connectivity')),

  bundleType: text(read(raw, 'bundleType', 'BundleType')),
  discountPercentage: text(read(raw, 'discountPercentage', 'DiscountPercentage')),
  validFrom: text(read(raw, 'validFrom', 'ValidFrom')),
  validTo: text(read(raw, 'validTo', 'ValidTo')),
  bundleItems: (read<any[]>(raw, 'bundleItems', 'BundleItems') || []).map(item => ({
    productId: text(read(item, 'productId', 'ProductId')),
    quantity: text(read(item, 'quantity', 'Quantity')),
    discountPercentage: text(read(item, 'discountPercentage', 'DiscountPercentage'))
  })) || [],

  cpu: read(raw, 'cpuBrand', 'CpuBrand') && read(raw, 'cpuModel', 'CpuModel')
    ? `${read(raw, 'cpuBrand', 'CpuBrand')} ${read(raw, 'cpuModel', 'CpuModel')} ${read(raw, 'cpuGeneration', 'CpuGeneration') || ''}`.trim()
    : text(read(raw, 'cpuBrand', 'CpuBrand')),
  ram: read(raw, 'ramCapacityGB', 'RamCapacityGB') && read(raw, 'ramType', 'RamType')
    ? `${read(raw, 'ramCapacityGB', 'RamCapacityGB')}GB ${read(raw, 'ramType', 'RamType')} ${read(raw, 'ramSpeed', 'RamSpeed') ? `${read(raw, 'ramSpeed', 'RamSpeed')}MHz` : ''}`.trim()
    : read(raw, 'ramCapacityGB', 'RamCapacityGB') ? `${read(raw, 'ramCapacityGB', 'RamCapacityGB')}GB` : '',
  storage: read(raw, 'storageCapacityGB', 'StorageCapacityGB') && read(raw, 'storageType', 'StorageType')
    ? `${read(raw, 'storageCapacityGB', 'StorageCapacityGB')}GB ${read(raw, 'storageType', 'StorageType')} ${read(raw, 'storageInterface', 'StorageInterface') || ''}`.trim()
    : read(raw, 'storageCapacityGB', 'StorageCapacityGB') ? `${read(raw, 'storageCapacityGB', 'StorageCapacityGB')}GB SSD` : '',
  gpu: read(raw, 'gpuBrand', 'GpuBrand') && read(raw, 'gpuModel', 'GpuModel')
    ? `${read(raw, 'gpuBrand', 'GpuBrand')} ${read(raw, 'gpuModel', 'GpuModel')} ${read(raw, 'gpuVramGB', 'GpuVramGB') ? `${read(raw, 'gpuVramGB', 'GpuVramGB')}GB` : ''}`.trim()
    : text(read(raw, 'gpuBrand', 'GpuBrand')),
  display: read(raw, 'displaySizeInches', 'DisplaySizeInches') && read(raw, 'displayResolution', 'DisplayResolution')
    ? `${read(raw, 'displaySizeInches', 'DisplaySizeInches')}" ${read(raw, 'displayResolution', 'DisplayResolution')} ${read(raw, 'displayPanelType', 'DisplayPanelType') || ''} ${read(raw, 'displayRefreshRateHz', 'DisplayRefreshRateHz') ? `${read(raw, 'displayRefreshRateHz', 'DisplayRefreshRateHz')}Hz` : ''}`.trim()
    : read(raw, 'displaySizeInches', 'DisplaySizeInches') ? `${read(raw, 'displaySizeInches', 'DisplaySizeInches')}" Display` : '',
  battery: read(raw, 'batteryCapacityWh', 'BatteryCapacityWh')
    ? `${read(raw, 'batteryCapacityWh', 'BatteryCapacityWh')}Wh`
    : text(read(raw, 'battery', 'Battery')),
  weight_kg: text(read(raw, 'weightKg', 'WeightKg') ?? read(raw, 'weight', 'Weight'))
  };
};

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
