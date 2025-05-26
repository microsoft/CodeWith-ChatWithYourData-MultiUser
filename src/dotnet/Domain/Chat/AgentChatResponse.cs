using System;
using System.Text.Json.Serialization;

namespace Domain.Chat
{
    /// <summary>
    /// Represents a response from an agent in a streaming chat scenario
    /// </summary>
    public class AgentChatResponse
    {
        /// <summary>
        /// Type of the message
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
        
        /// <summary>
        /// Name of the agent that produced this message
        /// </summary>
        [JsonPropertyName("role")]
        public string Role { get; set; }
        
        /// <summary>
        /// Content of the message
        /// </summary>
        [JsonPropertyName("content")]
        public string Content { get; set; }
        
        /// <summary>
        /// Timestamp of when the message was generated
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        /// <summary>
        /// Indicates if this is the final message in the conversation
        /// </summary>
        [JsonPropertyName("isFinal")]
        public bool IsFinal { get; set; }

        /// <summary>
        /// Creates a content message from an agent
        /// </summary>
        public static AgentChatResponse CreateAgentMessage(string agentName, string content, bool isFinal = false)
        {
            return new AgentChatResponse
            {
                Type = "message",
                Role = agentName,
                Content = content,
                Timestamp = DateTime.UtcNow,
                IsFinal = isFinal
            };
        }

        /// <summary>
        /// Creates a heartbeat message to indicate processing is still ongoing
        /// </summary>
        public static AgentChatResponse CreateHeartbeat()
        {
            return new AgentChatResponse
            {
                Type = "heartbeat",
                Role = "system",
                Content = "processing",
                Timestamp = DateTime.UtcNow,
                IsFinal = false
            };
        }
    }
}
