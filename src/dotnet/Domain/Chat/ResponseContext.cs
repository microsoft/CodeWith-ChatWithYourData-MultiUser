using System.Text.Json.Serialization;

namespace Domain.Chat
{
    public record ResponseContext(
        [property: JsonPropertyName("dataPointsContent")] SupportingContentRecord[]? DataPointsContent,
        [property: JsonPropertyName("followup_questions")] string[] FollowupQuestions,
        [property: JsonPropertyName("thoughts")] Thoughts[]? Thoughts,
        [property: JsonPropertyName("usageMetrics")] UsageMetrics? UsageMetrics);

    public record DataPoints([property: JsonPropertyName("text")] string[] Text)
    { }

    public record Thoughts(
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("props")] (string, string)[]? Props = null)
    { }

    public record SupportingContentRecord(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("description")] string Description,
        [property: JsonPropertyName("props")] (string, string)[]? Props = null)
    { }
}
