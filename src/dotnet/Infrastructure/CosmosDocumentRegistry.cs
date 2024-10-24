using Domain;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Linq;
using System.Reflection.Metadata;
using Container = Microsoft.Azure.Cosmos.Container;

namespace Infrastructure
{
    public class CosmosDocumentRegistry : IDocumentRegistry
    {
        private readonly Container _container;

        public CosmosDocumentRegistry(Container cosmosDbContainer)
        {
            _container = cosmosDbContainer;
        }

        public async Task<string> AddDocumentToThreadAsync(DocsPerThread docsPerThread)
        {
            try
            {
                var response = await _container.CreateItemAsync(docsPerThread, new PartitionKey(docsPerThread.UserId));
                if (response.StatusCode != System.Net.HttpStatusCode.Created)
                {
                    throw new Exception("Failed to add document to Document Registry");
                }
                return response.Resource.Id;
            }
            catch (CosmosException cosmosEx)
            {
                throw cosmosEx;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<string> UpdateDocumentAsync(DocsPerThread docsPerThread)
        {
            var response = await _container.ReplaceItemAsync<DocsPerThread>(docsPerThread, docsPerThread.Id, new PartitionKey(docsPerThread.UserId));
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Failed to change document");
            }
            return response.Resource.Id;
        }
        public async Task<bool> DeleteDocumentAsync(DocsPerThread docsPerThread)
        {
            var response = await _container.DeleteItemAsync<DocsPerThread>(docsPerThread.Id, new PartitionKey(docsPerThread.UserId));
            if (response.StatusCode != System.Net.HttpStatusCode.NoContent)
            {
                throw new Exception("Failed to delete document");
            }
            return true;
        }


        internal async Task<bool> MarkDocumentAsDeletedAsync(string documentId, string userId)
        {
            var fieldsToUpdate = new Dictionary<string, object>
            {
                { "deleted", true },
            };

            try
            {
                return await UpdateDocumentFieldsAsync(documentId, userId, fieldsToUpdate);
            }
            catch (CosmosException cosmosEx)
            {
                throw new Exception($"Failed to mark document as deleted: {cosmosEx.Message}", cosmosEx);
            }
            catch (Exception ex)
            {
                throw new Exception($"An error occurred while marking document as deleted: {ex.Message}", ex);
            }
        }

        public async Task<bool> RemoveDocumentAsync(DocsPerThread document)
        {
            return await MarkDocumentAsDeletedAsync(document.Id, document.UserId);
        }

        public async Task<bool> RemoveDocumentFromThreadAsync(List<DocsPerThread> documents)
        {
            foreach (var document in documents)
            {
                bool isUpdated = await MarkDocumentAsDeletedAsync(document.Id, document.UserId);
                if (!isUpdated)
                {
                    return false;
                }
            }

            return true;
        }

        internal async Task<bool> UpdateDocumentFieldsAsync(string documentId, string userId, Dictionary<string, object> fieldsToUpdate)
        {
            var patchOperations = new List<PatchOperation>();

            foreach (var field in fieldsToUpdate)
            {
                patchOperations.Add(PatchOperation.Set($"/{field.Key}", field.Value));
            }

            try
            {
                var response = await _container.PatchItemAsync<DocsPerThread>(documentId, new PartitionKey(userId), patchOperations);
                return response.StatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (CosmosException ex)
            {
                // Handle exception
                throw new Exception($"Failed to update document: {ex.Message}", ex);
            }
        }

        public async Task<List<DocsPerThread>> GetDocsPerThreadAsync(string threadId)
        {
            var queryable = _container.GetItemLinqQueryable<DocsPerThread>(requestOptions: new QueryRequestOptions { MaxItemCount = 500 })
                                      .Where(d => d.ThreadId == threadId);

            var documents = new List<DocsPerThread>();
            using (var iterator = queryable.ToFeedIterator())
            {
                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    documents.AddRange(response);
                }
            }

            return documents;
        }
    }
}