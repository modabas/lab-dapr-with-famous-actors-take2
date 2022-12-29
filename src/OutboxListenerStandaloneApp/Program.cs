using Npgsql;
using Npgsql.Replication;
using Npgsql.Replication.PgOutput;
using Npgsql.Replication.PgOutput.Messages;
using Shared.Utility;

namespace OutboxListenerStandaloneApp;

internal class Program
{
    static async Task Main(string[] args)
    {
        using (var tokenSource = new CancellationTokenSource())
        {
            var cancellationToken = tokenSource.Token;
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var connectionString = "User ID=postgres;Password=password;Host=localhost;Port=5432;Database=dapr_outbox;";
                    await using var conn = new LogicalReplicationConnection(connectionString);
                    await conn.Open();

                    var slot = new PgOutputReplicationSlot("repslot_outbox");

                    // The following will loop until the cancellation token is triggered, and will process messages coming from PostgreSQL:
                    await foreach (var message in conn.StartReplication(
                        slot, new PgOutputReplicationOptions("pub_outbox", 1), cancellationToken))
                    {
                        if (message is InsertMessage insertMessage)
                        {
                            var outboxMessage = await GetMessage(insertMessage, cancellationToken);
                            var messageContent = JsonHelper.SerializeJson(outboxMessage);
                            Console.WriteLine($"Fetched outbox message with content: {messageContent}");
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
                    Console.WriteLine($"Postgres WAL replication loop cannot access replication slot. Possibly being read by another consumer. {nex.ToString()}");
                    await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Postgres WAL replication loop encountered an error. {ex.ToString()}");
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                }
            }
        }
    }

    private static async Task<ReplicationMessage> GetMessage(InsertMessage message, CancellationToken cancellationToken)
    {
        var colNo = 0;
        var pubSubName = string.Empty;
        var topicName = string.Empty;
        var messageType = string.Empty;
        await foreach (var column in message.NewRow)
        {
            switch (colNo)
            {
                case 1:
                    pubSubName = await column.Get<string>(cancellationToken);
                    break;

                case 2:
                    topicName = await column.Get<string>(cancellationToken);
                    break;

                case 5:
                    messageType = await column.Get<string>(cancellationToken);
                    break;

                case 6:
                    var messageSystemType = Type.GetType(messageType);
                    if (messageSystemType is null)
                        throw new ArgumentOutOfRangeException(nameof(messageSystemType));
                    return new ReplicationMessage()
                    {
                        Message = await JsonHelper.DeserializeJsonAsync(column.GetStream(), messageSystemType, cancellationToken),
                        PubSubName = pubSubName,
                        TopicName = topicName
                    };

                default:
                    break;

            }
            colNo++;
        }
        throw new InvalidOperationException("This code should be unreachable!");
    }

    private class ReplicationMessage
    {
        public string PubSubName { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public object? Message { get; set; }
    }
}