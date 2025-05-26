// this brilliant piece of code comes from https://johnnyreilly.com/using-kernel-memory-to-chunk-documents-into-azure-ai-search
// thanks John Reilly :)
// and yes, it's modified to make use of our custom object and have implemented some parallelism

using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Domain.Cosmos;
using Microsoft.AspNetCore.Http;

namespace Infrastructure.Implementations.KernelMemory
{
    public record DocumentToProcess(byte[] FileData, string FileName, string ContentType, DocsPerThread docPerThread);

    public interface IDocumentProcessorQueue
    {
        DocumentToProcess? DequeueDocumentUri();
        void EnqueueDocumentUri(DocumentToProcess documentToProcess);
    }

    public class DocumentProcessorQueue : IDocumentProcessorQueue
    {
        readonly ConcurrentQueue<DocumentToProcess> _documentUrlQueue;
        readonly ILogger<DocumentProcessorQueue> _logger;

        public DocumentProcessorQueue(ILogger<DocumentProcessorQueue> logger)
        {
            _documentUrlQueue = new();
            _logger = logger;
        }

        public void EnqueueDocumentUri(DocumentToProcess documentToProcess)
        {
            _logger.LogInformation("Starting EnqueueDocumentUri");
            _documentUrlQueue.Enqueue(documentToProcess);
        }

        public DocumentToProcess? DequeueDocumentUri()
        {
            if (_documentUrlQueue.TryDequeue(out var documentToProcess))
            {
                return documentToProcess;
            }

            return null;
        }
    }

    public class DocumentProcessorBackgroundService : BackgroundService
    {
        private readonly ILogger<DocumentProcessorBackgroundService> _logger;
        private readonly IServiceProvider _services;
       

        public DocumentProcessorBackgroundService(IServiceProvider services, ILogger<DocumentProcessorBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
           
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("Starting RagGestion");

                var kernelMemoryService = _services.GetRequiredService<KernelMemoryService>();
                var documentProcessorQueue = _services.GetRequiredService<IDocumentProcessorQueue>();
              
                await PerformRagGestion(kernelMemoryService, documentProcessorQueue, stoppingToken);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error processing document");
            }
        }

        async Task PerformRagGestion(KernelMemoryService ragGestionService,
                                        IDocumentProcessorQueue documentProcessorQueue,
                                        CancellationToken stoppingToken)
        {
            // Configure the degree of parallelism
            int maxConcurrentProcessing = 3; // Adjust based on resources
            SemaphoreSlim throttler = new SemaphoreSlim(maxConcurrentProcessing);
            List<Task> tasks = new List<Task>();

            while (!stoppingToken.IsCancellationRequested)
            {
                // Process multiple items in parallel
                while (documentProcessorQueue.DequeueDocumentUri() is DocumentToProcess documentToProcess)
                {
                    await throttler.WaitAsync(stoppingToken);

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var formFile = CreateFormFile(documentToProcess);
                            await ragGestionService.Process(formFile, documentToProcess.docPerThread);
                            _logger.LogInformation("Successfully processed {DocumentName}",
                                documentToProcess.docPerThread.DocumentName);
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "Error processing document: {DocumentName}",
                                documentToProcess.docPerThread.DocumentName);
                            // Consider requeueing with backoff strategy
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }, stoppingToken));
                }

                // Wait a bit before checking queue again
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            // Wait for all processing to complete on shutdown
            await Task.WhenAll(tasks.Where(t => !t.IsCompleted));
        }

        private FormFile CreateFormFile(DocumentToProcess documentToProcess)
        {
            return new FormFile(
                new MemoryStream(documentToProcess.FileData),
                0,
                documentToProcess.FileData.Length,
                "file",
                documentToProcess.FileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = documentToProcess.ContentType
            };
        }
    }
}
