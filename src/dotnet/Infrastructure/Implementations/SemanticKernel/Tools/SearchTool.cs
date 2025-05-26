using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Models;
using DocumentFormat.OpenXml.Wordprocessing;
using Domain.Search;
using Microsoft.Extensions.Configuration;
using Microsoft.KernelMemory.Models;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Implementations.SemanticKernel.Tools
{
    public class SearchTool
    {
        private readonly SearchClient _searchClient;
        private readonly SearchIndexClient _indexClient;
        private string indexName;

        public SearchTool(SearchIndexClient searchIndexClient)
        {
            _indexClient = searchIndexClient;
         

            indexName = "onyourdata";
            _indexClient = searchIndexClient;
            _searchClient = searchIndexClient.GetSearchClient(indexName);
        }

        [KernelFunction("get_document_chunks")]
        public async Task<List<IndexDoc>> GetDocumentAsync(string documentId)
        {
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

        [KernelFunction("get_extracted_results")]
        public async Task<IndexDoc> GetExtractedResultsAsync(string threadId, string documentId)
        {
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
    }
}
