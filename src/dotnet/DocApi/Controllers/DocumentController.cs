using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Domain;
using Infrastructure;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using System.Reflection.Metadata;
using System.Xml.Linq;

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
        private readonly IConfiguration _configuration;
        private readonly string _containerName;

        public DocumentController(
            ILogger<DocumentController> logger,
            IDocumentStore blobDocumentStore,
            IDocumentRegistry cosmosDocumentRegistry,
            ISearchService aISearchService,
            IConfiguration configuration
            )
        {
            _documentStore = blobDocumentStore;
            _documentRegistry = cosmosDocumentRegistry;
            _searchService = aISearchService;
            _configuration = configuration;
            _logger = logger;

            _containerName = _configuration.GetValue<string>("Storage:ContainerName") ?? "documents";
        }

        [HttpGet(Name = "GetMyDocuments")]
        public async Task<IEnumerable<DocsPerThread>> Get([FromRoute] string threadId)
        {
            _logger.LogInformation("Fetching documents from CosmosDb for threadId : {0}", threadId);

            // fetch the documents from cosmos which belong to this thread
            var results = await _documentRegistry.GetDocsPerThread(threadId);

            _logger.LogInformation("Comparing documents from Cosmos against Search for threadId : {0}", threadId);

            // check for the uploaded docs if they are chunked
            return await _searchService.IsChunkingComplete(results);
        }

        [HttpPost(Name = "Upload")]
        public async Task<IActionResult> UploadDocuments(List<IFormFile> documents, string userId, [FromRoute] string threadId)
        {
            if (documents == null || !documents.Any())
            {
                _logger.LogWarning("No files uploaded.");
                return BadRequest("No files uploaded.");
            }

            var uploadResults = new List<string>();

            foreach (var document in documents)
            {
                try
                {
                    _logger.LogInformation("Uploading document: {0}", document.FileName);

                    // First step is to upload the document to the blob storage
                    DocsPerThread docsPerThread = await _documentStore.AddDocumentAsync(userId, document, threadId, _containerName);
                    if (docsPerThread == null)
                    {
                        throw new Exception("File upload failed");
                    }
                    _logger.LogInformation("Document uploaded to blob storage: {0}", document.FileName);

                    // Second step is to add the document to the cosmos db
                    var result = await _documentRegistry.AddDocumentToThreadAsync(docsPerThread);
                    _logger.LogInformation("Document added to Cosmos DB: {0}", document.FileName);

                    uploadResults.Add(result);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error uploading document: {document.FileName}");
                    uploadResults.Add($"Error uploading document: {document.FileName}");
                }
            }

            // Third step is to kick off the indexer
            _logger.LogInformation("Starting indexing process.");
            var chunks = await _searchService.StartIndexing();
            _logger.LogInformation("Indexing process started.");

            return Ok(uploadResults);
        }
    }
}