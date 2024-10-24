import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { IDocument } from "../models/Document";
import { DocumentService } from "../services/DocumentService";
import { useAuth } from "./useAuth";

export const useChatDocuments = (chatId: string | undefined) => {

    const queryClient = useQueryClient();
    const documentService = new DocumentService();
    const {userId, accessToken} = useAuth();   


    const [documents, setDocuments] = useState<IDocument[]>([]);

    const { isPending: documentsPending, error: documentsError, data: documentData } = useQuery({
        queryKey: ['documents', chatId],
        queryFn: async () => documentService.getDocumentsAsync(chatId || ""),
        enabled: chatId != undefined
    });

    useEffect(() => {
        if (documentData) {
            setDocuments(documentData);
        }
    }, [ documentData]);

    const { mutateAsync: addDocuments} = useMutation({
        mutationFn: ({chatId, documents} : {chatId: string, documents: File[]}) => documentService.addDocumentsAsync({chatId, userId, documents}),
        onError: () => {
            console.log('Failed to upload a document.');
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['documents', chatId] });
        }
    });

    const { mutateAsync: deleteDocument } = useMutation({
        mutationFn: documentService.deleteDocumentAsync,
        onError: () => {
            console.log('Failed to delete a document.');
        },
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['documents', chatId] });
        }
    });

    return {
        documentsPending,
        documentsError,
        documents,
        addDocuments,
        deleteDocument
    };

}