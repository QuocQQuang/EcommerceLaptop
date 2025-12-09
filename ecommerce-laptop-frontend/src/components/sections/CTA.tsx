'use client';

import { Button } from '@/components/ui/button';
import { motion } from 'framer-motion';
import { Zap } from 'lucide-react';
import Link from 'next/link';

const ctaVariants = {
    hidden: { opacity: 0, y: 30 },
    visible: {
        opacity: 1,
        y: 0,
        transition: { duration: 0.8 },
    },
};

export function CTA() {
    return (
        <section className="relative py-20 overflow-hidden">
            {/* Background Gradient */}
            <div className="absolute inset-0 bg-gradient-to-r from-primary/20 via-accent/20 to-primary/20" />

            {/* Blurred Laptop Image */}
            <div className="absolute inset-0">
                <img
                    src="https://images.unsplash.com/photo-1583394838336-acd977736f90?ixlib=rb-4.0.3&auto=format&fit=crop&w=1000&q=80"
                    alt="Laptop"
                    className="w-full h-full object-cover opacity-10 blur-sm"
                />
            </div>

            <motion.div
                className="relative container mx-auto px-4 text-center"
                variants={ctaVariants}
                initial="hidden"
                whileInView="visible"
                viewport={{ once: true }}
            >
                <h2 className="text-4xl lg:text-5xl font-bold text-foreground mb-4">
                    Sn sng nng cp?
                </h2>
                <p className="text-xl text-muted-foreground mb-8 max-w-2xl mx-auto">
                    Tham gia hng ngn khch hng hi lng  nng cao nng sut vi laptop cao cp ca chng ti.
                </p>
                <motion.div
                    whileHover={{ scale: 1.05 }}
                    whileTap={{ scale: 0.95 }}
                >
                    <Button asChild size="lg" className="bg-primary hover:bg-primary/90 text-primary-foreground shadow-lg px-8">
                        <Link href="/products">
                            Mua ngay
                            <Zap className="ml-2 h-5 w-5" />
                        </Link>
                    </Button>
                </motion.div>
            </motion.div>
        </section>
    );
}