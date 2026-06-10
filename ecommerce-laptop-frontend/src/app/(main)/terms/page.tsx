import { Separator } from '@/components/ui/separator';
import { Metadata } from 'next';

export const metadata: Metadata = {
    title: 'Điều khoản dịch vụ - Laptop Store',
    description: 'Điều khoản và điều kiện sử dụng dịch vụ của Laptop Store',
};

export default function TermsPage() {
    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <h1 className="text-3xl font-bold mb-6">Điều Khoản Dịch Vụ</h1>
            <p className="text-lg text-gray-700 mb-8">
                Chào mừng bạn đến với Laptop Store. Việc sử dụng website và dịch vụ của chúng tôi có nghĩa là bạn đồng ý với các điều khoản sau. Vui lòng đọc kỹ trước khi sử dụng. Các điều khoản này có hiệu lực từ ngày 25/09/2025.
            </p>

            <Separator className="my-8" />

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">1. Điều Khoản Chung</h2>
                <p className="text-gray-700 mb-4">
                    Laptop Store là nền tảng thương mại điện tử chuyên cung cấp sản phẩm laptop và phụ kiện chính hãng. Chúng tôi cam kết tuân thủ pháp luật Việt Nam và bảo vệ quyền lợi người tiêu dùng theo Luật Bảo vệ quyền lợi người tiêu dùng 2010 (sửa đổi 2023).
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1">
                    <li>Tuổi tối thiểu để sử dụng dịch vụ: 18 tuổi hoặc có sự giám hộ của phụ huynh.</li>
                    <li>Chúng tôi có quyền từ chối hoặc hủy đơn hàng nếu phát hiện vi phạm.</li>
                    <li>Thông tin trên website chỉ mang tính chất tham khảo, không thay thế tư vấn chuyên nghiệp.</li>
                </ul>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">2. Đặt Hàng Và Thanh Toán</h2>
                <p className="text-gray-700 mb-4">
                    Khi đặt hàng, bạn cam kết cung cấp thông tin chính xác. Laptop Store có quyền hủy đơn nếu thông tin sai lệch hoặc không thể xác minh.
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1">
                    <li>Giá sản phẩm hiển thị là giá cuối cùng, có thể thay đổi tùy chương trình khuyến mãi.</li>
                    <li>Chúng tôi không chịu trách nhiệm về sai sót in ấn hoặc hiển thị giá.</li>
                    <li>Thanh toán qua các cổng uy tín (VNPAY, Momo, PayPal, v.v.). Thông tin thanh toán được bảo mật theo tiêu chuẩn PCI DSS.</li>
                    <li>Đơn hàng chỉ được xác nhận sau khi nhận thanh toán thành công.</li>
                </ul>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">3. Giao Hàng Và Nhận Hàng</h2>
                <p className="text-gray-700 mb-4">
                    Laptop Store giao hàng toàn quốc qua các đơn vị vận chuyển uy tín (GHN, GHTK, Viettel Post).
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1">
                    <li>Thời gian giao hàng: 2-5 ngày làm việc tùy khu vực.</li>
                    <li>Khách hàng chịu trách nhiệm kiểm tra hàng khi nhận. Khi ký nhận, đơn hàng được coi là hoàn tất.</li>
                    <li>Chúng tôi không chịu trách nhiệm mất mát hoặc hư hỏng trong quá trình vận chuyển (bảo hiểm tùy chọn).</li>
                    <li>Phí giao hàng được thông báo trước khi xác nhận đơn.</li>
                </ul>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">4. Đổi Trả Và Hoàn Tiền</h2>
                <p className="text-gray-700 mb-4">
                    Chúng tôi áp dụng chính sách đổi trả theo quy định pháp luật (Luật Bảo vệ quyền lợi người tiêu dùng).
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1">
                    <li>Đổi trả trong 30 ngày nếu sản phẩm lỗi hoặc không đúng mô tả (giữ nguyên tem niêm phong).</li>
                    <li>Hoàn tiền 100% nếu lỗi nhà sản xuất, không hoàn nếu sử dụng sai.</li>
                    <li>Trường hợp hết hàng, chúng tôi hoàn tiền hoặc đề xuất sản phẩm thay thế.</li>
                    <li>Liên hệ hỗ trợ để bắt đầu quy trình đổi trả.</li>
                </ul>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">5. Bảo Hành Và Hỗ trợ</h2>
                <p className="text-gray-700 mb-4">
                    Tất cả sản phẩm được bảo hành chính hãng từ nhà sản xuất (1-3 năm tùy model).
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1">
                    <li>Bảo hành phần mềm và phần cứng theo chính sách nhà sản xuất.</li>
                    <li>Hỗ trợ kỹ thuật miễn phí trong thời gian bảo hành.</li>
                    <li>Không bảo hành nếu sửa chữa bên ngoài hoặc sử dụng sai quy định.</li>
                </ul>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">6. Bảo Mật Thông Tin</h2>
                <p className="text-gray-700">
                    Xem chi tiết tại <a href="/privacy" className="text-blue-600 hover:underline">Chính sách bảo mật</a>. Chúng tôi cam kết bảo vệ dữ liệu cá nhân theo GDPR và luật Việt Nam.
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">7. Trách Nhiệm Pháp Lý</h2>
                <p className="text-gray-700">
                    Laptop Store không chịu trách nhiệm cho nội dung người dùng tạo (reviews, comments). Chúng tôi có quyền xóa nội dung vi phạm. Tranh chấp được giải quyết theo pháp luật Việt Nam.
                </p>
            </section>

            <p className="text-sm text-gray-500 mt-8">
                Điều khoản này có thể được cập nhật mà không thông báo trước. Ngày cập nhật cuối: 25/09/2025.
            </p>
        </div>
    );
}
