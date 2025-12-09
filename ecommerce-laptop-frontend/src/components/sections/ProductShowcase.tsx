'use client';

import { AnimatePresence, motion } from 'framer-motion';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { useEffect, useState } from 'react';

const images = [
    'https://images.unsplash.com/photo-1496181133206-80ce9b88a853?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80',
    'https://images.unsplash.com/photo-1525547719571-a2d4ac8945e2?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80',
    'https://images.unsplash.com/photo-1583394838336-acd977736f90?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80',
    'https://images.unsplash.com/photo-1601784551446-20c9e07cdbdb?ixlib=rb-4.0.3&auto=format&fit=crop&w=800&q=80',
];

const specs = [
    { label: 'Processor', value: 'Intel Core i9-13900HX' },
    { label: 'Graphics', value: 'NVIDIA RTX 4080 12GB' },
    { label: 'RAM', value: '32GB DDR5-5600' },
    { label: 'Storage', value: '1TB NVMe SSD' },
    { label: 'Display', value: '16" 4K OLED 120Hz' },
    { label: 'Battery', value: '99Wh, 18hrs' },
];

const carouselVariants = {
    hidden: { opacity: 0, x: 50 },
    visible: {
        opacity: 1,
        x: 0,
        transition: { duration: 0.8 },
    },
};

const imageVariants = {
    hover: { scale: 1.05, rotateY: 5 },
    tap: { scale: 0.98 },
};

export function ProductShowcase() {
    const [currentIndex, setCurrentIndex] = useState(0);

    useEffect(() => {
        const interval = setInterval(() => {
            setCurrentIndex((prev) => (prev + 1) % images.length);
        }, 4000); // Auto-slide every 4 seconds

        return () => clearInterval(interval);
    }, []);

    const goToPrevious = () => {
        setCurrentIndex((prev) => (prev - 1 + images.length) % images.length);
    };

    const goToNext = () => {
        setCurrentIndex((prev) => (prev + 1) % images.length);
    };

    return (
        <section className="py-20 bg-card/50">
            <div className="container mx-auto px-4">
                <motion.h2
                    className="text-4xl font-bold text-center text-foreground mb-4"
                    variants={carouselVariants}
                    initial="hidden"
                    whileInView="visible"
                    viewport={{ once: true }}
                >
                    Trng by sn phm
                </motion.h2>
                <motion.p
                    className="text-xl text-center text-muted-foreground mb-16 max-w-2xl mx-auto"
                    variants={carouselVariants}
                    initial="hidden"
                    whileInView="visible"
                    viewport={{ once: true }}
                >
                    Khm ph laptop flagship ca chng ti t mi gc . K thut chnh xc gp g cng ngh tin tin.
                </motion.p>

                {/* Carousel */}
                <div className="relative max-w-4xl mx-auto mb-16">
                    <AnimatePresence mode="wait">
                        <motion.img
                            key={currentIndex}
                            src={images[currentIndex]}
                            alt={`Laptop view ${currentIndex + 1}`}
                            className="w-full h-96 object-cover rounded-xl shadow-2xl border border-border"
                            variants={imageVariants}
                            initial={{ opacity: 0, scale: 0.9 }}
                            animate={{ opacity: 1, scale: 1 }}
                            exit={{ opacity: 0, scale: 0.9 }}
                            whileHover="hover"
                            whileTap="tap"
                            transition={{ duration: 0.5 }}
                        />
                    </AnimatePresence>

                    {/* Navigation Buttons */}
                    <button
                        onClick={goToPrevious}
                        className="absolute left-4 top-1/2 -translate-y-1/2 bg-background/80 hover:bg-primary/80 text-foreground p-2 rounded-full transition-colors"
                    >
                        <ChevronLeft className="h-6 w-6" />
                    </button>
                    <button
                        onClick={goToNext}
                        className="absolute right-4 top-1/2 -translate-y-1/2 bg-background/80 hover:bg-primary/80 text-foreground p-2 rounded-full transition-colors"
                    >
                        <ChevronRight className="h-6 w-6" />
                    </button>

                    {/* Dots */}
                    <div className="flex justify-center space-x-2 mt-4">
                        {images.map((_, index) => (
                            <button
                                key={index}
                                onClick={() => setCurrentIndex(index)}
                                className={`w-3 h-3 rounded-full transition-colors ${index === currentIndex ? 'bg-primary' : 'bg-muted-foreground'
                                    }`}
                            />
                        ))}
                    </div>
                </div>

                {/* Specifications */}
                <motion.div
                    className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6 max-w-4xl mx-auto"
                    variants={carouselVariants}
                    initial="hidden"
                    whileInView="visible"
                    viewport={{ once: true }}
                >
                    {specs.map((spec, index) => (
                        <motion.div
                            key={index}
                            className="bg-background p-6 rounded-lg border border-border hover:border-primary/50 transition-colors"
                            whileHover={{ y: -2 }}
                        >
                            <h3 className="font-semibold text-foreground mb-1">{spec.label}</h3>
                            <p className="text-muted-foreground">{spec.value}</p>
                        </motion.div>
                    ))}
                </motion.div>
            </div>
        </section>
    );
}