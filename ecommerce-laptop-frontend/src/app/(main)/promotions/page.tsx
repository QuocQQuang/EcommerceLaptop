import { AppLink } from '@/components/atoms/AppLink';
import { StaticPageLayout } from '@/components/layouts/StaticPageLayout';
import { PromotionInteractiveContent } from '@/components/promotions/PromotionInteractiveContent';
import { Badge } from '@/components/ui/badge';
import { Gift, Percent, Tag } from 'lucide-react';

export default function PromotionsPage() {
  const tableOfContents = [
    { id: 'flash-sale', title: 'Flash Sale - Giá sốc 24h' },
    { id: 'monthly-deals', title: 'Ưu đãi tháng 9' },
    { id: 'student-discount', title: 'Ưu đãi sinh viên' },
    { id: 'trade-in', title: 'Thu cũ đổi mới' },
    { id: 'bulk-discount', title: 'Mua số lượng lớn' },
    { id: 'voucher-codes', title: 'Mã giảm giá' }
  ];

  const breadcrumbs = [
    { label: 'Trang chủ', href: '/' },
    { label: 'Khuyến mãi' }
  ];

  return (
    <StaticPageLayout
      title=" Khuyến Mãi & Ưu Đãi Đặc Biệt"
      subtitle="Cập nhật liên tục các chương trình khuyến mãi hấp dẫn, tiết kiệm tối đa cho bạn"
      lastUpdated="18/09/2025"
      author="Team Marketing"
      readTime="10"
      tableOfContents={tableOfContents}
      breadcrumbs={breadcrumbs}
    >
      <PromotionContent />
    </StaticPageLayout>
  );
}

function PromotionContent() {
  return (
    <div className="space-y-12">
      {/* Interactive Content */}
      <PromotionInteractiveContent />

      {/* Monthly Deals Section */}
      <section id="monthly-deals" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Gift className="w-8 h-8 mr-3 text-blue-600" />
           Ưu Đãi Tháng 9/2025
        </h2>

        <div className="bg-blue-50 rounded-xl p-6 mb-8">
          <div className="text-center mb-6">
            <h3 className="text-2xl font-bold text-blue-900 mb-2">
              Tháng khuyến mãi lớn nhất trong năm
            </h3>
            <p className="text-blue-700">
              Giảm giá lên đến 40% cho tất cả dòng laptop cao cấp
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="bg-white p-6 rounded-lg text-center">
              <div className="text-4xl mb-4"></div>
              <h4 className="font-bold text-blue-900 mb-2">Laptop văn phòng</h4>
              <div className="text-2xl font-bold text-blue-600 mb-2">Giảm 25%</div>
              <p className="text-sm text-blue-700">Áp dụng cho tất cả laptop văn phòng</p>
            </div>

            <div className="bg-white p-6 rounded-lg text-center">
              <div className="text-4xl mb-4"></div>
              <h4 className="font-bold text-blue-900 mb-2">Laptop gaming</h4>
              <div className="text-2xl font-bold text-blue-600 mb-2">Giảm 30%</div>
              <p className="text-sm text-blue-700">Kèm phụ kiện gaming miễn phí</p>
            </div>

            <div className="bg-white p-6 rounded-lg text-center">
              <div className="text-4xl mb-4"></div>
              <h4 className="font-bold text-blue-900 mb-2">Laptop đồ họa</h4>
              <div className="text-2xl font-bold text-blue-600 mb-2">Giảm 35%</div>
              <p className="text-sm text-blue-700">Tặng kèm phần mềm thiết kế</p>
            </div>
          </div>
        </div>
      </section>

      {/* Student Discount Section */}
      <section id="student-discount" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Percent className="w-8 h-8 mr-3 text-green-600" />
           Ưu Đãi Sinh Viên
        </h2>

        <div className="bg-green-50 rounded-xl p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div>
              <h3 className="text-2xl font-bold text-green-900 mb-4">
                Giảm 30% cho sinh viên
              </h3>
              <ul className="space-y-3 text-green-800">
                <li className="flex items-center">
                  <span className="w-2 h-2 bg-green-500 rounded-full mr-3"></span>
                  Áp dụng cho tất cả laptop dưới 25 triệu
                </li>
                <li className="flex items-center">
                  <span className="w-2 h-2 bg-green-500 rounded-full mr-3"></span>
                  Tặng kèm balo laptop + chuột không dây
                </li>
                <li className="flex items-center">
                  <span className="w-2 h-2 bg-green-500 rounded-full mr-3"></span>
                  Bảo hành mở rộng 36 tháng
                </li>
                <li className="flex items-center">
                  <span className="w-2 h-2 bg-green-500 rounded-full mr-3"></span>
                  Hỗ trợ trả góp 0% lãi suất
                </li>
              </ul>

              <div className="mt-6 p-4 bg-white rounded-lg">
                <h4 className="font-bold text-green-900 mb-2"> Điều kiện áp dụng:</h4>
                <ul className="text-sm text-green-700 space-y-1">
                  <li> Xuất trình thẻ sinh viên hoặc giấy xác nhận</li>
                  <li> Áp dụng cho học sinh, sinh viên từ 16-25 tuổi</li>
                  <li> Mỗi thẻ sinh viên chỉ mua được 1 máy/năm</li>
                  <li> Không áp dụng cùng với chương trình khác</li>
                </ul>
              </div>
            </div>

            <div className="text-center">
              <div className="bg-white p-8 rounded-xl shadow-md">
                <div className="text-6xl mb-4"></div>
                <h3 className="text-xl font-bold text-green-900 mb-4">
                  Đăng ký nhận ưu đãi
                </h3>
                <p className="text-green-700 mb-6">
                  Xác thực tài khoản sinh viên để nhận mã giảm giá độc quyền
                </p>
                <AppLink
                  href="/student-verification"
                  pageType="static"
                  className="inline-block bg-green-600 hover:bg-green-700 text-white px-6 py-3 rounded-lg font-semibold transition-colors"
                >
                  Xác thực ngay
                </AppLink>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Trade-in Section */}
      <section id="trade-in" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Tag className="w-8 h-8 mr-3 text-orange-600" />
           Thu Cũ Đổi Mới
        </h2>

        <div className="bg-orange-50 rounded-xl p-6">
          <div className="text-center mb-8">
            <h3 className="text-2xl font-bold text-orange-900 mb-2">
              Định giá laptop cũ - Nhận tiền mặt ngay
            </h3>
            <p className="text-orange-700">
              Giá thu cũ cao nhất thị trường, quy trình nhanh chóng chỉ 15 phút
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-4 gap-6 mb-8">
            <div className="text-center">
              <div className="w-16 h-16 bg-orange-200 rounded-full flex items-center justify-center mx-auto mb-3">
                <span className="text-2xl"></span>
              </div>
              <h4 className="font-semibold text-orange-900 mb-2">Đăng ký online</h4>
              <p className="text-sm text-orange-700">Điền thông tin laptop cũ</p>
            </div>

            <div className="text-center">
              <div className="w-16 h-16 bg-orange-200 rounded-full flex items-center justify-center mx-auto mb-3">
                <span className="text-2xl"></span>
              </div>
              <h4 className="font-semibold text-orange-900 mb-2">Định giá</h4>
              <p className="text-sm text-orange-700">Chuyên viên kiểm tra tại nhà</p>
            </div>

            <div className="text-center">
              <div className="w-16 h-16 bg-orange-200 rounded-full flex items-center justify-center mx-auto mb-3">
                <span className="text-2xl"></span>
              </div>
              <h4 className="font-semibold text-orange-900 mb-2">Nhận tiền</h4>
              <p className="text-sm text-orange-700">Thanh toán ngay tại chỗ</p>
            </div>

            <div className="text-center">
              <div className="w-16 h-16 bg-orange-200 rounded-full flex items-center justify-center mx-auto mb-3">
                <span className="text-2xl"></span>
              </div>
              <h4 className="font-semibold text-orange-900 mb-2">Mua mới</h4>
              <p className="text-sm text-orange-700">Áp dụng ngay vào máy mới</p>
            </div>
          </div>

          <div className="bg-white p-6 rounded-lg">
            <h4 className="font-bold text-orange-900 mb-4 text-center">
               Bảng giá thu cũ tham khảo
            </h4>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <div className="text-center p-4 border border-orange-200 rounded-lg">
                <h5 className="font-semibold text-orange-900 mb-2">MacBook</h5>
                <div className="text-lg font-bold text-orange-600">8-25 triệu</div>
                <p className="text-sm text-orange-700">Tùy theo năm và cấu hình</p>
              </div>
              <div className="text-center p-4 border border-orange-200 rounded-lg">
                <h5 className="font-semibold text-orange-900 mb-2">Laptop Gaming</h5>
                <div className="text-lg font-bold text-orange-600">Dưới 2000$</div>
                <p className="text-sm text-orange-700">RTX 20/30/40 series</p>
              </div>
              <div className="text-center p-4 border border-orange-200 rounded-lg">
                <h5 className="font-semibold text-orange-900 mb-2">Laptop văn phòng</h5>
                <div className="text-lg font-bold text-orange-600">2-12 triệu</div>
                <p className="text-sm text-orange-700">Dell, HP, Lenovo, Asus</p>
              </div>
            </div>
          </div>

          <div className="text-center mt-6">
            <AppLink
              href="/trade-in-quote"
              pageType="static"
              className="inline-block bg-orange-600 hover:bg-orange-700 text-white px-8 py-3 rounded-lg font-semibold transition-colors"
            >
              Định giá laptop cũ ngay
            </AppLink>
          </div>
        </div>
      </section>

      {/* Bulk Discount Section */}
      <section id="bulk-discount" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Gift className="w-8 h-8 mr-3 text-purple-600" />
           Ưu Đãi Doanh Nghiệp
        </h2>

        <div className="bg-purple-50 rounded-xl p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div>
              <h3 className="text-2xl font-bold text-purple-900 mb-4">
                Giảm giá theo số lượng
              </h3>

              <div className="space-y-4">
                <div className="bg-white p-4 rounded-lg border-l-4 border-purple-500">
                  <div className="flex justify-between items-center">
                  <span className="font-semibold text-purple-900">5-10 máy</span>
                  <Badge className="bg-purple-100 text-purple-800">Giảm 5%</Badge>
                  </div>
                  <p className="text-sm text-purple-700 mt-1">+ Tặng setup miễn phí</p>
                </div>

                <div className="bg-white p-4 rounded-lg border-l-4 border-purple-500">
                  <div className="flex justify-between items-center">
                  <span className="font-semibold text-purple-900">11-20 máy</span>
                  <Badge className="bg-purple-100 text-purple-800">Giảm 10%</Badge>
                  </div>
                  <p className="text-sm text-purple-700 mt-1">+ Hỗ trợ kỹ thuật 6 tháng</p>
                </div>

                <div className="bg-white p-4 rounded-lg border-l-4 border-purple-500">
                  <div className="flex justify-between items-center">
                    <span className="font-semibold text-purple-900">21-50 máy</span>
                    <Badge className="bg-purple-100 text-purple-800">Giảm 15%</Badge>
                  </div>
                  <p className="text-sm text-purple-700 mt-1">+ Đào tạo sử dụng miễn phí</p>
                </div>

                <div className="bg-white p-4 rounded-lg border-l-4 border-purple-500">
                  <div className="flex justify-between items-center">
                  <span className="font-semibold text-purple-900">50+ máy</span>
                  <Badge className="bg-purple-100 text-purple-800">Giảm 20%</Badge>
                  </div>
                  <p className="text-sm text-purple-700 mt-1">+ Đội ngũ hỗ trợ riêng</p>
                </div>
              </div>
            </div>

            <div>
              <h3 className="text-xl font-bold text-purple-900 mb-4">
                 Dịch vụ doanh nghiệp
              </h3>

              <ul className="space-y-3 text-purple-800">
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-purple-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Tư vấn chuyên sâu:</strong><br />
                    <span className="text-sm">Phân tích nhu cầu và đề xuất giải pháp tối ưu</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-purple-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Giao hàng và setup:</strong><br />
                    <span className="text-sm">Triển khai tại văn phòng, cài đặt phần mềm</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-purple-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Bảo hành ưu tiên:</strong><br />
                    <span className="text-sm">Hotline riêng, thời gian phản hồi trong 2h</span>
                  </div>
                </li>
                <li className="flex items-start">
                  <span className="w-2 h-2 bg-purple-500 rounded-full mr-3 mt-2"></span>
                  <div>
                    <strong>Thanh toán linh hoạt:</strong><br />
                    <span className="text-sm">Chuyển khoản, trả góp, leasing</span>
                  </div>
                </li>
              </ul>

              <div className="mt-6 text-center">
                <AppLink
                  href="/enterprise-contact"
                  pageType="static"
                  className="inline-block bg-purple-600 hover:bg-purple-700 text-white px-6 py-3 rounded-lg font-semibold transition-colors"
                >
                   Liên hệ tư vấn
                </AppLink>
              </div>
            </div>
          </div>
        </div>
      </section>
    </div>
  );
}