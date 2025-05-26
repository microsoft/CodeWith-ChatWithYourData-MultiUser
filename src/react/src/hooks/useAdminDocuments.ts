// filepath: d:\repos\codewith\codewith-philips\CodeWith-ChatWithYourData-MultiUser\src\react\src\hooks\useAdminDocuments.ts
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { AdminService } from "../services/AdminService";
import { useAuth } from "./useAuth";
import { IIndexDoc } from "../models/IndexDoc";

export const useAdminDocuments = () => {
    const [documentId, setDocumentId] = useState<string>("");
    const { accessToken } = useAuth();   
    const queryClient = useQueryClient();
    const adminService = new AdminService();

    // Query for all documents
    const { 
        data: allDocuments, 
        isLoading: isLoadingDocuments, 
        isError: isErrorDocuments, 
        error: documentsError 
    } = useQuery({
        queryKey: ['admin-documents'],
        queryFn: async () => adminService.getAllDocumentsAsync(accessToken),
        enabled: accessToken != undefined && accessToken != "",
    });

    // Query for document chunks by ID - modified to use a function parameter
    const { 
        data: documentChunks, 
        isLoading: isLoadingChunks,
        isError: isErrorChunks, 
        error: chunksError,
        refetch: searchChunks
    } = useQuery({
        queryKey: ['document-chunks', documentId],
        queryFn: async () => {
            if (!documentId) return [];
            return adminService.searchDocumentChunksAsync({documentId, token: accessToken});
        },
        enabled: false, // Don't run automatically, only when the search button is clicked
    });

    // Direct search function that doesn't depend on state updates
    const handleSearch = async (id: string) => {
        // Update the state for UI and future reference
        setDocumentId(id);
        
        if (id) {
            // Invalidate and refetch with the new ID directly
            await queryClient.invalidateQueries({queryKey: ['document-chunks']});
            // Manually update the query data with the new ID
            return queryClient.fetchQuery({
                queryKey: ['document-chunks', id],
                queryFn: async () => adminService.searchDocumentChunksAsync({documentId: id, token: accessToken})
            });
        }
    };

    return {
        allDocuments,
        isLoadingDocuments,
        isErrorDocuments,
        documentsError,
        documentChunks,
        isLoadingChunks,
        isErrorChunks,
        chunksError,
        handleSearch,
        documentId,
        setDocumentId
    };
}