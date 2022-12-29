using Dapper;
using Orleans.Runtime;
using PublisherService.Core.Database.OutboxPattern.Orleans;
using PublisherService.Core.Database.Service;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Orleans;

internal class OutboxTablePartitionCreationGrain : Grain, IOutboxTablePartitionCreationGrain, IRemindable
{
    private bool _isBusy = false;
    private const string ReminderName = "DbPartitionCreationGrain_Outbox_Postgres";

    private readonly ILogger<OutboxTablePartitionCreationGrain> _logger;
    private readonly IDbContext _dbContext;

    public OutboxTablePartitionCreationGrain(ILogger<OutboxTablePartitionCreationGrain> logger, IDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
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

    private string GetCreatePartitionCommandForOutboxTable(DateTime date)
    {
        //if not exists, create month long partitions on date from first day of current month to first day of next month
        var partitionTableName = $"tbl_outbox_y{date.Year.ToString("0000")}_m{date.Month.ToString("00")}_d{date.Day.ToString("00")}";
        var commandText =
                    "DO $$ " +
                    "BEGIN " +
                    $"IF NOT EXISTS(SELECT 1 FROM pg_tables WHERE schemaname = N'outbox_pattern' AND tablename = N'{partitionTableName}') THEN " +
                        $"CREATE TABLE outbox_pattern.{partitionTableName} PARTITION OF outbox_pattern.tbl_outbox " +
                        $"FOR VALUES FROM ('{date.ToString("yyyy'-'MM'-'dd")}') TO ('{date.AddDays(1).ToString("yyyy'-'MM'-'dd")}') " +
                        "TABLESPACE pg_default; " +
                    "END IF; " +
                    "END $$ ";

        return commandText;
    }

    private async Task<int> CreatePartitionForOutboxTable(DateTime date)
    {
        using (var conn = _dbContext.GetConnection())
        {
            var sql = GetCreatePartitionCommandForOutboxTable(date);
            return await conn.ExecuteAsync(new CommandDefinition(sql));
        }
    }

    private async Task CreateOutboxPartitions()
    {
        try
        {
            var todayUtc = DateTime.UtcNow.Date;
            //create partition for this month and next month (to be safe if app server time and db server time are skewed)
            await Task.WhenAll(CreatePartitionForOutboxTable(todayUtc), CreatePartitionForOutboxTable(todayUtc.AddDays(1)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating partitions for outbox table.");
        }
    }
}

