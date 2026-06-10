'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Mail, MapPin, Phone, Send } from 'lucide-react';
import { useState } from 'react';

export default function ContactPage() {
    const [formData, setFormData] = useState({
        name: '',
        email: '',
        subject: '',
        message: ''
    });

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        // Handle form submission (e.g., send to API)
        console.log('Contact form submitted:', formData);
        // Reset form
        setFormData({ name: '', email: '', subject: '', message: '' });
    };

    const handleChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => {
        setFormData({
            ...formData,
            [e.target.name]: e.target.value
        });
    };

    return (
        <div className="min-h-screen bg-background py-12">
            <div className="container mx-auto px-4 max-w-4xl">
                <div className="text-center mb-12">
                    <h1 className="text-4xl font-bold tracking-tight mb-4">Liên hệ với chúng tôi</h1>
                    <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
                        Chúng tôi luôn sẵn sàng hỗ trợ bạn. Hãy liên hệ qua form bên dưới hoặc thông tin liên lạc.
                    </p>
                </div>

                <div className="grid md:grid-cols-2 gap-8">
                    {/* Contact Info */}
                    <Card className="bg-card">
                        <CardHeader>
                            <CardTitle className="flex items-center gap-2">
                                <Send className="h-5 w-5" />
                                Thông tin liên lạc
                            </CardTitle>
                            <CardDescription>
                                Gửi tin nhắn cho chúng tôi hoặc gọi điện trực tiếp.
                            </CardDescription>
                        </CardHeader>
                        <CardContent className="space-y-4">
                            <div className="flex items-center gap-3">
                                <Phone className="h-5 w-5 text-muted-foreground" />
                                <div>
                                    <p className="font-medium">Điện thoại</p>
                                    <p className="text-sm text-muted-foreground">+84 123 456 789</p>
                                </div>
                            </div>
                            <div className="flex items-center gap-3">
                                <Mail className="h-5 w-5 text-muted-foreground" />
                                <div>
                                    <p className="font-medium">Email</p>
                                    <p className="text-sm text-muted-foreground">support@laptopstore.vn</p>
                                </div>
                            </div>
                            <div className="flex items-center gap-3">
                                <MapPin className="h-5 w-5 text-muted-foreground" />
                                <div>
                                    <p className="font-medium">Địa chỉ</p>
                                    <p className="text-sm text-muted-foreground">123 Đường Laptop, Quận 1, TP.HCM</p>
                                </div>
                            </div>
                        </CardContent>
                    </Card>

                    {/* Contact Form */}
                    <Card className="bg-card">
                        <CardHeader>
                            <CardTitle>Gửi tin nhắn</CardTitle>
                            <CardDescription>
                                Điền thông tin vào form để chúng tôi liên hệ lại với bạn.
                            </CardDescription>
                        </CardHeader>
                        <CardContent>
                            <form onSubmit={handleSubmit} className="space-y-4">
                                <div>
                                    <Input
                                        name="name"
                                        placeholder="Họ và tên"
                                        value={formData.name}
                                        onChange={handleChange}
                                        required
                                    />
                                </div>
                                <div>
                                    <Input
                                        name="email"
                                        type="email"
                                        placeholder="Email"
                                        value={formData.email}
                                        onChange={handleChange}
                                        required
                                    />
                                </div>
                                <div>
                                    <Input
                                        name="subject"
                                        placeholder="Tiêu đề"
                                        value={formData.subject}
                                        onChange={handleChange}
                                        required
                                    />
                                </div>
                                <div>
                                    <Textarea
                                        name="message"
                                        placeholder="Nội dung tin nhắn..."
                                        value={formData.message}
                                        onChange={handleChange}
                                        rows={5}
                                        required
                                    />
                                </div>
                                <Button type="submit" className="w-full">
                                    Gửi tin nhắn
                                </Button>
                            </form>
                        </CardContent>
                    </Card>
                </div>
            </div>
        </div>
    );
}