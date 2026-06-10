import { AppLink } from '@/components/atoms/AppLink';
import { StaticPageLayout } from '@/components/layouts/StaticPageLayout';
import { StoreLocatorInteractive } from '@/components/store-locator/StoreLocatorInteractive';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Clock,
  Package,
  Shield,
  Star,
  Users
} from 'lucide-react';

export default function StoreLocatorPage() {
  const tableOfContents = [
    { id: 'search-stores', title: 'Tìm cửa hàng' },
    { id: 'flagship-stores', title: 'Cửa hàng flagship' },
    { id: 'regional-stores', title: 'Cửa hàng khu vực' },
    { id: 'store-services', title: 'Dịch vụ tại cửa hàng' },
    { id: 'visit-tips', title: 'Lời khuyên khi đến cửa hàng' }
  ];

  const breadcrumbs = [
    { label: 'Trang chủ', href: '/' },
    { label: 'Hệ thống cửa hàng' }
  ];

  return (
    <StaticPageLayout
      title=" Hệ Thống Cửa Hàng"
      subtitle="Khám phá 50+ cửa hàng trên toàn quốc với không gian hiện đại và đội ngũ tư vấn chuyên nghiệp"
      lastUpdated="18/09/2025"
      author="Store Operations Team"
      readTime="12"
      tableOfContents={tableOfContents}
      breadcrumbs={breadcrumbs}
    >
      <StoreLocatorContent />
    </StaticPageLayout>
  );
}

function StoreLocatorContent() {
  return (
    <div className="space-y-12">
      {/* Interactive Store Locator */}
      <StoreLocatorInteractive />

      {/* Store Services Section */}
      <section id="store-services" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Package className="w-8 h-8 mr-3 text-purple-600" />
           Dịch Vụ Tại Cửa Hàng
        </h2>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          <Card className="hover:shadow-lg transition-shadow">
            <CardHeader>
              <CardTitle className="flex items-center text-lg">
                <Users className="w-6 h-6 mr-3 text-blue-600" />
                Tư vấn chuyên sâu
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="space-y-2 text-gray-600">
                <li> Phân tích nhu cầu sử dụng chi tiết</li>
                <li> Đề xuất cấu hình phù hợp ngân sách</li>
                <li> So sánh các dòng sản phẩm</li>
                <li> Tư vấn phụ kiện đi kèm</li>
              </ul>
            </CardContent>
          </Card>

          <Card className="hover:shadow-lg transition-shadow">
            <CardHeader>
              <CardTitle className="flex items-center text-lg">
                <Star className="w-6 h-6 mr-3 text-yellow-600" />
                Test sản phẩm
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="space-y-2 text-gray-600">
                <li> Trải nghiệm thực tế trước khi mua</li>
                <li> Test hiệu năng với phần mềm chuyên dụng</li>
                <li> Kiểm tra chất lượng màn hình, bàn phím</li>
                <li> Demo các tính năng đặc biệt</li>
              </ul>
            </CardContent>
          </Card>

          <Card className="hover:shadow-lg transition-shadow">
            <CardHeader>
              <CardTitle className="flex items-center text-lg">
                <Shield className="w-6 h-6 mr-3 text-green-600" />
                Bảo hành tại chỗ
              </CardTitle>
            </CardHeader>
            <CardContent>
              <ul className="space-y-2 text-gray-600">
                <li> Kiểm tra và sửa chữa ngay tại cửa hàng</li>
                <li> Thay thế linh kiện chính hãng</li>
                <li> Backup dữ liệu trước sửa chữa</li>
                <li> Bảo hành mở rộng có phí</li>
              </ul>
            </CardContent>
          </Card>
        </div>
      </section>

      {/* Visit Tips Section */}
      <section id="visit-tips" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Clock className="w-8 h-8 mr-3 text-orange-600" />
           Lời Khuyên Khi Đến Cửa Hàng
        </h2>

        <div className="bg-orange-50 rounded-xl p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div>
              <h3 className="text-xl font-bold text-orange-900 mb-4">
                 Trước khi đến
              </h3>
              <ul className="space-y-3 text-orange-800">
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Gọi điện đặt lịch:</strong><br />
                    <span className="text-sm">Đảm bảo có nhân viên tư vấn sẵn sàng</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Chuẩn bị ngân sách:</strong><br />
                    <span className="text-sm">Có khung giá rõ ràng để tư vấn chính xác</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Liệt kê nhu cầu:</strong><br />
                    <span className="text-sm">Gaming, văn phòng, đồ họa, lập trình...</span>
                  </div>
                </li>
              </ul>
            </div>

            <div>
              <h3 className="text-xl font-bold text-orange-900 mb-4">
                 Khi đến cửa hàng
              </h3>
              <ul className="space-y-3 text-orange-800">
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Mang theo giấy tờ:</strong><br />
                    <span className="text-sm">CMND/CCCD để làm thủ tục mua hàng</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Test kỹ sản phẩm:</strong><br />
                    <span className="text-sm">Kiểm tra tất cả các cổng, phím, touchpad</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Hỏi về promotion:</strong><br />
                    <span className="text-sm">Các ưu đãi hiện tại, quà tặng kèm</span>
                  </div>
                </li>
              </ul>
            </div>
          </div>

          <div className="mt-8 p-4 bg-white rounded-lg">
            <h4 className="font-bold text-orange-900 mb-3 text-center">
               Checklist mua laptop hoàn hảo
            </h4>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="space-y-2">
                <h5 className="font-semibold text-orange-900">Hardware</h5>
                <ul className="text-sm text-orange-700 space-y-1">
                   <li> CPU phù hợp với công việc</li>
                   <li> RAM đủ cho multitasking</li>
                   <li> Storage SSD cho tốc độ</li>
                   <li> GPU phù hợp với đồ họa</li>
                </ul>
              </div>
              <div className="space-y-2">
                <h5 className="font-semibold text-orange-900">Ngoại hình</h5>
                <ul className="text-sm text-orange-700 space-y-1">
                  <li> Kích thước phù hợp</li>
                  <li> Trọng lượng mang vác</li>
                  <li> Chất lượng build solid</li>
                  <li> Bàn phím comfortable</li>
                </ul>
              </div>
              <div className="space-y-2">
                <h5 className="font-semibold text-orange-900">Dịch vụ</h5>
                <ul className="text-sm text-orange-700 space-y-1">
                  <li> Chính sách bảo hành</li>
                  <li> Giá software kèm theo</li>
                  <li> Hỗ trợ sau bán hàng</li>
                  <li> Trade-in laptop cũ</li>
                </ul>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Store Hours & Contact */}
      <section className="content-section">
        <div className="bg-gray-50 rounded-xl p-6">
          <h3 className="text-xl font-bold text-gray-900 mb-4 text-center">
              Thông Tin Liên Hệ & Giờ Mở Cửa
          </h3>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="text-center p-4 bg-white rounded-lg">
              <div className="text-3xl mb-2"></div>
              <h4 className="font-semibold text-gray-900 mb-2">Giờ mở cửa</h4>
              <p className="text-gray-600">
                Thứ 2 - Chủ nhật<br />
                8:00 - 22:00
              </p>
            </div>

            <div className="text-center p-4 bg-white rounded-lg">
              <div className="text-3xl mb-2"></div>
              <h4 className="font-semibold text-gray-900 mb-2">Hotline</h4>
              <p className="text-gray-600">
                1900-1234 (min ph)<br />
                H tr 24/7
              </p>
            </div>

            <div className="text-center p-4 bg-white rounded-lg">
              <div className="text-3xl mb-2"></div>
              <h4 className="font-semibold text-gray-900 mb-2">Chat hỗ trợ</h4>
              <p className="text-gray-600">
                Zalo, Facebook, Website<br />
                Phản hồi trong 5 phút
              </p>
            </div>
          </div>

          <div className="text-center mt-6">
            <AppLink
              href="/contact"
              pageType="static"
              className="inline-block bg-blue-600 hover:bg-blue-700 text-white px-8 py-3 rounded-lg font-semibold transition-colors"
            >
               Gửi yêu cầu hỗ trợ
            </AppLink>
          </div>
        </div>
      </section>
    </div>
  );
}