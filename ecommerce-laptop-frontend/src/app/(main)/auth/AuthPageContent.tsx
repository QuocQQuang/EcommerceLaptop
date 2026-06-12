'use client';

import { Button } from '@/components/ui/button';
import { AnimatePresence, motion } from 'framer-motion';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { useRouter, useSearchParams } from 'next/navigation';
import { useEffect, useState } from 'react';
import { LoginForm } from './login/Form';
import { RegisterForm } from './register/Form';

const slideVariants = {
    hidden: { opacity: 0, x: 50 },
    visible: { opacity: 1, x: 0, transition: { duration: 0.4 } },
    exit: { opacity: 0, x: -50, transition: { duration: 0.4 } }
};

export default function AuthPageContent() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const callbackUrl = searchParams?.get('callbackUrl') || '/';
    const [mode, setMode] = useState<'login' | 'register'>('login');

    useEffect(() => {
        const initialMode = searchParams.get('mode') as 'login' | 'register' | null;
        if (initialMode === 'register') {
            setMode('register');
        }
    }, [searchParams]);

    const toggleMode = () => {
        setMode(prev => prev === 'login' ? 'register' : 'login');
    };

    const handleSuccess = () => {
        router.push(callbackUrl);
    };

    return (
        <div className="min-h-screen relative flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8 overflow-hidden bg-[#0a0a0a]">
            {/* Background Effects */}
            <div className="absolute inset-0 bg-gradient-to-br from-[#0a0a0a] via-[#111] to-[#0a0a0a]" />
            <div className="absolute inset-0 opacity-20">
                <div className="w-full h-full bg-[radial-gradient(circle_at_20%_80%,_#00f5d4_0%,_transparent_50%),_radial-gradient(circle_at_80%_20%,_#6366f1_0%,_transparent_50%)]" />
            </div>

            {/* Floating Glow Elements */}
            <motion.div
                className="absolute top-1/4 left-1/4 w-72 h-72 bg-[#00f5d4] rounded-full mix-blend-multiply filter blur-xl opacity-20"
                animate={{ scale: [1, 1.1, 1], opacity: [0.2, 0.4, 0.2] }}
                transition={{ duration: 3, repeat: Infinity }}
            />
            <motion.div
                className="absolute top-1/3 right-1/4 w-72 h-72 bg-[#6366f1] rounded-full mix-blend-multiply filter blur-xl opacity-20"
                animate={{ scale: [1, 1.05, 1], opacity: [0.2, 0.3, 0.2] }}
                transition={{ duration: 4, repeat: Infinity, delay: 1 }}
            />
            <motion.div
                className="absolute bottom-1/4 left-1/3 w-72 h-72 bg-[#00f5d4] rounded-full mix-blend-multiply filter blur-xl opacity-20"
                animate={{ scale: [1, 1.1, 1], opacity: [0.2, 0.4, 0.2] }}
                transition={{ duration: 3.5, repeat: Infinity, delay: 2 }}
            />

            {/* Back to Home */}
            <motion.div
                className="absolute top-6 left-6 z-20"
                initial={{ opacity: 0, x: -20 }}
                animate={{ opacity: 1, x: 0 }}
                transition={{ duration: 0.5 }}
            >
                <Link href="/" className="flex items-center space-x-2 text-[#f5f5f5] hover:text-[#00f5d4] transition-colors">
                    <ArrowLeft className="h-6 w-6" />
                    <span className="font-semibold">Trang chủ</span>
                </Link>
            </motion.div>

            <motion.div
                className="relative max-w-lg w-full z-10"
                initial={{ opacity: 0, scale: 0.95 }}
                animate={{ opacity: 1, scale: 1 }}
                transition={{ duration: 0.6 }}
            >
                {/* Title */}
                <motion.div
                    className="text-center mb-8"
                    initial={{ opacity: 0, y: 20 }}
                    animate={{ opacity: 1, y: 0 }}
                    transition={{ duration: 0.5, delay: 0.2 }}
                >
                    <motion.h1
                        className="text-4xl font-bold text-[#f5f5f5] mb-2 bg-gradient-to-r from-[#00f5d4] to-[#6366f1] bg-clip-text text-transparent"
                        whileHover={{ scale: 1.02 }}
                    >
                        {mode === 'login' ? 'Chào mừng trở lại' : 'Tạo tài khoản của bạn'}
                    </motion.h1>
                    <p className="text-[#9ca3af] font-medium">
                        {mode === 'login' ? 'Đăng nhập vào tài khoản của bạn' : 'Đăng Nhập'}
                    </p>
                </motion.div>

                {/* Form Container */}
                <div className="bg-[#1a1a1a] p-8 rounded-2xl shadow-2xl w-full border border-[#2a2a2a]">
                    <AnimatePresence mode="wait">
                        <motion.div
                            key={mode}
                            variants={slideVariants}
                            initial="hidden"
                            animate="visible"
                            exit="exit"
                        >
                            {mode === 'login' ? (
                                <LoginForm onSuccess={handleSuccess} />
                            ) : (
                                <RegisterForm onSuccess={handleSuccess} />
                            )}
                        </motion.div>
                    </AnimatePresence>

                    {/* Toggle Mode */}
                    <div className="mt-6 text-center">
                        <p className="text-[#9ca3af]">
                            {mode === 'login' ? "Chưa có tài khoản?" : "Đã có tài khoản?"}
                            <Button
                                variant="link"
                                className="font-semibold text-[#00f5d4] hover:text-[#00c4a9] pl-2"
                                onClick={toggleMode}
                            >
                                {mode === 'login' ? 'Đăng ký' : 'Đăng nhập'}
                            </Button>
                        </p>
                    </div>
                </div>
            </motion.div>
        </div>
    );
}
