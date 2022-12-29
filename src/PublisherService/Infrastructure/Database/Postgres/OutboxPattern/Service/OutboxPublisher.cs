using Dapper;
using PublisherService.Core.Database.OutboxPattern.Entity;
using PublisherService.Core.Database.OutboxPattern.Service;
using PublisherService.Core.Database.Service;
using PublisherService.Infrastructure.Database.Postgres.Dapper.QueryParameter;
using Shared.OutboxPattern;
using Shared.Utility;
using System.Data.Common;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Service;

public class OutboxPublisher : IOutboxPublisher
{
    private readonly IDbContext _dbContext;
    private readonly ILogger<OutboxPublisher> _logger;

    public OutboxPublisher(ILogger<OutboxPublisher> logger, IDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
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

    public async Task<OutboxPrimaryKey> CreateMessage<TMessage>(string pubSubName, string topicName, OutboxMessage<TMessage> message, CancellationToken cancellationToken)
    {
        var messageJsonString = JsonHelper.SerializeJson(message);
        var messageType = message.GetType().AssemblyQualifiedName;
        var sql = "INSERT INTO outbox_pattern.tbl_outbox (pubsub_name, topic_name, message_content, message_type) VALUES (@pubsub_name, @topic_name, @message_content, @message_type) RETURNING created_date, position;";

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

        async Task<OutboxPrimaryKey> CreateMessageInternal(DbConnection? conn, DbTransaction? tran)
        {
            return (await conn.QueryAsync<DateTime, long, OutboxPrimaryKey>(new CommandDefinition(sql,
                new
                {
                    pubsub_name = pubSubName,
                    topic_name = topicName,
                    message_content = new JsonbParameter(messageJsonString),
                    message_type = messageType
                }, transaction: tran, cancellationToken: cancellationToken),
                (createdDate, position) =>
                {
                    return new() { CreatedDate = createdDate, Position = position };
                },
                splitOn: "position")).Single();
        }
    }
}
