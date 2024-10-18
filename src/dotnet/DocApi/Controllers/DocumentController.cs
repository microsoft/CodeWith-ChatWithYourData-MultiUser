using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Domain;
using Infrastructure;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using System.Reflection.Metadata;

namespace DocApi.Controllers
{
    [ApiController]
    [Route("/chats/{threadId}/[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly IDocumentRegistry _documentRegistry;
        private readonly ISearchService _searchService;
        private readonly ILogger<DocumentController> _logger;
        private string _containerName = "documents";

        public DocumentController(
            ILogger<DocumentController> logger,
            IDocumentStore blobDocumentStore,
            IDocumentRegistry cosmosDocumentRegistry,
            ISearchService aISearchService
            )
        {
            _documentStore = blobDocumentStore;
            _documentRegistry = cosmosDocumentRegistry;
            _searchService = aISearchService;
            _logger = logger;
        }

        [HttpGet(Name = "GetMyDocuments")]
        public async Task<IEnumerable<DocsPerThread>> Get([FromRoute] string threadId)
        {
            // fetch the documents from cosmos which belong to this thread
            var results = await _documentRegistry.GetDocsPerThread(threadId);

            // check for the uploaded docs if they are chunked
            return await _searchService.IsChunkingComplete(results);
        }

        // this is test code to upload a document, just grabbing a local file
        //[HttpPost(Name = "Upload")]
        //public async Task<string> UploadDocument(string? document, string userId, [FromRoute] string threadId)
        //{
        //    if (string.IsNullOrEmpty(document))
        //        document = "C:\\Users\\rmeure\\downloads\\Brochure Elektrisch Rijden nieuw.pdf";

        //    // first step is to upload the document to the blob storage
        //    DocsPerThread docsPerThread = await _documentStore.AddDocumentAsync(userId, document, threadId, _containerName);
        //    if (docsPerThread == null)
        //        throw new Exception("File upload failed");

        //    // second step is to add the document to the cosmos db
        //    var result = await _documentRegistry.AddDocumentToThreadAsync(docsPerThread);

        //    // tnird step is to kick off the indexer
        //    var chunks = await _searchService.StartIndexing();

        //    return result;
        //}

        [HttpPost(Name = "Upload")]
        public async Task<IActionResult> UploadDocuments(List<IFormFile> documents, string userId, [FromRoute] string threadId)
        {
            if (documents == null || !documents.Any())
            {
                return BadRequest("No files uploaded.");
            }

            var uploadResults = new List<string>();

            foreach (var document in documents)
            {
                try
                {
                    // First step is to upload the document to the blob storage
                    DocsPerThread docsPerThread = await _documentStore.AddDocumentAsync(userId, document, threadId, _containerName);
                    if (docsPerThread == null)
                    {
                        throw new Exception("File upload failed");
                    }

                    // Second step is to add the document to the cosmos db
                    var result = await _documentRegistry.AddDocumentToThreadAsync(docsPerThread);

                    uploadResults.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error uploading document: {document.FileName}");
                    uploadResults.Add($"Error uploading document: {document.FileName}");
                }
            }

            // Third step is to kick off the indexer
            var chunks = await _searchService.StartIndexing();
            return Ok(uploadResults);
        }
    }
}
