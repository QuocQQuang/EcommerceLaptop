using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EcommerceLaptop.API.Seed;

public static class BlogDataSeeder
{
    public static void SeedIfMissing(ApplicationDbContext context, ILogger logger)
    {
        var now = DateTime.UtcNow;
        var authorId = context.Users
            .Where(u => u.IsAdminRole)
            .Select(u => u.Id)
            .FirstOrDefault();

        if (authorId == 0)
        {
            authorId = context.Users.Select(u => u.Id).FirstOrDefault();
        }

        if (authorId == 0)
        {
            logger.LogWarning("Skipping blog seed because no user exists for BlogPost.AuthorId.");
            return;
        }

        var categoryIds = SeedCategories(context, now);
        var tagIds = SeedTags(context, now);

        var posts = BuildPosts(categoryIds, authorId, now);
        foreach (var post in posts)
        {
            var existing = context.BlogPosts.FirstOrDefault(p => p.Slug == post.Slug);
            if (existing == null)
            {
                context.BlogPosts.Add(post);
                context.SaveChanges();
                existing = post;
                logger.LogInformation("Seeded blog post: {Title}", post.Title);
            }
            else
            {
                existing.Title = post.Title;
                existing.Excerpt = post.Excerpt;
                existing.Content = post.Content;
                existing.FeaturedImageUrl = post.FeaturedImageUrl;
                existing.MetaTitle = post.MetaTitle;
                existing.MetaDescription = post.MetaDescription;
                existing.Status = post.Status;
                existing.IsFeatured = post.IsFeatured;
                existing.CategoryId = post.CategoryId;
                existing.AuthorId = post.AuthorId;
                existing.PublishedAt = post.PublishedAt;
                existing.UpdatedAt = now;
                context.SaveChanges();
                logger.LogInformation("Updated seeded blog post: {Title}", post.Title);
            }

            LinkTags(context, existing.Id, post.Slug, tagIds);
        }

        SeedComments(context, now);
    }

    private static Dictionary<string, int> SeedCategories(ApplicationDbContext context, DateTime now)
    {
        var categories = new[]
        {
            new BlogCategory
            {
                Name = "Đánh giá Laptop",
                Slug = "danh-gia-laptop",
                Description = "Bài đánh giá thực tế về laptop mới, hiệu năng, màn hình, bàn phím, pin và trải nghiệm sử dụng.",
                MetaTitle = "Đánh giá laptop mới nhất",
                MetaDescription = "Review laptop chi tiết cho học tập, văn phòng, gaming và sáng tạo nội dung.",
                IsActive = true,
                SortOrder = 1
            },
            new BlogCategory
            {
                Name = "Tư vấn mua Laptop",
                Slug = "tu-van-mua-laptop",
                Description = "Hướng dẫn chọn laptop theo nhu cầu, ngân sách và cấu hình để tránh mua sai.",
                MetaTitle = "Tư vấn mua laptop",
                MetaDescription = "Kinh nghiệm chọn laptop đúng nhu cầu, cấu hình và mức giá.",
                IsActive = true,
                SortOrder = 2
            },
            new BlogCategory
            {
                Name = "Công nghệ CPU/GPU",
                Slug = "cpu-gpu",
                Description = "Phân tích CPU, GPU, NPU, RAM, SSD và những thay đổi phần cứng ảnh hưởng tới laptop.",
                MetaTitle = "Công nghệ CPU GPU laptop",
                MetaDescription = "Tin tức và phân tích phần cứng laptop mới nhất.",
                IsActive = true,
                SortOrder = 3
            },
            new BlogCategory
            {
                Name = "Phụ kiện Laptop",
                Slug = "phu-kien-laptop",
                Description = "Gợi ý dock, màn hình, chuột, bàn phím, balo, sleeve và phụ kiện tăng hiệu quả làm việc.",
                MetaTitle = "Phụ kiện laptop đáng mua",
                MetaDescription = "Review và hướng dẫn chọn phụ kiện laptop.",
                IsActive = true,
                SortOrder = 4
            }
        };

        foreach (var category in categories)
        {
            var existing = context.BlogCategories.FirstOrDefault(c => c.Slug == category.Slug);
            if (existing == null)
            {
                category.CreatedAt = now;
                category.UpdatedAt = now;
                context.BlogCategories.Add(category);
            }
            else
            {
                existing.Name = category.Name;
                existing.Description = category.Description;
                existing.MetaTitle = category.MetaTitle;
                existing.MetaDescription = category.MetaDescription;
                existing.IsActive = true;
                existing.SortOrder = category.SortOrder;
                existing.UpdatedAt = now;
            }
        }

        context.SaveChanges();

        return context.BlogCategories
            .Where(c => categories.Select(seed => seed.Slug).Contains(c.Slug))
            .ToDictionary(c => c.Slug, c => c.Id);
    }

    private static Dictionary<string, int> SeedTags(ApplicationDbContext context, DateTime now)
    {
        var tags = new[]
        {
            new BlogTag { Name = "laptop", Slug = "laptop", Description = "Laptop và thiết bị di động", Color = "#2563EB", IsActive = true },
            new BlogTag { Name = "ultrabook", Slug = "ultrabook", Description = "Laptop mỏng nhẹ cao cấp", Color = "#7C3AED", IsActive = true },
            new BlogTag { Name = "gaming", Slug = "gaming", Description = "Laptop gaming và hiệu năng cao", Color = "#DC2626", IsActive = true },
            new BlogTag { Name = "cpu-gpu", Slug = "cpu-gpu", Description = "CPU, GPU và phần cứng laptop", Color = "#059669", IsActive = true },
            new BlogTag { Name = "tu-van", Slug = "tu-van", Description = "Tư vấn mua laptop", Color = "#EA580C", IsActive = true }
        };

        foreach (var tag in tags)
        {
            var existing = context.BlogTags.FirstOrDefault(t => t.Slug == tag.Slug);
            if (existing == null)
            {
                tag.CreatedAt = now;
                tag.UpdatedAt = now;
                context.BlogTags.Add(tag);
            }
            else
            {
                existing.Name = tag.Name;
                existing.Description = tag.Description;
                existing.Color = tag.Color;
                existing.IsActive = true;
                existing.UpdatedAt = now;
            }
        }

        context.SaveChanges();

        return context.BlogTags
            .Where(t => tags.Select(seed => seed.Slug).Contains(t.Slug))
            .ToDictionary(t => t.Slug, t => t.Id);
    }

    private static List<BlogPost> BuildPosts(Dictionary<string, int> categoryIds, int authorId, DateTime now)
    {
        return
        [
            new BlogPost
            {
                Title = "Đánh giá Dell XPS 13 Plus 2025: ultrabook cao cấp cho công việc di động",
                Slug = "danh-gia-dell-xps-13-plus-2025",
                Excerpt = "Dell XPS 13 Plus 2025 nổi bật ở thiết kế mỏng nhẹ, màn hình đẹp, hiệu năng ổn định và trải nghiệm bàn phím hiện đại. Bài viết phân tích máy theo đúng các tiêu chí người mua laptop cao cấp quan tâm.",
                Content = DellXpsReviewContent(),
                FeaturedImageUrl = "https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=1600&auto=format&fit=crop&q=80",
                MetaTitle = "Đánh giá Dell XPS 13 Plus 2025",
                MetaDescription = "Review Dell XPS 13 Plus 2025 với thiết kế, màn hình, hiệu năng, pin và gợi ý mua hàng.",
                Status = "published",
                IsFeatured = true,
                CategoryId = categoryIds["danh-gia-laptop"],
                AuthorId = authorId,
                PublishedAt = now.AddDays(-9),
                CreatedAt = now.AddDays(-10),
                UpdatedAt = now
            },
            new BlogPost
            {
                Title = "Chọn laptop văn phòng năm 2026: cấu hình nào đủ dùng trong 3-5 năm?",
                Slug = "chon-laptop-van-phong-2026-cau-hinh-nao-du-dung",
                Excerpt = "Một chiếc laptop văn phòng tốt không chỉ cần giá hợp lý. Người mua nên cân bằng CPU, RAM, SSD, màn hình, bàn phím, pin và khả năng bảo hành để máy dùng bền trong nhiều năm.",
                Content = OfficeLaptopGuideContent(),
                FeaturedImageUrl = "https://images.unsplash.com/photo-1497366754035-f200968a6e72?w=1600&auto=format&fit=crop&q=80",
                MetaTitle = "Chọn laptop văn phòng 2026",
                MetaDescription = "Hướng dẫn chọn laptop văn phòng đủ mạnh, bền và dễ nâng cấp cho nhiều năm sử dụng.",
                Status = "published",
                IsFeatured = true,
                CategoryId = categoryIds["tu-van-mua-laptop"],
                AuthorId = authorId,
                PublishedAt = now.AddDays(-5),
                CreatedAt = now.AddDays(-6),
                UpdatedAt = now
            },
            new BlogPost
            {
                Title = "Intel Core Ultra, Ryzen AI và GPU rời: hiểu đúng trước khi mua laptop hiệu năng",
                Slug = "intel-core-ultra-ryzen-ai-gpu-roi-hieu-dung-truoc-khi-mua",
                Excerpt = "Tên CPU/GPU ngày càng phức tạp, nhưng người mua chỉ cần hiểu vài điểm cốt lõi: mức điện, số nhân, NPU, GPU tích hợp và hệ thống tản nhiệt của từng mẫu laptop.",
                Content = CpuGpuGuideContent(),
                FeaturedImageUrl = "https://images.unsplash.com/photo-1518779578993-ec3579fee39f?w=1600&auto=format&fit=crop&q=80",
                MetaTitle = "Intel Core Ultra, Ryzen AI và GPU rời trên laptop",
                MetaDescription = "Giải thích CPU, GPU, NPU và tản nhiệt laptop để chọn máy hiệu năng đúng nhu cầu.",
                Status = "published",
                IsFeatured = false,
                CategoryId = categoryIds["cpu-gpu"],
                AuthorId = authorId,
                PublishedAt = now.AddDays(-2),
                CreatedAt = now.AddDays(-3),
                UpdatedAt = now
            }
        ];
    }

    private static void LinkTags(ApplicationDbContext context, int postId, string postSlug, Dictionary<string, int> tagIds)
    {
        var slugs = postSlug switch
        {
            "danh-gia-dell-xps-13-plus-2025" => new[] { "laptop", "ultrabook", "tu-van" },
            "chon-laptop-van-phong-2026-cau-hinh-nao-du-dung" => new[] { "laptop", "tu-van" },
            "intel-core-ultra-ryzen-ai-gpu-roi-hieu-dung-truoc-khi-mua" => new[] { "laptop", "cpu-gpu", "gaming" },
            _ => []
        };

        var existingLinks = context.BlogPostTags.Where(pt => pt.BlogPostId == postId).ToList();
        context.BlogPostTags.RemoveRange(existingLinks);

        foreach (var slug in slugs)
        {
            if (tagIds.TryGetValue(slug, out var tagId))
            {
                context.BlogPostTags.Add(new BlogPostTag { BlogPostId = postId, BlogTagId = tagId });
            }
        }

        context.SaveChanges();
    }

    private static void SeedComments(ApplicationDbContext context, DateTime now)
    {
        var post = context.BlogPosts.FirstOrDefault(p => p.Slug == "danh-gia-dell-xps-13-plus-2025");
        if (post == null || context.BlogComments.Any(c => c.BlogPostId == post.Id))
        {
            return;
        }

        context.BlogComments.AddRange(
            new BlogComment
            {
                BlogPostId = post.Id,
                Content = "Bài viết rất dễ hiểu. Mình đang phân vân giữa XPS 13 Plus và một mẫu ThinkPad, phần nói về bàn phím và màn hình khá hữu ích.",
                AuthorName = "Minh Nguyen",
                AuthorEmail = "minh@example.com",
                IsApproved = true,
                CreatedAt = now.AddDays(-4)
            },
            new BlogComment
            {
                BlogPostId = post.Id,
                Content = "Mình muốn xem thêm so sánh nhiệt độ khi chạy nhiều tab trình duyệt và họp video liên tục.",
                AuthorName = "Lan Pham",
                AuthorEmail = "lan@example.com",
                IsApproved = true,
                CreatedAt = now.AddDays(-3)
            });

        context.SaveChanges();
    }

    private static string DellXpsReviewContent() => """
        <h2>Thiết kế và cảm giác sử dụng</h2>
        <p>Dell XPS 13 Plus 2025 tiếp tục theo đuổi phong cách tối giản: thân nhôm nguyên khối, viền màn hình mỏng và bề mặt kê tay liền mạch. Điểm mạnh của máy không chỉ nằm ở ngoại hình cao cấp mà còn ở cảm giác cầm nắm chắc, ít ọp ẹp và dễ mang theo trong balo công việc hằng ngày.</p>
        <p>Bàn phím có hành trình ngắn nhưng phản hồi rõ. Dãy phím chức năng cảm ứng cần một thời gian làm quen, tuy nhiên khi đã quen thì thao tác tăng giảm âm lượng, độ sáng và điều khiển media khá nhanh. Touchpad ẩn dưới mặt kính tạo cảm giác hiện đại, nhưng người dùng mới nên dành vài ngày để điều chỉnh thao tác kéo thả.</p>
        <h2>Màn hình, loa và webcam</h2>
        <p>Màn hình là điểm đáng tiền nhất. Tấm nền độ phân giải cao cho chữ sắc, màu đẹp và độ tương phản tốt khi làm việc với tài liệu, bảng tính, ảnh sản phẩm hoặc dashboard bán hàng. Nếu bạn thường chỉnh ảnh, viết nội dung hoặc làm presentation, màn hình này giúp giảm mỏi mắt rõ rệt so với laptop phổ thông.</p>
        <p>Loa đủ lớn cho phòng nhỏ, giọng nói trong cuộc họp online nghe rõ. Webcam không thay thế được camera rời, nhưng đủ dùng cho call nội bộ. Trong bối cảnh làm việc hybrid, tổ hợp màn hình đẹp, loa ổn và mic rõ giúp XPS 13 Plus trở thành một máy làm việc di động đáng tin cậy.</p>
        <p>[product:1:Dell XPS 13 Plus 2025:39990000:https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=600&auto=format&fit=crop&q=80]</p>
        <h2>Hiệu năng thực tế</h2>
        <p>Với nhóm tác vụ văn phòng nặng như mở nhiều tab, chạy Slack, Teams, Notion, Excel và dashboard quản trị cùng lúc, máy giữ phản hồi tốt nếu cấu hình RAM từ 16GB trở lên. CPU thế hệ mới xử lý tốt tác vụ ngắn, còn SSD NVMe giúp mở app và tìm kiếm file nhanh.</p>
        <p>Điều cần hiểu là XPS 13 Plus không phải laptop gaming hay workstation. Máy có thể chỉnh ảnh, biên tập video nhẹ, xử lý thiết kế marketing cơ bản, nhưng nếu render 3D hoặc dựng video dài mỗi ngày thì một mẫu có GPU rời và tản nhiệt lớn sẽ hợp lý hơn.</p>
        <h2>Pin và cổng kết nối</h2>
        <p>Thời lượng pin phụ thuộc mạnh vào độ sáng màn hình và workload. Với tác vụ văn phòng hỗn hợp, máy đủ cho một buổi làm việc dài; nếu bật màn hình độ phân giải cao ở độ sáng lớn, thời lượng sẽ giảm. Cổng kết nối tối giản là điểm cần cân nhắc: người dùng thường cắm màn hình, LAN hoặc nhiều USB nên mua thêm dock USB-C.</p>
        <h2>Có nên mua?</h2>
        <p>XPS 13 Plus phù hợp với người cần một chiếc laptop cao cấp, nhẹ, đẹp, màn hình xuất sắc và hiệu năng ổn định cho công việc tri thức. Nếu ưu tiên số cổng, dễ nâng cấp RAM hoặc hiệu năng GPU, bạn nên xem thêm các dòng business hoặc creator lớn hơn. Còn nếu ưu tiên trải nghiệm di động và cảm giác cao cấp, đây là một lựa chọn rất mạnh trong nhóm ultrabook.</p>
        """;

    private static string OfficeLaptopGuideContent() => """
        <h2>Đừng chỉ nhìn CPU khi mua laptop văn phòng</h2>
        <p>Nhiều người mua laptop văn phòng vẫn bắt đầu bằng câu hỏi: máy này i5 hay i7? Cách hỏi đó không sai, nhưng chưa đủ. Một chiếc laptop dùng tốt trong 3-5 năm cần cân bằng CPU, RAM, SSD, màn hình, bàn phím, pin, trọng lượng và chính sách bảo hành. Nếu một yếu tố quá yếu, trải nghiệm hằng ngày vẫn khó chịu dù CPU nghe có vẻ mạnh.</p>
        <p>Với nhân viên văn phòng, kế toán, sale, quản lý vận hành hoặc chủ shop online, workload thường là trình duyệt nhiều tab, Excel, phần mềm chat, họp video và dashboard web. Nhóm tác vụ này cần RAM và SSD ổn định hơn là GPU mạnh.</p>
        <h2>Cấu hình nên chọn</h2>
        <p>Mốc tối thiểu nên là RAM 16GB và SSD 512GB. RAM 8GB vẫn chạy được tác vụ nhẹ, nhưng nhanh đầy khi mở nhiều tab Chrome, file Excel lớn và họp video cùng lúc. SSD 256GB cũng dễ thiếu nếu bạn lưu nhiều ảnh sản phẩm, file báo cáo, video ngắn hoặc bộ cài phần mềm.</p>
        <p>CPU nên chọn từ Intel Core i5/Core Ultra 5 hoặc AMD Ryzen 5 đời mới trở lên. Với nhóm cần chạy phân tích dữ liệu nhẹ, Power BI, nhiều màn hình ngoài hoặc phần mềm kế toán nặng, nâng lên Core Ultra 7 hoặc Ryzen 7 sẽ có ý nghĩa hơn.</p>
        <p>[product:2:Lenovo ThinkPad T14 Business Laptop:28990000:https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=600&auto=format&fit=crop&q=80]</p>
        <h2>Màn hình và bàn phím quan trọng hơn bạn nghĩ</h2>
        <p>Một màn hình 14 inch hoặc 15.6 inch độ phân giải Full HD trở lên là mức hợp lý. Nếu làm việc với bảng tính rộng, màn hình 16:10 sẽ hiển thị thêm dòng dữ liệu và giảm thao tác cuộn. Độ sáng nên từ 300 nits nếu thường làm việc ở quán cà phê hoặc văn phòng nhiều ánh sáng.</p>
        <p>Bàn phím nên có hành trình rõ, layout dễ quen và phím điều hướng thuận tiện. Với người nhập liệu cả ngày, bàn phím tốt giúp giảm lỗi gõ và giảm mỏi tay. Touchpad cũng cần đủ rộng, tracking ổn định, vì không phải lúc nào bạn cũng dùng chuột rời.</p>
        <h2>Pin, trọng lượng và bảo hành</h2>
        <p>Nếu phải di chuyển nhiều, trọng lượng dưới 1.5kg là lý tưởng. Pin thực tế nên đạt ít nhất 6-8 giờ tác vụ hỗn hợp. Ngoài cấu hình, hãy kiểm tra bảo hành chính hãng, khả năng thay pin, thay bàn phím và nâng cấp SSD. Laptop doanh nghiệp thường có lợi thế ở độ bền và dịch vụ sau bán hàng.</p>
        <h2>Kết luận</h2>
        <p>Cấu hình đáng mua nhất cho phần lớn người dùng văn phòng năm 2026 là CPU Core Ultra 5/Ryzen 5 trở lên, RAM 16GB, SSD 512GB, màn hình 14 inch 16:10 và pin tốt. Nếu ngân sách cho phép, ưu tiên màn hình, bàn phím và bảo hành trước khi nâng CPU lên dòng cao hơn.</p>
        """;

    private static string CpuGpuGuideContent() => """
        <h2>Vì sao tên CPU/GPU laptop dễ gây nhầm lẫn?</h2>
        <p>Cùng một tên CPU nhưng hiệu năng trên hai laptop có thể rất khác nhau. Lý do là laptop bị giới hạn bởi công suất điện, thiết kế tản nhiệt và cách hãng cấu hình BIOS. Một CPU mạnh đặt trong thân máy quá mỏng có thể không duy trì xung cao lâu bằng CPU thấp hơn nhưng được tản nhiệt tốt.</p>
        <p>Với Intel Core Ultra và Ryzen AI, người mua còn gặp thêm khái niệm NPU. NPU giúp xử lý một số tác vụ AI tiết kiệm điện hơn, nhưng chưa thay thế CPU/GPU trong mọi phần mềm. Nếu ứng dụng của bạn chưa hỗ trợ NPU, lợi ích sẽ chưa rõ như quảng cáo.</p>
        <h2>GPU tích hợp hay GPU rời?</h2>
        <p>GPU tích hợp đời mới đã đủ tốt cho xuất nhiều màn hình, chỉnh ảnh nhẹ, xem video độ phân giải cao và một số game eSports. Tuy nhiên, nếu bạn dựng video, render 3D, chạy AI local hoặc chơi game AAA, GPU rời vẫn quan trọng. Khi chọn GPU rời, hãy xem cả mức TGP, không chỉ tên RTX 4050 hay RTX 4060.</p>
        <p>[product:4:ASUS ROG Zephyrus G14 Gaming Laptop:45990000:https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=600&auto=format&fit=crop&q=80]</p>
        <h2>Tản nhiệt quyết định hiệu năng bền</h2>
        <p>Một bài benchmark ngắn chỉ nói lên hiệu năng tức thời. Khi chạy game, render hoặc export video trong 20-30 phút, máy nóng sẽ giảm xung để bảo vệ linh kiện. Vì vậy, laptop hiệu năng nên có hệ thống tản nhiệt tốt, khe gió thông thoáng và profile quạt rõ ràng.</p>
        <p>Nếu bạn cần máy làm việc yên tĩnh, hãy ưu tiên laptop business hoặc creator có chế độ balanced tốt. Nếu bạn chấp nhận tiếng quạt để đổi lấy FPS/render nhanh, laptop gaming sẽ phù hợp hơn.</p>
        <h2>Cách chọn nhanh theo nhu cầu</h2>
        <p>Văn phòng và học tập: Core Ultra 5/Ryzen 5, RAM 16GB, GPU tích hợp là đủ. Sáng tạo nội dung nhẹ: Core Ultra 7/Ryzen 7, RAM 32GB nếu ngân sách cho phép. Gaming và render: ưu tiên GPU rời, tản nhiệt, màn hình tần số quét cao và nguồn sạc đủ công suất.</p>
        <p>Tóm lại, đừng mua laptop chỉ vì tên chip. Hãy đọc cấu hình điện, review nhiệt độ, độ ồn và hiệu năng duy trì. Đây mới là các yếu tố quyết định máy có nhanh trong công việc thật hay chỉ nhanh trên thông số.</p>
        """;
}
