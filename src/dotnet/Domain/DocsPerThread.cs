using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Domain
{
    public record DocsPerThread
    {
        [JsonProperty(PropertyName = "id")]
        public required string Id { get; set; }

        [JsonProperty(PropertyName = "threadId")]
        public required string ThreadId { get; set; }

        [JsonProperty(PropertyName = "userId")]
        public required string UserId { get; set; }

        [JsonProperty(PropertyName = "documentName")]
        public required string DocumentName { get; set; }

        [JsonProperty(PropertyName = "deleted")]
        public bool Deleted { get; set; } = false;

        [JsonProperty(PropertyName = "availableInSearchIndex")]
        public bool AvailableInSearchIndex { get; set; } = false;

        [JsonProperty(PropertyName = "chunkId")]
        public string? ChunkId { get; set; }
    }
}
