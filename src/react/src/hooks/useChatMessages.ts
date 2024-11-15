import { useQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { ChatService } from "../services/ChatService";
import { IChatMessage } from "../models/ChatMessage";
import { useAuth } from "./useAuth";

export const useChatMessages = (chatId: string | undefined) => {

    const chatService = new ChatService();
    const {userId, accessToken} = useAuth();   


    const [messages, setMessages] = useState<IChatMessage[]>([]);

    const { isPending: chatPending, error: chatError, data: messagesResult } = useQuery({
        queryKey: ['chat', chatId],
        queryFn: async () => chatService.getChatMessagesAsync({chatId: chatId || "", token: accessToken}),
        enabled: userId != undefined && accessToken != undefined && accessToken != "" && chatId != "" && chatId != undefined,
        staleTime: 10000
    });

    useEffect(() => {
        if (messagesResult) {
            setMessages(messagesResult.filter(message => message.role !== 'system'));

        }
    }, [messagesResult])


    const sendMessage = async ({ message }: { message: string }) => {

        if(!chatId) return false; 
        let result = '';
        setMessages(prev => {
            const updated = [...prev];
            updated.push({
                role: 'user',
                content: message
            },
                {
                    role: 'assistant',
                    content: result
                });
            return updated;
        });

        const response = await chatService.sendMessageAsync({chatId: chatId, message: message, token: accessToken});
        
        if (!response || !response.body) {
            return false;
        }

        const reader = response.body.getReader();
        const decoder = new TextDecoder();
        const loop = true;
        while (loop)
        {
            const { value, done } = await reader.read();
            if (done) {
                break;
            }
            const decodedChunk = decoder.decode(value);
            const chunk = JSON.parse(decodedChunk);
            result += chunk.message.content
            setMessages(prev => {
                const updated = [...prev];
                updated[updated.length - 1] = {
                    role: 'assistant',
                    content: result,
                    followupquestions:chunk.context.followup_questions

                };
                return updated;
            });
        }
        // Check if message is filled, otherwise stop
        if(result == ''){
            setMessages(prev => {
                const updated = [...prev];
                updated.pop();
                updated.pop();
                return updated;
            });
            return false;
        }
        return true;
    };

    const deleteMessages = async () => {
        if(!chatId) return false; 
        const response = await chatService.deleteMessagesAsync({chatId: chatId, token: accessToken});
        
        if (!response) {
            return false;
        }
        setMessages([]);
        return true;
    };

    return {
        chatPending,
        chatError,
        messages,
        sendMessage,
        deleteMessages  
    };

}