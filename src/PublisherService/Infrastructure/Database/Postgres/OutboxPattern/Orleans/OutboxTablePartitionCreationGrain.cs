using Dapper;
using Microsoft.Extensions.Options;
using Orleans.Runtime;
using PublisherService.Core.Database.OutboxPattern.Utility;
using PublisherService.Core.Database.Service;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Orleans;

public class OutboxTablePartitionCreationGrain : Grain, IOutboxTablePartitionCreationGrain, IRemindable
{
    private bool _isBusy = false;
    private const string ReminderName = "DbPartitionCreationGrain_Outbox_Postgres";

    private readonly ILogger<OutboxTablePartitionCreationGrain> _logger;
    private readonly IDbContext _dbContext;
    private readonly IOptions<OutboxPatternOptions> _outboxOptions;

    public OutboxTablePartitionCreationGrain(ILogger<OutboxTablePartitionCreationGrain> logger, 
        IDbContext dbContext, 
        IOptions<OutboxPatternOptions> outboxOptions)
    {
        _logger = logger;
        _dbContext = dbContext;
        _outboxOptions = outboxOptions;
    }

    public Task<bool> IsBusy()
    {
        return Task.FromResult(_isBusy);
    }

    public Task Poke()
    {
        return Task.CompletedTask;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        try
        {
            //on activation, register self as reminder
            //activation is done by a seperate startup task
            await this.RegisterOrUpdateReminder(ReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
            await base.OnActivateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnActivateAsync failed");
            throw;
        }
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        try
        {
            if (reminderName != ReminderName)
                return;
            if (_isBusy)
                return;
            _isBusy = true;

            await CreateOutboxPartitions();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {reminderName} reminder.", reminderName);
        }
        finally
        {
            _isBusy = false;
        }
    }

    private string GetCreatePartitionCommandForOutboxTable(int outboxNo, DateTime date)
    {
        var schemaName = OutboxPatternHelper.GetSchemaName(_outboxOptions.Value);
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

    private async Task<int> CreatePartitionForOutboxTable(int outboxNo, DateTime date)
    {
        using (var conn = _dbContext.GetConnection())
        {
            var sql = GetCreatePartitionCommandForOutboxTable(outboxNo, date);
            return await conn.ExecuteAsync(new CommandDefinition(sql));
        }
    }

    private async Task CreateOutboxPartitions()
    {
        try
        {
            var todayUtc = DateTime.UtcNow.Date;
            //create partition for this month and next month (to be safe if app server time and db server time are skewed)
            //for each outbox
            for (var outboxNo = 0; outboxNo < OutboxPatternHelper.MaxOutboxCount; outboxNo++)
            {
                await Task.WhenAll(CreatePartitionForOutboxTable(outboxNo, todayUtc), CreatePartitionForOutboxTable(outboxNo, todayUtc.AddDays(1)));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating partitions for outbox table.");
        }
    }
}

