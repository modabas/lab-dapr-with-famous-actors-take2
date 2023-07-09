using System.Security.Cryptography;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Utility;

public static class OutboxPatternHelper
{
    public const short MinOutboxCount = 1;
    public const short MaxOutboxCount = 5;

    public static string GetSchemaName(OutboxPatternOptions options)
    {
        var schemaName = options.SchemaName?.TrimEnd('.');
        if (string.IsNullOrWhiteSpace(schemaName))
        {
            throw new ArgumentException($"Schema name: {schemaName} defined in options is invalid.");
        }
        return schemaName;
    }

    public static short GetOutboxCount(OutboxPatternOptions options)
    {
        return Math.Max(MinOutboxCount, Math.Min(MaxOutboxCount, options.OutboxCount));
    }

    public static short DetermineOutboxNo(Guid correlationId, OutboxPatternOptions options)
    {
        var outboxCount = GetOutboxCount(options);
        if (outboxCount == 1) { return 0; }

        return ConvertGuidToShort(correlationId, outboxCount);
    }

    public static short RandomOutboxNo(OutboxPatternOptions options)
    {
        var outboxCount = GetOutboxCount(options);
        if (outboxCount == 1) { return 0; }

        return (short)RandomNumberGenerator.GetInt32(outboxCount);
    }

    private static short ConvertGuidToShort(Guid correlationId, short maxValue)
    {
        byte[] bytes = correlationId.ToByteArray();
        int sum = 0;
        foreach (byte b in bytes)
        {
            sum += b;
        }
        return (short)(sum % maxValue);
    }
}
