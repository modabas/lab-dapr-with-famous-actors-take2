using System.Text.Json.Serialization;

namespace Shared.GreetService.Events;

public record GreetingReceived([property: JsonPropertyName("from")] string From,
    [property: JsonPropertyName("to")] string To,
    [property: JsonPropertyName("message")] string Message);
