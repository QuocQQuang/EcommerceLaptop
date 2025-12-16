'use client';

import React, { useEffect, useRef } from 'react';
import { useSession } from 'next-auth/react';
import { MessageCircle, X, Send } from 'lucide-react';
import { useChatBot } from '../../hooks/useChatBot';
import MessageBubble from './MessageBubble';

const ChatWidget = () => {
    // Auth Token Retrieval
    const { data: session } = useSession();
    const userToken = (session as any)?.accessToken || (session as any)?.user?.accessToken || null;

    const {
        isOpen,
        setIsOpen,
        messages,
        isStreaming,
        sendMessage,
        isConnected,
        statusMessage
    } = useChatBot(userToken);

    const [inputValue, setInputValue] = React.useState('');
    const messagesEndRef = useRef<HTMLDivElement>(null);

    // Auto-scroll to bottom
    const scrollToBottom = () => {
        messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
    };

    useEffect(() => {
        scrollToBottom();
    }, [messages, isStreaming, statusMessage]);

    const handleSend = async () => {
        if (!inputValue.trim()) return;
        const text = inputValue;
        setInputValue('');
        await sendMessage(text);
    };

    const handleKeyDown = (e: React.KeyboardEvent) => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            handleSend();
        }
    };

    return (
        <div className="fixed bottom-6 right-6 z-50 flex flex-col items-end pointer-events-none">
            {/* Chat Window */}
            {isOpen && (
                <div className="mb-4 w-[90vw] md:w-[400px] h-[600px] bg-white/90 backdrop-blur-xl rounded-3xl shadow-2xl shadow-slate-200/50 border border-slate-100 ring-1 ring-slate-900/5 flex flex-col pointer-events-auto overflow-hidden animate-in slide-in-from-bottom-5 fade-in duration-300 origin-bottom-right">
                    {/* Header */}
                    <div className="p-4 border-b border-slate-100 bg-white/50 backdrop-blur-sm rounded-t-3xl flex justify-between items-center">
                        <div className="flex items-center space-x-3">
                            <div className="relative">
                                <div className="w-2 h-2 rounded-full bg-teal-500 animate-pulse"></div>
                                <div className="absolute inset-0 w-2 h-2 rounded-full bg-teal-500 animate-ping opacity-75"></div>
                            </div>
                            <div>
                                <span className="font-semibold text-slate-800 text-sm tracking-tight">AI Tech Assistant</span>
                                <p className="text-[10px] text-slate-500 uppercase tracking-wide">Online</p>
                            </div>
                        </div>
                        <button
                            onClick={() => setIsOpen(false)}
                            className="hover:bg-slate-100 p-2 rounded-lg transition-colors active:scale-95"
                            aria-label="Close chat"
                        >
                            <X size={18} className="text-slate-600" />
                        </button>
                    </div>

                    {/* Messages Area */}
                    <div className="flex-1 overflow-y-auto overflow-x-hidden p-4 bg-gradient-to-b from-slate-50/50 to-white/50 scrollbar-hide">
                        {messages.length === 0 ? (
                            <div className="flex flex-col items-center justify-center h-full text-slate-400 text-center space-y-3">
                                <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-indigo-500/10 to-violet-500/10 flex items-center justify-center">
                                    <MessageCircle size={32} className="text-indigo-500/40" />
                                </div>
                                <div>
                                    <p className="text-slate-700 font-medium">Hi! I'm your AI assistant.</p>
                                    <p className="text-xs text-slate-500 mt-1">Ask me about laptops, specs, or recommendations!</p>
                                </div>
                            </div>
                        ) : (
                            <>
                                {messages.map((msg) => (
                                    <MessageBubble key={msg.id} message={msg} />
                                ))}
                                {/* Status Indicator */}
                                {isStreaming && statusMessage && (
                                    <div className="flex items-center space-x-2 text-xs text-slate-500 ml-2 mb-2 animate-pulse">
                                        <div className="w-1.5 h-1.5 bg-indigo-500 rounded-full animate-bounce"></div>
                                        <span>{statusMessage}</span>
                                    </div>
                                )}
                                <div ref={messagesEndRef} />
                            </>
                        )}
                    </div>

                    {/* Input Area - Floating Pill */}
                    <div className="p-4">
                        <div className="flex items-center bg-slate-100 rounded-full px-4 py-2 focus-within:bg-white focus-within:ring-2 focus-within:ring-indigo-500/20 transition-all">
                            <textarea
                                value={inputValue}
                                onChange={(e) => setInputValue(e.target.value)}
                                onKeyDown={handleKeyDown}
                                placeholder="Type your message..."
                                className="flex-1 resize-none bg-transparent text-sm focus:outline-none max-h-20 scrollbar-hide text-slate-700 placeholder:text-slate-400"
                                rows={1}
                            />
                            <button
                                onClick={handleSend}
                                disabled={!inputValue.trim() || !isConnected}
                                className={`ml-2 p-2 rounded-full transition-all active:scale-95 ${inputValue.trim() && isConnected
                                        ? 'bg-gradient-to-br from-indigo-600 to-violet-600 text-white shadow-md shadow-indigo-500/20 hover:shadow-lg'
                                        : 'bg-slate-200 text-slate-400 cursor-not-allowed'
                                    }`}
                                aria-label="Send message"
                            >
                                <Send size={16} />
                            </button>
                        </div>
                        {!isConnected && (
                            <p className="text-[10px] text-red-500 mt-2 text-center">Connection lost. Reconnecting...</p>
                        )}
                    </div>
                </div>
            )}

            {/* Floating Launcher Button */}
            <button
                onClick={() => setIsOpen(!isOpen)}
                className={`p-4 rounded-2xl shadow-lg transition-all duration-200 pointer-events-auto ${isOpen
                        ? 'bg-slate-100 text-slate-600 hover:bg-slate-200 rotate-90 scale-95'
                        : 'bg-gradient-to-br from-indigo-600 to-violet-600 text-white shadow-indigo-500/30 hover:scale-110 active:scale-95 animate-breathing'
                    }`}
                aria-label={isOpen ? 'Minimize chat' : 'Open chat'}
            >
                {isOpen ? <X size={24} /> : <MessageCircle size={24} />}
            </button>
        </div>
    );
};

export default ChatWidget;
