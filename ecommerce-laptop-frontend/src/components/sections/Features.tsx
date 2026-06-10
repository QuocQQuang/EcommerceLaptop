'use client';

import { motion } from 'framer-motion';
import { Battery, Cpu, Monitor, Settings } from 'lucide-react';

const features = [
    {
        icon: Cpu,
        title: 'Hiệu năng vượt trội',
        description: 'Được trang bị vi xử lý Intel Core i9 thế hệ mới và GPU NVIDIA RTX 40-series để xử lý mọi tác vụ và chơi game.',
    },
    {
        icon: Monitor,
        title: 'Màn hình ấn tượng',
        description: 'Màn hình OLED 16" 4K, cảm ứng, tần số quét 120Hz, độ phủ màu 100% DCI-P3 và chống chói.',
    },
    {
        icon: Battery,
        title: 'Pin cả ngày',
        description: 'Lên tới 18 giờ sử dụng với sạc nhanh để làm việc, sáng tạo và giải trí không gián đoạn.',
    },
    {
        icon: Settings,
        title: 'Thiết kế cao cấp',
        description: 'Vỏ nhôm mịn đẹp, bàn phím đèn nền RGB và hệ thống tản nhiệt tối ưu cho trải nghiệm tuyệt vời.',
    },
];

const containerVariants = {
    hidden: { opacity: 0 },
    visible: {
        opacity: 1,
        transition: {
            staggerChildren: 0.2,
            delayChildren: 0.3,
        },
    },
};

const featureVariants = {
    hidden: { opacity: 0, y: 50 },
    visible: {
        opacity: 1,
        y: 0,
        transition: { duration: 0.6 },
    },
};

export function Features() {
    return (
        <section className="py-20 bg-background/50">
            <div className="container mx-auto px-4">
                <motion.div
                    className="text-center mb-16"
                    initial={{ opacity: 0, y: 30 }}
                    whileInView={{ opacity: 1, y: 0 }}
                    viewport={{ once: true }}
                    transition={{ duration: 0.6 }}
                >
                    <h2 className="text-4xl font-bold text-foreground mb-4">Tính năng chính</h2>
                    <p className="text-xl text-muted-foreground max-w-2xl mx-auto">
                        Khám phá điều gì làm cho laptop của chúng tôi nổi bật so với đối thủ.
                    </p>
                </motion.div>

                <motion.div
                    className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-8"
                    variants={containerVariants}
                    initial="hidden"
                    whileInView="visible"
                    viewport={{ once: true }}
                >
                    {features.map((feature, index) => (
                        <motion.div
                            key={index}
                            className="group bg-card p-8 rounded-xl border border-border hover:border-primary/50 transition-colors cursor-pointer"
                            variants={featureVariants}
                            whileHover={{ y: -5, scale: 1.02 }}
                            transition={{ type: 'spring', stiffness: 300 }}
                        >
                            <div className="w-16 h-16 bg-primary/10 rounded-lg flex items-center justify-center mb-6 group-hover:bg-primary/20 transition-colors">
                                <feature.icon className="w-8 h-8 text-primary group-hover:text-primary-foreground" />
                            </div>
                            <h3 className="text-2xl font-semibold text-foreground mb-3">{feature.title}</h3>
                            <p className="text-muted-foreground leading-relaxed">{feature.description}</p>
                        </motion.div>
                    ))}
                </motion.div>
            </div>
        </section>
    );
}