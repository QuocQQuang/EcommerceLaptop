'use client';

import { useState } from 'react';
import { useSession } from 'next-auth/react';
import { useChatBot } from '@/hooks/useChatBot';
import { ChatbotFAB } from './ChatbotFAB';
import { ChatbotWindow } from './ChatbotWindow';

export function ChatbotProvider() {
    const { data: session } = useSession();
    const userToken = (session as any)?.user?.accessToken || null;

    const {
        isOpen,
        setIsOpen,
        messages,
        isStreaming,
        sendMessage,
        isConnected,
        statusMessage
    } = useChatBot(userToken);

    const [hasUnread, setHasUnread] = useState(false);

    const handleToggle = () => {
        setIsOpen(!isOpen);
        if (!isOpen) {
            setHasUnread(false);
        }
    };

    const handleClearChat = () => {
        // Clear messages by reloading the page or implementing a clear function
        window.location.reload();
    };

    return (
        <>
            <ChatbotFAB
                isOpen={isOpen}
                onClick={handleToggle}
                hasUnread={hasUnread}
            />
            <ChatbotWindow
                isOpen={isOpen}
                messages={messages}
                isStreaming={isStreaming}
                statusMessage={statusMessage}
                onClose={() => setIsOpen(false)}
                onSendMessage={sendMessage}
                onClearChat={handleClearChat}
            />
        </>
    );
}
