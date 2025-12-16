import React from 'react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';
import { ChatMessage } from '../../types/chat';
import ProductCarousel from './ProductCarousel';
import TypingIndicator from './TypingIndicator';

interface MessageBubbleProps {
    message: ChatMessage;
}

const MessageBubble: React.FC<MessageBubbleProps> = ({ message }) => {
    const isUser = message.role === 'user';

    return (
        <div className={`flex w-full mb-4 ${isUser ? 'justify-end' : 'justify-start'}`}>
            <div className="flex flex-col w-full">
                {/* 1. Constraint Text Area */}
                <div className={`flex max-w-[85%] md:max-w-[75%] ${isUser ? 'self-end flex-row-reverse' : 'self-start flex-row'}`}>

                    {/* Avatar (Assistant only) */}
                    {!isUser && (
                        <div className="flex-shrink-0 mr-2">
                            <div className="w-8 h-8 rounded-full bg-gradient-to-tr from-blue-500 to-indigo-600 flex items-center justify-center text-white text-xs shadow-md">
                                AI
                            </div>
                        </div>
                    )}

                    <div className={`flex flex-col min-w-0 w-full ${isUser ? 'items-end' : 'items-start'}`}>
                        <div
                            className={`px-4 py-2 rounded-2xl shadow-sm text-sm ${isUser
                                ? 'bg-blue-600 text-white rounded-tr-none'
                                : 'bg-white border border-gray-100 text-gray-800 rounded-tl-none'
                                }`}
                        >
                            {/* Interactive Loading State */}
                            {message.isStreaming && message.content === '' ? (
                                <TypingIndicator />
                            ) : (
                                <div className="prose prose-sm max-w-none dark:prose-invert">
                                    <ReactMarkdown remarkPlugins={[remarkGfm]}>
                                        {message.content}
                                    </ReactMarkdown>
                                </div>
                            )}
                        </div>

                        {/* Streaming Cursor (optional visual flair) */}
                        {!isUser && message.isStreaming && message.content !== '' && (
                            <span className="text-gray-400 text-xs mt-1 animate-pulse">Typing...</span>
                        )}

                        <span className="text-[10px] text-gray-400 mt-1 px-1">
                            {new Date(message.timestamp).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </span>
                    </div>
                </div>

                {/* 2. Full Width Product Carousel (Outside Constraint) */}
                {!isUser && message.products && message.products.length > 0 && (
                    <div className="w-full px-1 animate-in fade-in slide-in-from-bottom-2 duration-300">
                        <ProductCarousel products={message.products} />
                    </div>
                )}
            </div>
        </div>
    );
};

export default MessageBubble;
