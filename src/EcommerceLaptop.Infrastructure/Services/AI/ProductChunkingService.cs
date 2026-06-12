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

            // 0. Semantic Profile Chunk (Highest Priority - RAG optimized)
            // This chunk is designed for semantic matching of intents like "student laptop", "gaming", "thin and light"
            var semanticProfile = GenerateSemanticProfile(product);
            var metadata = new Dictionary<string, object>
            {
                { "product_id", product.Id },
                { "chunk_type", "semantic_profile" },
                { "priority", 1.5 }, // Higher verification priority than raw specs
                { "product_name", product.Name },
                // Hard Filter Fields (Step 4 compliance)
                { "price", (double)product.Price },
                { "brand_id", product.BrandId ?? 0 },
                { "category_id", product.CategoryId ?? 0 }
            };

            if (product is Laptop l)
            {
                metadata.Add("weight", (double)l.WeightKg);
                metadata.Add("ram_gb", l.RamCapacityGB);
            }

            chunks.Add(new ProductChunk
            {
                Id = $"{product.Id}_chunk_{chunkIndex++}",
                Content = semanticProfile,
                Metadata = metadata
            });

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

        private string GenerateSemanticProfile(Product product)
        {
            var profile = $"Sản phẩm: {product.Name}\n";
            
            // 1. Identify Use Cases
            var useCases = new List<string>();
            var mobility = new List<string>();
            var priceSegment = "";

            if (product is Laptop laptop)
            {
                // -- Use Case Heuristics --
                bool isPowerfulGpu = laptop.GpuType == "Discrete" || 
                                     laptop.GpuModel.Contains("RTX") || 
                                     laptop.GpuModel.Contains("GTX") || 
                                     laptop.GpuModel.Contains("Radeon RX");

                bool isHighRam = laptop.RamCapacityGB >= 16;
                bool isCreativeScreen = laptop.DisplayResolution.Contains("Retina") || 
                                        laptop.DisplayResolution.Contains("OLED") || 
                                        laptop.DisplayResolution.Contains("QHD") || 
                                        laptop.DisplayResolution.Contains("2K") || 
                                        laptop.DisplayResolution.Contains("3K") || 
                                        laptop.DisplayResolution.Contains("4K");

                if (isPowerfulGpu)
                {
                    useCases.Add("Gaming (Chơi game)");
                    useCases.Add("Thiết kế đồ họa (Graphic Design)");
                    useCases.Add("Kỹ thuật (Engineering)");
                    useCases.Add("Dựng phim (Video Editing)");
                }

                if (product.Brand == "Apple" || (isHighRam && isCreativeScreen))
                {
                    useCases.Add("Sáng tạo nội dung (Content Creator)");
                    useCases.Add("Đồ họa 2D");
                    useCases.Add("Giải trí cao cấp");
                }

                if (product.Price < 15000000 && !isPowerfulGpu)
                {
                    useCases.Add("Học tập (Student)");
                    useCases.Add("Văn phòng cơ bản (Office)");
                    useCases.Add("Lướt web, xem phim");
                }
                
                if (product.Price >= 15000000 && product.Price < 30000000 && laptop.RamCapacityGB >= 16)
                {
                    useCases.Add("Lập trình viên (Coder/Programmer)");
                    useCases.Add("Văn phòng đa nhiệm");
                    useCases.Add("Sinh viên CNTT");
                }

                // -- Mobility Heuristics --
                if (laptop.WeightKg < 1.5m)
                {
                    mobility.Add("Mỏng nhẹ (Thin & Light)");
                    mobility.Add("Di động cao (High Mobility)");
                    mobility.Add("Dễ mang đi học/làm");
                }
                else if (laptop.WeightKg > 2.3m)
                {
                    mobility.Add("Máy trạm (Workstation)");
                    mobility.Add("Thay thế máy bàn (Desktop Replacement)");
                }
                else
                {
                     mobility.Add("Cân bằng hiệu năng/trọng lượng");
                }
            }
            else if (product is Accessory acc)
            {
                useCases.Add("Phụ kiện");
                useCases.Add(acc.AccessoryType);
            }

            // -- Price Segment --
            if (product.Price < 10000000) priceSegment = "Giá rẻ, bình dân (Dưới 10 triệu)";
            else if (product.Price < 15000000) priceSegment = "Phổ thông (10 - 15 triệu)";
            else if (product.Price < 25000000) priceSegment = "Tầm trung (15 - 25 triệu)";
            else if (product.Price < 40000000) priceSegment = "Cận cao cấp (25 - 40 triệu)";
            else priceSegment = "Cao cấp, sang trọng (Trên 40 triệu)";

            // -- Assemble Profile --
            if (useCases.Any()) profile += $"Phù hợp cho: {string.Join(", ", useCases)}.\n";
            if (mobility.Any()) profile += $"Tính di động: {string.Join(", ", mobility)}.\n";
            profile += $"Phân khúc giá: {priceSegment}.\n";
            
            // Add key specs in natural language for reinforcement
            if (product is Laptop l)
            {
                profile += $"Cấu hình: CPU {l.CpuBrand} {l.CpuModel}, RAM {l.RamCapacityGB}GB, SSD {l.StorageCapacityGB}GB, {l.GpuBrand} {l.GpuModel}, Màn hình {l.DisplaySizeInches} inch.\n";
                if (!string.IsNullOrEmpty(l.Color)) profile += $"Màu sắc: {l.Color}.\n";
                // Add soft filters helper including Storage
                profile += $"[FilterTags]: Price:{product.Price}, Weight:{l.WeightKg}kg, RAM:{l.RamCapacityGB}GB, Storage:{l.StorageCapacityGB}GB\n";
            }

            return profile;
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
