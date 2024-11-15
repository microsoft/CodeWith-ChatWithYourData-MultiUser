using System.Text.Json.Serialization;

namespace WebApi.Entities
{
    public record ResponseChoice(
        [property: JsonPropertyName("index")] int Index,
        [property: JsonPropertyName("message")] ResponseMessage Message,
        [property: JsonPropertyName("context")] ResponseContext Context,
        [property: JsonPropertyName("citationBaseUrl")] string CitationBaseUrl)
    {


    }
}
