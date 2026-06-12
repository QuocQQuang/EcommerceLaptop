using Microsoft.EntityFrameworkCore;
using EcommerceLaptop.Core.Entities;
using EcommerceLaptop.Infrastructure.Data;

namespace ReviewDataSeeder;

public class ReviewSeeder
{
    private readonly ApplicationDbContext _context;

    public ReviewSeeder(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task SeedReviewsAsync()
    {
        // Kiểm tra xem đã có reviews chưa
        if (await _context.Reviews.AnyAsync())
        {
            Console.WriteLine("Reviews đã tồn tại. Bỏ qua seeding.");
            return;
        }

        // Lấy tất cả users và products
        var users = await _context.Users.ToListAsync();
        var products = await _context.Products.ToListAsync();

        if (!users.Any() || !products.Any())
        {
            Console.WriteLine("Không có users hoặc products. Vui lòng seed users và products trước.");
            return;
        }

        var reviews = new List<Review>();
        var random = new Random();

        // Danh sách tiêu đề reviews tiếng Việt
        var reviewTitles = new[]
        {
            "Sản phẩm tuyệt vời!",
            "Rất hài lòng với chất lượng",
            "Đáng đồng tiền bát gạo",
            "Chất lượng vượt mong đợi",
            "Giao hàng nhanh, đóng gói cẩn thận",
            "Sử dụng mượt mà, hiệu năng tốt",
            "Thiết kế đẹp, chất lượng cao",
            "Giá cả hợp lý, chất lượng tốt",
            "Laptop chạy rất nhanh",
            "Pin bền, màn hình đẹp",
            "Cấu hình mạnh, phù hợp gaming",
            "Phù hợp cho công việc văn phòng",
            "Bàn phím êm, trackpad nhạy",
            "Thiết kế gọn nhẹ, dễ mang theo",
            "Âm thanh hay, màn hình sắc nét",
            "Khởi động nhanh, không lag",
            "Tản nhiệt tốt, không nóng máy",
            "Cổng kết nối đa dạng",
            "Webcam chất lượng tốt",
            "Phù hợp cho học sinh, sinh viên",
            "Chơi game mượt mà",
            "Render video nhanh chóng",
            "Màn hình sáng, màu sắc chân thực",
            "Build quality tốt",
            "Giá trị tốt so với tiền bỏ ra",
            "Sản phẩm chính hãng, uy tín",
            "Hỗ trợ khách hàng tốt",
            "Đóng gói chuyên nghiệp",
            "Giao hàng đúng hẹn",
            "Sản phẩm như mô tả"
        };

        // Danh sách comment tiếng Việt theo rating
        var positiveComments = new[]
        {
            "Mình rất hài lòng với sản phẩm này. Chất lượng tốt, hiệu năng mạnh mẽ. Đặc biệt là màn hình rất đẹp và sắc nét. Giao hàng nhanh chóng, đóng gói cẩn thận. Sẽ tiếp tục ủng hộ shop!",
            "Laptop này vượt xa mong đợi của mình. Cấu hình mạnh, chạy các phần mềm nặng rất mượt. Thiết kế đẹp, sang trọng. Pin cũng khá bền, dùng được cả ngày. Rất đáng tiền!",
            "Sử dụng được 2 tháng rồi, laptop vẫn hoạt động rất tốt. Khởi động nhanh, không bị lag. Bàn phím gõ rất êm, trackpad nhạy. Màn hình sáng, màu sắc chân thực. Recommend!",
            "Chất lượng build rất tốt, không có tiếng kêu lạ. Tản nhiệt hiệu quả, máy không bị nóng khi sử dụng lâu. Cổng kết nối đa dạng, tiện lợi. Webcam HD chất lượng tốt cho họp online.",
            "Mua để làm việc và chơi game, đều rất hài lòng. Render video nhanh, chơi game không bị giật. Âm thanh trong trẻo. Thiết kế gọn nhẹ, dễ mang theo. Service hỗ trợ nhiệt tình.",
            "Laptop tuyệt vời cho sinh viên như mình. Đa nhiệm tốt, mở nhiều tab Chrome không lag. Office chạy mượt mà. Pin kéo dài 6-7 tiếng sử dụng. Giá cả phải chăng, chất lượng tốt.",
            "Đã sử dụng 6 tháng, laptop vẫn như mới. Không có lỗi gì, hoạt động ổn định. Bàn phím có đèn nền tiện lợi. Màn hình IPS góc nhìn rộng. Cảm ơn shop đã tư vấn sản phẩm phù hợp!",
            "Gaming laptop tuyệt vời! Chơi các game AAA đều mượt mà ở setting cao. Card đồ họa mạnh, không bị drop fps. Tản nhiệt tốt, nhiệt độ ổn định. Bàn phím có học typing experience tuyệt vời.",
            "Laptop dành cho designer như mình thật quá tuyệt. Màn hình 4K sắc nét, màu sắc chuẩn xác. Render Photoshop, Premiere Pro rất nhanh. RAM 32GB đa nhiệm không giới hạn. Đáng đầu tư!",
            "Business laptop hoàn hảo. Thiết kế professional, chất liệu cao cấp. Bảo mật tốt với fingerprint và face unlock. Webcam IR chất lượng. Battery life cả ngày làm việc. Rất hài lòng!"
        };

        var neutralComments = new[]
        {
            "Sản phẩm tàm tạm, đáp ứng được nhu cầu cơ bản. Có một số điểm chưa hoàn hảo nhưng chấp nhận được với mức giá này. Giao hàng đúng hẹn, đóng gói cẩn thận.",
            "Laptop chạy tốt các tác vụ thông thường. Màn hình tàm tạm, độ sáng vừa phải. Pin kéo dài khoảng 4-5 tiếng. Có thể cải thiện thêm về âm thanh và webcam.",
            "Chất lượng tương xứng với giá tiền. Có một vài điểm cần cải thiện như tản nhiệt và độ ồn quạt. Nhìn chung là okay cho nhu cầu sử dụng cơ bản của mình.",
            "Sử dụng được vài tuần, cảm nhận chung là bình thường. Hiệu năng ổn cho văn phòng nhưng chưa mạnh lắm. Thiết kế trung bình, chưa có gì đặc biệt.",
            "Laptop này tạm được, đáp ứng 70% nhu cầu của mình. Một số phần mềm chạy hơi chậm. Build quality tàm tạm, có thể tốt hơn ở mức giá này.",
            "Sản phẩm như mô tả, không có gì bất ngờ. Chạy mượt các tác vụ cơ bản. Có điểm trừ về thời lượng pin và độ sáng màn hình. Nhìn chung là acceptable.",
            "Dùng tạm được cho công việc hàng ngày. Khởi động hơi chậm, cần upgrade SSD. Màn hình màu sắc tàm tạm. Bàn phím hơi cứng, cần thời gian làm quen."
        };

        var negativeComments = new[]
        {
            "Laptop có vấn đề về tản nhiệt, máy nóng khi sử dụng lâu. Quạt kêu ồn khá nhiều. Pin yếu, chỉ kéo dài được 2-3 tiếng. Chưa hài lòng lắm với sản phẩm này.",
            "Chất lượng build chưa tốt, có tiếng kêu khi mở máy. Màn hình hơi tối, phải chỉnh độ sáng cao. Bàn phím một số phím bị dính. Cần shop hỗ trợ bảo hành.",
            "Hiệu năng không như quảng cáo, chạy chậm hơn mong đợi. Một số phần mềm bị crash thường xuyên. Trackpad không nhạy lắm. Cần cải thiện driver và firmware.",
            "Giao hàng chậm, sản phẩm có một vài vết xước nhỏ. Setup khó khăn, phải tự cài đặt nhiều thứ. Webcam chất lượng kém, hình ảnh mờ. Chưa thật sự hài lòng.",
            "Laptop bị lag khi mở nhiều ứng dụng. RAM nhỏ không đủ dùng 8GB. Storage hết chỗ nhanh. Cần upgrade để sử dụng tốt hơn. Giá hơi cao so với hiệu năng."
        };

        Console.WriteLine($"Bắt đầu seed {100} reviews...");

        for (int i = 0; i < 100; i++)
        {
            var user = users[random.Next(users.Count)];
            var product = products[random.Next(products.Count)];


            // Tạo distribution rating thực tế (nhiều rating cao hơn)
            int rating;
            var ratingRand = random.NextDouble();
            if (ratingRand < 0.4) rating = 5;      // 40%
            else if (ratingRand < 0.7) rating = 4; // 30%  
            else if (ratingRand < 0.85) rating = 3; // 15%
            else if (ratingRand < 0.95) rating = 2; // 10%
            else rating = 1;                       // 5%

            string title = reviewTitles[random.Next(reviewTitles.Length)];
            string comment;

            // Chọn comment phù hợp với rating
            if (rating >= 4)
            {
                comment = positiveComments[random.Next(positiveComments.Length)];
            }
            else if (rating == 3)
            {
                comment = neutralComments[random.Next(neutralComments.Length)];
            }
            else
            {
                comment = negativeComments[random.Next(negativeComments.Length)];
            }

            var review = new Review
            {
                ProductId = product.Id,
                UserId = user.Id,
                Rating = rating,
                Title = title,
                Comment = comment,
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(365)),
                IsVerifiedPurchase = random.NextDouble() > 0.3
            };

            reviews.Add(review);

            if ((i + 1) % 20 == 0)
            {
                Console.WriteLine($"Đã tạo {i + 1}/100 reviews...");
            }
        }

        await _context.Reviews.AddRangeAsync(reviews);
        await _context.SaveChangesAsync();

        Console.WriteLine($"=== Đã seed thành công {reviews.Count} reviews! ===");

        // Thống kê
        var stats = reviews.GroupBy(r => r.Rating)
                          .Select(g => new { Rating = g.Key, Count = g.Count() })
                          .OrderByDescending(x => x.Rating);

        Console.WriteLine("\nThống kê reviews đã seed:");
        foreach (var stat in stats)
        {
            Console.WriteLine($"  {stat.Rating} sao: {stat.Count} reviews");
        }
    }
}
