using System;
using System.Collections.Generic;
using Domain.Cosmos;
using Infrastructure;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CleanUpJobs.Jobs;

public class DocumentCleanUpFunction
{
    private readonly ILogger _logger;
    private IConfiguration _config;
    private IDocumentStore _documentStore;
    private IDocumentRegistry _documentRegistry;
    private ISearchService _searchService;

    private readonly string _storageContainerName;

    public DocumentCleanUpFunction(
        ILoggerFactory loggerFactory,
        IConfiguration config,
        IDocumentStore documentStore,
        IDocumentRegistry documentRegistry,
        ISearchService searchService)
    {
        _logger = loggerFactory.CreateLogger<DocumentCleanUpFunction>();
        _config = config;
        _documentStore = documentStore;
        _documentRegistry = documentRegistry;
        _searchService = searchService;

        _storageContainerName = _config["StorageContainerName"] ?? throw new ArgumentNullException("StorageContainerName");
    }

    [Function("DocumentCleanUp")]
    public async Task<IActionResult> Run([CosmosDBTrigger(
        databaseName: "%CosmosDBDatabase%",
        containerName: "%CosmosDbDocumentContainer%",
        Connection = "CosmosDbConnection",
        LeaseContainerName ="%CosmosDbDocumentLease%",
        FeedPollDelay = 5000, // this to ensure that when deleting documents this is not triggered again
        CreateLeaseContainerIfNotExists = false)] IReadOnlyList<DocsPerThread> input)
    {
        _logger.LogInformation($"CosmosDbTrigger found {input.Count} documents which are soft-deleted.");

        for (int i = 0; i < input.Count; i++)
        {
            var doc = input[i];
            if (doc.Deleted)
            {
                _logger.LogInformation($"Document {doc.DocumentName} is marked for deletion. Deleting...");

                // Fetching data for the specified document (getting the chunk_id so we can delete it from the index later on)
                doc = await _searchService.IsChunkingComplete(doc);

                _logger.LogInformation($"BlobStore : trying to delete {doc.DocumentName}.");
                // Delete the document from storage
                if (await _documentStore.DeleteDocumentAsync(doc.Id, _storageContainerName))
                {
                    _logger.LogInformation($"CosmosDb : trying to delete {doc.DocumentName}.");

                    // Delete the document from the Cosmos DB container
                    if (await _documentRegistry.DeleteDocumentAsync(doc))
                    {
                        _logger.LogInformation($"SearchIndex : trying to delete {doc.DocumentName}.");

                        // Delete the document from the search index
                        await _searchService.DeleteDocumentAsync(doc);
                    }
                }
            }
            else
            {
                _logger.LogInformation($"Document {doc.DocumentName} is not marked for deletion.");
            }
        }

        return new OkResult();
    }
}

