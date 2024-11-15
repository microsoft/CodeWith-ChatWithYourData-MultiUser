export interface IChatMessage {
    id?: string,
    role: string,
    content: string,
    timestamp?: string,
    followupquestions?: string[],
    citations?: Citation[]
}

export type Citation = {
    content: string;
    id: string;
    title: string | null;
    filepath: string | null;
    url: string | null;
    metadata: string | null;
    chunk_id: string | null;
    reindex_id: string | null;
}