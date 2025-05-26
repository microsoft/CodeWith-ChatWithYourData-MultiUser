using Domain.Cosmos;
using Domain.Search;
using Infrastructure.Helpers;
using Infrastructure.Implementations.KernelMemory;
using Infrastructure.Implementations.SPE;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using System.IO;
using WebApi.Helpers;

namespace WebApi.Services
{
    public class DocumentService
    {
        private readonly ILogger<DocumentService> _logger;
        private readonly IDocumentStore _documentStore;
        private readonly IDocumentRegistry _documentRegistry;
        private readonly ISearchService _searchService;
        private readonly IDocumentProcessorQueue _documentProcessorQueue;
        private readonly IAIService _aiService;
        private readonly IConfiguration _configuration;
        private readonly string _containerName;
        private readonly HashSet<string> _blockedFileExtensions;

        public DocumentService(
            ILogger<DocumentService> logger,
            IDocumentStore documentStore,
            IDocumentRegistry documentRegistry,
            ISearchService searchService,
            IDocumentProcessorQueue documentProcessorQueue,
            IAIService aiService,
            IConfiguration configuration)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _documentStore = documentStore ?? throw new ArgumentNullException(nameof(documentStore));
            _documentRegistry = documentRegistry ?? throw new ArgumentNullException(nameof(documentRegistry));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
            _documentProcessorQueue = documentProcessorQueue ?? throw new ArgumentNullException(nameof(documentProcessorQueue));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

            // Read the container name from configuration
            _containerName = _configuration.GetValue<string>("Storage:ContainerName") ?? "documents";

            // Read blocked file extensions from configuration
            var blockedExtensions = _configuration.GetSection("BlockedFileExtensions").Get<List<string>>() ?? new List<string>();
            _blockedFileExtensions = new HashSet<string>(blockedExtensions.Select(ext => ext.ToLower()));

            _logger.LogInformation("DocumentService initialized successfully");
        }

        public async Task<List<IndexDoc>> GetDocumentChunksAsync(string documentId)
        {
            _logger.LogInformation("Fetching chunks for documentId: {DocumentId}", documentId);
            var documents = await _searchService.GetDocumentAsync(documentId);
            _logger.LogInformation("Successfully fetched chunks for documentId: {DocumentId}", documentId);
            return documents;
        }

        public async Task<string> GetExtractedDocumentAsync(string threadId, string documentId)
        {
            _logger.LogInformation("Fetching extracted content for documentId: {DocumentId}", documentId);
            var document = await _searchService.GetExtractedResultsAsync(threadId, documentId);
            _logger.LogInformation("Successfully fetched extracted content for documentId: {DocumentId}", documentId);
            return document?.Extract;
        }

        public async Task<IEnumerable<DocsPerThread>> GetDocsPerThreadAsync(string threadId)
        {
            _logger.LogInformation("Fetching documents from CosmosDb for threadId: {ThreadId}", threadId);
            var documents = await _documentRegistry.GetDocsPerThreadAsync(threadId);

            if (documents == null || !documents.Any())
                return Enumerable.Empty<DocsPerThread>();

            var results = await _searchService.IsChunkingComplete(documents);
            _logger.LogInformation("Successfully checked chunking status for {Count} documents for threadId: {ThreadId}", results.Count(), threadId);
            return results;
        }

        public async Task<bool> ExtractDocumentAsync(string userId, string threadId, string documentId)
        {
            _logger.LogInformation("Extracting document with ID: {DocumentId}", documentId);
            var documents = await _searchService.GetDocumentAsync(documentId);

            if (documents == null || !documents.Any())
            {
                _logger.LogWarning("No documents found for ID: {DocumentId}", documentId);
                return false;
            }

            var extracted = await _aiService.ExtractDocument(documents);
            if (string.IsNullOrEmpty(extracted))
            {
                _logger.LogWarning("No extracted content found for documentId: {DocumentId}", documentId);
                return false;
            }

            bool ingested = await _searchService.IngestExtractedDocumentIntoIndex(extracted, documentId);
            if (!ingested)
            {
                _logger.LogWarning("Failed to ingest extracted content for documentId: {DocumentId}", documentId);
                return false;
            }

            _logger.LogInformation("Successfully ingested extracted content for documentId: {DocumentId}", documentId);
            return true;
        }

        public async Task<List<UploadResult>> UploadDocumentsAsync(string userId, List<IFormFile> documents, string threadId)
        {
            if (documents == null || !documents.Any())
            {
                _logger.LogWarning("No files uploaded.");
                return new List<UploadResult>();
            }

            const long maxFileSize = 100 * 1024 * 1024; // 100 MB limit
            var uploadResults = new List<UploadResult>();

            string containerName = await DetermineContainerNameAsync(threadId);

            foreach (var document in documents)
            {
                var result = await ProcessDocumentUploadAsync(userId, document, threadId, containerName, maxFileSize);
                uploadResults.Add(result);
            }

            return uploadResults;
        }

        public async Task<bool> RemoveDocumentAsync(string threadId, string documentId)
        {
            _logger.LogInformation("Attempting to soft delete document for threadId {ThreadId} and documentId {DocumentId}", threadId, documentId);
            var documentToDelete = await _documentRegistry.GetDocPerThreadAsync(threadId, documentId);

            if (documentToDelete == null)
            {
                _logger.LogWarning("Document not found for threadId {ThreadId} and documentId {DocumentId}", threadId, documentId);
                return false;
            }

            documentToDelete.Deleted = true;
            var removedFromSearch = await _searchService.DeleteDocumentAsync(documentToDelete);

            if (removedFromSearch)
                await _documentRegistry.RemoveDocumentAsync(documentToDelete);

            _logger.LogInformation("Successfully soft deleted document {DocumentId} for threadId {ThreadId}", documentId, threadId);
            return true;
        }

        public async Task<int> RemoveAllDocumentsFromThreadAsync(string threadId)
        {
            _logger.LogInformation("Attempting to soft delete all documents from CosmosDb for threadId: {ThreadId}", threadId);
            var results = await _documentRegistry.GetDocsPerThreadAsync(threadId);
            var documentsToDelete = results?.Where(doc => !doc.Deleted).ToList();

            if (documentsToDelete == null || !documentsToDelete.Any())
            {
                _logger.LogInformation("No active documents found to delete for threadId {ThreadId}", threadId);
                return 0;
            }

            _logger.LogInformation("Soft deleting {Count} documents from CosmosDb for threadId {ThreadId}", documentsToDelete.Count, threadId);

            foreach (var doc in documentsToDelete)
            {
                doc.Deleted = true;
                await _documentRegistry.RemoveDocumentAsync(doc);
            }

            _logger.LogInformation("Successfully soft deleted {Count} documents for threadId {ThreadId}", documentsToDelete.Count, threadId);
            return documentsToDelete.Count;
        }

        #region Helper Methods

        private async Task<string> DetermineContainerNameAsync(string threadId)
        {
            try
            {
                if (_documentStore is SPEDocumentStore)
                {
                    var threadDriveId = await _documentRegistry.GetFolderForThreadAsync(threadId);
                    if (!string.IsNullOrEmpty(threadDriveId))
                    {
                        _logger.LogInformation("Using thread-specific drive ID: {DriveId}", threadDriveId);
                        return threadDriveId;
                    }

                    if (!IsDriveId(threadDriveId))
                        return string.Empty; // Signal to create container on first upload

                    _logger.LogInformation("No specific drive ID found for thread {ThreadId}, will use default or create.", threadId);
                }
            }
            catch (NotSupportedException)
            {
                _logger.LogDebug("GetFolderForThreadAsync not supported by the current document registry implementation. Using configured container.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get drive ID for thread {ThreadId}. Falling back to default.", threadId);
            }

            string containerName = _configuration.GetValue<string>("Storage:ContainerName") ?? "documents";
            _logger.LogInformation("Using container name from configuration or default: {ContainerName}", containerName);
            return containerName;
        }

        private bool IsDriveId(string containerName)
        {
            return !string.IsNullOrEmpty(containerName) &&
                   containerName.StartsWith("b!") &&
                   containerName.Length > 10 &&
                   containerName.All(c => char.IsLetterOrDigit(c) || c == '!' || c == '-' || c == '_');
        }

        private async Task<UploadResult> ProcessDocumentUploadAsync(string userId, IFormFile document, string threadId, string containerName, long maxFileSize)
        {
            var result = new UploadResult { FileName = document.FileName, Success = false };

            try
            {
                // Check file size
                if (document.Length > maxFileSize)
                {
                    _logger.LogWarning("File size exceeds the limit: {FileName}", document.FileName);
                    result.ErrorMessage = "File size exceeds the limit.";
                    return result;
                }

                // Check file extension
                var fileExtension = Path.GetExtension(document.FileName)?.ToLowerInvariant();
                if (string.IsNullOrEmpty(fileExtension) || _blockedFileExtensions.Contains(fileExtension))
                {
                    _logger.LogWarning("Filetype blocked or invalid: {FileName}", document.FileName);
                    result.ErrorMessage = "File type is blocked or invalid.";
                    return result;
                }

                // Sanitize filename and upload
                var fileName = Utilities.SanitizeFileName(document.FileName);
                _logger.LogInformation("Uploading document: {FileName}", fileName);

                // Upload to document store
                DocsPerThread docsPerThread = await _documentStore.AddDocumentAsync(userId, document, fileName, threadId, containerName);
                if (docsPerThread == null)
                {
                    throw new Exception("File upload to store failed.");
                }

                // Update container name if created during first upload (SPE-specific)
                if (string.IsNullOrEmpty(containerName) && !string.IsNullOrEmpty(docsPerThread.Folder))
                {
                    containerName = docsPerThread.Folder;
                    _logger.LogInformation("Container created/identified for thread {ThreadId}: {ContainerName}", threadId, containerName);
                }

                // Add to registry
                var registryResult = await _documentRegistry.AddDocumentToThreadAsync(docsPerThread);
                if (string.IsNullOrEmpty(registryResult))
                {
                    throw new Exception("Failed to add document metadata to registry.");
                }

                // Process for search
                _logger.LogInformation("Enqueueing document for processing: {FileName}", fileName);
                using (var memoryStream = new MemoryStream())
                {
                    await document.CopyToAsync(memoryStream);
                    _documentProcessorQueue.EnqueueDocumentUri(new DocumentToProcess(
                        memoryStream.ToArray(),
                        fileName,
                        document.ContentType,
                        docsPerThread));
                }

                result.Success = true;
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document: {FileName}", document.FileName);
                result.ErrorMessage = $"An error occurred: {ex.Message}";
                return result;
            }
            finally
            {
                if (document is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        #endregion
    }

    public class UploadResult
    {
        public string FileName { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
    }
}