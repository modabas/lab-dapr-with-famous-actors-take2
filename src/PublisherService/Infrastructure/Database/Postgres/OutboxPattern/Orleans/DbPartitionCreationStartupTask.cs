using Orleans.Runtime;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Orleans;

public class DbPartitionCreationStartupTask : IStartupTask
{
    private readonly IGrainFactory _grainFactory;

    public DbPartitionCreationStartupTask(IGrainFactory grainFactory)
    {
        this._grainFactory = grainFactory;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        var grain = this._grainFactory.GetGrain<IOutboxTablePartitionCreationGrain>(0);
        await grain.RegisterOrUpdateReminder();
    }
}
