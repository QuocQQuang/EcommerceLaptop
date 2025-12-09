import type { ReviewSummary } from './review';

// User Role Enums
export const UserRole = {
    Customer: 'Customer',
    Admin: 'Admin',
    SuperAdmin: 'SuperAdmin',
    Staff: 'Staff'
} as const;

export type UserRoleType = typeof UserRole[keyof typeof UserRole];

// User Types
export interface User {
    id: string;
    email: string;
    firstName: string;
    lastName: string;
    phoneNumber?: string;
    profilePictureUrl?: string;
    roles: string[];
    role?: UserRoleType; // Primary role for easier access
    isActive: boolean;
    createdAt: string;
    verified?: boolean; // Account verification status
    lastLoginAt?: string; // Track last login for security
}

// Authentication Types
export interface LoginRequest {
    email: string;
    password: string;
}

export interface RegisterRequest {
    email: string;
    password: string;
    confirmPassword: string;
    firstName: string;
    lastName: string;
    phoneNumber?: string;
    acceptTerms: boolean;
}

export interface LoginResponse {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
    user: User;
}

export interface RefreshTokenRequest {
    refreshToken: string;
}

export interface TokenResponse {
    accessToken: string;
    refreshToken: string;
    expiresAt: string;
}

// Product Image Types
export interface ProductImage {
    id: number;
    imageUrl: string;
    altText: string;
    sortOrder: number;
    isPrimary: boolean;
}

// Product Types
export interface Product {
    id: number;
    name: string;
    description: string;
    shortDescription?: string;
    slug: string;
    sku: string;
    brand: string;
    model: string;
    type: 'Laptop' | 'Accessory' | 'Bundle';
    price: number;
    discountPrice?: number;
    isActive: boolean;
    isFeatured: boolean;
    stockQuantity: number;
    imageUrl: string;
    images: ProductImage[];
    specifications: ProductSpecification[];
    categories: Category[];
    createdAt: string;
    updatedAt: string;
    reviewSummary?: ReviewSummary;
    // Backend-specific fields for TPT inheritance
    productType?: string;
    brandId?: number;
    categoryId?: number;

    // Enhanced Laptop-specific fields
    series?: string; //  FIXED: Add series field from backend
    cpuBrand?: string;
    cpuModel?: string;
    cpuGeneration?: string;
    cpuCores?: number;
    cpuBaseClockGHz?: number;
    cpuBoostClockGHz?: number;
    cpuCache?: string;

    // RAM specifications
    ramType?: string;
    ramCapacityGB?: number;
    ramSlots?: number;
    ramSpeed?: number;
    ramUpgradeable?: boolean;

    // Storage specifications
    storageType?: string;
    storageCapacityGB?: number;
    storageInterface?: string;
    nvMeSupport?: boolean;

    // GPU specifications
    gpuType?: string;
    gpuBrand?: string;
    gpuModel?: string;
    gpuVramGB?: number;

    // Display specifications
    displaySizeInches?: number;
    displayResolution?: string;
    displayPanelType?: string;
    displayRefreshRateHz?: number;
    displayTouchscreen?: boolean;

    // Battery & Physical
    batteryCapacityWh?: number;
    battery?: number | string;
    weightKg?: number;
    weight?: number | string;
    dimensions?: string;
    color?: string;

    // Connectivity
    ports?: string;
    wiFi6Support?: boolean;
    bluetoothSupport?: boolean;
    bluetoothVersion?: string;

    // Business info
    warrantyPeriod?: string;
    warranty?: string;
    targetAudience?: string;

    // Accessory-specific
    accessoryType?: string;
    compatibility?: string;
    specifications_json?: string;
    connectivity?: string;
    material?: string;

    // Bundle-specific
    bundleItems?: BundleItem[];
    bundleType?: string;
    discountPercentage?: number;
    validFrom?: string;
    validTo?: string;

    // Inventory information
    inventory?: ProductInventory;

    // Variant support
    parentProductId?: number;
    variantName?: string;
    variantSku?: string;
    isVariant: boolean;
    isBaseProduct: boolean;
    variants: Product[];
}

export interface BundleItem {
    id: number;
    productId: number;
    productName: string;
    productSku?: string;
    productImageUrl?: string;
    brand?: string;
    originalPrice: number;
    quantity: number;
    discountPercentage: number;
    discountedPrice: number;
    totalPrice: number;
    isAvailable?: boolean;
    stockQuantity?: number;
}

export interface ProductSpecification {
    id: number;
    name: string;
    value: string;
    category: string;
    displayOrder: number;
}

export interface Category {
    id: number;
    name: string;
    slug: string;
    description?: string;
    parentId?: number;
    isActive: boolean;
}

export interface ProductInventory {
    id: number;
    productId: number;
    quantityInStock: number;
    reservedQuantity: number;
    reorderLevel: number;
    maxStockLevel: number;
    warehouseLocation: string;
    lastStockUpdate: string;
    availableQuantity: number;
    transactions: any[];
}

export interface ProductSearchParams {
    page?: number;
    pageSize?: number;
    search?: string;
    type?: string;
    brand?: string;
    minPrice?: number;
    maxPrice?: number;
    category?: string;
    sortBy?: 'newest' | 'price_asc' | 'price_desc' | 'name' | 'rating' | 'popularity';
}

// Variant Types
export interface CreateVariantDto {
    variantName: string;
    variantSku: string;
    price: number;
    description?: string;
    stockQuantity: number;
    isActive: boolean;

    // Laptop-specific variant properties
    ramCapacityGB?: number;
    storageCapacityGB?: number;
    color?: string;
    gpuModel?: string;
}

export interface UpdateVariantDto {
    variantName?: string;
    variantSku?: string;
    price?: number;
    description?: string;
    stockQuantity?: number;
    isActive?: boolean;

    // Laptop-specific variant properties
    ramCapacityGB?: number;
    storageCapacityGB?: number;
    color?: string;
    gpuModel?: string;
}

export interface VariantSummary {
    id: number;
    variantName: string;
    variantSku: string;
    price: number;
    stockQuantity: number;
    isActive: boolean;
    imageUrl?: string;

    // Key differentiators
    ramCapacityGB?: number;
    storageCapacityGB?: number;
    color?: string;
    gpuModel?: string;
}

export interface PaginatedResponse<T> {
    items: T[];
    totalCount: number;
    page: number;
    pageSize: number;
    totalPages: number;
    hasNextPage?: boolean;
    hasPreviousPage?: boolean;
}

// Cart Types
export interface CartItem {
    id: number;
    productId: number;
    product: Product;
    quantity: number;
    unitPrice: number;
    totalPrice: number;
    itemDiscount?: number;
    finalPrice?: number;
    isBundle?: boolean;
    bundleItems?: CartItem[];
    addedAt?: string;
    isAvailable?: boolean;
    stockQuantity?: number;
    unavailabilityReason?: string;

    // Variant support
    variantId?: number;
    variantName?: string;
    variantSku?: string;
    isVariant?: boolean;
}

export interface Cart {
    id: number;
    userId?: string;
    sessionId?: string;
    items: CartItem[];
    totalAmount: number;
    discountAmount: number;
    finalAmount: number;
    createdAt: string;
    updatedAt: string;
}

export interface AddToCartRequest {
    productId: number;
    quantity: number;
    sessionId?: string;
    // For bundle products, frontend can send the bundle items selected/packed
    bundleItems?: BundleItem[];
    // For variant products
    variantId?: number;
}

export interface UpdateCartItemRequest {
    itemId: number;
    quantity: number;
    sessionId?: string;
}

// Order Types
export interface Order {
    id: number;
    orderNumber: string;
    userId: string;
    status: OrderStatus;
    totalAmount: number;
    shippingAddress: Address;
    billingAddress: Address;
    paymentMethod: string;
    paymentStatus: PaymentStatus;
    items: OrderItem[];
    createdAt: string;
    updatedAt: string;
}

export interface OrderItem {
    id: number;
    productId: number;
    product: Product;
    quantity: number;
    unitPrice: number;
    totalPrice: number;
}

export interface Address {
    id?: number;
    fullName: string;
    phoneNumber: string;
    addressLine1: string;
    addressLine2?: string;
    city: string;
    state: string;
    postalCode: string;
    country: string;
    isDefault?: boolean;
}

export interface CreateOrderRequest {
    shippingAddressId?: number;
    billingAddressId?: number;
    shippingAddress?: Address;
    billingAddress?: Address;
    paymentMethod: string;
    notes?: string;
}

export enum OrderStatus {
    Pending = 'Pending',
    Confirmed = 'Confirmed',
    Processing = 'Processing',
    Shipped = 'Shipped',
    Delivered = 'Delivered',
    Cancelled = 'Cancelled',
    Returned = 'Returned'
}

export enum PaymentStatus {
    Pending = 'Pending',
    Paid = 'Paid',
    Failed = 'Failed',
    Refunded = 'Refunded'
}

export enum PaymentGateway {
    VnPay = 1,
    MoMo = 2,
    PayPal = 3,
    ZaloPay = 4,
    SePay = 5,
    Stripe = 6
}

export enum PaymentMethod {
    CreditCard = 1,
    DebitCard = 2,
    EWallet = 3,
    BankTransfer = 4,
    QRCode = 5,
    Installment = 6,
    CashOnDelivery = 7
}

export interface PaymentInitializationResult {
    isSuccess: boolean;
    transactionId?: string;
    paymentUrl?: string;
    qrCodeData?: string;
    expiresAt?: string;
    errorMessage?: string;
}

export interface AtomicCheckoutResult {
    isSuccess: boolean;
    order?: Order;
    payment?: PaymentInitializationResult;
    errorMessage?: string;
}

// API Response Types
export interface ApiResponse<T = unknown> {
    success: boolean;
    message?: string;
    data?: T;
    errors?: string[];
    timestamp?: string;
    requestId?: string;
}

export interface ApiError {
    message: string;
    status: number;
    code?: string;
    errors?: string[];
    timestamp?: string;
    requestId?: string;
}

// Generic Result Type for better error handling
export type ApiResult<T> =
    | { success: true; data: T }
    | { success: false; error: ApiError };

// Session and Authentication Error Types
export interface AuthError extends ApiError {
    type: 'INVALID_CREDENTIALS' | 'SESSION_EXPIRED' | 'UNAUTHORIZED' | 'FORBIDDEN' | 'TOKEN_REFRESH_FAILED';
}

export interface SessionInfo {
    isValid: boolean;
    expiresAt: string;
    role?: UserRoleType;
    permissions?: string[];
}

// Search Types
export interface SearchSuggestion {
    id: number;
    name: string;
    type: 'product' | 'brand' | 'category';
    imageUrl?: string;
    price?: number;
    url: string;
}

// Re-export Review types for convenience
export type {
    CreateReviewRequest, Review, ReviewFilterParams, ReviewsPagedResponse, ReviewSummary, UpdateReviewRequest
} from './review';

// === BLOG MANAGEMENT TYPES ===

// Blog Status (match backend DB)
export type BlogStatusType = 'published' | 'draft';

export enum BlogStatus {
    Published = 'published',
    Draft = 'draft'
}

// Blog Category (match backend)
export interface BlogCategory {
    id: number;
    name: string;
    slug: string;
    description?: string;
    postCount?: number;
    metaTitle?: string;
    metaDescription?: string;
    isActive: boolean;
    sortOrder: number;
    parentId?: number;
    createdAt: string;
    updatedAt: string;
}

// Blog Tag (match backend)
export interface BlogTag {
    id: number;
    name: string;
    slug: string;
    description?: string;
    color: string;
    isActive: boolean;
    createdAt: string;
    updatedAt: string;
    postCount?: number;
}

// Blog Author (simple, backend uses User)
export interface BlogAuthor {
    id: string | number;
    name?: string;
    email: string;
    firstName?: string;
    lastName?: string;
    displayName?: string;
    avatarUrl?: string;
    bio?: string;
    profilePictureUrl?: string;
    socialLinks?: {
        twitter?: string;
        facebook?: string;
        linkedin?: string;
        github?: string;
        website?: string;
    };
    isActive?: boolean;
    role?: string;
    postCount?: number;
    totalViews?: number;
    totalComments?: number;
    followerCount?: number;
    averageRating?: number;
    createdAt?: string;
    updatedAt?: string;
    lastActiveAt?: string;
}

// Blog Comment (match backend)
export interface BlogComment {
    id: string;
    blogPostId: string;
    blogId?: string;
    content: string;
    authorName: string;
    authorEmail: string;
    authorWebsite?: string;
    status: CommentStatus;
    isApproved?: boolean;
    isRejected?: boolean;
    isSpam?: boolean;
    createdAt: string;
    updatedAt: string;
    blog?: BlogSummary;
    replies?: BlogComment[];
    parentId?: string;
}

export enum CommentStatus {
    Pending = 'pending',
    Approved = 'approved',
    Rejected = 'rejected',
    Spam = 'spam'
}

export interface BlogSummary {
    id: number | string;
    title: string;
    slug: string;
}

export interface UpdateCommentStatusRequest {
    status: CommentStatus;
    reason?: string;
}

export interface BlogSeo {
    title?: string;
    description?: string;
    keywords?: string[];
}

export interface BlogMonthlyStat {
    month: string;
    views: number;
    posts: number;
    comments: number;
}

export interface BlogTopPost {
    id: number | string;
    title: string;
    slug?: string;
    viewCount: number;
    publishedAt: string;
    status?: BlogStatusType;
    commentCount?: number;
    likeCount?: number;
}

export interface BlogRecentActivity {
    type: 'blog_created' | 'blog_published' | 'comment_added' | string;
    title: string;
    timestamp: string;
    author?: string;
    blogId?: number | string;
    commentId?: number | string;
    metadata?: Record<string, unknown>;
}

export interface BlogMedia {
    id: string;
    filename: string;
    url: string;
    mimeType: string;
    fileSize: number;
    altText?: string;
    width?: number;
    height?: number;
    tags?: string[];
    checksum?: string;
    storageProvider?: string;
    createdAt: string;
    updatedAt?: string;
    metadata?: Record<string, unknown>;
}

export interface MediaUploadRequest {
    file: File;
    alt?: string;
    description?: string;
    folder?: string;
    tags?: string[];
    metadata?: Record<string, unknown>;
}

// Main Blog Entity (match backend BlogPost - DB fields only)
export interface Blog {
    id: number;
    title: string;
    slug: string;
    excerpt?: string;
    content: string;
    featuredImageUrl?: string;
    metaTitle?: string;
    metaDescription?: string;
    status: BlogStatusType;
    publishedAt?: string;
    isFeatured: boolean;
    categoryId?: number;
    authorId: number;
    author?: BlogAuthor;
    category?: BlogCategory;
    tags?: BlogTag[];
    tagIds?: number[]; // Now supported
    viewCount: number;
    commentCount?: number; // Computed
    likeCount?: number;
    createdAt: string;
    updatedAt: string;
}

// Blog Request/Response Types (match backend - DB fields only)
export interface CreateBlogRequest {
    title: string;
    excerpt?: string;
    content: string;
    featuredImageUrl?: string; // Maps to FeaturedImageUrl
    metaTitle?: string;
    metaDescription?: string;
    isPublished?: boolean;
    isFeatured?: boolean;
    categoryId: number;
    tagIds?: number[]; // Now supported as int[]
}

export interface UpdateBlogRequest extends Partial<CreateBlogRequest> {
    status?: BlogStatus;
    publishedAt?: string;
    slug?: string;
}

export interface BlogSearchParams {
    page?: number;
    pageNumber?: number;
    pageSize?: number;
    categoryId?: number;
    search?: string;
    searchTerm?: string;
    status?: BlogStatusType;
    isPublished?: boolean;
    isFeatured?: boolean;
    authorId?: string | number;
    sortBy?: string;
    sortOrder?: 'asc' | 'desc';
}

// Category Request/Response Types (match backend)
export interface CreateCategoryRequest {
    name: string;
    slug?: string;
    description?: string;
    metaTitle?: string;
    metaDescription?: string;
    isActive?: boolean;
    sortOrder?: number;
}

export interface UpdateCategoryRequest extends Partial<CreateCategoryRequest> {
    id: number;
}

export interface CategorySearchParams {
    activeOnly?: boolean;
}

// Tag Request/Response Types (match backend)
export interface BlogTagMutation {
    name: string;
    slug?: string;
    description?: string;
    color?: string;
    isActive?: boolean;
}

export type CreateTagRequest = BlogTagMutation;
export type CreateBlogTagRequest = BlogTagMutation;

export interface UpdateTagRequest extends Partial<BlogTagMutation> {
    id: number;
}

export type UpdateBlogTagRequest = UpdateTagRequest;

export interface TagSearchParams {
    activeOnly?: boolean;
}

// Comment Request/Response Types (match backend)
export interface CreateCommentRequest {
    blogId: number | string;
    content: string;
    authorName: string;
    authorEmail: string;
    authorWebsite?: string;
    status?: CommentStatus;
    parentId?: string;
}

export interface UpdateCommentRequest {
    id: number;
    content?: string;
    status?: CommentStatus;
}

export interface CommentSearchParams {
    pageNumber?: number;
    pageSize?: number;
    status?: CommentStatus | 'all';
    blogId?: number | string;
    search?: string;
}
// Blog Analytics & Stats (match backend BlogStatistics)
export interface BlogAnalytics {
    totalPosts: number;
    totalBlogs?: number;
    publishedPosts: number;
    publishedBlogs?: number;
    draftPosts: number;
    draftBlogs?: number;
    scheduledPosts?: number;
    scheduledBlogs?: number;
    archivedPosts?: number;
    archivedBlogs?: number;
    totalCategories: number;
    totalComments: number;
    pendingComments: number;
    totalViews: number;
    totalLikes: number;
    averageViews?: number;
    averageComments?: number;
    engagementRate?: number;
    monthlyStats?: BlogMonthlyStat[];
    topBlogs?: BlogTopPost[];
    recentActivity?: BlogRecentActivity[];
    trendingTags?: string[];
}

