using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Data;
using System.Text.Json.Serialization;

public record IndexDoc
{
    [JsonPropertyName("chunk_id")]
    [VectorStoreRecordKey]
    [TextSearchResultName]
    public required string ChunkId { get; init; }

    [JsonPropertyName("content")]
    [VectorStoreRecordData]
    [TextSearchResultValue]

    public required string Content { get; init; }

    [JsonPropertyName("file_name")]
    [VectorStoreRecordData]
    [TextSearchResultLink]

    public required string FileName { get; init; }

    [JsonPropertyName("document_id")]
    [VectorStoreRecordData]
    [TextSearchResultLink]

    public required string DocumentId { get; init; }

    [JsonPropertyName("thread_id")]
    [VectorStoreRecordData(IsFilterable = true)]
    public required string ThreadId { get; init; }

    [JsonPropertyName("content_vector")]
    [VectorStoreRecordVector(1536)]
    public ReadOnlyMemory<float> ContentVector { get; init; }
}