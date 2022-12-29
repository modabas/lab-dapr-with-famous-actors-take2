using System.Text.Json;

namespace Shared.Utility;

public class JsonHelper
{
    public static string SerializeJson<T>(T? input)
    {
        if (input is null)
            return string.Empty;
        return JsonSerializer.Serialize(input);
    }

    public static async Task<object?> DeserializeJsonAsync(Stream stream, Type messageType, CancellationToken cancellationToken)
    {
        return await JsonSerializer.DeserializeAsync(stream, messageType, cancellationToken: cancellationToken);
    }
}
