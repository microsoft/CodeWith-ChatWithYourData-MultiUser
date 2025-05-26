using Azure;
using Azure.Identity;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using DocumentFormat.OpenXml.Office2010.Word;
using Domain.Cosmos;
using Domain.Search;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Implementations.AISearch
{
    public class AISearchService : ISearchService
    {
        private SearchClient _searchClient;
        private SearchIndexClient _indexClient;
        private SearchIndexerClient _indexerClient;
        private readonly ILogger<AISearchService> _logger;
        private readonly IConfiguration _configuration;

        private string indexerName;
        private string indexName;

        public AISearchService(SearchIndexClient indexClient, IConfiguration configuration, ILogger<AISearchService> logger)
        {
            _logger = logger;
            _configuration = configuration;

            indexerName = configuration.GetValue<string>("Search:IndexerName") ?? "onyourdata-indexer";
            indexName = configuration.GetValue<string>("Search:IndexName") ?? "onyourdata";

            _indexClient = indexClient;
            _searchClient = indexClient.GetSearchClient(indexName);
            _indexerClient = new SearchIndexerClient(_searchClient.Endpoint, new DefaultAzureCredential());
        }

        public async Task<bool> StartIndexing()
        {
            var response = await _indexerClient.RunIndexerAsync(indexerName);
            if (response.IsError)
                return false;
            return true;
        }

        public async IAsyncEnumerable<string> IngestIntoIndex(IEnumerable<IndexDoc> records, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                await _searchClient.IndexDocumentsAsync(
                    IndexDocumentsBatch.Upload(records),
                    new IndexDocumentsOptions { ThrowOnAnyError = true },
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (RequestFailedException e)
            {
                throw new IndexNotFoundException(e.Message, e);
            }

            foreach (var record in records)
            {
                yield return record.ChunkId;
            }
        }

        public async Task<bool> IngestExtractedDocumentIntoIndex(string extractedDocument, string documentId)
        {
            bool isSuccess = false;
            // get the current document
            var chunks = await GetDocumentAsync(documentId);

            IndexDocumentsBatch<IndexDoc> batch = new IndexDocumentsBatch<IndexDoc>();

            // add the extract doc to each chunk
            // perhaps we should think about this in terms of storage.. 
            // e.g. create a new index that only holds the extracted doc or add a new item to the index with the same documentId 
            foreach (var chunk in chunks)
            {
               chunk.Extract = extractedDocument;
               var mergAction = IndexDocumentsAction.Merge(chunk);
               batch.Actions.Add(mergAction);
            }
            
            try
            {
                IndexDocumentsResult result = await _searchClient.IndexDocumentsAsync(batch);
                isSuccess = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ingesting document data.");
            }

            return isSuccess;
        }

        public async Task<bool> DeleteDocumentAsync(DocsPerThread document)
        {
            try
            {
                
                var searchOptions = new SearchOptions
                {
                    Size = 500,
                    Select = { "chunk_id", "document_id", "thread_id" },
                    Filter = string.Format("document_id eq '{0}'", document.Id)
                };
                SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>("*", searchOptions);

                IndexDocumentsBatch<SearchDocument> batch = new IndexDocumentsBatch<SearchDocument>();
                await foreach (SearchResult<SearchDocument> searchResult in response.GetResultsAsync())
                {
                    var deleteAction = IndexDocumentsAction.Delete("chunk_id", searchResult.Document["chunk_id"].ToString());
                    batch.Actions.Add(deleteAction);
                }

                IndexDocumentsResult result = await _searchClient.IndexDocumentsAsync(batch);

            }
            catch (RequestFailedException ex)
            {
                return false;
            }

            return true;
        }

        public async Task<DocsPerThread> IsChunkingComplete(DocsPerThread docPerThread)
        {
            List<DocsPerThread> docsPerThread = new List<DocsPerThread> { docPerThread };
            var result = await IsChunkingComplete(docsPerThread);
            return result.First();
        }

        public async Task<List<DocsPerThread>> IsChunkingComplete(List<DocsPerThread> docsPerThreads)
        {

            for ( int x = 0; x < docsPerThreads.Count; x++)
            {
                var doc = docsPerThreads[x];
                var searchOptions = new SearchOptions
                {
                    Size = 1,
                    IncludeTotalCount = true,
                    Select = { "chunk_id", "document_id", "thread_id", "extract" },
                    Filter = string.Format("document_id eq '{0}'", doc.Id)
                };
                SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>("*", searchOptions);

                bool isChunkFound = false;
                bool isExtractFound = false;

                var results = response.GetResultsAsync();

                await foreach (SearchResult<SearchDocument> searchResult in results)
                {
                    if (searchResult.Document["chunk_id"] != null)
                    {
                        isChunkFound = true;
                    }
                    if (searchResult.Document["extract"] != null)
                    {
                        isExtractFound = true;
                    }
                    break;
                }

                doc.AvailableInSearchIndex = isChunkFound;
                doc.ExtractAvailable = isExtractFound;
            }

            return docsPerThreads;
        }

        public async Task<long> GetSearchResultsCountAsync()
        {
            var searchOptions = new SearchOptions
            {
                Size = 1,
                IncludeTotalCount = true,
                Select = { "chunk_id", "document_id", "thread_id" },
            };
            SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>("*", searchOptions);
            return response.TotalCount ?? 0;
        }

        
        public async Task<List<IndexDoc>> GetExtractedResultsAsync(string threadId)
        {
            List<IndexDoc> docs = new List<IndexDoc>();

            SearchOptions searchOptions = new SearchOptions
            {
                Size = 1,
                Filter = $"thread_id eq '{threadId}'",
                QueryType = SearchQueryType.Simple
            };

            SearchResults<IndexDoc> response = await _searchClient.SearchAsync<IndexDoc>("*", searchOptions);

            await foreach (SearchResult<IndexDoc> searchResult in response.GetResultsAsync())
            {
                docs.Add(searchResult.Document);
            }

            var uniqueDocs = docs
            .GroupBy(sr => sr.DocumentId)
            .Select(g => g).FirstOrDefault()
            .Distinct()
            .ToList();

            return uniqueDocs;
           
        }

        [KernelFunction("get_extracted_results")]
        public async Task<IndexDoc> GetExtractedResultsAsync(string threadId, string documentId)
        {
            Console.WriteLine("get_extracted_results");

            SearchOptions searchOptions = new SearchOptions
            {
                Size = 1,
                Filter = $"thread_id eq '{threadId}' and document_id eq '{documentId}'",
                QueryType = SearchQueryType.Simple
            };

            SearchResults<IndexDoc> response = await _searchClient.SearchAsync<IndexDoc>("*", searchOptions);

            await foreach (SearchResult<IndexDoc> searchResult in response.GetResultsAsync())
            {
                return searchResult.Document;
            }

            return null;
        }

        [KernelFunction("get_documents_from_thread")]
        public async Task<List<IndexDoc>> GetSearchResultsAsync(string query, string threadId)
        {
            Console.WriteLine("get_documents_from_thread");

            List<IndexDoc> docs = new List<IndexDoc>();

            SearchOptions searchOptions = new SearchOptions
            {
                Size = 100,
                Filter = $"thread_id eq '{threadId}'",
                QueryType = SearchQueryType.Full
            };

            if (query != "*")
            {
                searchOptions.VectorSearch = new()
                {
                    Queries = {
                        new VectorizableTextQuery(text: query)
                        {
                            KNearestNeighborsCount = 3,
                            Fields = { "content_vector" },
                            Exhaustive = true
                        }
                    },
                };
            }

            SearchResults<IndexDoc> response = await _searchClient.SearchAsync<IndexDoc>(query, searchOptions);

            await foreach (SearchResult<IndexDoc> searchResult in response.GetResultsAsync())
            {
                docs.Add(searchResult.Document);
            }

            return docs;
        }

        public async Task<List<IndexDoc>> GetSearchResultsAsync(string query, string threadId, List<string> documentIds)
        {
            string? filterString = null;
            if (documentIds != null && documentIds.Any())
            {
                filterString = string.Join(" or ", documentIds.Select(id => $"document_id eq '{id}'"));
            }
            filterString = filterString + $" and thread_id eq '{threadId}'";
            List<IndexDoc> docs = new List<IndexDoc>();
          

            SearchOptions searchOptions = new SearchOptions
            {
                Size = 100,
                Filter = filterString,
                QueryType = SearchQueryType.Full
            };
            searchOptions.VectorSearch = new()
            {
                Queries = {
                        new VectorizableTextQuery(text: query)
                        {
                            KNearestNeighborsCount = 3,
                            Fields = { "content_vector" },
                            Exhaustive = true
                        }
                    },
            };

            SearchResults<IndexDoc> response = await _searchClient.SearchAsync<IndexDoc>(query, searchOptions);

            await foreach (SearchResult<IndexDoc> searchResult in response.GetResultsAsync())
            {
                docs.Add(searchResult.Document);
            }

            return docs;
        }

        [KernelFunction("get_document_chunks")]
        public async Task<List<IndexDoc>> GetDocumentAsync(string documentId)
        {
            Console.WriteLine("get_document_chunks");

            List<IndexDoc> docs = new List<IndexDoc>();

            SearchOptions searchOptions = new SearchOptions
            {
                Size = 100,
                Filter = $"document_id eq '{documentId}'",
                QueryType = SearchQueryType.Full
            };

            SearchResults<IndexDoc> response = await _searchClient.SearchAsync<IndexDoc>("*", searchOptions);

            await foreach (SearchResult<IndexDoc> searchResult in response.GetResultsAsync())
            {
                IndexDoc indexDoc = new IndexDoc()
                {
                    ChunkId = searchResult.Document.ChunkId,
                    DocumentId = searchResult.Document.DocumentId,
                    ThreadId = searchResult.Document.ThreadId,
                    Content = searchResult.Document.Content,
                    FileName = searchResult.Document.FileName,
                };

                docs.Add(indexDoc);
            }

            docs = docs.OrderBy(x => x.ChunkId).ToList();
            return docs;
        }

        public async Task IsHealthyAsync()
        {
            try
            {
                // Attempt to get search stats to verify connection
                await _searchClient.GetDocumentCountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for Search service");
                throw new ServiceException("Search service health check failed", ex, ServiceType.SearchService);
            }
        }
    }
}
