import { IChat } from "../models/Chat";
import { IChatMessage } from "../models/ChatMessage";

export class ChatService {

    private readonly baseUrl = import.meta.env.VITE_BACKEND_URL;

    public getChatsAsync = async (userId: string, token: string): Promise<IChat[]> => {

        try {
            const response = await fetch(`${this.baseUrl}/threads?userId=${userId}`, {
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json'
                }
            });
            if (!response.ok) {
                throw new Error(`Error fetching chats: ${response.statusText}`);
            }
            const chats: IChat[] = await response.json();
            return chats;
        } catch (error) {
            console.error('Failed to fetch chats:', error);
        }
        return [];
    };

    public getChatMessagesAsync = async ({chatId, userId, token} : {chatId: string, userId: string, token: string}): Promise<IChatMessage[]> => {
        try {
            const response = await fetch(`${this.baseUrl}/threads/${chatId}/messages?userId=${userId}`,
                {
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json'
                    }
                }
            );
            if (!response.ok) {
                throw new Error(`Error fetching chat: ${response.statusText}`);
            }
            const messages: IChatMessage[] = await response.json();
            return messages;
        } catch (error) {
            console.error('Failed to fetch chats:', error);
            throw error;
        }
    };

    public createChatAsync = async ({userId, token} : {userId: string, token: string}): Promise<IChat> => {

        try {
            const response = await fetch(`${this.baseUrl}/threads?userId=${userId}`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json',
                }
            });
            if (!response.ok) {
                throw new Error(`Error creating chat: ${response.statusText}`);
            }
            const chat: IChat = await response.json();
            return chat;
        } catch (error) {
            console.error('Failed to create chat:', error);
            throw error; 
        }
    }

    public deleteChatAsync = async ({chatId, userId, token } : {chatId: string, userId: string, token: string}): Promise<boolean> => {

        try {
            const response = await fetch(`${this.baseUrl}/threads/${chatId}?userId=${userId}`, {
                method: 'DELETE',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json',
                }
            });
            if (!response.ok) {
                throw new Error(`Error deleting chat: ${response.statusText}`);
            }
            return true;
        } catch (error) {
            console.error('Failed to create chat:', error);
            return false;
        }
    }

    public sendMessageAsync = async ({chatId, message, userId, token} : { chatId: string, message: string, userId: string, token: string }): Promise<Response> => {

        try{
            const response = await fetch(`${this.baseUrl}/threads/${chatId}/messages`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ message: message, userId: userId }),
            });
            return response;
        } catch( error ) {
            console.error('Failed to send message:', error);
            throw error;
        }
    }
}