'use client';

import React, { useEffect, useRef } from 'react';
import { useSession } from 'next-auth/react';
import { MessageCircle, X, Send, Minimize2 } from 'lucide-react'; // Assuming lucide-react is installed or similar icons
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
        <div className="fixed bottom-4 right-4 z-50 flex flex-col items-end pointer-events-none">
            {/* Chat Window */}
            {isOpen && (
                <div className="mb-4 w-[90vw] md:w-[400px] h-[500px] bg-white rounded-2xl shadow-2xl flex flex-col border border-gray-200 pointer-events-auto overflow-hidden animate-in slide-in-from-bottom-5 duration-300">
                    {/* Header */}
                    <div className="bg-blue-600 p-4 text-white flex justify-between items-center shadow-md">
                        <div className="flex items-center space-x-2">
                            <div className="w-2 h-2 rounded-full bg-green-400 animate-pulse"></div>
                            <span className="font-semibold">AI Assistant</span>
                        </div>
                        <button
                            onClick={() => setIsOpen(false)}
                            className="hover:bg-blue-700 p-1 rounded transition-colors"
                        >
                            <X size={20} />
                        </button>
                    </div>

                    {/* Messages Area */}
                    <div className="flex-1 overflow-y-auto p-4 bg-gray-50 scrollbar-thin scrollbar-thumb-gray-200">
                        {messages.length === 0 ? (
                            <div className="flex flex-col items-center justify-center h-full text-gray-400 text-center space-y-2">
                                <MessageCircle size={48} className="opacity-20" />
                                <p>Hi! I'm your AI assistant.</p>
                                <p className="text-xs">Ask me about laptops, specs, or recommendations!</p>
                            </div>
                        ) : (
                            <>
                                {messages.map((msg) => (
                                    <MessageBubble key={msg.id} message={msg} />
                                ))}
                                {/* Status Indicator Bubbles */}
                                {isStreaming && statusMessage && (
                                    <div className="flex items-center space-x-2 text-xs text-gray-500 ml-2 mb-2 animate-pulse">
                                        <div className="w-1.5 h-1.5 bg-blue-500 rounded-full animate-bounce"></div>
                                        <span>{statusMessage}</span>
                                    </div>
                                )}
                                <div ref={messagesEndRef} />
                            </>
                        )}
                    </div>

                    {/* Input Area */}
                    <div className="p-4 bg-white border-t border-gray-100">
                        <div className="flex items-center space-x-2 relative">
                            <textarea
                                value={inputValue}
                                onChange={(e) => setInputValue(e.target.value)}
                                onKeyDown={handleKeyDown}
                                placeholder="Type your message..."
                                className="w-full resize-none border border-gray-200 rounded-xl py-3 px-4 pr-12 text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent max-h-32 scrollbar-none"
                                rows={1}
                            />
                            <button
                                onClick={handleSend}
                                disabled={!inputValue.trim() || !isConnected}
                                className={`absolute right-2 p-2 rounded-lg transition-all ${inputValue.trim() && isConnected
                                    ? 'bg-blue-600 text-white hover:bg-blue-700 shadow-md'
                                    : 'bg-gray-100 text-gray-400 cursor-not-allowed'
                                    }`}
                            >
                                <Send size={18} />
                            </button>
                        </div>
                        {!isConnected && (
                            <p className="text-[10px] text-red-500 mt-1 text-center">Connection lost. Reconnecting...</p>
                        )}
                    </div>
                </div>
            )}

            {/* Floating Launcher Button */}
            <button
                onClick={() => setIsOpen(!isOpen)}
                className={`p-4 rounded-full shadow-lg transition-all transform hover:scale-110 pointer-events-auto ${isOpen
                    ? 'bg-gray-200 text-gray-600 rotate-90'
                    : 'bg-blue-600 text-white hover:bg-blue-700'
                    }`}
            >
                {isOpen ? <Minimize2 size={24} /> : <MessageCircle size={24} />}
            </button>
        </div>
    );
};

export default ChatWidget;
