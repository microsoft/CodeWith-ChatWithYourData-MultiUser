using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Domain.Cosmos
{
    public record Thread
    {
        [JsonProperty(PropertyName = "id")]
        public required string Id { get; set; }

        [JsonProperty(PropertyName = "type")]
        public required string Type { get; set; }

        [JsonProperty(PropertyName = "userId")]
        public required string UserId { get; set; }

        [JsonProperty(PropertyName = "threadName")]
        public string? ThreadName { get; set; }

        [JsonProperty(PropertyName = "deleted")]
        public bool Deleted { get; set; } = false;

        [JsonProperty(PropertyName = "lastupdated")]
        public DateTimeOffset? LastUpdated { get; set; } 
    }
}
