using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Chat
{
    /// <summary>
    /// Enhanced response class that includes both the content and usage metrics
    /// </summary>
    public class CompletionResponse
    {
        /// <summary>
        /// The content of the response
        /// </summary>
        public string Content { get; set; }

        /// <summary>
        /// Token usage information
        /// </summary>
        public UsageMetrics Usage { get; set; }

        /// <summary>
        /// Any metadata available in the response
        /// </summary>
        public Dictionary<string, object> Metadata { get; set; }

        /// <summary>
        /// Indicates if the response was successful
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Error details if the response was not successful
        /// </summary>
        public ErrorDetails Error { get; set; }

        public CompletionResponse()
        {
            Metadata = new Dictionary<string, object>();
            IsSuccess = true;
        }
    }

    /// <summary>
    /// Token usage metrics
    /// </summary>
    public class UsageMetrics
    {
        /// <summary>
        /// Number of input tokens used
        /// </summary>
        public int InputTokens { get; set; }

        /// <summary>
        /// Number of output tokens used
        /// </summary>
        public int OutputTokens { get; set; }

        /// <summary>
        /// Total tokens used
        /// </summary>
        public int TotalTokens => InputTokens + OutputTokens;
    }

    /// <summary>
    /// Error details for failed requests
    /// </summary>
    public class ErrorDetails
    {
        /// <summary>
        /// HTTP status code
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Error message
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Whether this is a rate limit error
        /// </summary>
        public bool IsRateLimit => StatusCode == 429;

        /// <summary>
        /// Suggested retry time in seconds, if available
        /// </summary>
        public int? RetryAfterSeconds { get; set; }
    }

    public class FollowUpResponse
    {
        public required string[] FollowUpQuestions { get; set; }
    }
}
