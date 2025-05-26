import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { IDocument } from "../models/Document";
import { IIndexDoc } from "../models/IndexDoc";
import { DocumentService } from "../services/DocumentService";
import { useAuth } from "./useAuth";
import LoggingService from "../services/LoggingService";

export const useChatDocuments = (chatId: string | undefined) => {

    const queryClient = useQueryClient();
    const documentService = new DocumentService();
    const { accessToken } = useAuth();   


    const [documents, setDocuments] = useState<IDocument[]>([]);

    const { isPending: documentsPending, error: documentsError, data: documentData } = useQuery({
        queryKey: ['documents', chatId],
        queryFn: async () => documentService.getDocumentsAsync(chatId || "", accessToken),
        enabled: chatId != undefined && accessToken != undefined && accessToken != ""
    });

    useEffect(() => {
        if (documentData) {
            setDocuments(documentData);
        }
    }, [ documentData]);    const { mutateAsync: addDocuments} = useMutation({
        mutationFn: ({chatId, documents} : {chatId: string, documents: File[]}) => documentService.addDocumentsAsync({chatId, documents, token: accessToken}),
        onError: () => {
            LoggingService.warn('Failed to upload a document.');
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['documents', chatId] });
        }
    });    const { mutateAsync: deleteDocument } = useMutation({
        mutationFn: ({chatId, documentId} : {chatId: string, documentId: string}) => documentService.deleteDocumentAsync({chatId, documentId, token: accessToken}),
        onError: () => {
            LoggingService.warn('Failed to delete a document.');
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['documents', chatId] });
        }
    });

    const getDocumentChunks = async (threadId: string, documentId: string): Promise<IIndexDoc[]> => {
        return documentService.getDocumentChunksAsync({
            threadId,
            documentId,
            token: accessToken
        });
    };

    const getDocumentExtract = async (threadId: string, documentId: string): Promise<string> => {
        return documentService.getDocumentExtractAsync({
            threadId,
            documentId,
            token: accessToken
        });
    };

    const extractDocument = async (threadId: string, documentId: string): Promise<boolean> => {
        return documentService.extractDocumentAsync({
            threadId,
            documentId,
            token: accessToken
        });
    };

    return {
        documentsPending,
        documentsError,
        documents,
        addDocuments,
        deleteDocument,
        getDocumentChunks,
        getDocumentExtract,
        extractDocument
    };

}