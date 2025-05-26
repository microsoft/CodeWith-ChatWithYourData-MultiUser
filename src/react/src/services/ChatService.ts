import { IChat } from "../models/Chat";
import { IChatMessage } from "../models/ChatMessage";
import { env } from "../config/env";
import { IChatMessageRequest } from "../models/ChatMessageRequest";

// Define API response types for better type safety
type ApiResponse<T> = {
    data: T | null;
    error: string | null;
    status: number;
};

export class ChatService {
    private readonly baseUrl = env.BACKEND_URL;
    private readonly requestCache = new Map<string, Promise<any>>();

    // Add utility method for safe JSON parsing with better error details
    private async safeJsonParse<T>(response: Response): Promise<T> {
        try {
            return await response.json() as T;
        } catch (error) {
            console.error('JSON parsing error:', error);
            // Include response details in the error for better debugging
            const responseText = await response.text().catch(() => 'Unable to get response text');
            throw new Error(`Failed to parse response as JSON. Status: ${response.status}. Response text: ${responseText.substring(0, 200)}...`);
        }
    }

    // Helper method to create a fetch request with timeout and abort controller
    private async fetchWithTimeout<T>(
        url: string, 
        options: RequestInit,
        timeoutMs: number = 30000
    ): Promise<ApiResponse<T>> {
        const controller = new AbortController();
        const id = setTimeout(() => controller.abort(), timeoutMs);
        
        try {
            const response = await fetch(url, {
                ...options,
                signal: controller.signal
            });
            
            // For non-OK responses, format the error but don't throw
            if (!response.ok) {
                return {
                    data: null,
                    error: `API error: ${response.status} ${response.statusText}`,
                    status: response.status
                };
            }
            
            // For streaming responses, just return the response wrapped
            if (response.headers.get('content-type')?.includes('text/event-stream') || 
                response.headers.get('content-type')?.includes('application/x-ndjson')) {
                return {
                    data: response as unknown as T,
                    error: null,
                    status: response.status
                };
            }
            
            // Check for empty responses (common in DELETE and PATCH operations)
            const contentLength = response.headers.get('content-length');
            const contentType = response.headers.get('content-type');
            
            // If content length is 0 or no content type, likely empty response
            if (contentLength === '0' || !contentType) {
                return {
                    data: null as unknown as T, // For successful empty responses
                    error: null,
                    status: response.status
                };
            }
            
            // For other responses with content, safely parse JSON
            try {
                const data = await this.safeJsonParse<T>(response);
                return {
                    data,
                    error: null,
                    status: response.status
                };
            } catch (parseError) {
                return {
                    data: null,
                    error: (parseError as Error).message,
                    status: response.status
                };
            }
        } catch (error) {
            // Handle network errors and timeouts
            const errorMessage = error instanceof Error 
                ? error.message 
                : 'Unknown error occurred';
                
            return {
                data: null,
                error: errorMessage.includes('aborted') 
                    ? `Request timed out after ${timeoutMs}ms` 
                    : `Network error: ${errorMessage}`,
                status: 0 // Use 0 for network-level errors
            };
        } finally {
            clearTimeout(id);
        }
    }

    // Cache helper with improved cache invalidation strategy
    private async cachedFetch<T>(
        key: string, 
        fetchCall: () => Promise<ApiResponse<T>>, 
        ttlMs: number = 10000,
        forceRefresh: boolean = false
    ): Promise<ApiResponse<T>> {
        // If forcing refresh or cache doesn't have the key, fetch fresh data
        if (forceRefresh || !this.requestCache.has(key)) {
            const promise = fetchCall();
            this.requestCache.set(key, promise);
            
            // Auto-invalidate the cache after TTL
            promise.finally(() => {
                setTimeout(() => {
                    // Only delete if this promise is still the cached one
                    if (this.requestCache.get(key) === promise) {
                        this.requestCache.delete(key);
                    }
                }, ttlMs);
            });
        }
        
        try {
            return await this.requestCache.get(key) as Promise<ApiResponse<T>>;
        } catch (error) {
            // If the cached promise fails, delete it and retry once
            this.requestCache.delete(key);
            const retryPromise = fetchCall();
            this.requestCache.set(key, retryPromise);
            return retryPromise;
        }
    }

    public getChatsAsync = async (token: string, forceRefresh: boolean = false): Promise<IChat[]> => {
        const cacheKey = `chats_${token}`;
        
        try {
            const response = await this.cachedFetch<IChat[]>(
                cacheKey,
                () => this.fetchWithTimeout<IChat[]>(
                    `${this.baseUrl}/threads`, 
                    {
                        headers: {
                            'Authorization': `Bearer ${token}`,
                            'Content-Type': 'application/json'
                        },
                        // Add cache control headers to prevent browser caching
                        cache: 'no-cache'
                    }
                ),
                10000, // 10 second TTL
                forceRefresh
            );
            
            if (response.error) {
                console.error('Failed to fetch chats:', response.error);
                return [];
            }
            
            return response.data || [];
        } catch (error) {
            console.error('Failed to fetch chats:', error);
            return [];
        }
    };

    public getChatMessagesAsync = async ({chatId, token, forceRefresh = false} : {
        chatId: string, 
        token: string, 
        forceRefresh?: boolean
    }): Promise<IChatMessage[]> => {
        const cacheKey = `messages_${chatId}_${token}`;
        
        try {
            const response = await this.cachedFetch<IChatMessage[]>(
                cacheKey,
                () => this.fetchWithTimeout<IChatMessage[]>(
                    `${this.baseUrl}/threads/${chatId}/messages`,
                    {
                        headers: {
                            'Authorization': `Bearer ${token}`,
                            'Content-Type': 'application/json'
                        },
                        cache: 'no-cache'
                    }
                ),
                5000, // 5 second TTL for messages (shorter since they change frequently)
                forceRefresh
            );
            
            if (response.error) {
                throw new Error(`Failed to fetch messages: ${response.error}`);
            }
            
            return response.data || [];
        } catch (error) {
            console.error('Failed to fetch chat messages:', error);
            throw error;
        }
    };

    public createChatAsync = async ({token} : { token: string}): Promise<IChat> => {
        try {
            const response = await this.fetchWithTimeout<IChat>(
                `${this.baseUrl}/threads`, 
                {
                    method: 'POST',
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json',
                    }
                }
            );
            
            if (response.error) {
                throw new Error(`Failed to create chat: ${response.error}`);
            }
            
            // Invalidate the chats cache
            this.requestCache.delete(`chats_${token}`);
            
            if (!response.data) {
                throw new Error('No data returned from create chat API');
            }
            
            return response.data;
        } 
        catch (error) {
            console.error('Failed to create chat:', error);
            throw error; 
        }
    }

    public deleteMessagesAsync = async ({chatId, token} : {chatId: string, token: string}): Promise<boolean> => {
        try {
            const response = await this.fetchWithTimeout<any>(
                `${this.baseUrl}/threads/${chatId}/messages`, 
                {
                    method: 'DELETE',
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json',
                    }
                }
            );
            
            if (response.error) {
                throw new Error(`Failed to delete messages: ${response.error}`);
            }
            
            // Invalidate the messages cache for this chat
            this.requestCache.delete(`messages_${chatId}_${token}`);
            return true;
        } catch (error) {
            console.error('Failed to delete messages:', error);
            return false;
        }
    }

    public deleteChatAsync = async ({chatId, token} : {chatId: string, token: string}): Promise<boolean> => {
        try {
            const response = await this.fetchWithTimeout<any>(
                `${this.baseUrl}/threads/${chatId}`, 
                {
                    method: 'DELETE',
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json',
                    }
                }
            );
            
            if (response.error) {
                throw new Error(`Failed to delete chat: ${response.error}`);
            }
            
            // Invalidate relevant caches
            this.requestCache.delete(`chats_${token}`);
            this.requestCache.delete(`messages_${chatId}_${token}`);
            return true;
        } catch (error) {
            console.error('Failed to delete chat:', error);
            return false;
        }
    }

    public sendMessageAsync = async ({chatId, message, token} : { 
        chatId: string, 
        message: IChatMessageRequest, 
        token: string 
    }): Promise<Response> => {
        try {
            // Use regular fetch for streaming responses
            const response = await fetch(`${this.baseUrl}/threads/${chatId}/messages`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ message: message }),
            });

            if (!response.ok) {
                throw new Error(`Error sending message: ${response.status} ${response.statusText}`);
            }
            
            // Invalidate the messages cache for this chat
            this.requestCache.delete(`messages_${chatId}_${token}`);
            return response;
        } 
        catch (error) {
            console.error('Failed to send message:', error);
            throw error;
        }
    }    
      public sendMessageStreamAsync = async ({chatId, message, token} : { 
        chatId: string, 
        message: IChatMessageRequest, 
        token: string 
    }): Promise<Response> => {
        try {
            // Use regular fetch for streaming responses with the new stream endpoint
            const response = await fetch(`${this.baseUrl}/threads/${chatId}/messages/stream`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({message: message.message, documentIds: message.documentIds, tools: message.tools}),
            });

            if (!response.ok) {
                throw new Error(`Error sending message stream: ${response.status} ${response.statusText}`);
            }
            
            // Invalidate the messages cache for this chat
            this.requestCache.delete(`messages_${chatId}_${token}`);
            
            // Return the response with the stream
            // The stream will contain followupQuestions in the final message with final:true
            // The stream processor will need to parse these from the 'data: {...}' events
            return response;
        } 
        catch (error) {
            console.error('Failed to send message stream:', error);
            throw error;
        }
    }

    public sendCompliancyMessageStreamAsync = async ({chatId, message, token} : { 
        chatId: string, 
        message: string, 
        token: string 
    }): Promise<Response> => {
        try {
            // Use regular fetch for streaming responses
            const response = await fetch(`${this.baseUrl}/threads/${chatId}/messages/compliancy/stream`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify({ message: message }),
            });

            if (!response.ok) {
                throw new Error(`Error sending compliancy message: ${response.status} ${response.statusText}`);
            }
            
            // Invalidate the messages cache for this chat
            this.requestCache.delete(`messages_${chatId}_${token}`);
            return response;
        } 
        catch (error) {
            console.error('Failed to send compliancy message:', error);
            throw error;
        }
    }

    public updateChatNameAsync = async ({chatId, name, token} : {
        chatId: string, 
        name: string, 
        token: string
    }): Promise<boolean> => {
        try {
            const response = await this.fetchWithTimeout<any>(
                `${this.baseUrl}/threads/${chatId}`, 
                {
                    method: 'PATCH',
                    headers: {
                        'Authorization': `Bearer ${token}`,
                        'Content-Type': 'application/json',
                    },
                    // Send the name directly as a string value, not wrapped in a JSON object
                    body: JSON.stringify(name),
                }
            );
            
            if (response.error) {
                throw new Error(`Failed to update chat name: ${response.error}`);
            }
            
            // Invalidate the chats cache
            this.requestCache.delete(`chats_${token}`);
            return true;
        } catch (error) {
            console.error('Failed to update chat name:', error);
            return false;
        }
    }

    // Utility method to clear all caches (useful when logging out)
    public clearCaches(): void {
        this.requestCache.clear();
    }
}