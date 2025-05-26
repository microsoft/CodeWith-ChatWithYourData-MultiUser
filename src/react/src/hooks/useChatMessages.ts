import { useQuery } from "@tanstack/react-query";
import { useEffect, useState, useCallback } from "react";
import { ChatService } from "../services/ChatService";
import { IChatMessage } from "../models/ChatMessage";
import { useAuth } from "./useAuth";
import LoggingService from "../services/LoggingService";
import { IChatMessageRequest } from "../models/ChatMessageRequest";
import { useQueryClient } from "@tanstack/react-query";

export const useChatMessages = (chatId: string | undefined) => {
    const chatService = new ChatService();
    const { userId, accessToken, refreshToken } = useAuth();    const queryClient = useQueryClient();

    const [messages, setMessages] = useState<IChatMessage[]>([]);
    const [heartbeatStatus, setHeartbeatStatus] = useState<{
        content: string;
        agentName: string;
        timestamp: string;
        isActive: boolean;
    } | null>(null);
    
    // Function to force refresh messages for a specific chat
    const forceRefreshMessages = useCallback(() => {
        if (chatId) {
            LoggingService.log(`Forcing refresh of messages for chat ${chatId}`);
            queryClient.invalidateQueries({ queryKey: ['chat', chatId] });
        }    }, [chatId, queryClient]);const { isPending: chatPending, error: chatError, data: messagesResult } = useQuery({
        queryKey: ['chat', chatId],
        queryFn: async () => {
            try {
                const result = await chatService.getChatMessagesAsync({ chatId: chatId || "", token: accessToken });
                LoggingService.log(`Query succeeded for chat ${chatId}, received ${result.length} messages`);
                return result;
            } catch (error) {
                // Handle auth errors by attempting token refresh
                await handlePossibleAuthError(error);
                // Still need to throw to let React Query handle the error state
                throw error;
            }
        },
        enabled: userId != undefined && accessToken != undefined && accessToken != "" && chatId != "" && chatId != undefined,
        staleTime: 10000,
        // Ensure we're always getting fresh data when switching chats
        refetchOnMount: 'always',
        // Retry failed queries to handle token refresh
        retry: 2,
        retryDelay: 1000
    });    // Combined effect to handle both chatId changes and messagesResult updates
    useEffect(() => {
        // When chatId changes, immediately clear messages
        if (!chatId) {
            setMessages([]);
            return;
        }
        
        // When messagesResult is available, update messages
        // This will run both when chatId changes (causing a new query)
        // and when messagesResult updates from the query
        if (messagesResult) {
            LoggingService.log(`Setting ${messagesResult.length} messages for chat ${chatId}`);
            
            // Check if messages are empty when they shouldn't be (possible caching issue)
            if (messagesResult.length === 0 && chatId) {
                LoggingService.warn(`Received empty messages for chat ${chatId}, may need to force refresh`);
                // Try a delayed refresh if we get empty results unexpectedly
                // This helps in cases where the cache might be stale
                setTimeout(() => forceRefreshMessages(), 500);
            }
            
            setMessages(messagesResult.filter(message => message.role !== 'system'));
        } else if (chatId) {
            // If we have a chatId but no messagesResult yet, clear messages
            // This creates a nice loading state
            LoggingService.log(`Clearing messages while loading chat ${chatId}`);
            setMessages([]);
        }
    }, [chatId, messagesResult, forceRefreshMessages]);// Helper function to handle possible auth errors
    const handlePossibleAuthError = async (error: unknown) => {
        // Check if this is an auth error (401 Unauthorized)
        const errorObj = error as { message?: string; status?: number; response?: { status?: number } };
        
        if (
            (errorObj.message && errorObj.message.includes('401')) || 
            errorObj.status === 401 || 
            (errorObj.response && errorObj.response.status === 401)
        ) {
            LoggingService.warn('Auth error detected, attempting token refresh');
            
            // Try to refresh the token
            refreshToken();
            
            // Invalidate relevant queries to force refetching with the new token
            setTimeout(() => {
                queryClient.invalidateQueries({ queryKey: ['chat', chatId] });
                LoggingService.log('Invalidated queries after token refresh');
            }, 1000); // Small delay to ensure token refresh has completed
        }
    };

    // Existing sendMessage implementation
    const sendMessage = async ({ message }: { message: IChatMessageRequest }) => {
        if (!chatId) return false; 
        let result = '';
        setMessages(prev => {
            const updated = [...prev];
            updated.push(
                {
                    role: 'user',
                    content: message.message
                },
                {
                    role: 'assistant',
                    content: result
                }
            );
            return updated;
        });

        try {
            const response = await chatService.sendMessageAsync({ chatId: chatId, message: message, token: accessToken });
            
            if (!response || !response.body) {
                return false;
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let jsonBuffer = '';
            
            while (true) {
                const { value, done } = await reader.read();
                if (done) {
                    break;
                }
                
                const decodedChunk = decoder.decode(value);
                jsonBuffer += decodedChunk;
                
                try {
                    // Try to parse complete JSON objects
                    const lines = jsonBuffer.split('\n').filter(line => line.trim() !== '');
                    
                    // Process complete lines
                    for (let i = 0; i < lines.length - 1; i++) {
                        try {
                            const chunk = JSON.parse(lines[i]);
                            if (chunk.content) {
                                result += chunk.content;
                                setMessages(prev => {
                                    const updated = [...prev];
                                    updated[updated.length - 1] = {
                                        role: 'assistant',
                                        content: result,
                                        context: chunk.context,
                                        id: chunk.id,
                                        created: chunk.created
                                    };
                                    return updated;
                                });
                            }
                        } catch (parseError) {
                            console.error('Error parsing JSON line:', parseError);
                        }
                    }
                    
                    // Keep the last potentially incomplete line in the buffer
                    jsonBuffer = lines[lines.length - 1] || '';
                } catch (error) {
                    console.error('Error processing chunks:', error);
                    // Continue to next chunk, don't break the stream processing
                }
            }
            
            // Try to parse any remaining data in the buffer
            if (jsonBuffer.trim()) {
                try {
                    const chunk = JSON.parse(jsonBuffer);
                    if (chunk.content) {
                        result += chunk.content;
                        setMessages(prev => {
                            const updated = [...prev];
                            updated[updated.length - 1] = {
                                role: 'assistant',
                                content: result,
                                context: chunk.context,
                                id: chunk.id,
                                created: chunk.created
                            };
                            return updated;
                        });
                    }
                } catch (error) {
                    console.error('Error parsing final JSON chunk:', error);
                }
            }

            // If message is still empty, remove the two added messages
            if (result === '') {
                setMessages(prev => {
                    const updated = [...prev];
                    updated.pop();
                    updated.pop();
                    return updated;
                });
                return false;
            }
            return true;
        } catch (error) {
            console.error('Error sending message:', error);
            handlePossibleAuthError(error);
            return false;
        }
    };    // Method to handle message streaming with SSE format
    const sendMessageStream = async ({ message }: { message: IChatMessageRequest }) => {
        if (!chatId) return false; 
        let result = '';
        
        // Add initial messages for UI feedback
        setMessages(prev => [
            ...prev,
            { role: 'user', content: message.message },
            { role: 'assistant', content: '' }
        ]);

        try {
            const response = await chatService.sendMessageStreamAsync({ 
                chatId: chatId, 
                message: message, 
                token: accessToken 
            });
            
            if (!response || !response.body) {
                return false;
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';
            
            while (true) {
                const { value, done } = await reader.read();
                if (done) {
                    break;
                }
                
                const decodedChunk = decoder.decode(value);
                buffer += decodedChunk;
                
                // Process SSE format (data: {...})
                const lines = buffer.split('\n');
                let processedUpTo = 0;
                
                // Look for complete SSE messages
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i].trim();
                    processedUpTo = i;
                    
                    if (line.startsWith('data: ')) 
                    {
                        try 
                        {
                            // Extract the JSON part after "data: "
                            const jsonStr = line.substring(6);
                            
                            // Handle potential multi-line JSON strings in SSE data
                            try 
                            {
                                const eventData = JSON.parse(jsonStr);
                                
                                // Handle message content                                
                                if (eventData.content) 
                                {                                    
                                    LoggingService.log('Received message chunk');
                                    
                                    // Always append new message content
                                    if (eventData.isReset) 
                                    {
                                        // Reset the content if explicitly requested
                                        result = eventData.content;
                                    } 
                                    else 
                                    {
                                        // Otherwise append to existing content
                                        result += eventData.content;
                                    }
                                    
                                    // Check if this is the final message containing followupQuestions
                                    const contextData = eventData.context || {};
                                    
                                    // If eventData has followupQuestions and final=true, add to context                                    
                                    if (eventData.final === true && eventData.followupQuestions) 
                                    {
                                        LoggingService.log('Found final message with followup questions:', eventData.followupQuestions);
                                        if (!contextData.followup_questions) 
                                        {
                                            contextData.followup_questions = eventData.followupQuestions;
                                            result = eventData.content;
                                        }
                                    }
                                     // If eventData has followupQuestions and final=true, add to context                                    
                                    if (eventData.final === true && eventData.usageMetrics) 
                                    {
                                        LoggingService.log('Found final message with usage metrics:', eventData.usageMetrics);
                                        if (!contextData.usage_metrics) 
                                        {
                                            contextData.usage_metrics = eventData.usageMetrics;
                                            result = eventData.content;
                                        }
                                    }
                                    
                                    setMessages(prev => {
                                        const updated = [...prev];
                                        // Update the last assistant message
                                        updated[updated.length - 1] = {
                                            role: 'assistant',
                                            content: result,
                                            context: contextData,
                                            id: eventData.id || '',
                                            created: eventData.timestamp || new Date().toISOString()
                                        };
                                        return updated;
                                    });                            }                            }                              catch (_) {
                                // This might be a multi-line SSE message that has been split
                                LoggingService.warn('Possible multi-line SSE data, continuing to collect data');
                                // Just skip this line - it will be handled when we receive more data
                            }
                        } 
                        catch (parseError) {
                            console.error('Error parsing SSE data:', parseError, 'Line:', line);
                        }
                    }
                }
                
                // Keep any unprocessed data in the buffer
                if (processedUpTo < lines.length - 1) {
                    buffer = lines.slice(processedUpTo + 1).join('\n');
                } else {
                    buffer = '';
                }
            }
            
            // If we get here, the stream has ended normally
            console.log('Message stream completed');
            
            // If result is still empty, remove the two added messages
            if (result === '') {
                console.warn('No content received from message stream');
                setMessages(prev => prev.slice(0, -2));
                return false;
            }
            
            return true;
        } catch (error) {
            console.error('Error sending message:', error);
            handlePossibleAuthError(error);
            // Remove the messages if there was an error
            setMessages(prev => prev.slice(0, -2));
            return false;
        }
    };
    
    // Method to handle compliancy streaming with special SSE format
    const sendCompliancyMessageStream = async ({ message }: { message: string }) => {
        if (!chatId) return false; 
        let result = '';
        
        // Add initial messages for UI feedback
        setMessages(prev => [
            ...prev,
            { role: 'user', content: message },
            { role: 'assistant', content: '' }
        ]);

        try {
            const response = await chatService.sendCompliancyMessageStreamAsync({ 
                chatId: chatId, 
                message: message, 
                token: accessToken 
            });
            
            if (!response || !response.body) {
                return false;
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';
            
            while (true) {
                const { value, done } = await reader.read();
                if (done) {
                    break;
                }
                
                const decodedChunk = decoder.decode(value);
                buffer += decodedChunk;
                  // Process SSE format (data: {...})
                const lines = buffer.split('\n');
                let processedUpTo = 0;
                
                // Look for complete SSE messages
                for (let i = 0; i < lines.length; i++) {
                    const line = lines[i].trim();
                    processedUpTo = i;
                      if (line.startsWith('data: ')) {
                        try {
                            // Extract the JSON part after "data: "
                            const jsonStr = line.substring(6);
                            
                            // Handle potential multi-line JSON strings in SSE data
                            try {                                const eventData = JSON.parse(jsonStr);
                                  // Handle different message types (more permissive to catch all message types)
                                if (eventData.content) {
                                    LoggingService.log('Received compliancy message chunk', eventData);
                                      // Always append new message content
                                    if (eventData.isReset) {
                                        // Reset the content if explicitly requested
                                        result = eventData.content;
                                    } else {
                                        // Otherwise append to existing content
                                        result += eventData.content;
                                    }
                                      // Check if this is the final message containing followupQuestions
                                    const contextData = eventData.context || {};
                                    
                                    // If eventData has followupQuestions and final=true, add to context                                    
                                    if (eventData.final === true && eventData.followupQuestions) {
                                        LoggingService.log('Found final message with followup questions in compliancy stream:', eventData.followupQuestions);
                                        if (!contextData.followup_questions) {
                                            contextData.followup_questions = eventData.followupQuestions;
                                        }
                                        result = eventData.content;
                                    }
                                    
                                    setMessages(prev => {
                                        const updated = [...prev];
                                        
                                        // Find the last assistant message if it exists
                                        const lastAssistantIndex = updated.length - 1;
                                        
                                        // Check if the last message is from the same agent
                                        if (lastAssistantIndex >= 0 && 
                                            updated[lastAssistantIndex].role === 'assistant' && 
                                            updated[lastAssistantIndex].agentName === (eventData.agentName || 'Reviewer') &&
                                            !updated[lastAssistantIndex].context?.isFinal) {
                                              // Update existing message from this agent
                                            updated[lastAssistantIndex] = {
                                                role: 'assistant',
                                                content: result,
                                                context: { 
                                                    ...updated[lastAssistantIndex].context,
                                                    ...eventData.context,
                                                    ...contextData, // Include contextData which may contain followup_questions
                                                    isFinal: eventData.isFinal || false
                                                },
                                                id: eventData.id || updated[lastAssistantIndex].id || '',
                                                created: eventData.timestamp || updated[lastAssistantIndex].created || new Date().toISOString(),
                                                agentName: eventData.agentName || 'Reviewer'
                                            };
                                        } else {                                            // Add a new message for a different agent or first message
                                            const message = {
                                                role: 'assistant',
                                                content: result,
                                                context: { 
                                                    ...eventData.context,
                                                    ...contextData, // Include contextData which may contain followup_questions
                                                    isFinal: eventData.isFinal || false
                                                },
                                                id: eventData.id || '',
                                                created: eventData.timestamp || new Date().toISOString(),
                                                agentName: eventData.agentName || 'Reviewer'
                                            };
                                            updated.push(message);
                                        }
                                        
                                        return updated;
                                    });
                                    
                                    if (eventData.isFinal) {
                                        console.log('Final message received');
                                        // Clear heartbeat when we get the final message
                                        setHeartbeatStatus(null);
                                        return true;
                                    }
                                } 
                                else if (eventData.type === 'heartbeat') {
                                    // Handle heartbeat messages in separate state
                                    console.log('Received heartbeat message');
                                    
                                    // Update heartbeat status instead of adding to messages
                                    setHeartbeatStatus({
                                        content: eventData.content || 'processing...',
                                        agentName: eventData.agentName || 'System',
                                        timestamp: eventData.timestamp || new Date().toISOString(),
                                        isActive: true
                                    });                                }                            } catch (_) {
                                // This might be a multi-line SSE message that has been split
                                // Add this chunk to a buffer and try to parse it later
                                console.warn('Possible multi-line SSE data, continuing to collect data');
                                // Just skip this line - it will be handled when we receive more data
                            }
                        } catch (parseError) {
                            console.error('Error parsing SSE data:', parseError, 'Line:', line);
                        }
                    }
                }
                
                // Keep any unprocessed data in the buffer
                if (processedUpTo < lines.length - 1) {
                    buffer = lines.slice(processedUpTo + 1).join('\n');
                } else {
                    buffer = '';
                }
            }
              // If we get here, the stream has ended normally
            console.log('Compliancy stream completed');
            
            // Always clear heartbeat status when stream ends
            setHeartbeatStatus(null);
            
            // If result is still empty, remove the two added messages
            if (result === '') {
                console.warn('No content received from compliancy stream');
                setMessages(prev => {
                    const updated = [...prev];
                    updated.pop();
                    updated.pop();
                    return updated;
                });
                return false;
            }
            
            return true;
        } catch (error) {
            console.error('Error sending compliancy message:', error);
            // Remove the messages if there was an error
            setMessages(prev => prev.slice(0, -2));
            return false;
        }
    };

    const deleteMessages = async () => {
        if (!chatId) return false; 
        const response = await chatService.deleteMessagesAsync({ chatId: chatId, token: accessToken });
        if (!response) {
            return false;
        }
        setMessages([]);
        return true;
    };    return {
        chatPending,
        chatError,
        messages,
        heartbeatStatus,
        sendMessage,
        sendMessageStream,
        sendCompliancyMessageStream,
        deleteMessages,
        forceRefreshMessages  
    };
};