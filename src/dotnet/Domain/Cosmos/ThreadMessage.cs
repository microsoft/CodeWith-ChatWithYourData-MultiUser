using Domain.Chat;
using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Domain.Cosmos
{
    public record ThreadMessage
    {
        [JsonProperty(PropertyName = "id")]
        public required string Id { get; set; }

        [JsonProperty(PropertyName = "userId")]
        public required string UserId { get; set; }

        [JsonProperty(PropertyName = "threadId")]
        public required string ThreadId { get; set; }

        [JsonProperty(PropertyName = "type")]
        public required string Type { get; set; }

        [JsonProperty(PropertyName = "role")]
        public required string Role { get; set; }

        [JsonProperty(PropertyName = "content")]
        public required string Content { get; set; }

        [JsonProperty(PropertyName = "created")]
        public required DateTime Created { get; set; }

        [JsonProperty(PropertyName = "context")]
        public ResponseContext? Context { get; set; }

    }
}
