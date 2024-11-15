using Azure.Identity;
using Azure.Search.Documents.Indexes;
using DocApi.Utils;
using Microsoft.SemanticKernel.Connectors.AzureAISearch;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Data;
using Microsoft.SemanticKernel;

namespace WebApi.Extensions
{
    public static class WebApplicationBuilderExtensions
    {
        public static void AddSemanticKernel(this WebApplicationBuilder builder, DefaultAzureCredential azureCredential, HttpClient? httpClient)
        {
            var searchConfig = builder.Configuration.GetSection("Search");
            var openAIConfig = builder.Configuration.GetSection("OpenAI");

            if (searchConfig != null && openAIConfig != null)
            {
                Uri serviceUri = new Uri(searchConfig["EndPoint"]);
                string? indexName = searchConfig["IndexName"];
                string? endpoint = openAIConfig["EndPoint"];
                string? completionModel = openAIConfig["CompletionModel"];
                string? embeddingModel = openAIConfig["EmbeddingModel"];
                string? apiKey = openAIConfig["ApiKey"];


                var kernelBuilder = Kernel.CreateBuilder();
                if (httpClient == null)
                    kernelBuilder.AddAzureOpenAIChatCompletion(completionModel, endpoint, azureCredential);
                else
                    kernelBuilder.AddAzureOpenAIChatCompletion(completionModel, endpoint, apiKey, null, null, httpClient);

                var kernel = kernelBuilder.Build();
                builder.Services.AddSingleton(kernel);
                builder.Services.AddSingleton(new PromptHelper(kernel));
            }
        }

        public static void AddAzureAISearch(this WebApplicationBuilder builder, DefaultAzureCredential azureCredential, HttpClient? httpClient)
        {
            var searchConfig = builder.Configuration.GetSection("Search");
            var openAIConfig = builder.Configuration.GetSection("OpenAI");

            if (searchConfig != null && openAIConfig != null)
            {
                Uri serviceUri = new Uri(searchConfig["EndPoint"]);
                string? indexName = searchConfig["IndexName"];
                string? endpoint = openAIConfig["EndPoint"];
                string? completionModel = openAIConfig["CompletionModel"];
                string? embeddingModel = openAIConfig["EmbeddingModel"];
                string? apiKey = openAIConfig["ApiKey"];

                // Search
                AzureOpenAITextEmbeddingGenerationService? embedding = null;
                if (httpClient == null)
                    embedding = new AzureOpenAITextEmbeddingGenerationService(embeddingModel, endpoint, azureCredential);
                else
                    embedding = new AzureOpenAITextEmbeddingGenerationService(embeddingModel, endpoint, apiKey, null, httpClient);

                var collection = new AzureAISearchVectorStoreRecordCollection<IndexDoc>(new SearchIndexClient(serviceUri, azureCredential), indexName);
                builder.Services.AddSingleton(new VectorStoreTextSearch<IndexDoc>(collection, embedding));
            }
        }
    }
}
