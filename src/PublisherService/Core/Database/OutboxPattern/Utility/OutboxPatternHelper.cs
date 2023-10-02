namespace PublisherService.Core.Database.OutboxPattern.Utility;

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
}
