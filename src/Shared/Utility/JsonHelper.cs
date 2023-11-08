using System.Text.Json;

namespace Shared.Utility;

public class JsonHelper
{
    public static byte[] SerializeToJsonUtf8Bytes<T>(T? input)
    {
        if (input is null)
            return Array.Empty<byte>();
        return JsonSerializer.SerializeToUtf8Bytes(input);
    }

    public static async Task<object?> DeserializeJsonAsync(Stream stream, Type messageType, CancellationToken cancellationToken)
    {
        return await JsonSerializer.DeserializeAsync(stream, messageType, cancellationToken: cancellationToken);
    }
}
