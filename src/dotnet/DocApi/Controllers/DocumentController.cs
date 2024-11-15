using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Domain;
using Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web.Resource;
using System.Reflection.Metadata;
using System.Xml.Linq;
using WebApi.Helpers;

namespace DocApi.Controllers
{
    [Route("/threads/{threadId}/documents")]
    [Authorize]
    [ApiController]
    [RequiredScope("chat")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentStore _documentStore;
        private readonly IDocumentRegistry _documentRegistry;
        private readonly ISearchService _searchService;
        private readonly ILogger<DocumentController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _containerName;
        private readonly HashSet<string> _blockedFileExtensions;

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


            // Read the container name from configuration
            _containerName = _configuration.GetValue<string>("Storage:ContainerName") ?? "documents";

            // Read blocked file extensions from configuration
            var blockedExtensions = _configuration.GetSection("BlockedFileExtensions").Get<List<string>>() ?? new List<string>();
            _blockedFileExtensions = new HashSet<string>(blockedExtensions.Select(ext => ext.ToLower()));

        }

        [HttpGet()]
        public async Task<IEnumerable<DocsPerThread>> Get([FromRoute] string threadId)
        {
            _logger.LogInformation("Fetching documents from CosmosDb for threadId : {0}", threadId);

            // fetch the documents from cosmos which belong to this thread
            var documents = await _documentRegistry.GetDocsPerThreadAsync(threadId);

            _logger.LogInformation("Comparing documents from Cosmos against Search for threadId : {0}", threadId);

            // if there are no documents with the current thread, return an empty collection
            if (documents == null || documents.Count == 0)
                return Enumerable.Empty<DocsPerThread>();

            // check for the uploaded docs if they are chunked
            return await _searchService.IsChunkingComplete(documents);
        }

        [HttpPost()]
        public async Task<IActionResult> UploadDocuments(List<IFormFile> documents, [FromRoute] string threadId)
        {
            string userId = HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (userId == null)
            {
                return BadRequest();
            }

            if (documents == null || !documents.Any())
            {
                _logger.LogWarning("No files uploaded.");
                return BadRequest("No files uploaded.");
            }

            // Define the maximum file size limit (e.g., 100 MB)
            const long maxFileSize = 100 * 1024 * 1024;

            var uploadResults = new List<string>();

            foreach (var document in documents)
            {
                try
                {
                    // Check if the file size exceeds the limit
                    if (document.Length > maxFileSize)
                    {
                        _logger.LogWarning("File size exceeds the limit: {0}", document.FileName);
                        uploadResults.Add($"File size exceeds the limit: {document.FileName}");
                        continue;
                    }

                    // check if the file extension is blocked
                    if (_blockedFileExtensions.Contains(document.FileName.Split('.').Last().ToLower()))
                    {
                        _logger.LogWarning("Filetype blocked: {0}", document.FileName);
                        continue;
                    }

                    // Sanitize the filename to ensure it contains only ASCII characters
                    var fileName = Utilities.SanitizeFileName(document.FileName);

                    _logger.LogInformation("Uploading document: {0}", document.FileName);

                    // First step is to upload the document to the blob storage
                    DocsPerThread docsPerThread = await _documentStore.AddDocumentAsync(userId, document, fileName, threadId, _containerName);
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

            if (uploadResults != null)
            {
                // Third step is to kick off the indexer
                _logger.LogInformation("Starting indexing process.");
                var chunks = await _searchService.StartIndexing();
                _logger.LogInformation("Indexing process started.");
                return Ok(uploadResults);
            }

            return Ok();
        }

        [HttpDelete("{documentId}", Name = "DeleteDocumentFromThread")]
        public async Task<IActionResult> RemoveDocumentAsync([FromRoute] string threadId, string documentId)
        {
            _logger.LogInformation("Fetching documents from CosmosDb for threadId {0} and documentId {1}", threadId, documentId);

            // fetch the documents from cosmos which belong to this thread
            var results = await _documentRegistry.GetDocsPerThreadAsync(threadId);
            var updatedResults = results
                .Where(doc => doc.Id == documentId)
                .Select(doc => { doc.Deleted = true; return doc; })
                .ToList();

            // if the document is found, soft delete it
            if (updatedResults != null && updatedResults.Any())
            {
                _logger.LogInformation("Soft deleting document from CosmosDb for threadId {0} and documentId {1}", threadId, documentId);
                await _documentRegistry.RemoveDocumentAsync(updatedResults.First());
                return Ok();
            }

            // if the document is not found, return a 404
            return NotFound();
        }

        [HttpDelete("", Name = "DeleteDocument")]
        public async Task<IActionResult> RemoveDocumentFromThreadAsync([FromRoute] string threadId)
        {
            _logger.LogInformation("Fetching documents from CosmosDb for threadId : {0}", threadId);

            // fetch the documents from cosmos which belong to this thread
            var results = await _documentRegistry.GetDocsPerThreadAsync(threadId);
            var updatedResults = results.Select(doc => { doc.Deleted = true; return doc; }).ToList();

            if (updatedResults != null && updatedResults.Any())
            {
                _logger.LogInformation("Soft deleting from CosmosDb for threadId {0}", threadId);
                await _documentRegistry.RemoveDocumentAsync(updatedResults.First());
                return Ok();
            }


            return NotFound();
        }
    }
}
