using Domain.Cosmos;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using WebApi.Filters;
using WebApi.Services;

namespace WebApi.Controllers
{
    [Route("/threads/{threadId}/documents")]
    [Authorize]
    [ApiController]
    [RequiredScope("chat")]
    [ServiceFilter(typeof(UserIdValidationFilter))]
    public class DocumentController : BaseApiController
    {
        private readonly DocumentService _documentService;

        public DocumentController(
            ILogger<DocumentController> logger,
            DocumentService documentService)
            : base(logger)
        {
            _documentService = documentService ?? throw new ArgumentNullException(nameof(documentService));
            _logger.LogInformation("DocumentController2 initialized successfully");
        }

        [HttpGet("{documentId}/chunks")]
        public async Task<IActionResult> GetChunks([FromRoute] string documentId)
        {
            try
            {
                string userId = GetUserId();
                var documents = await _documentService.GetDocumentChunksAsync(documentId);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetChunks", documentId);
            }
        }

        [HttpGet("{documentId}/extract")]
        public async Task<IActionResult> GetExtract([FromRoute] string threadId, [FromRoute] string documentId)
        {
            try
            {
                string userId = GetUserId();
                var extract = await _documentService.GetExtractedDocumentAsync(threadId, documentId);
                return Ok(extract);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetExtract", documentId);
            }
        }

        [HttpGet()]
        public async Task<IActionResult> Get([FromRoute] string threadId)
        {
            try
            {
                var documents = await _documentService.GetDocsPerThreadAsync(threadId);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetDocuments", threadId);
            }
        }

        [HttpPost("{documentId}", Name = "ExtractDocument")]
        public async Task<IActionResult> ExtractDocument([FromRoute] string threadId, string documentId)
        {
            try
            {
                string userId = GetUserId();

                if (string.IsNullOrEmpty(documentId))
                {
                    return BadRequest(new { error = "No documentId provided." });
                }

                bool success = await _documentService.ExtractDocumentAsync(userId, threadId, documentId);

                if (!success)
                {
                    return NotFound(new { error = "Document not found or extraction failed." });
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "ExtractDocument", documentId);
            }
        }

        [HttpPost()]
        public async Task<IActionResult> UploadDocuments(List<IFormFile> documents, [FromRoute] string threadId)
        {
            try
            {
                string userId = GetUserId();

                if (documents == null || !documents.Any())
                {
                    return BadRequest(new { error = "No files uploaded." });
                }

                var uploadResults = await _documentService.UploadDocumentsAsync(userId, documents, threadId);
                return Ok(uploadResults);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "UploadDocuments", threadId);
            }
        }

        [HttpDelete("{documentId}", Name = "DeleteDocumentFromThread")]
        public async Task<IActionResult> RemoveDocumentAsync([FromRoute] string threadId, string documentId)
        {
            try
            {
                string userId = GetUserId();
                
                if (string.IsNullOrEmpty(documentId))
                {
                    return BadRequest(new { error = "DocumentId is required" });
                }
                
                bool success = await _documentService.RemoveDocumentAsync(threadId, documentId);

                if (success)
                {
                    return Ok(new { success = true, message = "Document marked for deletion." });
                }

                return NotFound(new { error = "Document not found or already deleted." });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "RemoveDocument", $"{threadId}/{documentId}");
            }
        }

        [HttpDelete("", Name = "DeleteAllDocumentsInThread")]
        public async Task<IActionResult> RemoveAllDocumentsFromThreadAsync([FromRoute] string threadId)
        {
            try
            {
                string userId = GetUserId();
                int count = await _documentService.RemoveAllDocumentsFromThreadAsync(threadId);
                
                return Ok(new { success = true, message = $"{count} documents marked for deletion." });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "RemoveAllDocumentsFromThread", threadId);
            }
        }
    }
}