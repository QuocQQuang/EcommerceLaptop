'use client';

import { motion, AnimatePresence } from 'framer-motion';
import { X, Trash2, Send } from 'lucide-react';
import { ChatMessage } from '@/types/chat';
import { MessageBubble } from './MessageBubble';
import { ProductCarousel } from './ProductCarousel';
import { OrderCarousel } from './OrderCarousel';
import { QuickActionChips } from './QuickActionChips';
import TypingIndicator from './TypingIndicator';
import { useState, useRef, useEffect } from 'react';

interface ChatbotWindowProps {
    isOpen: boolean;
    messages: ChatMessage[];
    isStreaming: boolean;
    statusMessage: string;
    onClose: () => void;
    onSendMessage: (message: string) => void;
    onClearChat: () => void;
}

export function ChatbotWindow({
    isOpen,
    messages,
    isStreaming,
    statusMessage,
    onClose,
    onSendMessage,
    onClearChat
}: ChatbotWindowProps) {
    const [inputValue, setInputValue] = useState('');
    const messagesEndRef = useRef<HTMLDivElement>(null);
    const inputRef = useRef<HTMLInputElement>(null);

    // Auto-scroll to bottom when new messages arrive
    useEffect(() => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    }, [messages, isStreaming]);

    // Focus input when chat opens
    useEffect(() => {
        if (isOpen) {
            inputRef.current?.focus();
        }
    }, [isOpen]);

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (inputValue.trim() && !isStreaming) {
            onSendMessage(inputValue.trim());
            setInputValue('');
        }
    };

    const handleQuickAction = (query: string) => {
        if (!isStreaming) {
            onSendMessage(query);
        }
    };

    const handleConsultation = (productName: string) => {
        if (!isStreaming) {
            onSendMessage(`T vn v ${productName}`);
        }
    };

    return (
        <AnimatePresence>
            {isOpen && (
                <>
                    {/* Mobile Overlay */}
                    <motion.div
                        initial={{ opacity: 0 }}
                        animate={{ opacity: 1 }}
                        exit={{ opacity: 0 }}
                        className="fixed inset-0 bg-black/20 z-40 md:hidden"
                        onClick={onClose}
                    />

                    {/* Chat Window */}
                    <motion.div
                        initial={{ opacity: 0, y: 20, scale: 0.95 }}
                        animate={{ opacity: 1, y: 0, scale: 1 }}
                        exit={{ opacity: 0, y: 20, scale: 0.95 }}
                        transition={{ duration: 0.3 }}
                        className="fixed bottom-6 right-6 z-50 w-full md:w-[33vw] md:min-w-[500px] md:max-w-[600px] h-[calc(100vh-3rem)] bg-white/80 backdrop-blur-xl saturate-150 border border-neutral-200/80 rounded-2xl shadow-chatbot-window flex flex-col overflow-hidden"
                        style={{
                            left: 'auto',
                            right: '1.5rem',
                            bottom: '1.5rem',
                        }}
                    >
                        {/* Header */}
                        <div className="flex items-center justify-between px-4 py-3 border-b border-neutral-200/80 bg-white/60">
                            <div className="flex items-center gap-2">
                                <div className="w-2 h-2 bg-green-500 rounded-full animate-pulse" />
                                <h3 className="font-semibold text-sm text-neutral-900">Tr l AI</h3>
                            </div>
                            <div className="flex items-center gap-2">
                                <button
                                    onClick={onClearChat}
                                    className="p-1.5 hover:bg-neutral-100 rounded-lg transition-colors"
                                    aria-label="Xóa cuộc trò chuyện"
                                    title="Xóa cuộc trò chuyện"
                                >
                                    <Trash2 className="w-4 h-4 text-neutral-600" />
                                </button>
                                <button
                                    onClick={onClose}
                                    className="p-1.5 hover:bg-neutral-100 rounded-lg transition-colors"
                                    aria-label="ng chat"
                                >
                                    <X className="w-4 h-4 text-neutral-600" />
                                </button>
                            </div>
                        </div>

                        {/* Messages Area */}
                        <div className="flex-1 overflow-y-auto p-4 space-y-4">
                            {messages.length === 0 ? (
                                <div className="flex flex-col items-center justify-center h-full text-center px-4">
                                    <div className="w-16 h-16 bg-neutral-100 rounded-2xl flex items-center justify-center mb-4">
                                        <motion.div
                                            animate={{ rotate: 360 }}
                                            transition={{ duration: 20, repeat: Infinity, ease: 'linear' }}
                                        >
                                            <svg className="w-8 h-8 text-neutral-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9.75 17L9 20l-1 1h8l-1-1-.75-3M3 13h18M5 17h14a2 2 0 002-2V5a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
                                            </svg>
                                        </motion.div>
                                    </div>
                                    <h4 className="font-semibold text-neutral-900 mb-2">Cho mng n vi Tr l Laptop</h4>
                                    <p className="text-sm text-neutral-600 mb-4">
                                        Tôi có thể giúp bạn tìm chiếc laptop hoàn hảo. Hỏi tôi bất cứ điều gì!
                                    </p>
                                    <QuickActionChips onChipClick={handleQuickAction} />
                                </div>
                            ) : (
                                <>
                                    {messages.map((message, index) => {
                                        const isLastMessage = index === messages.length - 1;
                                        // Only show products if: not last message OR (last message AND not streaming)
                                        const shouldShowProducts = message.products && message.products.length > 0 && !isLastMessage;

                                        return (
                                            <div key={message.id}>
                                                <MessageBubble message={message} />
                                                {shouldShowProducts && (
                                                    <div className="mt-2 mb-4">
                                                        <ProductCarousel
                                                            products={message.products || []}
                                                            onConsult={handleConsultation}
                                                        />
                                                    </div>
                                                )}

                                                {message.orders && message.orders.length > 0 && (
                                                    <div className="mt-2 mb-4">
                                                        <OrderCarousel orders={message.orders} />
                                                    </div>
                                                )}
                                            </div>
                                        );
                                    })}

                                    {isStreaming && <TypingIndicator />}

                                    {/* Show products for last message only when NOT streaming */}
                                    {!isStreaming && messages.length > 0 && messages[messages.length - 1].products && messages[messages.length - 1].products!.length > 0 && (
                                        <div className="mt-2 mb-4">
                                            <ProductCarousel
                                                products={messages[messages.length - 1].products!}
                                                onConsult={handleConsultation}
                                            />
                                        </div>

                                    )}



                                    {statusMessage && (
                                        <div className="text-xs text-neutral-500 italic px-2">
                                            {statusMessage}
                                        </div>
                                    )}

                                    <div ref={messagesEndRef} />
                                </>
                            )}
                        </div>

                        {/* Input Area */}
                        <div className="border-t border-neutral-200/80 bg-white/60 p-3">
                            <form onSubmit={handleSubmit} className="flex gap-2">
                                <input
                                    ref={inputRef}
                                    type="text"
                                    value={inputValue}
                                    onChange={(e) => setInputValue(e.target.value)}
                                    placeholder="Hi v laptop..."
                                    disabled={isStreaming}
                                    className="flex-1 px-3 py-2 text-sm border border-neutral-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-neutral-900 focus:border-transparent disabled:bg-neutral-50 disabled:text-neutral-400"
                                />
                                <button
                                    type="submit"
                                    disabled={!inputValue.trim() || isStreaming}
                                    className="p-2 bg-neutral-900 text-white rounded-lg hover:bg-neutral-800 disabled:bg-neutral-300 disabled:cursor-not-allowed transition-colors"
                                    aria-label="Gi tin nhn"
                                >
                                    <Send className="w-5 h-5" />
                                </button>
                            </form>
                        </div>
                    </motion.div>
                </>
            )}
        </AnimatePresence>
    );
}
