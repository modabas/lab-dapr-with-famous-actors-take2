using Orleans.Runtime;
using PublisherService.Core.Database.OutboxPattern.Orleans;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Orleans;

public class OutboxListenerGrainService : GrainService, IOutboxListenerGrainService
{
    private readonly IOutboxProcessor _outboxProcessor;

    public OutboxListenerGrainService(IOutboxProcessor outboxProcessor, GrainId id, Silo silo, ILoggerFactory loggerFactory, IGrainFactory grainFactory) : base(id, silo, loggerFactory)
    {
        _outboxProcessor = outboxProcessor;
    }

    public override Task Init(IServiceProvider serviceProvider)
    {
        return base.Init(serviceProvider);
    }

    public override async Task Start()
    {
        await base.Start();

        //await Init();

        await _outboxProcessor.Start();
    }

    public override async Task Stop()
    {
        await _outboxProcessor.Stop();

        await base.Stop();
    }
}
