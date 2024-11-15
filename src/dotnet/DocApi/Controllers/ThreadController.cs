using Domain;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Data;
using System.Text;
using DocApi.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web.Resource;
using Microsoft.Azure.Cosmos;
using System.Text.Json;
using WebApi.Helpers;
using ResponseMessage = WebApi.Entities.ResponseMessage;
using System.Text.RegularExpressions;
using WebApi.Entities;

namespace DocApi.Controllers
{

    [Route("threads")]
    [Authorize]
    [ApiController]
    [RequiredScope("chat")]

    public class ThreadController : ControllerBase
    {
        private readonly IThreadRepository _threadRepository;
        private readonly ILogger<ThreadController> _logger;
        private readonly IConfiguration _configuration;
        private readonly Kernel _kernel;
        private readonly VectorStoreTextSearch<IndexDoc> _search;
        private readonly PromptHelper _promptHelper;

        

        public ThreadController(
            ILogger<ThreadController> logger,
            IThreadRepository cosmosThreadRepository,
            IConfiguration configuration,
            Kernel kernel,
            VectorStoreTextSearch<IndexDoc> search,
            PromptHelper promptHelper
            )
        {
            _threadRepository = cosmosThreadRepository;
            _configuration = configuration;
            _logger = logger;
            _kernel = kernel;
            _search = search;
            _promptHelper = promptHelper;
        }

        [HttpGet("")]
        public async Task<IActionResult> GetThreads()
        {
            string? userId = HttpContext.GetUserId();

            if (userId == null)
            {
                return BadRequest();
            }

            _logger.LogInformation("Fetching threads from CosmosDb for userId : {0}", userId);
            
            List<Domain.Thread> threads = await _threadRepository.GetThreadsAsync(userId);

            _logger.LogInformation("Fetched threads from CosmosDb for userId : {0}", userId);
            return Ok(threads);
        }

        [HttpPost("")]
        public async Task<IActionResult> CreateThread()
        {
            string? userId = HttpContext.GetUserId();

            if (userId == null)
            {
                return BadRequest();
            }
            _logger.LogInformation("Creating thread in CosmosDb for userId : {0}", userId);

            Domain.Thread thread = await _threadRepository.CreateThreadAsync(userId);

            if(thread == null)
            {
                _logger.LogInformation("Failed to create thread in CosmosDb for userId : {0}", userId);
            }

            _logger.LogInformation("Created thread in CosmosDb for userId : {0}", userId);
            
            await _threadRepository.PostMessageAsync(userId, thread.Id, "You are a helpful assistant that helps people find information.", "system");

            return Ok(thread);
        }

        [HttpDelete("{threadId}")]
        public async Task<IActionResult> DeleteThread([FromRoute] string threadId)
        {
            string? userId = HttpContext.GetUserId();

            if (userId == null)
            {
                return BadRequest();
            }

            bool result = await _threadRepository.MarkThreadAsDeletedAsync(userId, threadId);

            if (result)
            {
                return Ok();
            } 

            return BadRequest();
           
        }

        [HttpGet("{threadId}/messages")]
        public async Task<IActionResult> Get([FromRoute] string threadId)
        {
            _logger.LogInformation("Fetching thread messages from CosmosDb for threadId : {0}", threadId);
            string userId = HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (userId == null)
            {
                return BadRequest();
            }

            List<ThreadMessage> result = await _threadRepository.GetMessagesAsync(userId, threadId);
            return Ok(result);
        }

        [HttpDelete("{threadId}/messages")]
        public async Task<IActionResult> DeleteMessages([FromRoute] string threadId)
        {
            _logger.LogInformation("Deleting messages in CosmosDb for threadId : {0}", threadId);

            string userId = HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (userId == null)
            {
                return BadRequest();
            }

            bool result = await _threadRepository.DeleteMessages(userId, threadId);

            if (result)
            {
                return Ok();
            }

            return BadRequest();

        }


        [HttpPost("{threadId}/messages")]
        [Produces("application/json")]
        [Consumes("application/json")]
        public async Task<IActionResult> Post([FromRoute] string threadId, [FromBody] MessageRequest messageRequest)
        {
            bool suggestFollowupQuestions = true; // need to configure this

            _logger.LogInformation("Adding thread message to CosmosDb for threadId : {0}", threadId);

            string userId = HttpContext.User.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value;

            if (userId == null)
            {
                return BadRequest();
            }

            try
            {

                List<ThreadMessage> messages = await _threadRepository.GetMessagesAsync(userId, threadId);

                ChatHistory history = _promptHelper.BuildConversationHistory(messages, messageRequest.Message);


                IChatCompletionService completionService = _kernel.GetRequiredService<IChatCompletionService>();

                string rewrittenQuery = await _promptHelper.RewriteQueryAsync(history);

                // Text search.
                var filter = new TextSearchFilter().Equality("ThreadId", threadId);
                var searchOptions = new TextSearchOptions()
                {
                    Filter = filter,
                    Top = 3
                };
                KernelSearchResults<object> searchResults = await _search.GetSearchResultsAsync(rewrittenQuery, searchOptions);

                await _promptHelper.AugmentHistoryWithSearchResults(history, searchResults);

                var response = await completionService.GetChatMessageContentsAsync(
                    chatHistory: history,
                    kernel: _kernel
                );

                var assistantResponse = "";
                foreach (var chunk in response)
                {
                    assistantResponse += chunk.Content;
                }

                string[]? followUpQuestionList = null;
                if (suggestFollowupQuestions)
                {
                    var question = messageRequest.Message;
                    followUpQuestionList = await _promptHelper.GenerateFollowUpQuestionsAsync(history, assistantResponse, question);
                }

                var responseMessage = new ResponseMessage("assistant", assistantResponse);
                var responseContext = new ResponseContext(FollowupQuestions: followUpQuestionList ?? Array.Empty<string>());
                var choice = new ResponseChoice(
                    Index: 0,
                    Message: responseMessage,
                    Context: responseContext,
                    CitationBaseUrl: "https://localhost");

                await _threadRepository.PostMessageAsync(userId, threadId, messageRequest.Message, "user");
                await _threadRepository.PostMessageAsync(userId, threadId, assistantResponse, "assistant");

                return Ok(choice);
            }
            catch (HttpOperationException httpOperationException)
            {
                _logger.LogError("An error occurred: {0}", httpOperationException.Message);
                return RateLimitResponse(httpOperationException);
            }
            catch (Exception ex)
            {
                _logger.LogError("An error occurred: {0}", ex.Message);
            }
            return new EmptyResult();
        }

        internal IActionResult RateLimitResponse(HttpOperationException httpOperationException)
        {
            string message = httpOperationException.Message;
            int retryAfterSeconds = Utilities.ExtractRetryAfterSeconds(message);
            Response.Headers["retry-after"] = retryAfterSeconds.ToString();
            return StatusCode(429, "Too many requests. Please try again later.");
        }
    }
}
