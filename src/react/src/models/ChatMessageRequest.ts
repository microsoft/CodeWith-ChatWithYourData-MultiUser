export interface IChatMessageRequest {
    message:string,
    documentIds?: string[],
    tools?: string[],
}