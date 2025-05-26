using Domain.Chat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using WebApi.Filters;
using WebApi.Services;

namespace WebApi.Controllers
{
    [Route("admin")]
    [Authorize]
    [ApiController]
    [RequiredScope("chat")]
    [ServiceFilter(typeof(UserIdValidationFilter))]
    public class AdminController : BaseApiController
    {
        private readonly AdminService _adminService;

        public AdminController(
            ILogger<AdminController> logger,
            AdminService adminService)
            : base(logger)
        {
            _adminService = adminService ?? throw new ArgumentNullException(nameof(adminService));
            _logger.LogInformation("AdminController2 initialized successfully");
        }

        [HttpGet("settings")]
        public IActionResult GetSettings()
        {
            try
            {
                string userId = GetUserId();
                _logger.LogInformation("Fetching settings for user {UserId}", userId);
                
                var settings = _adminService.GetSettings();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetSettings");
            }
        }

        [HttpPatch("settings")]
        [Consumes("application/json")]
        public IActionResult UpdateSettings([FromBody] Settings settings)
        {
            try
            {
                string userId = GetUserId();
                
                if (settings == null)
                {
                    return BadRequest(new { error = "Settings payload is required" });
                }

                _logger.LogInformation("Updating settings by user {UserId}", userId);
                var updatedSettings = _adminService.UpdateSettings(settings);
                
                return Ok(updatedSettings);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "UpdateSettings");
            }
        }

        [HttpGet("documents")]
        public async Task<IActionResult> GetDocuments()
        {
            try
            {
                string userId = GetUserId();
                _logger.LogInformation("Admin {UserId} fetching all documents", userId);
                
                var documents = await _adminService.GetAllDocumentsAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetDocuments");
            }
        }

        [HttpGet("documents/{documentId}/chunks")]
        public async Task<IActionResult> GetChunks([FromRoute] string documentId)
        {
            try
            {
                string userId = GetUserId();
                
                if (string.IsNullOrEmpty(documentId))
                {
                    return BadRequest(new { error = "DocumentId is required" });
                }

                _logger.LogInformation("Admin {UserId} fetching chunks for documentId: {DocumentId}", userId, documentId);
                var chunks = await _adminService.GetDocumentChunksAsync(documentId);
                
                return Ok(chunks);
            }
            catch (ArgumentNullException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetChunks", documentId);
            }
        }

        [HttpGet("threads")]
        public async Task<IActionResult> GetThreads()
        {
            try
            {
                string userId = GetUserId();
                _logger.LogInformation("Admin {UserId} fetching all threads", userId);
                
                var threads = await _adminService.GetAllThreadsAsync();
                return Ok(threads);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetThreads");
            }
        }

        [HttpGet("check")]
        public async Task<IActionResult> Check()
        {
            try
            {
                string userId = GetUserId();
                _logger.LogInformation("Admin {UserId} performing system health check", userId);
                
                var healthStatus = await _adminService.CheckSystemHealthAsync();
                return Ok(healthStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed with an unexpected error");
                return StatusCode(StatusCodes.Status500InternalServerError, new { 
                    error = "Health check failed with an unexpected error", 
                    details = ex.Message,
                    requestId = HttpContext.TraceIdentifier
                });
            }
        }
    }
}