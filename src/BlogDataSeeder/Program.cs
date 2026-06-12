using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EcommerceLaptop.Infrastructure.Data;
using EcommerceLaptop.Core.Entities;
using System.Collections.Generic;
using System.Linq;
using System;

namespace BlogDataSeeder;

class Program
{
    static async Task Main(string[] args)
    {
        // Tạo configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // Tạo service collection cho console app
        var services = new ServiceCollection();
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddLogging(builder =>
        {
            builder.AddConsole();
            builder.SetMinimumLevel(LogLevel.Information);
        });

        // Build service provider
        var serviceProvider = services.BuildServiceProvider();

        // Lấy DbContext và logger
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Kiểm tra tham số clear data
        var clearData = args.Length > 0 && args[0].ToLower() == "--clear";

        // Chạy seeding
        await BlogSeeder.SeedBlogDataAsync(context, logger, clearData);

        Console.WriteLine("Seeding hoàn tất! Nhấn phím bất kỳ để thoát...");
        Console.ReadKey();
    }
}

/// <summary>
/// Class chứa logic seed dữ liệu blog
/// </summary>
public static class BlogSeeder
{
    /// <summary>
    /// Hàm seed dữ liệu blog - có thể export để sử dụng trong các project khác
    /// </summary>
    public static async Task SeedBlogDataAsync(ApplicationDbContext context, ILogger logger, bool clearData = false)
    {
        try
        {
            // Kết nối database
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Kết nối database thành công.");

            if (clearData)
            {
                // Xóa dữ liệu cũ nếu có yêu cầu
                logger.LogInformation("Xóa dữ liệu blog cũ...");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM BlogPostTags");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM BlogComments");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM BlogPosts");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM BlogTags");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM BlogCategories");
                logger.LogInformation("Đã xóa dữ liệu cũ.");
            }

            // Seed categories (Danh mục blog)
            var categories = new[]
            {
                new BlogCategory
                {
                    Name = "Đánh giá Laptop",
                    Slug = "danh-gia-laptop",
                    Description = "Các bài đánh giá chi tiết về laptop mới nhất",
                    MetaTitle = "Đánh giá Laptop",
                    MetaDescription = "Đọc đánh giá chuyên sâu về các mẫu laptop tốt nhất",
                    IsActive = true,
                    SortOrder = 1
                },
                new BlogCategory
                {
                    Name = "Công nghệ CPU/GPU",
                    Slug = "cpu-gpu",
                    Description = "Tin tức và phân tích về CPU, GPU mới nhất",
                    MetaTitle = "CPU và GPU",
                    MetaDescription = "Cập nhật công nghệ xử lý và đồ họa hiện đại",
                    IsActive = true,
                    SortOrder = 2
                },
                new BlogCategory
                {
                    Name = "Phụ kiện Laptop",
                    Slug = "phu-kien-laptop",
                    Description = "Hướng dẫn chọn phụ kiện cho laptop",
                    MetaTitle = "Phụ kiện Laptop",
                    MetaDescription = "Review chuột, bàn phím, dock và phụ kiện khác",
                    IsActive = true,
                    SortOrder = 3
                },
                new BlogCategory
                {
                    Name = "Tin tức Công nghệ",
                    Slug = "tin-tuc-cong-nghe",
                    Description = "Tin mới nhất về công nghệ laptop và phần cứng",
                    MetaTitle = "Tin tức Công nghệ",
                    MetaDescription = "Cập nhật xu hướng công nghệ laptop mới nhất",
                    IsActive = true,
                    SortOrder = 4
                }
            };

            // Kiểm tra và thêm categories nếu chưa tồn tại
            var existingCategorySlugs = await context.BlogCategories
                .Select(c => c.Slug)
                .ToListAsync();

            var categoryIds = new Dictionary<string, int>();
            foreach (var category in categories)
            {
                if (!existingCategorySlugs.Contains(category.Slug))
                {
                    category.CreatedAt = DateTime.UtcNow;
                    category.UpdatedAt = DateTime.UtcNow;
                    context.BlogCategories.Add(category);
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Thêm category: {category.Name} (ID: {category.Id})");
                    categoryIds[category.Slug] = category.Id;
                }
                else
                {
                    var existing = await context.BlogCategories.FirstAsync(c => c.Slug == category.Slug);
                    categoryIds[category.Slug] = existing.Id;
                    logger.LogInformation($"Category đã tồn tại: {category.Name} (ID: {existing.Id})");
                }
            }

            // Lấy ID của categories
            var laptopReviewId = categoryIds["danh-gia-laptop"];
            var cpuGpuId = categoryIds["cpu-gpu"];
            var accessoryId = categoryIds["phu-kien-laptop"];
            var newsId = categoryIds["tin-tuc-cong-nghe"];

            // Seed blog posts (ví dụ với bài viết)
            var posts = new[]
            {
                new BlogPost
                {
                    Title = "Đánh giá Dell XPS 13 Plus 2025: Laptop cao cấp hoàn hảo",
                    Slug = "danh-gia-dell-xps-13-plus-2025",
                    Excerpt = "Dell XPS 13 Plus 2025 mang đến thiết kế sang trọng và hiệu năng mạnh mẽ, phù hợp cho doanh nhân và sáng tạo nội dung.",
                    Content = "Dell XPS 13 Plus 2025 là một trong những ultrabook cao cấp nhất hiện nay. Với thiết kế unibody nhôm nguyên khối, máy có trọng lượng chỉ 1.26kg và độ mỏng 15.28mm. Màn hình OLED 13.4 inch với độ phân giải 3.2K và tần số quét 120Hz mang đến trải nghiệm hình ảnh tuyệt vời.\n\nCPU Intel Core Ultra 7 155H với 16 lõi và GPU tích hợp Intel Arc Graphics xử lý mượt mà các tác vụ văn phòng và chỉnh sửa ảnh/video cơ bản. RAM 32GB LPDDR5X và SSD 1TB NVMe đảm bảo tốc độ nhanh chóng.\n\nPin 55Wh cho thời lượng sử dụng lên đến 12 giờ. Cổng kết nối bao gồm 2 Thunderbolt 4 và jack tai nghe. Giá bán khoảng 40 triệu VND, phù hợp cho người dùng chuyên nghiệp.",
                    FeaturedImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=1600&auto=format&fit=crop&q=80",
                    MetaTitle = "Đánh giá Dell XPS 13 Plus 2025",
                    MetaDescription = "Review chi tiết Dell XPS 13 Plus 2025: Thiết kế, hiệu năng và giá bán",
                    Status = "published",
                    IsFeatured = true,
                    CategoryId = laptopReviewId,
                    AuthorId = 1,
                    ViewCount = 0,
                    PublishedAt = DateTime.UtcNow.AddDays(-7),
                    CreatedAt = DateTime.UtcNow.AddDays(-14),
                    UpdatedAt = DateTime.UtcNow
                },
                new BlogPost
                {
                    Title = "So sánh Intel Core Ultra vs AMD Ryzen 7000: CPU nào tốt hơn cho laptop?",
                    Slug = "so-sanh-intel-core-ultra-vs-amd-ryzen-7000",
                    Excerpt = "Intel Core Ultra series mới với NPU AI so với AMD Ryzen 7000 với hiệu năng đa lõi vượt trội.",
                    Content = "Năm 2025 chứng kiến cuộc chiến CPU laptop gay gắt giữa Intel và AMD. Intel Core Ultra (Meteor Lake) giới thiệu NPU chuyên dụng cho AI, trong khi AMD Ryzen 7000 (Zen 4) tập trung vào hiệu năng thô.\n\nIntel Core Ultra 7 155H có 16 lõi (6P+8E+2LP-E), xung nhịp turbo 4.8GHz, cache 24MB. NPU Intel AI Boost cho phép xử lý machine learning nhanh hơn 3 lần so với thế hệ trước.\n\nAMD Ryzen 7 7840HS có 8 lõi Zen 4, xung nhịp cao, hiệu năng đa luồng ấn tượng và iGPU RDNA3 mạnh mẽ.",
                    FeaturedImageUrl = "https://images.unsplash.com/photo-1518779578993-ec3579fee39f?w=1600&auto=format&fit=crop&q=80",
                    MetaTitle = "So sánh Intel Core Ultra vs AMD Ryzen 7000",
                    MetaDescription = "So sánh hiệu năng và tính năng AI giữa Intel Core Ultra và AMD Ryzen 7000",
                    Status = "published",
                    IsFeatured = false,
                    CategoryId = cpuGpuId,
                    AuthorId = 1,
                    ViewCount = 0,
                    PublishedAt = DateTime.UtcNow.AddDays(-10),
                    CreatedAt = DateTime.UtcNow.AddDays(-20),
                    UpdatedAt = DateTime.UtcNow
                }
            };

            // Kiểm tra và thêm posts nếu chưa tồn tại
            var existingPostSlugs = await context.BlogPosts
                .Select(p => p.Slug)
                .ToListAsync();

            foreach (var post in posts)
            {
                if (!existingPostSlugs.Contains(post.Slug))
                {
                    post.CreatedAt = DateTime.UtcNow;
                    post.UpdatedAt = DateTime.UtcNow;
                    context.BlogPosts.Add(post);
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Thêm post: {post.Title} (ID: {post.Id})");
                }
                else
                {
                    var existing = await context.BlogPosts.FirstAsync(p => p.Slug == post.Slug);
                    post.Id = existing.Id;
                    post.CreatedAt = existing.CreatedAt;
                    post.UpdatedAt = DateTime.UtcNow;
                    context.BlogPosts.Update(post);
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Post đã tồn tại: {post.Title} (ID: {existing.Id})");
                }
            }

            // Seed tags và gán qua BlogPostTags
            var tagSeeds = new[]
            {
                new BlogTag { Name = "laptop", Slug = "laptop", Description = "Bài viết về laptop" },
                new BlogTag { Name = "ultrabook", Slug = "ultrabook", Description = "Mỏng nhẹ, cao cấp" },
                new BlogTag { Name = "intel", Slug = "intel", Description = "Bài viết về Intel" },
                new BlogTag { Name = "amd", Slug = "amd", Description = "Bài viết về AMD" }
            };

            foreach (var t in tagSeeds)
            {
                if (!await context.BlogTags.AnyAsync(x => x.Slug == t.Slug))
                {
                    context.BlogTags.Add(t);
                }
            }
            await context.SaveChangesAsync();

            var tagMap = await context.BlogTags.ToDictionaryAsync(t => t.Slug, t => t.Id);
            var postMap = await context.BlogPosts.ToDictionaryAsync(p => p.Slug, p => p.Id);

            void Link(string postSlug, params string[] tagSlugs)
            {
                if (!postMap.TryGetValue(postSlug, out var pid)) return;
                foreach (var ts in tagSlugs)
                {
                    if (tagMap.TryGetValue(ts, out var tid))
                    {
                        var exists = context.BlogPostTags.Any(pt => pt.BlogPostId == pid && pt.BlogTagId == tid);
                        if (!exists)
                        {
                            context.BlogPostTags.Add(new BlogPostTag { BlogPostId = pid, BlogTagId = tid });
                        }
                    }
                }
            }

            Link("danh-gia-dell-xps-13-plus-2025", "laptop", "ultrabook", "intel");
            Link("so-sanh-intel-core-ultra-vs-amd-ryzen-7000", "intel", "amd", "laptop");
            await context.SaveChangesAsync();

            // Seed vài bình luận mẫu
            var samplePost = await context.BlogPosts.FirstOrDefaultAsync(p => p.Slug == "danh-gia-dell-xps-13-plus-2025");
            if (samplePost != null && !await context.BlogComments.AnyAsync(c => c.BlogPostId == samplePost.Id))
            {
                context.BlogComments.AddRange(new[]
                {
                    new BlogComment { BlogPostId = samplePost.Id, Content = "Bài viết rất chi tiết, cảm ơn bạn!", AuthorName = "Minh Nguyen", AuthorEmail = "minh@example.com", IsApproved = true, CreatedAt = DateTime.UtcNow.AddDays(-6) },
                    new BlogComment { BlogPostId = samplePost.Id, Content = "Đang phân vân giữa XPS 13 và MacBook Air.", AuthorName = "Lan Pham", AuthorEmail = "lan@example.com", IsApproved = true, CreatedAt = DateTime.UtcNow.AddDays(-5) }
                });
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi khi seed dữ liệu blog.");
        }
    }
}
