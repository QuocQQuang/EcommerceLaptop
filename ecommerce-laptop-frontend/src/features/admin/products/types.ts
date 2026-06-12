export interface AdminProductsParams {
  page?: number;
  limit?: number;
  search?: string;
  categoryId?: number;
  brandId?: number;
  isActive?: boolean;
  sortBy?: string;
  sortOrder?: string;
  productType?: 'all' | 'base' | 'variant';
}

export interface AdminProductsResponse {
  products: import('@/types/api').Product[];
  totalCount: number;
  currentPage: number;
  totalPages: number;
  pageSize: number;
}

export interface ProductUpdateDto {
  Name: string;
  SKU: string;
  Description: string;
  Model: string;
  BrandId: number;
  CategoryId?: number;
  Price: number;
  DiscountPrice?: number;
  IsActive: boolean;
  IsFeatured?: boolean;
  StockQuantity: number;
  ProductType: string;
  Series?: string;
  CPU?: string;
  RAM?: string;
  Storage?: string;
  GPU?: string;
  Display?: string;
  Battery?: string;
  Weight_Kg?: string;
  OperatingSystem?: string;
  Ports?: string;
  Type?: string;
  AccessoryType?: string;
  Compatibility?: string;
  Color?: string;
  Material?: string;
  Warranty?: string;
}

export interface ProductFormData {
  name: string;
  sku: string;
  description: string;
  shortDescription?: string;
  categoryId: string;
  brandId: string;
  price: string;
  comparePrice?: string;
  costPrice?: string;
  stock: string;
  weight: string;
  dimensions: string;
  status: 'active' | 'inactive';
  isFeatured?: boolean;
  images: string[];
  productType: 'Laptop' | 'Accessory' | 'Bundle';
  tags?: string[];
  specifications?: Record<string, string>;

  inventory: {
    quantityInStock: string;
    reservedQuantity: string;
    reorderLevel: string;
    maxStockLevel: string;
    warehouseLocation: string;
  };

  series?: string;
  model?: string;
  cpuBrand?: string;
  cpuModel?: string;
  cpuGeneration?: string;
  cpuCores?: string;
  cpuBaseClockGHz?: string;
  cpuBoostClockGHz?: string;
  cpuCache?: string;
  ramType?: string;
  ramCapacityGB?: string;
  ramSlots?: string;
  ramSpeed?: string;
  ramUpgradeable?: boolean;
  storageType?: string;
  storageCapacityGB?: string;
  storageInterface?: string;
  nvMeSupport?: boolean;
  gpuType?: string;
  gpuBrand?: string;
  gpuModel?: string;
  gpuVramGB?: string;
  displaySizeInches?: string;
  displayResolution?: string;
  displayPanelType?: string;
  displayRefreshRateHz?: string;
  displayTouchscreen?: boolean;
  batteryCapacityWh?: string;
  weightKg?: string;
  color?: string;
  ports?: string;
  wiFi6Support?: boolean;
  bluetoothSupport?: boolean;
  bluetoothVersion?: string;
  warrantyPeriod?: string;
  targetAudience?: string;

  accessoryType?: string;
  compatibility?: string;
  specificationDetails?: string;
  connectivity?: string;
  material?: string;

  bundleType?: string;
  discountPercentage?: string;
  validFrom?: string;
  validTo?: string;
  bundleItems?: Array<{
    productId: string;
    quantity: string;
    discountPercentage: string;
  }>;

  cpu?: string;
  ram?: string;
  storage?: string;
  gpu?: string;
  display?: string;
  battery?: string;
  weight_kg?: string;
  operatingSystem?: string;
  warranty?: string;
}

export interface ImageUploadResult {
  productId: number;
  imageUrl: string;
  imageId: string;
  displayOrder: number;
  message: string;
}
