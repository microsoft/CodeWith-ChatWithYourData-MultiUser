export interface IDocument {
    id: string;
    threadId: string;
    userId: string;
    documentName: string;
    fileSize: number;
    uploadDate: string;
    deleted: boolean;
    availableInSearchIndex: boolean;
    extractAvailable:boolean;
    chunkId: string | null;
    extract: string | null;
}