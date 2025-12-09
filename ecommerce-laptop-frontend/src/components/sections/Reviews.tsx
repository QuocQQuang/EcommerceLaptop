'use client';

import { AnimatePresence, motion } from 'framer-motion';
import { Star, User } from 'lucide-react';
import { useEffect, useState } from 'react';

const reviews = [
    {
        name: 'Sarah Johnson',
        avatar: 'https://images.unsplash.com/photo-1494790108755-2616b612b786?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=200&q=80',
        rating: 5,
        text: 'Chic laptop ny  thay i hon ton cng vic ca ti. Hiu nng tuyt vi v thi lng pin n tng. Rt ng mua cho ngi dng chuyn nghip!',
    },
    {
        name: 'Mike Chen',
        avatar: 'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=200&q=80',
        rating: 5,
        text: 'L mt game th, ti n tng vi  ha RTX v mn hnh 120Hz mt m. Khng git, hnh nh sng ng  ng l c my mnh m!',
    },
    {
        name: 'Emily Rodriguez',
        avatar: 'https://images.unsplash.com/photo-1438761681033-6461ffad8d80?ixlib=rb-4.0.3&ixid=M3wxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8fA%3D%3D&auto=format&fit=crop&w=200&q=80',
        rating: 5,
        text: 'Thit k thanh lch, hin i. Rt ph hp cho cng vic sng to vi mn hnh OLED. Pin tru c ngy  ti rt hi lng!',
    },
];

const sliderVariants = {
    hidden: { opacity: 0, x: 50 },
    visible: {
        opacity: 1,
        x: 0,
        transition: { duration: 0.6 },
    },
};

export function Reviews() {
    const [currentIndex, setCurrentIndex] = useState(0);

    useEffect(() => {
        const interval = setInterval(() => {
            setCurrentIndex((prev) => (prev + 1) % reviews.length);
        }, 5000); // Auto-slide every 5 seconds

        return () => clearInterval(interval);
    }, []);

    return (
        <section className="py-20 bg-background/50">
            <div className="container mx-auto px-4">
                <motion.h2
                    className="text-4xl font-bold text-center text-foreground mb-4"
                    variants={sliderVariants}
                    initial="hidden"
                    whileInView="visible"
                    viewport={{ once: true }}
                >
                    Khch hng ni g
                </motion.h2>
                <motion.p
                    className="text-xl text-center text-muted-foreground mb-16 max-w-2xl mx-auto"
                    variants={sliderVariants}
                    initial="hidden"
                    whileInView="visible"
                    viewport={{ once: true }}
                >
                    ng ch nghe chng ti ni  lng nghe nh gi t nhng ngi dng thc t  nng cp tri nghim ca h.
                </motion.p>

                <div className="max-w-4xl mx-auto relative">
                    <AnimatePresence mode="wait">
                        <motion.div
                            key={currentIndex}
                            className="bg-card p-8 rounded-xl border border-border shadow-lg"
                            variants={sliderVariants}
                            initial={{ opacity: 0, x: 100 }}
                            animate={{ opacity: 1, x: 0 }}
                            exit={{ opacity: 0, x: -100 }}
                            transition={{ duration: 0.5 }}
                        >
                            <div className="flex items-start gap-4">
                                {/* Avatar */}
                                <div className="flex-shrink-0">
                                    <img
                                        src={reviews[currentIndex].avatar}
                                        alt={reviews[currentIndex].name}
                                        className="w-16 h-16 rounded-full object-cover border-2 border-primary/20"
                                    />
                                </div>

                                {/* Content */}
                                <div className="flex-1">
                                    {/* Stars */}
                                    <div className="flex items-center mb-3">
                                        {[...Array(5)].map((_, i) => (
                                            <Star
                                                key={i}
                                                className={`h-5 w-5 ${i < reviews[currentIndex].rating ? 'text-primary fill-primary' : 'text-muted-foreground'
                                                    }`}
                                            />
                                        ))}
                                    </div>

                                    {/* Quote */}
                                    <p className="text-lg text-foreground italic mb-4">"{reviews[currentIndex].text}"</p>

                                    {/* Name */}
                                    <div className="flex items-center gap-2">
                                        <User className="h-4 w-4 text-muted-foreground" />
                                        <span className="font-semibold text-foreground">{reviews[currentIndex].name}</span>
                                    </div>
                                </div>
                            </div>
                        </motion.div>
                    </AnimatePresence>

                    {/* Dots */}
                    <div className="flex justify-center space-x-2 mt-6">
                        {reviews.map((_, index) => (
                            <button
                                key={index}
                                onClick={() => setCurrentIndex(index)}
                                className={`w-3 h-3 rounded-full transition-all ${index === currentIndex ? 'bg-primary scale-125' : 'bg-muted-foreground'
                                    }`}
                            />
                        ))}
                    </div>
                </div>
            </div>
        </section>
    );
}