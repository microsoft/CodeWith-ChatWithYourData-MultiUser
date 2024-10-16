﻿using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure
{
    public class BlobDocumentStore : IDocumentStore
    {
        private BlobServiceClient _blobServiceClient;

        public BlobDocumentStore(BlobServiceClient client) {
            _blobServiceClient = client;
        }

        public async Task<DocsPerThread> AddDocumentAsync(string userId, string document, string threadId, string folder)
        {
            var documentId = Guid.NewGuid().ToString();
            var blobContainerClient = _blobServiceClient.GetBlobContainerClient(folder);
            var documentName = System.IO.Path.GetFileName(document);
            var blobClient = blobContainerClient.GetBlobClient(documentId);

            //Upload the document
            await blobClient.UploadAsync(document, true);

            //set meta data
            var metadata = new Dictionary<string, string>
            {               
                { "threadId", threadId },
                { "documentId", documentId },
                { "originalFilename", documentName }
            };
            blobClient.SetMetadata(metadata);

            // creating the document object to be returned so that the controller can store it in the cosmos db
            DocsPerThread docsPerThread = new()
            {
                Id = documentId,
                ThreadId = threadId,
                DocumentName = documentName,
                UserId = userId
            };

            return docsPerThread;
        }

        public Task DeleteDocumentAsync(string documentName, string folder)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DocumentExistsAsync(string documentName, string folder)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<string>> GetDocumentsAsync(string threadId, string folder)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(folder);
            var results = new List<string>();

            await foreach (BlobItem blob in containerClient.GetBlobsAsync())
            {
                var blobClient = containerClient.GetBlobClient(blob.Name);
                var properties = await blobClient.GetPropertiesAsync();
                var metadata = properties.Value.Metadata;

                // Optionally, you can filter blobs based on metadata, e.g., by threadId
                if (metadata.TryGetValue("threadId", out var blobThreadId) && blobThreadId == threadId)
                {
                    results.Add(blob.Name);
                }
            }

            return results;
        }

        public Task UpdateDocumentAsync(string documentName, string documentUri)
        {
            throw new NotImplementedException();
        }
    }
}
