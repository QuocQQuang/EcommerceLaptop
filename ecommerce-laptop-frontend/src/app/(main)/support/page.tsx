import { AppLink } from '@/components/atoms/AppLink';
import { StaticPageLayout } from '@/components/layouts/StaticPageLayout';
import { SupportCentersInteractive } from '@/components/support/SupportCentersInteractive';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Clock,
  Download,
  HeadphonesIcon,
  HelpCircle,
  Mail,
  MapPin,
  MessageCircle,
  Phone,
  RefreshCw,
  Shield,
  Truck,
  Users
} from 'lucide-react';

export default function SupportPage() {
  const tableOfContents = [
    { id: 'contact-info', title: 'Thông tin liên hệ' },
    { id: 'warranty', title: 'Chính sách bảo hành' },
    { id: 'shipping', title: 'Vận chuyển & Giao hàng' },
    { id: 'return-policy', title: 'Đổi trả sản phẩm' },
    { id: 'faq', title: 'Câu hỏi thường gặp' },
    { id: 'service-centers', title: 'Trung tâm bảo hành' },
    { id: 'technical-support', title: 'Hỗ trợ kỹ thuật' }
  ];

  const breadcrumbs = [
    { label: 'Trang chủ', href: '/' },
    { label: 'Hỗ trợ khách hàng' }
  ];

  return (
    <StaticPageLayout
      title=" Hỗ Trợ Khách Hàng"
      subtitle="Chúng tôi luôn sẵn sàng hỗ trợ bạn 24/7 với đội ngũ chuyên viên giàu kinh nghiệm"
      lastUpdated="18/09/2025"
      author="Team Support"
      readTime="8"
      tableOfContents={tableOfContents}
      breadcrumbs={breadcrumbs}
    >
      <SupportContent />
    </StaticPageLayout>
  );
}

function SupportContent() {
  const contactMethods = [
    {
      icon: <Phone className="w-6 h-6 text-blue-600" />,
      title: 'Hotline 24/7',
      info: '1900-1234',
      description: 'Tư vấn và hỗ trợ miễn phí',
      availability: 'Hoạt động 24/7'
    },
    {
      icon: <MessageCircle className="w-6 h-6 text-green-600" />,
      title: 'Live Chat',
      info: 'Chat ngay',
      description: 'Hỗ trợ trực tiếp qua website',
      availability: '6:00 - 23:00 hàng ngày'
    },
    {
      icon: <Mail className="w-6 h-6 text-purple-600" />,
      title: 'Email Support',
      info: 'support@laptopstore.vn',
      description: 'Phản hồi trong vòng 2 giờ',
      availability: 'Phản hồi nhanh chóng'
    },
    {
      icon: <MapPin className="w-6 h-6 text-red-600" />,
      title: 'Showroom',
      info: '15+ ca hng',
      description: 'Trải nghiệm trực tiếp sản phẩm',
      availability: '8:00 - 22:00 hàng ngày'
    }
  ];

  const faqItems = [
    {
      question: 'Làm thế nào để kiểm tra bảo hành của sản phẩm?',
      answer: 'Bạn có thể kiểm tra tình trạng bảo hành bằng cách nhập serial number hoặc mã đơn hàng trên trang tra cứu bảo hành của chúng tôi.'
    },
    {
      question: 'Tôi có thể đổi trả sản phẩm trong bao lâu?',
      answer: 'Chúng tôi chấp nhận đổi trả trong vòng 15 ngày kể từ ngày mua hàng với điều kiện sản phẩm còn nguyên seal, đầy đủ phụ kiện và hóa đơn.'
    },
    {
      question: 'Chi phí vận chuyển được tính như thế nào?',
      answer: 'Miễn phí vận chuyển cho đơn hàng từ 3 triệu đồng trở lên trong nội thành. Các khu vực khác tính phí theo khoảng cách và trọng lượng.'
    },
    {
      question: 'Làm sao để được hỗ trợ cài đặt phần mềm?',
      answer: 'Chúng tôi cung cấp dịch vụ cài đặt miễn phí Windows, Office và phần mềm cơ bản. Liên hệ hotline để đặt lịch hẹn.'
    }
  ];

  return (
    <div className="space-y-12">
      {/* Contact Information Section */}
      <section id="contact-info" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <HeadphonesIcon className="w-8 h-8 mr-3 text-blue-600" />
            Liên Hệ Với Chúng Tôi
        </h2>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
          {contactMethods.map((method, index) => (
            <Card key={index} className="text-center hover:shadow-lg transition-shadow">
              <CardHeader>
                <div className="w-16 h-16 mx-auto bg-gray-100 rounded-full flex items-center justify-center mb-4">
                  {method.icon}
                </div>
                <CardTitle className="text-lg">{method.title}</CardTitle>
              </CardHeader>
              <CardContent>
                <div className="font-semibold text-xl text-gray-900 mb-2">
                  {method.info}
                </div>
                <p className="text-gray-600 text-sm mb-2">{method.description}</p>
                <Badge variant="secondary" className="text-xs">
                  {method.availability}
                </Badge>
              </CardContent>
            </Card>
          ))}
        </div>

        <div className="mt-8 text-center">
          <Button
            size="lg"
            className="bg-blue-600 hover:bg-blue-700"
          >
             Bắt đầu trò chuyện ngay
          </Button>
        </div>
      </section>

      {/* Warranty Policy Section */}
      <section id="warranty" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Shield className="w-8 h-8 mr-3 text-green-600" />
           Chính Sách Bảo Hành
        </h2>

        <div className="bg-green-50 rounded-xl p-6 mb-8">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="text-center">
              <div className="text-4xl mb-3"></div>
              <h3 className="font-semibold text-green-900 mb-2">Bảo hành chính hãng</h3>
              <p className="text-green-700 text-sm">Toàn bộ sản phẩm được bảo hành chính hãng từ nhà sản xuất</p>
            </div>
            <div className="text-center">
              <div className="text-4xl mb-3"></div>
              <h3 className="font-semibold text-green-900 mb-2">Bảo hành nhanh</h3>
              <p className="text-green-700 text-sm">Thời gian bảo hành trung bình 3-5 ngày làm việc</p>
            </div>
            <div className="text-4xl mb-3"></div>
            <h3 className="font-semibold text-green-900 mb-2">Đổi mới 1:1</h3>
            <p className="text-green-700 text-sm">Đổi mới ngay nếu lỗi phần cứng trong 30 ngày đầu</p>
          </div>
        </div>

        <div className="space-y-6">
          <div className="border-l-4 border-green-500 pl-6">
            <h3 className="text-xl font-semibold text-gray-900 mb-3"> Quy trình bảo hành</h3>
            <ol className="space-y-2 text-gray-700">
              <li><strong>Bước 1:</strong> Liên hệ hotline 1900-1234 hoặc mang sản phẩm đến trung tâm bảo hành</li>
              <li><strong>Bước 2:</strong> Kỹ thuật viên kiểm tra và báo cáo tình trạng sản phẩm</li>
              <li><strong>Bước 3:</strong> Thực hiện sửa chữa hoặc thay thế linh kiện</li>
              <li><strong>Bước 4:</strong> Test và bàn giao sản phẩm về cho khách hàng</li>
            </ol>
          </div>

          <div className="border-l-4 border-blue-500 pl-6">
            <h3 className="text-xl font-semibold text-gray-900 mb-3"> Thời gian bảo hành</h3>
            <ul className="space-y-2 text-gray-700">
              <li> <strong>Laptop:</strong> 12-36 tháng tùy theo hãng và model</li>
              <li> <strong>Phụ kiện:</strong> 6-12 tháng bảo hành chính hãng</li>
              <li> <strong>Bảo hành mở rộng:</strong> Có thể mua thêm gói bảo hành 2-3 năm</li>
            </ul>
          </div>
        </div>
      </section>

      {/* Shipping Policy Section */}
      <section id="shipping" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Truck className="w-8 h-8 mr-3 text-orange-600" />
           Vận Chuyển & Giao Hàng
        </h2>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          <div>
            <h3 className="text-xl font-semibold text-gray-900 mb-4"> Chính sách giao hàng</h3>
            <div className="space-y-4">
              <div className="bg-orange-50 p-4 rounded-lg">
                <h4 className="font-semibold text-orange-900 mb-2">Giao hàng nhanh nội thành</h4>
                <p className="text-orange-800 text-sm">1-2 giờ (Hà Nội, TP.HCM) - Phí 30,000</p>
              </div>
              <div className="bg-blue-50 p-4 rounded-lg">
                <h4 className="font-semibold text-blue-900 mb-2">Giao hàng tiêu chuẩn</h4>
                <p className="text-blue-800 text-sm">1-3 ngày làm việc - Miễn phí từ 3 triệu</p>
              </div>
              <div className="bg-green-50 p-4 rounded-lg">
                <h4 className="font-semibold text-green-900 mb-2">Giao hàng toàn quốc</h4>
                <p className="text-green-800 text-sm">2-5 ngày làm việc - Phí theo khu vực</p>
              </div>
            </div>
          </div>

          <div>
            <h3 className="text-xl font-semibold text-gray-900 mb-4"> Dịch vụ đặc biệt</h3>
            <ul className="space-y-3 text-gray-700">
              <li className="flex items-start">
                <span className="w-2 h-2 bg-orange-500 rounded-full mr-3 mt-2"></span>
                <div>
                  <strong>Giao hàng và setup tại nhà:</strong><br />
                  <span className="text-sm text-gray-600">Phí 100,000 - Bao gồm cài đặt phần mềm cơ bản</span>
                </div>
              </li>
              <li className="flex items-start">
                <span className="w-2 h-2 bg-blue-500 rounded-full mr-3 mt-2"></span>
                <div>
                  <strong>Giao hàng theo lịch hẹn:</strong><br />
                  <span className="text-sm text-gray-600">Đặt lịch giao hàng theo thời gian mong muốn</span>
                </div>
              </li>
              <li className="flex items-start">
                <span className="w-2 h-2 bg-green-500 rounded-full mr-3 mt-2"></span>
                <div>
                  <strong>Kiểm tra hàng trước khi thanh toán:</strong><br />
                  <span className="text-sm text-gray-600">Cho phép mở seal và test máy trước khi nhận</span>
                </div>
              </li>
            </ul>
          </div>
        </div>
      </section>

      {/* Return Policy Section */}
      <section id="return-policy" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <RefreshCw className="w-8 h-8 mr-3 text-purple-600" />
           Chính Sách Đổi Trả
        </h2>

        <div className="bg-purple-50 rounded-xl p-6 mb-6">
          <div className="text-center mb-6">
            <h3 className="text-2xl font-bold text-purple-900 mb-2">
              Đổi trả miễn phí trong 15 ngày
            </h3>
            <p className="text-purple-700">
              Cam kết 100% hoàn tiền nếu không hài lòng về sản phẩm
            </p>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="text-center">
              <div className="text-3xl mb-3"></div>
              <h4 className="font-semibold text-purple-900 mb-2">15 ngày đầu</h4>
              <p className="text-purple-700 text-sm">Đổi mới hoặc hoàn tiền 100%</p>
            </div>
            <div className="text-center">
              <div className="text-3xl mb-3"></div>
              <h4 className="font-semibold text-purple-900 mb-2">Lỗi nhà sản xuất</h4>
              <p className="text-purple-700 text-sm">Đổi mới miễn phí toàn bộ chi phí</p>
            </div>
            <div className="text-center">
              <div className="text-3xl mb-3"></div>
              <h4 className="font-semibold text-purple-900 mb-2">Điều kiện đơn giản</h4>
              <p className="text-purple-700 text-sm">Giữ nguyên hộp, phụ kiện và hóa đơn</p>
            </div>
          </div>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <h3 className="text-lg font-semibold text-gray-900 mb-3"> Điều kiện đổi trả</h3>
            <ul className="space-y-2 text-gray-700 text-sm">
              <li> Sản phẩm trong thời gian bảo hành</li>
              <li> Còn nguyên tem niêm phong (nếu có)</li>
              <li> Đầy đủ hộp, phụ kiện, tài liệu</li>
              <li> Không có dấu hiệu va đập, cấn móp</li>
              <li> Có hóa đơn mua hàng hoặc phiếu bảo hành</li>
            </ul>
          </div>

          <div>
            <h3 className="text-lg font-semibold text-gray-900 mb-3"> Trường hợp không đổi trả</h3>
            <ul className="space-y-2 text-gray-700 text-sm">
              <li> Sản phẩm đã qua sử dụng lâu dài</li>
              <li> Hư hỏng do tác động ngoại lực</li>
              <li> Hư hỏng do ngấm nước, cháy nổ</li>
              <li> Sản phẩm đã can thiệp sửa chữa</li>
              <li> Quá thời hạn đổi trả quy định</li>
            </ul>
          </div>
        </div>
      </section>

      {/* FAQ Section */}
      <section id="faq" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <HelpCircle className="w-8 h-8 mr-3 text-indigo-600" />
           Câu Hỏi Thường Gặp
        </h2>

        <div className="space-y-4">
          {faqItems.map((item, index) => (
            <Card key={index} className="border-l-4 border-l-indigo-500">
              <CardHeader>
                <CardTitle className="text-lg text-indigo-900">{item.question}</CardTitle>
              </CardHeader>
              <CardContent>
                <p className="text-gray-700 leading-6">{item.answer}</p>
              </CardContent>
            </Card>
          ))}
        </div>

        <div className="mt-8 text-center">
          <p className="text-gray-600 mb-4">Không tìm thấy câu trả lời bạn cần?</p>
          <AppLink
            href="/contact"
            pageType="static"
            className="inline-block bg-indigo-600 hover:bg-indigo-700 text-white px-6 py-3 rounded-lg font-semibold transition-colors"
          >
            Đặt câu hỏi cho chúng tôi
          </AppLink>
        </div>
      </section>

      {/* Service Centers with Interactive Elements */}
      <SupportCentersInteractive />

      {/* Technical Support Section */}
      <section id="technical-support" className="content-section">
        <h2 className="text-3xl font-bold text-gray-900 mb-6 flex items-center">
          <Users className="w-8 h-8 mr-3 text-teal-600" />
           Hỗ Trợ Kỹ Thuật
        </h2>

        <div className="bg-teal-50 rounded-xl p-6">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div>
              <h3 className="text-xl font-semibold text-teal-900 mb-4"> Dịch vụ miễn phí</h3>
              <ul className="space-y-3 text-teal-800">
                <li className="flex items-center">
                  <Download className="w-5 h-5 mr-3" />
                  <span>Cài đặt Windows & Office bản quyền</span>
                </li>
                <li className="flex items-center">
                  <Shield className="w-5 h-5 mr-3" />
                  <span>Cài đặt phần mềm bảo mật & antivirus</span>
                </li>
                <li className="flex items-center">
                  <RefreshCw className="w-5 h-5 mr-3" />
                  <span>Chuyển dữ liệu từ máy cũ sang máy mới</span>
                </li>
                <li className="flex items-center">
                  <Clock className="w-5 h-5 mr-3" />
                  <span>Tối ưu hóa hiệu năng hệ thống</span>
                </li>
              </ul>
            </div>

            <div>
              <h3 className="text-xl font-semibold text-teal-900 mb-4"> Dịch vụ cao cấp</h3>
              <ul className="space-y-3 text-teal-800">
                <li className="flex items-center">
                  <span className="w-5 h-5 bg-teal-600 rounded-full mr-3 flex items-center justify-center text-white text-xs">1</span>
                  <span>Nâng cấp RAM, SSD - Tư vấn miễn phí</span>
                </li>
                <li className="flex items-center">
                  <span className="w-5 h-5 bg-teal-600 rounded-full mr-3 flex items-center justify-center text-white text-xs">2</span>
                  <span>Vệ sinh laptop định kỳ - 200,000/lần</span>
                </li>
                <li className="flex items-center">
                  <span className="w-5 h-5 bg-teal-600 rounded-full mr-3 flex items-center justify-center text-white text-xs">3</span>
                  <span>Cài đặt phần mềm chuyên ngành</span>
                </li>
                <li className="flex items-center">
                  <span className="w-5 h-5 bg-teal-600 rounded-full mr-3 flex items-center justify-center text-white text-xs">4</span>
                  <span>Hỗ trợ kỹ thuật từ xa 24/7</span>
                </li>
              </ul>
            </div>
          </div>

          <div className="text-center mt-6">
            <Button
              size="lg"
              className="bg-teal-600 hover:bg-teal-700"
            >
               Đặt lịch hỗ trợ kỹ thuật
            </Button>
          </div>
        </div>
      </section>
    </div>
  );
}