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
        // To configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        // To service collection cho console app
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

        // Ly DbContext v logger
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        // Kim tra tham s clear data
        var clearData = args.Length > 0 && args[0].ToLower() == "--clear";

        // Chy seeding
        await BlogSeeder.SeedBlogDataAsync(context, logger, clearData);

        Console.WriteLine("Seeding hon tt! Nhn phm bt k  thot...");
        Console.ReadKey();
    }
}

/// <summary>
/// Class cha logic seed d liu blog
/// </summary>
public static class BlogSeeder
{
    /// <summary>
    /// Hm seed d liu blog - c th export  s dng trong cc project khc
    /// </summary>
    public static async Task SeedBlogDataAsync(ApplicationDbContext context, ILogger logger, bool clearData = false)
    {
        try
        {
            // Kt ni database
            await context.Database.EnsureCreatedAsync();
            logger.LogInformation("Kt ni database thnh cng.");

            if (clearData)
            {
                // Xa d liu c nu c yu cu
                logger.LogInformation("Xa d liu blog c...");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM BlogPostTags");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM BlogComments");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM BlogPosts");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM BlogTags");
                await context.Database.ExecuteSqlRawAsync("DELETE FROM BlogCategories");
                logger.LogInformation(" xa d liu c.");
            }

            // Seed categories (Danh mc blog)
            var categories = new[]
            {
                new BlogCategory
                {
                    Name = "nh gi Laptop",
                    Slug = "danh-gia-laptop",
                    Description = "Cc bi nh gi chi tit v laptop mi nht",
                    MetaTitle = "nh gi Laptop",
                    MetaDescription = "c nh gi chuyn su v cc mu laptop tt nht",
                    IsActive = true,
                    SortOrder = 1
                },
                new BlogCategory
                {
                    Name = "Cng ngh CPU/GPU",
                    Slug = "cpu-gpu",
                    Description = "Tin tc v phn tch v CPU, GPU mi nht",
                    MetaTitle = "CPU v GPU",
                    MetaDescription = "Cp nht cng ngh x l v  ha hin i",
                    IsActive = true,
                    SortOrder = 2
                },
                new BlogCategory
                {
                    Name = "Ph kin Laptop",
                    Slug = "phu-kien-laptop",
                    Description = "Hng dn chn ph kin cho laptop",
                    MetaTitle = "Ph kin Laptop",
                    MetaDescription = "Review chut, bn phm, dock v ph kin khc",
                    IsActive = true,
                    SortOrder = 3
                },
                new BlogCategory
                {
                    Name = "Tin tc Cng ngh",
                    Slug = "tin-tuc-cong-nghe",
                    Description = "Tin mi nht v cng ngh laptop v phn cng",
                    MetaTitle = "Tin tc Cng ngh",
                    MetaDescription = "Cp nht xu hng cng ngh laptop mi nht",
                    IsActive = true,
                    SortOrder = 4
                }
            };

            // Kim tra v thm categories nu cha tn ti
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
                    logger.LogInformation($"Thm category: {category.Name} (ID: {category.Id})");
                    categoryIds[category.Slug] = category.Id;
                }
                else
                {
                    var existing = await context.BlogCategories.FirstAsync(c => c.Slug == category.Slug);
                    categoryIds[category.Slug] = existing.Id;
                    logger.LogInformation($"Category  tn ti: {category.Name} (ID: {existing.Id})");
                }
            }

            // Ly ID ca categories
            var laptopReviewId = categoryIds["danh-gia-laptop"];
            var cpuGpuId = categoryIds["cpu-gpu"];
            var accessoryId = categoryIds["phu-kien-laptop"];
            var newsId = categoryIds["tin-tuc-cong-nghe"];

            // Seed blog posts (v d vi bi vit)
            var posts = new[]
            {
                new BlogPost
                {
                    Title = "nh gi Dell XPS 13 Plus 2025: Laptop cao cp hon ho",
                    Slug = "danh-gia-dell-xps-13-plus-2025",
                    Excerpt = "Dell XPS 13 Plus 2025 mang n thit k sang trng v hiu nng mnh m, ph hp cho doanh nhn v sng to ni dung.",
                    Content = "Dell XPS 13 Plus 2025 l mt trong nhng ultrabook cao cp nht hin nay. Vi thit k unibody nhm nguyn khi, my c trng lng ch 1.26kg v  mng 15.28mm. Mn hnh OLED 13.4 inch vi  phn gii 3.2K v tn s qut 120Hz mang n tri nghim hnh nh tuyt vi.\n\nCPU Intel Core Ultra 7 155H vi 16 li v GPU tch hp Intel Arc Graphics x l mt m cc tc v vn phng v chnh sa nh/video c bn. RAM 32GB LPDDR5X v SSD 1TB NVMe m bo tc  nhanh chng.\n\nPin 55Wh cho thi lng s dng ln n 12 gi. Cng kt ni bao gm 2 Thunderbolt 4 v jack tai nghe. Gi bn khong 40 triu VND, ph hp cho ngi dng chuyn nghip.",
                    FeaturedImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=1600&auto=format&fit=crop&q=80",
                    MetaTitle = "nh gi Dell XPS 13 Plus 2025",
                    MetaDescription = "Review chi tit Dell XPS 13 Plus 2025: Thit k, hiu nng v gi bn",
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
                    Title = "So snh Intel Core Ultra vs AMD Ryzen 7000: CPU no tt hn cho laptop?",
                    Slug = "so-sanh-intel-core-ultra-vs-amd-ryzen-7000",
                    Excerpt = "Intel Core Ultra series mi vi NPU AI so vi AMD Ryzen 7000 vi hiu nng a li vt tri.",
                    Content = "Nm 2025 chng kin cuc chin CPU laptop gay gt gia Intel v AMD. Intel Core Ultra (Meteor Lake) gii thiu NPU chuyn dng cho AI, trong khi AMD Ryzen 7000 (Zen 4) tp trung vo hiu nng th.\n\nIntel Core Ultra 7 155H c 16 li (6P+8E+2LP-E), xung nhp turbo 4.8GHz, cache 24MB. NPU Intel AI Boost cho php x l machine learning nhanh hn 3 ln so vi th h trc.\n\nAMD Ryzen 7 7840HS c 8 li Zen 4, xung nhp cao, hiu nng a lung n tng v iGPU RDNA3 mnh m.",
                    FeaturedImageUrl = "https://images.unsplash.com/photo-1518779578993-ec3579fee39f?w=1600&auto=format&fit=crop&q=80",
                    MetaTitle = "So snh Intel Core Ultra vs AMD Ryzen 7000",
                    MetaDescription = "So snh hiu nng v tnh nng AI gia Intel Core Ultra v AMD Ryzen 7000",
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

            // Kim tra v thm posts nu cha tn ti
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
                    logger.LogInformation($"Thm post: {post.Title} (ID: {post.Id})");
                }
                else
                {
                    var existing = await context.BlogPosts.FirstAsync(p => p.Slug == post.Slug);
                    post.Id = existing.Id; // Update existing post's ID
                    post.CreatedAt = existing.CreatedAt;
                    post.UpdatedAt = DateTime.UtcNow;
                    context.BlogPosts.Update(post);
                    await context.SaveChangesAsync();
                    logger.LogInformation($"Post  tn ti: {post.Title} (ID: {existing.Id})");
                }
            }

            // Seed tags v gn qua BlogPostTags
            var tagSeeds = new[]
            {
                new BlogTag { Name = "laptop", Slug = "laptop", Description = "Bi vit v laptop" },
                new BlogTag { Name = "ultrabook", Slug = "ultrabook", Description = "Mng nh, cao cp" },
                new BlogTag { Name = "intel", Slug = "intel", Description = "Bi vit v Intel" },
                new BlogTag { Name = "amd", Slug = "amd", Description = "Bi vit v AMD" }
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

            // Seed vi bnh lun mu
            var samplePost = await context.BlogPosts.FirstOrDefaultAsync(p => p.Slug == "danh-gia-dell-xps-13-plus-2025");
            if (samplePost != null && !await context.BlogComments.AnyAsync(c => c.BlogPostId == samplePost.Id))
            {
                context.BlogComments.AddRange(new[]
                {
                    new BlogComment { BlogPostId = samplePost.Id, Content = "Bi vit rt chi tit, cm n bn!", AuthorName = "Minh Nguyen", AuthorEmail = "minh@example.com", IsApproved = true, CreatedAt = DateTime.UtcNow.AddDays(-6) },
                    new BlogComment { BlogPostId = samplePost.Id, Content = "ang phn vn gia XPS 13 v MacBook Air.", AuthorName = "Lan Pham", AuthorEmail = "lan@example.com", IsApproved = true, CreatedAt = DateTime.UtcNow.AddDays(-5) }
                });
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Li khi seed d liu blog.");
        }
    }
}