using Domain.Search;
using Infrastructure.Implementations.AISearch;
using Infrastructure.Implementations.SemanticKernel.Tools;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Implementations.SemanticKernel.Agents
{

    public class ExtractAgent
    {
        public ChatCompletionAgent CreateAgent(Kernel kernel, string agentName, string threadId, List<IndexDoc>? documents)
        {
            // Clone kernel instance to allow for agent specific plug-in definition
            Kernel agentKernel = kernel.Clone();

            if (documents == null || documents.Count == 0)
            {
                if (!agentKernel.Plugins.Contains("get_documents_from_thread"))
                {
                    agentKernel.ImportPluginFromType<AISearchService>("get_documents_from_thread");
                }
            }


            StringBuilder batchContent = new StringBuilder();
            batchContent.AppendLine("-------DOCUMENTS------");
            if (documents != null)
            {
                foreach (IndexDoc doc in documents)
                {
                    string[] parts = doc.ChunkId.Split("_");
                    string pageNumber = parts.Length > 1 ? parts[1] : "N/A";
                    batchContent.AppendLine($"PageNumber: {pageNumber}");
                    batchContent.AppendLine($"Document ID: {doc.DocumentId}");
                    batchContent.AppendLine($"File Name: {doc.FileName}");
                    batchContent.AppendLine($"Content: {doc.Content}");
                    batchContent.AppendLine("------");
                    batchContent.AppendLine();
                }
            }

            string instruction = $"""
                You are a document extractor expert. Your job is to extract requirements from the document. 
                Some of the chunks provided can overlap each other, keep that in mind. 
                Please make sure you extract all the requirements and categorize them. 
                The structure of the output is:
                - Id: the id of the requirement
                - Name: the name of the requirement
                - Definition: the definition of the requirement
                - Category: the category of the requirement
                - Related: the related requirements
                - Source: the source of the requirement
                - Remarks: any remarks about the requirement
                When you finish, please return the result in a markdown table format.

                ### Current ThreadId is {threadId}

                ### Document Chunks
                {batchContent}
                """;

            return
                new ChatCompletionAgent()
                {
                    Name = agentName,
                    Description = "Agent responsible for extracting requirements from documents.",
                    Instructions = instruction,
                    Kernel = agentKernel,
                    Arguments = new KernelArguments(
                        new OpenAIPromptExecutionSettings()
                        {
                            FunctionChoiceBehavior = FunctionChoiceBehavior.Required(),
                            ServiceId = "completion",
                        })
                };
        }
    }
}
