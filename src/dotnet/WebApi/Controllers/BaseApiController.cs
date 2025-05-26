using Infrastructure.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System.Net;
using WebApi.Helpers;

namespace WebApi.Controllers
{
    public abstract class BaseApiController : ControllerBase
    {
        protected readonly ILogger _logger;

        protected BaseApiController(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected string? GetUserId()
        {
            return HttpContext.GetUserId();
        }

        protected IActionResult HandleUserIdValidation()
        {
            string? userId = GetUserId();
            if (userId == null)
            {
                _logger.LogWarning("{MethodName} called with null userId", GetCallerMethodName());
                return Unauthorized(new { error = "UserId is required" });
            }
            return null;
        }

        protected IActionResult HandleException(Exception ex, string operation, string resourceId = null)
        {
            if (ex is ServiceException serviceEx)
            {
                _logger.LogError(ex, "Service error in {Operation}: {Type} - {Message}", 
                    operation, serviceEx.ServiceType, serviceEx.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                    new { error = $"Service unavailable: {serviceEx.ServiceType}", details = serviceEx.Message });
            }
            else if (ex is HttpOperationException httpEx)
            {
                _logger.LogError(ex, "HTTP operation failed in {Operation}: {Message}", operation, httpEx.Message);
                return StatusCode((int)(httpEx.StatusCode ?? HttpStatusCode.InternalServerError), 
                    new { 
                        error = "Service temporarily unavailable", 
                        details = httpEx.Message,
                        requestId = HttpContext.TraceIdentifier
                    });
            }
            else
            {
                string logMessage = resourceId == null 
                    ? $"Unexpected error in {operation}" 
                    : $"Unexpected error in {operation} for {resourceId}";
                
                _logger.LogError(ex, logMessage);
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new { 
                        error = "An unexpected error occurred", 
                        details = ex.Message,
                        requestId = HttpContext.TraceIdentifier
                    });
            }
        }

        protected IActionResult NotFoundResponse(string resourceType, string resourceId)
        {
            _logger.LogWarning("{ResourceType} not found: {ResourceId}", resourceType, resourceId);
            return NotFound(new { error = $"{resourceType} not found" });
        }

        protected IActionResult OkResponse<T>(T data, string message = null)
        {
            if (message == null)
                return Ok(data);
            
            return Ok(new { success = true, data, message });
        }

        private string GetCallerMethodName()
        {
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            var frame = stackTrace.GetFrame(2); // 0 is current method, 1 is caller's method, 2 is caller's caller's method
            return frame?.GetMethod()?.Name ?? "Unknown";
        }
    }
}