using Domain.Chat;
using Domain.Cosmos;
using Domain.Search;
using Infrastructure.Helpers;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Data;
using System.Net;
using System.Text.RegularExpressions;
using Thread = Domain.Cosmos.Thread;

namespace WebApi.Services
{
    public class ThreadService
    {
        private readonly ILogger<ThreadService> _logger;
        private readonly IThreadRepository _threadRepository;
        private readonly IAIService _aiService;
        private readonly ISearchService _search;
        private readonly ThreadSafeSettings _settings;
        private readonly IMemoryCache _memoryCache;
        private readonly IDocumentRegistry _documentRegistry;

        public ThreadService(
            ILogger<ThreadService> logger,
            IThreadRepository threadRepository,
            IAIService aiService,
            ISearchService search,
            ThreadSafeSettings settings,
            IMemoryCache memoryCache,
            IDocumentRegistry documentRegistry)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _threadRepository = threadRepository ?? throw new ArgumentNullException(nameof(threadRepository));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _search = search ?? throw new ArgumentNullException(nameof(search));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _documentRegistry = documentRegistry ?? throw new ArgumentNullException(nameof(documentRegistry));

            _logger.LogInformation("ThreadService initialized successfully");
        }

        public async Task<List<Thread>> GetThreadsAsync(string userId)
        {
            _logger.LogInformation("Fetching threads from CosmosDb for userId: {UserId}", userId);
            List<Thread> threads = await _threadRepository.GetThreadsAsync(userId);
            threads = threads.OrderByDescending(t => t.LastUpdated).ToList();
            _logger.LogInformation("Successfully fetched {Count} threads for userId: {UserId}", threads.Count, userId);
            return threads;
        }

        public async Task<Thread> CreateThreadAsync(string userId)
        {
            _logger.LogInformation("Creating thread in CosmosDb for userId: {UserId}", userId);
            Thread thread = await _threadRepository.CreateThreadAsync(userId);
            _logger.LogInformation("Successfully created thread {ThreadId} for userId: {UserId}", thread.Id, userId);
            return thread;
        }

        public async Task<bool> DeleteThreadAsync(string userId, string threadId)
        {
            _logger.LogInformation("Deleting thread {ThreadId} for userId: {UserId}", threadId, userId);
            bool result = await _threadRepository.MarkThreadAsDeletedAsync(userId, threadId);
            
            if (result)
                _logger.LogInformation("Successfully deleted thread {ThreadId} for userId: {UserId}", threadId, userId);
            else
                _logger.LogWarning("Failed to delete thread {ThreadId} for userId: {UserId}", threadId, userId);
                
            return result;
        }

        public async Task<Thread> UpdateThreadTitleAsync(string userId, string threadId, string title)
        {
            _logger.LogInformation("Updating thread {ThreadId} for userId: {UserId}", threadId, userId);
            Dictionary<string, object> fieldsToUpdate = new Dictionary<string, object>
            {
                { "threadName", title }
            };

            bool success = await _threadRepository.UpdateThreadFieldsAsync(threadId, userId, fieldsToUpdate);

            if (success)
            {
                Thread updatedThread = await _threadRepository.GetThreadAsync(userId, threadId);
                _logger.LogInformation("Successfully updated thread {ThreadId} for userId: {UserId}", threadId, userId);
                return updatedThread;
            }

            _logger.LogWarning("Failed to update thread {ThreadId} for userId: {UserId}", threadId, userId);
            return null;
        }

        public async Task<List<ThreadMessage>> GetMessagesAsync(string userId, string threadId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Fetching thread messages from CosmosDb for threadId: {ThreadId}", threadId);
            List<ThreadMessage> result = await _threadRepository.GetMessagesAsync(userId, threadId, cancellationToken);
            _logger.LogInformation("Successfully fetched {Count} messages for threadId: {ThreadId}", result.Count, threadId);

            // Add welcome message if thread is new and setting is enabled
            if (result.Count <= 1 && _settings.GetSettings().AllowInitialPromptToHelpUser)
            {
                result.Add(CreateWelcomeMessage(userId, threadId));
                _logger.LogInformation("Added initial welcome message for new thread {ThreadId}", threadId);
            }
            
            return result;
        }

        public async Task<bool> DeleteMessagesAsync(string userId, string threadId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Deleting messages in CosmosDb for threadId: {ThreadId}", threadId);
            bool result = await _threadRepository.DeleteMessages(userId, threadId, cancellationToken);
            
            if (result)
                _logger.LogInformation("Successfully deleted all messages for threadId: {ThreadId}", threadId);
            else
                _logger.LogWarning("Failed to delete messages for threadId: {ThreadId}", threadId);
                
            return result;
        }

        public async Task<ThreadMessage> ProcessUserMessageAsync(
            string userId, string threadId, string message, CancellationToken cancellationToken)
        {
            // Save user message
            var question = await CreateAndSaveUserMessage(userId, threadId, message, cancellationToken);
            
            // Build conversation history and process query
            var history = await BuildConversationHistoryAsync(userId, threadId, message, cancellationToken);
            string query = (_settings.GetSettings().AllowInitialPromptRewrite)
                ? await RewriteQuestionAsync(history, cancellationToken)
                : message;

            // Perform search and get response
            var documents = await _documentRegistry.GetDocsPerThreadAsync(threadId);
            var searchResults = await PerformSearchAsync(history, query, threadId, cancellationToken);
            var answer = await GenerateAndSaveAssistantResponseAsync(userId, threadId, history, searchResults, cancellationToken);
            
            return answer;
        }

        public async Task<ThreadMessage> ProcessUserMessageWithDocumentsAsync(
            string userId, string threadId, string message, List<string> documentIds, CancellationToken cancellationToken)
        {
            // Save user message
            var question = await CreateAndSaveUserMessage(userId, threadId, message, cancellationToken);
            
            // Build conversation history and process query
            var history = await BuildConversationHistoryAsync(userId, threadId, message, cancellationToken);
            string query = (_settings.GetSettings().AllowInitialPromptRewrite)
                ? await RewriteQuestionAsync(history, cancellationToken)
                : message;

            // Perform search with specific documents and get response
            var searchResults = await PerformSearchAsync(history, query, threadId, documentIds, cancellationToken);
            var answer = await GenerateAndSaveAssistantResponseAsync(userId, threadId, history, searchResults, cancellationToken);
            
            return answer;
        }

        public async Task<IAsyncEnumerable<StreamingChatMessageContent>> GetStreamingResponseAsync(
            string userId, string threadId, MessageRequest messageRequest, CancellationToken cancellationToken)
        {
            // Save user message first
            await SaveUserMessageAsync(userId, threadId, messageRequest.Message, cancellationToken);
            
            // Get conversation history
            var history = await BuildConversationHistoryAsync(userId, threadId, messageRequest.Message, cancellationToken);
            
            // Process query
            string query = (_settings.GetSettings().AllowInitialPromptRewrite)
                ? await RewriteQuestionAsync(history, cancellationToken)
                : messageRequest.Message;

            // Check if a tool is being used
            string toolName = string.Empty;
            if (messageRequest.Tools != null && messageRequest.Tools.Count > 0)
            {
                toolName = messageRequest.Tools.FirstOrDefault();
            }

            // Return appropriate stream based on tool or document selection
            if (!string.IsNullOrEmpty(toolName))
            {
                return await GetToolBasedStreamingResponseAsync(history, threadId, toolName, cancellationToken);
            }
            else
            {
                // If specific documents are selected
                if (messageRequest.DocumentIds?.Count > 0)
                {
                    await PerformSearchAsync(history, query, threadId, messageRequest.DocumentIds, cancellationToken);
                }
                else
                {
                    await PerformSearchAsync(history, query, threadId, cancellationToken);
                }
                
                return _aiService.GetChatCompletionStreaming(history);
            }
        }

        public async Task<ThreadMessage> SaveStreamingResponseAsync(
            string userId, string threadId, string content, 
            string[] followupQuestions, UsageMetrics usageMetrics)
        {
            var message = new ThreadMessage
            {
                Id = Guid.NewGuid().ToString(),
                ThreadId = threadId,
                UserId = userId,
                Role = "assistant",
                Content = content,
                Type = "CHAT_MESSAGE",
                Created = DateTime.UtcNow,
                Context = new ResponseContext(
                    DataPointsContent: null,
                    FollowupQuestions: followupQuestions,
                    Thoughts: Array.Empty<Thoughts>(),
                    UsageMetrics: usageMetrics)
            };
            
            await _threadRepository.PostMessageAsync(userId, message, CancellationToken.None);
            _logger.LogInformation("Successfully saved streaming response for threadId: {ThreadId}", threadId);
            
            return message;
        }

        public async Task<string[]> GenerateFollowupQuestionsAsync(
            ChatHistory history, string finalContent, CancellationToken cancellationToken)
        {
            if (_settings.GetSettings().AllowFollowUpPrompts)
            {
                return await _aiService.GenerateFollowUpQuestionsAsync(history, finalContent, finalContent);
            }
            return Array.Empty<string>();
        }

        #region Private Helper Methods

        private ThreadMessage CreateWelcomeMessage(string userId, string threadId)
        {
            string[] followUpQuestionList = new string[] { "Help me generate questions about the document", "Summarize the document" };
            var responseMessage = new ResponseMessage("assistant", "Do you need help?");
            var responseContext = new ResponseContext(
                   FollowupQuestions: followUpQuestionList ?? Array.Empty<string>(),
                   DataPointsContent: null,
                   UsageMetrics: null,
                   Thoughts: Array.Empty<Thoughts>());
                   
            return new ThreadMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = "CHAT_MESSAGE",
                ThreadId = threadId,
                UserId = userId,
                Role = responseMessage.Role,
                Content = responseMessage.Content,
                Context = responseContext,
                Created = DateTime.UtcNow
            };
        }

        private async Task SaveUserMessageAsync(string userId, string threadId, string message, CancellationToken cancellationToken)
        {
            await _threadRepository.PostMessageAsync(
                userId,
                new ThreadMessage
                {
                    Id = Guid.NewGuid().ToString(),
                    ThreadId = threadId,
                    UserId = userId,
                    Role = "user",
                    Content = message,
                    Type = "CHAT_MESSAGE",
                    Created = DateTime.UtcNow
                },
                cancellationToken);
        }

        private async Task<ThreadMessage> CreateAndSaveUserMessage(string userId, string threadId, string message, CancellationToken cancellationToken)
        {
            var question = new ThreadMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = "CHAT_MESSAGE",
                ThreadId = threadId,
                UserId = userId,
                Role = "user",
                Content = message,
                Context = null,
                Created = DateTime.UtcNow
            };
            
            await _threadRepository.PostMessageAsync(userId, question, cancellationToken);
            return question;
        }

        internal async Task<ChatHistory> BuildConversationHistoryAsync(string userId, string threadId, string message, CancellationToken cancellationToken)
        {
            string cacheKey = $"history:{userId}:{threadId}:{message}";
            var messages = await _threadRepository.GetMessagesAsync(userId, threadId, cancellationToken);
            var history = _aiService.BuildConversationHistory(messages, message);
            return history;
        }

        internal async Task<string> RewriteQuestionAsync(ChatHistory history, CancellationToken cancellationToken)
        {
            string query = await _aiService.RewriteQueryAsync(history);
            _logger.LogInformation("Query rewritten to: {Query}", query);
            return query;
        }

        internal async Task<List<IndexDoc>> PerformSearchAsync(ChatHistory history, string query, string threadId, CancellationToken cancellationToken)
        {
            string sanitarizedQuery = SanitizeQuery(query);
            var searchResults = await _search.GetSearchResultsAsync(sanitarizedQuery, threadId);
            _aiService.AugmentHistoryWithSearchResults(history, searchResults);
            return searchResults;
        }

        internal async Task<List<IndexDoc>> PerformSearchAsync(ChatHistory history, string query, string threadId, List<string> documentIds, CancellationToken cancellationToken)
        {
            string sanitarizedQuery = SanitizeQuery(query);
            var searchResults = await _search.GetSearchResultsAsync(sanitarizedQuery, threadId, documentIds);
            _aiService.AugmentHistoryWithSearchResults(history, searchResults);
            return searchResults;
        }

        internal string SanitizeQuery(string query)
        {
            return Regex.Replace(
                Regex.Replace(query, @"[^\w\s]", string.Empty)
                    .Replace("\r", " ")
                    .Replace("\n", " ")
                    .Replace("\t", " "),
                @"\s+", " ").Trim();
        }

        private async Task<IAsyncEnumerable<StreamingChatMessageContent>> GetToolBasedStreamingResponseAsync(
            ChatHistory history, string threadId, string toolName, CancellationToken cancellationToken)
        {
            switch (toolName)
            {
                case "incose":
                    {
                        var documents = await _search.GetSearchResultsAsync("*", threadId);
                        return _aiService.GetCompliancyResponseStreamingViaCompletionAsync(
                            threadId, documents, toolName, cancellationToken);
                    }
                case "search":
                    {
                        var documents = await _search.GetSearchResultsAsync("*", threadId);
                        return _aiService.GetCompliancyResponseStreamingViaCompletionAsync(
                            threadId, documents, toolName, cancellationToken);
                    }
                default:
                    // Fall back to standard chat completion if tool not recognized
                    return _aiService.GetChatCompletionStreaming(history);
            }
        }

        private async Task<ThreadMessage> GenerateAndSaveAssistantResponseAsync(
            string userId, string threadId, ChatHistory history, 
            List<IndexDoc> searchResults, CancellationToken cancellationToken)
        {
            var assistantAnswer = await _aiService.GetChatCompletion(history, Enums.CompletionType.Chat);
            if (!assistantAnswer.IsSuccess)
            {
                if (assistantAnswer.Error.IsRateLimit)
                {
                    _logger.LogWarning("Rate limit exceeded for user {UserId} in thread {ThreadId}", userId, threadId);
                    throw new HttpOperationException(HttpStatusCode.TooManyRequests, assistantAnswer.Error.Message, assistantAnswer.Error.Message, null);
                }
                else
                {
                    _logger.LogError("Error generating response for user {UserId} in thread {ThreadId}: {Error}", userId, threadId, assistantAnswer.Error.Message);
                    throw new ServiceException(assistantAnswer.Error.Message, ServiceType.AIService);
                }
            }

            var followUpQuestionList = _settings.GetSettings().AllowFollowUpPrompts
                ? await _aiService.GenerateFollowUpQuestionsAsync(history, assistantAnswer.Content, assistantAnswer.Content)
                : Array.Empty<string>();

            var answer = new ThreadMessage
            {
                Id = Guid.NewGuid().ToString(),
                Type = "CHAT_MESSAGE",
                ThreadId = threadId,
                UserId = userId,
                Role = "assistant",
                Content = assistantAnswer.Content,
                Context = new ResponseContext(
                    FollowupQuestions: followUpQuestionList,
                    DataPointsContent: null,
                    Thoughts: null!,
                    UsageMetrics: assistantAnswer.Usage),
                Created = DateTime.UtcNow
            };
            
            await _threadRepository.PostMessageAsync(userId, answer, cancellationToken);
            return answer;
        }

        #endregion
    }
}