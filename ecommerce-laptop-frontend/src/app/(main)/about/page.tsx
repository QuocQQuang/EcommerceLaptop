import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Separator } from '@/components/ui/separator';
import { Metadata } from 'next';

export const metadata: Metadata = {
    title: 'Giới thiệu - Laptop Store',
    description: 'Tìm hiểu về Laptop Store - cửa hàng laptop chính hãng uy tín nhất Việt Nam',
};

export default function AboutPage() {
    return (
        <div className="container mx-auto px-4 py-8 max-w-4xl">
            <h1 className="text-3xl font-bold mb-6">Giới Thiệu Về Laptop Store</h1>
            <p className="text-lg text-gray-700 mb-8">
                Laptop Store là cửa hàng chuyên cung cấp laptop chính hãng với hơn 10 năm kinh nghiệm trong lĩnh vực công nghệ. Chúng tôi cam kết mang đến cho khách hàng những sản phẩm chất lượng cao từ các thương hiệu hàng đầu thế giới như Dell, HP, Lenovo, Asus, MacBook và nhiều hơn nữa.
            </p>

            <Separator className="my-8" />

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">Sứ Mệnh Của Chúng Tôi</h2>
                <p className="text-gray-700">
                    Chúng tôi không chỉ bán laptop mà còn đồng hành cùng khách hàng trong việc lựa chọn thiết bị phù hợp với nhu cầu công việc, học tập và giải trí. Với đội ngũ chuyên viên tư vấn giàu kinh nghiệm, Laptop Store luôn sẵn sàng hỗ trợ bạn 24/7.
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">Lịch Sử Hình Thành</h2>
                <p className="text-gray-700">
                    Thành lập từ năm 2015, Laptop Store bắt đầu như một cửa hàng nhỏ chuyên cung cấp laptop cho sinh viên và nhân viên văn phòng. Với sự phát triển không ngừng và cam kết chất lượng, chúng tôi đã mở rộng quy mô và trở thành một trong những nhà phân phối laptop lớn nhất tại Việt Nam.
                </p>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">Giá Trị Cốt Lõi</h2>
                <div className="grid md:grid-cols-2 gap-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>Chất Lượng Đầu Tiên</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p>Tất cả sản phẩm đều được kiểm tra nghiêm ngặt trước khi đến tay khách hàng. Chúng tôi chỉ bán hàng chính hãng với đầy đủ giấy tờ chứng nhận.</p>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardHeader>
                            <CardTitle>Phục Vụ Tận Tâm</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p>Hỗ trợ khách hàng từ tư vấn đến hậu mãi với thời gian chuyên nghiệp. Đội ngũ của chúng tôi luôn sẵn sàng giải đáp mọi thắc mắc.</p>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardHeader>
                            <CardTitle>Giá Cả Cạnh Tranh</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p>Cam kết giá tốt nhất thị trường với chính sách bảo hành rõ ràng và nhiều chương trình khuyến mãi hấp dẫn.</p>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardHeader>
                            <CardTitle>Đổi Trả Linh Hoạt</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p>Chính sách đổi trả trong 30 ngày với điều kiện đơn giản. Chúng tôi luôn đặt sự hài lòng của khách hàng lên hàng đầu.</p>
                        </CardContent>
                    </Card>
                </div>
            </section>

            <section className="mb-8">
                <h2 className="text-2xl font-semibold mb-4">Liên Hệ Với Chúng Tôi</h2>
                <div className="grid md:grid-cols-3 gap-6">
                    <Card>
                        <CardHeader>
                            <CardTitle>Hotline</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p className="text-lg font-bold">1800-123-456</p>
                            <p className="text-sm text-gray-600">Tư vấn 24/7</p>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardHeader>
                            <CardTitle>Email</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p className="text-lg">support@laptopstore.vn</p>
                            <p className="text-sm text-gray-600">Phản hồi trong 24h</p>
                        </CardContent>
                    </Card>
                    <Card>
                        <CardHeader>
                            <CardTitle>Địa Chỉ</CardTitle>
                        </CardHeader>
                        <CardContent>
                            <p className="text-lg">123 Đường Laptop, TP.HCM</p>
                            <p className="text-sm text-gray-600">Mở cửa 8:00 - 22:00</p>
                        </CardContent>
                    </Card>
                </div>
            </section>
        </div>
    );
}
