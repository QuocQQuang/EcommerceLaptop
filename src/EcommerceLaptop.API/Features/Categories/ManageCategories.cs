using MediatR;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EcommerceLaptop.API.Features.Categories;

// Create Category
public record CreateCategoryCommand(ProductCategory Category) : IRequest<ProductCategory>;

public class CreateCategoryHandler : IRequestHandler<CreateCategoryCommand, ProductCategory>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CreateCategoryHandler> _logger;

    public CreateCategoryHandler(ApplicationDbContext context, ILogger<CreateCategoryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductCategory> Handle(CreateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = command.Category;
        try
        {
            // Normalize or generate slug
            category.Slug = (category.Slug ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(category.Slug))
            {
                category.Slug = CategoryHelpers.Slugify(category.Name);
            }
            else
            {
                category.Slug = CategoryHelpers.Slugify(category.Slug);
            }

            // Ensure slug uniqueness (append numeric suffix if necessary)
            category.Slug = await CategoryHelpers.GenerateUniqueSlug(_context, category.Slug);

            // Validate parent exists if specified
            if (category.ParentId.HasValue)
            {
                var parentExists = await _context.ProductCategories
                    .AnyAsync(c => c.Id == category.ParentId.Value, cancellationToken);

                if (!parentExists)
                {
                    throw new ArgumentException("Parent category not found");
                }
            }

            // Set default sort order if not provided
            if (category.SortOrder == 0)
            {
                var maxOrder = await _context.ProductCategories
                    .Where(c => c.ParentId == category.ParentId)
                    .MaxAsync(c => (int?)c.SortOrder, cancellationToken) ?? 0;

                category.SortOrder = maxOrder + 1;
            }

            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;

            _context.ProductCategories.Add(category);
            await _context.SaveChangesAsync(cancellationToken);

            return category;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating category");
            throw;
        }
    }
}

// Update Category
public record UpdateCategoryCommand(ProductCategory Category) : IRequest<ProductCategory>;

public class UpdateCategoryHandler : IRequestHandler<UpdateCategoryCommand, ProductCategory>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UpdateCategoryHandler> _logger;

    public UpdateCategoryHandler(ApplicationDbContext context, ILogger<UpdateCategoryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ProductCategory> Handle(UpdateCategoryCommand command, CancellationToken cancellationToken)
    {
        var category = command.Category;
        try
        {
            var existing = await _context.ProductCategories.FindAsync(new object[] { category.Id }, cancellationToken);
            if (existing == null)
            {
                throw new ArgumentException("Category not found");
            }

            // Normalize slug
            category.Slug = (category.Slug ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(category.Slug))
            {
                category.Slug = CategoryHelpers.Slugify(category.Name);
            }
            else
            {
                category.Slug = CategoryHelpers.Slugify(category.Slug);
            }

            // Validate slug uniqueness (excluding current category)
            var existingSlug = await _context.ProductCategories
                .AnyAsync(c => c.Slug == category.Slug && c.Id != category.Id, cancellationToken);

            if (existingSlug)
            {
                throw new ArgumentException("Category slug already exists");
            }

            // Validate parent exists if specified and not creating circular reference
            if (category.ParentId.HasValue)
            {
                if (category.ParentId == category.Id)
                {
                    throw new ArgumentException("Category cannot be its own parent");
                }

                var parentExists = await _context.ProductCategories
                    .AnyAsync(c => c.Id == category.ParentId.Value, cancellationToken);

                if (!parentExists)
                {
                    throw new ArgumentException("Parent category not found");
                }

                // Check for circular references
                if (await CategoryHelpers.IsCircularReference(_context, category.Id, category.ParentId.Value))
                {
                    throw new ArgumentException("Circular reference detected");
                }
            }

            // Update properties
            existing.Name = category.Name;
            existing.Slug = category.Slug;
            existing.Description = category.Description;
            existing.ImageUrl = category.ImageUrl;
            existing.IsActive = category.IsActive;
            existing.ParentId = category.ParentId;
            existing.SortOrder = category.SortOrder;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            return existing;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating category with ID {CategoryId}", category.Id);
            throw;
        }
    }
}

// Delete Category
public record DeleteCategoryCommand(int Id) : IRequest<bool>;

public class DeleteCategoryHandler : IRequestHandler<DeleteCategoryCommand, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DeleteCategoryHandler> _logger;

    public DeleteCategoryHandler(ApplicationDbContext context, ILogger<DeleteCategoryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(DeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var category = await _context.ProductCategories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == command.Id, cancellationToken);

            if (category == null)
            {
               throw new ArgumentException("Category not found");
            }

            // Check if category has products
            var hasProducts = await _context.Products
                .AnyAsync(p => p.CategoryId == command.Id, cancellationToken);

            if (hasProducts)
            {
                throw new InvalidOperationException("Cannot delete category with associated products");
            }

            // Check if category has children
            if (category.Children.Any())
            {
                throw new InvalidOperationException("Cannot delete category with child categories");
            }

            _context.ProductCategories.Remove(category);
            await _context.SaveChangesAsync(cancellationToken);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting category with ID {CategoryId}", command.Id);
            throw;
        }
    }
}

// Reorder Categories
public record ReorderCategoriesCommand(List<CategoryReorderRequest> ReorderRequests) : IRequest<bool>;

public class ReorderCategoriesHandler : IRequestHandler<ReorderCategoriesCommand, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReorderCategoriesHandler> _logger;

    public ReorderCategoriesHandler(ApplicationDbContext context, ILogger<ReorderCategoriesHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(ReorderCategoriesCommand command, CancellationToken cancellationToken)
    {
        try
        {
            foreach (var request in command.ReorderRequests)
            {
                var category = await _context.ProductCategories.FindAsync(new object[] { request.Id }, cancellationToken);
                if (category != null)
                {
                    category.SortOrder = request.SortOrder;
                    if (request.ParentId.HasValue && request.ParentId != category.ParentId)
                    {
                        // Check for circular reference if changing parent
                        if (await CategoryHelpers.IsCircularReference(_context, category.Id, request.ParentId.Value))
                        {
                            throw new InvalidOperationException($"Cannot move category to create circular reference");
                        }
                        category.ParentId = request.ParentId;
                    }
                    category.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering categories");
            throw;
        }
    }
}

// Reassign Products and Delete Category
public record ReassignProductsAndDeleteCategoryCommand(int CategoryIdToDelete, int NewCategoryId) : IRequest<bool>;

public class ReassignProductsAndDeleteCategoryHandler : IRequestHandler<ReassignProductsAndDeleteCategoryCommand, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReassignProductsAndDeleteCategoryHandler> _logger;

    public ReassignProductsAndDeleteCategoryHandler(ApplicationDbContext context, ILogger<ReassignProductsAndDeleteCategoryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(ReassignProductsAndDeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        var categoryIdToDelete = command.CategoryIdToDelete;
        var newCategoryId = command.NewCategoryId;

        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var categoryToDelete = await _context.ProductCategories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == categoryIdToDelete, cancellationToken);

            if (categoryToDelete == null)
            {
               throw new ArgumentException("Category to delete not found");
            }

            var newCategory = await _context.ProductCategories
                .FirstOrDefaultAsync(c => c.Id == newCategoryId, cancellationToken);

            if (newCategory == null)
            {
                throw new ArgumentException("Target category not found");
            }

            // Check if category has children - cannot delete category with children
            if (categoryToDelete.Children.Any())
            {
                throw new InvalidOperationException("Cannot delete category with child categories. Please reassign or delete child categories first.");
            }

            // Reassign all products to the new category
            var products = await _context.Products
                .Where(p => p.CategoryId == categoryIdToDelete)
                .ToListAsync(cancellationToken);

            foreach (var product in products)
            {
                product.CategoryId = newCategoryId;
                product.UpdatedAt = DateTime.UtcNow;
            }

            // Delete the category
            _context.ProductCategories.Remove(categoryToDelete);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Reassigned {ProductCount} products from category {OldCategoryName} to {NewCategoryName} and deleted old category",
                products.Count, categoryToDelete.Name, newCategory.Name);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error reassigning products and deleting category with ID {CategoryId}", categoryIdToDelete);
            throw;
        }
    }
}

// Force Delete Category
public record ForceDeleteCategoryCommand(int Id) : IRequest<bool>;

public class ForceDeleteCategoryHandler : IRequestHandler<ForceDeleteCategoryCommand, bool>
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ForceDeleteCategoryHandler> _logger;

    public ForceDeleteCategoryHandler(ApplicationDbContext context, ILogger<ForceDeleteCategoryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<bool> Handle(ForceDeleteCategoryCommand command, CancellationToken cancellationToken)
    {
        var id = command.Id;
        using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var category = await _context.ProductCategories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

            if (category == null)
            {
                 throw new ArgumentException("Category not found");
            }

            // Move child categories to parent category (or make them root categories)
            var childCategories = await _context.ProductCategories
                .Where(c => c.ParentId == id)
                .ToListAsync(cancellationToken);

            foreach (var child in childCategories)
            {
                child.ParentId = category.ParentId; // This could be null (root level)
                child.UpdatedAt = DateTime.UtcNow;
            }

            // Deactivate all products of this category
            var products = await _context.Products
                .Where(p => p.CategoryId == id)
                .ToListAsync(cancellationToken);

            foreach (var product in products)
            {
                product.IsActive = false;
                product.UpdatedAt = DateTime.UtcNow;
            }

            // Delete the category
            _context.ProductCategories.Remove(category);

            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogWarning("Force deleted category {CategoryName}, moved {ChildCount} child categories to parent level, and deactivated {ProductCount} associated products",
                category.Name, childCategories.Count, products.Count);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error force deleting category with ID {CategoryId}", id);
            throw;
        }
    }
}
