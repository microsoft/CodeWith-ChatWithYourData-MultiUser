using System.Text.Json.Serialization;

namespace WebApi.Entities
{
    public record ResponseMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content)
    {
    }
}
