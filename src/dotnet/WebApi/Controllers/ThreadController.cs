using Domain.Chat;
using Domain.Cosmos;
using Infrastructure.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Identity.Web.Resource;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using OpenAI.Chat;
using System.Net;
using WebApi.Filters;
using WebApi.Services;

namespace WebApi.Controllers
{
    [Route("threads")]
    [Authorize]
    [ApiController]
    [RequiredScope("chat")]
    [ServiceFilter(typeof(UserIdValidationFilter))]
    public class ThreadController : BaseApiController
    {
        private readonly ThreadService _threadService;

        public ThreadController(
            ILogger<ThreadController> logger,
            ThreadService threadService) 
            : base(logger)
        {
            _threadService = threadService ?? throw new ArgumentNullException(nameof(threadService));
            _logger.LogInformation("ThreadController2 initialized successfully");
        }

        [HttpGet("")]
        public async Task<IActionResult> GetThreads()
        {
            try
            {
                string userId = GetUserId();
                var threads = await _threadService.GetThreadsAsync(userId);
                return Ok(threads);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetThreads");
            }
        }

        [HttpPost("")]
        public async Task<IActionResult> CreateThread()
        {
            try
            {
                string userId = GetUserId();
                var thread = await _threadService.CreateThreadAsync(userId);
                
                if (thread == null)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, 
                        new { error = "Failed to create thread", requestId = HttpContext.TraceIdentifier });
                }

                return Ok(thread);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "CreateThread");
            }
        }

        [HttpDelete("{threadId}")]
        public async Task<IActionResult> DeleteThread([FromRoute] string threadId)
        {
            try
            {
                if (string.IsNullOrEmpty(threadId))
                {
                    return BadRequest(new { error = "ThreadId is required" });
                }

                string userId = GetUserId();
                bool result = await _threadService.DeleteThreadAsync(userId, threadId);

                if (result)
                {
                    return Ok(new { success = true });
                }

                return NotFound(new { error = "Thread not found or delete operation failed" });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "DeleteThread", threadId);
            }
        }

        [HttpPatch("{threadId}")]
        public async Task<IActionResult> UpdateThread([FromRoute] string threadId, [FromBody] string title)
        {
            try
            {
                if (string.IsNullOrEmpty(threadId))
                {
                    return BadRequest(new { error = "ThreadId is required" });
                }

                if (string.IsNullOrEmpty(title))
                {
                    return BadRequest(new { error = "Title is required" });
                }

                string userId = GetUserId();
                var updatedThread = await _threadService.UpdateThreadTitleAsync(userId, threadId, title);

                if (updatedThread != null)
                {
                    return Ok(updatedThread);
                }

                return NotFound(new { error = "Thread not found or update operation failed" });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "UpdateThread", threadId);
            }
        }

        [HttpGet("{threadId}/messages")]
        public async Task<IActionResult> GetMessages([FromRoute] string threadId, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(threadId))
                {
                    return BadRequest(new { error = "ThreadId is required" });
                }

                string userId = GetUserId();
                var messages = await _threadService.GetMessagesAsync(userId, threadId, cancellationToken);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "GetMessages", threadId);
            }
        }

        [HttpDelete("{threadId}/messages")]
        public async Task<IActionResult> DeleteMessages([FromRoute] string threadId, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(threadId))
                {
                    return BadRequest(new { error = "ThreadId is required" });
                }

                string userId = GetUserId();
                bool result = await _threadService.DeleteMessagesAsync(userId, threadId, cancellationToken);
                
                if (result)
                {
                    return Ok(new { success = true });
                }
                
                return NotFound(new { error = "Thread not found or delete messages operation failed" });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "DeleteMessages", threadId);
            }
        }

        [HttpPost("{threadId}/messages")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<IActionResult> PostMessage(
            [FromRoute] string threadId, 
            [FromBody] MessageRequest messageRequest, 
            CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(threadId))
                {
                    return BadRequest(new { error = "Thread ID is required." });
                }

                if (messageRequest == null || string.IsNullOrEmpty(messageRequest.Message))
                {
                    return BadRequest(new { error = "Message content is required." });
                }

                string userId = GetUserId();
                ThreadMessage answer;
                
                // Process message with or without specific documents
                if (messageRequest.DocumentIds?.Count > 0)
                {
                    answer = await _threadService.ProcessUserMessageWithDocumentsAsync(
                        userId, threadId, messageRequest.Message, messageRequest.DocumentIds, cancellationToken);
                }
                else
                {
                    answer = await _threadService.ProcessUserMessageAsync(
                        userId, threadId, messageRequest.Message, cancellationToken);
                }
                
                return Ok(answer);
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Service error in Post message: {Type} - {Message}", ex.ServiceType, ex.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { 
                        error = $"Service unavailable: {ex.ServiceType}", 
                        details = ex.Message,
                        requestId = HttpContext.TraceIdentifier
                    });
            }
            catch (HttpOperationException httpEx)
            {
                _logger.LogError(httpEx, "HTTP operation failed: {Message}", httpEx.Message);
                return StatusCode((int)(httpEx.StatusCode ?? HttpStatusCode.InternalServerError),
                    new { 
                        error = "Service temporarily unavailable", 
                        details = httpEx.Message,
                        requestId = HttpContext.TraceIdentifier
                    });
            }
            catch (Exception ex)
            {
                return HandleException(ex, "PostMessage", threadId);
            }
        }

        [HttpPost("{threadId}/messages/stream")]
        [Produces("text/event-stream")]
        public async Task<IActionResult> StreamChatResponse(
            [FromRoute] string threadId, 
            [FromBody] MessageRequest messageRequest, 
            CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(threadId))
                {
                    return BadRequest(new { error = "Thread ID is required." });
                }

                if (messageRequest == null || string.IsNullOrEmpty(messageRequest.Message))
                {
                    return BadRequest(new { error = "Message content is required." });
                }

                _logger.LogInformation("Processing streaming chat response for threadId: {ThreadId}", threadId);
                
                string userId = GetUserId();
                var stream = await _threadService.GetStreamingResponseAsync(userId, threadId, messageRequest, cancellationToken);
                
                // Process the streaming response
                await ProcessStreamAsync(userId, threadId, stream, cancellationToken);
                
                return new EmptyResult();
            }
            catch (ServiceException ex)
            {
                _logger.LogError(ex, "Service error in stream chat: {Type} - {Message}", ex.ServiceType, ex.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new {
                        error = $"Service unavailable: {ex.ServiceType}",
                        details = ex.Message,
                        requestId = HttpContext.TraceIdentifier
                    });
            }
            catch (HttpOperationException httpEx)
            {
                _logger.LogError(httpEx, "HTTP operation failed: {Message}", httpEx.Message);
                return StatusCode((int)(httpEx.StatusCode ?? HttpStatusCode.InternalServerError),
                    new {
                        error = "Service temporarily unavailable",
                        details = httpEx.Message,
                        requestId = HttpContext.TraceIdentifier
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing streaming chat for threadId: {ThreadId}", threadId);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new {
                        error = "An unexpected error occurred",
                        details = ex.Message,
                        requestId = HttpContext.TraceIdentifier
                    });
            }
        }

        #region Private Methods
        
        private async Task ProcessStreamAsync(
            string userId,
            string threadId,
            IAsyncEnumerable<StreamingChatMessageContent> stream,
            CancellationToken cancellationToken)
        {
            string finalContent = string.Empty;
            UsageMetrics? usageMetrics = new();

            // Process and send each chunk
            await foreach (var chatResponse in stream.WithCancellation(cancellationToken))
            {
                // Send chunk to client
                string payload = System.Text.Json.JsonSerializer.Serialize(
                    new { role = chatResponse.Role.ToString(), content = chatResponse.Content, final = false });
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                // Extract usage metrics if available
                if (chatResponse.InnerContent is StreamingChatCompletionUpdate chatCompletion &&
                    chatCompletion.Usage != null)
                {
                    usageMetrics.InputTokens = chatCompletion.Usage.InputTokenCount;
                    usageMetrics.OutputTokens = chatCompletion.Usage.OutputTokenCount;
                }

                finalContent += chatResponse.Content;
            }

            // Send final message with follow-up questions
            if (!string.IsNullOrWhiteSpace(finalContent))
            {
                await SendFinalMessageAsync(userId, threadId, finalContent, usageMetrics, cancellationToken);
            }
        }

        private async Task SendFinalMessageAsync(
            string userId,
            string threadId,
            string content,
            UsageMetrics usageMetrics,
            CancellationToken cancellationToken)
        {
            // Get conversation history for generating follow-up questions
            var history = await _threadService.BuildConversationHistoryAsync(userId, threadId, content, cancellationToken);
            
            // Generate follow-up questions
            var followUpQuestionList = await _threadService.GenerateFollowupQuestionsAsync(
                history, content, cancellationToken);

            // Send the final message with follow-up questions
            string finalPayload = System.Text.Json.JsonSerializer.Serialize(
                new {
                    role = "assistant",
                    content = content,
                    followupQuestions = followUpQuestionList,
                    usageMetrics = usageMetrics,
                    final = true
                });
                
            await Response.WriteAsync($"data: {finalPayload}\n\n", cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            // Save the final message asynchronously
            _ = Task.Run(async () =>
            {
                try
                {
                    await _threadService.SaveStreamingResponseAsync(userId, threadId, content, followUpQuestionList, usageMetrics);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving final chat response for threadId: {ThreadId}", threadId);
                }
            }, CancellationToken.None);
        }
        
        #endregion
    }
}