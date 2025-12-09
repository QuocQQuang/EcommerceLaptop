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
        // Kim tra xem  c reviews cha
        if (await _context.Reviews.AnyAsync())
        {
            Console.WriteLine("Reviews  tn ti. B qua seeding.");
            return;
        }

        // Ly tt c users v products
        var users = await _context.Users.ToListAsync();
        var products = await _context.Products.ToListAsync();

        if (!users.Any() || !products.Any())
        {
            Console.WriteLine("Khng c users hoc products. Vui lng seed users v products trc.");
            return;
        }

        var reviews = new List<Review>();
        var random = new Random();

        // Danh sch tiu  reviews ting Vit
        var reviewTitles = new[]
        {
            "Sn phm tuyt vi!",
            "Rt hi lng vi cht lng",
            "ng ng tin bt go",
            "Cht lng vt mong i",
            "Giao hng nhanh, ng gi cn thn",
            "S dng mt m, hiu nng tt",
            "Thit k p, cht lng cao",
            "Gi c hp l, cht lng tt",
            "Laptop chy rt nhanh",
            "Pin bn, mn hnh p",
            "Cu hnh mnh, ph hp gaming",
            "Ph hp cho cng vic vn phng",
            "Bn phm m, trackpad nhy",
            "Thit k gn nh, d mang theo",
            "m thanh hay, mn hnh sc nt",
            "Khi ng nhanh, khng lag",
            "Tn nhit tt, khng nng my",
            "Cng kt ni a dng",
            "Webcam cht lng tt",
            "Ph hp cho hc sinh, sinh vin",
            "Chi game mt m",
            "Render video nhanh chng",
            "Mn hnh sng, mu sc chn thc",
            "Build quality tt",
            "Gi tr tt so vi tin b ra",
            "Sn phm chnh hng, uy tn",
            "H tr khch hng tt",
            "ng gi chuyn nghip",
            "Giao hng ng hn",
            "Sn phm nh m t"
        };

        // Danh sch comment ting Vit theo rating
        var positiveComments = new[]
        {
            "Mnh rt hi lng vi sn phm ny. Cht lng tt, hiu nng mnh m. c bit l mn hnh rt p v sc nt. Giao hng nhanh chng, ng gi cn thn. S tip tc ng h shop!",
            "Laptop ny vt xa mong i ca mnh. Cu hnh mnh, chy cc phn mm nng rt mt. Thit k p, sang trng. Pin cng kh bn, dng c c ngy. Rt ng tin!",
            "S dng c 2 thng ri, laptop vn hot ng rt tt. Khi ng nhanh, khng b lag. Bn phm g rt m, trackpad nhy. Mn hnh sng, mu sc chn thc. Recomment!",
            "Cht lng build rt tt, khng c ting ku l. Tn nhit hiu qu, my khng b nng khi s dng lu. Cng kt ni a dng, tin li. Webcam HD cht lng tt cho hp online.",
            "Mua  lm vic v chi game, u rt hi lng. Render video nhanh, chi game khng b git. m thanh trong tro. Thit k gn nh, d mang theo. Service h tr nhit tnh.",
            "Laptop tuyt vi cho sinh vin nh mnh. a nhim tt, m nhiu tab Chrome khng lag. Office chy mt m. Pin ko di 6-7 ting s dng. Gi c phi chng, cht lng tt.",
            " s dng 6 thng, laptop vn nh mi. Khng c li g, hot ng n nh. Bn phm c n nn tin li. Mn hnh IPS gc nhn rng. Cm n shop  t vn sn phm ph hp!",
            "Gaming laptop tuyt vi! Chi cc game AAA u mt m  setting cao. Card  ha mnh, khng b drop fps. Tn nhit tt, nhit  n nh. Bn phm c hc typing experience tuyt vi.",
            "Laptop dnh cho designer nh mnh th qu tuyt. Mn hnh 4K sc nt, mu sc chun xc. Render Photoshop, Premiere Pro rt nhanh. RAM 32GB a nhim khng gii hn. ng u t!",
            "Business laptop hon ho. Thit k professional, cht liu cao cp. Bo mt tt vi fingerprint v face unlock. Webcam IR cht lng. Battery life c ngy lm vic. Rt hi lng!"
        };

        var neutralComments = new[]
        {
            "Sn phm tm n, p ng c nhu cu c bn. C mt s im cha hon ho nhng chp nhn c vi mc gi ny. Giao hng ng hn, ng gi cn thn.",
            "Laptop chy tt cc tc v thng thng. Mn hnh tm n,  sng va phi. Pin ko di khong 4-5 ting. C th ci thin thm v m thanh v webcam.",
            "Cht lng tng xng vi gi tin. C mt vi im cn ci thin nh tn nhit v  n qut. Nhn chung l okay cho nhu cu s dng c bn ca mnh.",
            "S dng c vi tun, cm nhn chung l bnh thng. Hiu nng n cho vn phng nhng cha mnh lm. Thit k trung bnh, cha c g c bit.",
            "Laptop ny tm c, p ng 70% nhu cu ca mnh. Mt s phn mm chy hi chm. Build quality tm n, c th tt hn  mc gi ny.",
            "Sn phm nh m t, khng c g bt ng. Chy mt cc tc v c bn. C im tr v thi lng pin v  sng mn hnh. Nhn chung l acceptable.",
            "Dng tm c cho cng vic hng ngy. Khi ng hi chm, cn upgrade SSD. Mn hnh mu sc tm n. Bn phm hi cng, cn thi gian lm quen."
        };

        var negativeComments = new[]
        {
            "Laptop c vn  v tn nhit, my nng khi s dng lu. Qut ku n kh nhiu. Pin yu, ch ko di c 2-3 ting. Cha hi lng lm vi sn phm ny.",
            "Cht lng build cha tt, c ting ku khi m my. Mn hnh hi ti, phi chnh  sng cao. Bn phm mt s phm b dnh. Cn shop h tr bo hnh.",
            "Hiu nng khng nh qung co, chy chm hn mong i. Mt s phn mm b crash thng xuyn. Trackpad khng nhy lm. Cn ci thin driver v firmware.",
            "Giao hng chm, sn phm c mt vi xc nh. Setup kh khn, phi t ci t nhiu th. Webcam cht lng km, hnh nh m. Cha tht s hi lng.",
            "Laptop b lag khi m nhiu ng dng. RAM nh khng  d  8GB. Storage ht ch nhanh. Cn upgrade  s dng tt hn. Gi hi cao so vi hiu nng."
        };

        Console.WriteLine($"Bt u seed {100} reviews...");

        for (int i = 0; i < 100; i++)
        {
            var user = users[random.Next(users.Count)];
            var product = products[random.Next(products.Count)];


            // To distribution rating thc t (nhiu rating cao hn)
            int rating;
            var ratingRand = random.NextDouble();
            if (ratingRand < 0.4) rating = 5;      // 40%
            else if (ratingRand < 0.7) rating = 4; // 30%  
            else if (ratingRand < 0.85) rating = 3; // 15%
            else if (ratingRand < 0.95) rating = 2; // 10%
            else rating = 1;                       // 5%

            string title = reviewTitles[random.Next(reviewTitles.Length)];
            string comment;

            // Chn comment ph hp vi rating
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
                CreatedAt = DateTime.UtcNow.AddDays(-random.Next(365)), // Reviews trong nm qua
                IsVerifiedPurchase = random.NextDouble() > 0.3 // 70% l verified purchase
            };

            reviews.Add(review);

            if ((i + 1) % 20 == 0)
            {
                Console.WriteLine($" to {i + 1}/100 reviews...");
            }
        }

        await _context.Reviews.AddRangeAsync(reviews);
        await _context.SaveChangesAsync();

        Console.WriteLine($"  seed thnh cng {reviews.Count} reviews!");

        // Thng k
        var stats = reviews.GroupBy(r => r.Rating)
                          .Select(g => new { Rating = g.Key, Count = g.Count() })
                          .OrderByDescending(x => x.Rating);

        Console.WriteLine("\n Thng k reviews  seed:");
        foreach (var stat in stats)
        {
            Console.WriteLine($" {stat.Rating} sao: {stat.Count} reviews");
        }
    }
}