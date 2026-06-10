import { Separator } from '@/components/ui/separator';
import { Metadata } from 'next';

export const metadata: Metadata = {
    title: 'Chính sách bảo mật - Laptop Store',
    description: 'Chính sách bảo vệ thông tin cá nhân của người dùng trên Laptop Store',
};

export default function PrivacyPage() {
    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <h1 className="text-3xl font-bold mb-6">Chính Sách Bảo Mật</h1>
            <p className="text-lg text-gray-700 mb-8">
                Tại Laptop Store, chúng tôi coi trọng quyền riêng tư của bạn. Chính sách này giải thích cách chúng tôi thu thập, sử dụng và bảo vệ thông tin cá nhân của bạn khi sử dụng website và dịch vụ. Chính sách có hiệu lực từ ngày 25/09/2025 và tuân thủ Luật An ninh mạng 2018 và Luật Bảo vệ dữ liệu cá nhân 2023 của Việt Nam.
            </p>

            <Separator className="my-8" />

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">1. Thông Tin Chúng Tôi Thu Thập</h2>
                <p className="text-gray-700 mb-4">
                    Chúng tôi thu thập thông tin cần thiết để cung cấp dịch vụ, bao gồm:
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1 mb-4">
                    <li>Tên, email, số điện thoại khi đăng ký tài khoản hoặc đặt hàng.</li>
                    <li>Địa chỉ giao hàng, thông tin thanh toán (không lưu thẻ tín dụng).</li>
                    <li>Thông tin duyệt web (IP, thiết bị, trình duyệt) để cải thiện dịch vụ và ngăn chặn gian lận.</li>
                    <li>Dữ liệu sử dụng (sản phẩm xem, tìm kiếm) để cá nhân hóa khuyến nghị.</li>
                </ul>
                <p className="text-gray-700">
                    Chúng tôi không thu thập dữ liệu nhạy cảm không cần thiết. Dữ liệu được mã hóa truyền tải (HTTPS).
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">2. Cách Chúng Tôi Sử Dụng Thông Tin</h2>
                <p className="text-gray-700 mb-4">
                    Thông tin được sử dụng để:
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1 mb-4">
                    <li>Xử lý đơn hàng, giao hàng, thanh toán.</li>
                    <li>Gửi email xác nhận, khuyến mãi (có thể hủy đăng ký).</li>
                    <li>Cải thiện website (phân tích ẩn danh).</li>
                    <li>Ngăn chặn lạm dụng (rate limiting, IP block nếu vi phạm).</li>
                </ul>
                <p className="text-gray-700">
                    Chúng tôi không bán hoặc chia sẻ dữ liệu với bên thứ ba ngoài đối tác vận chuyển/thanh toán (với sự đồng ý của bạn).
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">3. Chia Sẻ Thông Tin</h2>
                <p className="text-gray-700 mb-4">
                    Chúng tôi chỉ chia sẻ với:
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1 mb-4">
                    <li>Đối tác giao hàng (địa chỉ, số điện thoại).</li>
                    <li>Cổng thanh toán (thông tin giao dịch, không lưu thẻ).</li>
                    <li>Cơ quan pháp luật nếu yêu cầu (gian lận, lạm dụng).</li>
                </ul>
                <p className="text-gray-700">
                    Không chia sẻ với bên thứ ba quảng cáo. Cookie chỉ dùng cho phiên đăng nhập/giỏ hàng (có thể xóa).
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">4. Bảo Vệ Dữ Liệu</h2>
                <p className="text-gray-700 mb-4">
                    Chúng tôi sử dụng:
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1 mb-4">
                    <li>Mã hóa dữ liệu (AES-256 cho mật khẩu).</li>
                    <li>HTTPS cho tất cả truyền tải.</li>
                    <li>Firewall, rate limiting, IP blocking chống tấn công.</li>
                    <li>Backup mã hóa, lưu trữ an toàn.</li>
                </ul>
                <p className="text-gray-700">
                    Trong trường hợp vi phạm dữ liệu, chúng tôi thông báo trong 72 giờ theo luật.
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">5. Quyền Của Bạn</h2>
                <p className="text-gray-700">
                    Bạn có quyền:
                </p>
                <ul className="list-disc pl-6 text-gray-700 space-y-1 mb-4">
                    <li>Xem/xóa/cập nhật dữ liệu cá nhân (tài khoản).</li>
                    <li>Hủy đăng ký email marketing.</li>
                    <li>Tải dữ liệu (email support@laptopstore.vn).</li>
                    <li>Khiếu nại nếu vi phạm (liên hệ hotline).</li>
                </ul>
                <p className="text-gray-700">
                    Dữ liệu lưu 5 năm sau khi tài khoản không hoạt động, xóa theo yêu cầu.
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">6. Cookie Và Theo Dõi</h2>
                <p className="text-gray-700">
                    Chúng tôi sử dụng cookie cho phiên đăng nhập, giỏ hàng (essential). Cookie phân tích ẩn danh (Google Analytics). Bạn có thể xóa cookie qua trình duyệt settings.
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">7. Thay Đổi Chính Sách</h2>
                <p className="text-gray-700">
                    Chúng tôi có thể cập nhật chính sách này. Thay đổi sẽ được đăng tải trên website và thông báo qua email nếu ảnh hưởng lớn. Tiếp tục sử dụng sau khi cập nhật = đồng ý.
                </p>
            </section>

            <p className="text-sm text-gray-500 mt-8">
                Có câu hỏi? Liên hệ <a href="/contact" className="text-blue-600 hover:underline">support@laptopstore.vn</a> hoặc hotline 1800-123-456.
            </p>
        </div>
    );
}
