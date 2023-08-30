using Dapper;
using Microsoft.Extensions.Options;
using PublisherService.Core.Database.OutboxPattern.Dto;
using PublisherService.Core.Database.OutboxPattern.Service;
using PublisherService.Core.Database.Service;
using PublisherService.Infrastructure.Database.Postgres.OutboxPattern.QueryParameter;
using PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Utility;
using Shared.OutboxPattern;
using Shared.Utility;
using System.Data.Common;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Service;

public class OutboxPublisher : IOutboxPublisher
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<OutboxPublisher> _logger;
    private readonly IOptions<OutboxPatternOptions> _outboxOptions;

    public OutboxPublisher(ILogger<OutboxPublisher> logger, 
        IDbContext dbContext,
        IOptions<OutboxPatternOptions> outboxOptions)
    {
        _logger = logger;
        _dbContext = dbContext;
        _outboxOptions = outboxOptions;
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
        var outboxNo = OutboxPatternHelper.RandomOutboxNo(_outboxOptions.Value);
        return await CreateMessage(pubSubName, topicName, outboxNo, message, cancellationToken);
    }

    public async Task<OutboxMessageKey> CreateMessage<TMessage>(string pubSubName, string topicName, Guid correlationId, OutboxMessage<TMessage> message, CancellationToken cancellationToken)
    {
        var outboxNo = OutboxPatternHelper.DetermineOutboxNo(correlationId, _outboxOptions.Value);
        return await CreateMessage(pubSubName, topicName, outboxNo, message, cancellationToken);
    }

    private async Task<OutboxMessageKey> CreateMessage<TMessage>(string pubSubName, string topicName, short outboxNo, OutboxMessage<TMessage> message, CancellationToken cancellationToken)
    {
        var messageJsonString = JsonHelper.SerializeJson(message);
        var messageType = message.GetType().AssemblyQualifiedName;
        var schemaName = OutboxPatternHelper.GetSchemaName(_outboxOptions.Value);
        var sql = 
            @$"INSERT INTO {schemaName}.tbl_outbox (pubsub_name, topic_name, message_content, message_type, outbox_no) 
             VALUES (@pubsub_name, @topic_name, @message_content, @message_type, @outbox_no) 
             RETURNING created_date, position;";

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
                return await CreateMessageInternal(conn, null);
            }
        }

        async Task<OutboxMessageKey> CreateMessageInternal(DbConnection? conn, DbTransaction? tran)
        {
            return (await conn.QueryAsync<DateTime, long, OutboxMessageKey>(new CommandDefinition(sql,
                new
                {
                    pubsub_name = pubSubName,
                    topic_name = topicName,
                    message_content = new JsonbParameter(messageJsonString),
                    message_type = messageType,
                    outbox_no = outboxNo,
                }, transaction: tran, cancellationToken: cancellationToken),
                (createdDate, position) =>
                {
                    return new() { OutboxNo = outboxNo, CreatedDate = createdDate, Position = position };
                },
                splitOn: "position")).Single();
        }
    }

}
