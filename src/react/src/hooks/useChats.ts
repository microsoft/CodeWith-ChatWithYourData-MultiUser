import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { ChatService } from "../services/ChatService";
import { useAuth } from "./useAuth";

export const useChats = () => {

    const [selectedChatId, setSelectedChatId] = useState<string | undefined>(undefined);
    const {userId, accessToken} = useAuth();   

    const queryClient = useQueryClient();
    const chatService = new ChatService();

    const { isPending, error, data: chats } = useQuery({
        queryKey: ['chats'],
        queryFn: async () => chatService.getChatsAsync(userId, accessToken),
        enabled: userId != undefined && accessToken != undefined && accessToken != "",
    });

    const { mutateAsync: addChat} = useMutation({
        mutationFn: () => chatService.createChatAsync({userId, token: accessToken}),
        onError: () => {
            console.log('Failed to create a chat.');
        },
        onSuccess: (data) => {
            queryClient.invalidateQueries({ queryKey: ['chats'] });
            selectChat(data.id);
        }
    });

    const { mutateAsync: deleteChat} = useMutation({
        mutationFn: ({chatId} : { chatId: string}) => chatService.deleteChatAsync({chatId, userId, token: accessToken}),
        onError: () => {
            console.log('Failed to delete chat.');
        },
        onSuccess: (data) => {
            if(data){
                queryClient.invalidateQueries({ queryKey: ['chats'] });
                selectChat();
            }
        }
    });

    const selectChat = (chatId?: string) => {
        if (chatId) {
            setSelectedChatId(chatId);
        } else {
            setSelectedChatId(undefined);
        }
    };

    return {
        isPending,
        error,
        chats,
        selectChat,
        selectedChatId,
        addChat,
        deleteChat
    }
}