import { useState, useEffect, useRef, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import { v4 as uuidv4 } from 'uuid';
import { ChatMessage, EnrichedProduct } from '../types/chat';

const HUB_URL = 'http://localhost:5129/chatHub';

export const useChatBot = (userToken?: string | null) => {
    const [isOpen, setIsOpen] = useState(false);
    const [messages, setMessages] = useState<ChatMessage[]>([]);
    const [isStreaming, setIsStreaming] = useState(false);
    const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
    const [isConnected, setIsConnected] = useState(false);

    // Refs for safe access in callbacks
    const isStreamingRef = useRef(false);
    const messagesRef = useRef<ChatMessage[]>([]);

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

                connection.on('ReceiveToken', (token: string) => {
                    setIsStreaming(true);
                    isStreamingRef.current = true;

                    setMessages(prev => {
                        const lastMsg = prev[prev.length - 1];
                        if (lastMsg && lastMsg.role === 'assistant' && lastMsg.isStreaming) {
                            // Append to existing streaming message
                            const updatedMsg = { ...lastMsg, content: lastMsg.content + token };
                            return [...prev.slice(0, -1), updatedMsg];
                        } else {
                            // Start new message chunk (rare case if sync is off)
                            // Usually we push a placeholder before, so this implies finding the last one
                            return prev;
                        }
                    });
                });

                connection.on('ReceiveProductContext', (productsJson: string) => {
                    try {
                        const products: EnrichedProduct[] = JSON.parse(productsJson);
                        setMessages(prev => {
                            const lastMsg = prev[prev.length - 1];
                            if (lastMsg && lastMsg.role === 'assistant') {
                                return [...prev.slice(0, -1), { ...lastMsg, products }];
                            }
                            return prev;
                        });
                    } catch (e) {
                        console.error('Failed to parse products', e);
                    }
                });

                connection.on('ReceiveStatus', (status: string) => {
                    // e.g. "thinking", "searching", "completed", "error"
                    if (status === 'completed' || status === 'error') {
                        setIsStreaming(false);
                        isStreamingRef.current = false;

                        // Mark last message as not streaming
                        setMessages(prev => {
                            const lastMsg = prev[prev.length - 1];
                            if (lastMsg) {
                                return [...prev.slice(0, -1), { ...lastMsg, isStreaming: false }];
                            }
                            return prev;
                        });
                    }
                });

                // Keep-alive or Welcome logic could go here
            } catch (err) {
                console.error(' Connection failed: ', err);
                setIsConnected(false);
            }
        };

        startConnection();

        return () => {
            connection.off('ReceiveToken');
            connection.off('ReceiveProductContext');
            connection.off('ReceiveStatus');
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
            // Signature: SendMessage(string user, string message, string? guestId)
            // Or typically we send an object.
            // Based on backend implementation: 
            // await Clients.Caller.SendAsync("ReceiveToken", ...)

            // We need to match the Hub method name. Usually 'SendMessage'.
            // Ensure we pass the right args.
            await connection.invoke('SendMessage', "user", content);

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
        isConnected
    };
};
