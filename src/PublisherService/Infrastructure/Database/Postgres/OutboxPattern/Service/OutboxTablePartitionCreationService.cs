using Microsoft.Extensions.Options;
using PublisherService.Core.Database.OutboxPattern.Service;
using PublisherService.Core.Database.OutboxPattern.Utility;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Service;

public class OutboxTablePartitionCreationService : IOutboxTablePartitionCreationService
{
    private readonly ILogger<OutboxTablePartitionCreationService> _logger;
    private readonly IOutboxPatternDbContext _dbContext;
    private readonly IOptions<OutboxPatternOptions> _options;

    public OutboxTablePartitionCreationService(ILogger<OutboxTablePartitionCreationService> logger, IOutboxPatternDbContext dbContext, IOptions<OutboxPatternOptions> options)
    {
        _logger = logger;
        _dbContext = dbContext;
        _options = options;
    }

    private string GetCreatePartitionCommandForOutboxTable(int outboxNo, DateOnly date)
    {
        var schemaName = OutboxPatternHelper.GetSchemaName(_options.Value);
        //if not exists, create daily partitions 
        var partitionTableName = $"tbl_outbox_o{outboxNo:0}_y{date.Year:0000}_m{date.Month:00}_d{date.Day:00}";
        var commandText =
                    "DO $$ " +
                    "BEGIN " +
                    $"IF NOT EXISTS(SELECT 1 FROM pg_tables WHERE schemaname = N'{schemaName}' AND tablename = N'{partitionTableName}') THEN " +
                        $"CREATE TABLE {schemaName}.{partitionTableName} PARTITION OF {schemaName}.tbl_outbox_o{outboxNo:0} " +
                        $"FOR VALUES FROM ('{date.ToString("yyyy'-'MM'-'dd")}') TO ('{date.AddDays(1).ToString("yyyy'-'MM'-'dd")}') " +
                        "TABLESPACE pg_default; " +
                    "END IF; " +
                    "END $$ ";

        return commandText;
    }

    private async Task<int> CreatePartitionForOutboxTable(int outboxNo, DateOnly date, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sql = GetCreatePartitionCommandForOutboxTable(outboxNo, date);
            using (var conn = _dbContext.GetConnection())
            {
                await conn.OpenAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                var command = conn.CreateCommand();
                command.CommandText = sql;
                return await command.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating partition for outbox table. OutboxNo: {outboxNo}, Date: {partitionDate}", outboxNo, date);
            return -1;
        }
    }

    public async Task CreatePartitions(CancellationToken cancellationToken)
    {
        try
        {
            var todayUtc = DateTime.UtcNow.Date;
            //create partition for this month and next month (to be safe if app server time and db server time are skewed)
            //for each outbox
            for (var outboxNo = 0; outboxNo < OutboxPatternHelper.MaxOutboxCount; outboxNo++)
            {
                await Task.WhenAll(CreatePartitionForOutboxTable(outboxNo, DateOnly.FromDateTime(todayUtc), cancellationToken),
                    CreatePartitionForOutboxTable(outboxNo, DateOnly.FromDateTime(todayUtc.AddDays(1)), cancellationToken));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating partitions for outbox table.");
        }
    }
}
