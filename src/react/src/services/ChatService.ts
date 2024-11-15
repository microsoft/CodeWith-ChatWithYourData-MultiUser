import { IChat } from "../models/Chat";
import { IChatMessage } from "../models/ChatMessage";

export class ChatService {

    private readonly baseUrl = process.env.VITE_BACKEND_URL;
    private readonly MAX_RETRIES = 3;

    public getChatsAsync = async (token: string): Promise<IChat[]> => {

        try {
            const response = await fetch(`${this.baseUrl}/threads`, {
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

    public getChatMessagesAsync = async ({chatId, token} : {chatId: string, token: string}): Promise<IChatMessage[]> => {
        try {
            const response = await fetch(`${this.baseUrl}/threads/${chatId}/messages`,
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

    public createChatAsync = async ({token} : { token: string}): Promise<IChat> => {

        try {
            const response = await fetch(`${this.baseUrl}/threads`, {
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

    public deleteMessagesAsync = async ({chatId, token } : {chatId: string, token: string}): Promise<boolean> => {

        try {
            const response = await fetch(`${this.baseUrl}/threads/${chatId}/messages`, {
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

    public deleteChatAsync = async ({chatId, token } : {chatId: string, token: string}): Promise<boolean> => {

        try {
            const response = await fetch(`${this.baseUrl}/threads/${chatId}`, {
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

    public sendMessageAsync = async ({chatId, message, token} : { chatId: string, message: string, token: string }): Promise<Response> => 
    {
        let retries = 0;
        while (retries < this.MAX_RETRIES) 
        {
            try 
            {
                const response = await fetch(`${this.baseUrl}/threads/${chatId}/messages`, {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify({ message: message }),
                });

                if (response.status === 429) {
                    const retryAfter = response.headers.get('retry-after');
                    const delay = retryAfter ? parseInt(retryAfter, 10) * 1000 : Math.pow(2, retries) * 1000;
                    debugger;
                    await new Promise(resolve => setTimeout(resolve, delay));
                    retries++;
                } else {
                    return response;
                }
            } 
            catch (error) {
                console.error('Failed to send message:', error);
                throw error;
            }
        }
        throw new Error('Failed to send message after multiple attempts due to rate limiting.');
    }
}