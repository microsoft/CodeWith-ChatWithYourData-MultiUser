using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.Exporter;
using Azure.Search.Documents.Indexes;
using DocumentFormat.OpenXml.InkML;
using Infrastructure.Implementations.KernelMemory;
using Infrastructure.Implementations.SemanticKernel.Tools;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;

namespace WebApi.Extensions
{
    public static class WebApplicationBuilderExtensions
    {
        public static void AddKernelMemory(this WebApplicationBuilder builder, DefaultAzureCredential azureCredential)
        {
            var openAIConfig = builder.Configuration.GetSection("OpenAI");
            if (openAIConfig != null)
            {
                string? endpoint = openAIConfig["EndPoint"];
                string? embeddingModel = openAIConfig["EmbeddingModel"];

                var memoryConfiguration = new KernelMemoryConfig();
                var azureOpenAIEmbeddingConfig = new AzureOpenAIConfig();

                azureOpenAIEmbeddingConfig.Auth = AzureOpenAIConfig.AuthTypes.AzureIdentity;
                azureOpenAIEmbeddingConfig.Endpoint = endpoint;
                azureOpenAIEmbeddingConfig.Deployment = embeddingModel;

                // this is needed because we're only using the kernel memory for the chunking and embedding
                // from documents we inject into memory (from the Controller -> DocumentProcessorQueue -> SaveRecordsHandler -> AI Search)
                KernelMemoryBuilderBuildOptions kernelMemoryBuilderBuildOptions = new()
                {
                    AllowMixingVolatileAndPersistentData = true
                };

                var kernelMemoryBuilder = new KernelMemoryBuilder()
                    .WithAzureOpenAITextEmbeddingGeneration(azureOpenAIEmbeddingConfig)
                    .WithAzureOpenAITextGeneration(azureOpenAIEmbeddingConfig)
                    .Configure(builder => builder.Services.AddLogging(l =>
                    {
                        l.SetMinimumLevel(LogLevel.Error);
                        l.AddSimpleConsole(c => c.SingleLine = true);
                        l.AddDebug();

                    }));

                MemoryServerless kernelMemory = kernelMemoryBuilder.Build<MemoryServerless>(kernelMemoryBuilderBuildOptions);
                builder.Services.AddSingleton(kernelMemory);
                builder.Services.AddSingleton<SaveRecordsHandler>(sp =>
                {
                    // Get the required services from the service provider
                    var orchestrator = sp.GetRequiredService<IPipelineOrchestrator>();
                    var loggerFactory = sp.GetRequiredService<ILogger<SaveRecordsHandler>>();
                    var searchClient = sp.GetRequiredService<ISearchService>();

                    return new SaveRecordsHandler(
                        orchestrator,
                        searchClient,
                        loggerFactory);
                });
            }

            // Setting up Kernel Memory (for custom chunking/embedding and ingesting into AI Search)
            builder.Services.AddSingleton<KernelMemoryService>();
            builder.Services.AddSingleton<IDocumentProcessorQueue, DocumentProcessorQueue>();
            builder.Services.AddHostedService<DocumentProcessorBackgroundService>();
        }
    }
}
