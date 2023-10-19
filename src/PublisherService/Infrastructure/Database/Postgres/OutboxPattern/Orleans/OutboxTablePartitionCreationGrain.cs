using Orleans.Runtime;
using PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Service;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Orleans;

public class OutboxTablePartitionCreationGrain : Grain, IOutboxTablePartitionCreationGrain, IRemindable
{
    private bool _isBusy = false;
    private const string ReminderName = "DbPartitionCreationGrain_Outbox_Postgres";

    private readonly ILogger<OutboxTablePartitionCreationGrain> _logger;
    private readonly IOutboxTablePartitionCreationService _partitionCreationService;
    private readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

    public OutboxTablePartitionCreationGrain(ILogger<OutboxTablePartitionCreationGrain> logger, IOutboxTablePartitionCreationService partitionCreationService)
    {
        _logger = logger;
        _partitionCreationService = partitionCreationService;
    }

    public async Task<IGrainReminder> RegisterOrUpdateReminder()
    {
        return await this.RegisterOrUpdateReminder(ReminderName, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
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

            using (var cts = new CancellationTokenSource())
            {
                cts.CancelAfter(_timeout);
                await _partitionCreationService.CreatePartitions(cts.Token);
            }
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

}

