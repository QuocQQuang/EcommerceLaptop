// Customer Management TypeScript Types
// Complete type definitions for customer management system with security and privacy controls

export interface CustomerManagementDto {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  maskedEmail: string;
  phoneNumber?: string;
  maskedPhoneNumber?: string;
  isActive: boolean;
  emailConfirmed: boolean;
  createdAt: Date;
  lastLoginAt?: Date;
  totalOrders: number;
  totalSpent: number;
  lastOrderDate?: Date;
  vipTierId?: number;
  vipTierName?: string;
  failedLoginAttempts: number;
  lockedUntil?: Date;

  // Permission flags (set by backend based on requester's permissions)
  canViewDetails: boolean;
  canEdit: boolean;
  canDelete: boolean;
  canViewPersonalData: boolean;
}

export interface CustomerDetailDto {
  id: number;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  dateOfBirth?: Date;
  gender?: string;
  profilePictureUrl?: string;
  isActive: boolean;
  emailConfirmed: boolean;
  createdAt: Date;
  updatedAt: Date;
  lastLoginAt?: Date;
  lastLoginIP?: string;
  failedLoginAttempts: number;
  lockedUntil?: Date;
  lastPasswordChangeDate?: Date;
  vipTierId?: number;
  vipTierName?: string;
  totalSpent: number;
  vipTierUpdatedAt?: Date;
  totalOrders: number;
  completedOrders: number;
  cancelledOrders: number;
  firstOrderDate?: Date;
  lastOrderDate?: Date;
  averageOrderValue: number;
  notes?: string;
  notesUpdatedAt?: Date;

  // Addresses with privacy controls
  addresses: CustomerAddressDto[];

  // Recent activities (audit trail)
  recentActivities: CustomerRecentActivityDto[];

  // Permission flags (set by service based on requester's permissions)
  canEdit: boolean;
  canDelete: boolean;
  canViewOrderHistory: boolean;
  canViewPersonalData: boolean;
  canViewSecurityInfo: boolean;
}

export interface CustomerAddressDto {
  id: number;
  street?: string;
  maskedStreet?: string;
  city?: string;
  district?: string;
  ward?: string;
  zipCode?: string;
  isDefault: boolean;
  createdAt: Date;
  isFullDataVisible: boolean;
}

export interface CustomerRecentActivityDto {
  activityDate: Date;
  activityType: string;
  description: string;
  ipAddress?: string;
  userAgent?: string;
}

export interface CustomerOrderHistoryDto {
  customerId: number;
  totalOrders: number;
  totalSpent: number;
  averageOrderValue: number;
  firstOrderDate?: Date;
  lastOrderDate?: Date;
  pendingOrders: number;
  completedOrders: number;
  cancelledOrders: number;
  refundedOrders: number;
  recentOrders: CustomerOrderSummaryDto[];
  monthlySpending: MonthlySpendingDto[];
}

export interface CustomerOrderSummaryDto {
  orderId: number;
  orderNumber: string;
  orderDate: Date;
  total: number;
  status: string;
  itemCount: number;
}

export interface MonthlySpendingDto {
  year: number;
  month: number;
  amount: number;
  orderCount: number;
}

export interface CustomerStatisticsDto {
  totalCustomers: number;
  activeCustomers: number;
  newCustomersThisMonth: number;
  newCustomersToday: number;
  emailVerifiedCustomers: number;
  unverifiedCustomers: number;
  customersLoggedInToday: number;
  customersLoggedInThisWeek: number;
  inactiveCustomers30Days: number;
  averageCustomerValue: number;
  vipTierStats: VipTierStatDto[];
  registrationTrends: CustomerRegistrationTrendDto[];
}

export interface VipTierStatDto {
  tierId: number;
  tierName: string;
  customerCount: number;
  totalSpent: number;
  averageSpent: number;
}

export interface CustomerRegistrationTrendDto {
  date: Date;
  newRegistrations: number;
  emailVerifications: number;
}

export interface UpdateCustomerRequest {
  firstName?: string;
  lastName?: string;
  phoneNumber?: string;
  dateOfBirth?: Date;
  gender?: string;
  isActive?: boolean;
  notes?: string;

  // Admin-only fields
  emailConfirmed?: boolean;
  lockedUntil?: Date; // For account locking/unlocking
  vipTierId?: number;
}

export interface CustomerNotificationRequest {
  subject: string;
  message: string;
  notificationType?: string; // general, promotion, security, system
  sendEmail?: boolean;
  sendInApp?: boolean;
  scheduledAt?: Date; // For scheduled notifications
}

export interface CustomerActivityLogDto {
  id: number;
  customerId: number;
  action: string;
  description: string;
  createdAt: Date;
  ipAddress?: string;
  userAgent?: string;
  entityType?: string;
  entityId?: string;
  metadata?: string; // JSON metadata for additional context
}

export interface CustomerSearchParameters {
  searchTerm?: string;
  email?: string;
  isActive?: boolean;
  emailVerified?: boolean;
  registeredFrom?: Date;
  registeredTo?: Date;
  minTotalSpent?: number;
  maxTotalSpent?: number;
  vipTierId?: number;
  page?: number;
  pageSize?: number;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface ActivityLogParameters {
  page?: number;
  pageSize?: number;
  fromDate?: Date;
  toDate?: Date;
  activityType?: string;
  sortOrder?: 'asc' | 'desc';
}

export interface CustomerExportParameters {
  includePersonalData?: boolean;
  includeSecurityInfo?: boolean;
  includeOrderHistory?: boolean;
  includeAddresses?: boolean;
  dateRange?: {
    from: Date;
    to: Date;
  };
  filters?: CustomerSearchParameters;
}

export enum ExportFormat {
  CSV = 'CSV',
  Excel = 'Excel',
  PDF = 'PDF'
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages?: number;
  hasNextPage?: boolean;
  hasPreviousPage?: boolean;
}

export interface ExportResult {
  data: Uint8Array;
  fileName: string;
  contentType: string;
}

// Component Props Types
export interface CustomerListProps {
  searchParameters?: CustomerSearchParameters;
  onCustomerSelect?: (customer: CustomerManagementDto) => void;
  onCustomerUpdate?: (customerId: number) => void;
  allowSelection?: boolean;
  compact?: boolean;
}

export interface CustomerDetailProps {
  customerId: number;
  onUpdate?: (customer: CustomerDetailDto) => void;
  onClose?: () => void;
  readOnly?: boolean;
}

export interface CustomerFormProps {
  customer?: Partial<CustomerDetailDto>;
  onSubmit: (data: UpdateCustomerRequest) => Promise<void>;
  onCancel?: () => void;
  isLoading?: boolean;
  permissions?: CustomerPermissions;
}

export interface CustomerPermissions {
  canRead: boolean;
  canWrite: boolean;
  canDelete: boolean;
  canManage: boolean;
  canViewPersonalData: boolean;
  canViewSecurityInfo: boolean;
  canExport: boolean;
}

// Filter and Search Types
export interface CustomerFilters {
  status: 'all' | 'active' | 'inactive';
  emailStatus: 'all' | 'verified' | 'unverified';
  vipTier: 'all' | number;
  registrationPeriod: 'all' | 'today' | 'week' | 'month' | 'custom';
  spendingRange: {
    min?: number;
    max?: number;
  };
  customDateRange?: {
    from: Date;
    to: Date;
  };
}

// Dashboard Types
export interface CustomerDashboardData {
  statistics: CustomerStatisticsDto;
  recentCustomers: CustomerManagementDto[];
  topSpenders: CustomerManagementDto[];
  newRegistrations: CustomerRegistrationTrendDto[];
  vipTierDistribution: VipTierStatDto[];
}

// Bulk Operations
export interface BulkCustomerOperation {
  type: 'activate' | 'deactivate' | 'update' | 'export' | 'notify';
  customerIds: number[];
  data?: any;
  reason?: string;
}

export interface BulkOperationResult {
  success: boolean;
  processed: number;
  failed: number;
  errors: string[];
  results: Array<{
    customerId: number;
    success: boolean;
    message: string;
  }>;
}

// Security and Audit Types
export interface SecurityEvent {
  customerId: number;
  eventType: 'login_failed' | 'account_locked' | 'password_changed' | 'email_changed' | 'profile_updated';
  severity: 'low' | 'medium' | 'high' | 'critical';
  ipAddress?: string;
  userAgent?: string;
  timestamp: Date;
  details?: string;
}

export interface AuditLogEntry {
  id: number;
  customerId: number;
  adminUserId: number;
  action: string;
  entityType: string;
  entityId: string;
  oldValues?: any;
  newValues?: any;
  timestamp: Date;
  ipAddress?: string;
  userAgent?: string;
}

// API Response Types
export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
  statusCode: number;
}

export interface CustomerManagementApiError extends Error {
  statusCode: number;
  details?: string[];
  timestamp: Date;
}

// Form Validation Types
export interface CustomerFormValidation {
  firstName: {
    required: boolean;
    minLength: number;
    maxLength: number;
  };
  lastName: {
    required: boolean;
    minLength: number;
    maxLength: number;
  };
  email: {
    required: boolean;
    pattern: RegExp;
  };
  phoneNumber: {
    required: boolean;
    pattern: RegExp;
  };
  dateOfBirth: {
    required: boolean;
    minAge: number;
    maxAge: number;
  };
}

// Table Configuration Types
export interface CustomerTableColumn {
  key: string;
  label: string;
  sortable: boolean;
  filterable: boolean;
  width?: string;
  visible: boolean;
  permission?: string;
  render?: (value: any, customer: CustomerManagementDto) => React.ReactNode;
}

export interface CustomerTableConfig {
  columns: CustomerTableColumn[];
  defaultSort: {
    field: string;
    direction: 'asc' | 'desc';
  };
  pageSize: number;
  enableSelection: boolean;
  enableExport: boolean;
}

// All types are already exported above with their interface declarations