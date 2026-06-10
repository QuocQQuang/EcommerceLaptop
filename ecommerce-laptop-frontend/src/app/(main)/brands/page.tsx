import { AppLink } from '@/components/atoms/AppLink';
import { StaticPageLayout } from '@/components/layouts/StaticPageLayout';
import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Star } from 'lucide-react';
import Image from 'next/image';

export default function BrandsPage() {
  const tableOfContents = [
    { id: 'premium-brands', title: 'Thương hiệu cao cấp' },
    { id: 'business-brands', title: 'Thương hiệu doanh nghiệp' },
    { id: 'gaming-brands', title: 'Thương hiệu gaming' },
    { id: 'budget-brands', title: 'Thương hiệu phổ thông' },
    { id: 'why-choose', title: 'Tại sao chọn chúng tôi' }
  ];

  const breadcrumbs = [
    { label: 'Trang chủ', href: '/' },
    { label: 'Thương hiệu' }
  ];

  return (
    <StaticPageLayout
      title="Thương Hiệu Laptop Uy Tín"
      subtitle="Khám phá các thương hiệu laptop hàng đầu thế giới với chất lượng được chứng minh"
      lastUpdated="18/09/2025"
      author="Team LaptopStore"
      readTime="5"
      tableOfContents={tableOfContents}
      breadcrumbs={breadcrumbs}
    >
      <BrandsContent />
    </StaticPageLayout>
  );
}

function BrandsContent() {
  const premiumBrands = [
    {
      name: 'Apple',
      logo: '/images/brands/apple.png',
      description: 'Thiết kế đẳng cấp, hiệu năng vượt trội với chip M-series',
      rating: 4.8,
      products: 15,
      highlights: ['MacBook Air M3', 'MacBook Pro M3 Max', 'iMac 24"']
    },
    {
      name: 'Dell',
      logo: '/images/brands/dell.png',
      description: 'Độ tin cậy cho doanh nghiệp và sáng tạo chuyên nghiệp',
      rating: 4.6,
      products: 42,
      highlights: ['XPS 13 Plus', 'Alienware m16', 'Inspiron 15 3000']
    },
    {
      name: 'HP',
      logo: '/images/brands/hp.png',
      description: 'Đa dạng dòng sản phẩm từ văn phòng đến gaming',
      rating: 4.5,
      products: 38,
      highlights: ['Spectre x360', 'Omen Gaming', 'Pavilion']
    }
  ];

  const businessBrands = [
    {
      name: 'Lenovo',
      logo: '/images/brands/lenovo.png',
      description: 'ThinkPad huyền thoại và Legion gaming mạnh mẽ',
      rating: 4.7,
      products: 35,
      highlights: ['ThinkPad X1 Carbon', 'Legion Pro 7', 'IdeaPad']
    },
    {
      name: 'Asus',
      logo: '/images/brands/asus.png',
      description: 'Cân bằng hoàn hảo giữa hiệu năng và giá cả',
      rating: 4.4,
      products: 28,
      highlights: ['ZenBook Pro', 'ROG Strix', 'VivoBook']
    }
  ];

  const gamingBrands = [
    {
      name: 'MSI',
      logo: '/images/brands/msi.png',
      description: 'Chuyên gia laptop gaming với RGB đẹp mắt',
      rating: 4.6,
      products: 22,
      highlights: ['GE78 Raider', 'Katana 15', 'Creator Z16']
    },
    {
      name: 'Acer',
      logo: '/images/brands/acer.png',
      description: 'Predator gaming và Swift siêu mỏng',
      rating: 4.3,
      products: 25,
      highlights: ['Predator Helios', 'Swift X', 'Aspire 5']
    }
  ];

  return (
    <div className="space-y-12">
      {/* Premium Brands Section */}
      <section id="premium-brands" className="content-section">
          <h2 className="text-3xl font-bold text-gray-900 mb-6">
            Thương Hiệu Cao Cấp
          </h2>
          <p className="text-gray-600 mb-8 leading-7">
            Những thương hiệu dẫn đầu thế giới với công nghệ tiên tiến nhất,
            thiết kế đẳng cấp và chất lượng được công nhận toàn cầu.
          </p>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {premiumBrands.map((brand) => (
            <BrandCard key={brand.name} brand={brand} />
          ))}
        </div>
      </section>

      {/* Business Brands Section */}
      <section id="business-brands" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6">
            Thương Hiệu Doanh Nghiệp
          </h2>
          <p className="text-gray-600 mb-8 leading-7">
            Tối ưu cho công việc chuyên nghiệp với độ bền cao,
            bảo mật tốt và hỗ trợ doanh nghiệp toàn diện.
          </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {businessBrands.map((brand) => (
            <BrandCard key={brand.name} brand={brand} />
          ))}
        </div>
      </section>

      {/* Gaming Brands Section */}
      <section id="gaming-brands" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6">
            Thương Hiệu Gaming
          </h2>
          <p className="text-gray-600 mb-8 leading-7">
            Hiệu năng đỉnh cao cho game thủ với card đồ họa mạnh mẽ,
            tản nhiệt tối ưu và thiết kế gaming đặc trưng.
          </p>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          {gamingBrands.map((brand) => (
            <BrandCard key={brand.name} brand={brand} />
          ))}
        </div>
      </section>

      {/* Why Choose Section */}
      <section id="why-choose" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6">
            Tại Sao Chọn LaptopStore?
          </h2>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          <div className="space-y-6">
            <div className="flex items-start space-x-4">
              <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center flex-shrink-0">
                <span className="text-blue-600 font-bold">1</span>
              </div>
              <div>
                <h3 className="font-semibold text-gray-900 mb-2">Chính Hãng 100%</h3>
                <p className="text-gray-600">
                  Tất cả sản phẩm đều là hàng chính hãng, có tem phiếu đầy đủ
                  và được bảo hành theo chính sách của hàng.
                </p>
              </div>
            </div>

            <div className="flex items-start space-x-4">
              <div className="w-8 h-8 bg-green-100 rounded-full flex items-center justify-center flex-shrink-0">
                <span className="text-green-600 font-bold">2</span>
              </div>
              <div>
                <h3 className="font-semibold text-gray-900 mb-2">Giá Tốt Nhất</h3>
                <p className="text-gray-600">
                  Cam kết giá tốt nhất thị trường với chế độ hoàn tiền
                  nếu tìm thấy giá rẻ hơn ở nơi khác.
                </p>
              </div>
            </div>
          </div>

          <div className="space-y-6">
            <div className="flex items-start space-x-4">
              <div className="w-8 h-8 bg-purple-100 rounded-full flex items-center justify-center flex-shrink-0">
                <span className="text-purple-600 font-bold">3</span>
              </div>
              <div>
                <h3 className="font-semibold text-gray-900 mb-2">Tư Vấn Chuyên Sâu</h3>
                <p className="text-gray-600">
                  Đội ngũ chuyên gia tư vấn miễn phí để giúp bạn chọn
                  laptop phù hợp với nhu cầu và ngân sách.
                </p>
              </div>
            </div>

            <div className="flex items-start space-x-4">
              <div className="w-8 h-8 bg-orange-100 rounded-full flex items-center justify-center flex-shrink-0">
                <span className="text-orange-600 font-bold">4</span>
              </div>
              <div>
                <h3 className="font-semibold text-gray-900 mb-2">Hỗ Trợ Sau Bán</h3>
                <p className="text-gray-600">
                  Dịch vụ hỗ trợ 24/7, bảo hành nhanh chóng và
                  chăm sóc khách hàng tận tình.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}

function BrandCard({ brand }: { brand: any }) {
  return (
    <Card className="hover:shadow-lg transition-shadow duration-200">
      <CardHeader className="text-center">
        <div className="w-20 h-20 mx-auto mb-4 bg-gray-100 rounded-lg flex items-center justify-center">
          <Image
            src={brand.logo}
            alt={`${brand.name} logo`}
            width={60}
            height={60}
            className="object-contain"
          />
        </div>
        <CardTitle className="text-xl font-bold">{brand.name}</CardTitle>
        <div className="flex items-center justify-center space-x-2">
          <div className="flex items-center">
            <Star className="w-4 h-4 text-yellow-400 fill-current" />
            <span className="text-sm font-medium ml-1">{brand.rating}</span>
          </div>
          <Badge variant="secondary">{brand.products} sản phẩm</Badge>
        </div>
      </CardHeader>

      <CardContent>
        <p className="text-gray-600 mb-4 text-center leading-6">
          {brand.description}
        </p>

        <div className="space-y-2 mb-4">
          <h4 className="font-medium text-sm text-gray-900">Sản phẩm nổi bật:</h4>
          <ul className="text-sm text-gray-600 space-y-1">
            {brand.highlights.map((product: string, index: number) => (
              <li key={index} className="flex items-center">
                <span className="w-1.5 h-1.5 bg-blue-500 rounded-full mr-2"></span>
                {product}
              </li>
            ))}
          </ul>
        </div>

        <AppLink
          href={`/products?brand=${brand.name.toLowerCase()}`}
          pageType="static"
          className="block w-full text-center bg-blue-600 hover:bg-blue-700 text-white py-2 px-4 rounded-md transition-colors font-medium"
        >
          Xem sản phẩm {brand.name}
        </AppLink>
      </CardContent>
    </Card>
  );
}