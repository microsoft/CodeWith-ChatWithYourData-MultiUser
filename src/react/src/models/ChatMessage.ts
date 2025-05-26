export interface IChatMessage {
    id?: string,
    role: string,
    content: string,
    created?: string,
    context?:IChatContext,
    agentName?: string // Added for compliancy review responses
}

export interface IChatContext
{
    followup_questions?: string[],
    citations?: Citation[],
    dataPointsContent?: DataPointsContent[],
    thoughts?: Thoughts[],
    usageMetrics?: Usage,
    isFinal?: boolean
}

export type Usage = 
{
    inputTokens: number,
    outputTokens: number,
    totalTokens: number
}

export type DataPointsContent = 
{
    fileName:string;
    documentId: string;
}

export type Thoughts = {
    title: string;
    description: any; // It can be any output from the api
    props?: { [key: string]: string };
};

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

