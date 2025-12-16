'use client';

import { motion, AnimatePresence } from 'framer-motion';
import { MessageCircle, X } from 'lucide-react';

interface ChatbotFABProps {
    isOpen: boolean;
    onClick: () => void;
    hasUnread?: boolean;
}

export function ChatbotFAB({ isOpen, onClick, hasUnread = false }: ChatbotFABProps) {
    return (
        <motion.button
            onClick={onClick}
            className="fixed bottom-6 right-6 z-50 w-14 h-14 bg-neutral-900 text-white rounded-2xl shadow-chatbot-float flex items-center justify-center"
            initial={{ scale: 0, opacity: 0 }}
            animate={{ scale: 1, opacity: 1 }}
            whileHover={{ y: -4, scale: 1.05 }}
            whileTap={{ scale: 0.95 }}
            transition={{ duration: 0.3 }}
            aria-label={isOpen ? 'ng chat' : 'M chat'}
        >
            {/* Unread Badge */}
            <AnimatePresence>
                {hasUnread && !isOpen && (
                    <motion.div
                        initial={{ scale: 0 }}
                        animate={{ scale: 1 }}
                        exit={{ scale: 0 }}
                        className="absolute -top-1 -right-1 w-3 h-3 bg-red-500 rounded-full"
                    >
                        <span className="absolute inset-0 w-full h-full bg-red-500 rounded-full animate-ping opacity-75" />
                    </motion.div>
                )}
            </AnimatePresence>

            {/* Icon with rotation */}
            <motion.div
                animate={{ rotate: isOpen ? 90 : 0 }}
                transition={{ duration: 0.3 }}
            >
                {isOpen ? (
                    <X className="w-6 h-6" />
                ) : (
                    <MessageCircle className="w-6 h-6" />
                )}
            </motion.div>
        </motion.button>
    );
}
