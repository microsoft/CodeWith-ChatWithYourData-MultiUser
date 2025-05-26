using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Domain.Cosmos;
using Infrastructure.Implementations.AISearch;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Chunkers;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DataFormats.Pdf;
using Microsoft.KernelMemory.Handlers;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.SemanticKernel.Text;
using System;

namespace Infrastructure.Implementations.KernelMemory
{
    public class KernelMemoryService
    {
        private readonly MemoryServerless _memory;
        private readonly ILogger<KernelMemoryService> _logger;
        private readonly IServiceProvider _services;
        private readonly string index = "onyourdata";
        private readonly ISearchService _searchService;
     
        public KernelMemoryService(
            MemoryServerless memory,
            ILogger<KernelMemoryService> logger,
            IServiceProvider services            
            )
        {
            _logger = logger;
            _memory = memory;
            _services = services;

            _searchService = _services.GetService<ISearchService>();

            _memory.Orchestrator.AddHandler<TextExtractionHandler>("extract_text");
            _memory.Orchestrator.AddHandler<TextPartitioningHandler>("split_text_in_partitions");
            _memory.Orchestrator.AddHandler<GenerateEmbeddingsHandler>("generate_embeddings");
            
            var handler = new SaveRecordsHandler(
                _memory.Orchestrator,
                _searchService, 
                _services.GetService<ILogger<SaveRecordsHandler>>());

            _memory.Orchestrator.AddHandler(handler);
        }


        public async Task Process(IFormFile file, DocsPerThread docPerThread)
        {
            TagCollection tags = new()
            {
                { "DocumentId", docPerThread.Id },
                { "FileName", docPerThread.DocumentName },
                { "UploadDate", docPerThread.UploadDate.ToShortDateString()},
                { "ThreadId", docPerThread.ThreadId},
                { "UserId", docPerThread.UserId }
            };

            // Create a memory stream copy of the file content
            using MemoryStream memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

           // Import the document using the memory stream instead of file.OpenReadStream()
            await _memory.ImportDocumentAsync(
                content: memoryStream,
                fileName: docPerThread.DocumentName,
                documentId: docPerThread.Id,
                index: index,
                tags: tags,
                steps:
                [
                    "extract_text",
                    "split_text_in_partitions",
                    "generate_embeddings",
                    "save_memory_records"
                ]
            );
        }
    }
}
