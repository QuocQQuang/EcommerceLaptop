'use client';

import { Button } from '@/components/ui/button';
import { motion } from 'framer-motion';
import { Laptop, Zap } from 'lucide-react';
import Link from 'next/link';

const heroVariants = {
    hidden: { opacity: 0, y: 50 },
    visible: {
        opacity: 1,
        y: 0,
        transition: { duration: 0.8 }
    }
};

const imageVariants = {
    hidden: { scale: 0.8, opacity: 0 },
    visible: {
        scale: 1,
        opacity: 1,
        transition: { duration: 1.2 }
    }
};

const glowVariants = {
    hidden: { opacity: 0, scale: 1 },
    visible: {
        opacity: [0.5, 0.8, 0.5],
        scale: [1, 1.05, 1],
        transition: {
            duration: 3,
            repeat: Infinity,

        }
    }
};

export function Hero() {
    return (
        <section className="relative min-h-screen flex items-center justify-center overflow-hidden bg-background">
            {/* Background Glow Effect */}
            <motion.div
                className="absolute inset-0 bg-gradient-to-br from-primary/10 via-transparent to-accent/10"
                variants={glowVariants}
                initial="hidden"
                animate="visible"
            />

            {/* Particles/Glow Background - Simplified with CSS */}
            <div className="absolute inset-0 bg-[radial-gradient(circle_at_20%_80%,_#00f5d4_0%,_transparent_50%),_radial-gradient(circle_at_80%_20%,_#6366f1_0%,_transparent_50%)] opacity-20" />

            <div className="container relative z-10 mx-auto px-4 text-center">
                <div className="flex flex-col lg:flex-row items-center gap-12">
                    {/* Text Content */}
                    <motion.div
                        className="lg:w-1/2 space-y-6"
                        variants={heroVariants}
                        initial="hidden"
                        whileInView="visible"
                        viewport={{ once: true }}
                    >
                        <motion.h1
                            className="text-5xl lg:text-7xl font-bold text-foreground"
                            variants={heroVariants}
                        >
                            Tương lai của Laptop
                        </motion.h1>
                        <motion.p
                            className="text-xl lg:text-2xl text-muted-foreground max-w-md mx-auto lg:mx-0"
                            variants={heroVariants}
                        >
                            Trải nghiệm hiệu năng vượt trội và thiết kế tinh tế trong dòng laptop cao cấp mới nhất. Được thiết kế dành cho người sáng tạo, chuyên gia và game thủ.
                        </motion.p>
                        <div className="flex flex-col sm:flex-row gap-4 justify-center lg:justify-start">
                            <Button asChild size="lg" className="bg-primary hover:bg-primary/90 text-primary-foreground shadow-lg">
                                <Link href="/products">
                                    Mua ngay
                                    <Zap className="ml-2 h-5 w-5" />
                                </Link>
                            </Button>
                            <Button asChild variant="outline" size="lg">
                                <Link href="/features">
                                    Xem tính năng
                                    <Laptop className="ml-2 h-5 w-5" />
                                </Link>
                            </Button>
                        </div>
                    </motion.div>

                    {/* Laptop Image */}
                    <motion.div
                        className="lg:w-1/2 relative"
                        variants={imageVariants}
                        initial="hidden"
                        whileInView="visible"
                        viewport={{ once: true }}
                    >
                        <motion.img
                            src="https://images.unsplash.com/photo-1583394838336-acd977736f90?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=1000&q=80"
                            alt="Future Laptop"
                            className="w-full max-w-md mx-auto rounded-xl shadow-2xl drop-shadow-2xl"
                            whileHover={{ rotateY: 5, rotateX: 5 }}
                            transition={{ type: 'spring', stiffness: 300 }}
                        />
                        {/* Glow Effect around Image */}
                        <motion.div
                            className="absolute -inset-4 bg-gradient-to-r from-primary/20 to-accent/20 rounded-xl blur-xl opacity-70"
                            animate={{
                                scale: [1, 1.1, 1],
                                opacity: [0.7, 0.9, 0.7]
                            }}
                            transition={{ duration: 2, repeat: Infinity }}
                        />
                    </motion.div>
                </div>
            </div>
        </section>
    );
}