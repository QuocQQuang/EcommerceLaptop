using MediatR;
using EcommerceLaptop.Core.DTOs;
using EcommerceLaptop.API.DTOs;
using EcommerceLaptop.Core.Interfaces;
using EcommerceLaptop.Core.Specifications.Products;
using EcommerceLaptop.Core.Entities;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq; // Added for Select, OrderBy, etc.
using EcommerceLaptop.Core.Services;

namespace EcommerceLaptop.API.Features.Products;

// Bulk Update Pricing
public record BulkUpdatePricingCommand(BulkPricingUpdateRequest Request) : IRequest<bool>;

public class BulkUpdatePricingHandler : IRequestHandler<BulkUpdatePricingCommand, bool>
{
    private readonly IAsyncRepository<Product> _productRepository;

    public BulkUpdatePricingHandler(IAsyncRepository<Product> productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<bool> Handle(BulkUpdatePricingCommand command, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAsync(new ProductsByIdsSpecification(command.Request.ProductIds));
        foreach (var product in products)
        {
            var adjustment = product.Price * (command.Request.PriceAdjustmentPercentage / 100);
            product.Price = Math.Max(product.Price + adjustment, 0.01m);
            product.UpdatedAt = DateTime.UtcNow;
            await _productRepository.UpdateAsync(product);
        }
        return true;
    }
}

// Upload Variant Images - Note: This involves file handling which often stays in Controller, 
// but we can pass IFormFileCollection to command if we want strict MediaR.
// Ideally, commands should not depend on ASP.NET Core types like IFormFile, but for pragmatism here we can allowed it or map to streams.
// Given strict refactoring goals, I will pass IFormFileCollection to keep logic encapsulated.
public record UploadVariantImagesCommand(int ProductId, int VariantId, IFormFileCollection Files) : IRequest<UploadVariantImagesResult>;

public record UploadVariantImagesResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? Data { get; init; }
    public int StatusCode { get; init; } = 200;
}

public class UploadVariantImagesHandler : IRequestHandler<UploadVariantImagesCommand, UploadVariantImagesResult>
{
    private readonly IAsyncRepository<Product> _productRepository;
    private readonly IAsyncRepository<ProductImage> _imageRepository;
    private readonly IImageHostingService _imageHostingService; 
    private readonly Microsoft.Extensions.Logging.ILogger<UploadVariantImagesHandler> _logger;

    public UploadVariantImagesHandler(
        IAsyncRepository<Product> productRepository, 
        IAsyncRepository<ProductImage> imageRepository,
        IImageHostingService imageHostingService,
        Microsoft.Extensions.Logging.ILogger<UploadVariantImagesHandler> logger)
    {
        _productRepository = productRepository;
        _imageRepository = imageRepository;
        _imageHostingService = imageHostingService;
        _logger = logger;
    }

    public async Task<UploadVariantImagesResult> Handle(UploadVariantImagesCommand command, CancellationToken cancellationToken)
    {
        var files = command.Files;
        var variantId = command.VariantId;
        var id = command.ProductId;

        // ... Logic moved from Controller ...
        
        if (files == null || files.Count == 0)
             return new UploadVariantImagesResult { Success = false, Message = "No files provided", StatusCode = 400 };

        // Verify variant exists and belongs to the base product
        var variant = await _productRepository.GetByIdAsync(variantId);
        if (variant == null || variant.ParentProductId != id)
             return new UploadVariantImagesResult { Success = false, Message = "Variant not found", StatusCode = 404 };

        var uploadResults = new List<EcommerceLaptop.Core.Services.ImageUploadResult>();
        var productImages = new List<EcommerceLaptop.Core.Entities.ProductImage>();

        _logger.LogInformation("Starting upload of {FileCount} files for variant {VariantId}", files.Count, variantId);

        for (int i = 0; i < files.Count; i++)
        {
            var file = files[i];
            
            // Validate file
            if (!_imageHostingService.IsValidImage(file.FileName, file.ContentType, file.Length))
            {
                 return new UploadVariantImagesResult { Success = false, Message = $"Invalid image file: {file.FileName}", StatusCode = 400 };
            }

            try
            {
                // Upload to ImgBB
                using var stream = file.OpenReadStream();
                // Ensure ImageCategory enum is available or cast
                var uploadResult = await _imageHostingService.UploadImageAsync(stream, file.FileName, EcommerceLaptop.Core.Services.ImageCategory.Products);
                uploadResults.Add(uploadResult);

                // Prepare ProductImage entity
                productImages.Add(new EcommerceLaptop.Core.Entities.ProductImage
                {
                    ProductId = variantId,
                    ImageUrl = uploadResult.Url,
                    AltText = $"{variant.Name} - Image",
                    DisplayOrder = productImages.Count + 1, // Note: This internal count won't reflect DB state accurately but acceptable for batch
                    ImageId = uploadResult.ImageId,
                    DeleteUrl = uploadResult.DeleteUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload image {FileName} for variant {VariantId}", file.FileName, variantId);

                // Cleanup already uploaded images on error
                foreach (var uploadedResult in uploadResults)
                {
                    try
                    {
                        await _imageHostingService.DeleteImageAsync(uploadedResult.ImageId);
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(cleanupEx, "Failed to cleanup uploaded image {ImageId}", uploadedResult.ImageId);
                    }
                }

                 return new UploadVariantImagesResult { Success = false, Message = $"Failed to upload image: {file.FileName}", StatusCode = 500 };
            }
        }

        // Save all images to database
        // WAS: _productService.UpdateProductImagesAsync(variantId, productImages);
        // Logic: Get existing images to determine order, then add new ones.
        var productWithImages = await _productRepository.GetEntityWithSpec(new ProductWithDetailsSpecification(variantId));
        if (productWithImages == null) return new UploadVariantImagesResult { Success = false, Message = "Variant not found during save", StatusCode = 404 };

        var existingImages = productWithImages.Images?.OrderBy(img => img.DisplayOrder).ToList() ?? new List<ProductImage>();
        var nextDisplayOrder = existingImages.Any() ? existingImages.Max(img => img.DisplayOrder) + 1 : 1;

        foreach (var image in productImages)
        {
            image.ProductId = variantId; // Ensure correct ID
            if (image.DisplayOrder <= 0 || image.DisplayOrder <= existingImages.Count) // Adjust order
            {
                image.DisplayOrder = nextDisplayOrder++;
            }
            else
            {
                // If we set it in the loop based on local list count, we might need to offset it by existing count
                 image.DisplayOrder += existingImages.Count; // Simple offset approach
                 // Actually, simpler to just reassign all orders for the batch
                 image.DisplayOrder = nextDisplayOrder++;
            }
           
            await _imageRepository.AddAsync(image);
        }

        return new UploadVariantImagesResult
        {
            Success = true,
            Message = $"Successfully uploaded {productImages.Count} images",
            Data = new
            {
                variantId = variantId,
                uploadedImages = uploadResults.Select((r, idx) => new
                {
                    imageId = r.ImageId,
                    imageUrl = r.Url,
                    displayOrder = idx + 1,
                    message = "Upload successful"
                }).ToList()
            }
        };
    }
}
