using Domain.Cosmos;
using Domain.Search;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Interfaces;

public interface ISearchService
{
    //Task<bool> IsChunkingComplete(string threadId);
    Task<List<DocsPerThread>> IsChunkingComplete(List<DocsPerThread> docsPerThreads);
    Task<DocsPerThread> IsChunkingComplete(DocsPerThread docsPerThreads);
    Task<bool> StartIndexing();
    Task<bool> DeleteDocumentAsync(DocsPerThread document);
    Task<List<IndexDoc>> GetSearchResultsAsync(string query, string threadId);
    Task<List<IndexDoc>> GetSearchResultsAsync(string query, string threadId, List<string> documentIds);
    Task<List<IndexDoc>> GetExtractedResultsAsync(string threadId);
    Task<IndexDoc> GetExtractedResultsAsync(string threadId, string documentId);
    Task<long> GetSearchResultsCountAsync();
    Task<List<IndexDoc>> GetDocumentAsync(string documentId);
    Task<bool> IngestExtractedDocumentIntoIndex(string extractedDocument, string documentId);

    IAsyncEnumerable<string> IngestIntoIndex(IEnumerable<IndexDoc> records, [EnumeratorCancellation] CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a health check on the search service
    /// </summary>
    /// <returns>Task that completes when the health check is done</returns>
    Task IsHealthyAsync();

}
