using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Core.Interfaces;

namespace EcommerceLaptop.Infrastructure.Services.AI
{
    public class ProductChunkingService : IProductChunkingService
    {
        private const int MaxTokensPerChunk = 500;
        private const int OverlapTokens = 50;

        public IEnumerable<ProductChunk> ChunkProduct(Product product)
        {
            var chunks = new List<ProductChunk>();
            int chunkIndex = 0;

            // 1. Primary Chunk: Title + Specs (High Priority)
            var titleSpecsContent = GenerateTitleSpecsIndices(product);
            chunks.Add(new ProductChunk
            {
                Id = $"{product.Id}_chunk_{chunkIndex++}",
                Content = titleSpecsContent,
                Metadata = new Dictionary<string, object>
                {
                    { "product_id", product.Id },
                    { "chunk_type", "title_specs" },
                    { "priority", 1.0 },
                    { "product_name", product.Name }
                }
            });

            // 2. Description Chunks (Medium Priority)
            if (!string.IsNullOrWhiteSpace(product.Description))
            {
                var descriptionChunks = SplitTextIntoChunks(product.Description);
                foreach (var descChunk in descriptionChunks)
                {
                    chunks.Add(new ProductChunk
                    {
                        Id = $"{product.Id}_chunk_{chunkIndex++}",
                        Content = descChunk,
                        Metadata = new Dictionary<string, object>
                        {
                            { "product_id", product.Id },
                            { "chunk_type", "description" },
                            { "priority", 0.7 },
                            { "product_name", product.Name }
                        }
                    });
                }
            }
            
            // TODO: FAQs if available in future
            
            return chunks;
        }

        private string GenerateTitleSpecsIndices(Product product)
        {
            var content = $"Product: {product.Name}\n";
            content += $"Category: {product.Category?.Name ?? "Unknown"}\n";
            content += $"Brand: {product.Brand}\n";
            content += $"Model: {product.Model}\n";
            content += $"Price Range: {FormatPriceRange(product.Price)}\n"; // Approximate range for filtering

            if (product is Laptop laptop)
            {
                content += $"Specs: {laptop.CpuBrand} {laptop.CpuModel}, {laptop.RamCapacityGB}GB RAM, {laptop.StorageCapacityGB}GB {laptop.StorageType}, {laptop.GpuBrand} {laptop.GpuModel}, {laptop.DisplaySizeInches}\" {laptop.DisplayResolution}\n";
                content += $"Use Case: {laptop.TargetAudience}\n";
            }
            else if (product is Accessory accessory)
            {
                content += $"Type: {accessory.AccessoryType}\n";
                content += $"Connectivity: {accessory.Connectivity}\n";
            }

            return content;
        }

        private IEnumerable<string> SplitTextIntoChunks(string text)
        {
            // Simple robust splitting strategy
            // 1. Split by double newlines (paragraphs)
            // 2. If paragraph > max tokens, split by sentences
            // 3. Re-assemble into chunks of ~MaxTokensPerChunk

            var paragraphs = text.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            var currentChunk = "";
            
            foreach (var paragraph in paragraphs)
            {
                // Heuristic: 1 word ~ 1.3 tokens (English), but for Vietnamese it varies.
                // Using char count heuristic: 1 token ~ 4 chars
                if ((currentChunk.Length + paragraph.Length) / 4 > MaxTokensPerChunk)
                {
                    if (!string.IsNullOrEmpty(currentChunk))
                    {
                        yield return currentChunk.Trim();
                        // Start new chunk with overlaps if needed (omitted for simplicity here, just overlap last sentence could work)
                        currentChunk = ""; 
                    }

                    // If paragraph itself is too huge, split by sentences
                    if (paragraph.Length / 4 > MaxTokensPerChunk) 
                    {
                        var sentences = Regex.Split(paragraph, @"(?<=[.!?])\s+");
                        foreach (var sentence in sentences)
                        {
                             if ((currentChunk.Length + sentence.Length) / 4 > MaxTokensPerChunk)
                             {
                                 yield return currentChunk.Trim();
                                 currentChunk = sentence;
                             }
                             else
                             {
                                 currentChunk += " " + sentence;
                             }
                        }
                    }
                    else
                    {
                        currentChunk = paragraph;
                    }
                }
                else
                {
                    currentChunk += (currentChunk.Length > 0 ? "\n\n" : "") + paragraph;
                }
            }

            if (!string.IsNullOrWhiteSpace(currentChunk))
            {
                yield return currentChunk.Trim();
            }
        }

        private string FormatPriceRange(decimal price)
        {
            if (price < 10000000) return "Budget (< 10M)";
            if (price < 20000000) return "Mid-Range (10M - 20M)";
            if (price < 35000000) return "High-End (20M - 35M)";
            return "Premium (> 35M)";
        }
    }
}
