using MediatR;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Features.Brands;

// Create Brand
public record CreateBrandCommand(ProductBrand Brand) : IRequest<ProductBrand>;

public class CreateBrandHandler : IRequestHandler<CreateBrandCommand, ProductBrand>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreateBrandHandler> _logger;

    public CreateBrandHandler(ApplicationDbContext context, ILogger<CreateBrandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductBrand> Handle(CreateBrandCommand command, CancellationToken cancellationToken)
    {
        var brand = command.Brand;
        try
        {
            // Check if slug is unique
            var isUnique = !await _context.ProductBrands.AnyAsync(b => b.Slug == brand.Slug, cancellationToken);
            if (!isUnique)
            {
                throw new ArgumentException("Brand slug must be unique");
            }

            brand.CreatedAt = DateTime.UtcNow;
            brand.UpdatedAt = DateTime.UtcNow;

            _context.ProductBrands.Add(brand);
            await _context.SaveChangesAsync(cancellationToken);

            return brand;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating brand {BrandName}", brand.Name);
            throw;
        }
    }
}

// Update Brand
public record UpdateBrandCommand(ProductBrand Brand) : IRequest<ProductBrand>;

public class UpdateBrandHandler : IRequestHandler<UpdateBrandCommand, ProductBrand>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UpdateBrandHandler> _logger;

    public UpdateBrandHandler(ApplicationDbContext context, ILogger<UpdateBrandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductBrand> Handle(UpdateBrandCommand command, CancellationToken cancellationToken)
    {
        var brand = command.Brand;
        try
        {
            var existing = await _context.ProductBrands.FindAsync(new object[] { brand.Id }, cancellationToken);
            if (existing == null)
            {
                throw new ArgumentException("Brand not found");
            }

            // Check if slug is unique (excluding current brand)
            var isUnique = !await _context.ProductBrands.AnyAsync(b => b.Slug == brand.Slug && b.Id != brand.Id, cancellationToken);
            if (!isUnique)
            {
                throw new ArgumentException("Brand slug must be unique");
            }

            // Update properties
            existing.Name = brand.Name;
            existing.Slug = brand.Slug;
            existing.Description = brand.Description;
            existing.LogoUrl = brand.LogoUrl;
            existing.Website = brand.Website;
            existing.IsActive = brand.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating brand with ID {BrandId}", brand.Id);
            throw;
        }
    }
}

// Delete Brand
public record DeleteBrandCommand(int Id) : IRequest<bool>;

public class DeleteBrandHandler : IRequestHandler<DeleteBrandCommand, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DeleteBrandHandler> _logger;

    public DeleteBrandHandler(ApplicationDbContext context, ILogger<DeleteBrandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteBrandCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var brand = await _context.ProductBrands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken);

            if (brand == null)
            {
                throw new ArgumentException("Brand not found");
            }

            // Check if brand has products
            if (brand.Products.Any())
            {
                throw new InvalidOperationException("Cannot delete brand that has products assigned to it");
            }

            _context.ProductBrands.Remove(brand);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting brand with ID {BrandId}", command.Id);
            throw;
        }
    }
}

// Reassign Products and Delete Brand
public record ReassignProductsAndDeleteBrandCommand(int BrandIdToDelete, int NewBrandId) : IRequest<bool>;

public class ReassignProductsAndDeleteBrandHandler : IRequestHandler<ReassignProductsAndDeleteBrandCommand, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReassignProductsAndDeleteBrandHandler> _logger;

    public ReassignProductsAndDeleteBrandHandler(ApplicationDbContext context, ILogger<ReassignProductsAndDeleteBrandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(ReassignProductsAndDeleteBrandCommand command, CancellationToken cancellationToken)
    {
        var brandIdToDelete = command.BrandIdToDelete;
        var newBrandId = command.NewBrandId;

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var brandToDelete = await _context.ProductBrands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == brandIdToDelete, cancellationToken);

            if (brandToDelete == null)
            {
                throw new ArgumentException("Brand to delete not found");
            }

            var newBrand = await _context.ProductBrands
                .FirstOrDefaultAsync(b => b.Id == newBrandId, cancellationToken);

            if (newBrand == null)
            {
                throw new ArgumentException("Target brand not found");
            }

            // Reassign all products to the new brand
            var products = await _context.Products
                .Where(p => p.BrandId == brandIdToDelete)
                .ToListAsync(cancellationToken);

            foreach (var product in products)
            {
                product.BrandId = newBrandId;
                product.UpdatedAt = DateTime.UtcNow;
            }

            // Delete the brand
            _context.ProductBrands.Remove(brandToDelete);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Reassigned {ProductCount} products from brand {OldBrandName} to {NewBrandName} and deleted old brand",
                products.Count, brandToDelete.Name, newBrand.Name);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error reassigning products and deleting brand with ID {BrandId}", brandIdToDelete);
            throw;
        }
    }
}

// Force Delete Brand
public record ForceDeleteBrandCommand(int Id) : IRequest<bool>;

public class ForceDeleteBrandHandler : IRequestHandler<ForceDeleteBrandCommand, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ForceDeleteBrandHandler> _logger;

    public ForceDeleteBrandHandler(ApplicationDbContext context, ILogger<ForceDeleteBrandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(ForceDeleteBrandCommand command, CancellationToken cancellationToken)
    {
        var id = command.Id;
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var brand = await _context.ProductBrands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

            if (brand == null)
            {
                throw new ArgumentException("Brand not found");
            }

            // Deactivate all products of this brand
            var products = await _context.Products
                .Where(p => p.BrandId == id)
                .ToListAsync(cancellationToken);

            foreach (var product in products)
            {
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
            }

            // Delete the brand
            _context.ProductBrands.Remove(brand);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogWarning("Force deleted brand {BrandName} and deactivated {ProductCount} associated products",
                brand.Name, products.Count);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error force deleting brand with ID {BrandId}", id);
            throw;
        }
    }
}
