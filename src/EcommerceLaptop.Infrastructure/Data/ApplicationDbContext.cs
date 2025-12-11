using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Infrastructure.Services.Security;

namespace EcommerceLaptop.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    // User Management
    public DbSet<User> Users { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Address> Addresses { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<OAuth2Account> OAuth2Accounts { get; set; }
    public DbSet<PasswordReset> PasswordResets { get; set; }
    public DbSet<EmailConfirmationToken> EmailConfirmationTokens { get; set; }

    // Admin Management - UNIFIED SYSTEM (using Users table)
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }

    // Legacy Admin Management REMOVED - Using unified Users system
    // public DbSet<AdminUser> AdminUsers { get; set; }
    // public DbSet<AdminRefreshToken> AdminRefreshTokens { get; set; }
    // public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }
    // public DbSet<AdminUserMigrationMapping> AdminUserMigrationMappings { get; set; }

    // Product Management
    public DbSet<Product> Products { get; set; }
    public DbSet<Laptop> Laptops { get; set; }
    public DbSet<Accessory> Accessories { get; set; }
    public DbSet<Bundle> Bundles { get; set; }
    public DbSet<BundleItem> BundleItems { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<Review> Reviews { get; set; }

    // Order Management
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<PaymentAudit> PaymentAudits { get; set; }
    public DbSet<PaymentMethodConfiguration> PaymentMethodConfigurations { get; set; }
    public DbSet<OrderAudit> OrderAudits { get; set; }

    // Inventory Management
    public DbSet<Inventory> Inventories { get; set; }
    public DbSet<InventoryTransaction> InventoryTransactions { get; set; }
    public DbSet<SerialNumber> SerialNumbers { get; set; } // New

    // Marketing
    public DbSet<Coupon> Coupons { get; set; }
    public DbSet<Campaign> Campaigns { get; set; }
    public DbSet<CampaignProduct> CampaignProducts { get; set; }

    // Shopping Cart Management (T007)
    public DbSet<ShoppingCart> ShoppingCarts { get; set; }
    public DbSet<CartItem> CartItems { get; set; }
    public DbSet<CartSession> CartSessions { get; set; }

    // Blog Management
    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<BlogCategory> BlogCategories { get; set; }
    public DbSet<BlogComment> BlogComments { get; set; }
    public DbSet<BlogTag> BlogTags { get; set; }
    public DbSet<BlogPostTag> BlogPostTags { get; set; }

    // Wishlist Management
    public DbSet<WishlistItem> WishlistItems { get; set; }

    // New Admin Features - Product Categories & Brands
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<ProductBrand> ProductBrands { get; set; }

    // System Settings
    public DbSet<SystemSetting> SystemSettings { get; set; }

    // User Management Extensions
    public DbSet<UserActivityLog> UserActivityLogs { get; set; }
    public DbSet<UserVipTier> UserVipTiers { get; set; }

    // Order Extensions
    public DbSet<OrderEvent> OrderEvents { get; set; }
    public DbSet<Refund> Refunds { get; set; }
    public DbSet<ShippingRate> ShippingRates { get; set; }

    // Security System
    public DbSet<IPBlockRule> IPBlockRules { get; set; }
    public DbSet<RateLimitRule> RateLimitRules { get; set; }
    public DbSet<RateLimitViolation> RateLimitViolations { get; set; }
    public DbSet<SecurityEvent> SecurityEvents { get; set; }
    public DbSet<SystemAuditLog> SystemAuditLogs { get; set; }
    public DbSet<LoginAttempt> LoginAttempts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Initialize field encryption secret from environment
        FieldEncryption.Initialize(Environment.GetEnvironmentVariable("FIELD_ENCRYPTION_KEY"));

        // User configurations
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.LastName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.PhoneNumber).HasMaxLength(20);
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(500); // Image host URL (e.g. ImgBB)

            // Encrypt IP and notes fields
            entity.Property(e => e.LastLoginIP)
                .HasConversion(v => FieldEncryption.Encrypt(v), v => FieldEncryption.Decrypt(v));
            entity.Property(e => e.Notes)
                .HasConversion(v => FieldEncryption.Encrypt(v), v => FieldEncryption.Decrypt(v));
        });

        modelBuilder.Entity<Address>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PhoneNumber).IsRequired().HasMaxLength(15);
            entity.Property(e => e.Street).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Ward).IsRequired().HasMaxLength(100);
            entity.Property(e => e.District).IsRequired().HasMaxLength(100);
            entity.Property(e => e.City).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Country).HasMaxLength(100).HasDefaultValue("Vit Nam");

            entity.HasOne(e => e.User)
                .WithMany(e => e.Addresses)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.UserId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.IsAdminRole).HasDefaultValue(false); // Support unified admin system
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.RoleId });
            entity.HasOne(e => e.User)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.UserId);
            entity.HasOne(e => e.Role)
                .WithMany(e => e.UserRoles)
                .HasForeignKey(e => e.RoleId);
        });

        // Authentication configurations
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ReplacedByToken).HasMaxLength(500);
            entity.Property(e => e.RevokedReason).HasMaxLength(1000);
            entity.Property(e => e.CreatedByIp).HasMaxLength(45);
            entity.Property(e => e.RevokedByIp).HasMaxLength(45);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.Token).IsUnique();
        });

        modelBuilder.Entity<OAuth2Account>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).HasMaxLength(50).IsRequired();
            entity.Property(e => e.ProviderUserId).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.ProfilePictureUrl).HasMaxLength(255);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
        });

        // Password Reset configurations
        modelBuilder.Entity<PasswordReset>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ResetToken).HasMaxLength(128).IsRequired();
            entity.Property(e => e.RequestIpAddress).HasMaxLength(45); // Supports IPv6
            entity.Property(e => e.RequestUserAgent).HasMaxLength(1000);
            entity.Property(e => e.UsedIpAddress).HasMaxLength(45);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for performance on token lookups
            entity.HasIndex(e => e.ResetToken);
            // Index for cleanup queries (finding expired tokens)
            entity.HasIndex(e => e.ExpiresAt);
            // Composite index for user + active tokens
            entity.HasIndex(e => new { e.UserId, e.IsUsed, e.ExpiresAt });
        });

        // Email Confirmation Token configurations
        modelBuilder.Entity<EmailConfirmationToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).HasMaxLength(512).IsRequired();
            entity.Property(e => e.Email).HasMaxLength(255).IsRequired();
            entity.Property(e => e.GeneratedFromIp).HasMaxLength(45);
            entity.Property(e => e.GeneratedFromUserAgent).HasMaxLength(500);
            entity.Property(e => e.UsedFromIp).HasMaxLength(45);
            entity.Property(e => e.UsedFromUserAgent).HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Security indexes for performance and security
            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => new { e.UserId, e.IsUsed, e.ExpiresAt });
            entity.HasIndex(e => new { e.Email, e.IsUsed });
        });

        // Admin Management configurations - USING UNIFIED USERS SYSTEM
        // Legacy AdminUser configuration removed - now using Users table with IsAdminRole

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Module).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();

            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => new { e.Module, e.Action });
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(e => new { e.RoleId, e.PermissionId });

            entity.HasOne(e => e.Role)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Permission)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Legacy AdminRefreshToken configuration removed - using unified RefreshToken system

        // Legacy AdminAuditLog configuration removed - using unified audit system

        // Legacy Migration Support Configurations removed - migration completed

        // Product configurations with TPT (Table Per Type)
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Brand).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Model).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SKU).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.SKU).IsUnique();
            entity.Property(e => e.Price).HasPrecision(18, 2);

            // Variant configuration
            entity.Property(e => e.VariantName).HasMaxLength(255);
            entity.Property(e => e.VariantSku).HasMaxLength(50);
            entity.HasIndex(e => e.VariantSku).IsUnique().HasFilter("[VariantSku] IS NOT NULL");
            entity.HasIndex(e => e.ParentProductId);

            // Self-referencing relationship for variants
            entity.HasOne(e => e.ParentProduct)
                .WithMany(e => e.Variants)
                .HasForeignKey(e => e.ParentProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Use TPT strategy instead of TPH discriminator
            entity.UseTptMappingStrategy();
        });

        modelBuilder.Entity<Laptop>(entity =>
        {
            entity.ToTable("Laptops"); // Explicit table for TPT
            entity.Property(e => e.CpuBaseClockGHz).HasPrecision(4, 2);
            entity.Property(e => e.CpuBoostClockGHz).HasPrecision(4, 2);
            entity.Property(e => e.DisplaySizeInches).HasPrecision(4, 2);
            entity.Property(e => e.WeightKg).HasPrecision(4, 2);
        });

        modelBuilder.Entity<Accessory>(entity =>
        {
            entity.ToTable("Accessories"); // Explicit table for TPT
        });

        modelBuilder.Entity<Bundle>(entity =>
        {
            entity.ToTable("Bundles"); // Explicit table for TPT
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
        });

        modelBuilder.Entity<BundleItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);

            entity.HasOne(e => e.Bundle)
                .WithMany(e => e.BundleItems)
                .HasForeignKey(e => e.BundleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Order configurations
        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.OrderNumber).IsUnique();
            entity.Property(e => e.SubTotal).HasPrecision(18, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.ShippingAmount).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

            entity.OwnsOne(o => o.ShippingAddress, a =>
            {
                a.Property(p => p.Street).HasColumnName("ShippingStreet").HasMaxLength(255).IsRequired();
                a.Property(p => p.City).HasColumnName("ShippingCity").HasMaxLength(100).IsRequired();
                a.Property(p => p.Province).HasColumnName("ShippingProvince").HasMaxLength(100);
                a.Property(p => p.PostalCode).HasColumnName("ShippingPostalCode").HasMaxLength(20);
                a.Property(p => p.Country).HasColumnName("ShippingCountry").HasMaxLength(100);
            });

            entity.HasOne(e => e.User)
                .WithMany(e => e.Orders)
                .HasForeignKey(e => e.UserId);

            entity.Property(e => e.InventoryReserved).IsRequired().HasDefaultValue(false);

            entity.HasMany(e => e.Audits)
                .WithOne(a => a.Order)
                .HasForeignKey(a => a.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
            entity.Property(e => e.DiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(18, 2);

            entity.HasOne(e => e.Order)
                .WithMany(e => e.OrderItems)
                .HasForeignKey(e => e.OrderId);
            entity.HasOne(e => e.Product)
                .WithMany(e => e.OrderItems)
                .HasForeignKey(e => e.ProductId);
        });

        modelBuilder.Entity<OrderAudit>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Reason).HasMaxLength(1000);

            entity.HasOne(e => e.Order)
                .WithMany(e => e.Audits)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ChangedByUser)
                .WithMany()
                .HasForeignKey(e => e.ChangedByUserId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.ChangedAt);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasPrecision(18, 2);
            entity.Property(e => e.Currency).HasMaxLength(3).HasDefaultValue("VND");
            entity.Property(e => e.TransactionId).HasMaxLength(100).IsRequired();
            entity.Property(e => e.GatewayResponse)
                .HasColumnType("nvarchar(max)")
                .HasConversion(v => FieldEncryption.Encrypt(v), v => FieldEncryption.Decrypt(v));

            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => new { e.Gateway, e.Status });

            entity.HasOne(e => e.Order)
                .WithMany(e => e.Payments)
                .HasForeignKey(e => e.OrderId);
        });

        // Payment audit configurations
        modelBuilder.Entity<PaymentAudit>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(50).IsRequired();
            entity.Property(e => e.TransactionId).HasMaxLength(100);
            entity.Property(e => e.RequestData)
                .HasColumnType("nvarchar(max)")
                .HasConversion(v => FieldEncryption.Encrypt(v), v => FieldEncryption.Decrypt(v));
            entity.Property(e => e.ResponseData)
                .HasColumnType("nvarchar(max)")
                .HasConversion(v => FieldEncryption.Encrypt(v), v => FieldEncryption.Decrypt(v));
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.CreatedByIp).HasMaxLength(45)
                .HasConversion(v => FieldEncryption.Encrypt(v), v => FieldEncryption.Decrypt(v));
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.UserId).HasMaxLength(50);
            entity.Property(e => e.Metadata)
                .HasColumnType("nvarchar(max)")
                .HasConversion(v => FieldEncryption.Encrypt(v), v => FieldEncryption.Decrypt(v));

            entity.HasIndex(e => e.PaymentId);
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Gateway, e.Action });

            entity.HasOne(e => e.Payment)
                .WithMany()
                .HasForeignKey(e => e.PaymentId);
        });

        // Payment method configuration
        modelBuilder.Entity<PaymentMethodConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.MinAmount).HasPrecision(18, 2);
            entity.Property(e => e.MaxAmount).HasPrecision(18, 2);
            entity.Property(e => e.FeePercentage).HasPrecision(5, 4);
            entity.Property(e => e.FixedFee).HasPrecision(18, 2);
            entity.Property(e => e.SupportedCurrencies).HasMaxLength(500);
            entity.Property(e => e.Configuration).HasColumnType("nvarchar(max)");

            entity.HasIndex(e => new { e.Gateway, e.Method }).IsUnique();
            entity.HasIndex(e => e.IsEnabled);
        });

        // Inventory configurations
        modelBuilder.Entity<Inventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Product)
                .WithOne(e => e.Inventory)
                .HasForeignKey<Inventory>(e => e.ProductId);
        });

        // Marketing configurations
        modelBuilder.Entity<Coupon>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Code).HasMaxLength(20).IsRequired();
            entity.HasIndex(e => e.Code).IsUnique();
            entity.Property(e => e.Value).HasPrecision(18, 2);
            entity.Property(e => e.MinimumOrderAmount).HasPrecision(18, 2);
            entity.Property(e => e.MaximumDiscountAmount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Campaign>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
        });

        modelBuilder.Entity<CampaignProduct>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DiscountPercentage).HasPrecision(5, 2);
            entity.Property(e => e.FixedDiscountAmount).HasPrecision(18, 2);
            entity.Property(e => e.SpecialPrice).HasPrecision(18, 2);

            entity.HasOne(e => e.Campaign)
                .WithMany(e => e.CampaignProducts)
                .HasForeignKey(e => e.CampaignId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // T007: Shopping Cart Entity Configurations
        modelBuilder.Entity<ShoppingCart>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(450);
            entity.Property(e => e.DiscountCode).HasMaxLength(50);
            entity.Property(e => e.DiscountAmount).HasPrecision(10, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(10, 2);
            entity.Property(e => e.ShippingCost).HasPrecision(10, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(12, 2);
            entity.Property(e => e.SubTotal).HasPrecision(12, 2);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.UserId, e.IsActive });

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CartSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId).IsRequired().HasMaxLength(128);
            entity.Property(e => e.BrowserFingerprint).HasMaxLength(64);
            entity.Property(e => e.IpAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.MigratedToUserId).HasMaxLength(450);
            entity.Property(e => e.DiscountCode).HasMaxLength(50);
            entity.Property(e => e.DiscountAmount).HasPrecision(10, 2);
            entity.Property(e => e.TaxAmount).HasPrecision(10, 2);
            entity.Property(e => e.ShippingCost).HasPrecision(10, 2);
            entity.Property(e => e.TotalAmount).HasPrecision(12, 2);
            entity.Property(e => e.SubTotal).HasPrecision(12, 2);

            entity.HasIndex(e => e.SessionId).IsUnique();
            entity.HasIndex(e => new { e.SessionId, e.IsActive });
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.LastAccessedAt);
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.UnitPrice).HasPrecision(10, 2);
            entity.Property(e => e.TotalPrice).HasPrecision(12, 2);
            entity.Property(e => e.ItemDiscount).HasPrecision(10, 2);
            entity.Property(e => e.ConfigurationOptions).HasMaxLength(2000);

            entity.HasIndex(e => e.ShoppingCartId);
            entity.HasIndex(e => e.CartSessionId);
            entity.HasIndex(e => e.ProductId);
            entity.HasIndex(e => new { e.ShoppingCartId, e.ProductId });
            entity.HasIndex(e => new { e.CartSessionId, e.ProductId });

            entity.HasOne(e => e.ShoppingCart)
                .WithMany(e => e.CartItems)
                .HasForeignKey(e => e.ShoppingCartId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CartSession)
                .WithMany(e => e.CartItems)
                .HasForeignKey(e => e.CartSessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(e => e.ParentBundleItem)
                .WithMany(e => e.BundleItems)
                .HasForeignKey(e => e.ParentBundleItemId)
                .OnDelete(DeleteBehavior.NoAction);

            // Ensure either ShoppingCartId or CartSessionId is set, but not both
            entity.ToTable(t => t.HasCheckConstraint("CK_CartItem_CartReference",
                "(ShoppingCartId IS NOT NULL AND CartSessionId IS NULL) OR (ShoppingCartId IS NULL AND CartSessionId IS NOT NULL)"));
        });

        // Blog configurations
        modelBuilder.Entity<BlogCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.MetaTitle).HasMaxLength(200);
            entity.Property(e => e.MetaDescription).HasMaxLength(500);
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<BlogPost>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Excerpt).HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.FeaturedImageUrl).HasMaxLength(500);
            entity.Property(e => e.MetaTitle).HasMaxLength(255);
            entity.Property(e => e.MetaDescription).HasMaxLength(500);
            entity.Property(e => e.Status).HasMaxLength(20).HasDefaultValue("draft");

            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.PublishedAt);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Category)
                .WithMany(c => c.BlogPosts)
                .HasForeignKey(e => e.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Author)
                .WithMany()
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<BlogComment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.AuthorName).HasMaxLength(100).IsRequired();
            entity.Property(e => e.AuthorEmail).HasMaxLength(255).IsRequired();
            entity.Property(e => e.AuthorWebsite).HasMaxLength(200);

            entity.HasIndex(e => e.IsApproved);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.BlogPost)
                .WithMany(p => p.Comments)
                .HasForeignKey(e => e.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Wishlist configuration
        modelBuilder.Entity<WishlistItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Product)
                .WithMany()
                .HasForeignKey(e => e.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // NEW ADMIN FEATURES CONFIGURATIONS

        // ProductCategory configuration
        modelBuilder.Entity<ProductCategory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.ImageUrl).HasMaxLength(500);

            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ParentId);

            // Self-referencing relationship for hierarchical categories
            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ProductBrand configuration
        modelBuilder.Entity<ProductBrand>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(255).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(255).IsRequired();
            entity.Property(e => e.LogoUrl).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Website).HasMaxLength(500);
            entity.Property(e => e.ContactEmail).HasMaxLength(255);

            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Slug).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // SystemSetting configuration
        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Category).HasMaxLength(50).IsRequired();
            entity.Property(e => e.SettingKey).HasMaxLength(100).IsRequired();
            entity.Property(e => e.SettingValue);
            entity.Property(e => e.DataType).HasMaxLength(20).HasDefaultValue("string");
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => new { e.Category, e.SettingKey }).IsUnique();
            entity.HasIndex(e => e.Category);
        });

        // BlogTag configuration
        modelBuilder.Entity<BlogTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Slug).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.Slug).IsUnique();
        });

        // BlogPostTag junction table configuration
        modelBuilder.Entity<BlogPostTag>(entity =>
        {
            entity.HasKey(e => new { e.BlogPostId, e.BlogTagId });

            entity.HasOne(e => e.BlogPost)
                .WithMany(e => e.BlogPostTags)
                .HasForeignKey(e => e.BlogPostId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.BlogTag)
                .WithMany(e => e.BlogPostTags)
                .HasForeignKey(e => e.BlogTagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserActivityLog configuration
        modelBuilder.Entity<UserActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(100);
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.IPAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });

            entity.HasOne(e => e.User)
                .WithMany(e => e.ActivityLogs)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserVipTier configuration
        modelBuilder.Entity<UserVipTier>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Color).HasMaxLength(20);
            entity.Property(e => e.Icon).HasMaxLength(50);

            entity.HasIndex(e => e.Name).IsUnique();
            entity.HasIndex(e => e.MinSpendAmount);
            entity.HasIndex(e => e.IsActive);
        });

        // OrderEvent configuration
        modelBuilder.Entity<OrderEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.OldValue).HasMaxLength(500);
            entity.Property(e => e.NewValue).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000).IsRequired();

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.EventType);

            entity.HasOne(e => e.Order)
                .WithMany(e => e.OrderEvents)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne<User>(e => e.AdminUser)
                .WithMany()
                .HasForeignKey(e => e.AdminUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // Refund configuration
        modelBuilder.Entity<Refund>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Reason).HasMaxLength(1000).IsRequired();
            entity.Property(e => e.Status).HasMaxLength(50).IsRequired();
            entity.Property(e => e.PaymentGateway).HasMaxLength(50);
            entity.Property(e => e.TransactionId).HasMaxLength(100);
            entity.Property(e => e.GatewayResponse);

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Order)
                .WithMany(e => e.Refunds)
                .HasForeignKey(e => e.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.ProcessedByAdmin)
                .WithMany()
                .HasForeignKey(e => e.ProcessedByAdminId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // ShippingRate configuration
        modelBuilder.Entity<ShippingRate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Provider).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ServiceName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.FromProvince).HasMaxLength(100).IsRequired();
            entity.Property(e => e.ToProvince).HasMaxLength(100).IsRequired();

            entity.HasIndex(e => new { e.Provider, e.FromProvince, e.ToProvince });
            entity.HasIndex(e => e.IsActive);
        });

        // Update Product entity to include Category and Brand relationships
        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasOne<ProductCategory>()
                .WithMany(c => c.Products)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne<ProductBrand>()
                .WithMany(b => b.Products)
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Update User entity to include VIP tier relationship
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasOne(e => e.VipTier)
                .WithMany(t => t.Users)
                .HasForeignKey(e => e.VipTierId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Update BlogCategory to support hierarchical structure
        modelBuilder.Entity<BlogCategory>(entity =>
        {
            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Security Event configurations
        modelBuilder.Entity<SecurityEvent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Severity).HasMaxLength(20).HasDefaultValue("medium");
            entity.Property(e => e.IPAddress).HasMaxLength(45).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.Details).HasDefaultValue("{}");
            entity.Property(e => e.Status).HasMaxLength(50).HasDefaultValue("new");
            entity.Property(e => e.UserAgent).HasMaxLength(1000);
            entity.Property(e => e.CorrelationId).HasMaxLength(50);
            entity.Property(e => e.Endpoint).HasMaxLength(200);
            entity.Property(e => e.RequestMethod).HasMaxLength(10);
            entity.Property(e => e.RiskScore).HasMaxLength(10).HasDefaultValue("0");

            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.AdminUserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.IPAddress);
            entity.HasIndex(e => e.CorrelationId);
            entity.HasIndex(e => e.Severity);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // System Audit Log configurations
        modelBuilder.Entity<SystemAuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventCategory).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EventType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.EntityId).HasMaxLength(50);
            entity.Property(e => e.Action).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000);
            entity.Property(e => e.OldValues);
            entity.Property(e => e.NewValues);
            entity.Property(e => e.IPAddress).HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(1000);
            entity.Property(e => e.UserEmail).HasMaxLength(255);
            entity.Property(e => e.UserRole).HasMaxLength(50);
            entity.Property(e => e.Endpoint).HasMaxLength(200);
            entity.Property(e => e.HttpMethod).HasMaxLength(10);
            entity.Property(e => e.RiskLevel).HasMaxLength(20).HasDefaultValue("low");

            entity.HasIndex(e => e.Action);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.AdminUserId);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
            entity.HasIndex(e => e.IPAddress);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.AdminUser)
                .WithMany()
                .HasForeignKey(e => e.AdminUserId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // IP Block Rule configurations
        modelBuilder.Entity<IPBlockRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IPAddress).HasMaxLength(45).IsRequired();
            entity.Property(e => e.Type).HasMaxLength(20).HasDefaultValue("blacklist");
            entity.Property(e => e.Reason).HasMaxLength(500);
            entity.Property(e => e.ThreatLevel).HasMaxLength(20).HasDefaultValue("medium");
            entity.Property(e => e.CountryCode).HasMaxLength(2);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.LastUpdatedBy).HasMaxLength(100);

            entity.HasIndex(e => e.IPAddress).IsUnique();
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.ExpiresAt);
            entity.HasIndex(e => e.ThreatLevel);
        });

        // Rate Limit Rule configurations
        modelBuilder.Entity<RateLimitRule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(100).IsRequired();
            entity.Property(e => e.Endpoint).HasMaxLength(200).IsRequired();
            entity.Property(e => e.HttpMethod).HasMaxLength(10).HasDefaultValue("ALL");
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            entity.Property(e => e.LastUpdatedBy).HasMaxLength(100);

            entity.HasIndex(e => new { e.Endpoint, e.HttpMethod });
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.Priority);
        });

        // Rate Limit Violation configurations
        modelBuilder.Entity<RateLimitViolation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IPAddress).HasMaxLength(45).IsRequired();
            entity.Property(e => e.Endpoint).HasMaxLength(200).IsRequired();
            entity.Property(e => e.HttpMethod).HasMaxLength(10).IsRequired();
            entity.Property(e => e.UserId).HasMaxLength(50);
            entity.Property(e => e.UserAgent).HasMaxLength(1000);
            entity.Property(e => e.Action).HasMaxLength(20).HasDefaultValue("blocked");

            entity.HasIndex(e => e.IPAddress);
            entity.HasIndex(e => e.Endpoint);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.IPAddress, e.Endpoint, e.HttpMethod });

            entity.HasOne(e => e.Rule)
                .WithMany()
                .HasForeignKey(e => e.RateLimitRuleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Login Attempt configurations
        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).HasMaxLength(255);
            entity.Property(e => e.IPAddress).HasMaxLength(45).IsRequired();
            entity.Property(e => e.UserAgent).HasMaxLength(1000);
            entity.Property(e => e.FailureReason).HasMaxLength(200);
            entity.Property(e => e.UserType).HasMaxLength(20).HasDefaultValue("user");
            entity.Property(e => e.TwoFactorMethod).HasMaxLength(50);
            entity.Property(e => e.GeoLocation).HasMaxLength(100);

            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.IPAddress);
            entity.HasIndex(e => e.AttemptedAt);
            entity.HasIndex(e => e.Success);
            entity.HasIndex(e => new { e.Email, e.Success, e.AttemptedAt });
        });

        // Seed data
        modelBuilder.Seed();
    }
}
