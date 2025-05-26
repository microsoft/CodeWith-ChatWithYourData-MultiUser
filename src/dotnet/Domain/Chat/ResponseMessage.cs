using System.Text.Json.Serialization;

namespace Domain.Chat
{
    public record ResponseMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content)
    {
    }
}
