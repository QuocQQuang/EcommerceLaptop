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
                                    table: ({ children }) => (
                                        <div className="my-4 overflow-x-auto rounded-2xl border border-neutral-200/80">
                                            <table className="min-w-full border-collapse text-sm">{children}</table>
                                        </div>
                                    ),
                                    thead: ({ children }) => <thead className="bg-neutral-50 border-b border-neutral-200">{children}</thead>,
                                    tbody: ({ children }) => <tbody>{children}</tbody>,
                                    tr: ({ children }) => <tr className="even:bg-neutral-50">{children}</tr>,
                                    th: ({ children }) => <th className="px-3 py-2 text-left text-xs font-semibold uppercase tracking-wide text-neutral-700 border border-neutral-200">{children}</th>,
                                    td: ({ children }) => <td className="px-3 py-2 text-neutral-700 border border-neutral-200">{children}</td>,
                                    blockquote: ({ children }) => (
                                        <blockquote className="mb-4 rounded-2xl border-l-4 border-blue-200 bg-blue-50 px-4 py-3 text-sm text-blue-900">{children}</blockquote>
                                    ),
                                    pre: ({ children }) => (
                                        <pre className="mb-4 overflow-x-auto rounded-2xl bg-neutral-950 p-3 text-xs text-white">{children}</pre>
                                    ),
                                    code: ({ inline, children }) => (
                                        inline ? (
                                            <code className="bg-neutral-100 px-1.5 py-0.5 rounded text-xs font-mono text-neutral-900">{children}</code>
                                        ) : (
                                            <code className="block whitespace-pre-wrap rounded-2xl bg-neutral-950 px-3 py-2 text-xs text-white">{children}</code>
                                        )
                                    ),
                                    a: ({ children, href }) => (
                                        <a
                                            href={href}
                                            className="font-bold text-blue-600 hover:text-blue-700 hover:underline transition-colors"
                                            target={href?.startsWith('/') ? '_self' : '_blank'}
                                            rel={href?.startsWith('/') ? undefined : 'noopener noreferrer'}
                                        >
                                            {children}
                                        </a>
                                    ),
                                }}
                            >
                                {message.content.replace(/(đăng nhập|login)/gi, '[$1](/auth?mode=login)').replace(/(đăng ký|register|tạo tài khoản)/gi, '[$1](/auth?mode=register)')}
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
