import { useState, useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { v4 as uuidv4 } from 'uuid';
import { ChatMessage, EnrichedProduct } from '../types/chat';
import { useCart } from './useCart';

// Use environment variable for API base URL, fallback to localhost for development
const API_BASE = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5129';
const HUB_URL = `${API_BASE.replace('/api', '')}/chatHub`;

export const useChatBot = (userToken?: string | null) => {
    const { refreshCart } = useCart();
    const [isOpen, setIsOpen] = useState(false);
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [isStreaming, setIsStreaming] = useState(false);
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const [isConnected, setIsConnected] = useState(false);
    const [statusMessage, setStatusMessage] = useState<string>('');

    // Refs for safe access in callbacks
    const isStreamingRef = useRef(false);
    const messagesRef = useRef<ChatMessage[]>([]);
    const currentSessionIdRef = useRef<string | null>(null);

    // Sync ref with state
    useEffect(() => {
        messagesRef.current = messages;
    }, [messages]);

    // Initialize Connection
    useEffect(() => {
        if (!isOpen) return; // Only connect when chat is opened

        // Get or create guest ID
        let guestId = localStorage.getItem('guest_id');
        if (!guestId) {
            guestId = uuidv4();
            localStorage.setItem('guest_id', guestId);
        }

        const buildConnection = () => {
            // Append guest_id to query string for WebSockets support
            const url = userToken
                ? HUB_URL
                : `${HUB_URL}?guest_id=${guestId}`;

            const builder = new signalR.HubConnectionBuilder()
                .withUrl(url, {
                    accessTokenFactory: () => userToken || '', // Handles Auth
                })
                .withAutomaticReconnect()
                .configureLogging(signalR.LogLevel.Information);

            return builder.build();
        };

        const newConnection = buildConnection();

        setConnection(newConnection);

        return () => {
            newConnection.stop();
        };
    }, [isOpen, userToken]);

    // Handle Connection Events
    useEffect(() => {
        if (!connection) return;

        const startConnection = async () => {
            try {
                await connection.start();
                console.log(' Connected to ChatHub');
                setIsConnected(true);

                // --- Event Listeners ---

                // --- Event Listeners ---
                // Backend sends generic 'ReceiveEvent' with payload
                connection.on('ReceiveEvent', (evt: any) => {
                    // evt.type might be "token", "product", "status", "error" etc.
                    const type = evt.type?.toLowerCase();

                    if (type === 'token') {
                        const token = evt.token;
                        setIsStreaming(true);
                        isStreamingRef.current = true;

                        setMessages(prev => {
                            const lastMsg = prev[prev.length - 1];
                            if (lastMsg && lastMsg.role === 'assistant' && lastMsg.isStreaming) {
                                return [...prev.slice(0, -1), { ...lastMsg, content: lastMsg.content + token }];
                            }
                            return prev;
                        });
                    }
                    else if (type === 'product') {
                        // Backend sends individual ProductEvent or list? 
                        // Check ChatStreamEvents.cs: ProductEvent is a single event.
                        // But frontend expects list in bubble. 
                        // We might need to accumulate products or check how backend streams them.
                        // RagChatService.cs yields new ProductEvent... so we get one by one?
                        // Frontend MessageBubble expects products array. 
                        // Let's assume we append to the list of products in the message.
                        console.log(' Product Event received:', evt);

                        const product = {
                            id: evt.id || evt.Id,
                            name: evt.name || evt.Name,
                            price: evt.price || evt.Price,
                            thumbnailUrl: evt.imageUrl || evt.ImageUrl,
                            inStock: true,
                            slug: evt.slug || evt.Slug || evt.id || evt.Id
                        } as EnrichedProduct;

                        setMessages(prev => {
                            const lastMsg = prev[prev.length - 1];
                            if (lastMsg && lastMsg.role === 'assistant') {
                                const products = lastMsg.products || [];
                                // Avoid duplicates
                                if (!products.find(p => p.id === product.id)) {
                                    return [...prev.slice(0, -1), { ...lastMsg, products: [...products, product] }];
                                }
                            }
                            return prev;
                        });
                    }
                    else if (type === 'status' || type === 'progress' || type === 'complete' || type === 'metadata') {
                        if (evt.message) {
                            setStatusMessage(evt.message);
                        }

                        // Capture Session ID
                        if (type === 'metadata' && evt.sessionId) { // CamelCase from JSON
                            console.log(' Session Established:', evt.sessionId);
                            currentSessionIdRef.current = evt.sessionId;
                        }

                        // Check for completion or error
                        if (evt.message === 'completed' || type === 'error' || type === 'complete') {
                            setIsStreaming(false);
                            setStatusMessage(''); // Clear status
                            isStreamingRef.current = false;
                            setMessages(prev => {
                                const lastMsg = prev[prev.length - 1];
                                if (lastMsg) return [...prev.slice(0, -1), { ...lastMsg, isStreaming: false }];
                                return prev;
                            });
                        }
                    }
                    else if (type === 'tool' || type === 'cart') {
                        // Cart tool response detected - refresh cart to sync with backend
                        console.log(' Cart tool response received, refreshing cart...');

                        // Debounce cart refresh to avoid excessive API calls
                        setTimeout(() => {
                            refreshCart().catch(err => {
                                console.error('Failed to refresh cart after tool response:', err);
                            });
                        }, 500);
                    }
                    else if (type === 'error') {
                        setIsStreaming(false);
                        setMessages(prev => [...prev.slice(0, -1), {
                            id: uuidv4(),
                            role: 'assistant',
                            content: `Error: ${evt.message}`,
                            isStreaming: false,
                            timestamp: new Date()
                        }]);
                    }
                });

                // Keep-alive or Welcome logic could go here

                connection.onclose(() => {
                    console.warn(' Connection closed');
                    setIsConnected(false);
                    setIsStreaming(false);
                    setStatusMessage('');
                });

            } catch (err) {
                console.error(' Connection failed: ', err);
                setIsConnected(false);
                setIsStreaming(false);
            }
        };

        startConnection();

        return () => {
            connection.off('ReceiveEvent');
            // connection.onclose cleanup not strictly needed as connection stops
            // connection.off('ReceiveToken'); // Old listeners
        };
    }, [connection]);

    const sendMessage = useCallback(async (content: string) => {
        if (!connection || !isConnected) return;

        const newMessage: ChatMessage = {
            id: uuidv4(),
            role: 'user',
            content,
            timestamp: new Date()
        };

        // UI Optimistic Update
        setMessages(prev => [...prev, newMessage]);

        // Add placeholder for Assistant response immediately
        const placeholder: ChatMessage = {
            id: uuidv4(),
            role: 'assistant',
            content: '',
            isStreaming: true,
            timestamp: new Date()
        };
        setMessages(prev => [...prev, placeholder]);
        setIsStreaming(true);

        try {
            // Get guest ID again just in case (or rely on connection context)
            let guestId = localStorage.getItem('guest_id') || "anonymous";

            // Invoke Hub Method
            // Signature: SendQuery(string query, QueryOptions? options = null)
            const sessionId = currentSessionIdRef.current;
            await connection.invoke('SendQuery', content, { sessionId });

        } catch (e) {
            console.error('Send failed', e);
            // Remove placeholder or show error
            setMessages(prev => [...prev.slice(0, -1), { // Replace placeholder with error
                id: uuidv4(),
                role: 'assistant',
                content: 'Sorry, I failed to send that message.',
                timestamp: new Date(),
                isStreaming: false
            }]);
            setIsStreaming(false);
        }
    }, [connection, isConnected]);

    return {
        isOpen,
        setIsOpen,
        messages,
        isStreaming,
        sendMessage,
        isConnected,
        statusMessage
    };
};
