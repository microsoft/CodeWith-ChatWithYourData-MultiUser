using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Domain;

namespace Infrastructure
{
    public class AISearchService : ISearchService
    {
        private SearchIndexClient _indexClient;
        private SearchClient _searchClient;
        private SearchIndexerClient _indexerClient;

        private readonly string indexerName = "onyourdata-indexer";

        public AISearchService(SearchIndexClient indexClient, SearchClient searchClient, SearchIndexerClient indexerClient)
        {
            _indexClient = indexClient;
            _searchClient = searchClient;
            _indexerClient = indexerClient;
        }


        public async Task<bool> StartIndexing()
        {
            var response = await _indexerClient.RunIndexerAsync(indexerName);
            if (response.IsError)
                return false;
            return true;
        }

        public async Task<bool> DeleteDocumentAsync(DocsPerThread document)
        {
            try
            {

                IndexDocumentsBatch<SearchDocument> batch =
                    IndexDocumentsBatch.Create(IndexDocumentsAction.Delete("chunk_id", document.ChunkId));

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
            string vectorQuery = string.Format("thread_id = '{0}'", docsPerThreads.First().ThreadId);
            var query = new VectorizableTextQuery(vectorQuery);
            query.Fields.Add("content_vector");

            var searchOptions = new SearchOptions
            {
                VectorSearch = new()
                {
                    Queries = { query }
                },
                Size = 50,
                Select = { "chunk_id", "content_vector", "file_name", "document_id", "thread_id" },
            };

            SearchResults<SearchDocument> response = await _searchClient.SearchAsync<SearchDocument>(null, searchOptions);

            // temporary list to store the documents that are found in the search index
            var fromSearch = new Dictionary<string, string>();
            await foreach (SearchResult<SearchDocument> result in response.GetResultsAsync())
            {
                string chunkId = result.Document["chunk_id"].ToString();
                string documentId = result.Document["document_id"].ToString();
                if (string.IsNullOrEmpty(chunkId) || string.IsNullOrEmpty(documentId))
                    continue;

                fromSearch[documentId] = chunkId;
            }

            // Use LINQ to update the AvailableInSearchIndex and SearchId properties
            var returnSet = docsPerThreads.Select(dpt =>
            {
                if (fromSearch.TryGetValue(dpt.Id, out var chunkId))
                {
                    dpt.AvailableInSearchIndex = true;
                    dpt.ChunkId = chunkId;
                }
                else
                {
                    dpt.AvailableInSearchIndex = false;
                    dpt.ChunkId = null;
                }
                return dpt;
            }).ToList();

            return returnSet;
        }
    }
}
