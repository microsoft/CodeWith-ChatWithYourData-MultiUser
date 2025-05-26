export interface ISetting {
    allowInitialPromptRewrite:boolean, 
    allowInitialPromptToHelpUser: boolean,
    allowFollowUpPrompts: boolean,
    useSemanticRanker: boolean,
    temperature: number,
    maxTokens: number,
    seed: number,
    predefinedPrompts: PredefinedPrompt[]
    tools: Tool[]
}

export interface PredefinedPrompt
{
    id: string;
    name: string;
    prompt: string;
}

export interface Tool
{
    id: string;
    name: string;
    description: string;
}
