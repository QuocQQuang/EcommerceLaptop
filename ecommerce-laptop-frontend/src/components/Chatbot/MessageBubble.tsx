'use client';

import { ChatMessage } from '@/types/chat';
import { Bot, User } from 'lucide-react';
import ReactMarkdown from 'react-markdown';
import remarkGfm from 'remark-gfm';

interface MessageBubbleProps {
    message: ChatMessage;
}

export function MessageBubble({ message }: MessageBubbleProps) {
    const isAI = message.role === 'assistant';
    const isUser = message.role === 'user';

    if (!isAI && !isUser) return null;

    return (
        <div className={`flex gap-3 ${isUser ? 'flex-row-reverse' : 'flex-row'} mb-4`}>
            {/* Avatar */}
            <div className={`flex-shrink-0 w-8 h-8 rounded-lg flex items-center justify-center ${isAI ? 'bg-neutral-100' : 'bg-neutral-900'
                }`}>
                {isAI ? (
                    <Bot className="w-5 h-5 text-neutral-700" />
                ) : (
                    <User className="w-5 h-5 text-white" />
                )}
            </div>

            {/* Message Content */}
            <div className={`flex flex-col max-w-[75%] ${isUser ? 'items-end' : 'items-start'}`}>
                <div className={`px-4 py-2.5 ${isAI
                        ? 'bg-neutral-50 border border-neutral-100 rounded-2xl'
                        : 'bg-neutral-900 text-white rounded-2xl rounded-br-sm'
                    }`}>
                    {isAI ? (
                        <div className="prose prose-sm max-w-none prose-neutral">
                            <ReactMarkdown
                                remarkPlugins={[remarkGfm]}
                                components={{
                                    p: ({ children }) => <p className="mb-2 last:mb-0 text-neutral-800">{children}</p>,
                                    ul: ({ children }) => <ul className="mb-2 last:mb-0 ml-4 list-disc text-neutral-800">{children}</ul>,
                                    ol: ({ children }) => <ol className="mb-2 last:mb-0 ml-4 list-decimal text-neutral-800">{children}</ol>,
                                    li: ({ children }) => <li className="mb-1 text-neutral-800">{children}</li>,
                                    strong: ({ children }) => <strong className="font-semibold text-neutral-900">{children}</strong>,
                                    code: ({ children }) => <code className="bg-neutral-100 px-1.5 py-0.5 rounded text-xs font-mono text-neutral-900">{children}</code>,
                                }}
                            >
                                {message.content}
                            </ReactMarkdown>
                        </div>
                    ) : (
                        <p className="text-sm leading-relaxed">{message.content}</p>
                    )}
                </div>

                {/* Timestamp */}
                <span className="text-[10px] text-neutral-400 mt-1 px-1">
                    {message.timestamp.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}
                </span>
            </div>
        </div>
    );
}
