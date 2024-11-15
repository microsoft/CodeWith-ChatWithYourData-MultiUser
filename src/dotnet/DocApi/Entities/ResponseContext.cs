using System.Text.Json.Serialization;

namespace WebApi.Entities
{
    public record ResponseContext(
       [property: JsonPropertyName("followup_questions")] string[] FollowupQuestions)
    {
    }
}
