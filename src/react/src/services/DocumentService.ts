import { IDocument } from "../models/Document";
import { env } from "../config/env";
import { IIndexDoc } from "../models/IndexDoc";
import LoggingService from "./LoggingService";

export class DocumentService {
    private readonly baseUrl = env.BACKEND_URL;

    public getDocumentsAsync = async (chatId: string, token: string): Promise<IDocument[]> => {
        try {
            const response = await fetch(`${this.baseUrl}/threads/${chatId}/documents`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            if (!response.ok) {
                throw new Error(`Error fetching chat: ${response.statusText}`);
            }
           
            const documents: IDocument[] = await response.json();
            return documents;
        } catch (error) {
            console.error('Failed to fetch chats:', error);
            throw error;
        }
    };

    public addDocumentsAsync = async ({ chatId, documents, token }: { chatId: string, documents: File[], token: string }): Promise<boolean> => {        if (!chatId || !Array.isArray(documents) || documents.length === 0) {
            LoggingService.log('No chat or documents to upload');
            return false;
        }
        const formData = new FormData();
        documents.forEach(file => {
            formData.append('documents', file);
        });

        try {
            const response = await fetch(`${this.baseUrl}/threads/${chatId}/documents`, {
                method: 'POST',
                body: formData,
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            if (!response.ok) {
                return false;
            }            return true;
        } catch (e) {
            LoggingService.error(e);
            return false;
        }
    }

    public deleteDocumentAsync = async ({chatId, documentId, token} : {chatId: string, documentId: string, token: string}): Promise<boolean> => {
        
        try {
            const response = await fetch(`${this.baseUrl}/threads/${chatId}/documents/${documentId}`, {
                method: 'DELETE',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${token}`,
                }
            });
            if (!response.ok) {
                throw new Error(`Error deleting document: ${response.statusText}`);
            }
            return true;
        } catch (error) {
            console.error('Failed to create chat:', error);
            return false;
        }
    }

    public getDocumentChunksAsync = async ({threadId, documentId, token}: {threadId: string, documentId: string, token: string}): Promise<IIndexDoc[]> => {
        try {
            const response = await fetch(`${this.baseUrl}/threads/${threadId}/documents/${documentId}/chunks`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            if (!response.ok) {
                throw new Error(`Error fetching document chunks: ${response.statusText}`);
            }
           
            const chunks: IIndexDoc[] = await response.json();
            return chunks;
        } catch (error) {
            console.error('Failed to fetch document chunks:', error);
            throw error;
        }
    };

    public extractDocumentAsync = async ({threadId, documentId, token}: {threadId: string, documentId: string, token: string}): Promise<boolean> => {
        try {
            const response = await fetch(`${this.baseUrl}/threads/${threadId}/documents/${documentId}`, {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            if (!response.ok) {
                throw new Error(`Error extracting document: ${response.statusText}`);
            }
            return true;
        } catch (error) {
            console.error('Failed to extract document:', error);
            return false;
        }
    };

    public getDocumentExtractAsync = async ({threadId, documentId, token}: {threadId: string, documentId: string, token: string}): Promise<string> => {
        try {
            const response = await fetch(`${this.baseUrl}/threads/${threadId}/documents/${documentId}/extract`, {
                headers: {
                    'Authorization': `Bearer ${token}`
                }
            });
            if (!response.ok) {
                throw new Error(`Error fetching document extract: ${response.statusText}`);
            }
           
            const extractText = await response.text();
            return extractText;
        } catch (error) {
            console.error('Failed to fetch document extract:', error);
            throw error;
        }
    };
}