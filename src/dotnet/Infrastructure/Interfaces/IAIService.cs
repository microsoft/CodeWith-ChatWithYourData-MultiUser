using Domain.Chat;
using Domain.Cosmos;
using Domain.Search;
using Infrastructure.Implementations.SemanticKernel;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Domain.Chat.Enums;

namespace Infrastructure.Interfaces
{    public interface IAIService
    {
        ChatHistory BuildConversationHistory(List<ThreadMessage> messages, string newMessage);
        Task<string[]> GenerateFollowUpQuestionsAsync(ChatHistory history, string assistantResponse, string question);
        Task<string> RewriteQueryAsync(ChatHistory history);
        ChatHistory AugmentHistoryWithSearchResults(ChatHistory history, List<IndexDoc> searchResults);
        Task<CompletionResponse> GetChatCompletion(ChatHistory history, CompletionType completionType);
        Task<string> ExtractDocument(List<IndexDoc> searchResults);
        IAsyncEnumerable<StreamingChatMessageContent> GetChatCompletionStreaming(ChatHistory history);
        IAsyncEnumerable<StreamingChatMessageContent> GetCompliancyResponseStreamingViaCompletionAsync(string threadId, List<IndexDoc> documents, string guideLine, CancellationToken cancellationToken = default);
        Task IsHealthyAsync();
    }
}
