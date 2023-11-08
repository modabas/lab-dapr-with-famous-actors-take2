using Microsoft.Extensions.Options;
using Npgsql;
using NpgsqlTypes;
using PublisherService.Core.Database.OutboxPattern.Dto;
using PublisherService.Core.Database.OutboxPattern.OutboxSelector;
using PublisherService.Core.Database.OutboxPattern.Service;
using PublisherService.Core.Database.OutboxPattern.Utility;
using Shared.OutboxPattern;
using Shared.Utility;
using System.Data.Common;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Service;

public class OutboxPersistor : IOutboxPersistor
{
    private readonly IOutboxPatternDbContext _dbContext;
    private readonly ILogger<OutboxPersistor> _logger;
    private readonly IOptions<OutboxPatternOptions> _outboxOptions;
    private readonly IOutboxSelector _outboxSelector;

    public OutboxPersistor(ILogger<OutboxPersistor> logger,
        IOutboxPatternDbContext dbContext,
        IOptions<OutboxPatternOptions> outboxOptions,
        IOutboxSelector outboxSelector)
    {
        _logger = logger;
        _dbContext = dbContext;
        _outboxOptions = outboxOptions;
        _outboxSelector = outboxSelector;
    }

    private static readonly AsyncLocal<DbTransactionHolder> _dbTransactionCurrent = new AsyncLocal<DbTransactionHolder>();

    public DbTransaction? DbTransaction
    {
        get
        {
            return _dbTransactionCurrent.Value?.WrappedObject;
        }
        set
        {
            var holder = _dbTransactionCurrent.Value;
            if (holder != null)
            {
                // Clear current DbTransaction trapped in the AsyncLocals, as its done.
                holder.WrappedObject = null;
            }

            if (value != null)
            {
                // Use an object indirection to hold the DbTransaction in the AsyncLocal,
                // so it can be cleared in all ExecutionContexts when its cleared.
                _dbTransactionCurrent.Value = new DbTransactionHolder { WrappedObject = value };
            }
        }
    }

    private class DbTransactionHolder
    {
        public DbTransaction? WrappedObject;
    }

    private bool UseTransaction()
    {
        if (DbTransaction is null)
            return false;
        return true;
    }

    public async Task<OutboxMessageKey> CreateMessage<TMessage>(string pubSubName, string topicName, OutboxMessage<TMessage> message, CancellationToken cancellationToken)
    {
        var outboxNo = await _outboxSelector.DetermineOutboxNo(pubSubName, topicName, message, cancellationToken);
        return await CreateMessage(pubSubName, topicName, outboxNo, message, cancellationToken);
    }

    private async Task<OutboxMessageKey> CreateMessage<TMessage>(string pubSubName, string topicName, short outboxNo, OutboxMessage<TMessage> message, CancellationToken cancellationToken)
    {
        var messageJsonString = JsonHelper.SerializeToJsonUtf8Bytes(message);
        var messageType = message.GetType().AssemblyQualifiedName;
        var schemaName = OutboxPatternHelper.GetSchemaName(_outboxOptions.Value);
        var sql = 
            @$"INSERT INTO {schemaName}.tbl_outbox (message_id, correlation_id, pubsub_name, topic_name, message_content, message_type, outbox_no) 
             VALUES (@message_id, @correlation_id, @pubsub_name, @topic_name, @message_content, @message_type, @outbox_no) 
             RETURNING created_at;";

        if (UseTransaction())
        {
            //UseTransaction method does null check on dbTransaction
            var conn = DbTransaction!.Connection;
            if (conn is not null)
            {
                return await CreateMessageInternal(conn, DbTransaction);
            }
            else
                throw new ApplicationException("Cannot insert transactional outbox message. Connection is null");
        }
        else
        {
            using (var conn = _dbContext.GetConnection())
            {
                await conn.OpenAsync(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                return await CreateMessageInternal(conn, null);
            }
        }

        async Task<OutboxMessageKey> CreateMessageInternal(DbConnection conn, DbTransaction? tran)
        {
            var command = conn.CreateCommand();
            command.Transaction = tran;
            command.CommandText = sql;
            command.Parameters.Add(new NpgsqlParameter("message_id", NpgsqlDbType.Uuid) { Value = message.Id.ToGuid() });
            command.Parameters.Add(new NpgsqlParameter("correlation_id", NpgsqlDbType.Uuid) { Value = message.CorrelationId.ToGuid() });
            command.Parameters.Add(new NpgsqlParameter("pubsub_name", NpgsqlDbType.Text) { Value = pubSubName });
            command.Parameters.Add(new NpgsqlParameter("topic_name", NpgsqlDbType.Text) { Value = topicName });
            command.Parameters.Add(new NpgsqlParameter("message_content", NpgsqlDbType.Jsonb) { Value = messageJsonString });
            command.Parameters.Add(new NpgsqlParameter("message_type", NpgsqlDbType.Text) { Value = messageType });
            command.Parameters.Add(new NpgsqlParameter("outbox_no", NpgsqlDbType.Smallint) { Value = outboxNo });
            
            using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                return MapOutput(reader).Single();
            }

            IEnumerable<OutboxMessageKey> MapOutput(DbDataReader reader)
            {
                while (reader.Read())
                {
                    yield return new()
                    {
                        OutboxNo = outboxNo,
                        CreatedAt = DateTime.SpecifyKind(Convert.ToDateTime(reader[0]), DateTimeKind.Utc),
                        MessageId = message.Id
                    };
                }
            }
        }
    }

}
