import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ChatService } from "../services/ChatService";
import { useAuth } from "./useAuth";
import LoggingService from "../services/LoggingService";

export const useChats = () => {

    const [selectedChatId, setSelectedChatId] = useState<string | undefined>(undefined);
    const {userId, accessToken} = useAuth();   

    const queryClient = useQueryClient();
    const chatService = new ChatService();

    const { isPending, error, data: chats } = useQuery({
        queryKey: ['chats'],
        queryFn: async () => chatService.getChatsAsync(accessToken),
        enabled: userId != undefined && accessToken != undefined && accessToken != "",
    });    const { mutateAsync: addChat} = useMutation({
        mutationFn: () => chatService.createChatAsync({token: accessToken}),
        onError: () => {
            LoggingService.warn('Failed to create a chat.');
        },
        onSuccess: (data) => {
            // Make sure to invalidate queries first
            queryClient.invalidateQueries({ queryKey: ['chats'] });
            
            // Explicitly log and set the chat ID
            LoggingService.log("Chat created successfully, ID:", data.id);
            
            // Use setTimeout to ensure this happens after React updates
            setTimeout(() => {
                selectChat(data.id);
            }, 0);
        }
    });    const { mutateAsync: deleteChat} = useMutation({
        mutationFn: ({chatId} : { chatId: string}) => chatService.deleteChatAsync({chatId, token: accessToken}),
        onError: () => {
            LoggingService.warn('Failed to delete chat.');
        },
        onSuccess: (data) => {
            if(data){
                queryClient.invalidateQueries({ queryKey: ['chats'] });
                selectChat();
            }
        }
    });    const { mutateAsync: updateChatName } = useMutation({
        mutationFn: ({chatId, name}: { chatId: string, name: string }) => 
            chatService.updateChatNameAsync({chatId, name, token: accessToken}),
        onError: () => {
            LoggingService.warn('Failed to update chat name.');
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['chats'] });
        }
    });    const selectChat = (chatId?: string) => {
        // Log the change to help debug issues
        LoggingService.log(`Selecting chat: ${chatId || 'none'}`);
        
        if (chatId) {
            // When selecting a new chat, force query invalidation to ensure fresh data
            queryClient.invalidateQueries({ queryKey: ['chat', chatId] });
            setSelectedChatId(chatId);
        } else {
            setSelectedChatId(undefined);
        }
    };// Find the selected chat from the chats array using selectedChatId
    const selectedChat = chats?.find(chat => chat.id === selectedChatId);
    
    return {
        isPending,
        error,
        chats,
        selectChat,
        selectedChatId,
        selectedChat,
        addChat,
        deleteChat,
        updateChatName
    }
}