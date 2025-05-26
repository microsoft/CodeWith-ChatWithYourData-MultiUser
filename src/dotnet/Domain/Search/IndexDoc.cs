using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace Domain.Search
{
    public record IndexDoc
    {
        [JsonPropertyName("parent_id")]
        public string? ParentId { get; init; }
        
        [JsonPropertyName("chunk_id")]
        public required string ChunkId { get; init; }

        [JsonPropertyName("content")]

        public required string Content { get; init; }

        [JsonPropertyName("extract")]

        public string? Extract { get; set; }

        [JsonPropertyName("file_name")]

        public required string FileName { get; init; }

        [JsonPropertyName("document_id")]

        public required string DocumentId { get; init; }

        [JsonPropertyName("thread_id")]
        public required string ThreadId { get; init; }

        [JsonPropertyName("content_vector")]
        public ReadOnlyMemory<float> ContentVector { get; init; }
    }
}
