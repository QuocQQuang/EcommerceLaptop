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
        <h2>Một chiếc ultrabook được thiết kế cho người làm việc trong chuyển động</h2>
        <p>Dell XPS 13 Plus 2025 không cố trở thành chiếc laptop nhiều cổng nhất hay mạnh nhất trong mọi bài benchmark. Điểm bán hàng của máy nằm ở trải nghiệm cao cấp được đóng gói gọn: thân nhôm chắc, màn hình đẹp, bàn phím hiện đại và hiệu năng đủ nhanh cho một ngày làm việc dày đặc.</p>
        <p>Nếu bạn thường xuyên di chuyển giữa văn phòng, quán cà phê, phòng họp và sân bay, cảm giác cầm máy quan trọng không kém thông số. XPS 13 Plus tạo ấn tượng như một thiết bị làm việc cao cấp: mỏng, ít chi tiết thừa, mở ra là sẵn sàng làm việc ngay.</p>
        <blockquote>Điểm mạnh của XPS 13 Plus không phải là gây choáng bằng cấu hình, mà là làm cho các tác vụ hằng ngày trở nên liền mạch, gọn và ít ma sát hơn.</blockquote>
        <p><img src="https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=1400&auto=format&fit=crop&q=80" alt="Dell XPS 13 Plus trên bàn làm việc tối giản" /></p>
        <h2>Thiết kế: tối giản nhưng có chủ đích</h2>
        <p>Khung máy nguyên khối cho cảm giác chắc tay, các cạnh hoàn thiện sạch và tổng thể đủ nhẹ để mang theo cả ngày. Dãy phím chức năng cảm ứng và touchpad ẩn có thể gây lạ trong vài ngày đầu, nhưng khi đã quen, bề mặt làm việc liền mạch giúp máy trông hiện đại hơn hẳn laptop văn phòng truyền thống.</p>
        <p>Bàn phím có hành trình ngắn, phản hồi rõ và khoảng cách phím rộng. Với người viết email, xử lý proposal, nhập nội dung sản phẩm hoặc làm báo cáo, cảm giác gõ là một điểm cộng thực tế. Điểm cần cân nhắc là máy tối giản cổng, nên người dùng thường thuyết trình hoặc dùng màn hình ngoài nên chuẩn bị sẵn dock USB-C.</p>
        <h2>Màn hình là lý do khiến máy đáng nhớ</h2>
        <p>Màn hình độ phân giải cao giúp chữ sắc, màu sâu và hình ảnh sản phẩm hiển thị có chiều sâu hơn. Khi làm việc với landing page, slide bán hàng, ảnh campaign hoặc dashboard thương mại điện tử, chất lượng màn hình tạo khác biệt rõ: bạn ít phải nheo mắt, ít zoom tới lui và dễ phát hiện lỗi visual hơn.</p>
        <ul>
            <li><strong>Người làm nội dung:</strong> hưởng lợi từ màu sắc đẹp và chữ rõ khi viết, chỉnh ảnh, duyệt layout.</li>
            <li><strong>Người quản lý:</strong> xem dashboard, bảng tính và báo cáo trong thời gian dài thoải mái hơn.</li>
            <li><strong>Người bán hàng:</strong> trình bày proposal hoặc demo sản phẩm trông cao cấp hơn trên màn hình đẹp.</li>
        </ul>
        <p>[product:1:Dell XPS 13 Plus 2025:39990000:https://images.unsplash.com/photo-1517336714731-489689fd1ca8?w=600&auto=format&fit=crop&q=80]</p>
        <h2>Hiệu năng: nhanh ở đúng nơi cần nhanh</h2>
        <p>Trong kịch bản thực tế gồm Chrome nhiều tab, Teams, Slack, Notion, Excel, CMS và dashboard analytics, máy phản hồi tốt nếu chọn RAM từ 16GB trở lên. SSD NVMe giúp mở app, tìm file và chuyển ngữ cảnh nhanh; đây là thứ bạn cảm nhận được mỗi ngày rõ hơn vài phần trăm điểm benchmark.</p>
        <p>XPS 13 Plus vẫn không phải workstation. Máy phù hợp chỉnh ảnh, cắt video ngắn, thiết kế nhẹ, quản trị website và làm nội dung marketing. Nếu bạn render video dài, dựng 3D hoặc chơi game nặng, hãy chọn laptop creator/gaming có GPU rời và hệ thống tản nhiệt lớn hơn.</p>
        <h2>Pin, nhiệt và trải nghiệm họp online</h2>
        <p>Với độ sáng vừa phải và workload văn phòng, máy đủ cho một phiên làm việc dài. Khi bật màn hình sáng cao hoặc họp video liên tục, pin sẽ tụt nhanh hơn, nhưng đây là đánh đổi phổ biến của nhóm ultrabook màn hình đẹp. Loa và mic đủ tốt cho call nội bộ, webcam đáp ứng nhu cầu họp nhanh mà không cần setup thêm.</p>
        <h2>Kết luận: dành cho ai?</h2>
        <p>Hãy chọn Dell XPS 13 Plus nếu bạn muốn một laptop cao cấp, nhẹ, đẹp, màn hình xuất sắc và đủ mạnh cho công việc tri thức. Đừng chọn nếu bạn cần nhiều cổng, nâng cấp phần cứng linh hoạt hoặc hiệu năng GPU dài hạn. Với đúng nhóm người dùng, đây là chiếc máy tạo cảm giác chuyên nghiệp mỗi khi mở ra làm việc.</p>
        <p><strong>Gợi ý mua hàng:</strong> ưu tiên bản RAM 16GB hoặc 32GB, SSD 512GB trở lên. Nếu bạn thường làm presentation, hãy mua kèm dock USB-C ngay từ đầu để trải nghiệm trọn vẹn hơn.</p>
        """;

    private static string OfficeLaptopGuideContent() => """
        <h2>Chiếc laptop văn phòng tốt là chiếc máy biến mất khỏi suy nghĩ của bạn</h2>
        <p>Mua laptop văn phòng không nên bắt đầu bằng câu hỏi i5 hay i7. Người dùng văn phòng cần một thiết bị chạy ổn mỗi ngày, mở file nhanh, họp online không giật, gõ thoải mái, pin đủ dài và ít phát sinh lỗi vặt. Một chiếc máy cân bằng sẽ tạo ra năng suất thật tốt hơn một cấu hình nghe mạnh nhưng màn hình xấu, RAM thiếu hoặc bàn phím khó chịu.</p>
        <p>Hãy nghĩ về laptop như một công cụ làm việc 3-5 năm. Mỗi ngày bạn mở máy hàng chục lần, chuyển giữa trình duyệt, Excel, email, phần mềm chat, dashboard bán hàng và cuộc họp video. Những tác vụ này không cần GPU quá mạnh, nhưng rất cần RAM, SSD, màn hình và bàn phím tốt.</p>
        <p><img src="https://images.unsplash.com/photo-1497366754035-f200968a6e72?w=1400&auto=format&fit=crop&q=80" alt="Không gian làm việc văn phòng với laptop" /></p>
        <h2>Cấu hình nền: đừng tiết kiệm sai chỗ</h2>
        <p>Mốc hợp lý cho năm 2026 là <strong>RAM 16GB và SSD 512GB</strong>. RAM 8GB vẫn mở được Word và trình duyệt, nhưng sẽ nhanh đuối khi bạn mở nhiều tab, file Excel lớn, họp video và phần mềm quản trị cùng lúc. SSD 256GB cũng nhanh đầy nếu lưu ảnh sản phẩm, tài liệu khách hàng, báo cáo và file tải về.</p>
        <ul>
            <li><strong>CPU:</strong> Intel Core Ultra 5/Ryzen 5 đời mới là đủ cho phần lớn nhân viên văn phòng.</li>
            <li><strong>RAM:</strong> 16GB là điểm cân bằng; 32GB cho người dùng nhiều dashboard, Power BI hoặc dữ liệu lớn.</li>
            <li><strong>SSD:</strong> 512GB là mức nên chọn; ưu tiên máy cho phép nâng cấp nếu làm việc với file nặng.</li>
            <li><strong>Màn hình:</strong> 14 inch 16:10 giúp xem bảng tính và tài liệu dài dễ hơn.</li>
        </ul>
        <blockquote>Nếu ngân sách có hạn, hãy ưu tiên RAM, SSD, màn hình và bàn phím trước khi nâng CPU lên dòng cao hơn.</blockquote>
        <p>[product:2:Lenovo ThinkPad T14 Business Laptop:28990000:https://images.unsplash.com/photo-1588872657578-7efd1f1555ed?w=600&auto=format&fit=crop&q=80]</p>
        <h2>Trải nghiệm nhập liệu quyết định sự hài lòng dài hạn</h2>
        <p>Người làm kế toán, vận hành, chăm sóc khách hàng hoặc quản lý đơn hàng thường gõ rất nhiều. Bàn phím có layout rõ, hành trình ổn và phím điều hướng thuận tiện sẽ giảm lỗi nhập liệu. Touchpad đủ rộng cũng giúp thao tác nhanh khi bạn không mang chuột rời.</p>
        <p>Màn hình nên có độ sáng từ 300 nits nếu bạn hay làm việc ở môi trường nhiều ánh sáng. Tấm nền 16:10 cho thêm không gian dọc, giúp đọc hợp đồng, bảng tính và trang quản trị ít phải cuộn hơn. Đây là nâng cấp tưởng nhỏ nhưng ảnh hưởng rất lớn tới cảm giác làm việc.</p>
        <h2>Pin, độ bền và dịch vụ sau bán hàng</h2>
        <p>Laptop văn phòng nên nhẹ dưới 1.5kg nếu phải di chuyển nhiều. Pin thực tế 6-8 giờ là đủ cho một ngày làm việc linh hoạt. Với doanh nghiệp nhỏ, bảo hành chính hãng và khả năng thay linh kiện nhanh còn quan trọng hơn vài điểm hiệu năng, vì thời gian chết của máy là chi phí thật.</p>
        <h2>Checklist ra quyết định nhanh</h2>
        <ol>
            <li>Chọn RAM 16GB trước, rồi mới cân nhắc nâng CPU.</li>
            <li>Ưu tiên SSD 512GB nếu lưu nhiều tài liệu và ảnh sản phẩm.</li>
            <li>Kiểm tra bàn phím trực tiếp nếu công việc nhập liệu nhiều.</li>
            <li>Chọn màn hình 16:10 nếu thường làm bảng tính hoặc đọc tài liệu dài.</li>
            <li>Mua thêm dock USB-C nếu dùng màn hình ngoài, LAN hoặc nhiều thiết bị ngoại vi.</li>
        </ol>
        <p><strong>Kết luận:</strong> laptop văn phòng đáng mua nhất không phải chiếc có cấu hình cao nhất trên giấy. Đó là chiếc máy khiến bạn làm việc nhanh, ít mỏi, ít lỗi và yên tâm dùng lâu dài.</p>
        """;

    private static string CpuGpuGuideContent() => """
        <h2>Thông số mạnh chưa chắc tạo ra laptop nhanh</h2>
        <p>Thị trường laptop hiệu năng đang có quá nhiều tên gọi: Intel Core Ultra, Ryzen AI, RTX, NPU, TGP, AI Boost. Nếu chỉ nhìn tên chip, bạn rất dễ chọn sai. Hai chiếc laptop dùng cùng CPU hoặc GPU có thể cho hiệu năng khác nhau vì hệ thống tản nhiệt, mức điện và cách hãng cấu hình firmware.</p>
        <p>Điều người mua cần không phải là thuộc hết tên mã, mà là hiểu máy sẽ chạy nhanh trong công việc thật bao lâu. Một benchmark 3 phút không nói hết trải nghiệm render 30 phút, chơi game 2 giờ hoặc vừa xuất video vừa mở nhiều tab.</p>
        <p><img src="https://images.unsplash.com/photo-1518779578993-ec3579fee39f?w=1400&auto=format&fit=crop&q=80" alt="Bảng mạch và linh kiện phần cứng laptop" /></p>
        <h2>CPU: hãy đọc cùng mức điện và thân máy</h2>
        <p>CPU laptop hiện đại có thể tăng tốc rất nhanh trong tác vụ ngắn, nhưng hiệu năng duy trì phụ thuộc vào nhiệt. Một CPU mạnh trong thân máy quá mỏng có thể giảm xung sau vài phút. Ngược lại, một CPU thấp hơn trong khung máy tản nhiệt tốt lại ổn định hơn khi chạy tác vụ dài.</p>
        <blockquote>Tên CPU cho biết tiềm năng. Thiết kế tản nhiệt mới cho biết máy có giữ được tiềm năng đó hay không.</blockquote>
        <h2>NPU: đáng chú ý, nhưng chưa phải lý do duy nhất để nâng cấp</h2>
        <p>NPU giúp xử lý một số tác vụ AI tiết kiệm điện hơn, ví dụ lọc nền camera, nhận diện giọng nói hoặc tính năng AI trong app được tối ưu. Tuy nhiên, không phải phần mềm nào cũng tận dụng NPU. Nếu công việc của bạn chủ yếu là code, văn phòng, thiết kế nhẹ hoặc gaming, CPU/GPU và RAM vẫn quan trọng hơn.</p>
        <h2>GPU tích hợp hay GPU rời?</h2>
        <p>GPU tích hợp đời mới đủ cho màn hình ngoài, chỉnh ảnh nhẹ, xem video độ phân giải cao và một số game eSports. GPU rời cần thiết khi bạn dựng video, render 3D, chạy AI local hoặc chơi game AAA. Khi xem GPU rời, đừng chỉ nhìn tên RTX 4050/4060; hãy xem cả <strong>TGP</strong>, vì cùng một GPU nhưng mức điện khác nhau có thể tạo chênh lệch hiệu năng rõ.</p>
        <p>[product:4:ASUS ROG Zephyrus G14 Gaming Laptop:45990000:https://images.unsplash.com/photo-1593642632823-8f785ba67e45?w=600&auto=format&fit=crop&q=80]</p>
        <h2>Ba chân kiềng của laptop hiệu năng</h2>
        <ul>
            <li><strong>Hiệu năng tức thời:</strong> mở app nhanh, compile nhanh, load project nhanh.</li>
            <li><strong>Hiệu năng duy trì:</strong> giữ xung ổn khi render, chơi game hoặc export video lâu.</li>
            <li><strong>Độ ồn và nhiệt:</strong> máy mạnh nhưng quá ồn có thể không phù hợp văn phòng hoặc studio nhỏ.</li>
        </ul>
        <h2>Chọn theo tình huống thực tế</h2>
        <p><strong>Văn phòng và học tập:</strong> Core Ultra 5/Ryzen 5, RAM 16GB và GPU tích hợp là đủ. <strong>Sáng tạo nội dung:</strong> ưu tiên RAM 32GB, màn hình đẹp và SSD lớn. <strong>Gaming/render:</strong> chọn GPU rời, màn hình tần số quét cao, adapter đủ công suất và thân máy tản nhiệt tốt.</p>
        <p><strong>Kết luận:</strong> laptop hiệu năng đáng mua là chiếc cân bằng giữa chip, điện, nhiệt, màn hình và tiếng ồn. Đừng mua vì một con số; hãy mua vì máy phù hợp đúng workflow của bạn.</p>
        """;
}
