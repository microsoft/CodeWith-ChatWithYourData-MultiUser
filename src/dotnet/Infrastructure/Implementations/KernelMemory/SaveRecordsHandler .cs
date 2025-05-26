// this is based on the CustomSaveRecordsHandler.cs from the KernelMemory project
// see https://github.com/microsoft/kernel-memory/blob/1c424ed35738342ea2b3f8cae4091cb649071c49/service/Core/Handlers/SaveRecordsHandler.cs


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Search.Documents.Models;
using Azure.Search.Documents;
using Azure;
using Domain.Search;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.Diagnostics;
using Microsoft.KernelMemory.DocumentStorage;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.Pipeline;
using Microsoft.KernelMemory.MemoryDb.AzureAISearch;
using Azure.Search.Documents.Indexes;
using Microsoft.KernelMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.RecordIO;
using Infrastructure.Interfaces;

namespace Infrastructure.Implementations.KernelMemory;

public class SaveRecordsHandler : IPipelineStepHandler
{
    private readonly IPipelineOrchestrator _orchestrator;
    private readonly ILogger<SaveRecordsHandler> _logger;
    private readonly ISearchService _searchService;
    private readonly string _stepName = "save_memory_records";

    /// <inheritdoc />
    public string StepName { get; set; }

    /// <summary>
    /// Handler responsible for copying embeddings from storage to list of memory DBs
    /// Note: stepName and other params are injected with DI
    /// </summary>
    /// <param name="stepName">Pipeline step for which the handler will be invoked</param>
    /// <param name="orchestrator">Current orchestrator used by the pipeline, giving access to content and other helps.</param>
    /// <param name="config">Configuration settings</param>
    /// <param name="loggerFactory">Application logger factory</param>
    public SaveRecordsHandler(
        IPipelineOrchestrator orchestrator,
        ISearchService searchService,
        ILogger<SaveRecordsHandler> logger
        )
    {
        StepName = _stepName;
        _logger = logger;
        _orchestrator = orchestrator;
        _searchService = searchService;

    }

    /// <inheritdoc />
    public async Task<(ReturnType returnType, DataPipeline updatedPipeline)> InvokeAsync(
        DataPipeline pipeline, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Saving memory records, pipeline '{0}/{1}'", pipeline.Index, pipeline.DocumentId);

        bool recordsFound = false;
        var records = new List<IndexDoc>();

        var sourceFiles = pipeline.Files.SelectMany(f1 => f1.GeneratedFiles.Where(
                f2 => f2.Value.ArtifactType == DataPipeline.ArtifactTypes.TextEmbeddingVector));

        foreach (var sourceFile in sourceFiles)
        {
            if (sourceFile.Value == null) { continue; }

            var file = sourceFile.Value;

            // List of records to upsert, used only when batching
            if (file.AlreadyProcessedBy(this))
            {
                recordsFound = true;
                _logger.LogTrace("File {0} already processed by this handler", file.Name);
                continue;
            }

            DataPipeline.FileDetails fileDetails = pipeline.GetFile(file.ParentId);

            // Read vector data from embedding file
            string vectorJson = await _orchestrator.ReadTextFileAsync(pipeline, file.Name, cancellationToken).ConfigureAwait(false);
            EmbeddingFileContent? embeddingData = JsonSerializer.Deserialize<EmbeddingFileContent>(vectorJson.RemoveBOM().Trim());
            if (embeddingData == null) { throw new OrchestrationException($"Unable to deserialize embedding file {file.Name}"); }

            // Get text partition content
            string partitionContent = await _orchestrator.ReadTextFileAsync(pipeline, embeddingData.SourceFileName, cancellationToken).ConfigureAwait(false);

            string threadId = string.Empty;
            string documentId = string.Empty;
            string fileName = string.Empty;
            string uploadDate = string.Empty;
            string userId = string.Empty;

            // Safely extract values from tags collection
            if (file.Tags.TryGetValue("ThreadId", out var threadIdValues) && threadIdValues.Any())
            {
                threadId = threadIdValues.First().ToString();
            }

            if (file.Tags.TryGetValue("DocumentId", out var documentIdValues) && documentIdValues.Any())
            {
                documentId = documentIdValues.First().ToString();
            }

            if (file.Tags.TryGetValue("FileName", out var fileNameValues) && fileNameValues.Any())
            {
                fileName = fileNameValues.First().ToString();
            }

            if (file.Tags.TryGetValue("UploadDate", out var uploadDateValues) && uploadDateValues.Any())
            {
                uploadDate = uploadDateValues.First().ToString();
            }

            if (file.Tags.TryGetValue("UserId", out var userIdValues) && userIdValues.Any())
            {
                userId = userIdValues.First().ToString();
            }

            // Log the extracted values for debugging
            _logger.LogDebug("Extracted tags - ThreadId: {ThreadId}, DocumentId: {DocumentId}, FileName: {FileName}",
                threadId, documentId, fileName);

            IndexDoc record = new IndexDoc()
            {
                ChunkId = documentId + "_" + file.PartitionNumber,
                Content = partitionContent,
                FileName = fileName,
                DocumentId = documentId,
                ThreadId = threadId,
                ContentVector = embeddingData.Vector.Data,
                ParentId = file.ParentId
            };

            records.Add(record);
        }

        // Save records to memory DB
        if (records.Count > 0)
        {
            recordsFound = true;
            _logger.LogDebug("Saving {0} records to memory DB", records.Count);
            // Save records to memory DB
            await SaveRecordsBatchAsync(pipeline, records,cancellationToken).ConfigureAwait(false);
        }

        if (!recordsFound)
        {
            _logger.LogWarning("Pipeline '{0}/{1}': step {2}: no records found, cannot save, moving to next pipeline step.", pipeline.Index, pipeline.DocumentId, StepName);
        }

        return (ReturnType.Success, pipeline);
    }


    private async Task SaveRecordAsync(DataPipeline pipeline, IndexDoc record, CancellationToken cancellationToken)
    {
        await UpsertAsync(pipeline.Index, record, cancellationToken).ConfigureAwait(false);

    }

    private async Task SaveRecordsBatchAsync(DataPipeline pipeline, List<IndexDoc> records, CancellationToken cancellationToken)
    {
        await UpsertBatchAsync(pipeline.Index, records, cancellationToken).ToListAsync(cancellationToken).ConfigureAwait(false);
    }


    // here we go directly to the Azure Search API to save the records
    // this is taken from https://github.com/microsoft/kernel-memory/blob/1c424ed35738342ea2b3f8cae4091cb649071c49/extensions/AzureAISearch/AzureAISearch/AzureAISearchMemory.cs

    /// <inheritdoc />
    public async Task<string> UpsertAsync(string index, IndexDoc record, CancellationToken cancellationToken = default)
    {
        var result = UpsertBatchAsync(index, [record], cancellationToken);
        var id = await result.SingleAsync(cancellationToken).ConfigureAwait(false);
        return id;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> UpsertBatchAsync(
        string index,
        IEnumerable<IndexDoc> records,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var results =  _searchService.IngestIntoIndex(records, cancellationToken);

        await foreach (var result in results)
        {
            yield return result;
        }
        //var client = _searchIndexClient!.GetSearchClient(index);

        //try
        //{
        //    await client.IndexDocumentsAsync(
        //        IndexDocumentsBatch.Upload(records),
        //        new IndexDocumentsOptions { ThrowOnAnyError = true },
        //        cancellationToken: cancellationToken).ConfigureAwait(false);
        //}
        //catch (RequestFailedException e)
        //{
        //    throw new IndexNotFoundException(e.Message, e);
        //}

        //foreach (var record in records)
        //{
        //    yield return record.ChunkId;
        //}
    }
}