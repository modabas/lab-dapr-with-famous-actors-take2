using Npgsql.Replication.PgOutput;
using Npgsql.Replication;
using Npgsql.Replication.PgOutput.Messages;
using Microsoft.Extensions.Options;
using PublisherService.Core.Database.Config;
using Dapr.Client;
using Npgsql;
using System.Diagnostics;
using PublisherService.Core.Database.OutboxPattern.Service;
using PublisherService.Core.Database.OutboxPattern.Utility;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Service;

public class OutboxProcessor : IOutboxProcessor
{
    private CancellationTokenSource? _tokenSource;
    private Task? _executingTask;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly IOptionsMonitor<ServiceDbOptions> _dbOptions;
    private readonly IOptions<OutboxPatternOptions> _outboxOptions;
    private readonly DaprClient _daprClient;


    public OutboxProcessor(IOptionsMonitor<ServiceDbOptions> dbOptions,
        ILogger<OutboxProcessor> logger,
        DaprClient daprClient,
        IOptions<OutboxPatternOptions> outboxOptions)
    {
        _dbOptions = dbOptions;
        _logger = logger;
        _daprClient = daprClient;
        _outboxOptions = outboxOptions;
    }

    public async Task Stop()
    {
        if (_tokenSource is null)
            return;
        _tokenSource.Cancel();
        try
        {
            if (_executingTask is null)
                return;
            await _executingTask;
        }
        catch { }
        finally
        {
            _tokenSource.Dispose();
        }
    }

    public Task Start()
    {
        _tokenSource = new CancellationTokenSource();

        // Store the task we're executing
        _executingTask = ExecuteAsync(_tokenSource.Token);

        // If the task is completed then return it, this will bubble cancellation and failure to the caller
        if (_executingTask.IsCompleted)
        {
            return _executingTask;
        }

        // Otherwise it's running
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        var outboxCount = OutboxPatternHelper.GetOutboxCount(_outboxOptions.Value);
        var tasks = new List<Task>();
        for (var outboxNo = 0; outboxNo < outboxCount; outboxNo++)
        {
            tasks.Add(ProcessOutbox(outboxNo, cancellationToken));
        }
        await Task.WhenAll(tasks);
    }

    private async Task<ReplicationMessage> GetMessage(InsertMessage message, CancellationToken cancellationToken)
    {
        var colNo = 0;
        var replicationMessage = new ReplicationMessage();
        await foreach (var column in message.NewRow)
        {
            switch (colNo)
            {
                case 0:
                    replicationMessage.MessageId = new Ulid(await column.Get<Guid>(cancellationToken));
                    break;

                case 1:
                    replicationMessage.CorrelationId = new Ulid(await column.Get<Guid>(cancellationToken));
                    break;

                case 2:
                    replicationMessage.PubSubName = await column.Get<string>(cancellationToken);
                    break;

                case 3:
                    replicationMessage.TopicName = await column.Get<string>(cancellationToken);
                    break;

                case 4:
                    replicationMessage.CreatedAt = await column.Get<DateTimeOffset>(cancellationToken);
                    break;

                case 5:
                    replicationMessage.MessageType = await column.Get<string>(cancellationToken);
                    break;

                case 6:
                    replicationMessage.Message = new ReadOnlyMemory<byte>(await column.Get<byte[]>(cancellationToken));
                    break;

                case 7:
                    replicationMessage.OutboxNo = await column.Get<short>(cancellationToken);
                    return replicationMessage;

                default:
                    break;

            }
            colNo++;
        }
        throw new UnreachableException("This code should be unreachable!");
    }

    private async Task ProcessOutbox(int outboxNo, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var conn = new LogicalReplicationConnection(_dbOptions.CurrentValue.ConnectionString);
                await conn.Open();

                var slot = new PgOutputReplicationSlot($"repslot_outbox{outboxNo:0}");

                // The following will loop until the cancellation token is triggered, and will process messages coming from PostgreSQL:
                await foreach (var message in conn.StartReplication(
                    slot, new PgOutputReplicationOptions($"pub_outbox{outboxNo:0}", 1, binary: true), cancellationToken))
                {
                    if (message is InsertMessage insertMessage)
                    {
                        var outboxMessage = await GetMessage(insertMessage, cancellationToken);
                        if (outboxMessage.Message is not null)
                        {
                            await _daprClient.PublishByteEventAsync(outboxMessage.PubSubName, outboxMessage.TopicName, outboxMessage.Message.Value, cancellationToken: cancellationToken);
                        }
                    }

                    // Always call SetReplicationStatus() or assign LastAppliedLsn and LastFlushedLsn individually
                    // so that Npgsql can inform the server which WAL files can be removed/recycled.
                    conn.SetReplicationStatus(message.WalEnd);
                }
            }
            //postgres replication slot can be accessed by only one consumer
            //however if multiple instance of this processor is started
            //(such as when this is used in a grainservice, and grainservices are created one per each silo, so whenever multiple silos are in a cluster)
            //all except one instance will encouter this behaviour.
            //So in such cases, this is expected.
            catch (PostgresException nex) when (nex.SqlState == "55006" && nex.Routine == "ReplicationSlotAcquire")
            {
                _logger.LogDebug(nex, "Postgres WAL replication loop cannot access replication slot. Possibly being read by another consumer.");
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Postgres WAL replication loop encountered an error.");
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
            }
        }
    }

    private class ReplicationMessage
    {
        public Ulid MessageId { get; set; }
        public Ulid CorrelationId { get; set; }
        public string PubSubName { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public string MessageType { get; set; } = string.Empty;
        public ReadOnlyMemory<byte>? Message { get; set; }
        public short OutboxNo { get; set; }
    }
}
