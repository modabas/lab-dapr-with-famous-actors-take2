using PublisherService.Core.Database.OutboxPattern.Service;
using System.Data;
using System.Data.Common;

namespace PublisherService.Infrastructure.Database.Postgres.OutboxPattern.Extensions;

public static class DbConnectionExtensions
{
    //!!!IMPORTANT
    //if we override BeginTransactionAsync and implement with async keyword,
    //AsyncLocal parameter DbTransaction of calling repo will be lost when async method will return to caller
    //we have to stay in current async context or in its children
    //so we only use sync BeginTransaction overloads to set DbTransaction
    public static DbTransaction BeginTransaction(this DbConnection conn, IOutboxPersistor? outboxPersistor = null)
    {
        var transaction = conn.BeginTransaction();
        if (outboxPersistor is not null)
            outboxPersistor.DbTransaction = transaction;
        return transaction;
    }

    public static DbTransaction BeginTransaction(this DbConnection conn, IsolationLevel isolationLevel, IOutboxPersistor? outboxPersistor = null)
    {
        var transaction = conn.BeginTransaction(isolationLevel);
        if (outboxPersistor is not null)
            outboxPersistor.DbTransaction = transaction;
        return transaction;
    }
}
